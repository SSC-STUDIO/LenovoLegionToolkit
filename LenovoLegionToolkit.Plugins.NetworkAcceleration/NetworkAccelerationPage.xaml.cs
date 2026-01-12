using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Dns;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.ReverseProxy;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Certificate;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Hosts;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Script;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Statistics;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Settings;
using LenovoLegionToolkit.WPF.Utils;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

/// <summary>
/// Network Acceleration Page - Real-time network acceleration and optimization
/// </summary>
public partial class NetworkAccelerationPage : INotifyPropertyChanged
{
    private bool _isServiceEnabled;
    private string _serviceStatus = string.Empty;
    private string _serviceStatusDescription = string.Empty;
    private bool _isDnsOptimizationEnabled;
    private bool _isRequestInterceptionEnabled;
    private bool _isGithubAccelerationEnabled;
    private bool _isSteamAccelerationEnabled;
    private bool _isDiscordAccelerationEnabled;
    private bool _isNpmAccelerationEnabled;
    private bool _isPypiAccelerationEnabled;
    private string _proxyAddress = string.Empty;
    private int _proxyPort = 8888;
    private int _connectionTimeout = 30;
    private string _downloadedTraffic = "0 MB";
    private string _uploadedTraffic = "0 MB";
    private string _downloadSpeed = "0 B/s";
    private string _uploadSpeed = "0 B/s";
    private string _totalTraffic = "0 MB";

    private readonly INetworkAccelerationService _networkAccelerationService;
    private readonly IDnsOptimizationService _dnsOptimizationService;
    private readonly IReverseProxyService _reverseProxyService;
    private readonly INetworkStatisticsService _statisticsService;
    private readonly NetworkAccelerationSettings _settings;
    private readonly DispatcherTimer _statisticsUpdateTimer;

