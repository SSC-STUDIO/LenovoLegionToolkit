using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.DockLauncher.Services;
using LenovoLegionToolkit.Plugins.DockLauncher.Services.Settings;
using Microsoft.Win32;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.Plugins.DockLauncher;

/// <summary>
/// Dock launcher main page
/// </summary>
public partial class DockLauncherPage : INotifyPropertyChanged
{
    private readonly DockSettings _settings;
    private readonly IApplicationService _applicationService;
    private DockLauncherPlugin? _plugin;
    private bool _isEnabled;

    public ObservableCollection<DockItemViewModel> DockItems { get; } = new();

    public new bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
                return;
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: IsEnabled changing from {_isEnabled} to {value}");
            
            _isEnabled = value;
            OnPropertyChanged();
            
            _settings.IsEnabled = value;
            _ = _settings.SaveAsync(); // Save immediately when toggled
            
            UpdateDockWindow();
        }
    }

    public DockLauncherPage()
    {
        InitializeComponent();
        DataContext = this;
        
        _settings = new DockSettings();
        _applicationService = new ApplicationService();
        
        // Get plugin instance
        var pluginManager = Lib.IoCContainer.Resolve<Lib.Plugins.IPluginManager>();
        var plugin = pluginManager.GetRegisteredPlugins()
            .FirstOrDefault(p => p.Id == Lib.Plugins.PluginConstants.DockLauncher);
        _plugin = plugin as DockLauncherPlugin;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Loading settings");
            
            await _settings.LoadAsync();
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Settings loaded - IsEnabled={_settings.IsEnabled}, DockItems.Count={_settings.DockItems?.Count ?? 0}");
            
            // Temporarily disable property change notification to avoid triggering UpdateDockWindow twice
            _isEnabled = _settings.IsEnabled;
            OnPropertyChanged(nameof(IsEnabled));
            
            DockItems.Clear();
            if (_settings.DockItems != null)
            {
                foreach (var dockItem in _settings.DockItems.OrderBy(d => d.Order))
                {
                    var icon = _applicationService.GetApplicationIcon(dockItem.ExecutablePath);
                    DockItems.Add(new DockItemViewModel
                    {
                        Id = dockItem.Id,
                        Name = dockItem.Name,
                        ExecutablePath = dockItem.ExecutablePath,
                        Icon = icon,
                        Order = dockItem.Order
                    });
                }
            }
            
            UpdateNoApplicationsVisibility();
            UpdateDockWindow();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings: {ex.Message}", ex);
        }
    }

    private void UpdateNoApplicationsVisibility()
    {
        _noApplicationsTextBlock.Visibility = DockItems.Count == 0 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    private void UpdateDockWindow()
    {
        if (_plugin == null)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Plugin instance is null, cannot update dock window");
            return;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"DockLauncherPage: UpdateDockWindow - IsEnabled={IsEnabled}, _settings.IsEnabled={_settings.IsEnabled}");

        if (IsEnabled)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Showing dock window");
            _plugin.ShowDockWindow();
            _plugin.RefreshDockWindow();
        }
        else
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Closing dock window");
            _plugin.CloseDockWindow();
        }
    }

    private async void AddApplicationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Application"
            };

            if (dialog.ShowDialog() == true)
            {
                var executablePath = dialog.FileName;
                var name = System.IO.Path.GetFileNameWithoutExtension(executablePath);
                var icon = _applicationService.GetApplicationIcon(executablePath);
                
                var dockItem = new DockItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    ExecutablePath = executablePath,
                    Order = _settings.DockItems.Count
                };
                
                _settings.DockItems.Add(dockItem);
                await _settings.SaveAsync();
                
                DockItems.Add(new DockItemViewModel
                {
                    Id = dockItem.Id,
                    Name = dockItem.Name,
                    ExecutablePath = dockItem.ExecutablePath,
                    Icon = icon,
                    Order = dockItem.Order
                });
                
                UpdateNoApplicationsVisibility();
                UpdateDockWindow();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error adding application: {ex.Message}", ex);
        }
    }

    private void EnableDockToggle_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggleSwitch)
            return;

        try
        {
            // Toggle the value directly - the binding will update the UI
            // We toggle based on current IsEnabled state, not IsChecked, to avoid binding timing issues
            var newValue = !IsEnabled;
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: ToggleSwitch clicked - IsChecked={toggleSwitch.IsChecked}, Current IsEnabled={IsEnabled}, Toggling to {newValue}");
            
            // Update the property - this will trigger the setter which will save settings and update the dock window
            IsEnabled = newValue;
            
            // Also update the toggle switch directly to ensure UI is in sync
            toggleSwitch.IsChecked = newValue;
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: After update - IsEnabled={IsEnabled}, _settings.IsEnabled={_settings.IsEnabled}, toggleSwitch.IsChecked={toggleSwitch.IsChecked}");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Error in EnableDockToggle_Click: {ex.Message}", ex);
        }
    }

    private async void RemoveApplicationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is DockItemViewModel viewModel)
            {
                var dockItem = _settings.DockItems.FirstOrDefault(d => d.Id == viewModel.Id);
                if (dockItem != null)
                {
                    _settings.DockItems.Remove(dockItem);
                    await _settings.SaveAsync();
                    
                    var itemToRemove = DockItems.FirstOrDefault(d => d.Id == viewModel.Id);
                    if (itemToRemove != null)
                    {
                        DockItems.Remove(itemToRemove);
                    }
                    
                    UpdateNoApplicationsVisibility();
                    UpdateDockWindow();
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error removing application: {ex.Message}", ex);
        }
    }

    private void ForceShowButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_plugin == null)
                return;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Force show button clicked");

            // Force show the dock window
            _plugin.ForceShowDockWindow();
            _plugin.RefreshDockWindow();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Error in ForceShowButton_Click: {ex.Message}", ex);
        }
    }

    private async void AlwaysShowButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Always show button clicked - disabling auto-hide");

            // Disable auto-hide temporarily
            _settings.AutoHide = false;
            await _settings.SaveAsync();
            
            // Refresh dock window to apply changes
            if (_plugin != null)
            {
                _plugin.ShowDockWindow();
                _plugin.RefreshDockWindow();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Error in AlwaysShowButton_Click: {ex.Message}", ex);
        }
    }

    private void RefreshDockButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_plugin == null)
                return;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Refresh dock button clicked");

            // Refresh the dock window
            _plugin.RefreshDockWindow();
            _plugin.ShowDockWindow();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Error in RefreshDockButton_Click: {ex.Message}", ex);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// View model for dock item display
