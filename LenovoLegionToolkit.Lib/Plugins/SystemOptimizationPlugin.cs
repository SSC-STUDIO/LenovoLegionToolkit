using LenovoLegionToolkit.Lib.Plugins;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// System optimization plugin
/// </summary>
public class SystemOptimizationPlugin : IPlugin
{
    public string Id => PluginConstants.SystemOptimization;
    public string Name => "SystemOptimization"; // Actual display name is retrieved from resource file
    public string Description => "System optimization and cleanup features"; // Actual description is retrieved from resource file
    public string Icon => "Gauge24";
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

