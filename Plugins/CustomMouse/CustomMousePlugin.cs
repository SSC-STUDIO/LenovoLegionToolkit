using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Plugins.CustomMouse.Resources;
using LenovoLegionToolkit.Plugins.CustomMouse.Services;
using LenovoLegionToolkit.Plugins.SDK;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

/// <summary>
/// 自定义鼠标插件
/// </summary>
[Plugin(
    id: "custom-mouse",
    name: "自定义鼠标",
    version: "1.0.0",
    description: "自定义鼠标样式插件，支持智能颜色检测和多种光标主题",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "1.0.0",
    Icon = "Mouse"
)]
public class CustomMousePlugin : PluginBase
{
    public override string Id => "custom-mouse";
    public override string Name => "自定义鼠标";
    public override string Description => "自定义鼠标样式插件，支持智能颜色检测和多种光标主题";
    public override string Icon => "Mouse";
    public override bool IsSystemPlugin => false;

    /// <summary>
    /// 插件提供功能扩展
    /// </summary>
    public override object? GetFeatureExtension()
    {
        // 返回扩展模式的功能页面
        return new CustomMouseExtension();
    }

    /// <summary>
    /// 插件提供设置页面
    /// </summary>
    public override object? GetSettingsPage()
    {
        // 返回设置页面
        return new CustomMouseSettingsPage();
    }
}

/// <summary>
/// 自定义鼠标扩展功能（扩展模式）
/// </summary>
public class CustomMouseExtension : IPluginPage
{
    public string PageTitle => "鼠标样式扩展";
    public string PageIcon => "Mouse";

    public object CreatePage()
    {
        return new CustomMouseExtensionPage();
    }
}

/// <summary>
/// 自定义鼠标设置插件页面提供者
/// </summary>
public class CustomMouseSettingsPluginPage : IPluginPage
{
    public string PageTitle => "鼠标设置";
    public string PageIcon => "Settings";

    public object CreatePage()
    {
        return new CustomMouseSettingsPage();
    }
}