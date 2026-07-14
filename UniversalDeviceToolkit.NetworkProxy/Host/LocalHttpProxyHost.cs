using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UniversalDeviceToolkit.Lib.Network;

namespace UniversalDeviceToolkit.NetworkProxy.Host;

/// <summary>
/// Localhost-only HTTP proxy with CONNECT tunneling (no MITM / no TLS interception).
/// Binds exclusively to 127.0.0.1 / ::1.
/// When a non-empty domain allowlist is set, CONNECT/HTTP to non-matching hosts return 403.
/// </summary>
public sealed class LocalHttpProxyHost : INetworkProxyHost
{
    private const int HeaderBufferSize = 64 * 1024;
    private static readonly byte[] HeaderDelimiter = "\r\n\r\n"u8.ToArray();
    private static readonly byte[] ConnectOk =
        "HTTP/1.1 200 Connection Established\r\n\r\n"u8.ToArray();
    private static readonly byte[] BadRequest =
        "HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Length: 0\r\n\r\n"u8.ToArray();
    private static readonly byte[] Forbidden =
        "HTTP/1.1 403 Forbidden\r\nConnection: close\r\nContent-Length: 0\r\n\r\n"u8.ToArray();
    private static readonly byte[] BadGateway =
        "HTTP/1.1 502 Bad Gateway\r\nConnection: close\r\nContent-Length: 0\r\n\r\n"u8.ToArray();

    private readonly object _gate = new();
    private TcpListener? _listenerV4;
    private TcpListener? _listenerV6;
    private CancellationTokenSource? _acceptCts;
    private Task? _acceptLoopV4;
    private Task? _acceptLoopV6;
    private int _listenPort;
    private int _activeSessions;
    // Empty = allow all (full-proxy / before rules are pushed).
    private string[] _domainAllowlist = [];

