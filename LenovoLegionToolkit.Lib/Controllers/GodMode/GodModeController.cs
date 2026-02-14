using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Controllers.GodMode;

/// <summary>
/// God Mode控制器，根据硬件版本自动选择V1或V2实现。
/// </summary>
/// <remarks>
/// <para>
/// 此控制器作为门面(Facade)，根据机器信息自动选择合适的实现版本：
/// </para>
/// <list type="bullet">
///   <item><description>GodModeControllerV1 - 适用于旧款Legion设备</description></item>
///   <item><description>GodModeControllerV2 - 适用于新款Legion设备</description></item>
/// </list>
/// </remarks>
public class GodModeController(GodModeControllerV1 controllerV1, GodModeControllerV2 controllerV2)
    : IGodModeController
{
    private IGodModeController ControllerV1 => controllerV1;
    private IGodModeController ControllerV2 => controllerV2;

    public event EventHandler<Guid>? PresetChanged
    {
        add
        {
            ControllerV1.PresetChanged += value;
            ControllerV2.PresetChanged += value;
        }
        remove
        {
            ControllerV1.PresetChanged -= value;
            ControllerV2.PresetChanged -= value;
        }
    }

    public async Task<bool> NeedsVantageDisabledAsync()
    {
        var controller = await GetControllerAsync().ConfigureAwait(false);
        return await controller.NeedsVantageDisabledAsync().ConfigureAwait(false);
    }

    public async Task<bool> NeedsLegionZoneDisabledAsync()
    {
        var controller = await GetControllerAsync().ConfigureAwait(false);
        return await controller.NeedsLegionZoneDisabledAsync().ConfigureAwait(false);
    }

    public async Task<Guid> GetActivePresetIdAsync()
    {
        var controller = await GetControllerAsync().ConfigureAwait(false);
        return await controller.GetActivePresetIdAsync().ConfigureAwait(false);
    }

    public async Task<string?> GetActivePresetNameAsync()
    {
        var controller = await GetControllerAsync().ConfigureAwait(false);
        return await controller.GetActivePresetNameAsync().ConfigureAwait(false);
    }

    public async Task<GodModeState> GetStateAsync()
    {
        var controller = await GetControllerAsync().ConfigureAwait(false);
        return await controller.GetStateAsync().ConfigureAwait(false);
    }

    public async Task SetStateAsync(GodModeState state)
    {
        var controller = await GetControllerAsync().ConfigureAwait(false);
        await controller.SetStateAsync(state).ConfigureAwait(false);
    }

    public async Task ApplyStateAsync()
    {
        var controller = await GetControllerAsync().ConfigureAwait(false);
        await controller.ApplyStateAsync().ConfigureAwait(false);
    }

    public async Task<FanTable> GetDefaultFanTableAsync()
    {
        var controller = await GetControllerAsync().ConfigureAwait(false);
        return await controller.GetDefaultFanTableAsync().ConfigureAwait(false);
    }

    public async Task<FanTable> GetMinimumFanTableAsync()
    {
        var controller = await GetControllerAsync().ConfigureAwait(false);
        return await controller.GetMinimumFanTableAsync().ConfigureAwait(false);
    }

    public async Task<Dictionary<PowerModeState, GodModeDefaults>> GetDefaultsInOtherPowerModesAsync()
    {
        var controller = await GetControllerAsync().ConfigureAwait(false);
        return await controller.GetDefaultsInOtherPowerModesAsync().ConfigureAwait(false);
    }

    public async Task RestoreDefaultsInOtherPowerModeAsync(PowerModeState state)
    {
        var controller = await GetControllerAsync().ConfigureAwait(false);
        await controller.RestoreDefaultsInOtherPowerModeAsync(state).ConfigureAwait(false);
    }

    /// <summary>
    /// 检查当前设备是否支持God Mode功能。
    /// </summary>
    /// <returns>如果设备支持God Mode则返回true，否则返回false。</returns>
    public async Task<bool> IsSupportedAsync()
    {
        var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

        if (!Compatibility.IsSupportedLegionMachine(mi))
            return false;

        return mi.Properties.SupportsGodMode;
    }

    /// <summary>
    /// 根据机器信息获取合适的控制器实现。
    /// </summary>
    /// <returns>适合当前硬件的IGodModeController实现。</returns>
    /// <exception cref="InvalidOperationException">当没有找到支持的版本时抛出。</exception>
    private async Task<IGodModeController> GetControllerAsync()
    {
        var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

        if (mi.Properties.SupportsGodModeV1)
            return controllerV1;

        if (mi.Properties.SupportsGodModeV2)
            return controllerV2;

        throw new InvalidOperationException("No supported version found");
    }
}
