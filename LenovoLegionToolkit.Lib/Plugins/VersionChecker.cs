using System;
using System.Collections.Generic;
using System.Linq;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

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
/// Plugin update settings
/// </summary>
public class PluginUpdateSettings
{
    /// <summary>
    /// Whether to check for updates on application startup
    /// </summary>
    public bool CheckOnStartup { get; set; } = true;

    /// <summary>
    /// Whether to automatically download updates in the background
    /// </summary>
    public bool AutoDownloadUpdates { get; set; } = false;

    /// <summary>
    /// Whether to notify user when updates are available
    /// </summary>
    public bool NotifyOnUpdate { get; set; } = true;

    /// <summary>
    /// Update check frequency (in hours)
    /// </summary>
    public int CheckFrequencyHours { get; set; } = 24;

    /// <summary>
    /// Last time updates were checked
    /// </summary>
    public DateTime? LastCheckTime { get; set; }

    /// <summary>
    /// Pending updates to install after restart
    /// </summary>
    public List<string> PendingUpdates { get; set; } = new();
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
            Log.Instance.Trace($"Error checking version compatibility: {ex.Message}");
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

        try
        {
            var current = string.IsNullOrWhiteSpace(currentVersion) 
                ? new Version("0.0.0.0") 
                : new Version(currentVersion);
            var latest = new Version(newVersion);
            return latest > current;
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"Error checking update availability: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Compares two version strings
    /// </summary>
    /// <param name="version1">First version</param>
    /// <param name="version2">Second version</param>
    /// <returns>Negative if version1 < version2, zero if equal, positive if version1 > version2</returns>
    public int CompareVersions(string version1, string version2)
    {
        try
        {
            var v1 = string.IsNullOrWhiteSpace(version1) ? new Version("0.0.0.0") : new Version(version1);
            var v2 = string.IsNullOrWhiteSpace(version2) ? new Version("0.0.0.0") : new Version(version2);
            return v1.CompareTo(v2);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"Error comparing versions: {ex.Message}");
            return 0;
        }
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
        catch
        {
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
