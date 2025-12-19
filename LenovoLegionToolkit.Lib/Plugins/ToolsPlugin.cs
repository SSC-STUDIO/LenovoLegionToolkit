using LenovoLegionToolkit.Lib.Plugins;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// Tools plugin
/// </summary>
public class ToolsPlugin : IPlugin
{
    public string Id => PluginConstants.Tools;
    public string Name => "Tools"; // Actual display name is retrieved from resource file
    public string Description => "System tools and utilities"; // Actual description is retrieved from resource file
    public string Icon => "Toolbox24";
    public bool IsSystemPlugin => true;
    public string[]? Dependencies => null;

    public void OnInstalled()
    {
        // Logic when plugin is installed
    }

    public void OnUninstalled()
    {
        // Logic when plugin is uninstalled
    }
}