/// </summary>
public class DockItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public System.Windows.Media.ImageSource? Icon { get; set; }
    public int Order { get; set; }
}


    {
        try
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is DockItemViewModel viewModel)
            {
                var dockItem = _settings.DockItems.FirstOrDefault(d => d.Id == viewModel.Id);
                if (dockItem != null)
                {
                    _settings.DockItems.Remove(dockItem);
                    await _settings.SaveAsync();
                    
                    var itemToRemove = DockItems.FirstOrDefault(d => d.Id == viewModel.Id);
                    if (itemToRemove != null)
                    {
                        DockItems.Remove(itemToRemove);
                    }
                    
                    UpdateNoApplicationsVisibility();
                    UpdateDockWindow();
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error removing application: {ex.Message}", ex);
        }
    }

    private void ForceShowButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_plugin == null)
                return;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Force show button clicked");

            // Force show the dock window
            _plugin.ForceShowDockWindow();
            _plugin.RefreshDockWindow();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Error in ForceShowButton_Click: {ex.Message}", ex);
        }
    }

    private async void AlwaysShowButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Always show button clicked - disabling auto-hide");

            // Disable auto-hide temporarily
            _settings.AutoHide = false;
            await _settings.SaveAsync();
            
            // Refresh dock window to apply changes
            if (_plugin != null)
            {
                _plugin.ShowDockWindow();
                _plugin.RefreshDockWindow();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Error in AlwaysShowButton_Click: {ex.Message}", ex);
        }
    }

    private void RefreshDockButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_plugin == null)
                return;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Refresh dock button clicked");

            // Refresh the dock window
            _plugin.RefreshDockWindow();
            _plugin.ShowDockWindow();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Error in RefreshDockButton_Click: {ex.Message}", ex);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// View model for dock item display
/// </summary>
public class DockItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public System.Windows.Media.ImageSource? Icon { get; set; }
    public int Order { get; set; }
}


