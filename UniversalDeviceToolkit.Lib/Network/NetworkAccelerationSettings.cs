using LenovoLegionToolkit.Lib.Settings;

namespace LenovoLegionToolkit.Lib.Network;

public sealed class NetworkAccelerationSettings()
    : AbstractSettings<NetworkAccelerationConfig>(NetworkAccelerationDefaults.SettingsFileName)
{
    protected override NetworkAccelerationConfig Default => NetworkAccelerationConfig.CreateDefault();
}
