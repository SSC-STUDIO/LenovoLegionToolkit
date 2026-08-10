using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Avalonia.Resources;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class PluginExtensionsPage
{
    private PluginManifest? ResolvePluginManifestForDisplay(IPlugin plugin)
    {
        if (plugin is PluginManifestAdapter adapter)
            return adapter.Manifest;

        return ResolvePluginManifestMetadata(plugin.Id);
    }

    private static PluginMetadata CreatePluginDisplayMetadata(IPlugin plugin, PluginManifest? manifest)
    {
        var fallbackName = ResolvePluginManifestText(manifest, static localization => localization.Name, manifest?.Name);
        var fallbackDescription = ResolvePluginManifestText(manifest, static localization => localization.Description, manifest?.Description ?? manifest?.Store?.Description);

        return new PluginMetadata
        {
            Id = plugin.Id,
            Name = string.IsNullOrWhiteSpace(fallbackName) ? plugin.Name : fallbackName,
            Description = string.IsNullOrWhiteSpace(fallbackDescription) ? plugin.Description : fallbackDescription,
            Icon = plugin.Icon,
            IsSystemPlugin = plugin.IsSystemPlugin,
            Dependencies = plugin.Dependencies,
            Tags = manifest?.Tags ?? manifest?.Store?.Tags,
            LocalizedNames = MergeLocalizedStrings(manifest?.Store?.LocalizedNames, manifest?.LocalizedNames),
            LocalizedDescriptions = MergeLocalizedStrings(manifest?.Store?.LocalizedDescriptions, manifest?.LocalizedDescriptions),
            LocalizedTags = MergeLocalizedTags(manifest?.Store?.LocalizedTags, manifest?.LocalizedTags)
        };
    }

    private static IReadOnlyDictionary<string, string>? MergeLocalizedStrings(
        Dictionary<string, string>? secondary,
        Dictionary<string, string>? primary)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (secondary is not null)
        {
            foreach (var pair in secondary)
                result[pair.Key] = pair.Value;
        }

        if (primary is not null)
        {
            foreach (var pair in primary)
                result[pair.Key] = pair.Value;
        }

        return result.Count == 0 ? null : result;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>>? MergeLocalizedTags(
        Dictionary<string, string[]>? secondary,
        Dictionary<string, string[]>? primary)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (secondary is not null)
        {
            foreach (var pair in secondary)
                result[pair.Key] = pair.Value;
        }

        if (primary is not null)
        {
            foreach (var pair in primary)
                result[pair.Key] = pair.Value;
        }

        return result.Count == 0 ? null : result;
    }

    private string GetPluginLocalizedName(IPlugin plugin, PluginManifest? manifest)
    {
        var metadata = CreatePluginDisplayMetadata(plugin, manifest);
        return RemovePluginSuffix(metadata.GetDisplayName(Resource.Culture ?? CultureInfo.CurrentUICulture));
    }

    private string GetPluginLocalizedDescription(IPlugin plugin, PluginManifest? manifest)
    {
        var metadata = CreatePluginDisplayMetadata(plugin, manifest);
        return metadata.GetDisplayDescription(Resource.Culture ?? CultureInfo.CurrentUICulture);
    }

    private IReadOnlyList<string> GetPluginLocalizedTags(IPlugin plugin, PluginManifest? manifest)
    {
        var metadata = CreatePluginDisplayMetadata(plugin, manifest);
        return metadata.GetDisplayTags(Resource.Culture ?? CultureInfo.CurrentUICulture);
    }

    private string GetPluginDetailedDescription(PluginManifest? manifest)
    {
        var manifestValue = ResolvePluginManifestText(manifest, static localization => localization.Details, manifest?.Details ?? manifest?.Store?.Details);
        if (!string.IsNullOrWhiteSpace(manifestValue))
            return manifestValue;

        return string.Empty;
    }

    private string GetPluginUsageGuide(PluginManifest? manifest)
    {
        var manifestValue = ResolvePluginManifestText(manifest, static localization => localization.UsageGuide, manifest?.UsageGuide ?? manifest?.Store?.UsageGuide);
        if (!string.IsNullOrWhiteSpace(manifestValue))
            return manifestValue;

        return string.Empty;
    }

    private static string ResolvePluginManifestText(
        PluginManifest? manifest,
        Func<PluginManifestLocalization, string?> selector,
        string? fallback)
    {
        if (manifest is null)
            return fallback ?? string.Empty;

        foreach (var localization in EnumeratePluginLocalizations(manifest))
        {
            var value = selector(localization);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return fallback ?? string.Empty;
    }

    private static IEnumerable<PluginManifestLocalization> EnumeratePluginLocalizations(PluginManifest manifest)
    {
        var activeCulture = Resource.Culture ?? CultureInfo.CurrentUICulture;
        var localizations = MergePluginLocalizations(manifest.Localizations, manifest.Store?.Localizations);
        foreach (var cultureName in EnumerateCultureNames(activeCulture))
        {
            if (localizations.TryGetValue(cultureName, out var localization))
                yield return localization;
        }
    }

    private static Dictionary<string, PluginManifestLocalization> MergePluginLocalizations(
        Dictionary<string, PluginManifestLocalization>? primary,
        Dictionary<string, PluginManifestLocalization>? secondary)
    {
        var result = new Dictionary<string, PluginManifestLocalization>(StringComparer.OrdinalIgnoreCase);

        if (secondary is not null)
        {
            foreach (var pair in secondary)
                result[pair.Key] = pair.Value;
        }

        if (primary is not null)
        {
            foreach (var pair in primary)
                result[pair.Key] = pair.Value;
        }

        return result;
    }

    private static IEnumerable<string> EnumerateCultureNames(CultureInfo culture)
    {
        foreach (var fallbackCulture in LocalizationCatalog.GetFallbackChain(culture))
            yield return fallbackCulture.Name;

        // Plugin manifests may use the explicit neutral key even when their
        // host resource catalog uses the canonical English key.
        yield return "default";
    }

    private static PluginManifest? TryReadInstalledPluginManifest(string pluginId, string? pluginFilePath)
    {
        if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(pluginFilePath))
            return null;

        try
        {
            var pluginDirectory = Path.GetDirectoryName(pluginFilePath);
            if (string.IsNullOrWhiteSpace(pluginDirectory) || !Directory.Exists(pluginDirectory))
                return null;

            foreach (var manifestPath in EnumerateInstalledPluginManifestPaths(pluginDirectory))
            {
                try
                {
                    using var stream = File.OpenRead(manifestPath);
                    var manifest = JsonSerializer.Deserialize<PluginManifest>(stream, InstalledPluginManifestJsonOptions);
                    if (manifest is not null && pluginId.Equals(manifest.Id, StringComparison.OrdinalIgnoreCase))
                        return manifest;
                }
                catch (Exception ex)
                {
                    if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                        UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Failed to read plugin manifest '{manifestPath}': {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            if (UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled)
                UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace($"Failed to locate installed plugin manifest for {pluginId}: {ex.Message}", ex);
        }

        return null;
    }

    private static readonly JsonSerializerOptions InstalledPluginManifestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static IEnumerable<string> EnumerateInstalledPluginManifestPaths(string pluginDirectory)
    {
        yield return Path.Combine(pluginDirectory, "plugin.manifest.json");
        yield return Path.Combine(pluginDirectory, "plugin.json");
        yield return Path.Combine(pluginDirectory, "Plugin.json");
    }
}
