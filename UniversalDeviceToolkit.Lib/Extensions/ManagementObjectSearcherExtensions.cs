using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Extensions;

public static class ManagementObjectSearcherExtensions
{
    public static async Task<IEnumerable<ManagementBaseObject>> GetAsync(this ManagementObjectSearcher mos, int timeoutMs = 10000)
    {
        var task = Task.Run(() =>
        {
            using var collection = mos.Get();
            return collection.Cast<ManagementBaseObject>().ToArray();
        });

        using var cts = new CancellationTokenSource();
        var completedTask = await Task.WhenAny(task, Task.Delay(timeoutMs, cts.Token)).ConfigureAwait(false);
        if (completedTask == task)
        {
            cts.Cancel();
            return await task.ConfigureAwait(false);
        }

        throw new TimeoutException($"WMI query timed out after {timeoutMs}ms.");
    }
}
