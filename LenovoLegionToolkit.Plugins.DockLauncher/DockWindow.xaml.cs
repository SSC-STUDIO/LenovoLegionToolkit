using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.DockLauncher.Services;
using LenovoLegionToolkit.Plugins.DockLauncher.Services.Settings;

namespace LenovoLegionToolkit.Plugins.DockLauncher;

/// <summary>
/// Dock window - macOS-style application launcher with full features
/// </summary>
public partial class DockWindow : Window
{
    private readonly DockSettings _settings;
    private readonly IApplicationService _applicationService;
    private readonly IWindowService _windowService;
    private readonly TaskbarMonitorService _taskbarMonitor;
    private readonly WindowMinimizeMonitor _minimizeMonitor;
    private readonly TrashService _trashService;
    private readonly RecentAppsService _recentAppsService;
    private readonly Dictionary<string, DockItemControl> _dockItemControls = new();
    private System.Windows.Threading.DispatcherTimer? _autoHideTimer;
    private System.Windows.Threading.DispatcherTimer? _refreshTimer;
    private bool _isMouseOver;

    public DockWindow()
    {
        InitializeComponent();
        
        _settings = new DockSettings();
        _applicationService = new ApplicationService();
        _windowService = new WindowService();
        _taskbarMonitor = new TaskbarMonitorService(_applicationService);
        _minimizeMonitor = new WindowMinimizeMonitor(_windowService);
        _trashService = new TrashService();
        _recentAppsService = new RecentAppsService();
        
        DataContext = this;
        
        // Set initial position immediately to prevent showing at top-left
        SetInitialPosition();
        
        Loaded += DockWindow_Loaded;
        Closed += DockWindow_Closed;
        
        // Subscribe to taskbar monitor events
        _taskbarMonitor.ApplicationDetected += TaskbarMonitor_ApplicationDetected;
        _taskbarMonitor.ApplicationClosed += TaskbarMonitor_ApplicationClosed;
        
        // Subscribe to minimize monitor events
        _minimizeMonitor.WindowMinimized += MinimizeMonitor_WindowMinimized;
    }
    
    private void SetInitialPosition()
    {
        try
        {
            // Set initial position based on default settings (bottom)
            var screen = Screen.PrimaryScreen;
            if (screen != null)
            {
                var workingArea = screen.WorkingArea;
                var windowWidth = 400; // Default width estimate
                var windowHeight = 80;
                
                // Position at bottom center initially
                Left = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                Top = workingArea.Bottom - windowHeight - 10;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error setting initial position: {ex.Message}", ex);
        }
    }

    private async void DockWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Loading settings and initializing");
            
            await _settings.LoadAsync();
            await _recentAppsService.LoadAsync();
            
            // Apply settings
            ApplySettings();
            
            // Ensure window is visible initially
            Opacity = _settings.Opacity;
            Visibility = Visibility.Visible;
            ShowActivated = false; // Don't steal focus
            
            UpdateDockItems();
            
            // Start taskbar monitoring if enabled
            if (_settings.AutoAddTaskbarApps)
            {
                _taskbarMonitor.StartMonitoring(2);
            }
            
            // Start minimize monitoring for all dock items
            StartMinimizeMonitoring();
            
            // Start refresh timer to update running status
            StartRefreshTimer();
            
            // Update position after window is loaded and has actual size
            _ = Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdatePosition();
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"DockWindow: Position updated - Left={Left}, Top={Top}, Width={ActualWidth}, Height={ActualHeight}");
                
