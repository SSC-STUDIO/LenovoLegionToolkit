using System;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.ReverseProxy;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Dns;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Certificate;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Hosts;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Statistics;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.SystemProxy;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services;

/// <summary>
/// Network acceleration service implementation
/// </summary>
public class NetworkAccelerationService : INetworkAccelerationService
{
    private static NetworkAccelerationService? _currentInstance;
    private static readonly object _instanceLock = new object();

    private readonly IReverseProxyService _reverseProxyService;
    private readonly IDnsOptimizationService _dnsOptimizationService;
    private readonly ICertificateManagerService _certificateManagerService;
    private readonly IHostsFileService _hostsFileService;
    private readonly INetworkStatisticsService _statisticsService;
    private readonly SystemProxyService _systemProxyService;

    private bool _isRunning;
    private string _status = string.Empty;
    private string _statusDescription = string.Empty;

    /// <summary>
    /// Gets the current instance of the service (if any)
    /// </summary>
    public static NetworkAccelerationService? CurrentInstance
    {
        get
        {
            lock (_instanceLock)
            {
                return _currentInstance;
            }
        }
    }

    public NetworkAccelerationService(
        IReverseProxyService reverseProxyService,
        IDnsOptimizationService dnsOptimizationService,
        ICertificateManagerService certificateManagerService,
        IHostsFileService hostsFileService,
        INetworkStatisticsService statisticsService)
    {
        _reverseProxyService = reverseProxyService ?? throw new ArgumentNullException(nameof(reverseProxyService));
        _dnsOptimizationService = dnsOptimizationService ?? throw new ArgumentNullException(nameof(dnsOptimizationService));
        _certificateManagerService = certificateManagerService ?? throw new ArgumentNullException(nameof(certificateManagerService));
        _hostsFileService = hostsFileService ?? throw new ArgumentNullException(nameof(hostsFileService));
        _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        _systemProxyService = new SystemProxyService();
        
        // Initialize status with localized strings
        _status = Resource.NetworkAcceleration_ServiceStatusStopped;
        _statusDescription = Resource.NetworkAcceleration_ServiceStatusStoppedDescription;
        
        // Set this instance as the current instance
        lock (_instanceLock)
        {
            _currentInstance = this;
        }
    }

    public bool IsRunning => _isRunning;

    public async Task<bool> StartAsync()
    {
        if (_isRunning)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Network acceleration service is already running.");
            return true;
        }

        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Starting network acceleration service...");

            // 1. Setup certificate if needed
            var certResult = await _certificateManagerService.SetupRootCertificateAsync();
            if (!certResult)
            {
                _status = Resource.NetworkAcceleration_Error_CertificateSetupFailed;
                _statusDescription = Resource.NetworkAcceleration_Error_CertificateSetupFailedDescription;
                return false;
            }

            // 2. Start DNS optimization if enabled
            await _dnsOptimizationService.StartAsync();

            // 3. Start reverse proxy service
            var proxyResult = await _reverseProxyService.StartAsync();
            if (!proxyResult)
            {
                _status = Resource.NetworkAcceleration_Error_ProxyServiceStartupFailed;
                _statusDescription = Resource.NetworkAcceleration_Error_ProxyServiceStartupFailedDescription;
                await _dnsOptimizationService.StopAsync();
                return false;
            }

            // 4. Configure system proxy
            var systemProxyResult = await _systemProxyService.SetSystemProxyAsync(
                _reverseProxyService.ProxyIp,
                _reverseProxyService.ProxyPort);
            if (!systemProxyResult)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Warning: Failed to configure system proxy, but continuing...");
                // Continue even if system proxy setup fails, as some features may still work
            }

            // 5. Update hosts file if needed
            await _hostsFileService.UpdateHostsAsync();

            // 6. Start statistics tracking
            _statisticsService.Start();

            _isRunning = true;
            _status = Resource.NetworkAcceleration_ServiceStatusRunning;
            _statusDescription = Resource.NetworkAcceleration_ServiceStatusRunningDescription;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Network acceleration service started successfully.");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error starting network acceleration service: {ex.Message}", ex);

            _status = Resource.NetworkAcceleration_Error_StartupFailed;
            _statusDescription = string.Format(Resource.NetworkAcceleration_Error_StartupFailedDescription, ex.Message);
            await StopAsync(); // Cleanup on error
            return false;
        }
    }

    public async Task<bool> StopAsync()
    {
        if (!_isRunning)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Network acceleration service is not running.");
            return true;
        }

        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Stopping network acceleration service...");

            // Stop services in reverse order
            _statisticsService.Stop();
            await _hostsFileService.RestoreHostsAsync();
            await _systemProxyService.RestoreSystemProxyAsync();
            await _reverseProxyService.StopAsync();
            await _dnsOptimizationService.StopAsync();

            _isRunning = false;
            _status = Resource.NetworkAcceleration_ServiceStatusStopped;
            _statusDescription = Resource.NetworkAcceleration_ServiceStatusStoppedDescription;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Network acceleration service stopped successfully.");

            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping network acceleration service: {ex.Message}", ex);

            _status = Resource.NetworkAcceleration_Error_StopFailed;
            _statusDescription = string.Format(Resource.NetworkAcceleration_Error_StopFailedDescription, ex.Message);
            
            // Even if stop failed, try to restore proxy to prevent browser connectivity issues
            try
            {
                await _systemProxyService.RestoreSystemProxyAsync();
            }
            catch
            {
                // Try force disable as last resort
                try
                {
                    await _systemProxyService.ForceDisableProxyAsync();
                }
                catch
                {
                    // Ignore errors in force disable
                }
            }
            
            return false;
        }
    }

    public string GetStatus() => _status;

    public string GetStatusDescription() => _statusDescription;

    public void Dispose()
    {
        try
        {
            // Force stop service and restore proxy when disposing
            // Use Task.Run to avoid deadlocks on the UI thread
            if (_isRunning)
            {
                var stopTask = Task.Run(async () => await StopAsync());
                stopTask.Wait(TimeSpan.FromSeconds(3));
            }
            
            // Ensure proxy is restored even if service wasn't running
            var restoreTask = Task.Run(async () => await _systemProxyService.RestoreSystemProxyAsync());
            restoreTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error during dispose: {ex.Message}", ex);
            
            // Force disable proxy as last resort
            try
            {
                var forceDisableTask = Task.Run(async () => await _systemProxyService.ForceDisableProxyAsync());
                forceDisableTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // Ignore errors when force disabling
            }
        }
        finally
        {
            _reverseProxyService?.Dispose();
            _dnsOptimizationService?.Dispose();
            _certificateManagerService?.Dispose();
            _hostsFileService?.Dispose();
            _statisticsService?.Dispose();

            // Clear the current instance reference
            lock (_instanceLock)
            {
                if (_currentInstance == this)
                {
                    _currentInstance = null;
                }
            }
        }
    }
}

