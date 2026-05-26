using System;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors;

public class SensorsController(
    SensorsControllerV1 controllerV1,
    SensorsControllerV2 controllerV2,
    SensorsControllerV3 controllerV3,
    SensorsControllerV4 controllerV4,
    SensorsControllerV5 controllerV5,
    GenericSensorsController genericController)
    : ISensorsController
{
    private ISensorsController? _controller;

    public async Task<bool> IsSupportedAsync() => Compatibility.IsSmokeLegionSimulationEnabled || await GetControllerAsync().ConfigureAwait(false) is not null;

    public async Task PrepareAsync()
    {
        if (Compatibility.IsSmokeLegionSimulationEnabled)
            return;

        var controller = await GetControllerAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("No supported controller found");
        await controller.PrepareAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        // No resources to dispose directly, injected controllers are managed by IoC
        GC.SuppressFinalize(this);
    }

    public async Task<SensorsData> GetDataAsync(bool detailed = false)
    {
        if (Compatibility.IsSmokeLegionSimulationEnabled)
            return GetSmokeSensorsData(detailed);

        var controller = await GetControllerAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("No supported controller found");
        return await controller.GetDataAsync(detailed).ConfigureAwait(false);
    }

    public async Task<(int cpuFanSpeed, int gpuFanSpeed)> GetFanSpeedsAsync()
    {
        if (Compatibility.IsSmokeLegionSimulationEnabled)
            return (2140, 1680);

        var controller = await GetControllerAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("No supported controller found");
        return await controller.GetFanSpeedsAsync().ConfigureAwait(false);
    }

    private async Task<ISensorsController?> GetControllerAsync()
    {
        if (_controller is not null)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Reusing selected sensors controller. [type={_controller.GetType().Name}]");

            return _controller;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace("Selecting sensors controller...");

        if (await ProbeControllerAsync(controllerV5).ConfigureAwait(false))
            return _controller = controllerV5;

        if (await ProbeControllerAsync(controllerV4).ConfigureAwait(false))
            return _controller = controllerV4;

        if (await ProbeControllerAsync(controllerV3).ConfigureAwait(false))
            return _controller = controllerV3;

        if (await ProbeControllerAsync(controllerV2).ConfigureAwait(false))
            return _controller = controllerV2;

        if (await ProbeControllerAsync(controllerV1).ConfigureAwait(false))
            return _controller = controllerV1;

        if (await ProbeControllerAsync(genericController).ConfigureAwait(false))
            return _controller = genericController;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace("No supported sensors controller found.");

        return null;
    }

    private static async Task<bool> ProbeControllerAsync(ISensorsController controller)
    {
        var supported = await controller.IsSupportedAsync().ConfigureAwait(false);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Sensors controller probe result: {supported}. [type={controller.GetType().Name}]");

        return supported;
    }

    private static SensorsData GetSmokeSensorsData(bool detailed)
    {
        var cpu = new SensorData(
            18,
            100,
            4300,
            5600,
            -1,
            -1,
            58,
            100,
            detailed ? 42 : -1,
            detailed ? 1.105 : 0,
            2140,
            5600).WithMinMax(
                detailed ? 0.904 : double.MaxValue,
                detailed ? 1.183 : double.MinValue,
                52,
                74);

        var gpu = new SensorData(
            7,
            100,
            480,
            2505,
            810,
            10501,
            44,
            87,
            detailed ? 18 : -1,
            detailed ? 0.721 : 0,
            1680,
            5600).WithMinMax(
                detailed ? 0.681 : double.MaxValue,
                detailed ? 0.862 : double.MinValue,
                39,
                64);

        return new SensorsData(cpu, gpu);
    }
}
