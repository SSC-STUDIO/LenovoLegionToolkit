namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 垃圾清理插件
/// </summary>
public class CleanupPlugin : IPlugin
{
    public string Id => PluginConstants.Cleanup;
    public string Name => "Cleanup"; // 实际显示名称从资源文件获取
    public string Description => "System cleanup and maintenance features"; // 实际描述从资源文件获取
    public string Icon => "Delete24";
    public bool IsSystemPlugin => false;
    public string[]? Dependencies => [PluginConstants.SystemOptimization];

    public void OnInstalled()
    {
        // 插件安装时的逻辑
    }

    public void OnUninstalled()
    {
        // 插件卸载时的逻辑
    }
}
