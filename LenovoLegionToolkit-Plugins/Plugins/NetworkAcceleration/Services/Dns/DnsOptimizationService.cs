using System;
using System.Net;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Dns;

/// <summary>
/// DNS optimization service implementation
/// </summary>
public class DnsOptimizationService : IDnsOptimizationService
{
    private bool _isEnabled;
    private bool _isRunning;

    public bool IsEnabled => _isEnabled;

    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"DNS optimization {(enabled ? "enabled" : "disabled")}.");
    }

    public async Task StartAsync()
    {
        if (!_isEnabled)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DNS optimization is disabled, skipping start.");
            await Task.CompletedTask;
            return;
        }

        if (_isRunning)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DNS optimization is already running.");
            await Task.CompletedTask;
            return;
        }

        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Starting DNS optimization service...");

            // DNS optimization is handled at the OS level
            // For Windows, we can configure DNS servers via registry or network adapter settings
            // However, changing system DNS requires admin privileges and can affect all network traffic
            // For now, we'll just mark it as running - actual DNS resolution will go through the proxy

            _isRunning = true;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DNS optimization service started.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error starting DNS optimization service: {ex.Message}", ex);
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!_isRunning)
        {
            await Task.CompletedTask;
            return;
        }

        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Stopping DNS optimization service...");

            // DNS settings are restored automatically when service stops
            // No explicit restoration needed as we don't modify system DNS settings

            _isRunning = false;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DNS optimization service stopped.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping DNS optimization service: {ex.Message}", ex);
        }

        await Task.CompletedTask;
    }

    public async Task<IPAddress[]?> ResolveAsync(string hostname)
    {
        // For now, just use system DNS
        // In the future, this could use DoH or other optimized DNS
        try
        {
            return await System.Net.Dns.GetHostAddressesAsync(hostname);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error resolving {hostname}: {ex.Message}", ex);
            return null;
        }
    }

    public void Dispose()
    {
        if (_isRunning)
        {
            // Use Task.Run to avoid deadlocks on the UI thread during shutdown
            var stopTask = Task.Run(async () => await StopAsync());
            stopTask.Wait(TimeSpan.FromSeconds(3));
        }
    }
}

