using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace UniversalDeviceToolkit.WPF.Utils;

internal sealed class DebounceDispatcher
{
    private CancellationTokenSource? _cts;

    public void Debounce(int delayMs, Action action)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token).ConfigureAwait(false);
                if (!token.IsCancellationRequested)
                    Application.Current.Dispatcher.Invoke(action);
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    public void Throttle(int intervalMs, Action action)
    {
        if (_cts is { IsCancellationRequested: false })
            return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(intervalMs, token).ConfigureAwait(false);
                if (!token.IsCancellationRequested)
                    Application.Current.Dispatcher.Invoke(action);
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    public void Cancel()
    {
        _cts?.Cancel();
        _cts = null;
    }
}
