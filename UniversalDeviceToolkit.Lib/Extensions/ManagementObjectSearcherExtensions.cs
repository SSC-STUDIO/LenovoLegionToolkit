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
    public static Task<IEnumerable<ManagementBaseObject>> GetAsyncWithTimeout(this ManagementObjectSearcher searcher, int timeoutMs = 5000) =>
        searcher.GetAsync(timeoutMs);

    public static async Task<IEnumerable<ManagementBaseObject>> GetAsync(this ManagementObjectSearcher mos, int timeoutMs = 10000)
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

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"WMI query timed out after {timeoutMs}ms: {queryString}");

        throw new TimeoutException($"WMI query timed out after {timeoutMs}ms.");
    }
}
