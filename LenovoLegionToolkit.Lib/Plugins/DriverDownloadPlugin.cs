namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 驱动下载插件
/// </summary>
public class DriverDownloadPlugin : IPlugin
{
    public string Id => PluginConstants.DriverDownload;
    public string Name => "DriverDownload"; // 实际显示名称从资源文件获取
    public string Description => "Driver download and management features"; // 实际描述从资源文件获取
    public string Icon => "ArrowDownload24";
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
