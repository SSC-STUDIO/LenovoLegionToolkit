using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin manager interface
/// </summary>
public interface IPluginManager : IDisposable
{
    /// <summary>
    /// Plugin state changed event
    /// </summary>
    event EventHandler<PluginEventArgs>? PluginStateChanged;

    /// <summary>
    /// Get all registered plugins
    /// </summary>
    IEnumerable<IPlugin> GetRegisteredPlugins();

    /// <summary>
    /// Get plugin metadata
    /// </summary>
    PluginMetadata? GetPluginMetadata(string pluginId);

    /// <summary>
    /// Check if plugin is installed
    /// </summary>
    bool IsInstalled(string pluginId);

    /// <summary>
    /// Install plugin
    /// </summary>
    void InstallPlugin(string pluginId);

    /// <summary>
    /// Uninstall plugin
    /// </summary>
    bool UninstallPlugin(string pluginId);

    /// <summary>
    /// Get all installed plugin IDs
    /// </summary>
    IEnumerable<string> GetInstalledPluginIds();

    /// <summary>
    /// Scan and load plugins from the plugins directory
    /// </summary>
    /// <returns>A task that completes when the scan is finished</returns>
    Task ScanAndLoadPluginsAsync(bool forceRefresh = false);

    /// <summary>
    /// Permanently delete plugin files from disk asynchronously
    /// </summary>
    Task<bool> PermanentlyDeletePluginAsync(string pluginId);

    /// <summary>
    /// Unload all plugins and release references (useful before plugin updates)
    /// </summary>
    void UnloadAllPlugins();

    /// <summary>
    /// Stop a specific plugin (call its Stop method) before update or uninstallation
    /// </summary>
    bool StopPlugin(string pluginId);

    /// <summary>
    /// Stop all plugins (call Stop method for each plugin)
    /// </summary>
    void StopAllPlugins();

    /// <summary>
    /// Try to get a plugin by ID
    /// </summary>
    /// <param name="pluginId">The plugin ID</param>
    /// <param name="plugin">The plugin instance if found</param>
    /// <returns>True if the plugin was found</returns>
    bool TryGetPlugin(string pluginId, out IPlugin? plugin);
    
    /// <summary>
    /// Perform pending plugin deletions asynchronously
    /// </summary>
    Task PerformPendingDeletionsAsync();
    
    /// <summary>
    /// Check if all plugin dependencies are satisfied
    /// </summary>
    bool CheckDependencies(string pluginId, out List<string> missingDependencies);

    /// <summary>
    /// Check for plugin updates (returns a dictionary of pluginId -> availableVersion)
    /// </summary>
    Task<Dictionary<string, string>> CheckForUpdatesAsync();
}

/// <summary>
/// Plugin event arguments
/// </summary>
public class PluginEventArgs : EventArgs
{
    public string PluginId { get; }
    public bool IsInstalled { get; }

    public PluginEventArgs(string pluginId, bool isInstalled)
    {
        PluginId = pluginId;
        IsInstalled = isInstalled;
    }
}
