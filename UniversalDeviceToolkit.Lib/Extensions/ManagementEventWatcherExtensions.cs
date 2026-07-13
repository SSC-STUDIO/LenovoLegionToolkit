using System;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Extensions;

public static class ManagementEventWatcherExtensions
{
    public static async Task StartAsyncWithTimeout(this ManagementEventWatcher watcher, int timeoutMs = 2500)
    {
        var startTask = Task.Run(() => watcher.Start());
        using var cts = new CancellationTokenSource();
        var completed = await Task.WhenAny(startTask, Task.Delay(timeoutMs, cts.Token)).ConfigureAwait(false);
        if (completed != startTask)
        {
            try
            {
                watcher.Dispose();
            }
            catch (ManagementException ex)
            {
                Log.Instance.TraceOnce(
                    "wmi-watcher-dispose-timeout",
                    "WMI event watcher dispose failed after start timeout.",
                    ex);
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"WMI event watcher start timed out after {timeoutMs}ms.");

            throw new TimeoutException($"WMI event watcher start timed out after {timeoutMs}ms.");
        }

        cts.Cancel();
        await startTask.ConfigureAwait(false);
    }

    public static void StartWithTimeout(this ManagementEventWatcher watcher, int timeoutMs = 2500)
    {
        var startTask = Task.Run(() => watcher.Start());
        if (!startTask.Wait(timeoutMs))
        {
            try { watcher.Dispose(); }
            catch (ManagementException ex)
            {
                Log.Instance.TraceOnce(
                    "wmi-watcher-dispose-timeout-sync",
                    "WMI event watcher dispose failed after sync start timeout.",
                    ex);
            }
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"WMI event watcher start timed out after {timeoutMs}ms.");
            throw new TimeoutException($"WMI event watcher start timed out after {timeoutMs}ms.");
        }
        if (startTask.IsFaulted)
        {
            var ex = startTask.Exception?.InnerException ?? (Exception?)startTask.Exception
                ?? new InvalidOperationException("WMI event watcher start failed.");
            global::System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
        }
    }
}
