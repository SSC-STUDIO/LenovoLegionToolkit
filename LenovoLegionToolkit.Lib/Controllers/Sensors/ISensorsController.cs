using System;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors;

/// <summary>
/// 传感器控制器接口，用于获取CPU/GPU温度、频率、风扇转速等传感器数据。
/// </summary>
/// <remarks>
/// 根据硬件版本不同，实现由SensorsControllerV1、V2或V3提供。
/// 数据采集通过WMI接口与Lenovo硬件通信。
/// </remarks>
public interface ISensorsController : IDisposable
{
    /// <summary>
    /// 检查当前设备是否支持传感器监控。
    /// </summary>
    /// <returns>如果设备支持传感器监控则返回true，否则返回false。</returns>
    Task<bool> IsSupportedAsync();

    /// <summary>
    /// 准备传感器控制器，初始化必要的资源。
    /// </summary>
    /// <returns>表示异步操作的任务。</returns>
    Task PrepareAsync();

    /// <summary>
    /// 获取传感器数据。
    /// </summary>
    /// <param name="detailed">是否获取详细数据（包括GPU功耗等）。</param>
    /// <returns>包含温度、频率、风扇转速等信息的SensorsData对象。</returns>
    Task<SensorsData> GetDataAsync(bool detailed = false);

    /// <summary>
    /// 获取CPU和GPU风扇转速。
    /// </summary>
    /// <returns>包含CPU风扇转速和GPU风扇转速的元组。</returns>
    Task<(int cpuFanSpeed, int gpuFanSpeed)> GetFanSpeedsAsync();
}
