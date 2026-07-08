using LenovoLegionToolkit.Lib.Optimization;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Plugin base class providing default implementation and configuration support
/// </summary>
public abstract class PluginBase : IPlugin
{
    private IPluginConfiguration? _configuration;

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Icon { get; }
    public abstract bool IsSystemPlugin { get; }
    public virtual string[]? Dependencies => null;

    /// <summary>
    /// Gets the current host context exposed by the active application.
    /// </summary>
    protected IPluginHostContext HostContext => PluginHostContext.Current;

    /// <summary>
    /// Gets the plugin configuration instance
    /// </summary>
    public IPluginConfiguration Configuration
    {
        get
        {
            return _configuration ??= new PluginConfiguration(Id);
        }
    }

    public virtual void OnInstalled()
    {
    }

    public virtual void OnUninstalled()
    {
    }

    public virtual void OnShutdown()
    {
    }

    public virtual void Stop()
    {
    }

    /// <summary>
    /// Gets feature extensions (e.g. IPluginPage)
    /// </summary>
    public virtual object? GetFeatureExtension()
    {
        return null;
    }

    /// <summary>
    /// Gets the settings page
    /// </summary>
    public virtual object? GetSettingsPage()
    {
        return null;
    }

    /// <summary>
    /// Gets the Windows optimization category
    /// </summary>
    public virtual WindowsOptimizationCategoryDefinition? GetOptimizationCategory()
    {
        return null;
    }
}
