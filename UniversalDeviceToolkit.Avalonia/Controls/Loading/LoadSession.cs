using System;
using System.Threading;

namespace UniversalDeviceToolkit.Avalonia.Controls.Loading;

public sealed class LoadSession : IDisposable
{
    private readonly LoadStateCoordinator _coordinator;
    private int _isDisposed;

    internal LoadSession(LoadStateCoordinator coordinator, long generation, CancellationToken cancellationToken)
    {
        _coordinator = coordinator;
        Generation = generation;
        CancellationToken = cancellationToken;
    }

    public long Generation { get; }

    public CancellationToken CancellationToken { get; }

    public bool IsCurrent => Volatile.Read(ref _isDisposed) == 0 && _coordinator.IsCurrent(Generation);

    public void Cancel()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
            return;

        _coordinator.Cancel(Generation);
    }

    public void Dispose() => Cancel();
}
