using System;
using System.Collections.Generic;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin manager interface
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// Plugin state changed event
    /// </summary>
    event EventHandler<PluginEventArgs>? PluginStateChanged;

    /// <summary>
    /// Register plugin
    /// </summary>
    void RegisterPlugin(IPlugin plugin);

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
    void ScanAndLoadPlugins();

    /// <summary>
    /// Permanently delete plugin files from disk (cannot be recovered)
    /// </summary>
    bool PermanentlyDeletePlugin(string pluginId);

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

