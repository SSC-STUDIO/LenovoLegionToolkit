using System;
using System.Threading;

namespace UniversalDeviceToolkit.WPF.Utils;

internal static class CtsSwap
{
    public static CancellationTokenSource Replace(ref CancellationTokenSource? field, CancellationTokenSource? next)
    {
        var previous = Interlocked.Exchange(ref field, next);
        if (previous is not null)
        {
            try
            {
                if (!previous.IsCancellationRequested)
                    previous.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                previous.Dispose();
            }
        }
        return next!;
    }
}
