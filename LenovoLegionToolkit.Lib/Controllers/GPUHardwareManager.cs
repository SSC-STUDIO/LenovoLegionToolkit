using System;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Controllers;

public interface IGPUHardwareManager
{
    Task RestartGPUAsync(string gpuInstanceId);
}

public class GPUHardwareManager : IGPUHardwareManager
{
    public async Task RestartGPUAsync(string gpuInstanceId)
    {
        if (string.IsNullOrEmpty(gpuInstanceId))
            return;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Restarting GPU device: {gpuInstanceId}");

        await CMD.RunAsync("pnputil", $"/restart-device \"{gpuInstanceId}\"").ConfigureAwait(false);
    }
}
