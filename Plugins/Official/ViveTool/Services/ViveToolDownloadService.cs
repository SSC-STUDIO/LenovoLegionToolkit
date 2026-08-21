using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Net.Http;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Plugins.Core;
using UniversalDeviceToolkit.Plugins.ViveTool.Utils;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Services;

/// <summary>
/// Handles downloading and importing feature configurations.
/// </summary>
public class ViveToolDownloadService
{
    // Official ViVeTool release asset (ZIP file containing ViVeTool.exe).
    // Pinned to the v0.3.4 tag so /latest cannot silently swap the payload.
    public const string DefaultViveToolDownloadUrl = "https://github.com/thebookisclosed/ViVe/releases/download/v0.3.4/ViVeTool-v0.3.4-IntelAmd.zip";
    private const int MaxImportContentBytes = 1024 * 1024;
    private const int MaxDownloadBytes = 32 * 1024 * 1024;
    private const string ExpectedViveToolZipSha256 = "cc27f073f3fe5dd2c3d947faf558fd4b2f8e34454f812689b0d65ee8a52e4147";

    private static readonly IReadOnlyDictionary<string, string> ExpectedBuiltInFileHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [ViveToolPathService.ViveToolExeName] = "d3b69c982622a26ad0b37c65b8f006b5139e50aeb45fda68734a33ca28706dea",
        ["Albacore.ViVe.dll"] = "f57e02e954244781d86a7c1a849be24142c4aa4883c07aa9cd93be0919d2e50c",
        ["Newtonsoft.Json.dll"] = "e1e27af7b07eeedf5ce71a9255f0422816a6fc5849a483c6714e1b472044fa9d",
        ["FeatureDictionary.pfs"] = "8ee86b7abd13390d06f251de998fb578e149cc42e7ea9114212ff6af4c956828"
    };

    private static readonly SemaphoreSlim DownloadLock = new(1, 1);

    private readonly ViveToolPathService _pathService;

    public ViveToolDownloadService(ViveToolPathService pathService)
    {
        _pathService = pathService;
    }

    public async Task<bool> DownloadViveToolAsync(System.IProgress<long>? progress = null)
    {
        await DownloadLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var bundledPath = _pathService.GetBundledViveToolPath();
            if (ViveToolPathService.IsInstallComplete(Path.GetDirectoryName(bundledPath)))
            {
                _pathService.CachedPath = bundledPath;
                return true;
            }

            var builtInPath = _pathService.GetBuiltInViveToolPath();
            var builtInDir = Path.GetDirectoryName(builtInPath);
            if (ViveToolPathService.IsInstallComplete(builtInDir))
            {
                _pathService.CachedPath = builtInPath;
                return true;
            }

            if (string.IsNullOrEmpty(builtInDir))
            {
                PluginLog.Trace("ViveTool: Cannot determine built-in installation directory");
                return false;
            }

            if (!Directory.Exists(builtInDir))
            {
                Directory.CreateDirectory(builtInDir);
            }

            // Download ZIP file to temporary location
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"ViVeTool_{Guid.NewGuid()}.zip");
            var stagingDirectory = Path.Combine(Path.GetTempPath(), $"ViVeTool_extract_{Guid.NewGuid():N}");
            try
            {
                using var httpClient = HttpClientManager.CreateClientWithTimeout(
                    Constants.DownloadTimeoutSeconds);

                // Get the response as a stream to track progress
                // Wrap in using so the connection is returned to the pool even when
                // EnsureSuccessStatusCode() throws on a non-2xx status. Without this,
                // the HttpResponseMessage is never disposed on the failure path and
                // the underlying socket is leaked (the sibling ImportFeaturesFromUrlAsync
                // method already uses `using var response` — this matches that pattern).
                using var response = await httpClient.GetAsync(DefaultViveToolDownloadUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                if (!IsTrustedDownloadUri(response.RequestMessage?.RequestUri))
                {
                    PluginLog.Trace($"ViveTool: Download redirected to an untrusted host: {response.RequestMessage?.RequestUri}");
                    return false;
                }

                if (response.Content.Headers.ContentLength is long zipContentLength && zipContentLength > MaxDownloadBytes)
                {
                    PluginLog.Trace($"ViveTool: Download exceeds size limit: {zipContentLength} bytes");
                    return false;
                }

                long downloadedBytes = 0;

                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var hashAlgorithm = SHA256.Create())
                {
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                    {
                        downloadedBytes += bytesRead;
                        if (downloadedBytes > MaxDownloadBytes)
                        {
                            PluginLog.Trace($"ViveTool: Download exceeded size limit after {downloadedBytes} bytes");
                            return false;
                        }

                        await fileStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                        hashAlgorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
                        progress?.Report(downloadedBytes);
                    }

                    hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    if (hashAlgorithm.Hash is not { Length: > 0 } hashBytes)
                    {
                        PluginLog.Trace("ViveTool: ZIP hash could not be computed");
                        return false;
                    }

                    var actualZipHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
                    if (!actualZipHash.Equals(ExpectedViveToolZipSha256, StringComparison.OrdinalIgnoreCase))
                    {
                        PluginLog.Trace($"ViveTool: ZIP hash mismatch. Expected {ExpectedViveToolZipSha256}, actual {actualZipHash}.");
                        return false;
                    }
                }

                Directory.CreateDirectory(stagingDirectory);
                using (var archive = ZipFile.OpenRead(tempZipPath))
                {
                    var exeEntry = archive.GetEntry(ViveToolPathService.ViveToolExeName) ?? archive.Entries.FirstOrDefault(e =>
                        e.Name.Equals(ViveToolPathService.ViveToolExeName, StringComparison.OrdinalIgnoreCase));
                    if (exeEntry == null)
                    {
                        PluginLog.Trace($"ViveTool: {ViveToolPathService.ViveToolExeName} not found in ZIP archive");
                        return false;
                    }

                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            continue;
                        }

                        if (!ExpectedBuiltInFileHashes.ContainsKey(entry.Name))
                        {
                            PluginLog.Trace($"ViveTool: Skipping unexpected ZIP entry: {entry.FullName}");
                            continue;
                        }

                        if (!ViveToolPathGuard.TryGetSafeZipDestination(entry, stagingDirectory, out var destinationPath))
                        {
                            PluginLog.Trace($"SECURITY: Skipping unsafe ZIP entry: {entry.FullName}");
                            continue;
                        }

                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                }

                TryDeleteFile(tempZipPath);

                if (!ViveToolPathService.IsInstallComplete(stagingDirectory))
                {
                    PluginLog.Trace("ViveTool: Extracted archive is missing required runtime files");
                    return false;
                }

                if (!VerifyKnownInstallHashes(stagingDirectory))
                {
                    PluginLog.Trace("ViveTool: Extracted archive failed file hash verification");
                    return false;
                }

                //
                // Atomic install: swap the staging directory into the final location
                // in a crash-safe manner. The original approach copied files one by
                // one with File.Copy — if the process crashed or power was lost
                // midway through that loop, the built-in directory ended up with an
                // inconsistent mix of old and new files (e.g. a new ViVeTool.exe
                // referencing an old Albacore.ViVe.dll), causing silent runtime
                // failures on the next launch.
                //
                // The new strategy:
                //   1. If the built-in dir exists, rename it to a backup directory.
                //   2. Move the staging directory into the built-in location.
                //   3. Verify the new install (IsInstallComplete + hash check).
                //   4. On success, delete the backup. On failure, roll back.
                //
                // Directory.Move is atomic on the same volume (NTFS). Both paths
                // are under %LocalAppData% so same-volume is the common case. If a
                // cross-volume move is needed, .NET falls back to copy+delete
                // internally, which is still safer than per-file copies because the
                // old install is preserved as a backup until the new one is verified.
                //
                var backupDir = builtInDir + $".old_{Guid.NewGuid():N}";
                var hadExistingInstall = Directory.Exists(builtInDir) &&
                    Directory.GetFiles(builtInDir, "*", SearchOption.TopDirectoryOnly).Length > 0;

                if (hadExistingInstall)
                {
                    Directory.Move(builtInDir, backupDir);
                }

                try
                {
                    // Move the verified staging directory into the final location.
                    Directory.Move(stagingDirectory, builtInDir);

                    if (!ViveToolPathService.IsInstallComplete(builtInDir))
                    {
                        PluginLog.Trace("ViveTool: Built-in install directory is incomplete after swap");
                        RollbackInstallSwap(builtInDir, backupDir, hadExistingInstall);
                        return false;
                    }

                    if (!VerifyKnownInstallHashes(builtInDir))
                    {
                        PluginLog.Trace("ViveTool: Built-in install directory failed file hash verification");
                        RollbackInstallSwap(builtInDir, backupDir, hadExistingInstall);
                        return false;
                    }
                }
                catch (Exception swapEx)
                {
                    PluginLog.Trace($"ViveTool: Atomic directory swap failed: {swapEx.Message}", swapEx);
                    RollbackInstallSwap(builtInDir, backupDir, hadExistingInstall);
                    return false;
                }

                // Success — clean up the old backup if it exists.
                if (Directory.Exists(backupDir))
                {
                    try
                    {
                        Directory.Delete(backupDir, recursive: true);
                    }
                    catch (Exception cleanupEx)
                    {
                        PluginLog.Trace($"ViveTool: Backup directory cleanup failed: {cleanupEx.Message}", cleanupEx);
                    }
                }

                PluginLog.Trace($"ViveTool: Downloaded and extracted built-in ViVeTool and dependencies to {builtInDir}");

                _pathService.CachedPath = builtInPath;
                return true;
            }
            finally
            {
                TryDeleteFile(tempZipPath);

                try
                {
                    if (Directory.Exists(stagingDirectory))
                    {
                        Directory.Delete(stagingDirectory, recursive: true);
                    }
                }
                catch (Exception cleanupEx)
                {
                    PluginLog.Trace($"ViveTool: Temporary download cleanup failed: {cleanupEx.Message}", cleanupEx);
                }
            }
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Failed to download built-in ViVeTool: {ex.Message}", ex);
            return false;
        }
        finally
        {
            DownloadLock.Release();
        }
    }

    public async Task<List<FeatureFlagInfo>> ImportFeaturesFromFileAsync(string filePath)
    {
        try
        {
            if (!ViveToolPathGuard.TryNormalizeUserFilePath(filePath, out var normalizedPath))
            {
                PluginLog.Trace($"ViveTool: Import file path is not allowed: {filePath}");
                return new List<FeatureFlagInfo>();
            }

            if (!File.Exists(normalizedPath))
            {
                PluginLog.Trace($"ViveTool: Import file not found: {normalizedPath}");
                return new List<FeatureFlagInfo>();
            }

            var fileInfo = new FileInfo(normalizedPath);
            if (fileInfo.Length > MaxImportContentBytes)
            {
                PluginLog.Trace($"ViveTool: Import file exceeds size limit: {normalizedPath}");
                return new List<FeatureFlagInfo>();
            }

            var content = await File.ReadAllTextAsync(normalizedPath).ConfigureAwait(false);
            return ParseImportContent(content);
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Error importing features from file: {ex.Message}", ex);
            return new List<FeatureFlagInfo>();
        }
    }

    public async Task<List<FeatureFlagInfo>> ImportFeaturesFromUrlAsync(string url)
    {
        try
        {
            if (!TryValidateImportUri(url, out var uri))
            {
                return new List<FeatureFlagInfo>();
            }

            using var httpClient = HttpClientManager.CreateClientWithTimeout(
                Constants.DownloadTimeoutSeconds);
            using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            if (!TryValidateImportUri(response.RequestMessage?.RequestUri?.ToString(), out _))
            {
                PluginLog.Trace($"ViveTool: Import URL redirected to an untrusted location: {response.RequestMessage?.RequestUri}");
                return new List<FeatureFlagInfo>();
            }

            if (response.Content.Headers.ContentLength is long contentLength && contentLength > MaxImportContentBytes)
            {
                PluginLog.Trace($"ViveTool: Import URL content exceeds size limit: {uri}");
                return new List<FeatureFlagInfo>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var content = await ReadContentWithLimitAsync(stream, MaxImportContentBytes).ConfigureAwait(false);
            return ParseImportContent(content);
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Error importing features from URL: {ex.Message}", ex);
            return new List<FeatureFlagInfo>();
        }
    }

    public async Task<bool> ExportFeaturesToFileAsync(string filePath, IReadOnlyCollection<FeatureFlagInfo> features)
    {
        try
        {
            if (!ViveToolPathGuard.TryNormalizeUserFilePath(filePath, out var normalizedPath))
            {
                return false;
            }

            ArgumentNullException.ThrowIfNull(features);

            var directoryPath = Path.GetDirectoryName(normalizedPath);
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return false;
            }

            Directory.CreateDirectory(directoryPath);

            var exportPayload = features
                .OrderBy(feature => feature.Id)
                .Select(feature => new
                {
                    id = feature.Id,
                    name = feature.Name,
                    description = feature.Description,
                    status = feature.Status.ToString()
                })
                .ToArray();

            var json = JsonSerializer.Serialize(
                exportPayload,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

            await AtomicWriteAllTextAsync(normalizedPath, json).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Error exporting features to file: {ex.Message}", ex);
            return false;
        }
    }

    private List<FeatureFlagInfo> ParseImportContent(string content)
    {
        var features = new List<FeatureFlagInfo>();

        if (string.IsNullOrWhiteSpace(content))
        {
            return features;
        }

        try
        {
            // Try to parse as JSON first
            var jsonDoc = JsonDocument.Parse(content);
            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    var feature = ParseJsonFeature(element);
                    if (feature != null)
                    {
                        features.Add(feature);
                    }
                }
            }
            else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
            {
                // Single object or object with array property
                if (jsonDoc.RootElement.TryGetProperty("features", out var featuresArray))
                {
                    foreach (var element in featuresArray.EnumerateArray())
                    {
                        var feature = ParseJsonFeature(element);
                        if (feature != null)
                        {
                            features.Add(feature);
                        }
                    }
                }
                else
                {
                    var feature = ParseJsonFeature(jsonDoc.RootElement);
                    if (feature != null)
                    {
                        features.Add(feature);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON, try parsing as text (one ID per line or CSV)
            features = ParseTextContent(content);
        }

        return features;
    }

    private FeatureFlagInfo? ParseJsonFeature(JsonElement element)
    {
        try
        {
            var id = 0;
            var name = string.Empty;
            var description = string.Empty;

            if (element.TryGetProperty("id", out var idElement))
            {
                id = idElement.GetInt32();
            }
            else if (element.TryGetProperty("Id", out var idElement2))
            {
                id = idElement2.GetInt32();
            }
            else if (element.TryGetProperty("featureId", out var idElement3))
            {
                id = idElement3.GetInt32();
            }
            else if (element.TryGetProperty("FeatureId", out var idElement4))
            {
                id = idElement4.GetInt32();
            }

            if (id <= 0)
            {
                return null;
            }

            if (element.TryGetProperty("name", out var nameElement))
            {
                name = nameElement.GetString() ?? string.Empty;
            }
            else if (element.TryGetProperty("Name", out var nameElement2))
            {
                name = nameElement2.GetString() ?? string.Empty;
            }

            if (element.TryGetProperty("description", out var descElement))
            {
                description = descElement.GetString() ?? string.Empty;
            }
            else if (element.TryGetProperty("Description", out var descElement2))
            {
                description = descElement2.GetString() ?? string.Empty;
            }

            return new FeatureFlagInfo
            {
                Id = id,
                Name = string.IsNullOrEmpty(name) ? $"Feature {id}" : name,
                Description = description,
                Status = FeatureFlagStatus.Unknown
            };
        }
        catch
        {
            return null;
        }
    }

    private List<FeatureFlagInfo> ParseTextContent(string content)
    {
        var features = new List<FeatureFlagInfo>();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length >= 1 && TryParseFeatureId(parts[0].Trim(), out var id))
            {
                var name = parts.Length > 1 ? parts[1].Trim() : string.Empty;
                var description = parts.Length > 2 ? parts[2].Trim() : string.Empty;

                features.Add(new FeatureFlagInfo
                {
                    Id = id,
                    Name = string.IsNullOrEmpty(name) ? $"Feature {id}" : name,
                    Description = description,
                    Status = FeatureFlagStatus.Unknown
                });
            }
            else if (TryParseFeatureId(line.Trim(), out var simpleId))
            {
                features.Add(new FeatureFlagInfo
                {
                    Id = simpleId,
                    Name = $"Feature {simpleId}",
                    Description = string.Empty,
                    Status = FeatureFlagStatus.Unknown
                });
            }
        }

        return features;
    }

    /// <summary>
    /// Rollback helper for the atomic directory swap. If the new install
    /// failed verification, restore the old directory and remove the
    /// incomplete new one so the system is left in a consistent state.
    /// </summary>
    private static void RollbackInstallSwap(string builtInDir, string backupDir, bool hadExistingInstall)
    {
        try
        {
            // Remove the incomplete/bad new install directory if it exists.
            if (Directory.Exists(builtInDir))
            {
                Directory.Delete(builtInDir, recursive: true);
            }

            // Restore the old install from the backup directory.
            if (hadExistingInstall && Directory.Exists(backupDir))
            {
                Directory.Move(backupDir, builtInDir);
            }
        }
        catch (Exception rollbackEx)
        {
            PluginLog.Trace($"ViveTool: Rollback of failed install swap encountered an error: {rollbackEx.Message}", rollbackEx);
        }
    }

    private static bool VerifyKnownInstallHashes(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return false;
        }

        foreach (var expectedFile in ExpectedBuiltInFileHashes)
        {
            var filePath = Path.Combine(directoryPath, expectedFile.Key);
            if (!File.Exists(filePath))
            {
                return false;
            }

            using var stream = File.OpenRead(filePath);
            var actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!actualHash.Equals(expectedFile.Value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryValidateImportUri(string? url, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUri))
        {
            return false;
        }

        uri = parsedUri;

        if (!uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return false;
        }

        if (uri.IsLoopback || uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IPAddress.TryParse(uri.Host, out var address))
        {
            return !IsPrivateOrReservedAddress(address);
        }

        return !ResolvesToPrivateOrReservedAddress(uri.Host);
    }

    private static bool IsTrustedDownloadUri(Uri? uri)
    {
        if (uri is null ||
            !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        return uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Equals("objects.githubusercontent.com", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Equals("release-assets.githubusercontent.com", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Equals("github-releases.githubusercontent.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ResolvesToPrivateOrReservedAddress(string host)
    {
        try
        {
            var addresses = Dns.GetHostAddresses(host);
            if (addresses.Length == 0)
            {
                return true;
            }

            return addresses.Any(IsPrivateOrReservedAddress);
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            return true;
        }
    }

    private static bool IsPrivateOrReservedAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            return IsPrivateOrReservedAddress(address.MapToIPv4());
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            return bytes[0] == 10 ||
                   bytes[0] == 127 ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 169 && bytes[1] == 254) ||
                   (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) ||
                   bytes[0] == 0;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            var isUniqueLocal = (bytes[0] & 0xFE) == 0xFC;
            return isUniqueLocal ||
                   address.IsIPv6LinkLocal ||
                   address.IsIPv6SiteLocal ||
                   address.IsIPv6Multicast ||
                   address.Equals(IPAddress.IPv6Any) ||
                   address.Equals(IPAddress.IPv6None);
        }

        return false;
    }

    private static async Task<string> ReadContentWithLimitAsync(Stream stream, int maxBytes)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];

        while (true)
        {
            var bytesRead = await stream.ReadAsync(chunk, 0, chunk.Length).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                break;
            }

            if (buffer.Length + bytesRead > maxBytes)
            {
                throw new InvalidOperationException("Import content exceeds size limit.");
            }

            await buffer.WriteAsync(chunk, 0, bytesRead).ConfigureAwait(false);
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static bool TryParseFeatureId(string? text, out int id)
    {
        return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0;
    }

    private static async Task AtomicWriteAllTextAsync(string path, string contents)
    {
        var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(tempPath, contents).ConfigureAwait(false);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            TryDeleteFile(tempPath);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception cleanupEx)
        {
            PluginLog.Trace($"ViveTool: Temporary file cleanup failed: {cleanupEx.Message}", cleanupEx);
        }
    }
}
