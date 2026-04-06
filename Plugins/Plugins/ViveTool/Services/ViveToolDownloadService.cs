using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.ViveTool.Services;

/// <summary>
/// Handles downloading and importing feature configurations.
/// </summary>
public class ViveToolDownloadService
{
    // Official ViVeTool release asset (ZIP file containing ViVeTool.exe)
    public const string DefaultViveToolDownloadUrl = "https://github.com/thebookisclosed/ViVe/releases/latest/download/ViVeTool-v0.3.4-IntelAmd.zip";

    private readonly ViveToolPathService _pathService;

    public ViveToolDownloadService(ViveToolPathService pathService)
    {
        _pathService = pathService;
    }

    public async Task<bool> DownloadViveToolAsync(System.IProgress<long>? progress = null)
    {
        try
        {
            var bundledPath = _pathService.GetBundledViveToolPath();
            if (File.Exists(bundledPath))
            {
                _pathService.CachedPath = bundledPath;
                return true;
            }

            var builtInPath = _pathService.GetBuiltInViveToolPath();
            if (File.Exists(builtInPath))
                return true;

            var builtInDir = Path.GetDirectoryName(builtInPath);
            if (!string.IsNullOrEmpty(builtInDir) && !Directory.Exists(builtInDir))
                Directory.CreateDirectory(builtInDir);

            // Download ZIP file to temporary location
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"ViVeTool_{Guid.NewGuid()}.zip");
            try
            {
                using var httpClient = LenovoLegionToolkit.Plugins.Shared.HttpClientManager.CreateClientWithTimeout(
                LenovoLegionToolkit.Plugins.Shared.Constants.DownloadTimeoutSeconds);

                // Get the response as a stream to track progress
                var response = await httpClient.GetAsync(DefaultViveToolDownloadUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                long downloadedBytes = 0;

                using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                    downloadedBytes += bytesRead;
                    progress?.Report(downloadedBytes);
                }

                // Extract all files from ZIP to the built-in directory
                using var archive = ZipFile.OpenRead(tempZipPath);

                // Verify ViVeTool.exe exists in the archive
                var exeEntry = archive.GetEntry(ViveToolPathService.ViveToolExeName);
                if (exeEntry == null)
                {
                    // Try case-insensitive search
                    exeEntry = archive.Entries.FirstOrDefault(e =>
                        e.Name.Equals(ViveToolPathService.ViveToolExeName, StringComparison.OrdinalIgnoreCase));
                }

                if (exeEntry == null)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"ViveTool: {ViveToolPathService.ViveToolExeName} not found in ZIP archive");
                    return false;
                }

                // Extract all entries to the built-in directory
                foreach (var entry in archive.Entries)
                {
                    // Skip directories
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    // SECURITY: Validate entry name to prevent path traversal in ZIP
                    if (entry.Name.Contains("..") || entry.Name.Contains('/') || entry.Name.Contains('\\'))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"SECURITY: Skipping suspicious entry name in ZIP: {entry.Name}");
                        continue;
                    }

                    var destinationPath = Path.Combine(builtInDir!, entry.Name);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: Downloaded and extracted built-in ViVeTool and dependencies to {builtInDir}");

                _pathService.CachedPath = builtInPath;
                return true;
            }
            finally
            {
                // Clean up temporary ZIP file
                try
                {
                    if (File.Exists(tempZipPath))
                        File.Delete(tempZipPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Failed to download built-in ViVeTool: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<List<FeatureFlagInfo>> ImportFeaturesFromFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: Import file not found: {filePath}");
                return new List<FeatureFlagInfo>();
            }

            var content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
            return ParseImportContent(content);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Error importing features from file: {ex.Message}", ex);
            return new List<FeatureFlagInfo>();
        }
    }

    public async Task<List<FeatureFlagInfo>> ImportFeaturesFromUrlAsync(string url)
    {
        try
        {
            using var httpClient = LenovoLegionToolkit.Plugins.Shared.HttpClientManager.CreateClientWithTimeout(30);
            var content = await httpClient.GetStringAsync(url).ConfigureAwait(false);
            return ParseImportContent(content);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Error importing features from URL: {ex.Message}", ex);
            return new List<FeatureFlagInfo>();
        }
    }

    private List<FeatureFlagInfo> ParseImportContent(string content)
    {
        var features = new List<FeatureFlagInfo>();

        if (string.IsNullOrWhiteSpace(content))
            return features;

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
                        features.Add(feature);
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
                            features.Add(feature);
                    }
                }
                else
                {
                    var feature = ParseJsonFeature(jsonDoc.RootElement);
                    if (feature != null)
                        features.Add(feature);
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
                id = idElement.GetInt32();
            else if (element.TryGetProperty("Id", out var idElement2))
                id = idElement2.GetInt32();
            else if (element.TryGetProperty("featureId", out var idElement3))
                id = idElement3.GetInt32();
            else if (element.TryGetProperty("FeatureId", out var idElement4))
                id = idElement4.GetInt32();

            if (id == 0)
                return null;

            if (element.TryGetProperty("name", out var nameElement))
                name = nameElement.GetString() ?? string.Empty;
            else if (element.TryGetProperty("Name", out var nameElement2))
                name = nameElement2.GetString() ?? string.Empty;

            if (element.TryGetProperty("description", out var descElement))
                description = descElement.GetString() ?? string.Empty;
            else if (element.TryGetProperty("Description", out var descElement2))
                description = descElement2.GetString() ?? string.Empty;

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
                continue;

            // Try CSV format: ID,Name,Description
            var parts = line.Split(',');
            if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), out var id))
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
            else
            {
                // Try simple format: just the ID
                if (int.TryParse(line.Trim(), out var simpleId))
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
        }

        return features;
    }
}
