using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.ViveTool.Resources;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;

namespace LenovoLegionToolkit.Plugins.ViveTool;

/// <summary>
/// ViVeTool plugin for managing Windows feature flags
/// </summary>
[Plugin(
    id: PluginConstants.ViveTool,
    name: "ViVeTool",
    version: "1.1.0",
    description: "Manage Windows feature flags using ViVeTool",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "1.0.0",
    Icon = "Code24"
)]
public class ViveToolPlugin : LenovoLegionToolkit.Plugins.SDK.PluginBase
{
    public override string Id => PluginConstants.ViveTool;
    public override string Name => Resource.ViveTool_PageTitle;
    public override string Description => Resource.ViveTool_PageDescription;
    public override string Icon => "Code24";
    public override bool IsSystemPlugin => false; // Third-party plugin, can be uninstalled

    /// <summary>
    /// Plugin provides feature extensions and UI pages
    /// </summary>
    public override object? GetFeatureExtension()
    {
        // Return ViVeTool page (implements IPluginPage interface)
        return new ViveToolPluginPage();
    }

    /// <summary>
    /// Plugin provides settings page
    /// </summary>
    public override object? GetSettingsPage()
    {
        // Return ViVeTool settings page
        return new ViveToolSettingsPluginPage();
    }
}

/// <summary>
/// ViVeTool plugin page provider
/// </summary>
public class ViveToolPluginPage : LenovoLegionToolkit.Plugins.SDK.IPluginPage
{
    // Return empty string to hide title in PluginPageWrapper, as we show it in the page content with description
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty; // No icon required

    public object CreatePage()
    {
        // Return ViVeTool page control
        return new ViveToolPage();
    }
}

/// <summary>
/// ViVeTool settings plugin page provider
/// </summary>
public class ViveToolSettingsPluginPage : LenovoLegionToolkit.Plugins.SDK.IPluginPage
{
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty;

    public object CreatePage()
    {
        // Return ViVeTool settings page control
        return new ViveToolSettingsPage();
    }
}
