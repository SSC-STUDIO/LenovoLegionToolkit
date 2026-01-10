using System;
using System.Threading;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Utils;

public class ThrottleLastDispatcher(TimeSpan interval, string? tag = null) : IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    public async Task DispatchAsync(Func<Task> task)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (_disposed)
            throw new ObjectDisposedException(nameof(ThrottleLastDispatcher));

        try
        {
            if (_cancellationTokenSource is not null)
                await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);

            _cancellationTokenSource = new();

            var token = _cancellationTokenSource.Token;

            await Task.Delay(interval, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            if (tag is not null && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Allowing... [tag={tag}]");

            await task().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (tag is not null && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Throttling... [tag={tag}]");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
}
