using System;
using System.Net.Http;

namespace UniversalDeviceToolkit.Plugins.Shared;

/// <summary>
/// Manages shared HttpClient instances to prevent socket exhaustion.
/// Provides singleton HttpClient instances for all plugins to use.
/// Each shared client uses <see cref="SocketsHttpHandler"/> with
/// <see cref="SocketsHttpHandler.PooledConnectionLifetime"/> so DNS changes
/// are periodically re-resolved and stale connections are recycled,
/// following Microsoft's recommended best practice for modern .NET.
/// </summary>
public static class HttpClientManager
{
    /// <summary>
    /// Lifetime after which pooled connections are considered stale and
    /// will be closed and re-established on the next request, allowing
    /// DNS changes (load balancer failover, container migration, etc.)
    /// to be picked up within this window.
    /// </summary>
    private static readonly TimeSpan ConnectionLifetime = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Idle time after which a pooled connection is eligible for recycling.
    /// </summary>
    private static readonly TimeSpan ConnectionIdleTimeout = TimeSpan.FromMinutes(5);

    private static readonly object _sharedLock = new();
    private static readonly object _downloadLock = new();
    private static volatile HttpClient? _sharedClient;
    private static volatile HttpClient? _downloadClient;

    /// <summary>
    /// Creates a <see cref="SocketsHttpHandler"/> configured with pooled connection
    /// lifetime and idle timeout so DNS changes are periodically picked up and
    /// stale connections are recycled automatically.
    /// </summary>
    private static SocketsHttpHandler CreateHandler() => new()
    {
        PooledConnectionLifetime = ConnectionLifetime,
        PooledConnectionIdleTimeout = ConnectionIdleTimeout
    };

    /// <summary>
    /// Gets the shared HttpClient instance.
    /// This instance should be used for all HTTP requests across plugins.
    /// </summary>
    public static HttpClient GetSharedClient() =>
        _sharedClient ?? GetOrCreateShared();

    private static HttpClient GetOrCreateShared()
    {
        lock (_sharedLock)
        {
            return _sharedClient ??= new HttpClient(CreateHandler())
            {
                Timeout = TimeSpan.FromSeconds(Constants.DefaultTimeoutSeconds)
            };
        }
    }

    /// <summary>
    /// Gets the shared download HttpClient instance.
    /// This instance is cached and should be reused across all download operations.
    /// </summary>
    public static HttpClient GetDownloadClient() =>
        _downloadClient ?? GetOrCreateDownload();

    private static HttpClient GetOrCreateDownload()
    {
        lock (_downloadLock)
        {
            return _downloadClient ??= new HttpClient(CreateHandler())
            {
                Timeout = TimeSpan.FromSeconds(Constants.DownloadTimeoutSeconds)
            };
        }
    }

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

        return new HttpClient(CreateHandler())
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
        HttpClient? oldShared, oldDownload;
        lock (_sharedLock)
        {
            oldShared = _sharedClient;
            _sharedClient = null;
        }
        lock (_downloadLock)
        {
            oldDownload = _downloadClient;
            _downloadClient = null;
        }
        oldShared?.Dispose();
        oldDownload?.Dispose();
    }
}
