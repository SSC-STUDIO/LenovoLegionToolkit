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
