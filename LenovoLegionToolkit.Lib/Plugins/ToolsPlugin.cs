using LenovoLegionToolkit.Lib.Plugins;

namespace LenovoLegionToolkit.Lib.Plugins;

/// <summary>
/// 工具箱插件
/// </summary>
public class ToolsPlugin : IPlugin
{
    public string Id => PluginConstants.Tools;
    public string Name => "Tools"; // 实际显示名称从资源文件获取
    public string Description => "System tools and utilities"; // 实际描述从资源文件获取
    public string Icon => "Toolbox24";
    public bool IsSystemPlugin => false;
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

