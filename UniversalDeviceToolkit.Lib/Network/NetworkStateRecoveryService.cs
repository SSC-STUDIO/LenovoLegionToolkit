using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;
using Microsoft.Win32;

namespace UniversalDeviceToolkit.Lib.Network;

/// <summary>
/// Snapshot / restore for system proxy, UDT-marked hosts block, and PAC path.
/// Restore is implemented carefully; apply/capture of live proxy may be best-effort.
/// </summary>
public sealed class NetworkStateRecoveryService : INetworkStateRecoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _snapshotDirectory;
    private readonly Func<string> _readHosts;
    private readonly Action<string> _writeHosts;
    private readonly Func<SystemProxySnapshot?> _readSystemProxy;
    private readonly Action<SystemProxySnapshot?> _writeSystemProxy;

    public NetworkStateRecoveryService()
        : this(
            Folders.AppData,
            ReadHostsFile,
            WriteHostsFile,
            ReadSystemProxyFromRegistry,
            WriteSystemProxyToRegistry)
    {
    }

    /// <summary>Testable constructor with injectable IO.</summary>
    public NetworkStateRecoveryService(
        string snapshotDirectory,
        Func<string> readHosts,
        Action<string> writeHosts,
        Func<SystemProxySnapshot?> readSystemProxy,
        Action<SystemProxySnapshot?> writeSystemProxy)
    {
        _snapshotDirectory = snapshotDirectory ?? throw new ArgumentNullException(nameof(snapshotDirectory));
        _readHosts = readHosts ?? throw new ArgumentNullException(nameof(readHosts));
        _writeHosts = writeHosts ?? throw new ArgumentNullException(nameof(writeHosts));
        _readSystemProxy = readSystemProxy ?? throw new ArgumentNullException(nameof(readSystemProxy));
        _writeSystemProxy = writeSystemProxy ?? throw new ArgumentNullException(nameof(writeSystemProxy));
    }

    public string SnapshotPath => Path.Combine(_snapshotDirectory, NetworkAccelerationDefaults.SnapshotFileName);

    public async Task<NetworkStateSnapshot?> LoadSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(SnapshotPath))
            return null;

        await using var stream = File.OpenRead(SnapshotPath);
        return await JsonSerializer.DeserializeAsync<NetworkStateSnapshot>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveSnapshotAsync(NetworkStateSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Directory.CreateDirectory(_snapshotDirectory);
        await using var stream = File.Create(SnapshotPath);
        await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<NetworkStateSnapshot> CaptureSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var hosts = _readHosts();
        HostsMarkedBlock.TryExtract(hosts, out var block);

        var snapshot = new NetworkStateSnapshot
        {
            CapturedAtUtc = DateTimeOffset.UtcNow,
            SystemProxy = _readSystemProxy(),
            HostsMarkedBlock = block,
            PacFilePath = _readSystemProxy()?.AutoConfigUrl,
            PacFileContents = null
        };

        await SaveSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
        return snapshot;
    }

    public bool TryRestoreFromSnapshot(out string report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Network state recovery report");
        sb.AppendLine("----------------------------------------");

        NetworkStateSnapshot? snapshot = null;
        try
        {
            if (!File.Exists(SnapshotPath))
            {
                sb.AppendLine("snapshot: none (idempotent no-op).");
                sb.AppendLine("----------------------------------------");
                sb.AppendLine("Result: OK");
                report = sb.ToString();
                return true;
            }

            var json = File.ReadAllText(SnapshotPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                sb.AppendLine("snapshot: empty file (idempotent no-op).");
                sb.AppendLine("----------------------------------------");
                sb.AppendLine("Result: OK");
                report = sb.ToString();
                return true;
            }

            snapshot = JsonSerializer.Deserialize<NetworkStateSnapshot>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            sb.AppendLine($"snapshot: failed to load ({ex.GetType().Name}: {ex.Message}).");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine("Result: PARTIAL");
            report = sb.ToString();
            return false;
        }

        if (snapshot is null)
        {
            sb.AppendLine("snapshot: null after deserialize (idempotent no-op).");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine("Result: OK");
            report = sb.ToString();
            return true;
        }

        var success = true;

        try
        {
            if (snapshot.SystemProxy is null)
            {
                // No proxy recorded — clear UDT-managed proxy leftovers carefully by writing disabled state only
                // when current server points at loopback UDT port. Otherwise leave user proxy alone.
                var current = _readSystemProxy();
                if (LooksLikeUdtProxy(current))
                {
                    _writeSystemProxy(new SystemProxySnapshot { Enabled = false });
                    sb.AppendLine("system proxy: cleared UDT loopback proxy.");
                }
                else
                {
                    sb.AppendLine("system proxy: skipped (no snapshot proxy; current is not UDT loopback).");
                }
            }
            else
            {
                _writeSystemProxy(snapshot.SystemProxy);
                sb.AppendLine("system proxy: restored from snapshot.");
            }
        }
        catch (Exception ex)
        {
            success = false;
            sb.AppendLine($"system proxy: failure ({ex.GetType().Name}: {ex.Message}).");
        }

        try
        {
            var hosts = _readHosts();
            if (snapshot.HostsMarkedBlock is null)
            {
                var removed = HostsMarkedBlock.Remove(hosts);
                if (!string.Equals(removed, hosts, StringComparison.Ordinal))
                {
                    _writeHosts(removed);
                    sb.AppendLine("hosts: UDT marked block removed.");
                }
                else
                {
                    sb.AppendLine("hosts: no UDT marked block present.");
                }
            }
            else
            {
                var lines = snapshot.HostsMarkedBlock.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                var updated = HostsMarkedBlock.Upsert(hosts, lines);
                _writeHosts(updated);
                sb.AppendLine("hosts: UDT marked block restored from snapshot.");
            }
        }
        catch (Exception ex)
        {
            success = false;
            sb.AppendLine($"hosts: failure ({ex.GetType().Name}: {ex.Message}).");
        }

        if (!string.IsNullOrWhiteSpace(snapshot.PacFilePath) &&
            !string.IsNullOrWhiteSpace(snapshot.PacFileContents))
        {
            try
            {
                var pacPath = snapshot.PacFilePath!;
                var directory = Path.GetDirectoryName(pacPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(pacPath, snapshot.PacFileContents);
                sb.AppendLine($"pac: restored file at {pacPath}.");
            }
            catch (Exception ex)
            {
                success = false;
                sb.AppendLine($"pac: failure ({ex.GetType().Name}: {ex.Message}).");
            }
        }
        else
        {
            sb.AppendLine("pac: skipped (no path/contents in snapshot).");
        }

        sb.AppendLine("----------------------------------------");
        sb.AppendLine(success ? "Result: OK" : "Result: PARTIAL");
        report = sb.ToString();
        return success;
    }

    private static bool LooksLikeUdtProxy(SystemProxySnapshot? proxy)
    {
        if (proxy?.Server is null)
            return false;
        var server = proxy.Server;
        return server.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
               || server.Contains("[::1]", StringComparison.OrdinalIgnoreCase)
               || server.Contains("localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadHostsFile()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    private static void WriteHostsFile(string content)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
        File.WriteAllText(path, content);
    }

    private static SystemProxySnapshot? ReadSystemProxyFromRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
            if (key is null)
                return null;

            return new SystemProxySnapshot
            {
                Enabled = Convert.ToInt32(key.GetValue("ProxyEnable", 0)) != 0,
                Server = key.GetValue("ProxyServer") as string,
                Override = key.GetValue("ProxyOverride") as string,
                AutoConfigUrl = key.GetValue("AutoConfigURL") as string
            };
        }
        catch (Exception ex)
        {
            Log.Instance.WarningOnce(
                "system-proxy-read",
                "Failed to read system proxy settings from registry.",
                ex);
            return null;
        }
    }

    private static void WriteSystemProxyToRegistry(SystemProxySnapshot? snapshot)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings");
        if (snapshot is null)
        {
            key.SetValue("ProxyEnable", 0);
            return;
        }

        key.SetValue("ProxyEnable", snapshot.Enabled ? 1 : 0);
        if (snapshot.Server is not null)
            key.SetValue("ProxyServer", snapshot.Server);
        if (snapshot.Override is not null)
            key.SetValue("ProxyOverride", snapshot.Override);
        if (snapshot.AutoConfigUrl is not null)
            key.SetValue("AutoConfigURL", snapshot.AutoConfigUrl);
        else if (key.GetValue("AutoConfigURL") is not null && string.IsNullOrEmpty(snapshot.AutoConfigUrl))
            key.DeleteValue("AutoConfigURL", throwOnMissingValue: false);
    }
}
