using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Avalonia port of the WPF DebounceDispatcher: schedules an action on the UI
/// thread after a delay, cancelling any pending invocation on re-entry.
/// </summary>
internal sealed class DebounceDispatcher
{
    private CancellationTokenSource? _cts;

    public void Debounce(int delayMs, Action action)
    {
        DisposeCts();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (!token.IsCancellationRequested)
                    Dispatcher.UIThread.Post(action);
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

        DisposeCts();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(intervalMs, token);
                if (!token.IsCancellationRequested)
                    Dispatcher.UIThread.Post(action);
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    public void Cancel()
    {
        DisposeCts();
    }

    private void DisposeCts()
    {
        if (_cts is null)
            return;

        try { _cts.Cancel(); }
        catch (ObjectDisposedException) { /* already disposed */ }

        _cts.Dispose();
        _cts = null;
    }
}
