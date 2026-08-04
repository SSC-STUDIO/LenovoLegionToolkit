using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
    private bool TryCreateLocalPackageFromInstalledFiles(PluginManifest manifest, string destinationPath)
    {
        try
        {
            var localPluginDirectory = FindLocalPluginDirectory(manifest.Id);
            if (localPluginDirectory == null)
                return false;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Attempting local package fallback for {manifest.Id} from {localPluginDirectory}");

            // Basic sanity check: ensure the directory contains at least one plugin DLL.
            var mainDll = FindPluginMainDll(localPluginDirectory, manifest.Id);
            if (string.IsNullOrWhiteSpace(mainDll))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Local package fallback aborted for {manifest.Id}: no plugin DLL in {localPluginDirectory}");
                return false;
            }

            var localVersion = TryReadLocalPluginVersion(localPluginDirectory);
            if (!IsLocalPackageVersionUsableForFallback(manifest.Version, localVersion))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Local package fallback aborted for {manifest.Id}: local version '{localVersion ?? "<unknown>"}' is older than requested version '{manifest.Version}'");

                return false;
            }

            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            ZipFile.CreateFromDirectory(localPluginDirectory, destinationPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            var zipFileInfo = new FileInfo(destinationPath);
            DownloadProgressChanged?.Invoke(this, new PluginDownloadProgress
            {
                PluginId = manifest.Id,
                BytesDownloaded = zipFileInfo.Length,
                TotalBytes = zipFileInfo.Length,
                ProgressPercentage = 100
            });

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Local package fallback failed for {manifest.Id}: {ex.Message}", ex);
            return false;
        }
    }

    private static bool IsLocalPackageVersionUsableForFallback(string requestedVersion, string? localVersion)
    {
        if (string.IsNullOrWhiteSpace(requestedVersion))
            return true;

        if (string.IsNullOrWhiteSpace(localVersion))
            return false;

        if (PluginVersionParser.TryParse(localVersion, out var parsedLocalVersion) &&
            PluginVersionParser.TryParse(requestedVersion, out var parsedRequestedVersion))
        {
            return parsedLocalVersion >= parsedRequestedVersion;
        }

        return string.Equals(localVersion.Trim(), requestedVersion.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryReadLocalPluginVersion(string pluginDirectory)
    {
        foreach (var manifestPath in EnumerateLocalVersionManifestPaths(pluginDirectory))
        {
            try
            {
                using var stream = File.OpenRead(manifestPath);
                using var document = JsonDocument.Parse(stream);

                var version = TryGetJsonStringProperty(document.RootElement, "version");
                if (!string.IsNullOrWhiteSpace(version))
                    return version;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to read local plugin version from {manifestPath}: {ex.Message}", ex);
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateLocalVersionManifestPaths(string pluginDirectory)
    {
        var candidateNames = new[]
        {
            "plugin.manifest.json",
            "plugin.json",
            "Plugin.json"
        };

        return candidateNames
            .Select(fileName => Path.Combine(pluginDirectory, fileName))
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? TryGetJsonStringProperty(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private string? FindLocalPluginDirectory(string pluginId)
    {
        try
        {
            if (!Directory.Exists(_pluginsDirectory))
                return null;

            var directCandidate = Path.Combine(_pluginsDirectory, pluginId);
            if (Directory.Exists(directCandidate))
                return directCandidate;

            var localCandidate = Path.Combine(_pluginsDirectory, "local", pluginId);
            if (Directory.Exists(localCandidate))
                return localCandidate;

            var normalizedPluginId = NormalizePluginToken(pluginId);
            var directories = Directory.GetDirectories(_pluginsDirectory, "*", SearchOption.TopDirectoryOnly)
                .Concat(Directory.Exists(Path.Combine(_pluginsDirectory, "local"))
                    ? Directory.GetDirectories(Path.Combine(_pluginsDirectory, "local"), "*", SearchOption.TopDirectoryOnly)
                    : Array.Empty<string>());

            foreach (var directory in directories)
            {
                var directoryName = Path.GetFileName(directory);
                var normalizedDirectoryName = NormalizePluginToken(directoryName);
                var normalizedDirectoryShortName = NormalizePluginToken(
                    PluginAssemblyNaming.StripPluginPrefixForNormalization(directoryName));

                if (normalizedDirectoryName.Equals(normalizedPluginId, StringComparison.OrdinalIgnoreCase) ||
                    normalizedDirectoryShortName.Equals(normalizedPluginId, StringComparison.OrdinalIgnoreCase))
                {
                    return directory;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error locating local plugin directory for {pluginId}: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Production builds always require store ZIP/DLL hashes unless explicitly waived.
    /// Official online packages require hashes even in DEBUG (local/dev fallback may omit them).
    /// </summary>
    private static bool RequirePackageIntegrity(bool trustAsOfficialOnlinePackage) =>
        !PluginPackageIntegrity.IsVerificationWaived()
        && (IsProductionMode || trustAsOfficialOnlinePackage);

    private static async Task<bool> VerifyDownloadedPackageIntegrityAsync(
        string zipPath,
        PluginManifest manifest,
        bool trustAsOfficialOnlinePackage)
    {
        var require = RequirePackageIntegrity(trustAsOfficialOnlinePackage);
        try
        {
            var zipHash = await PluginPackageIntegrity.ComputeSha256HexAsync(zipPath).ConfigureAwait(false);
            if (!PluginPackageIntegrity.TryVerifyExpectedHash(
                    manifest.ZipHash,
                    zipHash,
                    require,
                    out var zipIntegrityFailure))
            {
                Log.Instance.Warning($"Plugin ZIP integrity check failed for {manifest.Id}: {zipIntegrityFailure}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(manifest.ZipHash) && !require)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {manifest.Id} has no zipHash in store manifest; skipping ZIP integrity verification.");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Plugin ZIP integrity check failed for {manifest.Id}: {ex.Message}", ex);
            return false;
        }
    }

    private static string? FindPluginMainDll(string extractPath, string pluginId)
    {
        var pluginDlls = Directory.GetFiles(extractPath, "*.dll", SearchOption.AllDirectories)
            .Where(path =>
            {
                var fileName = Path.GetFileName(path);
                return !fileName.Contains(".resources.dll", StringComparison.OrdinalIgnoreCase) &&
                       !PluginAssemblyNaming.IsSdkOrSharedDllFileName(fileName);
            })
            .ToList();

        if (!pluginDlls.Any())
            return null;

        var exactMatch = pluginDlls.FirstOrDefault(path =>
            Path.GetFileNameWithoutExtension(path).Equals(pluginId, StringComparison.OrdinalIgnoreCase));
        if (exactMatch != null)
            return exactMatch;

        var normalizedPluginId = NormalizePluginToken(pluginId);
        var normalizedMatches = pluginDlls
            .Where(path => NormalizePluginToken(Path.GetFileNameWithoutExtension(path))
                .Equals(normalizedPluginId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (normalizedMatches.Count == 1)
            return normalizedMatches[0];

        if (normalizedMatches.Count > 1)
        {
            return normalizedMatches.FirstOrDefault(path =>
                PluginAssemblyNaming.IsPluginPrefixedFileName(Path.GetFileName(path)))
                ?? normalizedMatches[0];
        }

        var prefixedMatch = pluginDlls.FirstOrDefault(path =>
        {
            var fileName = Path.GetFileName(path);
            if (!PluginAssemblyNaming.IsPluginPrefixedFileName(fileName))
                return false;

            var normalizedFileName = NormalizePluginToken(Path.GetFileNameWithoutExtension(path));
            return normalizedFileName.Contains(normalizedPluginId, StringComparison.OrdinalIgnoreCase);
        });

        return prefixedMatch ?? pluginDlls[0];
    }

    private static string NormalizePluginToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var chars = value.Where(char.IsLetterOrDigit).ToArray();
        return new string(chars).ToLowerInvariant();
    }

    private static bool ShouldSkipPluginPayloadFile(string filePath)
    {
        return PluginAssemblyNaming.IsSdkOrSharedDllFileName(Path.GetFileName(filePath));
    }

    private static void TryStageCanonicalPluginSharedAssembly(string pluginDirectory)
    {
        var sourcePath = PluginAssemblyNaming.EnumerateAppBaseSharedCandidates().FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        try
        {
            PluginAssemblyNaming.StageDualNamedSharedDll(sourcePath, pluginDirectory);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to stage canonical plugin shared runtime into {pluginDirectory}: {ex.Message}", ex);
        }
    }

    private static void TryStageCanonicalPluginSdkAssembly(string pluginDirectory)
    {
        var sourcePath = PluginAssemblyNaming.EnumerateAppBaseSdkCandidates().FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        try
        {
            PluginAssemblyNaming.StageDualNamedSdkDll(sourcePath, pluginDirectory);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to stage canonical plugin SDK runtime into {pluginDirectory}: {ex.Message}", ex);
        }
    }
}
