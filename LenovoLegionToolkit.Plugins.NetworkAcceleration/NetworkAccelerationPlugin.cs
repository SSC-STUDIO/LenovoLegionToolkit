using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Plugins.SDK;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

/// <summary>
/// Network acceleration plugin for real-time network acceleration
/// </summary>
[Plugin(
    id: PluginConstants.NetworkAcceleration,
    name: "Network Acceleration",
    version: "1.0.0",
    description: "Real-time network acceleration and optimization features",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "1.0.0",
    Icon = "Rocket24"
)]
public class NetworkAccelerationPlugin : PluginBase
{
    public override string Id => PluginConstants.NetworkAcceleration;
    public override string Name => Resource.NetworkAcceleration_PageTitle;
    public override string Description => Resource.NetworkAcceleration_PageDescription;
    public override string Icon => "Rocket24";
    public override bool IsSystemPlugin => false; // Third-party plugin, can be uninstalled

    /// <summary>
    /// Plugin provides feature extensions and UI pages
    /// </summary>
    public override object? GetFeatureExtension()
    {
        // Return Network acceleration page (implements IPluginPage interface)
        return new NetworkAccelerationPluginPage();
    }

    /// <summary>
    /// Plugin provides settings page
    /// </summary>
    public override object? GetSettingsPage()
    {
        // Return Network acceleration settings page
        return new NetworkAccelerationSettingsPluginPage();
    }
}

/// <summary>
/// Network acceleration plugin page provider
/// </summary>
public class NetworkAccelerationPluginPage : IPluginPage
{
    // Return empty string to hide title in PluginPageWrapper, as we show it in the page content with description
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty; // No icon required

    public object CreatePage()
    {
        // Return Network acceleration page control
        return new NetworkAccelerationPage();
    }
}

/// <summary>
/// Network acceleration settings plugin page provider
/// </summary>
public class NetworkAccelerationSettingsPluginPage : IPluginPage
{
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty;

    public object CreatePage()
    {
        // Return Network acceleration settings page control
        return new NetworkAccelerationSettingsPage();
    }
}






