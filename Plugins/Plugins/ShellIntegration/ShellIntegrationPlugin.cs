using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

public class ShellIntegrationPlugin : LenovoLegionToolkit.Plugins.SDK.PluginBase
{
    public override string Id => "shell-integration";

    public override string Name => "Shell Integration";

    public override string Description => "Integrate Lenovo Legion Toolkit with Windows shell context menu";

    public override string Icon => "Folder24";

    public override bool IsSystemPlugin => true;
}
