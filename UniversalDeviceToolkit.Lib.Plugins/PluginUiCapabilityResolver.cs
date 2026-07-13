using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

public readonly struct PluginUiCapabilities
{
    public bool SupportsSettingsPage { get; init; }
    public bool SupportsFeaturePage { get; init; }
    public bool SupportsOptimizationCategory { get; init; }

    public bool HasAny => SupportsSettingsPage || SupportsFeaturePage || SupportsOptimizationCategory;

    public PluginUiCapabilities Merge(PluginUiCapabilities other) =>
        new()
        {
            SupportsSettingsPage = SupportsSettingsPage || other.SupportsSettingsPage,
            SupportsFeaturePage = SupportsFeaturePage || other.SupportsFeaturePage,
            SupportsOptimizationCategory = SupportsOptimizationCategory || other.SupportsOptimizationCategory,
        };
}

public static class PluginUiCapabilityResolver
{
    private static readonly string[] ManifestFileNames = ["plugin.manifest.json", "plugin.json", "Plugin.json"];

    // Disk/JSON capability resolution is hot on the plugin list path — cache aggressively.
    private static readonly ConcurrentDictionary<string, PluginUiCapabilities> CapabilityCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, PluginManifest?> ManifestCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly object IdIndexGate = new();
    private static Dictionary<string, string>? _manifestIdToPath;

    public static void InvalidateCache(string? pluginId = null)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
        {
            CapabilityCache.Clear();
            ManifestCache.Clear();
            lock (IdIndexGate)
                _manifestIdToPath = null;
            return;
        }

