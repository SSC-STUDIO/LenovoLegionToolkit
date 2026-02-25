using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.ShellIntegration.Services;
using LenovoLegionToolkit.WPF.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

/// <summary>
/// Snackbar type enum for notifications (mirrors LenovoLegionToolkit.WPF.SnackbarType)
/// </summary>
public enum SnackbarType
{
    Success,
    Warning,
    Error,
    Info
}

/// <summary>
/// Shell Integration main page
/// </summary>
public partial class ShellIntegrationPage : Page, IPluginPage
{
    private readonly ILogger<ShellIntegrationPage> _logger;
    private readonly IShellIntegrationService _shellService;
    private readonly IContextMenuItemManager _menuManager;
    private readonly IShellExtensionManager _extensionManager;

    public ShellIntegrationPage(
        ILogger<ShellIntegrationPage> logger,
        IShellIntegrationService shellService,
        IContextMenuItemManager menuManager,
        IShellExtensionManager extensionManager)
    {
        InitializeComponent();
        
        _logger = logger;
        _shellService = shellService;
        _menuManager = menuManager;
        _extensionManager = extensionManager;

        DataContext = this;
        
        Loaded += ShellIntegrationPage_Loaded;
        Unloaded += ShellIntegrationPage_Unloaded;
    }

    #region Properties

    public string PageTitle => "Shell Integration";
    
    public string PageIcon => "ContextMenu24";

    public object CreatePage() => this;
    
    public string PageDescription => "Enhanced Windows Shell integration with context menu extensions";

    public bool IsInstalled => _shellService.IsInstalled;

    public bool IsRunning => _shellService.IsRunning;

    public string Status => _shellService.GetStatusDescription();

    public string Version => GetVersionDisplay();

    #endregion

    #region Event Handlers

    private async void ShellIntegrationPage_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.LogDebug("Shell Integration page loaded");
        
        // Subscribe to service events
        _shellService.StatusChanged += OnServiceStatusChanged;
        
