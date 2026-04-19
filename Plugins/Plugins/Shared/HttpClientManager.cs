using System;
using System.Net.Http;

namespace LenovoLegionToolkit.Plugins.Shared;

/// <summary>
/// Manages shared HttpClient instances to prevent socket exhaustion.
/// Provides a singleton HttpClient for all plugins to use.
/// </summary>
public static class HttpClientManager
{
    private static readonly Lazy<HttpClient> _sharedClient =
        new Lazy<HttpClient>(() => new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Constants.DefaultTimeoutSeconds)
        });

    /// <summary>
    /// Gets the shared HttpClient instance.
    /// This instance should be used for all HTTP requests across plugins.
    /// </summary>
    public static HttpClient GetSharedClient() => _sharedClient.Value;

    /// <summary>
    /// Creates a new HttpClient with custom timeout for specific use cases.
    /// Use sparingly - prefer GetSharedClient() for most scenarios.
    /// Caller is responsible for disposing the returned client.
    /// </summary>
    /// <param name="timeoutSeconds">Timeout in seconds (must be positive)</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeoutSeconds is zero or negative</exception>
    public static HttpClient CreateClientWithTimeout(int timeoutSeconds)
    {
        if (timeoutSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "Timeout must be positive");

        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
    }

    /// <summary>
    /// Disposes the shared HttpClient instance.
    /// Should be called during application shutdown to release resources.
    /// </summary>
    public static void DisposeSharedClient()
    {
        if (_sharedClient.IsValueCreated)
        {
            _sharedClient.Value.Dispose();
        }
    }
}
