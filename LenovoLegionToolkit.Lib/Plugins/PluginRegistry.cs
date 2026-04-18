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
    /// Get all registered plugins
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
/// </summary>
public class PluginRegistry : IPluginRegistry
{
    private readonly Dictionary<string, IPlugin> _registeredPlugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PluginMetadata> _pluginMetadataCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _startedPlugins = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Get count of registered plugins
    /// </summary>
    public int Count => _registeredPlugins.Count;

    /// <summary>
    /// Register a plugin with its metadata
    /// </summary>
    public void Register(IPlugin plugin, PluginMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(plugin.Id))
            throw new ArgumentException("Plugin ID cannot be null or empty", nameof(plugin));

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

        if (_registeredPlugins.TryGetValue(pluginId, out var plugin))
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

            plugin.OnUninstalled();
        }

        _registeredPlugins.Remove(pluginId);
        _pluginMetadataCache.Remove(pluginId);
        _startedPlugins.Remove(pluginId);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Unregistered plugin: {pluginId}");
    }

    /// <summary>
    /// Get a registered plugin by ID
    /// </summary>
    public IPlugin? Get(string pluginId)
    {
        return _registeredPlugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
    }

    /// <summary>
    /// Get all registered plugins
    /// </summary>
    public IEnumerable<IPlugin> GetAll()
    {
        return _registeredPlugins.Values;
    }

    /// <summary>
    /// Get metadata for a plugin
    /// </summary>
    public PluginMetadata? GetMetadata(string pluginId)
    {
        return _pluginMetadataCache.TryGetValue(pluginId, out var metadata) ? metadata : null;
    }

    /// <summary>
    /// Check if a plugin is registered
    /// </summary>
    public bool IsRegistered(string pluginId)
    {
        return _registeredPlugins.ContainsKey(pluginId);
    }

    /// <summary>
    /// Check if a plugin has been started
    /// </summary>
    public bool IsStarted(string pluginId)
    {
        return _startedPlugins.Contains(pluginId);
    }

    /// <summary>
    /// Mark a plugin as started
    /// </summary>
    public bool MarkStarted(string pluginId)
    {
        return _startedPlugins.Add(pluginId);
    }

    /// <summary>
    /// Mark a plugin as stopped
    /// </summary>
    public void MarkStopped(string pluginId)
    {
        _startedPlugins.Remove(pluginId);
    }

    /// <summary>
    /// Get all started plugins
    /// </summary>
    public IEnumerable<string> GetStartedPluginIds()
    {
        return _startedPlugins;
    }

    /// <summary>
    /// Clear all registrations
    /// </summary>
    public void Clear()
    {
        foreach (var plugin in _registeredPlugins.Values)
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

        _registeredPlugins.Clear();
        _pluginMetadataCache.Clear();
        _startedPlugins.Clear();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace("Cleared all plugin registrations");
    }
}