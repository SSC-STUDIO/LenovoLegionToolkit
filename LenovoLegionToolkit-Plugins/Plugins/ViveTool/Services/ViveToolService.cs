using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
    public const string ViveToolExeName = "ViVeTool.exe";
    // Official ViVeTool release asset (ZIP file containing ViVeTool.exe)
    private const string DefaultViveToolDownloadUrl = "https://github.com/thebookisclosed/ViVe/releases/latest/download/ViVeTool-v0.3.4-IntelAmd.zip";
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);
    
    private string? _cachedViveToolPath;
    private string? _cachedViveToolVersion;
    private List<FeatureFlagInfo>? _cachedFeatures;
    private DateTime _cachedFeaturesTimestamp = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = DefaultCacheDuration;
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
            return _cachedViveToolPath;

        // First check user-specified path from settings
        await _settings.LoadAsync().ConfigureAwait(false);
        var userSpecifiedPath = _settings.ViveToolPath;
        if (!string.IsNullOrEmpty(userSpecifiedPath) && File.Exists(userSpecifiedPath))
        {
            _cachedViveToolPath = userSpecifiedPath;
            return _cachedViveToolPath;
        }

        // Try built-in (download to AppData if missing)
        var builtInPath = GetBuiltInViveToolPath();
        var builtInAvailable = await EnsureBuiltInViveToolAsync().ConfigureAwait(false);
        if (builtInAvailable && File.Exists(builtInPath))
        {
            _cachedViveToolPath = builtInPath;
            return _cachedViveToolPath;
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
                    return _cachedViveToolPath;
                }
            }
        }

        // Check current directory
        var currentPath = Path.Combine(Directory.GetCurrentDirectory(), ViveToolExeName);
        if (File.Exists(currentPath))
        {
            _cachedViveToolPath = currentPath;
            return _cachedViveToolPath;
        }

        return null;
    }

    private string GetBuiltInViveToolPath()
    {
        return Path.Combine(Folders.AppData, "ViveTool", ViveToolExeName);
    }

    private async Task<bool> EnsureBuiltInViveToolAsync()
    {
        try
        {
            var builtInPath = GetBuiltInViveToolPath();
            if (File.Exists(builtInPath))
                return true;

            var builtInDir = Path.GetDirectoryName(builtInPath);
            if (!string.IsNullOrEmpty(builtInDir) && !Directory.Exists(builtInDir))
                Directory.CreateDirectory(builtInDir);

            // Download ZIP file to temporary location
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"ViVeTool_{Guid.NewGuid()}.zip");
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60);
                var zipBytes = await httpClient.GetByteArrayAsync(DefaultViveToolDownloadUrl).ConfigureAwait(false);
                await File.WriteAllBytesAsync(tempZipPath, zipBytes).ConfigureAwait(false);

                // Extract all files from ZIP to the built-in directory
                // ViVeTool.exe needs its dependencies (DLLs) in the same directory
                using var archive = ZipFile.OpenRead(tempZipPath);
                
                // Verify ViVeTool.exe exists in the archive
                var exeEntry = archive.GetEntry(ViveToolExeName);
                if (exeEntry == null)
                {
                    // Try case-insensitive search
                    exeEntry = archive.Entries.FirstOrDefault(e => 
                        e.Name.Equals(ViveToolExeName, StringComparison.OrdinalIgnoreCase));
                }

                if (exeEntry == null)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"ViveTool: {ViveToolExeName} not found in ZIP archive");
                    return false;
                }

                // Extract all entries to the built-in directory
                foreach (var entry in archive.Entries)
                {
                    // Skip directories
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var destinationPath = Path.Combine(builtInDir!, entry.Name);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: Downloaded and extracted built-in ViVeTool and dependencies to {builtInDir}");

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
            if (result.Success)
                ClearFeatureCache();
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
            if (result.Success)
                ClearFeatureCache();
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
        // Check cache first
        var now = DateTime.Now;
        if (_cachedFeatures != null && (now - _cachedFeaturesTimestamp) < _cacheDuration)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Returning {_cachedFeatures.Count} features from cache");
            return new List<FeatureFlagInfo>(_cachedFeatures);
        }

        var viveToolPath = await GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
            return new List<FeatureFlagInfo>();

        try
        {
            // For ViVeTool v0.3.4, we need to use specific commands to get features
            // The /list command is deprecated, and /query without parameters only shows modified features
            
            // Try /query command (only shows modified features in v0.3.4)
            var result = await ExecuteViveToolCommandAsync(viveToolPath, "/query").ConfigureAwait(false);
            
            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"ViveTool: /query command result - Success: {result.Success}");
                Log.Instance.Trace($"ViveTool: /query command output: {result.Output ?? "(null)"}");
            }

            List<FeatureFlagInfo> features;
            if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: No output from query command, returning empty list");
                features = new List<FeatureFlagInfo>();
            }
            else
            {
                features = ParseFeatureList(result.Output);
            }

            // Update cache
            _cachedFeatures = features;
            _cachedFeaturesTimestamp = now;
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Caching {features.Count} features for {_cacheDuration.TotalMinutes} minutes");

            return new List<FeatureFlagInfo>(_cachedFeatures);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Error listing features: {ex.Message}", ex);
            return new List<FeatureFlagInfo>();
        }
    }
    
    /// <summary>
    /// Clear the feature cache to force reload on next request
    /// </summary>
    public void ClearFeatureCache()
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Clearing feature cache");
        
        _cachedFeatures = null;
        _cachedFeaturesTimestamp = DateTime.MinValue;
    }
    
    /// <summary>
    /// Get the ViVeTool version
    /// </summary>
    public async Task<string?> GetViveToolVersionAsync()
    {
        // Return cached version if available
        if (!string.IsNullOrEmpty(_cachedViveToolVersion))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Returning cached version: {_cachedViveToolVersion}");
            return _cachedViveToolVersion;
        }
        
        var viveToolPath = await GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: vivetool.exe not found, cannot get version");
            return null;
        }
        
        try
        {
            // Try /help command to get version info (most CLI tools show version in help)
            var result = await ExecuteViveToolCommandAsync(viveToolPath, "/help").ConfigureAwait(false);
            
            if (result.Success && !string.IsNullOrWhiteSpace(result.Output))
            {
                // Parse version from output
                var version = ParseVersionFromOutput(result.Output);
                
                // Cache the version
                _cachedViveToolVersion = version;
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: Detected version: {version}");
                
                return version;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Error getting version: {ex.Message}", ex);
        }
        
        return null;
    }
    
    /// <summary>
    /// Parse version from ViVeTool output
    /// </summary>
    private string? ParseVersionFromOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;
        
        // Try to find version patterns like "v0.3.4", "0.3.4", "Version: 0.3.4", etc.
        var versionRegexes = new[]
        {
            @"v([0-9]+\.[0-9]+\.[0-9]+)",  // v0.3.4
            @"Version: ([0-9]+\.[0-9]+\.[0-9]+)",  // Version: 0.3.4
            @"([0-9]+\.[0-9]+\.[0-9]+)",  // 0.3.4
            @"v([0-9]+\.[0-9]+)",  // v0.3
            @"Version: ([0-9]+\.[0-9]+)",  // Version: 0.3
            @"([0-9]+\.[0-9]+)"  // 0.3
        };
        
        foreach (var regex in versionRegexes)
        {
            var match = Regex.Match(output, regex, RegexOptions.IgnoreCase);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }
        
        return null;
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
            // Set working directory to the directory containing ViVeTool.exe
            // This ensures that DLL dependencies can be found
            var workingDirectory = Path.GetDirectoryName(viveToolPath);
            
            var processStartInfo = new ProcessStartInfo
            {
                FileName = viveToolPath,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
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

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Parsing feature list output (length: {output.Length} chars)");

        // Parse vivetool output formats
        // Check for v0.3.4+ format (starts with [ID])
        var featureSections = Regex.Split(output, @"\[(\d+)\]", RegexOptions.Multiline);
        
        if (featureSections.Length > 1)
        {
            // Handle v0.3.4+ format
            features.AddRange(ParseViveTool34Format(featureSections));
        }
        else
        {
            // Handle older formats
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Found {lines.Length} lines to parse");

            features.AddRange(ParseLegacyFormats(lines));
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Parsed {features.Count} features from output");

        return features;
    }

    private IEnumerable<FeatureFlagInfo> ParseViveTool34Format(string[] featureSections)
    {
        for (int i = 1; i < featureSections.Length; i += 2)
        {
            if (int.TryParse(featureSections[i], out int id))
            {
                string section = featureSections[i + 1];
                string name = $"Feature {id}";
                FeatureFlagStatus status = ParseStateFromSection(section);
                
                yield return new FeatureFlagInfo
                {
                    Id = id,
                    Name = name,
                    Status = status,
                    Description = string.Empty
                };
            }
        }
    }

    private IEnumerable<FeatureFlagInfo> ParseLegacyFormats(string[] lines)
    {
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Skip header lines or help text
            if (line.Contains("Usage:", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Options:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("-", StringComparison.OrdinalIgnoreCase))
                continue;

            if (TryParseLegacyFeatureLine(line, out var feature))
                yield return feature;
        }
    }

    private bool TryParseLegacyFeatureLine(string line, out FeatureFlagInfo feature)
    {
        int id = 0;
        string name = string.Empty;
        FeatureFlagStatus status = FeatureFlagStatus.Unknown;

        // Try Format 2: "ID: 12345, Name: FeatureName, State: Enabled"
        if (TryParseFormat2(line, ref id, ref name, ref status))
        {
            feature = CreateFeatureFlagInfo(id, name, status);
            return true;
        }

        // Try Format 3: "12345: FeatureName (Enabled)" or just "12345"
        if (TryParseFormat3(line, ref id, ref name, ref status))
        {
            feature = CreateFeatureFlagInfo(id, name, status);
            return true;
        }

        // Try Format 4: Just a number
        if (TryParseFormat4(line, ref id, ref name))
        {
            feature = CreateFeatureFlagInfo(id, name, status);
            return true;
        }

        feature = CreateFeatureFlagInfo(0, string.Empty, FeatureFlagStatus.Unknown);
        return false;
    }

    private bool TryParseFormat2(string line, ref int id, ref string name, ref FeatureFlagStatus status)
    {
        var idMatch = Regex.Match(line, @"ID[:\s]+(\d+)", RegexOptions.IgnoreCase);
        if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out id))
        {
            var nameMatch = Regex.Match(line, @"Name[:\s]+([^,]+)", RegexOptions.IgnoreCase);
            name = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : $"Feature {id}";

            status = ParseStatusFromLine(line);
            return true;
        }
        return false;
    }

    private bool TryParseFormat3(string line, ref int id, ref string name, ref FeatureFlagStatus status)
    {
        var colonMatch = Regex.Match(line, @"^(\d+)[:\s]*(.*)$", RegexOptions.IgnoreCase);
        if (colonMatch.Success && int.TryParse(colonMatch.Groups[1].Value, out id))
        {
            var rest = colonMatch.Groups[2].Value.Trim();
            if (!string.IsNullOrWhiteSpace(rest))
            {
                // Extract name and status from rest
                var parenMatch = Regex.Match(rest, @"^(.+?)\s*\(([^)]+)\)\s*$");
                if (parenMatch.Success)
                {
                    name = parenMatch.Groups[1].Value.Trim();
                    var statusStr = parenMatch.Groups[2].Value.Trim();
                    status = ParseStatusFromString(statusStr);
                }
                else
                {
                    name = rest;
                }
            }
            return true;
        }
        return false;
    }

    private bool TryParseFormat4(string line, ref int id, ref string name)
    {
        if (int.TryParse(line.Trim(), out id))
        {
            name = $"Feature {id}";
            return true;
        }
        return false;
    }

    private FeatureFlagStatus ParseStateFromSection(string section)
    {
        var stateMatch = Regex.Match(section, @"State\s*:\s*(\w+)\s*\(\d+\)", RegexOptions.IgnoreCase);
        if (stateMatch.Success)
        {
            string stateStr = stateMatch.Groups[1].Value.Trim();
            return ParseStatusFromString(stateStr);
        }
        return FeatureFlagStatus.Unknown;
    }

    private FeatureFlagStatus ParseStatusFromLine(string line)
    {
        if (line.Contains("Enabled", StringComparison.OrdinalIgnoreCase))
            return FeatureFlagStatus.Enabled;
        if (line.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
            return FeatureFlagStatus.Disabled;
        if (line.Contains("Default", StringComparison.OrdinalIgnoreCase))
            return FeatureFlagStatus.Default;
        return FeatureFlagStatus.Unknown;
    }

    private FeatureFlagStatus ParseStatusFromString(string statusStr)
    {
        if (statusStr.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
            return FeatureFlagStatus.Enabled;
        if (statusStr.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
            return FeatureFlagStatus.Disabled;
        if (statusStr.Equals("Default", StringComparison.OrdinalIgnoreCase))
            return FeatureFlagStatus.Default;
        return FeatureFlagStatus.Unknown;
    }

    private FeatureFlagInfo CreateFeatureFlagInfo(int id, string name, FeatureFlagStatus status)
    {
        return new FeatureFlagInfo
        {
            Id = id,
            Name = string.IsNullOrEmpty(name) ? $"Feature {id}" : name,
            Status = status,
            Description = string.Empty
        };
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

    public async Task<bool> DownloadViveToolAsync(System.IProgress<long>? progress = null)
    {
        try
        {
            var builtInPath = GetBuiltInViveToolPath();
            if (File.Exists(builtInPath))
                return true;

            var builtInDir = Path.GetDirectoryName(builtInPath);
            if (!string.IsNullOrEmpty(builtInDir) && !Directory.Exists(builtInDir))
                Directory.CreateDirectory(builtInDir);

            // Download ZIP file to temporary location
            var tempZipPath = Path.Combine(Path.GetTempPath(), $"ViVeTool_{Guid.NewGuid()}.zip");
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(60);

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
                var exeEntry = archive.GetEntry(ViveToolExeName);
                if (exeEntry == null)
                {
                    // Try case-insensitive search
                    exeEntry = archive.Entries.FirstOrDefault(e => 
                        e.Name.Equals(ViveToolExeName, StringComparison.OrdinalIgnoreCase));
                }

                if (exeEntry == null)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"ViveTool: {ViveToolExeName} not found in ZIP archive");
                    return false;
                }

                // Extract all entries to the built-in directory
                foreach (var entry in archive.Entries)
                {
                    // Skip directories
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    var destinationPath = Path.Combine(builtInDir!, entry.Name);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: Downloaded and extracted built-in ViVeTool and dependencies to {builtInDir}");

                _cachedViveToolPath = builtInPath;
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
}
