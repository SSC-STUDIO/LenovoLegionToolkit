using UniversalDeviceToolkit.Plugins.SDK;
using UniversalDeviceToolkit.Plugins.Core;
using UniversalDeviceToolkit.Plugins.ViveTool.Resources;

namespace UniversalDeviceToolkit.Plugins.ViveTool;

[Plugin(
    id: "vive-tool",
    name: "ViVeTool",
    version: "1.2.4",
    description: "Manage Windows feature flags using ViVeTool",
    author: "SSC-STUDIO",
    MinimumHostVersion = "5.0.0",
    Icon = "Code24"
)]
public class ViveToolPlugin : UniversalDeviceToolkit.Plugins.SDK.PluginBase
{
#if !UDT_PLUGIN_AVALONIA_ONLY
    static ViveToolPlugin()
    {
        PluginLog.Configure(
            isTraceEnabled: () => UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled,
            trace: (message, exception) => UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace(message, exception));
    }
#endif

    public override string Id => "vive-tool";
    public override string Name => Resource.ViveTool_PageTitle;
    public override string Description => Resource.ViveTool_PageDescription;
    public override string Icon => "Code24";
    public override bool IsSystemPlugin => false;

    public override object? GetFeatureExtension()
    {
        return new ViveToolPluginPage();
    }

    public override object? GetSettingsPage()
    {
        return new ViveToolSettingsPluginPage();
    }
}

#if UDT_PLUGIN_AVALONIA_ONLY
public class ViveToolPluginPage : UniversalDeviceToolkit.Lib.Plugins.IAvaloniaPluginPage
#else
public class ViveToolPluginPage : UniversalDeviceToolkit.Lib.Plugins.IPluginPage, UniversalDeviceToolkit.Lib.Plugins.IAvaloniaPluginPage
#endif
{
    public string PageTitle => Resource.ViveTool_PageTitle;
    public string? PageIcon => "Code24";

#if !UDT_PLUGIN_AVALONIA_ONLY
    public object CreatePage()
    {
        return new ViveToolPage();
    }
#endif

    /// <summary>
    /// Optional Avalonia factory. The legacy WPF factory above remains the
    /// default for the WPF host and preserves the plugin ABI.
    /// </summary>
    public object CreateAvaloniaPage()
    {
        return new AvaloniaViveToolPage();
    }
}

#if UDT_PLUGIN_AVALONIA_ONLY
public class ViveToolSettingsPluginPage : UniversalDeviceToolkit.Lib.Plugins.IAvaloniaPluginPage
#else
public class ViveToolSettingsPluginPage : UniversalDeviceToolkit.Lib.Plugins.IPluginPage, UniversalDeviceToolkit.Lib.Plugins.IAvaloniaPluginPage
#endif
{
    public string PageTitle => Resource.ViveTool_BinaryPathTitle;
    public string? PageIcon => "Settings24";

#if !UDT_PLUGIN_AVALONIA_ONLY
    public object CreatePage()
    {
        return new ViveToolSettingsPage();
    }
#endif

    /// <summary>
    /// Optional Avalonia factory. The WPF settings page is still returned by
    /// <see cref="CreatePage"/> for the legacy desktop host.
    /// </summary>
    public object CreateAvaloniaPage()
    {
        return new AvaloniaViveToolSettingsPage();
    }
}
