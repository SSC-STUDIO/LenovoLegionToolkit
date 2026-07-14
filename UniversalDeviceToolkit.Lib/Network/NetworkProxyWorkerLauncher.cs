using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Network;

/// <summary>Spawns / stops the isolated NetworkProxy worker process.</summary>
public sealed class NetworkProxyWorkerLauncher : IAsyncDisposable
{
    public const string WorkerFileName = "UniversalDeviceToolkit.NetworkProxy.exe";

    private readonly object _gate = new();
    private Process? _process;
    private string? _sessionToken;
    private string? _pipeName;
    private int _listenPort;

    public bool IsWorkerAlive
    {
        get
        {
            lock (_gate)
                return _process is { HasExited: false };
        }
    }

    public string? SessionToken
    {
        get
        {
            lock (_gate)
                return _sessionToken;
        }
    }

    public string? PipeName
    {
        get
        {
            lock (_gate)
                return _pipeName;
        }
    }

    public int ListenPort
    {
        get
        {
            lock (_gate)
                return _listenPort;
        }
    }

    public static string? ResolveWorkerPath()
    {
        var candidates = new[]
        {
            Path.Combine(Folders.Program, WorkerFileName),
            Path.Combine(AppContext.BaseDirectory, WorkerFileName),
            Path.Combine(Environment.CurrentDirectory, WorkerFileName)
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    public static bool IsWorkerAvailable() => ResolveWorkerPath() is not null;

    /// <summary>
    /// Best-effort kill of orphaned NetworkProxy processes (e.g. after GUI crash).
    /// Does not touch system proxy — pair with snapshot restore.
    /// </summary>
    public static void TryKillOrphanedWorkers()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("UniversalDeviceToolkit.NetworkProxy"))
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    Log.Instance.TraceOnce(
                        "network-proxy-kill-orphan",
                        $"Failed to kill orphaned NetworkProxy process (pid={process.Id}).",
                        ex);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Log.Instance.WarningOnce(
                "network-proxy-enum-orphans",
                "Failed to enumerate orphaned NetworkProxy workers.",
                ex);
        }
    }

    public async Task EnsureStartedAsync(int listenPort, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_process is { HasExited: false })
                return;
        }

        var workerPath = ResolveWorkerPath()
            ?? throw new FileNotFoundException("NetworkProxy worker executable was not found beside the application.", WorkerFileName);

        var token = NetworkProxySessionToken.Create();
        var pipe = $"{NetworkAccelerationDefaults.DefaultPipeName}-{Guid.NewGuid():N}"[..Math.Min(80, NetworkAccelerationDefaults.DefaultPipeName.Length + 1 + 32)];
        var port = listenPort > 0 ? listenPort : NetworkAccelerationDefaults.DefaultListenPort;

        // Token goes via env (not argv) so it is not visible in process listings / WMI CommandLine.
        // Worker still accepts --token for backward compatibility.
        var startInfo = new ProcessStartInfo
        {
            FileName = workerPath,
            Arguments = $"--pipe {pipe} --port {port}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(workerPath) ?? Folders.Program
        };
        startInfo.Environment[NetworkProxySessionToken.WorkerTokenEnvironmentVariable] = token;

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("Failed to start NetworkProxy worker.");
        }

        lock (_gate)
        {
            _process?.Dispose();
            _process = process;
            _sessionToken = token;
            _pipeName = pipe;
            _listenPort = port;
        }

        // Give the IPC server a brief moment to bind the pipe.
        await Task.Delay(150, cancellationToken).ConfigureAwait(false);

        if (process.HasExited)
            throw new InvalidOperationException($"NetworkProxy worker exited early (code {process.ExitCode}).");
    }

    public NetworkProxyIpcClient CreateClient()
    {
        lock (_gate)
        {
            if (_sessionToken is null || _pipeName is null)
                throw new InvalidOperationException("Worker has not been started.");
            return new NetworkProxyIpcClient(_pipeName, _sessionToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        Process? process;
        NetworkProxyIpcClient? client = null;

        lock (_gate)
        {
            process = _process;
            if (_sessionToken is not null && _pipeName is not null)
            {
                try { client = new NetworkProxyIpcClient(_pipeName, _sessionToken); }
                catch (Exception ex)
                {
                    Log.Instance.TraceOnce("network-proxy-ipc-client", "Failed to create NetworkProxy IPC client during stop.", ex);
                    client = null;
                }
            }
        }

        if (client is not null)
        {
            try
            {
                await client.StopAsync(cancellationToken).ConfigureAwait(false);
                await client.ShutdownAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Instance.WarningOnce(
                    "network-proxy-graceful-stop",
                    "NetworkProxy graceful stop failed; falling back to process kill.",
                    ex);
            }
        }

        if (process is not null)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Log.Instance.WarningOnce(
                    "network-proxy-force-kill",
                    "Best-effort NetworkProxy process kill failed during stop.",
                    ex);
            }
            finally
            {
                process.Dispose();
            }
        }

        lock (_gate)
        {
            if (ReferenceEquals(_process, process))
                _process = null;
            _sessionToken = null;
            _pipeName = null;
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
}
