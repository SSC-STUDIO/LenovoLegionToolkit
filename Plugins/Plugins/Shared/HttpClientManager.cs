using System;
using System.Net.Http;
using System.Threading;

namespace LenovoLegionToolkit.Plugins.Shared;

/// <summary>
/// Manages shared HttpClient instances to prevent socket exhaustion.
/// Provides singleton HttpClient instances for all plugins to use.
/// </summary>
public static class HttpClientManager
{
    private static volatile HttpClient? _sharedClient;
    private static volatile HttpClient? _downloadClient;

    /// <summary>
    /// Gets the shared HttpClient instance.
    /// This instance should be used for all HTTP requests across plugins.
    /// </summary>
    public static HttpClient GetSharedClient() =>
        _sharedClient ??= new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Constants.DefaultTimeoutSeconds)
        };

    /// <summary>
    /// Gets the shared download HttpClient instance.
    /// This instance is cached and should be reused across all download operations.
    /// </summary>
    public static HttpClient GetDownloadClient() =>
        _downloadClient ??= new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Constants.DownloadTimeoutSeconds)
        };

    /// <summary>
    /// Creates a new HttpClient with custom timeout for specific use cases.
    /// Use sparingly - prefer GetSharedClient() or GetDownloadClient() for most scenarios.
    /// Caller is responsible for disposing the returned client.
    /// </summary>
    /// <param name="timeoutSeconds">Timeout in seconds (must be positive)</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeoutSeconds is zero or negative</exception>
    public static HttpClient CreateClientWithTimeout(int timeoutSeconds)
    {
        if (timeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "Timeout must be positive");
        }

        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
    }

    /// <summary>
    /// Disposes the shared HttpClient instances.
    /// Should be called during application shutdown to release resources.
    /// After disposal, new clients will be lazily recreated on next access.
    /// </summary>
    public static void DisposeSharedClient()
    {
        var oldShared = Interlocked.Exchange(ref _sharedClient, null);
        oldShared?.Dispose();

        var oldDownload = Interlocked.Exchange(ref _downloadClient, null);
        oldDownload?.Dispose();
    }
}
