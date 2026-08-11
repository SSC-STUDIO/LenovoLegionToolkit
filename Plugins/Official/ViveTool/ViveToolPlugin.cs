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
    static ViveToolPlugin()
    {
        PluginLog.Configure(
            isTraceEnabled: () => UniversalDeviceToolkit.Lib.Utils.Log.Instance.IsTraceEnabled,
            trace: (message, exception) => UniversalDeviceToolkit.Lib.Utils.Log.Instance.Trace(message, exception));
    }

    public override string Id => "vive-tool";
    public override string Name => Resource.ViveTool_PageTitle;
    public override string Description => Resource.ViveTool_PageDescription;
    public override string Icon => "Code24";
    public override bool IsSystemPlugin => false;

    public override object? GetFeatureExtension()
    {
        return null;
    }

    public override object? GetSettingsPage()
    {
        return null;
    }
}
