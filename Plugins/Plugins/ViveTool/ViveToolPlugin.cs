using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Plugins.SDK;
using UniversalDeviceToolkit.Plugins.Shared;
using UniversalDeviceToolkit.Plugins.ViveTool.Resources;

namespace UniversalDeviceToolkit.Plugins.ViveTool;

[Plugin(
    id: "vive-tool",
    name: "ViVeTool",
    version: "1.2.3",
    description: "Manage Windows feature flags using ViVeTool",
    author: "SSC-STUDIO",
    MinimumHostVersion = "5.0.0",
    Icon = "Code24"
)]
public class ViveToolPlugin : UniversalDeviceToolkit.Plugins.SDK.PluginBase
{
    static ViveToolPlugin()
    {
        PluginLog.Configure(
            isTraceEnabled: () => Log.Instance.IsTraceEnabled,
            trace: (message, exception) => Log.Instance.Trace(message, exception));
    }

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

public class ViveToolPluginPage : UniversalDeviceToolkit.Plugins.SDK.IPluginPage
{
    public string PageTitle => Resource.ViveTool_PageTitle;
    public string? PageIcon => "Code24";

    public object CreatePage()
    {
        return new ViveToolPage();
    }
}

public class ViveToolSettingsPluginPage : UniversalDeviceToolkit.Plugins.SDK.IPluginPage
{
    public string PageTitle => Resource.ViveTool_BinaryPathTitle;
    public string? PageIcon => "Settings24";

    public object CreatePage()
    {
        return new ViveToolSettingsPage();
    }
}
