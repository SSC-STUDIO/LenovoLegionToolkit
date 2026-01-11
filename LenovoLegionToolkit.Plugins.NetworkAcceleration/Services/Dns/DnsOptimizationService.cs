using System;
using System.Net;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using NeoSmart.AsyncLock;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Dns;

/// <summary>
/// DNS optimization service implementation
/// </summary>
public class DnsOptimizationService : IDnsOptimizationService
{
    private readonly AsyncLock _stateLock = new();
    private bool _isEnabled;
    private bool _isRunning;

    public bool IsEnabled
    {
        get
        {
            using (_stateLock.Lock())
                return _isEnabled;
        }
    }

    public void SetEnabled(bool enabled)
    {
        using (_stateLock.Lock())
        {
            _isEnabled = enabled;
        }
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"DNS optimization {(enabled ? "enabled" : "disabled")}.");
    }

    public async Task StartAsync()
    {
        bool isEnabled;
        bool isRunning;
        using (await _stateLock.LockAsync().ConfigureAwait(false))
        {
            isEnabled = _isEnabled;
            isRunning = _isRunning;
        }

        if (!isEnabled)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DNS optimization is disabled, skipping start.");
            return;
        }

        if (isRunning)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DNS optimization is already running.");
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

            using (await _stateLock.LockAsync().ConfigureAwait(false))
            {
                _isRunning = true;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DNS optimization service started.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error starting DNS optimization service: {ex.Message}", ex);
        }
    }

    public async Task StopAsync()
    {
        bool isRunning;
        using (await _stateLock.LockAsync().ConfigureAwait(false))
        {
            isRunning = _isRunning;
        }

        if (!isRunning)
        {
            return;
        }

        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Stopping DNS optimization service...");

            // DNS settings are restored automatically when service stops
            // No explicit restoration needed as we don't modify system DNS settings

            using (await _stateLock.LockAsync().ConfigureAwait(false))
            {
                _isRunning = false;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DNS optimization service stopped.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping DNS optimization service: {ex.Message}", ex);
        }
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
        bool isRunning;
        using (_stateLock.Lock())
        {
            isRunning = _isRunning;
        }

        if (isRunning)
        {
            StopAsync().GetAwaiter().GetResult();
        }
    }
}

