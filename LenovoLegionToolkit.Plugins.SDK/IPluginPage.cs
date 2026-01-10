namespace LenovoLegionToolkit.Plugins.SDK;

/// <summary>
/// Plugin page interface, used for plugins to provide UI pages
/// </summary>
public interface IPluginPage
{
    /// <summary>
    /// Page title
    /// </summary>
    string PageTitle { get; }

    /// <summary>
    /// Page icon (WPF UI Symbol)
    /// </summary>
    string? PageIcon { get; }

    /// <summary>
    /// Create page control
    /// </summary>
    /// <returns>UI element</returns>
    object CreatePage();
}





