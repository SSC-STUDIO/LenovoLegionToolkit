using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
    private void CacheAvailablePlugins(List<PluginManifest> plugins)
    {
        lock (_availablePluginsCacheLock)
        {
            _availablePluginsMemoryCache = ClonePluginManifestList(plugins);
            _availablePluginsMemoryCacheUpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static List<PluginManifest> ClonePluginManifestList(IEnumerable<PluginManifest> plugins) =>
        plugins.Select(ClonePluginManifest).ToList();

    private static PluginManifest ClonePluginManifest(PluginManifest manifest) =>
        new()
        {
            Id = manifest.Id,
            Name = manifest.Name,
            Description = manifest.Description,
            Details = manifest.Details,
            UsageGuide = manifest.UsageGuide,
            Localizations = CloneLocalizations(manifest.Localizations),
            LocalizedNames = CloneLocalizedStrings(manifest.LocalizedNames),
            LocalizedDescriptions = CloneLocalizedStrings(manifest.LocalizedDescriptions),
            LocalizedTags = CloneLocalizedTags(manifest.LocalizedTags),
            Store = CloneStore(manifest.Store),
            Contributes = CloneContributions(manifest.Contributes),
            Icon = manifest.Icon,
            IconBackground = manifest.IconBackground,
            Author = manifest.Author,
            Version = manifest.Version,
            MinimumHostVersion = manifest.MinimumHostVersion,
            Dependencies = manifest.Dependencies?.ToArray(),
            DownloadUrl = manifest.DownloadUrl,
            FileHash = manifest.FileHash,
            ZipHash = manifest.ZipHash,
            FileSize = manifest.FileSize,
            ReleaseDate = manifest.ReleaseDate,
            Changelog = manifest.Changelog,
            Tags = manifest.Tags?.ToArray(),
            IsSystemPlugin = manifest.IsSystemPlugin
        };

    private static PluginManifestStore? CloneStore(PluginManifestStore? store) =>
        store is null
            ? null
            : new PluginManifestStore
            {
                Description = store.Description,
                Details = store.Details,
                UsageGuide = store.UsageGuide,
                Localizations = CloneLocalizations(store.Localizations),
                LocalizedNames = CloneLocalizedStrings(store.LocalizedNames),
                LocalizedDescriptions = CloneLocalizedStrings(store.LocalizedDescriptions),
                LocalizedTags = CloneLocalizedTags(store.LocalizedTags),
                Tags = store.Tags?.ToArray()
            };

    private static Dictionary<string, PluginManifestLocalization>? CloneLocalizations(
        Dictionary<string, PluginManifestLocalization>? localizations) =>
        localizations is null
            ? null
            : localizations.ToDictionary(
                pair => pair.Key,
                pair => new PluginManifestLocalization
                {
                    Name = pair.Value.Name,
                    Description = pair.Value.Description,
                    Details = pair.Value.Details,
                    UsageGuide = pair.Value.UsageGuide
                },
                StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string>? CloneLocalizedStrings(Dictionary<string, string>? localized) =>
        localized is null
            ? null
            : localized.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string[]>? CloneLocalizedTags(Dictionary<string, string[]>? localized) =>
        localized is null
            ? null
            : localized.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, string>? MergeLocalizedStrings(
        Dictionary<string, string>? primary,
        Dictionary<string, string>? secondary)
    {
        var merged = CloneLocalizedStrings(primary) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (secondary is not null)
        {
            foreach (var pair in secondary)
                merged.TryAdd(pair.Key, pair.Value);
        }

        return merged.Count == 0 ? null : merged;
    }

    private static Dictionary<string, string[]>? MergeLocalizedTags(
        Dictionary<string, string[]>? primary,
        Dictionary<string, string[]>? secondary)
    {
        var merged = CloneLocalizedTags(primary) ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (secondary is not null)
        {
            foreach (var pair in secondary)
                merged.TryAdd(pair.Key, pair.Value.ToArray());
        }

        return merged.Count == 0 ? null : merged;
    }

    private static PluginManifestContributions? CloneContributions(PluginManifestContributions? contributes) =>
        contributes is null
            ? null
            : new PluginManifestContributions
            {
                FeaturePage = ClonePageContribution(contributes.FeaturePage),
                SettingsPage = ClonePageContribution(contributes.SettingsPage),
                Runtime = contributes.Runtime is null
                    ? null
                    : new PluginManifestRuntimeContribution
                    {
                        Class = contributes.Runtime.Class
                    },
                OptimizationActions = contributes.OptimizationActions?
                    .Select(action => new PluginManifestOptimizationContribution
                    {
                        Id = action.Id,
                        Key = action.Key,
                        Description = action.Description,
                        Recommended = action.Recommended,
                        Title = action.Title
                    })
                    .ToList()
            };

    private static PluginManifestPageContribution? ClonePageContribution(PluginManifestPageContribution? contribution) =>
        contribution is null
            ? null
            : new PluginManifestPageContribution
            {
                Class = contribution.Class,
                Title = contribution.Title
            };

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return string.Empty;
    }

    private static string? FirstNonEmptyNullable(params string?[] values)
    {
        var value = FirstNonEmpty(values);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
