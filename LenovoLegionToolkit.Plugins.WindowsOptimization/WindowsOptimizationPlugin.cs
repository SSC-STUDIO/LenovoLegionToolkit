using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Plugins.SDK;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;

namespace LenovoLegionToolkit.Plugins.WindowsOptimization;

/// <summary>
/// 系统优化插件
/// </summary>
[Plugin(
    id: PluginConstants.SystemOptimization,
    name: "Windows Optimization",
    version: "1.0.0",
    description: "System optimization and beautification features",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "1.0.0",
    Icon = "Gauge24"
)]
public class WindowsOptimizationPlugin : PluginBase
{
    public override string Id => PluginConstants.SystemOptimization;
    public override string Name => "Windows Optimization";
    public override string Description => "System optimization and beautification features";
    public override string Icon => "Gauge24";
    public override bool IsSystemPlugin => true;

    /// <summary>
    /// 插件提供功能扩展和UI页面
    /// </summary>
    public override object? GetFeatureExtension()
    {
        // 返回系统优化页面（实现IPluginPage接口）
        return new WindowsOptimizationPluginPage();
    }
}

/// <summary>
/// 系统优化插件页面提供者
/// </summary>
public class WindowsOptimizationPluginPage : IPluginPage
{
    public string PageTitle => "Windows Optimization";
    public string PageIcon => "Gauge24";

    public object CreatePage()
    {
        // 返回系统优化页面控件
        return new WindowsOptimizationPage();
    }
}
