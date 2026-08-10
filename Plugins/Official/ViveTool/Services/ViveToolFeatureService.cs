using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Plugins.Core;

namespace UniversalDeviceToolkit.Plugins.ViveTool.Services;

/// <summary>
/// Manages feature flag operations.
/// </summary>
public class ViveToolFeatureService : IDisposable
{
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    // Pre-compiled regexes for version parsing — avoids allocating new
    // Regex objects on every TryParseVersionLine call.
    private static readonly Regex VersionRegex_Comprehensive = new(
        @"^(?:ViVeTool|ViveTool)\s+v?(?<version>[0-9]+\.[0-9]+\.[0-9]+|[0-9]+\.[0-9]+)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VersionRegex_VersionKeyword = new(
        @"^(?:ViVeTool|ViveTool)\s+version[:\s]+(?<version>[0-9]+\.[0-9]+\.[0-9]+|[0-9]+\.[0-9]+)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VersionRegex_Prefix = new(
        @"^Version:\s*(?<version>[0-9]+\.[0-9]+\.[0-9]+|[0-9]+\.[0-9]+)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VersionRegex_VPrefix = new(
        @"^v(?<version>[0-9]+\.[0-9]+\.[0-9]+|[0-9]+\.[0-9]+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VersionRegex_BareVersion = new(
        @"^(?<version>[0-9]+\.[0-9]+\.[0-9]+|[0-9]+\.[0-9]+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex[] VersionRegexes =
    {
        VersionRegex_Comprehensive, VersionRegex_VersionKeyword,
        VersionRegex_Prefix, VersionRegex_VPrefix, VersionRegex_BareVersion
    };

    // Pre-compiled regexes for feature list parsing — avoids recompiling
    // the same patterns on every ParseFeatureList call.
    private static readonly Regex FeatureIdSplitRegex = new(
        @"\[(\d+)\]", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex Format2IdRegex = new(
        @"ID[:\s]+(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Format2NameRegex = new(
        @"Name[:\s]+([^,]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Format3Regex = new(
        @"^(\d+)[:\s]*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Format3ParenRegex = new(
        @"^(.+?)\s*\(([^)]+)\)\s*$", RegexOptions.Compiled);
    private static readonly Regex StateRegex = new(
        @"State\s*:\s*(\w+)\s*\(\d+\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
        PluginLog.Trace($"ViveTool: Clearing feature cache");

        lock (_cacheSync)
        {
            _cachedFeatures = null;
            _cachedFeaturesTimestamp = DateTime.MinValue;
        }
    }

    public async Task<bool> EnableFeatureAsync(int featureId)
    {
        // Validate feature ID (must be positive)
        if (featureId <= 0)
        {
            PluginLog.Trace($"ViveTool: Invalid feature ID {featureId}, must be positive");
            return false;
        }

        var viveToolPath = await _pathService.GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
        {
            PluginLog.Trace($"ViveTool: vivetool.exe not found");
            return false;
        }

        try
        {
            var result = await _processService.ExecuteCommandAsync(viveToolPath, $"/enable /id:{featureId}").ConfigureAwait(false);
            if (result.Success)
            {
                ClearFeatureCache();
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Error enabling feature {featureId}: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> DisableFeatureAsync(int featureId)
    {
        // Validate feature ID (must be positive)
        if (featureId <= 0)
        {
            PluginLog.Trace($"ViveTool: Invalid feature ID {featureId}, must be positive");
            return false;
        }

        var viveToolPath = await _pathService.GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
        {
            PluginLog.Trace($"ViveTool: vivetool.exe not found");
            return false;
        }

        try
        {
            var result = await _processService.ExecuteCommandAsync(viveToolPath, $"/disable /id:{featureId}").ConfigureAwait(false);
            if (result.Success)
            {
                ClearFeatureCache();
            }

            return result.Success;
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Error disabling feature {featureId}: {ex.Message}", ex);
            return false;
        }
    }

    public async Task<FeatureFlagStatus?> GetFeatureStatusAsync(int featureId)
    {
        // Validate feature ID
        if (featureId <= 0)
        {
            PluginLog.Trace($"ViveTool: Invalid feature ID {featureId}, must be positive");
            return null;
        }

        var viveToolPath = await _pathService.GetViveToolPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(viveToolPath))
        {
            return null;
        }

        try
        {
            var result = await _processService.ExecuteCommandAsync(viveToolPath, $"/query /id:{featureId}").ConfigureAwait(false);
            if (!result.Success)
            {
                return null;
            }

            // Parse output to determine status — use word-boundary regex to avoid
            // false-positives on substrings like "not enabled" matching "enabled".
            var output = result.Output?.ToLowerInvariant() ?? string.Empty;
            if (Regex.IsMatch(output, @"(?<!not\s)\benabled\b") || output.Contains("state: 1"))
            {
                return FeatureFlagStatus.Enabled;
            }

            if (Regex.IsMatch(output, @"(?<!not\s)\bdisabled\b") || output.Contains("state: 0"))
            {
                return FeatureFlagStatus.Disabled;
            }

            if (Regex.IsMatch(output, @"(?<!not\s)\bdefault\b") || output.Contains("state: 2"))
            {
                return FeatureFlagStatus.Default;
            }

            return FeatureFlagStatus.Unknown;
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Error querying feature {featureId}: {ex.Message}", ex);
            return null;
        }
    }

    public async Task<List<FeatureFlagInfo>> ListFeaturesAsync()
    {
        if (TryGetCachedFeatures(out var cachedFeatures))
        {
            return cachedFeatures;
        }

        await _featureLoadGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (TryGetCachedFeatures(out cachedFeatures))
            {
                return cachedFeatures;
            }

            var viveToolPath = await _pathService.GetViveToolPathAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(viveToolPath))
            {
                return [];
            }

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
            {
                return [];
            }

            UpdateCachedFeatures(features, DateTime.UtcNow);

            PluginLog.Trace($"ViveTool: Caching {features.Count} features for {_cacheDuration.TotalMinutes} minutes");

            return new List<FeatureFlagInfo>(features);
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Error listing features: {ex.Message}", ex);
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
        {
            return allFeatures;
        }

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
            PluginLog.Trace($"ViveTool: vivetool.exe not found, cannot get version");
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

                PluginLog.Trace($"ViveTool: Detected version: {version}");

                return version;
            }
        }
        catch (Exception ex)
        {
            PluginLog.Trace($"ViveTool: Error getting version: {ex.Message}", ex);
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
            PluginLog.Trace("ViveTool: Query did not return configured feature data");

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
            {
                return [];
            }

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
            {
                continue;
            }

            var separatorIndex = rawLine.LastIndexOf(',');
            if (separatorIndex <= 0 || separatorIndex >= rawLine.Length - 1)
            {
                continue;
            }

            var name = rawLine[..separatorIndex].Trim();
            var idText = rawLine[(separatorIndex + 1)..].Trim();
            if (!int.TryParse(idText, out var id) || id <= 0 || !seenIds.Add(id))
            {
                continue;
            }

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
        {
            return;
        }

        var configuredById = new Dictionary<int, FeatureFlagInfo>();
        foreach (var configuredFeature in configuredFeatures)
        {
            configuredById[configuredFeature.Id] = configuredFeature;
        }

        for (var i = 0; i < features.Count; i++)
        {
            if (!configuredById.TryGetValue(features[i].Id, out var configuredFeature))
            {
                continue;
            }

            features[i].Status = configuredFeature.Status;
            if (!string.IsNullOrWhiteSpace(configuredFeature.Description))
            {
                features[i].Description = configuredFeature.Description;
            }

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
        {
            return null;
        }

        foreach (var line in output.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var version = TryParseVersionLine(line);
            if (!string.IsNullOrWhiteSpace(version))
            {
                return version;
            }
        }

        return null;
    }

    private static string? TryParseVersionLine(string line)
    {
        foreach (var regex in VersionRegexes)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                return match.Groups["version"].Value;
            }
        }

        return null;
    }

    private List<FeatureFlagInfo> ParseFeatureList(string output)
    {
        var features = new List<FeatureFlagInfo>();

        if (string.IsNullOrWhiteSpace(output))
        {
            return features;
        }

        PluginLog.Trace($"ViveTool: Parsing feature list output (length: {output.Length} chars)");

        // Parse vivetool output formats
        // Check for v0.3.4+ format (starts with [ID])
        var featureSections = FeatureIdSplitRegex.Split(output);

        if (featureSections.Length > 1)
        {
            // Handle v0.3.4+ format
            features.AddRange(ParseViveTool34Format(featureSections));
        }
        else
        {
            // Handle older formats
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            PluginLog.Trace($"ViveTool: Found {lines.Length} lines to parse");

            features.AddRange(ParseLegacyFormats(lines));
        }

        PluginLog.Trace($"ViveTool: Parsed {features.Count} features from output");

        return features;
    }

    private IEnumerable<FeatureFlagInfo> ParseViveTool34Format(string[] featureSections)
    {
        // Regex.Split interleaves captures between each split segment.
        // index 0 = before first match, then alternates: [CaptureID][Segment][CaptureID][Segment]...
        // The body segment is always at i+1. Guard against malformed output that has a dangling
        // ID with no trailing segment (would otherwise throw IndexOutOfRangeException).
        for (int i = 1; i + 1 < featureSections.Length; i += 2)
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
            {
                continue;
            }

            // Skip header lines or help text
            if (line.Contains("Usage:", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("Options:", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryParseLegacyFeatureLine(line, out var feature))
            {
                yield return feature;
            }
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
        var idMatch = Format2IdRegex.Match(line);
        if (idMatch.Success && int.TryParse(idMatch.Groups[1].Value, out id))
        {
            var nameMatch = Format2NameRegex.Match(line);
            name = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : $"Feature {id}";

            status = ParseStatusFromLine(line);
            return true;
        }
        return false;
    }

    private bool TryParseFormat3(string line, ref int id, ref string name, ref FeatureFlagStatus status)
    {
        var colonMatch = Format3Regex.Match(line);
        if (colonMatch.Success && int.TryParse(colonMatch.Groups[1].Value, out id))
        {
            var rest = colonMatch.Groups[2].Value.Trim();
            if (!string.IsNullOrWhiteSpace(rest))
            {
                // Extract name and status from rest
                var parenMatch = Format3ParenRegex.Match(rest);
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
        var stateMatch = StateRegex.Match(section);
        if (stateMatch.Success)
        {
            string stateStr = stateMatch.Groups[1].Value.Trim();
            return ParseStatusFromString(stateStr);
        }
        return FeatureFlagStatus.Unknown;
    }

    private FeatureFlagStatus ParseStatusFromLine(string line)
    {
        if (Regex.IsMatch(line, @"(?<!\bnot\s)\benabled\b", RegexOptions.IgnoreCase))
        {
            return FeatureFlagStatus.Enabled;
        }

        if (Regex.IsMatch(line, @"(?<!\bnot\s)\bdisabled\b", RegexOptions.IgnoreCase))
        {
            return FeatureFlagStatus.Disabled;
        }

        if (Regex.IsMatch(line, @"(?<!\bnot\s)\bdefault\b", RegexOptions.IgnoreCase))
        {
            return FeatureFlagStatus.Default;
        }

        return FeatureFlagStatus.Unknown;
    }

    private FeatureFlagStatus ParseStatusFromString(string statusStr)
    {
        if (statusStr.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
        {
            return FeatureFlagStatus.Enabled;
        }

        if (statusStr.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
        {
            return FeatureFlagStatus.Disabled;
        }

        if (statusStr.Equals("Default", StringComparison.OrdinalIgnoreCase))
        {
            return FeatureFlagStatus.Default;
        }

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
                PluginLog.Trace($"ViveTool: Returning {_cachedFeatures.Count} features from cache");

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

    public void Dispose()
    {
        _featureLoadGate.Dispose();
        GC.SuppressFinalize(this);
    }
}
