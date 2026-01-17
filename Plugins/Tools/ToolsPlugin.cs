using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.Tools;

[Plugin("Tools", "Tools", "工具箱插件 - 提供系统工具的统一管理", "1.0.0")]
public class ToolsPlugin : PluginBase
{
    private ToolPlugin? _toolPlugin;

    public override void OnInitialize()
    {
        base.OnInitialize();
        
        _toolPlugin = new ToolPlugin("Tools", "Tools", "工具箱插件", "1.0.0");
        _toolPlugin.OnInitialize();
    }

    public override void OnLoaded()
    {
        base.OnLoaded();
    }

    public override void OnUnloaded()
    {
        base.OnUnloaded();
    }

    public ToolPlugin? GetToolPlugin()
    {
        return _toolPlugin;
    }
}