using System;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Shared parsing and comparison helpers for plugin package/manifest versions.
/// Accepts semver-ish numeric versions and optional leading <c>v</c>/<c>V</c> prefixes.
/// </summary>
public static class PluginVersionParser
{
    /// <summary>
    /// Attempts to parse a plugin version string into a <see cref="Version"/>.
    /// Trims whitespace and strips a single leading <c>v</c>/<c>V</c> prefix.
    /// On failure, <paramref name="version"/> is set to <c>0.0.0.0</c>.
    /// </summary>
    public static bool TryParse(string? rawVersion, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(rawVersion))
            return false;

        var normalized = rawVersion.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
            normalized = normalized[1..];

        if (Version.TryParse(normalized, out var parsedVersion))
        {
            version = parsedVersion;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true only when both versions parse successfully and
    /// <paramref name="candidateVersion"/> is strictly greater than <paramref name="baselineVersion"/>.
    /// Invalid or null inputs never report as an upgrade.
    /// </summary>
    public static bool IsNewerThan(string? candidateVersion, string? baselineVersion)
    {
        if (!TryParse(candidateVersion, out var candidate))
            return false;

        if (!TryParse(baselineVersion, out var baseline))
            return false;

        return candidate > baseline;
    }

    /// <summary>
    /// Resolves the version string for an installed plugin: prefer the on-disk
    /// installed manifest, otherwise fall back to <paramref name="metadataVersion"/>.
    /// Returns null when the plugin id is empty or no version source is available.
    /// </summary>
    public static string? ResolveInstalledVersion(string pluginId, string? metadataVersion)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return null;

        var manifest = PluginUiCapabilityResolver.ReadInstalledManifest(pluginId);
        if (!string.IsNullOrWhiteSpace(manifest?.Version))
            return manifest.Version;

        return string.IsNullOrWhiteSpace(metadataVersion) ? null : metadataVersion;
    }
}
