using System.IO;
using System.Net.Http;
using System.Security.Cryptography;

namespace UniversalDeviceToolkit.Installer;

internal sealed class DownloadProgress
{
    public double? Percent { get; init; }
    public required string Status { get; init; }
}

/// <summary>
/// Downloads installer resources with optional SHA-256 or SHA-512 verification.
/// </summary>
internal static class Downloader
{
    public static async Task<string> DownloadToTempFileAsync(
        IReadOnlyList<string> urls,
        string expectedSha256,
        IProgress<DownloadProgress> progress,
        CancellationToken ct)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"udt-payload-{Guid.NewGuid():N}.zip");
        Exception? lastError = null;

        for (var i = 0; i < urls.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            progress.Report(new DownloadProgress
            {
                Status = Strings.Format("StatusDownloadMirror", i + 1, urls.Count),
            });

            try
            {
                await DownloadSingleAsync(urls[i], tempPath, progress, ct).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(expectedSha256))
                {
                    progress.Report(new DownloadProgress { Status = Strings.Get("StatusVerifying") });
                    var actual = await ComputeSha256Async(tempPath, ct).ConfigureAwait(false);
                    if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException($"SHA-256 mismatch: expected {expectedSha256}, got {actual}.");
                }

                return tempPath;
            }
            catch (OperationCanceledException)
            {
                TryDelete(tempPath);
                throw;
            }
            catch (Exception ex)
            {
                InstallerLog.Error($"Payload mirror {i + 1}/{urls.Count} failed", ex);
                lastError = ex;
                TryDelete(tempPath);
            }
        }

        throw new InvalidOperationException($"All payload mirrors failed. Last error: {lastError?.Message}", lastError);
    }

    public static async Task<string> DownloadFileAsync(
        string url,
        string destinationPath,
        CancellationToken ct,
        string? expectedSha512 = null)
    {
        try
        {
            using var client = CreateClient();
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var target = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await source.CopyToAsync(target, ct).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(expectedSha512))
            {
                var actual = await ComputeSha512Async(destinationPath, ct).ConfigureAwait(false);
                if (!actual.Equals(expectedSha512, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"SHA-512 mismatch: expected {expectedSha512}, got {actual}.");
            }

            return destinationPath;
        }
        catch
        {
            TryDelete(destinationPath);
            throw;
        }
    }

    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(30);

    private static async Task DownloadSingleAsync(
        string url,
        string destinationPath,
        IProgress<DownloadProgress> progress,
        CancellationToken ct)
    {
        using var client = CreateClient();

        // Bound the time to the first response headers — a dead mirror must fail fast.
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectCts.CancelAfter(ConnectTimeout);

        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, connectCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Connecting to '{url}' timed out after {ConnectTimeout.TotalSeconds:0} s.", ex);
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();

            var total = response.Content.Headers.ContentLength;
            using var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var target = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[256 * 1024];
            long received = 0;
            int read;
            var lastReport = Environment.TickCount64;

            // Stall watchdog: reset on every chunk; abort if the mirror stops sending.
            using var stallCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            while (true)
            {
                stallCts.CancelAfter(StallTimeout);
                try
                {
                    read = await source.ReadAsync(buffer, stallCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
                {
                    throw new TimeoutException($"Download from '{url}' stalled for {StallTimeout.TotalSeconds:0} s at {received} bytes.", ex);
                }

                if (read <= 0)
                    break;

                await target.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                received += read;

                if (Environment.TickCount64 - lastReport >= 100)
                {
                    lastReport = Environment.TickCount64;
                    double? percent = total is > 0 ? received * 100.0 / total.Value : null;
                    progress.Report(new DownloadProgress
                    {
                        Percent = percent,
                        Status = percent.HasValue
                            ? Strings.Format("StatusDownloading", percent.Value)
                            : Strings.Format("StatusDownloading", 0d),
                    });
                }
            }

            if (total is > 0 && received != total.Value)
                throw new InvalidDataException($"Truncated download: {received}/{total} bytes.");
        }
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.None,
            AllowAutoRedirect = true,
        })
        {
            Timeout = System.Threading.Timeout.InfiniteTimeSpan,
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("UniversalDeviceToolkit-Installer/1.0");
        return client;
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken ct)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = await SHA256.HashDataAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    private static async Task<string> ComputeSha512Async(string path, CancellationToken ct)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = await SHA512.HashDataAsync(stream, ct).ConfigureAwait(false);
        return Convert.ToHexString(hash);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best effort */ }
    }
}
