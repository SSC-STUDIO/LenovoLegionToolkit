using LenovoLegionToolkit.Lib.Plugins;

namespace LenovoLegionToolkit.Plugins.SDK;

/// <summary>
/// Base class for plugins that provides default implementation
/// </summary>
public abstract class PluginBase : IPlugin
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Icon { get; }
    public abstract bool IsSystemPlugin { get; }
    public virtual string[]? Dependencies => null;

    public virtual void OnInstalled()
    {
        // Default implementation: do nothing
    }

    public virtual void OnUninstalled()
    {
        // Default implementation: do nothing
    }

    public virtual void OnShutdown()
    {
        // Default implementation: do nothing
    }

    /// <summary>
    /// Get feature extension provided by this plugin (e.g., IPluginPage)
    /// </summary>
    /// <returns>Feature extension object, or null if not provided</returns>
    public virtual object? GetFeatureExtension()
    {
        return null;
    }

    /// <summary>
    /// Get settings page provided by this plugin (e.g., IPluginPage for settings)
    /// </summary>
    /// <returns>Settings page object, or null if not provided</returns>
    public virtual object? GetSettingsPage()
    {
        return null;
    }
}





