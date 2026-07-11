using System.Threading;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Network;

/// <summary>
/// Phase 1 stub: persists config and reports status. Does not spawn NetworkProxy
/// or apply system proxy/hosts — that arrives in a later phase.
/// </summary>
public sealed class NetworkAccelerationService : INetworkAccelerationService
{
    private readonly NetworkAccelerationSettings _settings;
    private readonly object _gate = new();
    private bool _isRunning;

    public NetworkAccelerationService(NetworkAccelerationSettings settings)
    {
        _settings = settings;
    }

    public NetworkAccelerationConfig Config => _settings.Store;

    public bool IsBackendReady => false;

    public bool IsRunning
    {
        get
        {
            lock (_gate)
                return _isRunning;
        }
    }

    public string StatusText
    {
        get
        {
            if (!Config.AccelerationEnabled)
                return "Off (default)";
            if (!IsBackendReady)
                return "Configured but backend not ready (Phase 1 stub)";
            return IsRunning ? "Running" : "Stopped";
        }
    }

    public Task ReloadConfigAsync(CancellationToken cancellationToken = default)
    {
        _settings.InvalidateCache();
        _ = _settings.Store;
        return Task.CompletedTask;
    }

    public Task SaveConfigAsync(CancellationToken cancellationToken = default)
    {
        // Enforce safe defaults on save: never leave mode active without explicit enable.
        if (!Config.AccelerationEnabled)
            Config.Mode = NetworkAccelerationMode.Off;

        _settings.SynchronizeStore();
        return Task.CompletedTask;
    }

    public Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        // Phase 1: refuse to start — backend worker integration is not wired from WPF yet.
        lock (_gate)
            _isRunning = false;
        return Task.FromResult(false);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
            _isRunning = false;
        return Task.CompletedTask;
    }
}
