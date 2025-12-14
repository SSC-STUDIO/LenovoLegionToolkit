using LenovoLegionToolkit.Lib.Plugins;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 系统优化插件
/// </summary>
public class SystemOptimizationPlugin : IPlugin
{
    public string Id => PluginConstants.SystemOptimization;
    public string Name => "SystemOptimization"; // 实际显示名称从资源文件获取
    public string Description => "System optimization and cleanup features"; // 实际描述从资源文件获取
    public string Icon => "Gauge24";
    public bool IsSystemPlugin => true;
    public string[]? Dependencies => null;

    public void OnInstalled()
    {
        // 插件安装时的逻辑
    }

    public void OnUninstalled()
    {
        // 插件卸载时的逻辑
    }
}

