using System;
using System.Collections.Generic;
using System.Linq;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 插件管理器实现
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly ApplicationSettings _applicationSettings;
    private readonly Dictionary<string, IPlugin> _registeredPlugins = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<PluginEventArgs>? PluginStateChanged;

    public PluginManager(ApplicationSettings applicationSettings)
    {
        _applicationSettings = applicationSettings;
    }

    public void RegisterPlugin(IPlugin plugin)
    {
        if (string.IsNullOrWhiteSpace(plugin.Id))
            throw new ArgumentException("Plugin ID cannot be null or empty", nameof(plugin));

        if (_registeredPlugins.ContainsKey(plugin.Id))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugin {plugin.Id} is already registered. Replacing existing registration.");
        }

        _registeredPlugins[plugin.Id] = plugin;
    }

    public IEnumerable<IPlugin> GetRegisteredPlugins()
    {
        return _registeredPlugins.Values;
    }

    public PluginMetadata? GetPluginMetadata(string pluginId)
    {
        if (!_registeredPlugins.TryGetValue(pluginId, out var plugin))
            return null;

        return new PluginMetadata
        {
            Id = plugin.Id,
            Name = plugin.Name,
            Description = plugin.Description,
            Icon = plugin.Icon,
            IsSystemPlugin = plugin.IsSystemPlugin,
            Dependencies = plugin.Dependencies
        };
    }

    public bool IsInstalled(string pluginId)
    {
        return _applicationSettings.Store.InstalledExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase);
    }

    public void InstallPlugin(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return;

        var installedExtensions = _applicationSettings.Store.InstalledExtensions;

        if (!installedExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
        {
            installedExtensions.Add(pluginId);
            _applicationSettings.SynchronizeStore();

            // 检查并安装依赖
            if (_registeredPlugins.TryGetValue(pluginId, out var plugin) && plugin.Dependencies != null)
            {
                foreach (var dependency in plugin.Dependencies)
                {
                    if (!IsInstalled(dependency))
                    {
                        InstallPlugin(dependency);
                    }
                }
            }

            // 确保系统插件在安装其他插件时也被安装
            EnsureSystemPluginWhenNeeded();

            // 触发安装回调
            if (_registeredPlugins.TryGetValue(pluginId, out var installedPlugin))
            {
                installedPlugin.OnInstalled();
            }

            OnPluginStateChanged(pluginId, true);
        }
    }

    public bool UninstallPlugin(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        var installedExtensions = _applicationSettings.Store.InstalledExtensions;

        if (!installedExtensions.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
            return false;

        // 检查是否为系统插件，如果是系统插件且还有其他插件依赖它，则不能卸载
        if (_registeredPlugins.TryGetValue(pluginId, out var plugin) && plugin.IsSystemPlugin)
        {
            var hasOtherPlugins = installedExtensions.Any(ext =>
                !string.Equals(ext, pluginId, StringComparison.OrdinalIgnoreCase));

            if (hasOtherPlugins)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Cannot uninstall system plugin {pluginId} because other plugins are installed.");
                return false;
            }
        }

        // 检查是否有其他插件依赖此插件
        var dependentPlugins = _registeredPlugins.Values
            .Where(p => p.Dependencies != null && p.Dependencies.Contains(pluginId, StringComparer.OrdinalIgnoreCase))
            .Where(p => IsInstalled(p.Id))
            .ToList();

        if (dependentPlugins.Any())
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Cannot uninstall plugin {pluginId} because it is a dependency of other installed plugins.");
            return false;
        }

        installedExtensions.Remove(pluginId);
        _applicationSettings.SynchronizeStore();

        // 触发卸载回调
        plugin?.OnUninstalled();

        OnPluginStateChanged(pluginId, false);

        return true;
    }

    public IEnumerable<string> GetInstalledPluginIds()
    {
        return _applicationSettings.Store.InstalledExtensions;
    }

    /// <summary>
    /// 当安装其他插件时，确保系统插件也被安装（如果它是基础插件）
    /// </summary>
    private void EnsureSystemPluginWhenNeeded()
    {
        var systemPlugin = _registeredPlugins.Values.FirstOrDefault(p => p.IsSystemPlugin);
        if (systemPlugin == null)
            return;

        var installedExtensions = _applicationSettings.Store.InstalledExtensions;
        var hasOtherPlugins = installedExtensions.Any(ext =>
            !string.Equals(ext, systemPlugin.Id, StringComparison.OrdinalIgnoreCase));

        if (hasOtherPlugins && !installedExtensions.Contains(systemPlugin.Id, StringComparer.OrdinalIgnoreCase))
        {
            installedExtensions.Add(systemPlugin.Id);
            _applicationSettings.SynchronizeStore();
        }
    }

    protected virtual void OnPluginStateChanged(string pluginId, bool isInstalled)
    {
        PluginStateChanged?.Invoke(this, new PluginEventArgs(pluginId, isInstalled));
    }
}
