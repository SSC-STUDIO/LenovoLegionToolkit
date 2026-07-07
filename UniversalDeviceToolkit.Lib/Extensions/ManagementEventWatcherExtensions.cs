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
            catch (ManagementException)
            {
                // Ignore exceptions during cleanup
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"WMI event watcher start timed out after {timeoutMs}ms.");

            throw new TimeoutException($"WMI event watcher start timed out after {timeoutMs}ms.");
        }

        cts.Cancel();
        await startTask.ConfigureAwait(false);
    }

    public static void StartWithTimeout(this ManagementEventWatcher watcher, int timeoutMs = 2500) =>
        StartAsyncWithTimeout(watcher, timeoutMs).GetAwaiter().GetResult();
}
