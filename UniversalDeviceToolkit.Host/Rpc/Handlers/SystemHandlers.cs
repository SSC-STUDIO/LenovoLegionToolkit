using System;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Host.Rpc;

namespace UniversalDeviceToolkit.Host.Rpc.Handlers;

/// <summary>
/// System-level bridge handlers: machine information and compatibility.
/// </summary>
public static class SystemHandlers
{
    public static void Register(BridgeRpcServer rpc)
    {
        rpc.RegisterHandler("system.info", async _ =>
        {
            try
            {
                var machineInformation = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
                var (isCompatible, _) = await Compatibility.IsCompatibleAsync().ConfigureAwait(false);

                return BridgeResult.Ok(new
                {
                    vendor = machineInformation.Vendor,
                    model = machineInformation.Model,
                    machineType = machineInformation.MachineType,
                    biosVersion = machineInformation.BiosVersionRaw,
                    isCompatible,
                });
            }
            catch (Exception ex)
            {
                return BridgeResult.Error(-32603, $"{ex.GetType().Name}: {ex.Message}");
            }
        });
    }
}
