namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Describes the current plugin host execution mode.
/// </summary>
public enum PluginHostMode
{
    /// <summary>
    /// Safe preview-oriented mode. Host-provided actions may be unavailable.
    /// </summary>
    Preview = 0,

    /// <summary>
    /// Full application runtime mode.
    /// </summary>
    RealRuntime = 1,
}
