using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Resources;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Network;

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
                return Resource.NetworkAcceleration_Status_Off;

            if (!IsBackendReady)
                return Resource.NetworkAcceleration_Status_WorkerMissing;

            if (Config.Mode == NetworkAccelerationMode.DiagnosticsOnly)
                return Resource.NetworkAcceleration_Status_DiagnosticsOnly;

            return IsRunning
                ? string.Format(Resource.NetworkAcceleration_Status_Running, Config.Mode, Config.ListenPort)
                : string.Format(Resource.NetworkAcceleration_Status_Stopped, Config.Mode);
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

            // Hosts→127.0.0.1 without a local TLS origin breaks HTTPS. Refuse until redesigned.
            if (Config.Mode == NetworkAccelerationMode.Hosts)
            {
                Log.Instance.Warning(
                    "NetworkAcceleration Hosts mode Start refused: mapping domains to 127.0.0.1 is disabled until a local TLS origin exists. Use SystemProxy (PAC) or DiagnosticsOnly. Hosts file helpers remain for future use.");
                lock (_gate)
                    _isRunning = false;
                return false;
            }

            if (!IsBackendReady)
                return false;

            // SystemProxy: require enabled domains up front — never CreateLoopbackProxy as silent fallback.
            if (Config.Mode == NetworkAccelerationMode.SystemProxy)
            {
                var preDomains = CollectEnabledDomains();
                if (!CanApplySystemProxy(preDomains))
                {
                    Log.Instance.Warning(
                        "NetworkAcceleration SystemProxy Start refused: no enabled domains. Enable at least one domain group; refusing silent full-loopback proxy.");
                    lock (_gate)
                        _isRunning = false;
                    return false;
                }
            }

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

            var domains = CollectEnabledDomains();

            // Re-check after worker start before mutating system proxy.
            if (Config.Mode == NetworkAccelerationMode.SystemProxy)
            {
                if (!CanApplySystemProxy(domains))
                {
                    Log.Instance.Warning(
                        "NetworkAcceleration SystemProxy Start aborted after worker start: no enabled domains; worker stopped, system proxy not mutated.");
                    await _launcher.StopAsync(cancellationToken).ConfigureAwait(false);
                    lock (_gate)
                        _isRunning = false;
                    return false;
                }
            }

            // Defense-in-depth: push enabled domains so the worker rejects non-allowlisted hosts.
            // Empty list = deny-all on the host (fail closed; no open loopback forwarder).
            var rulesResult = await client.SetRulesAsync(domains, cancellationToken).ConfigureAwait(false);
            if (!rulesResult.Success)
            {
                Log.Instance.Warning(
                    $"NetworkAcceleration SetRules failed after Start; stopping worker. {rulesResult.Message}");
                try { await client.StopAsync(cancellationToken).ConfigureAwait(false); }
                catch (Exception ex)
                {
                    Log.Instance.TraceOnce(
                        "network-accel-rules-stop",
                        "Stop after SetRules failure failed.",
                        ex);
                }

                await _launcher.StopAsync(cancellationToken).ConfigureAwait(false);
                lock (_gate)
                    _isRunning = false;
                return false;
            }

            ApplySystemSideEffects(Config.ListenPort);
            _appliedSystemMutation = Config.Mode is NetworkAccelerationMode.SystemProxy;

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
                catch (Exception ex)
                {
                    Log.Instance.WarningOnce(
                        "network-accel-stop-ipc",
                        "Network acceleration IPC stop failed; falling back to process kill.",
                        ex);
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
                // Selective PAC only — never CreateLoopbackProxy when the domain list is empty.
                if (!CanApplySystemProxy(domains))
                {
                    Log.Instance.Warning(
                        "SystemProxy apply refused: no enabled domains (full loopback proxy is not used).");
                    return;
                }

                SystemProxyApplicator.Apply(SystemProxyApplicator.CreatePacProxy(port, domains.ToArray()));
                break;
            }
            case NetworkAccelerationMode.Hosts:
            {
                // Defense-in-depth: StartAsync refuses Hosts mode. Do not map domains to 127.0.0.1.
                Log.Instance.Warning(
                    "Hosts mode system apply is disabled until a local TLS origin exists; hosts file not modified.");
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

    /// <summary>
    /// Pure gate: system proxy / PAC may be applied only when at least one non-empty domain is present.
    /// Empty lists must not fall back to a full loopback system proxy.
    /// </summary>
    internal static bool CanApplySystemProxy(IEnumerable<string>? domains)
    {
        if (domains is null)
            return false;

        foreach (var d in domains)
        {
            if (!string.IsNullOrWhiteSpace(d))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Pure gate for Start eligibility by mode (no worker / no system mutation).
    /// Hosts mode is always refused until a local TLS origin exists.
    /// SystemProxy requires enabled domains. DiagnosticsOnly is always allowed.
    /// </summary>
    internal static bool CanStartMode(NetworkAccelerationMode mode, IEnumerable<string>? enabledDomains) =>
        mode switch
        {
            NetworkAccelerationMode.DiagnosticsOnly => true,
            NetworkAccelerationMode.SystemProxy => CanApplySystemProxy(enabledDomains),
            // Hosts→127.0.0.1 without local origin breaks HTTPS; helpers kept for future redesign.
            NetworkAccelerationMode.Hosts => false,
            _ => false
        };

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
