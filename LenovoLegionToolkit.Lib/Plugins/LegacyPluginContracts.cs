using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Optimization;

namespace LenovoLegionToolkit.Lib.Plugins;

// Plugin contracts live in LenovoLegionToolkit.Lib so older plugins that still
// reference this assembly remain ABI-compatible with the current host.
public interface IPlugin
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    string Icon { get; }
    bool IsSystemPlugin { get; }
    string[]? Dependencies { get; }
    void OnInstalled();
    void OnUninstalled();
    void OnShutdown();
    void Stop();
}

public interface IPluginPage
{
    string PageTitle { get; }
    string? PageIcon { get; }
    object CreatePage();
}

public interface IPluginConfiguration
{
    T GetValue<T>(string key, T defaultValue = default!);
    void SetValue<T>(string key, T value);
    bool HasKey(string key);
    void RemoveKey(string key);
    Task SaveAsync();
    Task ReloadAsync();
    void Clear();
}

public interface IPluginHostContext
{
    PluginHostMode Mode { get; }
    bool AllowSystemActions { get; }
    object? OwnerWindow { get; }
    bool OpenPluginSettings(string pluginId);
    bool? ShowDialog(object dialogOrContent, string? title = null, string? icon = null);
}

public interface IOptimizationCategoryProvider
{
    WindowsOptimizationCategoryDefinition? GetOptimizationCategory();
}

public interface IAppStartupPlugin
{
    void OnAppStarted();
}

public abstract class PluginBase : IPlugin
{
    private IPluginConfiguration? _configuration;

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Icon { get; }
    public abstract bool IsSystemPlugin { get; }
    public virtual string[]? Dependencies => null;

    protected IPluginHostContext HostContext => PluginHostContext.Current;

    public IPluginConfiguration Configuration => _configuration ??= new PluginConfiguration(Id);

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

    public virtual object? GetFeatureExtension()
    {
        return null;
    }

    public virtual object? GetSettingsPage()
    {
        return null;
    }

    public virtual WindowsOptimizationCategoryDefinition? GetOptimizationCategory()
    {
        return null;
    }
}
