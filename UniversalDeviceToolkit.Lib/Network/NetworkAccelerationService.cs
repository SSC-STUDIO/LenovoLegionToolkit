using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Network;

/// <summary>
/// Orchestrates the NetworkProxy worker, system proxy / PAC / Hosts apply, and stop/restore.
/// Default remains OFF — never auto-starts on application launch.
/// </summary>
public sealed class NetworkAccelerationService : INetworkAccelerationService, IAsyncDisposable
{
    private readonly NetworkAccelerationSettings _settings;
    private readonly INetworkStateRecoveryService _recovery;
    private readonly NetworkProxyWorkerLauncher _launcher;
    private readonly object _gate = new();
    private bool _isRunning;
    private bool _appliedSystemMutation;

    public NetworkAccelerationService(
        NetworkAccelerationSettings settings,
        INetworkStateRecoveryService recovery)
    {
        _settings = settings;
        _recovery = recovery;
        _launcher = new NetworkProxyWorkerLauncher();
    }

    public NetworkAccelerationConfig Config => _settings.Store;

    public bool IsBackendReady => NetworkProxyWorkerLauncher.IsWorkerAvailable();

    public bool IsRunning
    {
        get
        {
            lock (_gate)
                return _isRunning && _launcher.IsWorkerAlive;
        }
    }

    public string StatusText
    {
        get
        {
            if (!Config.AccelerationEnabled || Config.Mode == NetworkAccelerationMode.Off)
                return "Off (default)";

            if (!IsBackendReady)
                return "Worker binary not found — build/install UniversalDeviceToolkit.NetworkProxy.exe";

            if (Config.Mode == NetworkAccelerationMode.DiagnosticsOnly)
                return "Diagnostics only (no system network changes)";

            return IsRunning
                ? $"Running ({Config.Mode}) loopback:{Config.ListenPort}"
                : $"Stopped ({Config.Mode})";
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
        if (!Config.AccelerationEnabled)
            Config.Mode = NetworkAccelerationMode.Off;

        if (Config.ListenPort is <= 0 or > 65535)
            Config.ListenPort = NetworkAccelerationDefaults.DefaultListenPort;

        _settings.SynchronizeStore();
        return Task.CompletedTask;
    }

    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Config.AccelerationEnabled)
                return false;

            if (Config.Mode is NetworkAccelerationMode.Off)
                return false;

            if (Config.Mode == NetworkAccelerationMode.DiagnosticsOnly)
            {
                lock (_gate)
                    _isRunning = false;
                return true;
            }

            if (!IsBackendReady)
                return false;

            // Capture pre-mutation state for crash/stop recovery.
            var snapshot = await _recovery.CaptureSnapshotAsync(cancellationToken).ConfigureAwait(false);
            Config.LastRecoverySnapshot = new NetworkRecoverySnapshotMetadata
            {
                CapturedAtUtc = snapshot.CapturedAtUtc,
                SnapshotPath = _recovery.SnapshotPath,
                HadSystemProxy = snapshot.SystemProxy is not null,
                HadHostsBlock = !string.IsNullOrEmpty(snapshot.HostsMarkedBlock),
                HadPacPath = !string.IsNullOrEmpty(snapshot.PacFilePath),
                Notes = "Captured before StartAsync"
            };
            await SaveConfigAsync(cancellationToken).ConfigureAwait(false);

            await _launcher.EnsureStartedAsync(Config.ListenPort, cancellationToken).ConfigureAwait(false);

            var client = _launcher.CreateClient();
            var startResult = await client.StartAsync(cancellationToken).ConfigureAwait(false);
            if (!startResult.Success)
            {
                await _launcher.StopAsync(cancellationToken).ConfigureAwait(false);
                lock (_gate)
                    _isRunning = false;
                return false;
            }

            ApplySystemSideEffects(Config.ListenPort);
            _appliedSystemMutation = Config.Mode is NetworkAccelerationMode.SystemProxy or NetworkAccelerationMode.Hosts;

