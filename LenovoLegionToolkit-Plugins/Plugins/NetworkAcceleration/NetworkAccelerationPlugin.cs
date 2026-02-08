using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services;
using LenovoLegionToolkit.Plugins.SDK;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

/// <summary>
/// Network acceleration plugin for real-time network acceleration
/// </summary>
[Plugin(
    id: PluginConstants.NetworkAcceleration,
    name: "Network Acceleration",
    version: "1.4.0",
    description: "Real-time network acceleration and optimization features",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "1.0.0",
    Icon = "Rocket24"
)]
public class NetworkAccelerationPlugin : LenovoLegionToolkit.Plugins.SDK.PluginBase
{
    public override string Id => PluginConstants.NetworkAcceleration;
    public override string Name => Resource.NetworkAcceleration_PageTitle;
    public override string Description => Resource.NetworkAcceleration_PageDescription;
    public override string Icon => "Rocket24";
    public override bool IsSystemPlugin => false; // Third-party plugin, can be uninstalled

    /// <summary>
    /// Plugin provides feature extensions and UI pages
    /// </summary>
    public override object? GetFeatureExtension()
    {
        // Return Network acceleration page (implements IPluginPage interface)
        return new NetworkAccelerationPluginPage();
    }

    /// <summary>
    /// Plugin provides settings page
    /// </summary>
    public override object? GetSettingsPage()
    {
        // Return Network acceleration settings page
        return new NetworkAccelerationSettingsPluginPage();
    }

    /// <summary>
    /// Called when the application is shutting down
    /// </summary>
    public override void OnShutdown()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Shutting down network acceleration plugin...");

            // Get the current service instance and stop it
            var service = NetworkAccelerationService.CurrentInstance;
            if (service != null && service.IsRunning)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Stopping network acceleration service on shutdown...");

                // Use Task.Run to avoid deadlocks on the UI thread during shutdown
                // This is critical to prevent the process from hanging during exit
                var stopTask = Task.Run(async () => await service.StopAsync());
                
                // Wait with a timeout to prevent infinite hanging
                if (!stopTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Network acceleration service stop timed out after 5 seconds.");
                }
                else
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Network acceleration service stopped successfully.");
                }
            }
        }
        catch (System.Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping network acceleration service on shutdown: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Network acceleration plugin page provider
/// </summary>
public class NetworkAccelerationPluginPage : LenovoLegionToolkit.Plugins.SDK.IPluginPage
{
    // Return empty string to hide title in PluginPageWrapper, as we show it in the page content with description
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty; // No icon required

    public object CreatePage()
    {
        // Return Network acceleration page control
        return new NetworkAccelerationPage();
    }
}

/// <summary>
/// Network acceleration settings plugin page provider
/// </summary>
public class NetworkAccelerationSettingsPluginPage : LenovoLegionToolkit.Plugins.SDK.IPluginPage
{
    public string PageTitle => string.Empty;
    public string PageIcon => string.Empty;

    public object CreatePage()
    {
        // Return Network acceleration settings page control
        return new NetworkAccelerationSettingsPage();
    }
}