    public LocalHttpProxyHost(int listenPort)
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
                    ? $"running loopback:{_listenPort} sessions={_activeSessions} allowlist={_domainAllowlist.Length}"
                    : "stopped (default off)";
            }
        }
    }

    /// <inheritdoc />
    public void SetDomainAllowlist(IReadOnlyList<string>? domains)
    {
        var normalized = (domains ?? Array.Empty<string>())
            .Where(static d => !string.IsNullOrWhiteSpace(d))
            .Select(DomainMatcher.Normalize)
            .Where(static d => d.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        lock (_gate)
            _domainAllowlist = normalized;
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
                _listenerV6 = null;
            }

            _acceptCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _acceptLoopV4 = AcceptLoopAsync(_listenerV4, _acceptCts.Token);
            _acceptLoopV6 = _listenerV6 is null
                ? Task.CompletedTask
                : AcceptLoopAsync(_listenerV6, _acceptCts.Token);
            IsRunning = true;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        Task? loopV4;
        Task? loopV6;
        CancellationTokenSource? cts;
        TcpListener? v4;
        TcpListener? v6;

        lock (_gate)
        {
            if (!IsRunning)
                return;

            IsRunning = false;
            loopV4 = _acceptLoopV4;
            loopV6 = _acceptLoopV6;
            cts = _acceptCts;
            v4 = _listenerV4;
            v6 = _listenerV6;
            _acceptLoopV4 = null;
            _acceptLoopV6 = null;
            _acceptCts = null;
            _listenerV4 = null;
            _listenerV6 = null;
        }

        try { cts?.Cancel(); }
        catch (ObjectDisposedException) { /* already disposed */ }

        try { v4?.Stop(); }
        catch (ObjectDisposedException) { /* already disposed */ }
        catch (SocketException ex)
        {
            Debug.WriteLine($"NetworkProxy: IPv4 listener stop: {ex.SocketErrorCode}");
        }

        try { v6?.Stop(); }
        catch (ObjectDisposedException) { /* already disposed */ }
        catch (SocketException ex)
        {
            Debug.WriteLine($"NetworkProxy: IPv6 listener stop: {ex.SocketErrorCode}");
        }

        if (loopV4 is not null)
        {
            try { await loopV4.ConfigureAwait(false); }
            catch (OperationCanceledException) { /* expected on stop */ }
            catch (ObjectDisposedException) { /* expected on stop */ }
            catch (Exception ex)
            {
                Debug.WriteLine($"NetworkProxy: accept loop V4 exit: {ex.GetType().Name}");
            }
        }

        if (loopV6 is not null)
        {
            try { await loopV6.ConfigureAwait(false); }
            catch (OperationCanceledException) { /* expected on stop */ }
            catch (ObjectDisposedException) { /* expected on stop */ }
            catch (Exception ex)
            {
                Debug.WriteLine($"NetworkProxy: accept loop V6 exit: {ex.GetType().Name}");
            }
        }

        cts?.Dispose();
    }

    private async Task AcceptLoopAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient? client = null;
            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                _ = HandleClientAsync(client, cancellationToken);
                client = null;
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
            finally
            {
                client?.Dispose();
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _activeSessions);
        try
        {
            using (client)
            {
                client.NoDelay = true;
                await using var clientStream = client.GetStream();
                await ProcessSessionAsync(clientStream, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Host stop / client abort — expected.
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"NetworkProxy: session IO: {ex.GetType().Name}: {ex.Message}");
        }
        catch (SocketException ex)
        {
            Debug.WriteLine($"NetworkProxy: session socket: {ex.SocketErrorCode}");
        }
        catch (Exception ex)
        {
            // Per-connection failures must not tear down the host.
            Debug.WriteLine($"NetworkProxy: session error: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            Interlocked.Decrement(ref _activeSessions);
        }
    }

    private bool IsHostAllowed(string host)
    {
        string[] allowlist;
        lock (_gate)
            allowlist = _domainAllowlist;

        return DomainMatcher.IsAllowed(host, allowlist);
    }

    private async Task ProcessSessionAsync(NetworkStream clientStream, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(HeaderBufferSize);
        try
        {
            var (headerLength, totalRead) = await ReadHeadersAsync(clientStream, buffer, cancellationToken)
                .ConfigureAwait(false);
            if (headerLength <= 0)
                return;

            if (!TryParseRequest(buffer.AsSpan(0, headerLength), out var method, out var target, out var hostHeader))
            {
                await clientStream.WriteAsync(BadRequest, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (string.Equals(method, "CONNECT", StringComparison.OrdinalIgnoreCase))
            {
                await HandleConnectAsync(clientStream, target, cancellationToken).ConfigureAwait(false);
                return;
            }

            var prelude = totalRead > headerLength
                ? buffer.AsMemory(headerLength, totalRead - headerLength)
                : ReadOnlyMemory<byte>.Empty;

            await HandleHttpAsync(clientStream, buffer.AsMemory(0, headerLength), prelude, method, target, hostHeader, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task HandleConnectAsync(NetworkStream clientStream, string target, CancellationToken cancellationToken)
    {
        if (!TrySplitHostPort(target, defaultPort: 443, out var host, out var port))
        {
            await clientStream.WriteAsync(BadRequest, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!IsHostAllowed(host))
        {
            Debug.WriteLine($"NetworkProxy: CONNECT denied host '{host}' (allowlist)");
            await clientStream.WriteAsync(Forbidden, cancellationToken).ConfigureAwait(false);
            return;
        }

        using var remote = new TcpClient();
        try
        {
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(TimeSpan.FromSeconds(15));
            await remote.ConnectAsync(host, port, connectCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await clientStream.WriteAsync(BadGateway, cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (SocketException ex)
        {
            Debug.WriteLine($"NetworkProxy: CONNECT {host}:{port} socket {ex.SocketErrorCode}");
            await clientStream.WriteAsync(BadGateway, cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"NetworkProxy: CONNECT {host}:{port} IO {ex.Message}");
            await clientStream.WriteAsync(BadGateway, cancellationToken).ConfigureAwait(false);
            return;
        }

        remote.NoDelay = true;
        await clientStream.WriteAsync(ConnectOk, cancellationToken).ConfigureAwait(false);
        await using var remoteStream = remote.GetStream();
        await RelayBidirectionalAsync(clientStream, remoteStream, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleHttpAsync(
        NetworkStream clientStream,
        ReadOnlyMemory<byte> headerBytes,
        ReadOnlyMemory<byte> bodyPrelude,
        string method,
        string target,
        string? hostHeader,
        CancellationToken cancellationToken)
    {
        if (!TryResolveOrigin(target, hostHeader, out var host, out var port, out var originForm))
        {
            await clientStream.WriteAsync(BadRequest, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!IsHostAllowed(host))
        {
            Debug.WriteLine($"NetworkProxy: HTTP denied host '{host}' (allowlist)");
            await clientStream.WriteAsync(Forbidden, cancellationToken).ConfigureAwait(false);
            return;
        }

        using var remote = new TcpClient();
        try
        {
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(TimeSpan.FromSeconds(15));
            await remote.ConnectAsync(host, port, connectCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await clientStream.WriteAsync(BadGateway, cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (SocketException ex)
        {
            Debug.WriteLine($"NetworkProxy: HTTP {host}:{port} socket {ex.SocketErrorCode}");
            await clientStream.WriteAsync(BadGateway, cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"NetworkProxy: HTTP {host}:{port} IO {ex.Message}");
            await clientStream.WriteAsync(BadGateway, cancellationToken).ConfigureAwait(false);
            return;
        }

        remote.NoDelay = true;
        await using var remoteStream = remote.GetStream();

        var rewritten = RewriteRequestLine(headerBytes.Span, method, originForm);
        await remoteStream.WriteAsync(rewritten, cancellationToken).ConfigureAwait(false);
        if (!bodyPrelude.IsEmpty)
            await remoteStream.WriteAsync(bodyPrelude, cancellationToken).ConfigureAwait(false);

        await RelayBidirectionalAsync(clientStream, remoteStream, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<(int HeaderLength, int TotalRead)> ReadHeadersAsync(
        NetworkStream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        var filled = 0;
        while (filled < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(filled, buffer.Length - filled), cancellationToken)
                .ConfigureAwait(false);
            if (read <= 0)
                return filled <= 0 ? (0, 0) : (-1, filled);

            filled += read;
            var index = buffer.AsSpan(0, filled).IndexOf(HeaderDelimiter);
            if (index >= 0)
                return (index + HeaderDelimiter.Length, filled);
        }

        return (-1, filled);
    }

    internal static bool TryParseRequest(ReadOnlySpan<byte> headers, out string method, out string target, out string? hostHeader)
    {
        method = string.Empty;
        target = string.Empty;
        hostHeader = null;

        var text = Encoding.ASCII.GetString(headers);
        var lineEnd = text.IndexOf("\r\n", StringComparison.Ordinal);
        if (lineEnd <= 0)
            return false;

        var requestLine = text[..lineEnd];
        var parts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false;

        method = parts[0];
        target = parts[1];

        const string hostPrefix = "\r\nHost:";
        var hostIndex = text.IndexOf(hostPrefix, StringComparison.OrdinalIgnoreCase);
        if (hostIndex >= 0)
        {
            var valueStart = hostIndex + hostPrefix.Length;
            var valueEnd = text.IndexOf("\r\n", valueStart, StringComparison.Ordinal);
            if (valueEnd < 0)
                valueEnd = text.Length;
            hostHeader = text[valueStart..valueEnd].Trim();
        }

        return !string.IsNullOrWhiteSpace(method) && !string.IsNullOrWhiteSpace(target);
    }

    internal static bool TrySplitHostPort(string authority, int defaultPort, out string host, out int port)
    {
        host = string.Empty;
        port = defaultPort;

        if (string.IsNullOrWhiteSpace(authority))
            return false;

        authority = authority.Trim();
        if (authority.StartsWith('[') && authority.Contains(']', StringComparison.Ordinal))
        {
            var close = authority.IndexOf(']');
            host = authority[1..close];
            if (close + 1 < authority.Length && authority[close + 1] == ':')
            {
                if (!int.TryParse(authority[(close + 2)..], NumberStyles.None, CultureInfo.InvariantCulture, out port))
                    return false;
            }

            return !string.IsNullOrWhiteSpace(host) && port is > 0 and <= 65535;
        }

        var colon = authority.LastIndexOf(':');
        if (colon > 0 && authority.IndexOf(':') == colon)
        {
            host = authority[..colon];
            if (!int.TryParse(authority[(colon + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out port))
                return false;
        }
        else
        {
            host = authority;
        }

        return !string.IsNullOrWhiteSpace(host) && port is > 0 and <= 65535;
    }

    internal static bool TryResolveOrigin(
        string target,
        string? hostHeader,
        out string host,
        out int port,
        out string originForm)
    {
        host = string.Empty;
        port = 80;
        originForm = "/";

        if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
                return false;

            host = uri.Host;
            port = uri.IsDefaultPort ? (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ? 443 : 80) : uri.Port;
            originForm = string.IsNullOrEmpty(uri.PathAndQuery) ? "/" : uri.PathAndQuery;
            return !string.IsNullOrWhiteSpace(host);
        }

        originForm = string.IsNullOrWhiteSpace(target) ? "/" : target;
        if (string.IsNullOrWhiteSpace(hostHeader))
            return false;

        return TrySplitHostPort(hostHeader, 80, out host, out port);
    }

    private static byte[] RewriteRequestLine(ReadOnlySpan<byte> headers, string method, string originForm)
    {
        var text = Encoding.ASCII.GetString(headers);
        var lineEnd = text.IndexOf("\r\n", StringComparison.Ordinal);
        if (lineEnd <= 0)
            return headers.ToArray();

        var rest = text[lineEnd..];
        var version = "HTTP/1.1";
        var first = text[..lineEnd].Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (first.Length >= 3)
            version = first[2];

        var rewritten = $"{method} {originForm} {version}{rest}";
        return Encoding.ASCII.GetBytes(rewritten);
    }

    private static async Task RelayBidirectionalAsync(
        NetworkStream left,
        NetworkStream right,
        CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var forward = CopyAsync(left, right, linked.Token);
        var backward = CopyAsync(right, left, linked.Token);
        var completed = await Task.WhenAny(forward, backward).ConfigureAwait(false);
        linked.Cancel();
        try { await Task.WhenAll(forward, backward).ConfigureAwait(false); }
        catch (OperationCanceledException) { /* peer close / cancel expected */ }
        catch (IOException) { /* peer close expected */ }
        catch (SocketException) { /* peer close expected */ }
        _ = completed;
    }

    private static async Task CopyAsync(NetworkStream source, NetworkStream destination, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (read <= 0)
                    break;
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);
}
