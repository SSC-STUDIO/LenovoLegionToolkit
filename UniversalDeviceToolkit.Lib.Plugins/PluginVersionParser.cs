using System;
using System.Globalization;

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Shared parsing and comparison helpers for plugin package/manifest versions.
/// Accepts semver-ish numeric versions, optional leading <c>v</c>/<c>V</c>,
/// optional <c>-prerelease</c>, and optional <c>+metadata</c>.
/// </summary>
public static class PluginVersionParser
{
    /// <summary>
    /// Attempts to parse a plugin version string into a <see cref="Version"/>.
    /// Trims whitespace, strips a single leading <c>v</c>/<c>V</c> prefix,
    /// build metadata, and a SemVer prerelease suffix.
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

        var plus = normalized.IndexOf('+');
        if (plus >= 0)
            normalized = normalized[..plus];

        var hyphen = normalized.IndexOf('-');
        if (hyphen >= 0)
            normalized = normalized[..hyphen];

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
        if (!TryParseSemVer(candidateVersion, out var candidate))
            return false;

        if (!TryParseSemVer(baselineVersion, out var baseline))
            return false;

        return Compare(candidate, baseline) > 0;
    }

    public static int Compare(string? leftVersion, string? rightVersion)
    {
        var leftRaw = string.IsNullOrWhiteSpace(leftVersion) ? "0.0.0.0" : leftVersion;
        var rightRaw = string.IsNullOrWhiteSpace(rightVersion) ? "0.0.0.0" : rightVersion;
        if (!TryParseSemVer(leftRaw, out var left) || !TryParseSemVer(rightRaw, out var right))
            return 0;

        return Compare(left, right);
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

    internal readonly record struct SemVer(int Major, int Minor, int Patch, string? Prerelease);

    internal static bool TryParseSemVer(string? rawVersion, out SemVer semVer)
    {
        semVer = default;
        if (string.IsNullOrWhiteSpace(rawVersion))
            return false;

        var normalized = rawVersion.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
            normalized = normalized[1..];

        var plus = normalized.IndexOf('+');
        if (plus >= 0)
            normalized = normalized[..plus];

        string? prerelease = null;
        var hyphen = normalized.IndexOf('-');
        if (hyphen >= 0)
        {
            prerelease = normalized[(hyphen + 1)..];
            normalized = normalized[..hyphen];
            if (string.IsNullOrWhiteSpace(prerelease))
                return false;
        }

        var parts = normalized.Split('.');
        if (parts.Length is < 2 or > 4)
            return false;

        if (!TryParseNonNegativeInt(parts[0], out var major) ||
            !TryParseNonNegativeInt(parts[1], out var minor))
            return false;

        var patch = 0;
        if (parts.Length >= 3 && !TryParseNonNegativeInt(parts[2], out patch))
            return false;

        if (parts.Length == 4 && !TryParseNonNegativeInt(parts[3], out _))
            return false;

        semVer = new SemVer(major, minor, patch, prerelease);
        return true;
    }

    internal static int Compare(SemVer left, SemVer right)
    {
        var numeric = left.Major.CompareTo(right.Major);
        if (numeric != 0)
            return numeric;

        numeric = left.Minor.CompareTo(right.Minor);
        if (numeric != 0)
            return numeric;

        numeric = left.Patch.CompareTo(right.Patch);
        if (numeric != 0)
            return numeric;

        var leftHasPre = !string.IsNullOrEmpty(left.Prerelease);
        var rightHasPre = !string.IsNullOrEmpty(right.Prerelease);
        if (!leftHasPre && !rightHasPre)
            return 0;
        if (!leftHasPre)
            return 1;
        if (!rightHasPre)
            return -1;

        return ComparePrerelease(left.Prerelease!, right.Prerelease!);
    }

    private static int ComparePrerelease(string left, string right)
    {
        var leftParts = left.Split('.');
        var rightParts = right.Split('.');
        var count = Math.Max(leftParts.Length, rightParts.Length);
        for (var i = 0; i < count; i++)
        {
            if (i >= leftParts.Length)
                return -1;
            if (i >= rightParts.Length)
                return 1;

            var leftNumeric = int.TryParse(leftParts[i], NumberStyles.None, CultureInfo.InvariantCulture, out var leftValue);
            var rightNumeric = int.TryParse(rightParts[i], NumberStyles.None, CultureInfo.InvariantCulture, out var rightValue);
            if (leftNumeric && rightNumeric)
            {
                var compared = leftValue.CompareTo(rightValue);
                if (compared != 0)
                    return compared;
                continue;
            }

            if (leftNumeric)
                return -1;
            if (rightNumeric)
                return 1;

            var ordinal = string.Compare(leftParts[i], rightParts[i], StringComparison.Ordinal);
            if (ordinal != 0)
                return ordinal;
        }

        return 0;
    }

    private static bool TryParseNonNegativeInt(string value, out int parsed)
    {
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed) && parsed >= 0;
    }
}
