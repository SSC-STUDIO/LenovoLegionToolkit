using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public class NetworkAccelerationPlugin : LenovoLegionToolkit.Plugins.SDK.PluginBase
{
    public override string Id => "network-acceleration";

    public override string Name => "Network Acceleration";

    public override string Description => "Real-time network acceleration and optimization features";

    public override string Icon => "Rocket24";

    public override bool IsSystemPlugin => false;
}
