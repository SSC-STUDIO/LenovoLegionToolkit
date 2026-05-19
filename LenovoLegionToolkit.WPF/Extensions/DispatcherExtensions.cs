using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using LenovoLegionToolkit.Lib.Extensions;

namespace LenovoLegionToolkit.WPF.Extensions;

public static class DispatcherExtensions
{
    public static Task InvokeTaskAsync(this Dispatcher dispatcher, Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(action);

        if (dispatcher.CheckAccess())
            return action();

        return dispatcher.InvokeAsync(action).Task.Unwrap();
    }

    public static void InvokeTask(this Dispatcher dispatcher, Func<Task> action, string operationName = "dispatch UI task")
    {
        dispatcher.InvokeTaskAsync(action).Forget(operationName);
    }
}
