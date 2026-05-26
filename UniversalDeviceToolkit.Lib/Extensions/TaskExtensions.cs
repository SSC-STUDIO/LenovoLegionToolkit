using System.Threading.Tasks;

using System;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Extensions;

public static class TaskExtensions
{
    public static ValueTask AsValueTask(this Task task) => new(task);

    public static Task<T?> OrNullIfException<T>(this Task<T> task) where T : struct
    {
        return task.ContinueWith(t => t.IsCompletedSuccessfully ? (T?)t.Result : null);
    }

    public static void Forget(this Task task, string operationName)
    {
        _ = ObserveAsync(task, operationName);
    }

    private static async Task ObserveAsync(Task task, string operationName)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Background task failed: {operationName}.", ex);
        }
    }
}
