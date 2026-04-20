using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.ViveTool.Services;

/// <summary>
/// Manages feature flag operations.
/// </summary>
public class ViveToolFeatureService
{
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    private readonly object _cacheSync = new();
    private readonly SemaphoreSlim _featureLoadGate = new(1, 1);
    private List<FeatureFlagInfo>? _cachedFeatures;
    private DateTime _cachedFeaturesTimestamp = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = DefaultCacheDuration;
    private readonly ViveToolPathService _pathService;
    private readonly ViveToolProcessService _processService;

    public ViveToolFeatureService(ViveToolPathService pathService, ViveToolProcessService processService)
    {
        _pathService = pathService;
        _processService = processService;
    }

    /// <summary>
    /// Clear the feature cache to force reload on next request.
    /// </summary>
    public void ClearFeatureCache()
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"ViveTool: Clearing feature cache");

        lock (_cacheSync)
        {
            _cachedFeatures = null;
            _cachedFeaturesTimestamp = DateTime.MinValue;
        }
    }

    public async Task<bool> EnableFeatureAsync(int featureId)
    {
        var viveToolPath = await _pathService.GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: vivetool.exe not found");
            return false;
        }

        try
        {
            var result = await _processService.ExecuteCommandAsync(viveToolPath, $"/enable /id:{featureId}").ConfigureAwait(false);
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
        var viveToolPath = await _pathService.GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: vivetool.exe not found");
            return false;
        }

        try
        {
            var result = await _processService.ExecuteCommandAsync(viveToolPath, $"/disable /id:{featureId}").ConfigureAwait(false);
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
        // Validate feature ID
        if (featureId <= 0)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Invalid feature ID {featureId}, must be positive");
            return null;
        }

        var viveToolPath = await _pathService.GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
            return null;

        try
        {
            var result = await _processService.ExecuteCommandAsync(viveToolPath, $"/query /id:{featureId}").ConfigureAwait(false);
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
        if (TryGetCachedFeatures(out var cachedFeatures))
            return cachedFeatures;

        await _featureLoadGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (TryGetCachedFeatures(out cachedFeatures))
                return cachedFeatures;

            var viveToolPath = await _pathService.GetViveToolPathAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(viveToolPath))
                return [];

            var configuredFeatures = await QueryConfiguredFeaturesAsync(viveToolPath).ConfigureAwait(false);
            var features = LoadFeatureDictionary(viveToolPath);
            if (features.Count == 0)
            {
                features = configuredFeatures ?? [];
            }
            else
            {
                ApplyConfiguredStatuses(features, configuredFeatures);
            }

            if (features.Count == 0)
                return [];

            UpdateCachedFeatures(features, DateTime.UtcNow);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Caching {features.Count} features for {_cacheDuration.TotalMinutes} minutes");

            return new List<FeatureFlagInfo>(features);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: Error listing features: {ex.Message}", ex);
            return [];
        }
        finally
        {
            _featureLoadGate.Release();
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

    public async Task<string?> GetViveToolVersionAsync()
    {
        var viveToolPath = await _pathService.GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"ViveTool: vivetool.exe not found, cannot get version");
            return null;
        }

        try
        {
            // Try /help command to get version info (most CLI tools show version in help)
            var result = await _processService.ExecuteCommandAsync(viveToolPath, "/help").ConfigureAwait(false);

            if (result.Success && !string.IsNullOrWhiteSpace(result.Output))
            {
                // Parse version from output
                var version = ParseVersionFromOutput(result.Output);

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

    private async Task<List<FeatureFlagInfo>?> QueryConfiguredFeaturesAsync(string viveToolPath)
    {
        // ViVeTool v0.3.4 exposes only the configured subset through /query.
        var result = await _processService.ExecuteCommandAsync(viveToolPath, "/query").ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
        {
            Log.Instance.Trace($"ViveTool: /query command result - Success: {result.Success}");
            Log.Instance.Trace($"ViveTool: /query command output: {result.Output ?? "(null)"}");
        }

        if (!result.Success || string.IsNullOrWhiteSpace(result.Output))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("ViveTool: Query did not return configured feature data");

            return null;
        }

        return ParseFeatureList(result.Output);
    }

    private static List<FeatureFlagInfo> LoadFeatureDictionary(string viveToolPath)
    {
        try
        {
            var dictionaryPath = Path.Combine(
                Path.GetDirectoryName(viveToolPath) ?? string.Empty,
                "FeatureDictionary.pfs");
            if (!File.Exists(dictionaryPath))
                return [];

            return ParseFeatureDictionaryLines(File.ReadLines(dictionaryPath));
        }
        catch
        {
            return [];
        }
    }

    private static List<FeatureFlagInfo> ParseFeatureDictionaryLines(IEnumerable<string> lines)
    {
        var features = new List<FeatureFlagInfo>();
        var seenIds = new HashSet<int>();

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
                continue;

            var separatorIndex = rawLine.LastIndexOf(',');
            if (separatorIndex <= 0 || separatorIndex >= rawLine.Length - 1)
                continue;

            var name = rawLine[..separatorIndex].Trim();
            var idText = rawLine[(separatorIndex + 1)..].Trim();
            if (!int.TryParse(idText, out var id) || id <= 0 || !seenIds.Add(id))
                continue;

            features.Add(new FeatureFlagInfo
            {
                Id = id,
                Name = string.IsNullOrWhiteSpace(name) ? $"Feature {id}" : name,
                Status = FeatureFlagStatus.Default,
                Description = string.Empty
            });
        }

        return features;
    }

    private static void ApplyConfiguredStatuses(List<FeatureFlagInfo> features, List<FeatureFlagInfo>? configuredFeatures)
    {
        if (configuredFeatures is null || configuredFeatures.Count == 0)
            return;

        var configuredById = configuredFeatures.ToDictionary(feature => feature.Id);
        for (var i = 0; i < features.Count; i++)
        {
            if (!configuredById.TryGetValue(features[i].Id, out var configuredFeature))
                continue;

            features[i].Status = configuredFeature.Status;
            if (!string.IsNullOrWhiteSpace(configuredFeature.Description))
                features[i].Description = configuredFeature.Description;
            if (!string.IsNullOrWhiteSpace(configuredFeature.Name) &&
                !configuredFeature.Name.Equals($"Feature {configuredFeature.Id}", StringComparison.OrdinalIgnoreCase))
            {
                features[i].Name = configuredFeature.Name;
            }
        }
    }

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

    private bool TryGetCachedFeatures(out List<FeatureFlagInfo> features)
    {
        var now = DateTime.UtcNow;
        lock (_cacheSync)
        {
            if (_cachedFeatures != null && (now - _cachedFeaturesTimestamp) < _cacheDuration)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"ViveTool: Returning {_cachedFeatures.Count} features from cache");

                features = new List<FeatureFlagInfo>(_cachedFeatures);
                return true;
            }
        }

        features = [];
        return false;
    }

    private void UpdateCachedFeatures(List<FeatureFlagInfo> features, DateTime timestamp)
    {
        lock (_cacheSync)
        {
            _cachedFeatures = new List<FeatureFlagInfo>(features);
            _cachedFeaturesTimestamp = timestamp;
        }
    }
}
