namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 插件接口，定义插件的基本信息和行为
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// 插件唯一标识符
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 插件名称（用于显示）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 插件描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 插件图标（WPF UI Symbol）
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// 是否为系统基础插件（基础插件在某些情况下无法卸载）
    /// </summary>
    bool IsSystemPlugin { get; }

    /// <summary>
    /// 是否依赖其他插件
    /// </summary>
    string[]? Dependencies { get; }

    /// <summary>
    /// 插件安装时调用
    /// </summary>
    void OnInstalled();

    /// <summary>
    /// 插件卸载时调用
    /// </summary>
    void OnUninstalled();
}

