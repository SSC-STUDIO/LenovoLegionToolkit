using UniversalDeviceToolkit.Lib.Plugins;

namespace UniversalDeviceToolkit.Host;

/// <summary>
/// Plugin host context for the headless host: real runtime mode with system
/// actions allowed. Owner window is null (no native window); plugin UI
/// interactions are surfaced to the Electron front end via bridge events.
/// </summary>
public sealed class HostPluginHostContext : IPluginHostContext
{
    public PluginHostMode Mode => PluginHostMode.RealRuntime;

    public bool AllowSystemActions => true;

    public object? OwnerWindow => null;

    public bool OpenPluginSettings(string pluginId) => false;

    public bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null) => null;
}
