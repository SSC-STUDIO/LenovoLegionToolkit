using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Controllers.GodMode;

/// <summary>
/// God Mode控制器接口，用于管理自定义性能模式和风扇曲线。
/// </summary>
/// <remarks>
/// God Mode允许用户自定义CPU/GPU功耗限制、风扇曲线等高级性能参数。
/// 根据硬件版本不同，实现由GodModeControllerV1或GodModeControllerV2提供。
/// </remarks>
public interface IGodModeController
{
    /// <summary>
    /// 当预设变更时触发的事件。
    /// </summary>
    /// <remarks>
    /// 事件参数为新激活的预设ID。
    /// </remarks>
    event EventHandler<Guid> PresetChanged;

    /// <summary>
    /// 检查是否需要禁用Lenovo Vantage软件。
    /// </summary>
    /// <returns>如果需要禁用Vantage则返回true，否则返回false。</returns>
    Task<bool> NeedsVantageDisabledAsync();

    /// <summary>
    /// 检查是否需要禁用Legion Zone软件。
    /// </summary>
    /// <returns>如果需要禁用Legion Zone则返回true，否则返回false。</returns>
    Task<bool> NeedsLegionZoneDisabledAsync();

    /// <summary>
    /// 获取当前激活的预设ID。
    /// </summary>
    /// <returns>当前激活预设的唯一标识符。</returns>
    Task<Guid> GetActivePresetIdAsync();

    /// <summary>
    /// 获取当前激活的预设名称。
    /// </summary>
    /// <returns>当前激活预设的名称，如果没有则返回null。</returns>
    Task<string?> GetActivePresetNameAsync();

    /// <summary>
    /// 获取当前God Mode状态。
    /// </summary>
    /// <returns>包含风扇曲线、功耗限制等参数的GodModeState对象。</returns>
    Task<GodModeState> GetStateAsync();

    /// <summary>
    /// 设置God Mode状态。
    /// </summary>
    /// <param name="state">要设置的God Mode状态。</param>
    /// <remarks>
    /// 此方法仅更新内存中的状态，需要调用<see cref="ApplyStateAsync"/>才能应用到硬件。
    /// </remarks>
    Task SetStateAsync(GodModeState state);

    /// <summary>
    /// 将当前God Mode状态应用到硬件。
    /// </summary>
    /// <returns>表示异步操作的任务。</returns>
    Task ApplyStateAsync();

    /// <summary>
    /// 获取默认风扇表。
    /// </summary>
    /// <returns>包含默认风扇转速曲线的FanTable对象。</returns>
    Task<FanTable> GetDefaultFanTableAsync();

    /// <summary>
    /// 获取最小风扇表。
    /// </summary>
    /// <returns>包含最小风扇转速曲线的FanTable对象。</returns>
    Task<FanTable> GetMinimumFanTableAsync();

    /// <summary>
    /// 获取其他电源模式的默认设置。
    /// </summary>
    /// <returns>以电源模式状态为键，默认设置为值的字典。</returns>
    Task<Dictionary<PowerModeState, GodModeDefaults>> GetDefaultsInOtherPowerModesAsync();

    /// <summary>
    /// 恢复指定电源模式的默认设置。
    /// </summary>
    /// <param name="state">要恢复默认设置的电源模式。</param>
    Task RestoreDefaultsInOtherPowerModeAsync(PowerModeState state);
}
