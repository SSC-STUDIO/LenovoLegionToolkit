using System;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors;

public class SensorsController(
    SensorsControllerV1 controllerV1,
    SensorsControllerV2 controllerV2,
    SensorsControllerV3 controllerV3)
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
            return _controller;

        if (await controllerV3.IsSupportedAsync().ConfigureAwait(false))
            return _controller = controllerV3;

        if (await controllerV2.IsSupportedAsync().ConfigureAwait(false))
            return _controller = controllerV2;

        if (await controllerV1.IsSupportedAsync().ConfigureAwait(false))
            return _controller = controllerV1;

        return null;
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