                // Play entrance animation - slide up from bottom
                if (_settings.EnableAnimations)
                {
                    var entranceAnimation = (Storyboard)Resources["EntranceAnimation"];
                    entranceAnimation.Begin(this);
                    
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"DockWindow: Playing entrance animation");
                }
                
                // Start auto-hide timer if enabled (with delay)
                if (_settings.AutoHide)
                {
                    // Delay auto-hide start to give user time to see the dock
                    var delayTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(_settings.AutoHideDelay + 2) // Extra 2 seconds initial delay
                    };
                    delayTimer.Tick += (s, args) =>
                    {
                        delayTimer.Stop();
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"DockWindow: Starting auto-hide timer");
                        StartAutoHideTimer();
                    };
                    delayTimer.Start();
                }
                else
                {
                    // If auto-hide is disabled, ensure window stays visible
                    Opacity = _settings.Opacity;
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"DockWindow: Auto-hide disabled, keeping window visible");
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error in Loaded event: {ex.Message}", ex);
        }
    }

    public void RefreshDockItems()
    {
        UpdateDockItems();
    }

    private void DockWindow_Closed(object? sender, EventArgs e)
    {
        _autoHideTimer?.Stop();
        _refreshTimer?.Stop();
        _taskbarMonitor.StopMonitoring();
        _minimizeMonitor.StopMonitoring();
    }
    
    private void StartMinimizeMonitoring()
    {
        if (!_settings.MinimizeToDock)
            return;

        foreach (var dockItem in _settings.DockItems)
        {
            _minimizeMonitor.StartMonitoring(dockItem.ExecutablePath);
        }
    }
    
    private void MinimizeMonitor_WindowMinimized(object? sender, WindowMinimizedEventArgs e)
    {
        try
        {
            // Find the dock item control for this executable
            if (_dockItemControls.TryGetValue(e.ExecutablePath, out var control))
            {
                Dispatcher.Invoke(() =>
                {
                    control.PlayMinimizeAnimation();
                });
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error handling minimize animation: {ex.Message}", ex);
        }
    }
    
    private void ApplySettings()
    {
        // Apply opacity
        Opacity = _settings.Opacity;
        
        // Apply height
        Height = _settings.DockHeight;
        
        // Apply blur effect (if supported)
        // Note: WPF blur effect requires additional setup
        
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"DockWindow: Applied settings - Opacity={_settings.Opacity}, Height={_settings.DockHeight}, Position={_settings.Position}");
    }
    
    private void StartRefreshTimer()
    {
        _refreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3) // Refresh every 3 seconds
        };
        _refreshTimer.Tick += (s, e) =>
        {
            if (!_isMouseOver) // Only refresh when mouse is not over to avoid flickering
            {
                UpdateDockItems();
            }
        };
        _refreshTimer.Start();
    }
    
    private async void TaskbarMonitor_ApplicationDetected(object? sender, ApplicationDetectedEventArgs e)
    {
        try
        {
            // Check if application is already in dock
            var existingItem = _settings.DockItems.FirstOrDefault(d => 
                string.Equals(d.ExecutablePath, e.ExecutablePath, StringComparison.OrdinalIgnoreCase));
            
            if (existingItem == null)
            {
                // Add new application to dock
                var newItem = new DockItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = e.ProcessName,
                    ExecutablePath = e.ExecutablePath,
                    Order = _settings.DockItems.Count,
                    IsAutoAdded = true
                };
                
                _settings.DockItems.Add(newItem);
                await _settings.SaveAsync();
                
                Dispatcher.Invoke(() => UpdateDockItems());
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"DockWindow: Auto-added taskbar application - {e.ProcessName}");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error auto-adding taskbar application: {ex.Message}", ex);
        }
    }
    
    private void TaskbarMonitor_ApplicationClosed(object? sender, ApplicationClosedEventArgs e)
    {
        // Optionally remove auto-added items when closed
        // For now, we keep them in dock for future launches
        Dispatcher.Invoke(() => UpdateDockItems());
    }

    private void UpdatePosition()
    {
        try
        {
            var screen = Screen.PrimaryScreen;
            if (screen != null)
            {
                var workingArea = screen.WorkingArea;
                
                // Force layout update to get actual size
                UpdateLayout();
                
                var windowWidth = ActualWidth > 0 ? ActualWidth : (Width > 0 ? Width : 400);
                var windowHeight = ActualHeight > 0 ? ActualHeight : (Height > 0 ? Height : _settings.DockHeight);
                
                // Position based on settings
                switch (_settings.Position)
                {
                    case DockPosition.Bottom:
                        Left = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                        Top = workingArea.Bottom - windowHeight - 10;
                        break;
                    case DockPosition.Top:
                        Left = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                        Top = workingArea.Top + 10;
                        break;
                    case DockPosition.Left:
                        Left = workingArea.Left + 10;
                        Top = workingArea.Top + (workingArea.Height - windowHeight) / 2;
                        break;
                    case DockPosition.Right:
                        Left = workingArea.Right - windowWidth - 10;
                        Top = workingArea.Top + (workingArea.Height - windowHeight) / 2;
                        break;
                }
                
                // Ensure window is within bounds
                if (Top < 0) Top = 10;
                if (Left < 0) Left = 10;
                
                // Ensure window is visible
                Visibility = Visibility.Visible;
                Opacity = _settings.Opacity;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error updating dock window position: {ex.Message}", ex);
        }
    }

    private void UpdateDockItems()
    {
        _dockItemsControl.Items.Clear();
        
        if (_settings.DockItems == null || _settings.DockItems.Count == 0)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: No dock items to display - showing placeholder");
            _noItemsPlaceholder.Visibility = Visibility.Visible;
        }
        else
        {
            _noItemsPlaceholder.Visibility = Visibility.Collapsed;
            
            // Separate running and non-running applications
            var runningItems = new System.Collections.Generic.List<DockItem>();
            var nonRunningItems = new System.Collections.Generic.List<DockItem>();
            
            foreach (var dockItem in _settings.DockItems.OrderBy(d => d.Order))
            {
                if (_applicationService.IsApplicationRunning(dockItem.ExecutablePath))
                {
                    runningItems.Add(dockItem);
                }
                else
                {
                    nonRunningItems.Add(dockItem);
                }
            }
            
            // Add running items first
            foreach (var dockItem in runningItems)
            {
                var control = CreateDockItemControl(dockItem);
                _dockItemsControl.Items.Add(control);
            }
            
            // Add separator if both running and non-running items exist
            if (runningItems.Count > 0 && nonRunningItems.Count > 0)
            {
                var separator = new System.Windows.Controls.Separator
                {
                    Width = 1,
                    Height = 48,
                    Margin = new Thickness(4, 0, 4, 0)
                };
                separator.SetResourceReference(System.Windows.Controls.Separator.BackgroundProperty, "ControlStrokeColorDefaultBrush");
                _dockItemsControl.Items.Add(separator);
            }
            
            // Add non-running items
            foreach (var dockItem in nonRunningItems)
            {
                var control = CreateDockItemControl(dockItem);
                _dockItemsControl.Items.Add(control);
            }
            
            // Add separator before recent apps if there are fixed apps
            var hasFixedApps = runningItems.Count > 0 || nonRunningItems.Count > 0;
            var recentApps = _recentAppsService.GetRecentApps(5);
            var recentAppsToShow = recentApps
                .Where(ra => !_settings.DockItems.Any(di => di.ExecutablePath.Equals(ra.ExecutablePath, StringComparison.OrdinalIgnoreCase)))
                .Take(5)
                .ToList();
            
            if (hasFixedApps && recentAppsToShow.Count > 0)
            {
                var separator = new System.Windows.Controls.Separator
                {
                    Width = 1,
                    Height = 48,
                    Margin = new Thickness(4, 0, 4, 0)
                };
                separator.SetResourceReference(System.Windows.Controls.Separator.BackgroundProperty, "ControlStrokeColorDefaultBrush");
                _dockItemsControl.Items.Add(separator);
            }
            
            // Add recent apps
            foreach (var recentApp in recentAppsToShow)
            {
                var dockItem = new DockItem
                {
                    Id = $"RECENT_{recentApp.ExecutablePath.GetHashCode()}",
                    Name = recentApp.AppName,
                    ExecutablePath = recentApp.ExecutablePath,
                    Order = int.MaxValue - recentAppsToShow.IndexOf(recentApp),
                    IsAutoAdded = true
                };
                var control = CreateDockItemControl(dockItem);
                _dockItemsControl.Items.Add(control);
            }
            
            // Add trash can at the end (always visible)
            var trashControl = CreateTrashControl();
            if (trashControl != null)
            {
                _dockItemsControl.Items.Add(trashControl);
            }
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Added {_settings.DockItems.Count} dock items (Running: {runningItems.Count}, Non-running: {nonRunningItems.Count})");
        }
        
        // Update position after items are added
        _ = Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdatePosition();
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: UpdateDockItems completed - Width={ActualWidth}, Height={ActualHeight}, Left={Left}, Top={Top}, IsVisible={IsVisible}");
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private DockItemControl CreateDockItemControl(DockItem dockItem)
    {
        var control = new DockItemControl(dockItem, _applicationService, _windowService);
        
        // Store control for minimize animation
        _dockItemControls[dockItem.ExecutablePath] = control;
        
        // Subscribe to events
        control.ItemReorderRequested += DockItemControl_ItemReorderRequested;
        control.ItemRemoveRequested += DockItemControl_ItemRemoveRequested;
        control.ItemLaunchRequested += DockItemControl_ItemLaunchRequested;
        
        // Start monitoring for minimize if enabled
        if (_settings.MinimizeToDock)
        {
            _minimizeMonitor.StartMonitoring(dockItem.ExecutablePath);
        }
        
        return control;
    }
    
    private DockItemControl? CreateTrashControl()
    {
        try
        {
            var trashIcon = _trashService.GetRecycleBinIcon();
            if (trashIcon == null)
                return null;
            
            // Create a special DockItem for trash
            var trashItem = new DockItem
            {
                Id = "TRASH_CAN",
                Name = "回收站",
                ExecutablePath = _trashService.GetRecycleBinPath(),
                Order = int.MaxValue, // Always last
                IsTrashCan = true
            };
            
            var control = new DockItemControl(trashItem, _applicationService, _windowService);
            control.SetTrashService(_trashService);
            
            return control;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error creating trash control: {ex.Message}", ex);
            return null;
        }
    }

    private async void DockItemControl_ItemReorderRequested(object? sender, DockItemReorderEventArgs e)
    {
        try
        {
            var draggedItem = e.DraggedItem;
            var targetItem = e.TargetItem;
            
            // Find indices
            var draggedIndex = _settings.DockItems.FindIndex(d => d.Id == draggedItem.Id);
            var targetIndex = _settings.DockItems.FindIndex(d => d.Id == targetItem.Id);
            
            if (draggedIndex >= 0 && targetIndex >= 0 && draggedIndex != targetIndex)
            {
                // Remove dragged item
                var item = _settings.DockItems[draggedIndex];
                _settings.DockItems.RemoveAt(draggedIndex);
                
                // Insert at target position
                var newIndex = draggedIndex < targetIndex ? targetIndex : targetIndex + 1;
                _settings.DockItems.Insert(newIndex, item);
                
                // Update order values
                for (int i = 0; i < _settings.DockItems.Count; i++)
                {
                    _settings.DockItems[i].Order = i;
                }
                
                await _settings.SaveAsync();
                UpdateDockItems();
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"DockWindow: Reordered item {draggedItem.Name} to position {newIndex}");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error reordering item: {ex.Message}", ex);
        }
    }

    private async void DockItemControl_ItemRemoveRequested(object? sender, DockItemEventArgs e)
    {
        try
        {
            var itemToRemove = _settings.DockItems.FirstOrDefault(d => d.Id == e.Item.Id);
            if (itemToRemove != null)
            {
                _settings.DockItems.Remove(itemToRemove);
                await _settings.SaveAsync();
                UpdateDockItems();
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"DockWindow: Removed item {itemToRemove.Name}");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error removing item: {ex.Message}", ex);
        }
    }

    private void DockItemControl_ItemLaunchRequested(object? sender, DockItemEventArgs e)
    {
        // Refresh items after launch to update running status
        _ = Task.Delay(1000).ContinueWith(_ =>
        {
            Dispatcher.Invoke(() => UpdateDockItems());
        });
    }

    private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isMouseOver = true;
        _autoHideTimer?.Stop();
        
        if (_settings.AutoHide)
        {
            // Stop any hide animation
            var hideAnimation = (Storyboard)Resources["HideAnimation"];
            hideAnimation.Stop(this);
            
            // Show window
            var showAnimation = (Storyboard)Resources["ShowAnimation"];
            showAnimation.Begin(this);
        }
    }

    private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isMouseOver = false;
        
        // Reset 3D perspective effect
        if (_settings.Enable3DPerspective)
        {
            Reset3DPerspective();
        }
        
        if (_settings.AutoHide)
        {
            StartAutoHideTimer();
        }
    }
    
    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_settings.Enable3DPerspective && _isMouseOver)
        {
            Update3DPerspective(e.GetPosition(this));
        }
    }
    
    private void Update3DPerspective(Point mousePosition)
    {
        try
        {
            var dockCenterX = ActualWidth / 2;
            var mouseX = mousePosition.X;
            
            // Calculate distance from center
            var maxDistance = dockCenterX;
            var distanceFromCenter = Math.Abs(mouseX - dockCenterX);
            
            // Update each dock item
            foreach (var item in _dockItemsControl.Items)
            {
                if (item is DockItemControl control)
                {
                    var itemPosition = control.TransformToAncestor(this).Transform(new Point(control.ActualWidth / 2, 0));
                    var itemCenterX = itemPosition.X;
                    var distance = Math.Abs(mouseX - itemCenterX);
                    
                    // Calculate scale based on distance (closer = larger)
                    var normalizedDistance = Math.Min(distance / maxDistance, 1.0);
                    var scale = 1.0 + (1.0 - normalizedDistance) * 0.4; // Max 1.4x scale
                    
                    // Calculate vertical offset for arc effect
                    var verticalOffset = (1.0 - normalizedDistance) * 15; // Max 15px up
                    
                    // Apply with animation
                    control.Apply3DTransform(scale, verticalOffset);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error updating 3D perspective: {ex.Message}", ex);
        }
    }
    
    private void Reset3DPerspective()
    {
        foreach (var item in _dockItemsControl.Items)
        {
            if (item is DockItemControl control)
            {
                control.Apply3DTransform(1.0, 0);
            }
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        // Keep window visible even when deactivated
    }

    private void StartAutoHideTimer()
    {
        if (!_settings.AutoHide)
            return;

        _autoHideTimer?.Stop();
        _autoHideTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_settings.AutoHideDelay)
        };
        _autoHideTimer.Tick += AutoHideTimer_Tick;
        _autoHideTimer.Start();
    }

    private void AutoHideTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isMouseOver && _settings.AutoHide)
        {
            _autoHideTimer?.Stop();
            var hideAnimation = (Storyboard)Resources["HideAnimation"];
            hideAnimation.Begin(this);
        }
    }
}

        // Note: WPF blur effect requires additional setup
        
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"DockWindow: Applied settings - Opacity={_settings.Opacity}, Height={_settings.DockHeight}, Position={_settings.Position}");
    }
    
    private void StartRefreshTimer()
    {
        _refreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3) // Refresh every 3 seconds
        };
        _refreshTimer.Tick += (s, e) =>
        {
            if (!_isMouseOver) // Only refresh when mouse is not over to avoid flickering
            {
                UpdateDockItems();
            }
        };
        _refreshTimer.Start();
    }
    
    private async void TaskbarMonitor_ApplicationDetected(object? sender, ApplicationDetectedEventArgs e)
    {
        try
        {
            // Check if application is already in dock
            var existingItem = _settings.DockItems.FirstOrDefault(d => 
                string.Equals(d.ExecutablePath, e.ExecutablePath, StringComparison.OrdinalIgnoreCase));
            
            if (existingItem == null)
            {
                // Add new application to dock
                var newItem = new DockItem
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = e.ProcessName,
                    ExecutablePath = e.ExecutablePath,
                    Order = _settings.DockItems.Count,
                    IsAutoAdded = true
                };
                
                _settings.DockItems.Add(newItem);
                await _settings.SaveAsync();
                
                Dispatcher.Invoke(() => UpdateDockItems());
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"DockWindow: Auto-added taskbar application - {e.ProcessName}");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error auto-adding taskbar application: {ex.Message}", ex);
        }
    }
    
    private void TaskbarMonitor_ApplicationClosed(object? sender, ApplicationClosedEventArgs e)
    {
        // Optionally remove auto-added items when closed
        // For now, we keep them in dock for future launches
        Dispatcher.Invoke(() => UpdateDockItems());
    }

    private void UpdatePosition()
    {
        try
        {
            var screen = Screen.PrimaryScreen;
            if (screen != null)
            {
                var workingArea = screen.WorkingArea;
                
                // Force layout update to get actual size
                UpdateLayout();
                
                var windowWidth = ActualWidth > 0 ? ActualWidth : (Width > 0 ? Width : 400);
                var windowHeight = ActualHeight > 0 ? ActualHeight : (Height > 0 ? Height : _settings.DockHeight);
                
                // Position based on settings
                switch (_settings.Position)
                {
                    case DockPosition.Bottom:
                        Left = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                        Top = workingArea.Bottom - windowHeight - 10;
                        break;
                    case DockPosition.Top:
                        Left = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                        Top = workingArea.Top + 10;
                        break;
                    case DockPosition.Left:
                        Left = workingArea.Left + 10;
                        Top = workingArea.Top + (workingArea.Height - windowHeight) / 2;
                        break;
                    case DockPosition.Right:
                        Left = workingArea.Right - windowWidth - 10;
                        Top = workingArea.Top + (workingArea.Height - windowHeight) / 2;
                        break;
                }
                
                // Ensure window is within bounds
                if (Top < 0) Top = 10;
                if (Left < 0) Left = 10;
                
                // Ensure window is visible
                Visibility = Visibility.Visible;
                Opacity = _settings.Opacity;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error updating dock window position: {ex.Message}", ex);
        }
    }

    private void UpdateDockItems()
    {
        _dockItemsControl.Items.Clear();
        
        if (_settings.DockItems == null || _settings.DockItems.Count == 0)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: No dock items to display - showing placeholder");
            _noItemsPlaceholder.Visibility = Visibility.Visible;
        }
        else
        {
            _noItemsPlaceholder.Visibility = Visibility.Collapsed;
            
            // Separate running and non-running applications
            var runningItems = new System.Collections.Generic.List<DockItem>();
            var nonRunningItems = new System.Collections.Generic.List<DockItem>();
            
            foreach (var dockItem in _settings.DockItems.OrderBy(d => d.Order))
            {
                if (_applicationService.IsApplicationRunning(dockItem.ExecutablePath))
                {
                    runningItems.Add(dockItem);
                }
                else
                {
                    nonRunningItems.Add(dockItem);
                }
            }
            
            // Add running items first
            foreach (var dockItem in runningItems)
            {
                var control = CreateDockItemControl(dockItem);
                _dockItemsControl.Items.Add(control);
            }
            
            // Add separator if both running and non-running items exist
            if (runningItems.Count > 0 && nonRunningItems.Count > 0)
            {
                var separator = new System.Windows.Controls.Separator
                {
                    Width = 1,
                    Height = 48,
                    Margin = new Thickness(4, 0, 4, 0)
                };
                separator.SetResourceReference(System.Windows.Controls.Separator.BackgroundProperty, "ControlStrokeColorDefaultBrush");
                _dockItemsControl.Items.Add(separator);
            }
            
            // Add non-running items
            foreach (var dockItem in nonRunningItems)
            {
                var control = CreateDockItemControl(dockItem);
                _dockItemsControl.Items.Add(control);
            }
            
            // Add separator before recent apps if there are fixed apps
            var hasFixedApps = runningItems.Count > 0 || nonRunningItems.Count > 0;
            var recentApps = _recentAppsService.GetRecentApps(5);
            var recentAppsToShow = recentApps
                .Where(ra => !_settings.DockItems.Any(di => di.ExecutablePath.Equals(ra.ExecutablePath, StringComparison.OrdinalIgnoreCase)))
                .Take(5)
                .ToList();
            
            if (hasFixedApps && recentAppsToShow.Count > 0)
            {
                var separator = new System.Windows.Controls.Separator
                {
                    Width = 1,
                    Height = 48,
                    Margin = new Thickness(4, 0, 4, 0)
                };
                separator.SetResourceReference(System.Windows.Controls.Separator.BackgroundProperty, "ControlStrokeColorDefaultBrush");
                _dockItemsControl.Items.Add(separator);
            }
            
            // Add recent apps
            foreach (var recentApp in recentAppsToShow)
            {
                var dockItem = new DockItem
                {
                    Id = $"RECENT_{recentApp.ExecutablePath.GetHashCode()}",
                    Name = recentApp.AppName,
                    ExecutablePath = recentApp.ExecutablePath,
                    Order = int.MaxValue - recentAppsToShow.IndexOf(recentApp),
                    IsAutoAdded = true
                };
                var control = CreateDockItemControl(dockItem);
                _dockItemsControl.Items.Add(control);
            }
            
            // Add trash can at the end (always visible)
            var trashControl = CreateTrashControl();
            if (trashControl != null)
            {
                _dockItemsControl.Items.Add(trashControl);
            }
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Added {_settings.DockItems.Count} dock items (Running: {runningItems.Count}, Non-running: {nonRunningItems.Count})");
        }
        
        // Update position after items are added
        _ = Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdatePosition();
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: UpdateDockItems completed - Width={ActualWidth}, Height={ActualHeight}, Left={Left}, Top={Top}, IsVisible={IsVisible}");
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private DockItemControl CreateDockItemControl(DockItem dockItem)
    {
        var control = new DockItemControl(dockItem, _applicationService, _windowService);
        
        // Store control for minimize animation
        _dockItemControls[dockItem.ExecutablePath] = control;
        
        // Subscribe to events
        control.ItemReorderRequested += DockItemControl_ItemReorderRequested;
        control.ItemRemoveRequested += DockItemControl_ItemRemoveRequested;
        control.ItemLaunchRequested += DockItemControl_ItemLaunchRequested;
        
        // Start monitoring for minimize if enabled
        if (_settings.MinimizeToDock)
        {
            _minimizeMonitor.StartMonitoring(dockItem.ExecutablePath);
        }
        
        return control;
    }
    
    private DockItemControl? CreateTrashControl()
    {
        try
        {
            var trashIcon = _trashService.GetRecycleBinIcon();
            if (trashIcon == null)
                return null;
            
            // Create a special DockItem for trash
            var trashItem = new DockItem
            {
                Id = "TRASH_CAN",
                Name = "回收站",
                ExecutablePath = _trashService.GetRecycleBinPath(),
                Order = int.MaxValue, // Always last
                IsTrashCan = true
            };
            
            var control = new DockItemControl(trashItem, _applicationService, _windowService);
            control.SetTrashService(_trashService);
            
            return control;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error creating trash control: {ex.Message}", ex);
            return null;
        }
    }

    private async void DockItemControl_ItemReorderRequested(object? sender, DockItemReorderEventArgs e)
    {
        try
        {
            var draggedItem = e.DraggedItem;
            var targetItem = e.TargetItem;
            
            // Find indices
            var draggedIndex = _settings.DockItems.FindIndex(d => d.Id == draggedItem.Id);
            var targetIndex = _settings.DockItems.FindIndex(d => d.Id == targetItem.Id);
            
            if (draggedIndex >= 0 && targetIndex >= 0 && draggedIndex != targetIndex)
            {
                // Remove dragged item
                var item = _settings.DockItems[draggedIndex];
                _settings.DockItems.RemoveAt(draggedIndex);
                
                // Insert at target position
                var newIndex = draggedIndex < targetIndex ? targetIndex : targetIndex + 1;
                _settings.DockItems.Insert(newIndex, item);
                
                // Update order values
                for (int i = 0; i < _settings.DockItems.Count; i++)
                {
                    _settings.DockItems[i].Order = i;
                }
                
                await _settings.SaveAsync();
                UpdateDockItems();
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"DockWindow: Reordered item {draggedItem.Name} to position {newIndex}");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error reordering item: {ex.Message}", ex);
        }
    }

    private async void DockItemControl_ItemRemoveRequested(object? sender, DockItemEventArgs e)
    {
        try
        {
            var itemToRemove = _settings.DockItems.FirstOrDefault(d => d.Id == e.Item.Id);
            if (itemToRemove != null)
            {
                _settings.DockItems.Remove(itemToRemove);
                await _settings.SaveAsync();
                UpdateDockItems();
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"DockWindow: Removed item {itemToRemove.Name}");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error removing item: {ex.Message}", ex);
        }
    }

    private void DockItemControl_ItemLaunchRequested(object? sender, DockItemEventArgs e)
    {
        // Refresh items after launch to update running status
        _ = Task.Delay(1000).ContinueWith(_ =>
        {
            Dispatcher.Invoke(() => UpdateDockItems());
        });
    }

    private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isMouseOver = true;
        _autoHideTimer?.Stop();
        
        if (_settings.AutoHide)
        {
            // Stop any hide animation
            var hideAnimation = (Storyboard)Resources["HideAnimation"];
            hideAnimation.Stop(this);
            
            // Show window
            var showAnimation = (Storyboard)Resources["ShowAnimation"];
            showAnimation.Begin(this);
        }
    }

    private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isMouseOver = false;
        
        // Reset 3D perspective effect
        if (_settings.Enable3DPerspective)
        {
            Reset3DPerspective();
        }
        
        if (_settings.AutoHide)
        {
            StartAutoHideTimer();
        }
    }
    
    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_settings.Enable3DPerspective && _isMouseOver)
        {
            Update3DPerspective(e.GetPosition(this));
        }
    }
    
    private void Update3DPerspective(Point mousePosition)
    {
        try
        {
            var dockCenterX = ActualWidth / 2;
            var mouseX = mousePosition.X;
            
            // Calculate distance from center
            var maxDistance = dockCenterX;
            var distanceFromCenter = Math.Abs(mouseX - dockCenterX);
            
            // Update each dock item
            foreach (var item in _dockItemsControl.Items)
            {
                if (item is DockItemControl control)
                {
                    var itemPosition = control.TransformToAncestor(this).Transform(new Point(control.ActualWidth / 2, 0));
                    var itemCenterX = itemPosition.X;
                    var distance = Math.Abs(mouseX - itemCenterX);
                    
                    // Calculate scale based on distance (closer = larger)
                    var normalizedDistance = Math.Min(distance / maxDistance, 1.0);
                    var scale = 1.0 + (1.0 - normalizedDistance) * 0.4; // Max 1.4x scale
                    
                    // Calculate vertical offset for arc effect
                    var verticalOffset = (1.0 - normalizedDistance) * 15; // Max 15px up
                    
                    // Apply with animation
                    control.Apply3DTransform(scale, verticalOffset);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error updating 3D perspective: {ex.Message}", ex);
        }
    }
    
    private void Reset3DPerspective()
    {
        foreach (var item in _dockItemsControl.Items)
        {
            if (item is DockItemControl control)
            {
                control.Apply3DTransform(1.0, 0);
            }
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        // Keep window visible even when deactivated
    }

    private void StartAutoHideTimer()
    {
        if (!_settings.AutoHide)
            return;

        _autoHideTimer?.Stop();
        _autoHideTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_settings.AutoHideDelay)
        };
        _autoHideTimer.Tick += AutoHideTimer_Tick;
        _autoHideTimer.Start();
    }

    private void AutoHideTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isMouseOver && _settings.AutoHide)
        {
            _autoHideTimer?.Stop();
            var hideAnimation = (Storyboard)Resources["HideAnimation"];
            hideAnimation.Begin(this);
        }
    }
}

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.DockLauncher.Services;
using LenovoLegionToolkit.Plugins.DockLauncher.Services.Settings;

