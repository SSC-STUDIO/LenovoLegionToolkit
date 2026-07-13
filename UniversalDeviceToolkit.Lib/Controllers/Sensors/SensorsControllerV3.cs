using System;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors;

public class SensorsControllerV3(GPUController gpuController) : AbstractSensorsController(gpuController)
{
    private const int CPU_SENSOR_ID = 4;
    private const int GPU_SENSOR_ID = 5;
    private const int CPU_FAN_ID = 1;
    private const int GPU_FAN_ID = 2;

    public override async Task<bool> IsSupportedAsync()
    {
        try
        {
            var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

            if (!Compatibility.IsSupportedLegionMachine(mi))
                return false;

            var result = await WMI.LenovoFanTableData.ExistsAsync(CPU_SENSOR_ID, CPU_FAN_ID).ConfigureAwait(false);
            result &= await WMI.LenovoFanTableData.ExistsAsync(GPU_SENSOR_ID, GPU_FAN_ID).ConfigureAwait(false);

            if (result)
                result = await CanReadSensorSnapshotAsync().ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error checking support. [type={GetType().Name}]", ex);

            return false;
        }
    }

    protected override async Task<int> GetCpuCurrentTemperatureAsync()
    {
        var value = await WMI.LenovoOtherMethod.GetFeatureValueAsync(CapabilityID.CpuCurrentTemperature).ConfigureAwait(false);
        return value < 1 ? -1 : value;
    }

    protected override async Task<int> GetGpuCurrentTemperatureAsync()
    {
        var value = await WMI.LenovoOtherMethod.GetFeatureValueAsync(CapabilityID.GpuCurrentTemperature).ConfigureAwait(false);
        return value < 1 ? -1 : value;
    }

    // Restore historical multi-source RPM: Fan_GetCurrentFanSpeed (V3 IDs 1/2 + legacy 0/1)
    // first — capability IDs often stick at 0 on IRX9 while fans are spinning.
    protected override Task<int> GetCpuCurrentFanSpeedAsync() =>
        ReadFanSpeedWithFallbackAsync(
            () => WMI.LenovoFanMethod.FanGetCurrentFanSpeedPreferAsync(CPU_FAN_ID, 0),
            () => WMI.LenovoOtherMethod.TryGetFeatureValueAsync(CapabilityID.CpuCurrentFanSpeed));

    protected override Task<int> GetGpuCurrentFanSpeedAsync() =>
        ReadFanSpeedWithFallbackAsync(
            () => WMI.LenovoFanMethod.FanGetCurrentFanSpeedPreferAsync(GPU_FAN_ID, 1),
            () => WMI.LenovoOtherMethod.TryGetFeatureValueAsync(CapabilityID.GpuCurrentFanSpeed));

    protected override Task<int> GetCpuMaxFanSpeedAsync() => WMI.LenovoFanMethod.GetCurrentFanMaxSpeedAsync(CPU_SENSOR_ID, CPU_FAN_ID);

    protected override Task<int> GetGpuMaxFanSpeedAsync() => WMI.LenovoFanMethod.GetCurrentFanMaxSpeedAsync(GPU_SENSOR_ID, GPU_FAN_ID);
}
