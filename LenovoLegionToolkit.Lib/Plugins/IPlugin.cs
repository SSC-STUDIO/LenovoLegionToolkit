namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin interface, defines basic plugin information and behavior
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Plugin unique identifier
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Plugin name (for display)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Plugin icon (WPF UI Symbol or icon file path)
    /// </summary>
    string Icon { get; }

    /// <summary>
    /// Whether it's a system base plugin (base plugins cannot be uninstalled in some cases)
    /// </summary>
    bool IsSystemPlugin { get; }

    /// <summary>
    /// Whether it depends on other plugins
    /// </summary>
    string[]? Dependencies { get; }

    /// <summary>
    /// Called when the plugin is installed
    /// </summary>
    void OnInstalled();

    /// <summary>
    /// Called when the plugin is uninstalled
    /// </summary>
    void OnUninstalled();

    /// <summary>
    /// Called when the application is shutting down
    /// </summary>
    void OnShutdown();
}

