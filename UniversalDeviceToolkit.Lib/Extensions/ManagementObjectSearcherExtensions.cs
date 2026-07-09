using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Extensions;

public static class ManagementObjectSearcherExtensions
{
    public static Task<IEnumerable<ManagementBaseObject>> GetAsyncWithTimeout(this ManagementObjectSearcher searcher, int timeoutMs = 2500) =>
        searcher.GetAsync(timeoutMs);

    public static async Task<IEnumerable<ManagementBaseObject>> GetAsync(this ManagementObjectSearcher mos, int timeoutMs = 2500)
    {
        var scopePath = mos.Scope?.Path?.Path ?? string.Empty;
        var queryString = mos.Query?.QueryString ?? throw new ArgumentException("Query is required.", nameof(mos));

        var task = Task.Run(() =>
        {
            using var searcher = string.IsNullOrEmpty(scopePath)
                ? new ManagementObjectSearcher(queryString)
                : new ManagementObjectSearcher(scopePath, queryString);
            using var collection = searcher.Get();
            return collection.Cast<ManagementBaseObject>().ToArray();
        });

        using var cts = new CancellationTokenSource();
        var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMs, cts.Token)).ConfigureAwait(false);
        if (completedTask == task)
        {
            cts.Cancel();
            return await task.ConfigureAwait(false);
        }

        Log.Instance.Warning($"WMI query timed out after {timeoutMs}ms: {queryString}");

        ObserveOrphanedTask(task, queryString);

        throw new TimeoutException($"WMI query timed out after {timeoutMs}ms.");
    }

    private static void ObserveOrphanedTask(Task<ManagementBaseObject[]> task, string queryString)
    {
        _ = task.ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Orphaned WMI query task faulted after timeout. [query={queryString}]", t.Exception);
            }
            else if (t.IsCompletedSuccessfully)
            {
                foreach (var obj in t.Result)
                    obj.Dispose();
            }
        }, TaskContinuationOptions.None);
    }
}
