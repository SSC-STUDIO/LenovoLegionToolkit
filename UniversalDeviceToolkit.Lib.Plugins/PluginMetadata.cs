using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Plugin metadata, used to describe plugin information
/// </summary>
public class PluginMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsSystemPlugin { get; set; }
    public string[]? Dependencies { get; set; }
    public string Version { get; set; } = "1.0.0";
    public string MinimumHostVersion { get; set; } = "1.0.0";
    public string? Author { get; set; }
    public string? FilePath { get; set; }
    public IReadOnlyList<string>? Tags { get; set; }
    public IReadOnlyDictionary<string, string>? LocalizedNames { get; set; }
    public IReadOnlyDictionary<string, string>? LocalizedDescriptions { get; set; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? LocalizedTags { get; set; }

    public string GetDisplayName(CultureInfo? culture = null)
    {
        return ResolveLocalizedString(LocalizedNames, Name, culture);
    }

    public string GetDisplayDescription(CultureInfo? culture = null)
    {
        return ResolveLocalizedString(LocalizedDescriptions, Description, culture);
    }

    public IReadOnlyList<string> GetDisplayTags(CultureInfo? culture = null)
    {
        if (LocalizedTags is not null)
        {
            var tagCulture = culture ?? CultureInfo.CurrentUICulture;
            if (TryMatchCulture(LocalizedTags, tagCulture, out var localized) && localized is not null)
                return localized;
            if (LocalizedTags.TryGetValue("default", out var defaultTags) && defaultTags is not null)
                return defaultTags;
        }

        return Tags ?? global::System.Array.Empty<string>();
    }

    private static string ResolveLocalizedString(
        IReadOnlyDictionary<string, string>? localized,
        string fallback,
        CultureInfo? culture)
    {
        if (localized is not null)
        {
            var tagCulture = culture ?? CultureInfo.CurrentUICulture;
            if (TryMatchCulture(localized, tagCulture, out var localizedValue) && localizedValue is not null)
                return localizedValue;
            if (localized.TryGetValue("default", out var defaultValue) && defaultValue is not null)
                return defaultValue;
        }

        return fallback;
    }

    private static bool TryMatchCulture<T>(
        IReadOnlyDictionary<string, T> dictionary,
        CultureInfo culture,
        out T? value)
    {
        foreach (var candidate in LocalizationCatalog.GetFallbackChain(culture))
        {
            if (dictionary.TryGetValue(candidate.Name, out var localized))
            {
                value = localized;
                return true;
            }
        }

        if (dictionary.TryGetValue("default", out var defaultValue))
        {
            value = defaultValue;
            return true;
        }

        value = default;
        return false;
    }
}
