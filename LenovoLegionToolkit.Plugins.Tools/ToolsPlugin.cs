using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.Tools;

/// <summary>
/// 工具箱插件
/// </summary>
[Plugin(
    id: PluginConstants.Tools,
    name: "Tools",
    version: "1.0.0",
    description: "System tools and utilities",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "1.0.0",
    Icon = "Toolbox24"
)]
public class ToolsPlugin : PluginBase
{
    public override string Id => PluginConstants.Tools;
    public override string Name => "Tools";
    public override string Description => "System tools and utilities";
    public override string Icon => "Toolbox24";

    /// <summary>
    /// 插件提供功能扩展和UI页面
    /// </summary>
    public override object? GetFeatureExtension()
    {
        // 返回工具箱页面（实现IPluginPage接口）
        return new ToolsPluginPage();
    }
}

/// <summary>
/// 工具箱插件页面提供者
/// </summary>
public class ToolsPluginPage : IPluginPage
{
    public string PageTitle => "Tools";
    public string PageIcon => "Toolbox24";

    public object CreatePage()
    {
        // 返回工具箱页面控件
        return new ToolsPage();
    }
}

