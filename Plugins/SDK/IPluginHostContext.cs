namespace LenovoLegionToolkit.Plugins.SDK;

/// <summary>
/// Host-level services that plugins can use without hard-coding the main application UI.
/// </summary>
public interface IPluginHostContext
{
    public PluginHostMode Mode { get; }

    public bool AllowSystemActions { get; }

    public object? OwnerWindow { get; }

    public bool OpenPluginSettings(string pluginId);

    public bool ShowDialog(object dialogOrContent, string? title = null, string? icon = null);
}
