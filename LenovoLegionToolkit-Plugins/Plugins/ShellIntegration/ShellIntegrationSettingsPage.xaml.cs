using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.ShellIntegration.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

/// <summary>
/// Shell Integration settings page
/// </summary>
public partial class ShellIntegrationSettingsPage : System.Windows.Controls.UserControl, IPluginPage
{
    private readonly ILogger<ShellIntegrationSettingsPage> _logger;
    private readonly IContextMenuItemManager _menuManager;
    private readonly IShellExtensionManager _extensionManager;
    private readonly IShellOptimizationService _optimizationService;

    public string PageTitle => "Shell Integration Settings";
    
    public string PageIcon => "Settings24";

    public object CreatePage() => this;

    public string PageDescription => "Configure context menu items and shell extensions";

    public ShellIntegrationSettingsPage(
        ILogger<ShellIntegrationSettingsPage> logger,
        IContextMenuItemManager menuManager,
        IShellExtensionManager extensionManager,
        IShellOptimizationService optimizationService)
    {
        InitializeComponent();
        
        _logger = logger;
        _menuManager = menuManager;
        _extensionManager = extensionManager;
        _optimizationService = optimizationService;

        DataContext = this;
        
        Loaded += ShellIntegrationSettingsPage_Loaded;
    }

    #region Properties

    public List<ContextMenuItem> MenuItems => _menuManager.GetMenuItems().ToList();

    public List<ShellExtension> Extensions => _extensionManager.GetExtensions().ToList();

    public bool ContextMenuAnimations
    {
        get => _optimizationService.GetContextMenuAnimations();
        set
        {
            _optimizationService.SetContextMenuAnimations(value);
            OnPropertyChanged();
        }
    }

    public bool ShowFileExtensions
    {
        get => _optimizationService.GetShowFileExtensions();
        set
        {
            _optimizationService.SetShowFileExtensions(value);
            OnPropertyChanged();
        }
    }

    public bool ShowHiddenFiles
    {
        get => _optimizationService.GetShowHiddenFiles();
        set
        {
            _optimizationService.SetShowHiddenFiles(value);
            OnPropertyChanged();
        }
    }

    public bool QuickAccess
    {
        get => _optimizationService.GetQuickAccess();
        set
        {
            _optimizationService.SetQuickAccess(value);
            OnPropertyChanged();
        }
    }

    public bool PreviewPane
    {
        get => _optimizationService.GetPreviewPane();
        set
        {
            _optimizationService.SetPreviewPane(value);
            OnPropertyChanged();
        }
    }

    public bool Transparency
    {
        get => _optimizationService.GetTransparency();
        set
        {
            _optimizationService.SetTransparency(value);
            OnPropertyChanged();
        }
    }

    public bool RoundedCorners
    {
        get => _optimizationService.GetRoundedCorners();
        set
        {
            _optimizationService.SetRoundedCorners(value);
            OnPropertyChanged();
        }
    }

    public bool Shadows
    {
        get => _optimizationService.GetShadows();
        set
        {
            _optimizationService.SetShadows(value);
            OnPropertyChanged();
        }
    }

    #endregion

    #region Event Handlers

    private async void ShellIntegrationSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        _logger.LogDebug("Shell Integration settings page loaded");
        await RefreshDataAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshDataAsync();
        ShowSnackbar("Settings refreshed successfully", "Info");
    }

    private void ToggleMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement element && element.Tag is string itemId)
            {
                var menuItem = _menuManager.GetMenuItem(itemId);
                if (menuItem != null)
                {
                    menuItem.IsEnabled = !menuItem.IsEnabled;
                    var success = _menuManager.ToggleMenuItem(itemId, menuItem.IsEnabled);
                     
                    if (success)
                    {
                        ShowSnackbar($"Menu item {menuItem.Name} {(menuItem.IsEnabled ? "enabled" : "disabled")}", "Success");
                        OnPropertyChanged(nameof(MenuItems));
                    }
                    else
                    {
                        ShowSnackbar("Failed to toggle menu item", "Error");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ShowSnackbar($"Error toggling menu item: {ex.Message}", "Error");
        }
    }

    private void ToggleExtension_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement element && element.Tag is string extensionId)
            {
                var extension = _extensionManager.GetExtension(extensionId);
                if (extension != null && !extension.IsSystem)
                {
                    extension.IsEnabled = !extension.IsEnabled;
                    var success = _extensionManager.ToggleExtension(extensionId, extension.IsEnabled);
                    
                    if (success)
                    {
                        ShowSnackbar($"Extension {extension.Name} {(extension.IsEnabled ? "enabled" : "disabled")}", "Success");
                        OnPropertyChanged(nameof(Extensions));
                    }
                    else
                    {
                        ShowSnackbar("Failed to toggle extension", "Error");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle extension");
            ShowSnackbar("Failed to toggle extension", "Error");
        }
    }

    private void ResetMenuItems_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all menu items to default? This will remove any custom menu items you have added.",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _menuManager.ResetToDefaults();
                OnPropertyChanged(nameof(MenuItems));
                ShowSnackbar("Menu items reset to defaults", "Success");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset menu items");
            ShowSnackbar("Failed to reset menu items", "Error");
        }
    }

    #endregion

    #region Private Methods

    private async Task RefreshDataAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                // Refresh data
                _menuManager.GetMenuItems();
                _extensionManager.RefreshExtensions();
            });

            OnPropertyChanged(nameof(MenuItems));
            OnPropertyChanged(nameof(Extensions));
            
            _logger.LogDebug("Settings data refreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh settings data");
        }
    }

    private void ShowSnackbar(string message, string title = "")
    {
        // This would use the main application's snackbar
        _logger.LogInformation("Snackbar: {Message} ({Title})", message, title);
    }

    #endregion

    #region INotifyPropertyChanged

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
    }

    #endregion
}