using Avalonia.Threading;
using UniversalDeviceToolkit.Abstractions.Platform;

namespace UniversalDeviceToolkit.Platform.Windows;

public sealed class WindowsDispatcherService : IDispatcherService
{
    public bool IsUIThread => Dispatcher.UIThread.CheckAccess();

    public Task RunOnUIThreadAsync(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return Dispatcher.UIThread.InvokeAsync(action).GetTask();
    }
}