        CapabilityCache.TryRemove(pluginId, out _);
        ManifestCache.TryRemove(pluginId, out _);
        lock (IdIndexGate)
            _manifestIdToPath = null;
    }

    public static PluginUiCapabilities ResolveFromInstalledManifest(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return default;

        if (CapabilityCache.TryGetValue(pluginId, out var cached))
            return cached;

        try
        {
            foreach (var pluginDirectory in GetInstalledPluginDirectories(pluginId))
            {
                foreach (var manifestFileName in ManifestFileNames)
                {
                    var manifestPath = Path.Combine(pluginDirectory, manifestFileName);
                    if (!File.Exists(manifestPath))
                        continue;

                    var caps = ReadCapabilitiesFromJson(manifestPath);
                    CapabilityCache[pluginId] = caps;
                    return caps;
                }
            }

            if (FindInstalledManifestPathByManifestId(pluginId) is { } matchingManifestPath)
            {
                var caps = ReadCapabilitiesFromJson(matchingManifestPath);
                CapabilityCache[pluginId] = caps;
                return caps;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read UI capabilities for {pluginId}: {ex.Message}", ex);
        }

        CapabilityCache[pluginId] = default;
        return default;
    }

    public static PluginUiCapabilities ResolveFromManifest(PluginManifest? manifest)
    {
        var contributes = manifest?.Contributes;
        if (contributes is null)
            return default;

        return new PluginUiCapabilities
        {
            SupportsSettingsPage = HasContribution(contributes.SettingsPage),
            SupportsFeaturePage = HasContribution(contributes.FeaturePage),
            SupportsOptimizationCategory = SupportsOptimizationActions(manifest),
        };
    }

    public static bool SupportsOptimizationActions(PluginManifest? manifest) =>
        manifest?.Contributes?.OptimizationActions?.Any(action =>
            !string.IsNullOrWhiteSpace(GetOptimizationActionId(action)) &&
            !string.IsNullOrWhiteSpace(action.Title)) == true;

    public static string GetOptimizationActionId(PluginManifestOptimizationContribution? action)
    {
        if (action is null)
            return string.Empty;

        return FirstNonEmpty(action.Id, action.Key);
    }

    private static PluginUiCapabilities ReadCapabilitiesFromJson(string manifestPath)
    {
        using var stream = File.OpenRead(manifestPath);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        var contributes = ReadObject(root, "contributes");

        var supportsSettings =
            HasContribution(contributes, "settingsPage") ||
            ReadBool(root, "hasSettingsPage", "hasSettings", "settingsPage", "supportsSettingsPage");

        var supportsFeature =
            HasContribution(contributes, "featurePage") ||
            ReadBool(root, "hasFeaturePage", "featurePage", "supportsFeaturePage", "hasPluginPage");

        return new PluginUiCapabilities
        {
            SupportsSettingsPage = supportsSettings,
            SupportsFeaturePage = supportsFeature,
            SupportsOptimizationCategory = HasOptimizationActions(contributes),
        };
    }

    public static PluginManifest? ReadInstalledManifest(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return null;

        if (ManifestCache.TryGetValue(pluginId, out var cached))
            return cached;

        try
        {
            foreach (var pluginDirectory in GetInstalledPluginDirectories(pluginId))
            {
                foreach (var manifestFileName in ManifestFileNames)
                {
                    var manifestPath = Path.Combine(pluginDirectory, manifestFileName);
                    if (!File.Exists(manifestPath))
                        continue;

                    var manifest = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestPath));
                    ManifestCache[pluginId] = manifest;
                    return manifest;
                }
            }

            var byId = FindInstalledManifestByManifestId(pluginId);
            ManifestCache[pluginId] = byId;
            return byId;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read installed manifest for {pluginId}: {ex.Message}", ex);
        }

        ManifestCache[pluginId] = null;
        return null;
    }

    private static PluginManifest? FindInstalledManifestByManifestId(string pluginId)
    {
        var manifestPath = FindInstalledManifestPathByManifestId(pluginId);
        if (string.IsNullOrWhiteSpace(manifestPath))
            return null;

        try
        {
            return JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestPath));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read installed plugin manifest {manifestPath}: {ex.Message}", ex);
        }

        return null;
    }

    private static string? FindInstalledManifestPathByManifestId(string pluginId)
    {
        // Build the id→path index once per process (or until InvalidateCache). Scanning
        // every plugin.json on every list row was freezing the plugin page on UI thread.
        var index = EnsureManifestIdIndex();
        return index.TryGetValue(pluginId, out var path) ? path : null;
    }

    private static Dictionary<string, string> EnsureManifestIdIndex()
    {
        lock (IdIndexGate)
        {
            if (_manifestIdToPath is not null)
                return _manifestIdToPath;

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var manifestPath in EnumerateInstalledManifestPaths())
            {
                try
                {
                    var manifestId = ReadManifestIdFromJson(manifestPath);
                    if (!string.IsNullOrWhiteSpace(manifestId) && !map.ContainsKey(manifestId))
                        map[manifestId] = manifestPath;
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to inspect installed plugin manifest {manifestPath}: {ex.Message}", ex);
                }
            }

            _manifestIdToPath = map;
            return map;
        }
    }

    private static string? ReadManifestIdFromJson(string manifestPath)
    {
        using var stream = File.OpenRead(manifestPath);
        using var document = JsonDocument.Parse(stream);

        return ReadNonEmptyString(document.RootElement, "id");
    }

    private static bool HasContribution(PluginManifestPageContribution? contribution) =>
        contribution is not null &&
        (!string.IsNullOrWhiteSpace(contribution.Class) || !string.IsNullOrWhiteSpace(contribution.Title));

    private static bool HasContribution(JsonElement? root, string propertyName)
    {
        if (root is null)
            return false;

        if (!TryGetProperty(root.Value, propertyName, out var property))
            return false;

        return property.ValueKind switch
        {
            JsonValueKind.Object => property.EnumerateObject().Any(item =>
                item.Value.ValueKind != JsonValueKind.Null &&
                item.Value.ValueKind != JsonValueKind.Undefined &&
                (item.Value.ValueKind != JsonValueKind.String || !string.IsNullOrWhiteSpace(item.Value.GetString()))),
            JsonValueKind.True => true,
            JsonValueKind.String => !string.IsNullOrWhiteSpace(property.GetString()),
            _ => false,
        };
    }

    private static bool HasOptimizationActions(JsonElement? contributes)
    {
        if (contributes is null || !TryGetProperty(contributes.Value, "optimizationActions", out var actions))
            return false;

        return actions.ValueKind == JsonValueKind.Array &&
               actions.EnumerateArray().Any(action =>
                   action.ValueKind == JsonValueKind.Object &&
                   ReadNonEmptyString(action, "title") is not null &&
                   (ReadNonEmptyString(action, "id") is not null || ReadNonEmptyString(action, "key") is not null));
    }

    private static string? ReadNonEmptyString(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            return null;

        var value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static JsonElement? ReadObject(JsonElement root, string propertyName) =>
        TryGetProperty(root, propertyName, out var property) && property.ValueKind == JsonValueKind.Object
            ? property
            : null;

    private static bool ReadBool(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                return property.Value.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.String => bool.TryParse(property.Value.GetString(), out var parsed) && parsed,
                    JsonValueKind.Number => property.Value.TryGetInt32(out var number) && number != 0,
                    _ => false,
                };
            }
        }

        return false;
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string[] GetInstalledPluginDirectories(string pluginId)
    {
        var pluginsDirectory = PluginPaths.GetPluginsDirectory();
        var assemblyDirectoryName = $"LenovoLegionToolkit.Plugins.{pluginId}";
        var compactAssemblyDirectoryName = $"LenovoLegionToolkit.Plugins.{pluginId.Replace("-", string.Empty)}";
        var pascalAssemblyDirectoryName = $"LenovoLegionToolkit.Plugins.{ToPascalCasePluginId(pluginId)}";
        var directories = new[]
        {
            Path.Combine(pluginsDirectory, "local", pluginId),
            Path.Combine(pluginsDirectory, "local", assemblyDirectoryName),
            Path.Combine(pluginsDirectory, "local", compactAssemblyDirectoryName),
            Path.Combine(pluginsDirectory, "local", pascalAssemblyDirectoryName),
            PluginPaths.GetPluginDirectory(pluginId),
            Path.Combine(pluginsDirectory, assemblyDirectoryName),
            Path.Combine(pluginsDirectory, compactAssemblyDirectoryName),
            Path.Combine(pluginsDirectory, pascalAssemblyDirectoryName)
        };

        return directories
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ToPascalCasePluginId(string pluginId)
    {
        var parts = pluginId.Split(['-', '_', '.', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Concat(parts.Select(static part =>
            part.Length == 0
                ? string.Empty
                : char.ToUpperInvariant(part[0]) + (part.Length > 1 ? part[1..] : string.Empty)));
    }

    private static IEnumerable<string> EnumerateInstalledManifestPaths()
    {
        var pluginsDirectory = PluginPaths.GetPluginsDirectory();
        if (!Directory.Exists(pluginsDirectory))
            yield break;

        var localDirectory = Path.Combine(pluginsDirectory, "local");
        if (Directory.Exists(localDirectory))
        {
            foreach (var localPluginDirectory in Directory.EnumerateDirectories(localDirectory))
            {
                foreach (var manifestFileName in ManifestFileNames)
                {
                    var manifestPath = Path.Combine(localPluginDirectory, manifestFileName);
                    if (File.Exists(manifestPath))
                        yield return manifestPath;
                }
            }
        }

        foreach (var pluginDirectory in EnumerateInstalledPluginDirectories(pluginsDirectory))
        {
            foreach (var manifestFileName in ManifestFileNames)
            {
                var manifestPath = Path.Combine(pluginDirectory, manifestFileName);
                if (File.Exists(manifestPath))
                    yield return manifestPath;
            }
        }
    }

    private static IEnumerable<string> EnumerateInstalledPluginDirectories(string pluginsDirectory)
    {
        foreach (var directory in Directory.EnumerateDirectories(pluginsDirectory))
        {
            if (string.Equals(Path.GetFileName(directory), "local", StringComparison.OrdinalIgnoreCase))
                continue;

            yield return directory;
        }
    }
}
