using System;
using System.IO;
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
            var pluginDirectory = PluginPaths.GetPluginDirectory(pluginId);
            if (!Directory.Exists(pluginDirectory))
                return default;

            foreach (var manifestFileName in ManifestFileNames)
            {
                var manifestPath = Path.Combine(pluginDirectory, manifestFileName);
                if (!File.Exists(manifestPath))
                    continue;

                return ReadCapabilitiesFromJson(manifestPath);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to read UI capabilities for {pluginId}: {ex.Message}", ex);
        }

        return default;
    }

    public static PluginUiCapabilities ResolveKnownStorePlugin(string pluginId) =>
        pluginId.Trim().ToLowerInvariant() switch
        {
            "custom-mouse" => new PluginUiCapabilities
            {
                SupportsSettingsPage = true,
                SupportsOptimizationCategory = true,
            },
            "shell-integration" => new PluginUiCapabilities
            {
                SupportsOptimizationCategory = true,
            },
            "network-acceleration" => new PluginUiCapabilities
            {
                SupportsFeaturePage = true,
            },
            "vive-tool" => new PluginUiCapabilities
            {
                SupportsFeaturePage = true,
            },
            _ => default,
        };

    private static PluginUiCapabilities ReadCapabilitiesFromJson(string manifestPath)
    {
        using var stream = File.OpenRead(manifestPath);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;

        var supportsSettings = ReadBool(root, "hasSettingsPage", "hasSettings", "settingsPage", "supportsSettingsPage");
        var supportsFeature = ReadBool(root, "hasFeaturePage", "featurePage", "supportsFeaturePage", "hasPluginPage");
        var optimizationCategoryId = ReadString(root, "optimizationCategoryId", "optimizationCategory", "categoryId");
        var supportsOptimization = ReadBool(root, "hasOptimizationCategory", "supportsOptimizationCategory", "optimizationCategory")
            || !string.IsNullOrWhiteSpace(optimizationCategoryId);

        return new PluginUiCapabilities
        {
            SupportsSettingsPage = supportsSettings,
            SupportsFeaturePage = supportsFeature,
            SupportsOptimizationCategory = supportsOptimization,
        };
    }

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
}
