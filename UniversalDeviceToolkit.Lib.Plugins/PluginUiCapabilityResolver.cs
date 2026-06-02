using System;
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

    public static PluginUiCapabilities ResolveFromInstalledManifest(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return default;

        try
        {
            foreach (var pluginDirectory in GetInstalledPluginDirectories(pluginId))
            {
                foreach (var manifestFileName in ManifestFileNames)
                {
                    var manifestPath = Path.Combine(pluginDirectory, manifestFileName);
                    if (!File.Exists(manifestPath))
                        continue;

                    return ReadCapabilitiesFromJson(manifestPath);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read UI capabilities for {pluginId}: {ex.Message}", ex);
        }

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
            !string.IsNullOrWhiteSpace(GetOptimizationActionId(action))) == true;

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

        var supportsOptimization =
            HasOptimizationContribution(contributes);

        return new PluginUiCapabilities
        {
            SupportsSettingsPage = supportsSettings,
            SupportsFeaturePage = supportsFeature,
            SupportsOptimizationCategory = supportsOptimization,
        };
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

    private static bool HasOptimizationContribution(JsonElement? root)
    {
        if (root is null)
            return false;

        if (!TryGetProperty(root.Value, "optimizationActions", out var property))
            return false;

        return property.ValueKind switch
        {
            JsonValueKind.Array => property.EnumerateArray().Any(HasOptimizationContributionAction),
            JsonValueKind.Object => HasOptimizationContributionAction(property),
            _ => false,
        };
    }

    private static bool HasOptimizationContributionAction(JsonElement action)
    {
        return action.ValueKind switch
        {
            JsonValueKind.Object => !string.IsNullOrWhiteSpace(ReadString(action, "id", "key")),
            _ => false,
        };
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

    private static string? ReadString(JsonElement root, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (!string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString();
            }
        }

        return null;
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
        var directories = new[]
        {
            PluginPaths.GetPluginDirectory(pluginId),
            Path.Combine(pluginsDirectory, "local", pluginId),
            Path.Combine(pluginsDirectory, $"LenovoLegionToolkit.Plugins.{pluginId}"),
            Path.Combine(pluginsDirectory, $"LenovoLegionToolkit.Plugins.{pluginId.Replace("-", string.Empty)}")
        };

        return directories
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
