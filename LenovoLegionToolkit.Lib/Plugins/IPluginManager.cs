using System;
using System.Collections.Generic;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 插件管理器接口
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// 插件状态变化事件
    /// </summary>
    event EventHandler<PluginEventArgs>? PluginStateChanged;

    /// <summary>
    /// 注册插件
    /// </summary>
    void RegisterPlugin(IPlugin plugin);

    /// <summary>
    /// 获取所有已注册的插件
    /// </summary>
    IEnumerable<IPlugin> GetRegisteredPlugins();

    /// <summary>
    /// 获取插件元数据
    /// </summary>
    PluginMetadata? GetPluginMetadata(string pluginId);

    /// <summary>
    /// 检查插件是否已安装
    /// </summary>
    bool IsInstalled(string pluginId);

    /// <summary>
    /// 安装插件
    /// </summary>
    void InstallPlugin(string pluginId);

    /// <summary>
    /// 卸载插件
    /// </summary>
    bool UninstallPlugin(string pluginId);

    /// <summary>
    /// 获取所有已安装的插件ID
    /// </summary>
    IEnumerable<string> GetInstalledPluginIds();
}

/// <summary>
/// 插件事件参数
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
