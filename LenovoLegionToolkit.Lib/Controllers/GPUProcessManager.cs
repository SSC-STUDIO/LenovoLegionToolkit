using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Controllers;

public interface IGPUProcessManager
{
    Task KillGPUProcessesAsync(IEnumerable<Process> processes);
}

public class GPUProcessManager : IGPUProcessManager
{
    public async Task KillGPUProcessesAsync(IEnumerable<Process> processes)
    {
        if (processes is null)
            return;

        foreach (var process in processes)
        {
            var processId = 0;
            var processName = "";
            try { processId = process.Id; processName = process.ProcessName; } catch { }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                    await process.WaitForExitAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Couldn't kill process. [pid={processId}, name={processName}]", ex);
            }
            finally
            {
                try
                {
                    process.Dispose();
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Couldn't dispose process. [pid={processId}, name={processName}]", ex);
                }
            }
        }
    }
}
