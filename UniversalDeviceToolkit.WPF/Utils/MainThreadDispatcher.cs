using System;
using System.Threading.Tasks;
using System.Windows;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Extensions;

namespace UniversalDeviceToolkit.WPF.Utils;

public class MainThreadDispatcher : IMainThreadDispatcher
{
    public void Dispatch(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var dispatcher = Application.Current?.Dispatcher ?? throw new InvalidOperationException("Application dispatcher is not available.");
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

        var dispatcher = Application.Current?.Dispatcher ?? throw new InvalidOperationException("Application dispatcher is not available.");
        return dispatcher.InvokeTaskAsync(callback);
    }
}
