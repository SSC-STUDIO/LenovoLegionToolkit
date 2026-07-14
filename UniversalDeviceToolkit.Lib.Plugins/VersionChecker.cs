using System;
using System.Collections.Generic;
using System.Linq;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Plugin update information
/// </summary>
public class PluginUpdateInfo
{
    public string PluginId { get; set; } = string.Empty;
    public string CurrentVersion { get; set; } = string.Empty;
    public string NewVersion { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string Changelog { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
}

/// <summary>
/// Result of update check operation
/// </summary>
public class UpdateCheckResult
{
    public List<PluginUpdateInfo> AvailableUpdates { get; set; } = new();
    public bool HasUpdates => AvailableUpdates.Count > 0;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? LastCheckTime { get; set; }
}

/// <summary>
/// Update check result with compatibility information
/// </summary>
public class CompatibilityUpdateCheckResult : UpdateCheckResult
{
    public List<PluginManifest> IncompatiblePlugins { get; set; } = new();
}

/// <summary>
/// Version compatibility checker for plugins
/// </summary>
public class VersionChecker
{
    private readonly string _currentHostVersion;

    /// <summary>
    /// Creates a new VersionChecker with the current host version
    /// </summary>
    /// <param name="currentHostVersion">Current host application version</param>
    public VersionChecker(string currentHostVersion)
    {
        _currentHostVersion = currentHostVersion ?? throw new ArgumentNullException(nameof(currentHostVersion));
    }

    /// <summary>
    /// Creates a VersionChecker using the executing assembly version
    /// </summary>
    public VersionChecker() : this(GetCurrentHostVersion())
    {
    }

    /// <summary>
    /// Checks if a plugin's minimum host version requirement is satisfied by the current host version
    /// </summary>
    /// <param name="minimumHostVersion">The plugin's required minimum host version</param>
    /// <returns>True if the current host version meets or exceeds the minimum requirement</returns>
    public bool IsCompatible(string minimumHostVersion)
    {
        if (string.IsNullOrWhiteSpace(minimumHostVersion))
            return true;

        try
        {
            var minVersion = new Version(minimumHostVersion);
            var currentVersion = new Version(_currentHostVersion);
            return currentVersion >= minVersion;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning($"Error checking version compatibility: {ex.Message}");
            return true;
        }
    }

    /// <summary>
    /// Checks if an update is available by comparing versions
    /// </summary>
    /// <param name="currentVersion">Current installed version</param>
    /// <param name="newVersion">Available new version</param>
    /// <returns>True if new version is newer than current version</returns>
    public bool IsUpdateAvailable(string currentVersion, string newVersion)
    {
        if (string.IsNullOrWhiteSpace(newVersion))
            return false;

        // Empty/missing current version is treated as 0.0.0 so first installs can surface as upgrades.
        var baseline = string.IsNullOrWhiteSpace(currentVersion) ? "0.0.0.0" : currentVersion;
        return PluginVersionParser.IsNewerThan(newVersion, baseline);
    }

    /// <summary>
    /// Compares two version strings
    /// </summary>
    /// <param name="version1">First version</param>
    /// <param name="version2">Second version</param>
    /// <returns>Negative if version1 < version2, zero if equal, positive if version1 > version2</returns>
    public int CompareVersions(string version1, string version2)
    {
        var leftRaw = string.IsNullOrWhiteSpace(version1) ? "0.0.0.0" : version1;
        var rightRaw = string.IsNullOrWhiteSpace(version2) ? "0.0.0.0" : version2;

        if (PluginVersionParser.TryParse(leftRaw, out var left) &&
            PluginVersionParser.TryParse(rightRaw, out var right))
            return left.CompareTo(right);

        Log.Instance.Warning($"Error comparing versions: unable to parse '{version1}' and/or '{version2}'");
        return 0;
    }

    /// <summary>
    /// Gets the current host version from the executing assembly
    /// </summary>
    private static string GetCurrentHostVersion()
    {
        try
        {
            var assembly = global::System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0";
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "plugin-host-version",
                "Failed to read host assembly version; defaulting to 1.0.0.",
                ex);
            return "1.0.0";
        }
    }

    /// <summary>
    /// Checks multiple plugins for compatibility
    /// </summary>
    /// <param name="plugins">List of plugin manifests to check</param>
    /// <returns>List of incompatible plugins</returns>
    public List<PluginManifest> CheckCompatibility(List<PluginManifest> plugins)
    {
        var incompatible = new List<PluginManifest>();

        foreach (var plugin in plugins)
        {
            if (!IsCompatible(plugin.MinimumHostVersion))
            {
                incompatible.Add(plugin);
            }
        }

        return incompatible;
    }

    /// <summary>
    /// Gets available updates from a list of plugins
    /// </summary>
    /// <param name="installedPlugins">Dictionary of installed plugin versions</param>
    /// <param name="availablePlugins">List of available plugin manifests</param>
    /// <returns>List of available updates</returns>
    public List<PluginUpdateInfo> GetAvailableUpdates(
        Dictionary<string, string> installedPlugins,
        List<PluginManifest> availablePlugins)
    {
        var updates = new List<PluginUpdateInfo>();

        foreach (var available in availablePlugins)
        {
            if (installedPlugins.TryGetValue(available.Id, out var currentVersion))
            {
                if (IsUpdateAvailable(currentVersion, available.Version))
                {
                    updates.Add(new PluginUpdateInfo
                    {
                        PluginId = available.Id,
                        CurrentVersion = currentVersion,
                        NewVersion = available.Version,
                        DownloadUrl = available.DownloadUrl,
                        Changelog = available.Changelog ?? string.Empty,
                        ReleaseDate = available.ReleaseDate ?? string.Empty
                    });
                }
            }
        }

        return updates;
    }
}
