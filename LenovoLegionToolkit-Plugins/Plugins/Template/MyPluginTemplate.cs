using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.Template;

/// <summary>
/// My Plugin - Main Plugin Class
/// </summary>
[Plugin(
    id: "myplugin",
    name: "My Plugin",
    version: "1.0.0",
    description: "My custom plugin for Lenovo Legion Toolkit",
    author: "Your Name",
    MinimumHostVersion = "2.0.0",
    Icon = "Apps24"
)]
public class MyPluginTemplate : PluginBase
{
    public override string Id => "myplugin";
    public override string Name => "My Plugin";
    public override string Description => "My custom plugin for Lenovo Legion Toolkit";
    public override string Icon => "Apps24";
    public override bool IsSystemPlugin => false;

    /// <summary>
    /// Get feature page provided by this plugin
    /// </summary>
    public override object? GetFeatureExtension()
    {
        return new MyPluginTemplatePage();
    }

    /// <summary>
    /// Get settings page provided by this plugin
    /// </summary>
    public override object? GetSettingsPage()
    {
        return new MyPluginTemplateSettingsPage();
    }

    /// <summary>
    /// Called when the plugin is installed
    /// </summary>
    public override void OnInstalled()
    {
        base.OnInstalled();
    }

    /// <summary>
    /// Called when the plugin is uninstalled
    /// </summary>
    public override void OnUninstalled()
    {
        base.OnUninstalled();
    }

    /// <summary>
    /// Called when the application is shutting down
    /// </summary>
    public override void OnShutdown()
    {
        base.OnShutdown();
    }

    /// <summary>
    /// Called to stop the plugin (before update/uninstall)
    /// </summary>
    public override void Stop()
    {
        base.Stop();
    }
}