            lock (_gate)
                _isRunning = true;
            return true;
        }
        catch (Exception ex)
        {
            Log.Instance.Warning("NetworkAcceleration StartAsync failed.", ex);
            try { await _launcher.StopAsync(cancellationToken).ConfigureAwait(false); }
            catch { /* ignore */ }
            lock (_gate)
                _isRunning = false;
            return false;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_launcher.IsWorkerAlive)
            {
                try
                {
                    var client = _launcher.CreateClient();
                    await client.StopAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Fall through to process kill.
                }
            }

            await _launcher.StopAsync(cancellationToken).ConfigureAwait(false);

            if (_appliedSystemMutation)
            {
                _recovery.TryRestoreFromSnapshot(out _);
                _appliedSystemMutation = false;
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Warning("NetworkAcceleration StopAsync failed.", ex);
        }
        finally
        {
            lock (_gate)
                _isRunning = false;
        }
    }

    private void ApplySystemSideEffects(int port)
    {
        switch (Config.Mode)
        {
            case NetworkAccelerationMode.SystemProxy:
            {
                var domains = CollectEnabledDomains();
                // Prefer PAC for selective domains; fall back to full loopback proxy when empty.
                if (domains.Count > 0)
                    SystemProxyApplicator.Apply(SystemProxyApplicator.CreatePacProxy(port, domains.ToArray()));
                else
                    SystemProxyApplicator.Apply(SystemProxyApplicator.CreateLoopbackProxy(port));
                break;
            }
            case NetworkAccelerationMode.Hosts:
            {
                // Hosts rewrites require elevation for system hosts file — best-effort.
                try
                {
                    var hostsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.System),
                        "drivers", "etc", "hosts");
                    var current = File.Exists(hostsPath) ? File.ReadAllText(hostsPath) : string.Empty;
                    var lines = CollectEnabledDomains()
                        .Select(d => $"127.0.0.1 {d}")
                        .ToArray();
                    if (lines.Length == 0)
                        break;
                    var updated = HostsMarkedBlock.Upsert(current, lines);
                    File.WriteAllText(hostsPath, updated);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Log.Instance.Warning("Hosts mode requires elevation; hosts file not modified.", ex);
                }
                catch (Exception ex)
                {
                    Log.Instance.Warning("Hosts mode apply failed.", ex);
                }

                break;
            }
        }
    }

    private List<string> CollectEnabledDomains()
    {
        return (Config.DomainGroups ?? [])
            .Where(g => g.Enabled)
            .SelectMany(g => g.Domains ?? [])
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <inheritdoc />
    public Task EnsureCleanSystemStateOnStartupAsync(CancellationToken cancellationToken = default)
    {
        // Never auto-start acceleration. Only heal leftover UDT mutations from a previous crash.
        try
        {
            // Ensure default domain groups exist for older configs.
            EnsureBuiltinDomainGroups();

            // Force master switch semantics: disabled config never implies running.
            if (!Config.AccelerationEnabled)
                Config.Mode = NetworkAccelerationMode.Off;

            // If a snapshot exists and current proxy still points at loopback UDT, restore.
            // TryRestoreFromSnapshot is idempotent when clean / missing.
            if (File.Exists(_recovery.SnapshotPath) || Config.LastRecoverySnapshot is not null)
            {
                _recovery.TryRestoreFromSnapshot(out var report);
                Log.Instance.Trace($"NetworkAcceleration startup recovery: {report}");
            }

            // Kill any orphaned worker processes left from a previous GUI crash.
            NetworkProxyWorkerLauncher.TryKillOrphanedWorkers();
        }
        catch (Exception ex)
        {
            Log.Instance.Warning("NetworkAcceleration EnsureCleanSystemStateOnStartupAsync failed.", ex);
        }

        return Task.CompletedTask;
    }

    private void EnsureBuiltinDomainGroups()
    {
        Config.DomainGroups ??= [];
        if (Config.DomainGroups.Count > 0)
            return;

        Config.DomainGroups = BuiltinDomainGroups.CreateDefaults();
        try { _settings.SynchronizeStore(); }
        catch { /* non-fatal */ }
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
}
