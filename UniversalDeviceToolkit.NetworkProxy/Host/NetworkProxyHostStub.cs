using System.Net;
using System.Net.Sockets;
using LenovoLegionToolkit.Lib.Network;

namespace UniversalDeviceToolkit.NetworkProxy.Host;

/// <summary>
/// Minimal localhost-only listener stub. Phase 1 does not implement CONNECT/MITM;
/// it only proves bind policy (loopback) and start/stop lifecycle for IPC.
/// </summary>
public sealed class NetworkProxyHostStub : IAsyncDisposable
{
    private readonly object _gate = new();
    private TcpListener? _listenerV4;
    private TcpListener? _listenerV6;
    private CancellationTokenSource? _acceptCts;
    private Task? _acceptLoop;
    private int _listenPort;

    public NetworkProxyHostStub(int listenPort)
    {
        _listenPort = listenPort > 0 ? listenPort : NetworkAccelerationDefaults.DefaultListenPort;
    }

    public bool IsRunning { get; private set; }

    public int ListenPort
    {
        get
        {
            lock (_gate)
                return _listenPort;
        }
    }

    public string StatusSummary
    {
        get
        {
            lock (_gate)
            {
                return IsRunning
                    ? $"running loopback:{_listenPort}"
                    : "stopped (default off)";
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (IsRunning)
                return Task.CompletedTask;

            // Bind loopback only — never 0.0.0.0 / ::.
            _listenerV4 = new TcpListener(IPAddress.Loopback, _listenPort);
            _listenerV4.Start();
            _listenPort = ((IPEndPoint)_listenerV4.LocalEndpoint).Port;

            try
            {
                _listenerV6 = new TcpListener(IPAddress.IPv6Loopback, _listenPort);
                _listenerV6.Start();
            }
            catch (SocketException)
            {
                // IPv6 loopback may be unavailable; IPv4 alone is acceptable for Phase 1.
                _listenerV6 = null;
            }

            _acceptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _acceptLoop = AcceptLoopAsync(_acceptCts.Token);
            IsRunning = true;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        Task? loop;
        CancellationTokenSource? cts;
        TcpListener? v4;
        TcpListener? v6;

        lock (_gate)
        {
            if (!IsRunning)
                return;

            IsRunning = false;
            loop = _acceptLoop;
            cts = _acceptCts;
            v4 = _listenerV4;
            v6 = _listenerV6;
            _acceptLoop = null;
            _acceptCts = null;
            _listenerV4 = null;
            _listenerV6 = null;
        }

        try { cts?.Cancel(); } catch { /* ignore */ }
        try { v4?.Stop(); } catch { /* ignore */ }
        try { v6?.Stop(); } catch { /* ignore */ }

        if (loop is not null)
        {
            try { await loop.ConfigureAwait(false); }
            catch { /* accept loop cancellation is expected */ }
        }

        cts?.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        // Phase 1: accept and immediately close — proves listener liveness without proxying.
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var listener = _listenerV4;
                if (listener is null)
                    break;

                using var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                client.Close();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (SocketException)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
            }
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
}
