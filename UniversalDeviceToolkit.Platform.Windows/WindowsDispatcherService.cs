using System.Windows;
using System.Windows.Threading;
using UniversalDeviceToolkit.Abstractions.Platform;

namespace UniversalDeviceToolkit.Platform.Windows;

public sealed class WindowsDispatcherService : IDispatcherService
{
    public bool IsUIThread => Application.Current?.Dispatcher?.CheckAccess() ?? false;

    public Task RunOnUIThreadAsync(Action action)
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
        {
            return dispatcher.InvokeAsync(action, DispatcherPriority.Normal).Task;
        }
        action();
        return Task.CompletedTask;
    }
}
