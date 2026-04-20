namespace LenovoLegionToolkit.Plugins.SDK;

/// <summary>
/// Host-level services that plugins can use without hard-coding the main application UI.
/// </summary>
public interface IPluginHostContext
{
    PluginHostMode Mode { get; }

    bool AllowSystemActions { get; }

    object? OwnerWindow { get; }

    bool OpenPluginSettings(string pluginId);

    bool ShowDialog(object dialogOrContent, string? title = null, string? icon = null);
}
