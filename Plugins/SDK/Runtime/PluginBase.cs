using System.Net.Http;
using System.Threading;
using UniversalDeviceToolkit.Plugins.Core;

namespace UniversalDeviceToolkit.Plugins.SDK;

/// <summary>
/// Base class for plugins that provides default implementation
/// This is a forwarder class that inherits from the main PluginBase in Lib
/// </summary>
public abstract class PluginBase : UniversalDeviceToolkit.Lib.Plugins.PluginBase
{
    /// <summary>
    /// Gets a shared HttpClient instance for making HTTP requests.
    /// Use this instead of creating new HttpClient instances to prevent socket exhaustion.
    /// </summary>
    /// <remarks>
    /// Delegates to HttpClientManager singleton to ensure consistent HttpClient usage across all plugins.
    /// </remarks>
    protected HttpClient GetSharedHttpClient() => HttpClientManager.GetSharedClient();

    /// <summary>
    /// Gets the runtime cancellation token if the plugin has an active runtime.
    /// Returns CancellationToken.None if no runtime is active.
    /// </summary>
    /// <remarks>
    /// Override this method in derived classes to provide the actual runtime cancellation token.
    /// Base implementation returns CancellationToken.None as a safe default.
    /// </remarks>
    protected virtual CancellationToken GetRuntimeCancellationToken()
    {
        // Base implementation returns None as safe default
        // Derived classes with runtime implementations should override this
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
