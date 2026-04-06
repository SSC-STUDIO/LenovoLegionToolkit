using System;
using System.Net.Http;
using System.Threading;
using LenovoLegionToolkit.Lib.Plugins;

namespace LenovoLegionToolkit.Plugins.SDK;

/// <summary>
/// Base class for plugins that provides default implementation
/// This is a forwarder class that inherits from the main PluginBase in Lib
/// </summary>
public abstract class PluginBase : LenovoLegionToolkit.Lib.Plugins.PluginBase
{
    private static readonly Lazy<HttpClient> _sharedHttpClient =
        new Lazy<HttpClient>(() => new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        });

    /// <summary>
    /// Gets a shared HttpClient instance for making HTTP requests.
    /// Use this instead of creating new HttpClient instances to prevent socket exhaustion.
    /// </summary>
    protected HttpClient GetSharedHttpClient() => _sharedHttpClient.Value;

    /// <summary>
    /// Gets the runtime cancellation token if the plugin has an active runtime.
    /// Returns CancellationToken.None if no runtime is active.
    /// </summary>
    protected CancellationToken GetRuntimeCancellationToken()
    {
        // Access the runtime through the plugin infrastructure
        // This will be overridden by specific runtime implementations
        return CancellationToken.None;
    }

    /// <summary>
    /// Called when plugin settings are changed.
    /// Override this method to react to settings changes in derived classes.
    /// </summary>
    protected virtual void OnSettingsChanged()
    {
        // Default implementation does nothing
        // Derived classes can override to handle settings changes
    }
}

