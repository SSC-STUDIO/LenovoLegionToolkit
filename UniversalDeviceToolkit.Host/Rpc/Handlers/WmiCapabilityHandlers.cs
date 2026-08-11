using System;
using System.Text.Json;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.System.Management;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// WMI capability bridge: raw Lenovo feature value reads/writes that are not
/// covered by the generic feature registry. Mirrors SettingsPowerControl
/// (GodModeFnQSwitchable probe + get/set via WMI.LenovoOtherMethod).
/// </summary>
public static class WmiCapabilityHandlers
{
    public static void Register(BridgeRpcServer rpc)
    {
        rpc.RegisterHandler("wmi.getGodModeFnQ", async _ =>
        {
            try
            {
                var machineInformation = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
                var supported = machineInformation.Features[CapabilityID.GodModeFnQSwitchable];
                bool? enabled = null;

                if (supported)
                {
                    // GetFeatureValueAsync never throws; -1 means the value is unavailable.
                    var value = await WMI.LenovoOtherMethod.GetFeatureValueAsync(CapabilityID.GodModeFnQSwitchable).ConfigureAwait(false);
                    if (value == -1)
                    {
                        supported = false;
                    }
                    else
                    {
                        enabled = value == 1;
                    }
                }

                return BridgeResult.Ok(new { supported, enabled });
            }
            catch (Exception)
            {
                // WPF hides the card when the probe/read fails; mirror that.
                return BridgeResult.Ok(new { supported = false, enabled = (bool?)null });
            }
        });

        rpc.RegisterHandler("wmi.setGodModeFnQ", async request =>
        {
            try
            {
                if (!request.Parameters.TryGetProperty("enabled", out var enabledProp) ||
                    (enabledProp.ValueKind != JsonValueKind.True && enabledProp.ValueKind != JsonValueKind.False))
                    throw new BridgeErrorException(-32602, "Missing boolean parameter 'enabled'.");

                var enabled = enabledProp.GetBoolean();
                await WMI.LenovoOtherMethod.SetFeatureValueAsync(CapabilityID.GodModeFnQSwitchable, enabled ? 1 : 0).ConfigureAwait(false);
                return BridgeResult.Ok(new { ok = true });
            }
            catch (BridgeErrorException ex)
            {
                return BridgeResult.Error(ex.Code, ex.Message);
            }
            catch (Exception ex)
            {
                return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
            }
        });
    }
}