namespace LenovoLegionToolkit.Plugins.DockLauncher;

/// <summary>
/// Dock window - macOS-style application launcher
/// </summary>
public partial class DockWindow : Window
{
    private readonly DockSettings _settings;
    private readonly IApplicationService _applicationService;
    private readonly IWindowService _windowService;
    private System.Windows.Threading.DispatcherTimer? _autoHideTimer;
    private bool _isMouseOver;

    public DockWindow()
    {
        InitializeComponent();
        
        _settings = new DockSettings();
        _applicationService = new ApplicationService();
        _windowService = new WindowService();
        
        DataContext = this;
        
        Loaded += DockWindow_Loaded;
        Closed += DockWindow_Closed;
    }

    private async void DockWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Loading settings and initializing");
            
            await _settings.LoadAsync();
            
            // Ensure window is visible initially
            Opacity = 1.0;
            Visibility = Visibility.Visible;
            ShowActivated = false; // Don't steal focus
            
            UpdateDockItems();
            
            // Update position after window is loaded and has actual size
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdatePosition();
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"DockWindow: Position updated - Left={Left}, Top={Top}, Width={ActualWidth}, Height={ActualHeight}");
                
                // Start auto-hide timer if enabled (with delay)
                if (_settings.AutoHide)
                {
                    // Delay auto-hide start to give user time to see the dock
                    var delayTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(_settings.AutoHideDelay + 2) // Extra 2 seconds initial delay
                    };
                    delayTimer.Tick += (s, args) =>
                    {
                        delayTimer.Stop();
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"DockWindow: Starting auto-hide timer");
                        StartAutoHideTimer();
                    };
                    delayTimer.Start();
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Error in Loaded event: {ex.Message}", ex);
        }
    }

    public void RefreshDockItems()
    {
        UpdateDockItems();
    }

    private void DockWindow_Closed(object? sender, EventArgs e)
    {
        _autoHideTimer?.Stop();
    }

    private void UpdatePosition()
    {
        try
        {
            var screen = Screen.PrimaryScreen;
            if (screen != null)
            {
                var workingArea = screen.WorkingArea;
                
                // Force layout update to get actual size
                UpdateLayout();
                
                var windowWidth = ActualWidth > 0 ? ActualWidth : (Width > 0 ? Width : 400);
                var windowHeight = ActualHeight > 0 ? ActualHeight : (Height > 0 ? Height : 80);
                
                Left = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                Top = workingArea.Bottom - windowHeight - 10;
                
                if (Top < 0)
                    Top = 10;
                
                // Ensure window is visible
                Visibility = Visibility.Visible;
                Opacity = 1.0;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating dock window position: {ex.Message}", ex);
        }
    }

    private void UpdateDockItems()
    {
        _dockItemsControl.Items.Clear();
        
        if (_settings.DockItems == null || _settings.DockItems.Count == 0)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: No dock items to display");
            // Add a placeholder or keep empty - window will still be visible with min width
        }
        else
        {
            foreach (var dockItem in _settings.DockItems.OrderBy(d => d.Order))
            {
                var control = new DockItemControl(dockItem, _applicationService, _windowService);
                _dockItemsControl.Items.Add(control);
            }
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"DockWindow: Added {_settings.DockItems.Count} dock items");
        }
        
        // Update position after items are added
        Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdatePosition();
        }), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isMouseOver = true;
        _autoHideTimer?.Stop();
        
        if (_settings.AutoHide)
        {
            // Stop any hide animation
            var hideAnimation = (Storyboard)Resources["HideAnimation"];
            hideAnimation.Stop(this);
            
            // Show window
            var showAnimation = (Storyboard)Resources["ShowAnimation"];
            showAnimation.Begin(this);
        }
    }

    private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isMouseOver = false;
        
        if (_settings.AutoHide)
        {
            StartAutoHideTimer();
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        // Keep window visible even when deactivated
    }

    private void StartAutoHideTimer()
    {
        if (!_settings.AutoHide)
            return;

        _autoHideTimer?.Stop();
        _autoHideTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_settings.AutoHideDelay)
        };
        _autoHideTimer.Tick += AutoHideTimer_Tick;
        _autoHideTimer.Start();
    }

    private void AutoHideTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isMouseOver && _settings.AutoHide)
        {
            _autoHideTimer?.Stop();
            var hideAnimation = (Storyboard)Resources["HideAnimation"];
            hideAnimation.Begin(this);
        }
    }

}

