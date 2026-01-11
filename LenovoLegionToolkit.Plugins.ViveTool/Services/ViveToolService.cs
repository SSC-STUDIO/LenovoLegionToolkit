using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.ViveTool.Services;

/// <summary>
/// ViVeTool service implementation for managing Windows feature flags
/// </summary>
public class ViveToolService : IViveToolService
{
    private const string ViveToolExeName = "vivetool.exe";
    private string? _cachedViveToolPath;
    private readonly Settings.ViveToolSettings _settings;

    public ViveToolService()
    {
        _settings = new Settings.ViveToolSettings();
        _ = _settings.LoadAsync();
    }

    public async Task<bool> IsViveToolAvailableAsync()
    {
        var path = await GetViveToolPathAsync().ConfigureAwait(false);
        return !string.IsNullOrEmpty(path) && File.Exists(path);
    }

    public async Task<string?> GetViveToolPathAsync()
    {
        if (!string.IsNullOrEmpty(_cachedViveToolPath) && File.Exists(_cachedViveToolPath))
            return Task.FromResult<string?>(_cachedViveToolPath);

        // First check user-specified path from settings
        await _settings.LoadAsync().ConfigureAwait(false);
        var userSpecifiedPath = _settings.ViveToolPath;
        if (!string.IsNullOrEmpty(userSpecifiedPath) && File.Exists(userSpecifiedPath))
        {
            _cachedViveToolPath = userSpecifiedPath;
            return Task.FromResult<string?>(_cachedViveToolPath);
        }

        // Check in plugin directory first
        var pluginDirectory = Path.GetDirectoryName(typeof(ViveToolService).Assembly.Location);
        if (!string.IsNullOrEmpty(pluginDirectory))
        {
            var pluginPath = Path.Combine(pluginDirectory, ViveToolExeName);
            if (File.Exists(pluginPath))
            {
                _cachedViveToolPath = pluginPath;
                return Task.FromResult<string?>(_cachedViveToolPath);
            }
        }

        // Check in tools directory
        var toolsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools");
        var toolsPath = Path.Combine(toolsDirectory, ViveToolExeName);
        if (File.Exists(toolsPath))
        {
            _cachedViveToolPath = toolsPath;
            return Task.FromResult<string?>(_cachedViveToolPath);
        }

        // Check in PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathEnv))
        {
            var paths = pathEnv.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, ViveToolExeName);
                if (File.Exists(fullPath))
                {
                    _cachedViveToolPath = fullPath;
                    return Task.FromResult<string?>(_cachedViveToolPath);
                }
            }
        }

        // Check current directory
        var currentPath = Path.Combine(Directory.GetCurrentDirectory(), ViveToolExeName);
        if (File.Exists(currentPath))
        {
            _cachedViveToolPath = currentPath;
            return Task.FromResult<string?>(_cachedViveToolPath);
        }

        return Task.FromResult<string?>(null);
    }

    public async Task<bool> EnableFeatureAsync(int featureId)
    {
        var viveToolPath = await GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: vivetool.exe not found");
            return false;
        }

        try
        {
            var result = await ExecuteViveToolCommandAsync(viveToolPath, $"/enable /id:{featureId}").ConfigureAwait(false);
            return result.Success;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Error enabling feature {featureId}: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> DisableFeatureAsync(int featureId)
    {
        var viveToolPath = await GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: vivetool.exe not found");
            return false;
        }

        try
        {
            var result = await ExecuteViveToolCommandAsync(viveToolPath, $"/disable /id:{featureId}").ConfigureAwait(false);
            return result.Success;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Error disabling feature {featureId}: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<FeatureFlagStatus?> GetFeatureStatusAsync(int featureId)
    {
        var viveToolPath = await GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
            return null;

        try
        {
            var result = await ExecuteViveToolCommandAsync(viveToolPath, $"/query /id:{featureId}").ConfigureAwait(false);
            if (!result.Success)
                return null;

            // Parse output to determine status
            var output = result.Output?.ToLowerInvariant() ?? string.Empty;
            if (output.Contains("enabled") || output.Contains("state: 1"))
                return FeatureFlagStatus.Enabled;
            if (output.Contains("disabled") || output.Contains("state: 0"))
                return FeatureFlagStatus.Disabled;
            if (output.Contains("default") || output.Contains("state: 2"))
                return FeatureFlagStatus.Default;

            return FeatureFlagStatus.Unknown;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Error querying feature {featureId}: {ex.Message}", ex);
            return null;
        }
    }

    public async Task<List<FeatureFlagInfo>> ListFeaturesAsync()
    {
        var viveToolPath = await GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
            return new List<FeatureFlagInfo>();

        try
        {
            var result = await ExecuteViveToolCommandAsync(viveToolPath, "/list").ConfigureAwait(false);
            if (!result.Success)
                return new List<FeatureFlagInfo>();

            return ParseFeatureList(result.Output ?? string.Empty);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Error listing features: {ex.Message}", ex);
            return new List<FeatureFlagInfo>();
        }
    }

    public async Task<List<FeatureFlagInfo>> SearchFeaturesAsync(string keyword)
    {
        var allFeatures = await ListFeaturesAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(keyword))
            return allFeatures;

        var lowerKeyword = keyword.ToLowerInvariant();
        return allFeatures.Where(f =>
            f.Id.ToString().Contains(lowerKeyword) ||
            f.Name.ToLowerInvariant().Contains(lowerKeyword) ||
            f.Description.ToLowerInvariant().Contains(lowerKeyword)
        ).ToList();
    }

    private async Task<(bool Success, string? Output, string? Error)> ExecuteViveToolCommandAsync(string viveToolPath, string arguments)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = viveToolPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Note: Admin privileges are typically required for vivetool.exe
            // The user should run the application as administrator
            // We don't use Verb = "runas" here as it would show a UAC prompt for each command

            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            var error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

            var success = process.ExitCode == 0;
            return (success, output, error);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Error executing command: {ex.Message}", ex);
            return (false, null, ex.Message);
        }
    }

    private List<FeatureFlagInfo> ParseFeatureList(string output)
    {
        var features = new List<FeatureFlagInfo>();
        
        if (string.IsNullOrWhiteSpace(output))
            return features;

        // Parse vivetool output format
        // Example: "ID: 12345, Name: FeatureName, State: Enabled"
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Try to extract feature ID
            var idMatch = Regex.Match(line, @"ID[:\s]+(\d+)", RegexOptions.IgnoreCase);
            if (!idMatch.Success)
                continue;

            if (!int.TryParse(idMatch.Groups[1].Value, out var id))
                continue;

            // Extract name
            var nameMatch = Regex.Match(line, @"Name[:\s]+([^,]+)", RegexOptions.IgnoreCase);
            var name = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : $"Feature {id}";

            // Extract status
            var status = FeatureFlagStatus.Unknown;
            if (line.Contains("Enabled", StringComparison.OrdinalIgnoreCase))
                status = FeatureFlagStatus.Enabled;
            else if (line.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
                status = FeatureFlagStatus.Disabled;
            else if (line.Contains("Default", StringComparison.OrdinalIgnoreCase))
                status = FeatureFlagStatus.Default;

            features.Add(new FeatureFlagInfo
            {
                Id = id,
                Name = name,
                Status = status,
                Description = string.Empty
            });
        }

        return features;
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
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
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

    public async Task<bool> SetViveToolPathAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _settings.ViveToolPath = null;
                _cachedViveToolPath = null;
                return true;
            }

            if (!File.Exists(filePath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: Specified file does not exist: {filePath}");
                return false;
            }

            // Verify it's actually vivetool.exe
            var fileName = Path.GetFileName(filePath);
            if (!fileName.Equals(ViveToolExeName, StringComparison.OrdinalIgnoreCase))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: Specified file is not vivetool.exe: {filePath}");
                return false;
            }

            _settings.ViveToolPath = filePath;
            _cachedViveToolPath = filePath;
            await _settings.SaveAsync().ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Path set to: {filePath}");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Error setting path: {ex.Message}", ex);
            return false;
        }
    }
}
