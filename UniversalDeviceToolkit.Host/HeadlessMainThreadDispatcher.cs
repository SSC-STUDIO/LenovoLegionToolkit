using System;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Host;

/// <summary>
/// Headless implementation of <see cref="IMainThreadDispatcher"/> for the
/// bridge host. There is no UI thread; callbacks run on the thread pool.
/// </summary>
public sealed class HeadlessMainThreadDispatcher : IMainThreadDispatcher
{
    public void Dispatch(Action callback)
        => Task.Run(callback);

    public Task DispatchAsync(Func<Task> callback)
        => Task.Run(callback);
}
