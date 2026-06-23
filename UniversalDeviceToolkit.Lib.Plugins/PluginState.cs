// Plugin SDK surface: see SDK_BOUNDARY.md for the full public contract
// that plugins are allowed to depend on. This file is part of the
// public SDK; transitions between PluginState values are enforced
// by PluginLifecycleStateMachine (host-internal) rather than by
// plugins themselves.

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 插件状态枚举
/// </summary>
public enum PluginState
{
    /// <summary>
    /// 未安装
    /// </summary>
    NotInstalled,
    
    /// <summary>
    /// 已安装但未启用
    /// </summary>
    Installed,
    
    /// <summary>
    /// 已启用（运行中）
    /// </summary>
    Enabled,
    
    /// <summary>
    /// 已禁用
    /// </summary>
    Disabled,
    
    /// <summary>
    /// 加载错误
    /// </summary>
    Error
}

/// <summary>
/// 插件健康状态枚举
/// </summary>
public enum PluginHealthStatus
{
    /// <summary>
    /// 插件正常
    /// </summary>
    Healthy,
    
    /// <summary>
    /// 插件有警告
    /// </summary>
    Warning,
    
    /// <summary>
    /// 插件有错误
    /// </summary>
    Error,
    
    /// <summary>
    /// 插件未找到
    /// </summary>
    NotFound,
    
    /// <summary>
    /// 插件依赖缺失
    /// </summary>
    MissingDependencies,
    
    /// <summary>
    /// 插件版本不兼容
    /// </summary>
    VersionIncompatible
}

/// <summary>
/// 插件状态变更事件参数
/// </summary>
public class PluginStateChangedEventArgs : global::System.EventArgs
{
    /// <summary>
    /// 插件ID
    /// </summary>
    public string PluginId { get; }
    
    /// <summary>
    /// 旧状态
    /// </summary>
    public PluginState OldState { get; }
    
    /// <summary>
    /// 新状态
    /// </summary>
    public PluginState NewState { get; }
    
    /// <summary>
    /// 错误信息（如果有）
    /// </summary>
    public string? ErrorMessage { get; }

    public PluginStateChangedEventArgs(string pluginId, PluginState oldState, PluginState newState, string? errorMessage = null)
    {
        PluginId = pluginId;
        OldState = oldState;
        NewState = newState;
        ErrorMessage = errorMessage;
    }
}
