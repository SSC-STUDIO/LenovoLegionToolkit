using System;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Features.Hybrid.Notify;

public class DGPUFeatureFlagsNotify(IDelayProvider delayProvider) : AbstractDGPUNotify(delayProvider)
{
    public override async Task<bool> IsSupportedAsync()
    {
        try
        {
            var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
            return mi is { Features.Source: MachineInformation.FeatureData.SourceType.Flags, Properties.SupportsIGPUMode: true };
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce("dgpu-flags-notify-supported", "DGPUFeatureFlagsNotify support probe failed.", ex);
            return false;
        }
    }

    protected override Task NotifyDGPUStatusAsync(bool state) => WMI.LenovoOtherMethod.SetDGPUDeviceStatusAsync(state);

    protected override async Task<HardwareId> GetDGPUHardwareIdAsync()
    {
        try
        {
            return await WMI.LenovoOtherMethod.GetDGPUDeviceDIDVIDAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce("dgpu-flags-hwid", "Failed to read dGPU hardware id via feature flags.", ex);
            return HardwareId.Empty;
        }
    }
}