    public NetworkAccelerationPage()
    {
        InitializeComponent();
        DataContext = this;

        // Initialize services
        _statisticsService = new NetworkStatisticsService();
        var scriptService = new ScriptManagerService();
        _dnsOptimizationService = new DnsOptimizationService();
        _reverseProxyService = new ReverseProxyService(scriptService, _statisticsService, _dnsOptimizationService);
        var certificateService = new CertificateManagerService();
        var hostsService = new HostsFileService();

        _networkAccelerationService = new NetworkAccelerationService(
            _reverseProxyService,
            _dnsOptimizationService,
            certificateService,
            hostsService,
            _statisticsService);

        _settings = new NetworkAccelerationSettings();

        // Setup statistics update timer
        _statisticsUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _statisticsUpdateTimer.Tick += StatisticsUpdateTimer_Tick;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        // Ensure plugin Resource.Culture is set before loading
        ApplyPluginResourceCulture();
        await LoadSettingsAsync();
    }
    
    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        // Do NOT stop the acceleration service when navigating away.
        // Users expect the service to keep running while they browse other pages.
        // Only pause the statistics timer to avoid unnecessary UI work.
        _statisticsUpdateTimer?.Stop();
    }
    
    private Task ForceRestoreProxyAsync()
    {
        try
        {
            // Dispose the service to ensure proxy is restored
            if (_networkAccelerationService is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error force restoring proxy: {ex.Message}", ex);
        }
        return Task.CompletedTask;
    }
    
    private async Task ForceDisableProxyAsync()
    {
        try
        {
            // Create a temporary SystemProxyService to force disable proxy as last resort
            var proxyService = new Services.SystemProxy.SystemProxyService();
            await proxyService.ForceDisableProxyAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error force disabling proxy: {ex.Message}", ex);
        }
    }
    
    private void ApplyPluginResourceCulture()
    {
        try
        {
            // Use LocalizationHelper to set plugin resource culture
            // This will respect plugin-specific language settings
            WPF.Utils.LocalizationHelper.SetPluginResourceCultures();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error applying plugin resource culture: {ex.Message}", ex);
        }
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            await _settings.LoadAsync();

            IsServiceEnabled = _settings.IsServiceEnabled;
            IsDnsOptimizationEnabled = _settings.IsDnsOptimizationEnabled;
            IsRequestInterceptionEnabled = _settings.IsRequestInterceptionEnabled;
            IsGithubAccelerationEnabled = _settings.IsGithubAccelerationEnabled;

            IsSteamAccelerationEnabled = _settings.IsSteamAccelerationEnabled;
            IsDiscordAccelerationEnabled = _settings.IsDiscordAccelerationEnabled;
            IsNpmAccelerationEnabled = _settings.IsNpmAccelerationEnabled;
            IsPypiAccelerationEnabled = _settings.IsPypiAccelerationEnabled;
            ProxyAddress = _settings.ProxyAddress;
            ProxyPort = _settings.ProxyPort;
            _connectionTimeout = _settings.ConnectionTimeout;

            // Apply DNS optimization setting
            _dnsOptimizationService.SetEnabled(IsDnsOptimizationEnabled);

            // If service was enabled, start it
            if (IsServiceEnabled && !_networkAccelerationService.IsRunning)
            {
                await StartServiceAsync();
            }
            // If service is already running (e.g., user enabled it before navigation), ensure timer is running
            else if (IsServiceEnabled && _networkAccelerationService.IsRunning)
            {
                _statisticsUpdateTimer.Start();
            }
            else if (!IsServiceEnabled && _networkAccelerationService.IsRunning)
            {
                await StopServiceAsync();
            }

            UpdateServiceStatus();
            UpdateTrafficStatistics();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings: {ex.Message}", ex);
        }
    }

    private async void ServiceToggleSwitch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
            return;

        try
        {
            if (toggleSwitch.IsChecked == true)
            {
                await StartServiceAsync();
            }
            else
            {
                await StopServiceAsync();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error toggling network acceleration service: {ex.Message}", ex);
            
            // Revert toggle state on error
            toggleSwitch.IsChecked = !toggleSwitch.IsChecked;
        }
    }

    private async Task StartServiceAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Starting network acceleration service...");

            // Apply DNS optimization setting
            _dnsOptimizationService.SetEnabled(IsDnsOptimizationEnabled);

            // Start the service
            var result = await _networkAccelerationService.StartAsync();
            
            if (result)
            {
                IsServiceEnabled = true;
                _settings.IsServiceEnabled = true;
                _statisticsUpdateTimer.Start();
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Network acceleration service started successfully.");
            }
            else
            {
                IsServiceEnabled = false;
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to start network acceleration service.");
            }

            UpdateServiceStatus();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error starting network acceleration service: {ex.Message}", ex);
            
            IsServiceEnabled = false;
            UpdateServiceStatus();
        }
    }

    private async Task StopServiceAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Stopping network acceleration service...");

            _statisticsUpdateTimer.Stop();

            // Stop the service
            var result = await _networkAccelerationService.StopAsync();
            
            if (result)
            {
                IsServiceEnabled = false;
                _settings.IsServiceEnabled = false;
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Network acceleration service stopped successfully.");
            }

            UpdateServiceStatus();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping network acceleration service: {ex.Message}", ex);
        }
    }

    private void UpdateServiceStatus()
    {
        if (_networkAccelerationService.IsRunning)
        {
            ServiceStatus = _networkAccelerationService.GetStatus();
            ServiceStatusDescription = _networkAccelerationService.GetStatusDescription();
        }
        else
        {
            ServiceStatus = Resource.NetworkAcceleration_ServiceStatusStopped;
            ServiceStatusDescription = Resource.NetworkAcceleration_ServiceStatusStoppedDescription;
        }
    }

    private void StatisticsUpdateTimer_Tick(object? sender, EventArgs e)
    {
        UpdateTrafficStatistics();
    }

    private void UpdateTrafficStatistics()
    {
        // Update total traffic first
        TotalTraffic = _statisticsService.GetFormattedTotal();
        
        // Update download and upload totals
        DownloadedTraffic = _statisticsService.GetFormattedDownloaded();
        UploadedTraffic = _statisticsService.GetFormattedUploaded();
        
        // Update speeds (GetDownloadSpeed will update the history)
        var (_, downloadSpeedFormatted) = _statisticsService.GetDownloadSpeed();
        var (_, uploadSpeedFormatted) = _statisticsService.GetUploadSpeed();
        DownloadSpeed = downloadSpeedFormatted;
        UploadSpeed = uploadSpeedFormatted;
        
        // Update chart (ensure UI thread)
        var history = _statisticsService.GetSpeedHistory(60);
        if (_trafficChartControl != null)
        {
            if (Dispatcher.CheckAccess())
            {
                _trafficChartControl.UpdateChart(history);
            }
            else
            {
                Dispatcher.Invoke(() => _trafficChartControl.UpdateChart(history));
            }
        }
    }

    private void ResetStatisticsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _statisticsService.Reset();
            UpdateTrafficStatistics();
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Network statistics reset successfully.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error resetting statistics: {ex.Message}", ex);
        }
    }

    private void Page_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Handle mouse wheel scrolling at UiPage level (bubbling event)
        // This is called after PreviewMouseWheel if event wasn't handled
        if (e.Handled)
            return;
            
        if (_contentScrollViewer != null && _contentScrollViewer.ScrollableHeight > 0)
        {
            var offset = _contentScrollViewer.VerticalOffset - (e.Delta / 3.0);
            _contentScrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, _contentScrollViewer.ScrollableHeight)));
            e.Handled = true;
        }
    }
    
    private void Page_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Handle mouse wheel scrolling at page level (tunneling event - fires before routing to children)
        // This is the first chance to handle the event before it reaches child controls
        if (e.Handled)
            return;
            
        if (_contentScrollViewer != null && _contentScrollViewer.ScrollableHeight > 0)
        {
            var offset = _contentScrollViewer.VerticalOffset - (e.Delta / 3.0);
            _contentScrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, _contentScrollViewer.ScrollableHeight)));
            e.Handled = true;
        }
    }
    
    private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Handle mouse wheel scrolling at Grid level (bubbling event)
        // This ensures scrolling works even when mouse is over child controls within the Grid
        if (e.Handled)
            return;
            
        if (_contentScrollViewer != null && _contentScrollViewer.ScrollableHeight > 0)
        {
            var offset = _contentScrollViewer.VerticalOffset - (e.Delta / 3.0);
            _contentScrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, _contentScrollViewer.ScrollableHeight)));
            e.Handled = true;
        }
    }
    
    private void StackPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Handle mouse wheel scrolling at content StackPanel level
        // This ensures events from all child controls are captured
        if (e.Handled)
            return;
            
        if (_contentScrollViewer != null && _contentScrollViewer.ScrollableHeight > 0)
        {
            var offset = _contentScrollViewer.VerticalOffset - (e.Delta / 3.0);
            _contentScrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, _contentScrollViewer.ScrollableHeight)));
            e.Handled = true;
        }
    }
    
    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Handle mouse wheel scrolling in the ScrollViewer itself
        if (e.Handled)
            return;
            
        if (sender is ScrollViewer scrollViewer && scrollViewer.ScrollableHeight > 0)
        {
            var offset = scrollViewer.VerticalOffset - (e.Delta / 3.0);
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, Math.Min(offset, scrollViewer.ScrollableHeight)));
            e.Handled = true;
        }
    }
    
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null)
            return null;
            
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T result)
                return result;
            
            var childOfChild = FindVisualChild<T>(child);
            if (childOfChild != null)
                return childOfChild;
        }
        return null;
    }

    #region Properties

    public bool IsServiceEnabled
    {
        get => _isServiceEnabled;
        set
        {
            if (_isServiceEnabled == value)
                return;
            
            _isServiceEnabled = value;
            OnPropertyChanged();
            UpdateServiceStatus();
        }
    }

    public string ServiceStatus
    {
        get => _serviceStatus;
        set
        {
            if (_serviceStatus == value)
                return;
            
            _serviceStatus = value;
            OnPropertyChanged();
        }
    }

    public string ServiceStatusDescription
    {
        get => _serviceStatusDescription;
        set
        {
            if (_serviceStatusDescription == value)
                return;
            
            _serviceStatusDescription = value;
            OnPropertyChanged();
        }
    }

    public bool IsDnsOptimizationEnabled
    {
        get => _isDnsOptimizationEnabled;
        set
        {
            if (_isDnsOptimizationEnabled == value)
                return;
            
            _isDnsOptimizationEnabled = value;
            OnPropertyChanged();
            
            _settings.IsDnsOptimizationEnabled = value;
            _dnsOptimizationService.SetEnabled(value);
        }
    }

    public bool IsRequestInterceptionEnabled
    {
        get => _isRequestInterceptionEnabled;
        set
        {
            if (_isRequestInterceptionEnabled == value)
                return;
            
            _isRequestInterceptionEnabled = value;
            OnPropertyChanged();
            
            _settings.IsRequestInterceptionEnabled = value;
            // Request interception is handled by the reverse proxy service
        }
    }

    public bool IsGithubAccelerationEnabled
    {
        get => _isGithubAccelerationEnabled;
        set
        {
            if (_isGithubAccelerationEnabled == value)
                return;
            
            _isGithubAccelerationEnabled = value;
            OnPropertyChanged();
            
            _settings.IsGithubAccelerationEnabled = value;
            // GitHub acceleration is handled by the reverse proxy service with specific domain rules
        }
    }

    public string ProxyAddress
    {
        get => _proxyAddress;
        set
        {
            if (_proxyAddress == value)
                return;
            
            _proxyAddress = value ?? string.Empty;
            OnPropertyChanged();
            
            _settings.ProxyAddress = value ?? string.Empty;
            // Proxy address is used when configuring the reverse proxy service
        }
    }

    public string DownloadedTraffic
    {
        get => _downloadedTraffic;
        set
        {
            if (_downloadedTraffic == value)
                return;
            
            _downloadedTraffic = value;
            OnPropertyChanged();
        }
    }

    public string UploadedTraffic
    {
        get => _uploadedTraffic;
        set
        {
            if (_uploadedTraffic == value)
                return;
            
            _uploadedTraffic = value;
            OnPropertyChanged();
        }
    }

    public string DownloadSpeed
    {
        get => _downloadSpeed;
        set
        {
            if (_downloadSpeed == value)
                return;
            
            _downloadSpeed = value;
            OnPropertyChanged();
        }
    }

    public string UploadSpeed
    {
        get => _uploadSpeed;
        set
        {
            if (_uploadSpeed == value)
                return;
            
            _uploadSpeed = value;
            OnPropertyChanged();
        }
    }

    public string TotalTraffic
    {
        get => _totalTraffic;
        set
        {
            if (_totalTraffic == value)
                return;
            
            _totalTraffic = value;
            OnPropertyChanged();
        }
    }



    public bool IsSteamAccelerationEnabled
    {
        get => _isSteamAccelerationEnabled;
        set
        {
            if (_isSteamAccelerationEnabled == value)
                return;
            
            _isSteamAccelerationEnabled = value;
            OnPropertyChanged();
            _settings.IsSteamAccelerationEnabled = value;
        }
    }

    public bool IsDiscordAccelerationEnabled
    {
        get => _isDiscordAccelerationEnabled;
        set
        {
            if (_isDiscordAccelerationEnabled == value)
                return;
            
            _isDiscordAccelerationEnabled = value;
            OnPropertyChanged();
            _settings.IsDiscordAccelerationEnabled = value;
        }
    }

    public bool IsNpmAccelerationEnabled
    {
        get => _isNpmAccelerationEnabled;
        set
        {
            if (_isNpmAccelerationEnabled == value)
                return;
            
            _isNpmAccelerationEnabled = value;
            OnPropertyChanged();
            _settings.IsNpmAccelerationEnabled = value;
        }
    }

    public bool IsPypiAccelerationEnabled
    {
        get => _isPypiAccelerationEnabled;
        set
        {
            if (_isPypiAccelerationEnabled == value)
                return;
            
            _isPypiAccelerationEnabled = value;
            OnPropertyChanged();
            _settings.IsPypiAccelerationEnabled = value;
        }
    }

    public int ProxyPort
    {
        get => _proxyPort;
        set
        {
            if (_proxyPort == value)
                return;
            
            _proxyPort = value;
            OnPropertyChanged();
            _settings.ProxyPort = value;
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}






