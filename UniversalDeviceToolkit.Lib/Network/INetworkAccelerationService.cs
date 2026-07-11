using System.Threading;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Network;

public interface INetworkAccelerationService
{
    NetworkAccelerationConfig Config { get; }

    bool IsBackendReady { get; }

    bool IsRunning { get; }

    string StatusText { get; }

    Task ReloadConfigAsync(CancellationToken cancellationToken = default);

    Task SaveConfigAsync(CancellationToken cancellationToken = default);

    /// <summary>Phase 1 stub: does not spawn the worker or mutate system state.</summary>
    Task<bool> StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}

public interface INetworkDiagnosticsService
{
    Task<NetworkDiagnosticsReport> RunQuickCheckAsync(CancellationToken cancellationToken = default);
}

public sealed class NetworkDiagnosticsReport
{
    public bool LoopbackReachable { get; init; } = true;
    public bool AccelerationEnabled { get; init; }
    public NetworkAccelerationMode Mode { get; init; }
    public string Summary { get; init; } = "Diagnostics placeholder (Phase 1).";
}

public interface INetworkStateRecoveryService
{
    string SnapshotPath { get; }

    Task<NetworkStateSnapshot?> LoadSnapshotAsync(CancellationToken cancellationToken = default);

    Task SaveSnapshotAsync(NetworkStateSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores system proxy / UDT hosts block / PAC path from the last snapshot.
    /// Idempotent when no snapshot exists (returns success with a skipped report).
    /// </summary>
    bool TryRestoreFromSnapshot(out string report);

    /// <summary>Captures current state into a snapshot file. Apply paths may be stubbed.</summary>
    Task<NetworkStateSnapshot> CaptureSnapshotAsync(CancellationToken cancellationToken = default);
}