using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.DockLauncher.Services;
using LenovoLegionToolkit.Plugins.DockLauncher.Services.Settings;
using Microsoft.Win32;

namespace LenovoLegionToolkit.Plugins.DockLauncher;

/// <summary>
/// Dock launcher main page
/// </summary>
public partial class DockLauncherPage : INotifyPropertyChanged
{
    private readonly DockSettings _settings;
    private readonly IApplicationService _applicationService;
    private DockLauncherPlugin? _plugin;
    private bool _isEnabled;

    public ObservableCollection<DockItemViewModel> DockItems { get; } = new();

    public new bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value)
                return;
            
            _isEnabled = value;
            OnPropertyChanged();
            
            _settings.IsEnabled = value;
            UpdateDockWindow();
        }
    }

    public DockLauncherPage()
    {
        InitializeComponent();
        DataContext = this;
        
        _settings = new DockSettings();
        _applicationService = new ApplicationService();
        
        // Get plugin instance
        var pluginManager = Lib.IoCContainer.Resolve<Lib.Plugins.IPluginManager>();
        var plugin = pluginManager.GetRegisteredPlugins()
            .FirstOrDefault(p => p.Id == Lib.Plugins.PluginConstants.DockLauncher);
        _plugin = plugin as DockLauncherPlugin;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            await _settings.LoadAsync();
            
            IsEnabled = _settings.IsEnabled;
            
            DockItems.Clear();
            foreach (var dockItem in _settings.DockItems.OrderBy(d => d.Order))
            {
                var icon = _applicationService.GetApplicationIcon(dockItem.ExecutablePath);
                DockItems.Add(new DockItemViewModel
                {
                    Id = dockItem.Id,
                    Name = dockItem.Name,
                    ExecutablePath = dockItem.ExecutablePath,
                    Icon = icon,
                    Order = dockItem.Order
                });
            }
            
            UpdateNoApplicationsVisibility();
            UpdateDockWindow();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error loading settings: {ex.Message}", ex);
        }
    }

    private void UpdateNoApplicationsVisibility()
    {
        _noApplicationsTextBlock.Visibility = DockItems.Count == 0 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    private void UpdateDockWindow()
    {
        if (_plugin == null)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Plugin instance is null, cannot update dock window");
            return;
        }

        if (IsEnabled && _settings.IsEnabled)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Showing dock window");
            _plugin.ShowDockWindow();
            _plugin.RefreshDockWindow();
        }
        else
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockLauncherPage: Closing dock window");
            _plugin.CloseDockWindow();
        }
    }

    private async void AddApplicationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*",
                Title = "Select Application"
            };

            if (dialog.ShowDialog() == true)
            {
                var executablePath = dialog.FileName;
                var name = System.IO.Path.GetFileNameWithoutExtension(executablePath);
                var icon = _applicationService.GetApplicationIcon(executablePath);
                
                var dockItem = new DockItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    ExecutablePath = executablePath,
                    Order = _settings.DockItems.Count
                };
                
                _settings.DockItems.Add(dockItem);
                await _settings.SaveAsync();
                
                DockItems.Add(new DockItemViewModel
                {
                    Id = dockItem.Id,
                    Name = dockItem.Name,
                    ExecutablePath = dockItem.ExecutablePath,
                    Icon = icon,
                    Order = dockItem.Order
                });
                
                UpdateNoApplicationsVisibility();
                UpdateDockWindow();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error adding application: {ex.Message}", ex);
        }
    }

    private async void RemoveApplicationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.Tag is DockItemViewModel viewModel)
            {
                var dockItem = _settings.DockItems.FirstOrDefault(d => d.Id == viewModel.Id);
                if (dockItem != null)
                {
                    _settings.DockItems.Remove(dockItem);
                    await _settings.SaveAsync();
                    
                    var itemToRemove = DockItems.FirstOrDefault(d => d.Id == viewModel.Id);
                    if (itemToRemove != null)
                    {
                        DockItems.Remove(itemToRemove);
                    }
                    
                    UpdateNoApplicationsVisibility();
                    UpdateDockWindow();
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error removing application: {ex.Message}", ex);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// View model for dock item display
/// </summary>
public class DockItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public System.Windows.Media.ImageSource? Icon { get; set; }
    public int Order { get; set; }
}

