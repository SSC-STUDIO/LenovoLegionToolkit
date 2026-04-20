namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Provides host-specific services that plugins can optionally use without
/// referencing a concrete host implementation.
/// </summary>
public interface IPluginHostContext
{
    /// <summary>
    /// Current host execution mode.
    /// </summary>
    PluginHostMode Mode { get; }

    /// <summary>
    /// Indicates whether the active host allows runtime-affecting plugin actions.
    /// </summary>
    bool AllowSystemActions { get; }

    /// <summary>
    /// Host owner window object, if the active host exposes one.
    /// </summary>
    object? OwnerWindow { get; }

    /// <summary>
    /// Opens the plugin settings experience hosted by the active application.
    /// </summary>
    /// <param name="pluginId">Plugin identifier.</param>
    /// <returns><c>true</c> when the host accepted the request.</returns>
    bool OpenPluginSettings(string pluginId);

    /// <summary>
    /// Shows a host-managed modal dialog for the supplied dialog object.
    /// For WPF hosts this is typically a window instance.
    /// </summary>
    /// <param name="dialogOrContent">Host-specific dialog object or content.</param>
    /// <param name="title">Optional host dialog title.</param>
    /// <param name="icon">Optional host dialog icon identifier.</param>
    /// <returns>Dialog result when available; otherwise <c>null</c>.</returns>
    bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null);
}
