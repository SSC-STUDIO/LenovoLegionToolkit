using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.Lib.Network;

/// <summary>Outcome of one HTTPS connectivity probe (TCP connect + TLS handshake timings).</summary>
public sealed record ConnectivityResult(
    string Host,
    bool Success,
    long ConnectMs,
    long TlsMs,
    string? Error);

/// <summary>Probes domains for TCP/TLS reachability, used by the per-group "connectivity test".</summary>
public static class ConnectivityProbeService
{
    public static async Task<ConnectivityResult> ProbeAsync(
        string host,
        int port = 443,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            using var client = new TcpClient();
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));

            await client.ConnectAsync(host, port, connectCts.Token).ConfigureAwait(false);
            var connectMs = watch.ElapsedMilliseconds;

            using var ssl = new SslStream(client.GetStream(), leaveInnerStreamOpen: false);
            using var tlsCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            tlsCts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));

            var tlsOptions = new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = global::System.Security.Authentication.SslProtocols.None
            };
            await ssl.AuthenticateAsClientAsync(tlsOptions, tlsCts.Token).ConfigureAwait(false);
            var tlsMs = watch.ElapsedMilliseconds - connectMs;

            return new ConnectivityResult(host, true, connectMs, tlsMs, null);
        }
        catch (Exception ex)
        {
            return new ConnectivityResult(host, false, watch.ElapsedMilliseconds, 0, ex.Message);
        }
    }

    /// <summary>Probes every domain in a group, sequentially to keep UI updates simple.</summary>
    public static async Task<IReadOnlyList<ConnectivityResult>> ProbeGroupAsync(
        IEnumerable<string> domains,
        int perHostTimeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ConnectivityResult>();
        foreach (var domain in domains)
        {
            if (string.IsNullOrWhiteSpace(domain))
                continue;

            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await ProbeAsync(domain.Trim(), 443, perHostTimeoutMs, cancellationToken).ConfigureAwait(false));
        }

        return results;
    }
}