        // Refresh data
        await RefreshDataAsync();
    }

    private void ShellIntegrationPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _logger.LogDebug("Shell Integration page unloaded");
        
        // Unsubscribe from events
        _shellService.StatusChanged -= OnServiceStatusChanged;
    }

    private async void OnServiceStatusChanged(object? sender, ShellIntegrationStatusEventArgs e)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            OnPropertyChanged(nameof(IsInstalled));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(Version));
            
            UpdateActionButtons();
        });
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Installing Shell Integration...");
            
            InstallButton.IsEnabled = false;
            InstallProgressBar.Visibility = Visibility.Visible;
            InstallProgressBar.IsIndeterminate = true;

            var success = await _shellService.InstallAsync();
            
            InstallProgressBar.IsIndeterminate = false;
            InstallProgressBar.Visibility = Visibility.Collapsed;
            InstallButton.IsEnabled = true;

            if (success)
            {
                ShowSnackbar("Shell Integration installed successfully", "Success", SnackbarType.Success);
            }
            else
            {
                ShowSnackbar("Failed to install Shell Integration", "Error", SnackbarType.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install Shell Integration");
            ShowSnackbar($"Installation failed: {ex.Message}", "Error", SnackbarType.Error);
            
            InstallProgressBar.Visibility = Visibility.Collapsed;
            InstallButton.IsEnabled = true;
        }
    }

    private async void UninstallButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Uninstalling Shell Integration...");
            
            var result = MessageBox.Show(
                "Are you sure you want to uninstall Shell Integration? This will remove all context menu enhancements.",
                "Confirm Uninstallation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            UninstallButton.IsEnabled = false;
            UninstallProgressBar.Visibility = Visibility.Visible;
            UninstallProgressBar.IsIndeterminate = true;

            var success = await _shellService.UninstallAsync();
            
            UninstallProgressBar.IsIndeterminate = false;
            UninstallProgressBar.Visibility = Visibility.Collapsed;
            UninstallButton.IsEnabled = true;

            if (success)
            {
                ShowSnackbar("Shell Integration uninstalled successfully", "Success", SnackbarType.Success);
            }
            else
            {
                ShowSnackbar("Failed to uninstall Shell Integration", "Error", SnackbarType.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall Shell Integration");
            ShowSnackbar($"Uninstallation failed: {ex.Message}", "Error", SnackbarType.Error);
            
            UninstallProgressBar.Visibility = Visibility.Collapsed;
            UninstallButton.IsEnabled = true;
        }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Starting Shell Integration...");
            
            StartButton.IsEnabled = false;
            StartProgressBar.Visibility = Visibility.Visible;
            StartProgressBar.IsIndeterminate = true;

            var success = await _shellService.StartAsync();
            
            StartProgressBar.IsIndeterminate = false;
            StartProgressBar.Visibility = Visibility.Collapsed;

            if (success)
            {
                ShowSnackbar("Shell Integration started successfully", "Success", SnackbarType.Success);
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to start Shell Integration. Check logs for details.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in StartButton_Click");
            System.Windows.MessageBox.Show("An error occurred: " + ex.Message);
            
            StartProgressBar.Visibility = Visibility.Collapsed;
        }
        finally
        {
            StartButton.IsEnabled = true;
            UninstallButton.IsEnabled = true;
        }
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Stopping Shell Integration...");
            
            StopButton.IsEnabled = false;
            StopProgressBar.Visibility = Visibility.Visible;
            StopProgressBar.IsIndeterminate = true;

            var success = await _shellService.StopAsync();
            
            StopProgressBar.IsIndeterminate = false;
            StopProgressBar.Visibility = Visibility.Collapsed;
            StopButton.IsEnabled = true;

            if (success)
            {
                ShowSnackbar("Shell Integration stopped successfully", "Success", SnackbarType.Success);
            }
            else
            {
                ShowSnackbar("Failed to stop Shell Integration", "Error", SnackbarType.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop Shell Integration");
            ShowSnackbar($"Failed to stop: {ex.Message}", "Error", SnackbarType.Error);
            
            StopProgressBar.Visibility = Visibility.Collapsed;
            StopButton.IsEnabled = true;
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshDataAsync();
        ShowSnackbar("Data refreshed successfully", "Info", SnackbarType.Info);
    }

    private void MenuSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to menu settings
        _logger.LogInformation("Navigate to menu settings");
        // Implementation would depend on navigation framework
    }

    private void ExtensionSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to extension settings
        _logger.LogInformation("Navigate to extension settings");
        // Implementation would depend on navigation framework
    }

    #endregion

    #region Private Methods

    private async Task RefreshDataAsync()
    {
        try
        {
            _logger.LogDebug("Refreshing Shell Integration data...");
            
            await Task.Run(() =>
            {
                // Refresh menu items
                _menuManager.GetMenuItems();
                
                // Refresh extensions
                _extensionManager.RefreshExtensions();
            });

            OnPropertyChanged(nameof(IsInstalled));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(Version));
            
            UpdateActionButtons();
            
            _logger.LogDebug("Shell Integration data refreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh data");
        }
    }

    private void UpdateActionButtons()
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (IsInstalled)
            {
                UninstallButton.Visibility = Visibility.Visible;
                
                if (IsRunning)
                {
                    StartButton.Visibility = Visibility.Collapsed;
                    StopButton.Visibility = Visibility.Visible;
                }
                else
                {
                    StartButton.Visibility = Visibility.Visible;
                    StopButton.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                UninstallButton.Visibility = Visibility.Collapsed;
                StartButton.Visibility = Visibility.Visible;
                StopButton.Visibility = Visibility.Collapsed;
            }
        });
    }

    private string GetVersionDisplay()
    {
        try
        {
            var version = _shellService.GetVersionAsync().GetAwaiter().GetResult();
            return string.IsNullOrEmpty(version) ? "Unknown" : version;
        }
        catch
        {
            return "Unknown";
        }
    }

    private void ShowSnackbar(string message, string title = "", SnackbarType type = SnackbarType.Info)
    {
        _logger.LogInformation("Snackbar: {Message} ({Title})", message, title);
        
        // Use reflection to call SnackbarHelper.Show with the correct SnackbarType
        var snackbarHelperType = typeof(SnackbarHelper);
        var showMethod = snackbarHelperType.GetMethod("Show", new[] { typeof(string), typeof(string), typeof(object) });
        
        if (showMethod != null)
        {
            // Get the SnackbarType enum value from WPF project
            var wpfSnackbarTypeEnum = Type.GetType("LenovoLegionToolkit.WPF.SnackbarType, Lenovo Legion Toolkit");
            if (wpfSnackbarTypeEnum != null)
            {
                var enumValue = Enum.Parse(wpfSnackbarTypeEnum, type.ToString());
                showMethod.Invoke(null, new[] { title, message, enumValue });
            }
            else
            {
                // Fallback: try calling without type parameter
                var simpleShowMethod = snackbarHelperType.GetMethod("Show", new[] { typeof(string), typeof(string) });
                simpleShowMethod?.Invoke(null, new[] { title, message });
            }
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }

    #endregion
}