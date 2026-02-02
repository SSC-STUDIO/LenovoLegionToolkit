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
        foreach (var process in processes)
        {
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
                    Log.Instance.Trace($"Couldn't kill process. [pid={process.Id}, name={process.ProcessName}]", ex);
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
                        Log.Instance.Trace($"Couldn't dispose process. [pid={process.Id}, name={process.ProcessName}]", ex);
                }
            }
        }
    }
}
