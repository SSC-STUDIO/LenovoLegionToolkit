using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors;

public interface ISensorsController
{
    Task<bool> IsSupportedAsync();
    Task PrepareAsync();
    Task<SensorsData> GetDataAsync(bool detailed = false);
    Task<(int cpuFanSpeed, int gpuFanSpeed)> GetFanSpeedsAsync();
}
