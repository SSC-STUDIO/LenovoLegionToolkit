using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Utils;

public class MainThreadDispatcher : IMainThreadDispatcher
{
    public void Dispatch(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var dispatcher = Dispatcher.UIThread;
        if (dispatcher.CheckAccess())
        {
            callback();
            return;
        }

        dispatcher.Invoke(callback);
    }

    public Task DispatchAsync(Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return Dispatcher.UIThread.InvokeTaskAsync(callback);
    }
}
