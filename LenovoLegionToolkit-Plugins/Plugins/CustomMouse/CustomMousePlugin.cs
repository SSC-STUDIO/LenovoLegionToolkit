using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Plugins.CustomMouse.Resources;
using LenovoLegionToolkit.Plugins.CustomMouse.Services;
using LenovoLegionToolkit.Plugins.SDK;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

/// <summary>
/// Custom Mouse Plugin - Provides custom mouse cursor themes and smart color detection
/// </summary>
[Plugin(
    id: "custom-mouse",
    name: "Resource.CustomMouse_PluginName",
    version: "1.0.0",
    description: "Resource.CustomMouse_PluginDescription",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "1.0.0",
    Icon = "Mouse"
)]
public class CustomMousePlugin : PluginBase
{
    public override string Id => "custom-mouse";
    public override string Name => Resource.CustomMouse_PluginName;
    public override string Description => Resource.CustomMouse_PluginDescription;
    public override string Icon => "Mouse";
    public override bool IsSystemPlugin => false;

    /// <summary>
    /// Plugin provides feature extension
    /// </summary>
    public override object? GetFeatureExtension()
    {
        return new CustomMouseExtension();
    }

    /// <summary>
    /// Plugin provides settings page
    /// </summary>
    public override object? GetSettingsPage()
    {
        return new CustomMouseSettingsPage();
    }
}

/// <summary>
/// Custom Mouse Extension Feature (Extension Mode)
/// </summary>
public class CustomMouseExtension : IPluginPage
{
    public string PageTitle => Resource.CustomMouse_ExtensionTitle;
    public string PageIcon => "Mouse";

    public object CreatePage()
    {
        return new CustomMouseExtensionPage();
    }
}

/// <summary>
/// Custom Mouse Settings Plugin Page Provider
/// </summary>
public class CustomMouseSettingsPluginPage : IPluginPage
{
    public string PageTitle => Resource.CustomMouse_SettingsTitle;
    public string PageIcon => "Settings";

    public object CreatePage()
    {
        return new CustomMouseSettingsPage();
    }
}