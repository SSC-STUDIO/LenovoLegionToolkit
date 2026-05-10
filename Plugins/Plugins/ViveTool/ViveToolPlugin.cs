using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.Shared;
using LenovoLegionToolkit.Plugins.ViveTool.Resources;

namespace LenovoLegionToolkit.Plugins.ViveTool;

[Plugin(
    id: "vive-tool",
    name: "ViVeTool",
    version: "1.2.1",
    description: "Manage Windows feature flags using ViVeTool",
    author: "SSC-STUDIO",
    MinimumHostVersion = "3.6.1",
    Icon = "Code24"
)]
public class ViveToolPlugin : LenovoLegionToolkit.Plugins.SDK.PluginBase
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

public class ViveToolPluginPage : LenovoLegionToolkit.Plugins.SDK.IPluginPage
{
    public string PageTitle => Resource.ViveTool_PageTitle;
    public string? PageIcon => "Code24";

    public object CreatePage()
    {
        return new ViveToolPage();
    }
}

public class ViveToolSettingsPluginPage : LenovoLegionToolkit.Plugins.SDK.IPluginPage
{
    public string PageTitle => Resource.ViveTool_BinaryPathTitle;
    public string? PageIcon => "Settings24";

    public object CreatePage()
    {
        return new ViveToolSettingsPage();
    }
}
