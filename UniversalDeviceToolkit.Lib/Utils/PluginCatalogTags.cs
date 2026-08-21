using System;

namespace UniversalDeviceToolkit.Lib.Utils;

/// <summary>
/// Rolling GitHub Release tags that hold plugin <c>store.json</c> and package ZIPs.
/// Application updates must ignore these tags; preview hosts read the preview catalog.
/// </summary>
public static class PluginCatalogTags
{
    public const string Stable = "plugin-catalog";
    public const string Preview = "plugin-catalog-preview";

    public const string OfficialOwner = "SSC-STUDIO";
    public const string OfficialRepository = "UniversalDeviceToolkit";

    public static bool IsCatalogTag(string? tag) =>
        string.Equals(tag, Stable, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(tag, Preview, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when a GitHub release is a public application update: not a draft,
    /// not a rolling plugin-catalog tag, and not a prerelease unless requested.
    /// SemVer hyphens are treated as prerelease even if the GitHub flag is off.
    /// </summary>
    public static bool IsPublicApplicationRelease(
        string? tagName,
        bool draft,
        bool prerelease,
        bool includePrerelease)
    {
        if (draft || IsCatalogTag(tagName))
            return false;

        if (!includePrerelease && (prerelease || IsPrereleaseApplicationVersion(tagName)))
            return false;

        return true;
    }

    /// <summary>
    /// True when the version label contains a SemVer prerelease hyphen, matching
    /// <c>Release.yml</c> (tags such as <c>v6.0.0-preview.1</c>). Build metadata
    /// after <c>+</c> is ignored.
    /// </summary>
    public static bool IsPrereleaseApplicationVersion(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
            return false;

        var core = informationalVersion.Trim();
        if (core.StartsWith('v') || core.StartsWith('V'))
            core = core[1..];

        var plus = core.IndexOf('+');
        if (plus >= 0)
            core = core[..plus];

        return core.Contains('-', StringComparison.Ordinal);
    }

    public static string ResolveTag(string? informationalVersion) =>
        IsPrereleaseApplicationVersion(informationalVersion) ? Preview : Stable;

    public static string StoreDownloadUrl(string tag) =>
        $"https://github.com/{OfficialOwner}/{OfficialRepository}/releases/download/{tag}/store.json";

    public static string ReleasesApiUrl(string tag) =>
        $"https://api.github.com/repos/{OfficialOwner}/{OfficialRepository}/releases/tags/{tag}";

    public static string PackageDownloadUrl(string tag, string pluginId, string pluginVersion) =>
        $"https://github.com/{OfficialOwner}/{OfficialRepository}/releases/download/{tag}/{pluginId}-v{pluginVersion}.zip";
}
