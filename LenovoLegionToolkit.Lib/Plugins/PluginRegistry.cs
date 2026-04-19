using System;
using System.Collections.Generic;
using System.Linq;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin registry interface
/// </summary>
public interface IPluginRegistry
{
    /// <summary>
    /// Register a plugin with its metadata
    /// </summary>
    void Register(IPlugin plugin, PluginMetadata metadata);

    /// <summary>
    /// Unregister a plugin by ID
    /// </summary>
    void Unregister(string pluginId);

    /// <summary>
    /// Get a registered plugin by ID
    /// </summary>
    IPlugin? Get(string pluginId);

    /// <summary>
    /// Get all registered plugins (returns a snapshot copy for thread safety)
    /// </summary>
    IEnumerable<IPlugin> GetAll();

    /// <summary>
    /// Get metadata for a plugin
    /// </summary>
    PluginMetadata? GetMetadata(string pluginId);

    /// <summary>
    /// Check if a plugin is registered
    /// </summary>
    bool IsRegistered(string pluginId);

    /// <summary>
    /// Get count of registered plugins
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Mark a plugin as started
    /// </summary>
    bool MarkStarted(string pluginId);

    /// <summary>
    /// Mark a plugin as stopped
    /// </summary>
    void MarkStopped(string pluginId);

    /// <summary>
    /// Get all started plugin IDs
    /// </summary>
    IEnumerable<string> GetStartedPluginIds();

    /// <summary>
    /// Clear all registrations
    /// </summary>
    void Clear();
}

/// <summary>
/// Plugin registry implementation
/// Manages plugin registration, metadata, and lifecycle
/// Thread-safe implementation using locks
/// </summary>
public class PluginRegistry : IPluginRegistry
{
    private readonly Dictionary<string, IPlugin> _registeredPlugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PluginMetadata> _pluginMetadataCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _startedPlugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    /// <summary>
    /// Get count of registered plugins
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _registeredPlugins.Count;
            }
        }
    }

    /// <summary>
    /// Register a plugin with its metadata
    /// </summary>
    public void Register(IPlugin plugin, PluginMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(plugin.Id))
            throw new ArgumentException("Plugin ID cannot be null or empty", nameof(plugin));

        lock (_lock)
        {
            if (_registeredPlugins.TryGetValue(plugin.Id, out var existingPlugin))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Plugin {plugin.Id} is already registered. Replacing existing registration.");

                try
                {
                    existingPlugin.Stop();
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to stop existing plugin {plugin.Id}: {ex.Message}", ex);
                }

                _startedPlugins.Remove(plugin.Id);
            }

            _registeredPlugins[plugin.Id] = plugin;
            _pluginMetadataCache[plugin.Id] = metadata;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Registered plugin: {plugin.Id} ({plugin.Name})");
    }

    /// <summary>
    /// Unregister a plugin by ID
    /// </summary>
    public void Unregister(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        IPlugin? plugin = null;
        PluginMetadata? metadata = null;
        lock (_lock)
        {
            if (_registeredPlugins.TryGetValue(pluginId, out plugin))
            {
                _pluginMetadataCache.TryGetValue(pluginId, out metadata);
                _registeredPlugins.Remove(pluginId);
                _pluginMetadataCache.Remove(pluginId);
                _startedPlugins.Remove(pluginId);
            }
        }

        if (plugin != null)
        {
            try
            {
                plugin.Stop();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to stop plugin {pluginId}: {ex.Message}", ex);
            }

            // Preserve metadata access during uninstall callback
            // Some plugins may need to read their own metadata during cleanup
            try
            {
                plugin.OnUninstalled();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to call OnUninstalled for plugin {pluginId}: {ex.Message}", ex);
            }
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Unregistered plugin: {pluginId}");
    }

    /// <summary>
    /// Get a registered plugin by ID
    /// </summary>
    public IPlugin? Get(string pluginId)
    {
        lock (_lock)
        {
            return _registeredPlugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
        }
    }

    /// <summary>
    /// Get all registered plugins (returns a snapshot copy for thread safety)
    /// </summary>
    public IEnumerable<IPlugin> GetAll()
    {
        lock (_lock)
        {
            return _registeredPlugins.Values.ToList();
        }
    }

    /// <summary>
    /// Get metadata for a plugin
    /// </summary>
    public PluginMetadata? GetMetadata(string pluginId)
    {
        lock (_lock)
        {
            return _pluginMetadataCache.TryGetValue(pluginId, out var metadata) ? metadata : null;
        }
    }

    /// <summary>
    /// Check if a plugin is registered
    /// </summary>
    public bool IsRegistered(string pluginId)
    {
        lock (_lock)
        {
            return _registeredPlugins.ContainsKey(pluginId);
        }
    }

    /// <summary>
    /// Check if a plugin has been started
    /// </summary>
    public bool IsStarted(string pluginId)
    {
        lock (_lock)
        {
            return _startedPlugins.Contains(pluginId);
        }
    }

    /// <summary>
    /// Mark a plugin as started
    /// </summary>
    public bool MarkStarted(string pluginId)
    {
        lock (_lock)
        {
            return _startedPlugins.Add(pluginId);
        }
    }

    /// <summary>
    /// Mark a plugin as stopped
    /// </summary>
    public void MarkStopped(string pluginId)
    {
        lock (_lock)
        {
            _startedPlugins.Remove(pluginId);
        }
    }

    /// <summary>
    /// Get all started plugins
    /// </summary>
    public IEnumerable<string> GetStartedPluginIds()
    {
        lock (_lock)
        {
            return _startedPlugins.ToList();
        }
    }

    /// <summary>
    /// Clear all registrations
    /// </summary>
    public void Clear()
    {
        List<IPlugin> plugins;
        lock (_lock)
        {
            plugins = _registeredPlugins.Values.ToList();
            _registeredPlugins.Clear();
            _pluginMetadataCache.Clear();
            _startedPlugins.Clear();
        }

        foreach (var plugin in plugins)
        {
            try
            {
                plugin.OnUninstalled();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to trigger OnUninstalled for plugin {plugin.Id}: {ex.Message}", ex);
            }
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace("Cleared all plugin registrations");
    }
}
