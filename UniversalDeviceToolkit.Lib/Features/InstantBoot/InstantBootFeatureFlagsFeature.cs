using System;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.System.Management;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Features.InstantBoot;

public class InstantBootFeatureFlagsFeature : IFeature<InstantBootState>
{
    private const int AC_INDEX = 5;
    private const int USB_POWER_DELIVERY_INDEX = 6;

    public async Task<bool> IsSupportedAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
            return mi.Features.Source == MachineInformation.FeatureData.SourceType.Flags && mi.Features[CapabilityID.InstantBootAc] && mi.Features[CapabilityID.InstantBootUsbPowerDelivery];
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce("feature-instantboot-supported", "InstantBoot feature flags support probe failed.", ex);
            return false;
        }
    }

    public Task<InstantBootState[]> GetAllStatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Enum.GetValues<InstantBootState>());
    }

    public async Task<InstantBootState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Getting state...");

        var flags = await WMI.LenovoOtherMethod.GetDeviceCurrentSupportFeatureAsync().ConfigureAwait(false);

        var acAdapter = flags.IsBitSet(AC_INDEX);
        var usbPowerDelivery = flags.IsBitSet(USB_POWER_DELIVERY_INDEX);

        var result = (acAdapter, usbPowerDelivery) switch
        {
            (true, true) => InstantBootState.AcAdapterAndUsbPowerDelivery,
            (true, false) => InstantBootState.AcAdapter,
            (false, true) => InstantBootState.UsbPowerDelivery,
            _ => InstantBootState.Off
        };

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"State is {result}");

        return result;
    }

    public async Task SetStateAsync(InstantBootState state, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Setting state to {state}...");

        var (acAdapter, usbPowerDelivery) = state switch
        {
            InstantBootState.AcAdapterAndUsbPowerDelivery => (1, 1),
            InstantBootState.AcAdapter => (1, 0),
            InstantBootState.UsbPowerDelivery => (0, 1),
            _ => (0, 0)
        };

        await WMI.LenovoOtherMethod.SetDeviceCurrentSupportFeatureAsync(AC_INDEX, acAdapter).ConfigureAwait(false);
        await WMI.LenovoOtherMethod.SetDeviceCurrentSupportFeatureAsync(USB_POWER_DELIVERY_INDEX, usbPowerDelivery).ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Set state to {state}");
    }

    public void InvalidateResolution()
    {
    }
}
