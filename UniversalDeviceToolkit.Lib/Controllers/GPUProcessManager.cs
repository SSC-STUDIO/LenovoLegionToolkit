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
    private static readonly TimeSpan GracefulShutdownTimeout = TimeSpan.FromSeconds(5);

    public async Task KillGPUProcessesAsync(IEnumerable<Process> processes)
    {
        if (processes is null)
            return;

        foreach (var process in processes)
        {
            var processId = 0;
            var processName = "";
            try { processId = process.Id; processName = process.ProcessName; }
            catch
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Failed to access process properties");
            }

            try
            {
                if (!process.HasExited)
                {
                    // Step 1: Attempt graceful shutdown via WM_CLOSE
                    process.CloseMainWindow();

                    // Step 2: Wait for graceful exit (synchronous wait is acceptable
                    // here since this runs on a background thread, not the UI thread)
                    if (!process.WaitForExit((int)GracefulShutdownTimeout.TotalMilliseconds))
                    {
                        // Step 3: Force kill the process tree if graceful shutdown timed out
                        process.Kill(true);
                        await process.WaitForExitAsync().ConfigureAwait(false);
                    }
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
