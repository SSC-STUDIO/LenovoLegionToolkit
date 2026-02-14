using LenovoLegionToolkit.Lib.Optimization;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 插件基类，提供默认实现和配置支持
/// </summary>
public abstract class PluginBase : IPlugin
{
    private IPluginConfiguration? _configuration;
    
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Icon { get; }
    public abstract bool IsSystemPlugin { get; }
    public virtual string[]? Dependencies => null;

    /// <summary>
    /// 获取插件配置实例
    /// </summary>
    public IPluginConfiguration Configuration
    {
        get
        {
            return _configuration ??= new PluginConfiguration(Id);
        }
    }

    public virtual void OnInstalled()
    {
    }

    public virtual void OnUninstalled()
    {
    }

    public virtual void OnShutdown()
    {
    }

    public virtual void Stop()
    {
    }

    /// <summary>
    /// 获取功能扩展（如 IPluginPage）
    /// </summary>
    public virtual object? GetFeatureExtension()
    {
        return null;
    }

    /// <summary>
    /// 获取设置页面
    /// </summary>
    public virtual object? GetSettingsPage()
    {
        return null;
    }

    /// <summary>
    /// 获取 Windows 优化分类
    /// </summary>
    public virtual WindowsOptimizationCategoryDefinition? GetOptimizationCategory()
    {
        return null;
    }
}