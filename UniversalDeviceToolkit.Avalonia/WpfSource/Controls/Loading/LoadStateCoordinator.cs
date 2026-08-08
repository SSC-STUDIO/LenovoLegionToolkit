using System;
using System.Threading;

namespace UniversalDeviceToolkit.WPF.Controls.Loading;

public sealed class LoadStateCoordinator : IDisposable
{
    private readonly object _sync = new();
    private CancellationTokenSource? _currentCancellationTokenSource;
    private long _generation;
    private bool _disposed;

    public LoadSession BeginSession()
    {
        CancellationTokenSource? previous;
        LoadSession session;

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            previous = _currentCancellationTokenSource;
            _currentCancellationTokenSource = new CancellationTokenSource();
            session = new LoadSession(this, ++_generation, _currentCancellationTokenSource.Token);
        }

        CancelAndDispose(previous);
        return session;
    }

    public void CancelCurrent()
    {
        CancellationTokenSource? cancellationTokenSource;

        lock (_sync)
        {
            if (_disposed)
                return;

            cancellationTokenSource = _currentCancellationTokenSource;
            _currentCancellationTokenSource = null;
            _generation++;
        }

        CancelAndDispose(cancellationTokenSource);
    }

    internal bool IsCurrent(long generation)
    {
        lock (_sync)
            return !_disposed && generation == _generation && _currentCancellationTokenSource is { IsCancellationRequested: false };
    }

    internal void Cancel(long generation)
    {
        CancellationTokenSource? cancellationTokenSource;

        lock (_sync)
        {
            if (_disposed || generation != _generation)
                return;

            cancellationTokenSource = _currentCancellationTokenSource;
            _currentCancellationTokenSource = null;
            _generation++;
        }

        CancelAndDispose(cancellationTokenSource);
    }

    public void Dispose()
    {
        CancellationTokenSource? cancellationTokenSource;

        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            cancellationTokenSource = _currentCancellationTokenSource;
            _currentCancellationTokenSource = null;
            _generation++;
        }

        CancelAndDispose(cancellationTokenSource);
    }

    private static void CancelAndDispose(CancellationTokenSource? cancellationTokenSource)
    {
        if (cancellationTokenSource is null)
            return;

        try
        {
            cancellationTokenSource.Cancel();
        }
        finally
        {
            cancellationTokenSource.Dispose();
        }
    }
}
