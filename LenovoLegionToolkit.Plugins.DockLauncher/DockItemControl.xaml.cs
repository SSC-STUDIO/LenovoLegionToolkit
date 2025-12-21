using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.DockLauncher.Services;
using LenovoLegionToolkit.Plugins.DockLauncher.Services.Settings;

namespace LenovoLegionToolkit.Plugins.DockLauncher;

/// <summary>
/// Dock item control with full macOS-style features
/// </summary>
public partial class DockItemControl : UserControl
{
    private readonly DockItem _dockItem;
    private readonly IApplicationService _applicationService;
    private readonly IWindowService _windowService;
    private readonly DockSettings _settings;
    private readonly WindowThumbnailService _thumbnailService;
    private static MagnifiedIconPopup? _magnifiedIconPopup;
    private TrashService? _trashService;
    private System.Windows.Threading.DispatcherTimer? _updateTimer;
    private System.Windows.Threading.DispatcherTimer? _previewDelayTimer;
    private Point _dragStartPoint;
    private bool _isDragging;
    private DateTime _lastClickTime = DateTime.MinValue;
    private const double DoubleClickInterval = 500; // milliseconds
    private const int PreviewDelayMs = 500; // Delay before showing preview to avoid flickering

    public event EventHandler<DockItemReorderEventArgs>? ItemReorderRequested;
    public event EventHandler<DockItemEventArgs>? ItemRemoveRequested;
    public event EventHandler<DockItemEventArgs>? ItemLaunchRequested;
    
    /// <summary>
    /// Apply 3D perspective transform
    /// </summary>
    public void Apply3DTransform(double scale, double verticalOffset)
    {
        if (!_settings.Enable3DPerspective)
            return;
            
        // Animate scale
        var scaleAnimation = new DoubleAnimation
        {
            To = scale,
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        MainScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
        MainScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        
        // Animate vertical offset
        var offsetAnimation = new DoubleAnimation
        {
            To = -verticalOffset, // Negative to move up
            Duration = TimeSpan.FromMilliseconds(150),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        MainTranslateTransform.BeginAnimation(TranslateTransform.YProperty, offsetAnimation);
    }

    public DockItemControl(DockItem dockItem, IApplicationService applicationService, IWindowService windowService)
    {
        InitializeComponent();
        
        _dockItem = dockItem;
        _applicationService = applicationService;
        _windowService = windowService;
        _settings = new DockSettings();
        _thumbnailService = new WindowThumbnailService();
        
        // Create shared magnified icon popup if not exists
        if (_magnifiedIconPopup == null)
        {
            _magnifiedIconPopup = new MagnifiedIconPopup();
        }
        
        DataContext = this;
        
        Loaded += DockItemControl_Loaded;
        Unloaded += DockItemControl_Unloaded;
    }
    
    public void SetTrashService(TrashService trashService)
    {
        _trashService = trashService;
    }

    private async void DockItemControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Load icon
        ImageSource? icon = null;
        if (_dockItem.IsTrashCan && _trashService != null)
        {
            icon = _trashService.GetRecycleBinIcon();
        }
        else
        {
            icon = _applicationService.GetApplicationIcon(_dockItem.ExecutablePath);
        }
        
        if (icon != null)
        {
            _iconImage.Source = icon;
        }

        // Start update timer to check running status
        _updateTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
        
        await _settings.LoadAsync();
        UpdateRunningStatus();
        UpdateContextMenu();
    }

    private void DockItemControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _updateTimer?.Stop();
        _previewDelayTimer?.Stop();
        _previewPopup.IsOpen = false;
    }
    
    private void PreviewPopup_MouseEnter(object sender, MouseEventArgs e)
    {
        // Keep preview open when mouse is over it
        _previewDelayTimer?.Stop();
    }
    
    private void PreviewPopup_MouseLeave(object sender, MouseEventArgs e)
    {
        // Hide preview when mouse leaves, but check if mouse moved back to control
        var hideTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        hideTimer.Tick += (s, args) =>
        {
            hideTimer.Stop();
            // Only hide if mouse is not over control or popup
            if (!IsMouseOver && !_previewPopup.IsMouseOver)
            {
                _previewPopup.IsOpen = false;
            }
        };
        hideTimer.Start();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        UpdateRunningStatus();
    }

    private void UpdateRunningStatus()
    {
        try
        {
            var isRunning = _applicationService.IsApplicationRunning(_dockItem.ExecutablePath);
            _runningIndicator.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
            
            // Update stack indicator for multiple windows
            if (isRunning)
            {
                var processIds = _applicationService.GetRunningProcessIds(_dockItem.ExecutablePath);
                var windowCount = 0;
                
                foreach (var processId in processIds)
                {
                    var windows = _windowService.GetWindowsForProcess(processId);
                    windowCount += windows.Count(w => w.IsVisible);
                }
                
                if (windowCount > 1)
                {
                    _stackIndicator.Visibility = Visibility.Visible;
                    _stackCount.Text = windowCount.ToString();
                }
                else
                {
                    _stackIndicator.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                _stackIndicator.Visibility = Visibility.Collapsed;
            }
            
            // Update notification badge (placeholder for future implementation)
            UpdateNotificationBadge();
            
            // Update context menu based on running status
            if (_contextMenu != null)
            {
                _quitMenuItem.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating running status: {ex.Message}", ex);
        }
    }
    
    private void UpdateNotificationBadge()
    {
        // Placeholder for notification badge
        // In a full implementation, this would check for app notifications
        // For now, we'll hide it
        _notificationBadge.Visibility = Visibility.Collapsed;
    }
    
    /// <summary>
    /// Set notification badge count
    /// </summary>
    public void SetNotificationBadge(int count)
    {
        if (count > 0)
        {
            _badgeCount.Text = count > 99 ? "99+" : count.ToString();
            _notificationBadge.Visibility = Visibility.Visible;
        }
        else
        {
            _notificationBadge.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateContextMenu()
    {
        if (_contextMenu == null) return;
        
        var isRunning = _applicationService.IsApplicationRunning(_dockItem.ExecutablePath);
        _quitMenuItem.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UserControl_MouseEnter(object sender, MouseEventArgs e)
    {
        // Show hover animation
        var hoverAnimation = (Storyboard)Resources["HoverAnimation"];
        hoverAnimation.Begin(this);
        
        // Cancel any pending preview hide
        _previewDelayTimer?.Stop();
        
        // Show window preview with delay to avoid flickering
        if (_applicationService.IsApplicationRunning(_dockItem.ExecutablePath) && _settings.ShowWindowPreview)
        {
            _previewDelayTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(PreviewDelayMs)
            };
            _previewDelayTimer.Tick += (s, args) =>
            {
                _previewDelayTimer.Stop();
                ShowWindowPreview();
            };
            _previewDelayTimer.Start();
        }
    }

    private void UserControl_MouseLeave(object sender, MouseEventArgs e)
    {
        // Hide hover animation
        var leaveAnimation = (Storyboard)Resources["LeaveAnimation"];
        leaveAnimation.Begin(this);
        
        // Cancel pending preview show
        _previewDelayTimer?.Stop();
        
        // Hide preview immediately if mouse left the control
        // (The preview popup itself will handle mouse enter/leave)
        if (!_previewPopup.IsMouseOver)
        {
            _previewPopup.IsOpen = false;
        }
    }

    private void ShowWindowPreview()
    {
        try
        {
            // Don't show if mouse is no longer over the control
            if (!IsMouseOver)
                return;
            
            var processIds = _applicationService.GetRunningProcessIds(_dockItem.ExecutablePath);
            var windowHandles = new List<nint>();
            var windowTitles = new Dictionary<nint, string>();
            
            foreach (var processId in processIds)
            {
                var windowList = _windowService.GetWindowsForProcess(processId);
                foreach (var window in windowList.Where(w => w.IsVisible && !string.IsNullOrWhiteSpace(w.Title)))
                {
                    windowHandles.Add(window.Handle);
                    windowTitles[window.Handle] = window.Title;
                }
            }
            
            if (windowHandles.Count > 0)
            {
                // Get thumbnails for windows
                var thumbnails = _thumbnailService.GetWindowThumbnails(windowHandles.Take(5).ToList(), 180, 120);
                
                // Create thumbnail info objects
                var thumbnailInfos = new List<WindowThumbnailInfo>();
                foreach (var thumbnail in thumbnails)
                {
                    if (windowTitles.TryGetValue(thumbnail.WindowHandle, out var title))
                    {
                        thumbnail.WindowTitle = title;
                    }
                    thumbnailInfos.Add(thumbnail);
                }
                
                if (thumbnailInfos.Count > 0)
                {
                    _previewTitle.Text = thumbnailInfos.Count == 1 ? thumbnailInfos[0].WindowTitle : $"{thumbnailInfos.Count} 个窗口";
                    _previewTitle.Visibility = thumbnailInfos.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                    _previewWindows.ItemsSource = thumbnailInfos;
                    
                    // Only open if mouse is still over
                    if (IsMouseOver)
                    {
                        _previewPopup.IsOpen = true;
                    }
                }
                else
                {
                    _previewPopup.IsOpen = false;
                }
            }
            else
            {
                _previewPopup.IsOpen = false;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error showing window preview: {ex.Message}", ex);
        }
    }
    
    private void WindowThumbnail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is WindowThumbnailInfo thumbnailInfo)
        {
            _windowService.BringToForeground(thumbnailInfo.WindowHandle);
            _previewPopup.IsOpen = false;
        }
    }

    private async void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        e.Handled = true;
        
        // Check for double-click
        var now = DateTime.Now;
        var isDoubleClick = (now - _lastClickTime).TotalMilliseconds < DoubleClickInterval;
        _lastClickTime = now;
        
        if (isDoubleClick)
        {
            // Double-click: launch new instance
            await LaunchApplication();
            return;
        }
        
        // Single click: toggle window or launch
        try
        {
            var isRunning = _applicationService.IsApplicationRunning(_dockItem.ExecutablePath);
            
            if (isRunning)
            {
                // Toggle windows: minimize if visible, restore if minimized
                var processIds = _applicationService.GetRunningProcessIds(_dockItem.ExecutablePath);
                var hasVisibleWindow = false;
                
                foreach (var processId in processIds)
                {
                    var windows = _windowService.GetWindowsForProcess(processId);
                    foreach (var window in windows)
                    {
                        if (window.IsMinimized)
                        {
                            _windowService.RestoreWindow(window.Handle);
                            hasVisibleWindow = true;
                        }
                        else if (window.IsVisible)
                        {
                            _windowService.MinimizeWindow(window.Handle);
                            hasVisibleWindow = true;
                        }
                    }
                }
                
                // If no visible windows, launch new instance
                if (!hasVisibleWindow)
                {
                    await LaunchApplication();
                }
            }
            else
            {
                // Launch application with bounce animation
                await LaunchApplication();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error handling dock item click: {ex.Message}", ex);
        }
    }

    private async Task LaunchApplication()
    {
        // Play bounce animation if enabled
        if (_settings.EnableAnimations)
        {
            var bounceAnimation = (Storyboard)Resources["BounceAnimation"];
            bounceAnimation.Begin(this);
        }
        
        // Launch application
        var success = await _applicationService.LaunchApplicationAsync(_dockItem.ExecutablePath);
        
        if (success)
        {
            ItemLaunchRequested?.Invoke(this, new DockItemEventArgs(_dockItem));
        }
    }
    
    /// <summary>
    /// Play minimize animation when window is minimized to dock
    /// </summary>
    public void PlayMinimizeAnimation()
    {
        if (_settings.EnableAnimations)
        {
            var minimizeAnimation = (Storyboard)Resources["MinimizeAnimation"];
            minimizeAnimation.Begin(this);
        }
    }

    private void UserControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        UpdateContextMenu();
        _contextMenu.IsOpen = true;
        e.Handled = true;
    }

    private void UserControl_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
        {
            var currentPoint = e.GetPosition(this);
            var distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) + 
                                   Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));
            
            if (distance > 5) // Start dragging after 5 pixels
            {
                _isDragging = true;
                var data = new DataObject("DockItem", _dockItem);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                _isDragging = false;
            }
        }
    }

    private void UserControl_DragEnter(object sender, DragEventArgs e)
    {
        // Handle DockItem reordering
        if (e.Data.GetDataPresent("DockItem"))
        {
            if (!_dockItem.IsTrashCan)
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            return;
        }
        
        // Handle file drag to trash can
        if (_dockItem.IsTrashCan && _trashService != null)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            return;
        }
        
        // Handle file drag to application
        if (!_dockItem.IsTrashCan && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void UserControl_DragOver(object sender, DragEventArgs e)
    {
        // Handle DockItem reordering
        if (e.Data.GetDataPresent("DockItem"))
        {
            if (!_dockItem.IsTrashCan)
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            return;
        }
        
        // Handle file drag to trash can
        if (_dockItem.IsTrashCan && _trashService != null)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            return;
        }
        
        // Handle file drag to application
        if (!_dockItem.IsTrashCan && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void UserControl_Drop(object sender, DragEventArgs e)
    {
        // Handle DockItem reordering
        if (e.Data.GetDataPresent("DockItem"))
        {
            if (!_dockItem.IsTrashCan)
            {
                var draggedItem = e.Data.GetData("DockItem") as DockItem;
                if (draggedItem != null && draggedItem.Id != _dockItem.Id)
                {
                    ItemReorderRequested?.Invoke(this, new DockItemReorderEventArgs(draggedItem, _dockItem));
                }
            }
            e.Handled = true;
            return;
        }
        
        // Handle file drop to trash can
        if (_dockItem.IsTrashCan && _trashService != null && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                _trashService.MoveToRecycleBin(file);
            }
            e.Handled = true;
            return;
        }
        
        // Handle file drop to application
        if (!_dockItem.IsTrashCan && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            _ = LaunchApplicationWithFiles(files);
            e.Handled = true;
        }
    }
    
    private void UserControl_DragLeave(object sender, DragEventArgs e)
    {
        // Reset visual feedback if needed
    }
    
    private async Task LaunchApplicationWithFiles(string[] files)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _dockItem.ExecutablePath,
                UseShellExecute = true
            };
            
            // Add files as arguments
            foreach (var file in files)
            {
                processStartInfo.ArgumentList.Add(file);
            }
            
            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error launching application with files: {ex.Message}", ex);
        }
    }

    private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _ = LaunchApplication();
    }

    private void ShowInExplorerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var directory = Path.GetDirectoryName(_dockItem.ExecutablePath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                Process.Start("explorer.exe", $"/select,\"{_dockItem.ExecutablePath}\"");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error showing in explorer: {ex.Message}", ex);
        }
    }

    private void OptionsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Show options dialog
    }

    private void QuitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var processIds = _applicationService.GetRunningProcessIds(_dockItem.ExecutablePath);
            foreach (var processId in processIds)
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    process.CloseMainWindow();
                    
                    // Force kill if not closed within 3 seconds
                    if (!process.WaitForExit(3000))
                    {
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error quitting process {processId}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error quitting application: {ex.Message}", ex);
        }
    }

    private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ItemRemoveRequested?.Invoke(this, new DockItemEventArgs(_dockItem));
    }
}

/// <summary>
/// Event args for dock item reorder
/// </summary>
public class DockItemReorderEventArgs : EventArgs
{
    public DockItem DraggedItem { get; }
    public DockItem TargetItem { get; }
    
    public DockItemReorderEventArgs(DockItem draggedItem, DockItem targetItem)
    {
        DraggedItem = draggedItem;
        TargetItem = targetItem;
    }
}

/// <summary>
/// Event args for dock item actions
/// </summary>
public class DockItemEventArgs : EventArgs
{
    public DockItem Item { get; }
    
    public DockItemEventArgs(DockItem item)
    {
        Item = item;
    }
}

    {
        // Hide preview when mouse leaves, but check if mouse moved back to control
        var hideTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(150)
        };
        hideTimer.Tick += (s, args) =>
        {
            hideTimer.Stop();
            // Only hide if mouse is not over control or popup
            if (!IsMouseOver && !_previewPopup.IsMouseOver)
            {
                _previewPopup.IsOpen = false;
            }
        };
        hideTimer.Start();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        UpdateRunningStatus();
    }

    private void UpdateRunningStatus()
    {
        try
        {
            var isRunning = _applicationService.IsApplicationRunning(_dockItem.ExecutablePath);
            _runningIndicator.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
            
            // Update stack indicator for multiple windows
            if (isRunning)
            {
                var processIds = _applicationService.GetRunningProcessIds(_dockItem.ExecutablePath);
                var windowCount = 0;
                
                foreach (var processId in processIds)
                {
                    var windows = _windowService.GetWindowsForProcess(processId);
                    windowCount += windows.Count(w => w.IsVisible);
                }
                
                if (windowCount > 1)
                {
                    _stackIndicator.Visibility = Visibility.Visible;
                    _stackCount.Text = windowCount.ToString();
                }
                else
                {
                    _stackIndicator.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                _stackIndicator.Visibility = Visibility.Collapsed;
            }
            
            // Update notification badge (placeholder for future implementation)
            UpdateNotificationBadge();
            
            // Update context menu based on running status
            if (_contextMenu != null)
            {
                _quitMenuItem.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating running status: {ex.Message}", ex);
        }
    }
    
    private void UpdateNotificationBadge()
    {
        // Placeholder for notification badge
        // In a full implementation, this would check for app notifications
        // For now, we'll hide it
        _notificationBadge.Visibility = Visibility.Collapsed;
    }
    
    /// <summary>
    /// Set notification badge count
    /// </summary>
    public void SetNotificationBadge(int count)
    {
        if (count > 0)
        {
            _badgeCount.Text = count > 99 ? "99+" : count.ToString();
            _notificationBadge.Visibility = Visibility.Visible;
        }
        else
        {
            _notificationBadge.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateContextMenu()
    {
        if (_contextMenu == null) return;
        
        var isRunning = _applicationService.IsApplicationRunning(_dockItem.ExecutablePath);
        _quitMenuItem.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UserControl_MouseEnter(object sender, MouseEventArgs e)
    {
        // Show hover animation
        var hoverAnimation = (Storyboard)Resources["HoverAnimation"];
        hoverAnimation.Begin(this);
        
        // Cancel any pending preview hide
        _previewDelayTimer?.Stop();
        
        // Show window preview with delay to avoid flickering
        if (_applicationService.IsApplicationRunning(_dockItem.ExecutablePath) && _settings.ShowWindowPreview)
        {
            _previewDelayTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(PreviewDelayMs)
            };
            _previewDelayTimer.Tick += (s, args) =>
            {
                _previewDelayTimer.Stop();
                ShowWindowPreview();
            };
            _previewDelayTimer.Start();
        }
    }

    private void UserControl_MouseLeave(object sender, MouseEventArgs e)
    {
        // Hide hover animation
        var leaveAnimation = (Storyboard)Resources["LeaveAnimation"];
        leaveAnimation.Begin(this);
        
        // Cancel pending preview show
        _previewDelayTimer?.Stop();
        
        // Hide preview immediately if mouse left the control
        // (The preview popup itself will handle mouse enter/leave)
        if (!_previewPopup.IsMouseOver)
        {
            _previewPopup.IsOpen = false;
        }
    }

    private void ShowWindowPreview()
    {
        try
        {
            // Don't show if mouse is no longer over the control
            if (!IsMouseOver)
                return;
            
            var processIds = _applicationService.GetRunningProcessIds(_dockItem.ExecutablePath);
            var windowHandles = new List<nint>();
            var windowTitles = new Dictionary<nint, string>();
            
            foreach (var processId in processIds)
            {
                var windowList = _windowService.GetWindowsForProcess(processId);
                foreach (var window in windowList.Where(w => w.IsVisible && !string.IsNullOrWhiteSpace(w.Title)))
                {
                    windowHandles.Add(window.Handle);
                    windowTitles[window.Handle] = window.Title;
                }
            }
            
            if (windowHandles.Count > 0)
            {
                // Get thumbnails for windows
                var thumbnails = _thumbnailService.GetWindowThumbnails(windowHandles.Take(5).ToList(), 180, 120);
                
                // Create thumbnail info objects
                var thumbnailInfos = new List<WindowThumbnailInfo>();
                foreach (var thumbnail in thumbnails)
                {
                    if (windowTitles.TryGetValue(thumbnail.WindowHandle, out var title))
                    {
                        thumbnail.WindowTitle = title;
                    }
                    thumbnailInfos.Add(thumbnail);
                }
                
                if (thumbnailInfos.Count > 0)
                {
                    _previewTitle.Text = thumbnailInfos.Count == 1 ? thumbnailInfos[0].WindowTitle : $"{thumbnailInfos.Count} 个窗口";
                    _previewTitle.Visibility = thumbnailInfos.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
                    _previewWindows.ItemsSource = thumbnailInfos;
                    
                    // Only open if mouse is still over
                    if (IsMouseOver)
                    {
                        _previewPopup.IsOpen = true;
                    }
                }
                else
                {
                    _previewPopup.IsOpen = false;
                }
            }
            else
            {
                _previewPopup.IsOpen = false;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error showing window preview: {ex.Message}", ex);
        }
    }
    
    private void WindowThumbnail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is WindowThumbnailInfo thumbnailInfo)
        {
            _windowService.BringToForeground(thumbnailInfo.WindowHandle);
            _previewPopup.IsOpen = false;
        }
    }

    private async void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        e.Handled = true;
        
        // Check for double-click
        var now = DateTime.Now;
        var isDoubleClick = (now - _lastClickTime).TotalMilliseconds < DoubleClickInterval;
        _lastClickTime = now;
        
        if (isDoubleClick)
        {
            // Double-click: launch new instance
            await LaunchApplication();
            return;
        }
        
        // Single click: toggle window or launch
        try
        {
            var isRunning = _applicationService.IsApplicationRunning(_dockItem.ExecutablePath);
            
            if (isRunning)
            {
                // Toggle windows: minimize if visible, restore if minimized
                var processIds = _applicationService.GetRunningProcessIds(_dockItem.ExecutablePath);
                var hasVisibleWindow = false;
                
                foreach (var processId in processIds)
                {
                    var windows = _windowService.GetWindowsForProcess(processId);
                    foreach (var window in windows)
                    {
                        if (window.IsMinimized)
                        {
                            _windowService.RestoreWindow(window.Handle);
                            hasVisibleWindow = true;
                        }
                        else if (window.IsVisible)
                        {
                            _windowService.MinimizeWindow(window.Handle);
                            hasVisibleWindow = true;
                        }
                    }
                }
                
                // If no visible windows, launch new instance
                if (!hasVisibleWindow)
                {
                    await LaunchApplication();
                }
            }
            else
            {
                // Launch application with bounce animation
                await LaunchApplication();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error handling dock item click: {ex.Message}", ex);
        }
    }

    private async Task LaunchApplication()
    {
        // Play bounce animation if enabled
        if (_settings.EnableAnimations)
        {
            var bounceAnimation = (Storyboard)Resources["BounceAnimation"];
            bounceAnimation.Begin(this);
        }
        
        // Launch application
        var success = await _applicationService.LaunchApplicationAsync(_dockItem.ExecutablePath);
        
        if (success)
        {
            ItemLaunchRequested?.Invoke(this, new DockItemEventArgs(_dockItem));
        }
    }
    
    /// <summary>
    /// Play minimize animation when window is minimized to dock
    /// </summary>
    public void PlayMinimizeAnimation()
    {
        if (_settings.EnableAnimations)
        {
            var minimizeAnimation = (Storyboard)Resources["MinimizeAnimation"];
            minimizeAnimation.Begin(this);
        }
    }

    private void UserControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        UpdateContextMenu();
        _contextMenu.IsOpen = true;
        e.Handled = true;
    }

    private void UserControl_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
        {
            var currentPoint = e.GetPosition(this);
            var distance = Math.Sqrt(Math.Pow(currentPoint.X - _dragStartPoint.X, 2) + 
                                   Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));
            
            if (distance > 5) // Start dragging after 5 pixels
            {
                _isDragging = true;
                var data = new DataObject("DockItem", _dockItem);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                _isDragging = false;
            }
        }
    }

    private void UserControl_DragEnter(object sender, DragEventArgs e)
    {
        // Handle DockItem reordering
        if (e.Data.GetDataPresent("DockItem"))
        {
            if (!_dockItem.IsTrashCan)
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            return;
        }
        
        // Handle file drag to trash can
        if (_dockItem.IsTrashCan && _trashService != null)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            return;
        }
        
        // Handle file drag to application
        if (!_dockItem.IsTrashCan && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void UserControl_DragOver(object sender, DragEventArgs e)
    {
        // Handle DockItem reordering
        if (e.Data.GetDataPresent("DockItem"))
        {
            if (!_dockItem.IsTrashCan)
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            return;
        }
        
        // Handle file drag to trash can
        if (_dockItem.IsTrashCan && _trashService != null)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            return;
        }
        
        // Handle file drag to application
        if (!_dockItem.IsTrashCan && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }
    }

    private void UserControl_Drop(object sender, DragEventArgs e)
    {
        // Handle DockItem reordering
        if (e.Data.GetDataPresent("DockItem"))
        {
            if (!_dockItem.IsTrashCan)
            {
                var draggedItem = e.Data.GetData("DockItem") as DockItem;
                if (draggedItem != null && draggedItem.Id != _dockItem.Id)
                {
                    ItemReorderRequested?.Invoke(this, new DockItemReorderEventArgs(draggedItem, _dockItem));
                }
            }
            e.Handled = true;
            return;
        }
        
        // Handle file drop to trash can
        if (_dockItem.IsTrashCan && _trashService != null && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                _trashService.MoveToRecycleBin(file);
            }
            e.Handled = true;
            return;
        }
        
        // Handle file drop to application
        if (!_dockItem.IsTrashCan && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            _ = LaunchApplicationWithFiles(files);
            e.Handled = true;
        }
    }
    
    private void UserControl_DragLeave(object sender, DragEventArgs e)
    {
        // Reset visual feedback if needed
    }
    
    private async Task LaunchApplicationWithFiles(string[] files)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = _dockItem.ExecutablePath,
                UseShellExecute = true
            };
            
            // Add files as arguments
            foreach (var file in files)
            {
                processStartInfo.ArgumentList.Add(file);
            }
            
            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error launching application with files: {ex.Message}", ex);
        }
    }

    private void OpenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _ = LaunchApplication();
    }

    private void ShowInExplorerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var directory = Path.GetDirectoryName(_dockItem.ExecutablePath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                Process.Start("explorer.exe", $"/select,\"{_dockItem.ExecutablePath}\"");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error showing in explorer: {ex.Message}", ex);
        }
    }

    private void OptionsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Show options dialog
    }

    private void QuitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var processIds = _applicationService.GetRunningProcessIds(_dockItem.ExecutablePath);
            foreach (var processId in processIds)
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    process.CloseMainWindow();
                    
                    // Force kill if not closed within 3 seconds
                    if (!process.WaitForExit(3000))
                    {
                        process.Kill();
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error quitting process {processId}: {ex.Message}", ex);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error quitting application: {ex.Message}", ex);
        }
    }

    private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ItemRemoveRequested?.Invoke(this, new DockItemEventArgs(_dockItem));
    }
}

/// <summary>
/// Event args for dock item reorder
/// </summary>
public class DockItemReorderEventArgs : EventArgs
{
    public DockItem DraggedItem { get; }
    public DockItem TargetItem { get; }
    
    public DockItemReorderEventArgs(DockItem draggedItem, DockItem targetItem)
    {
        DraggedItem = draggedItem;
        TargetItem = targetItem;
    }
}

/// <summary>
/// Event args for dock item actions
/// </summary>
public class DockItemEventArgs : EventArgs
{
    public DockItem Item { get; }
    
    public DockItemEventArgs(DockItem item)
    {
        Item = item;
    }
}

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Plugins.DockLauncher.Services;
using LenovoLegionToolkit.Plugins.DockLauncher.Services.Settings;

namespace LenovoLegionToolkit.Plugins.DockLauncher;

/// <summary>
/// Dock item control for displaying application icons
/// </summary>
public partial class DockItemControl : UserControl
{
    private readonly DockItem _dockItem;
    private readonly IApplicationService _applicationService;
    private readonly IWindowService _windowService;
    private System.Windows.Threading.DispatcherTimer? _updateTimer;

    public DockItemControl(DockItem dockItem, IApplicationService applicationService, IWindowService windowService)
    {
        InitializeComponent();
        
        _dockItem = dockItem;
        _applicationService = applicationService;
        _windowService = windowService;
        
        Loaded += DockItemControl_Loaded;
        Unloaded += DockItemControl_Unloaded;
    }

    private void DockItemControl_Loaded(object sender, RoutedEventArgs e)
    {
        // Load icon
        var icon = _applicationService.GetApplicationIcon(_dockItem.ExecutablePath);
        if (icon != null)
        {
            _iconImage.Source = icon;
        }

        // Start update timer to check running status
        _updateTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
        
        UpdateRunningStatus();
    }

    private void DockItemControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _updateTimer?.Stop();
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        UpdateRunningStatus();
    }

    private void UpdateRunningStatus()
    {
        try
        {
            var isRunning = _applicationService.IsApplicationRunning(_dockItem.ExecutablePath);
            _runningIndicator.Visibility = isRunning ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating running status: {ex.Message}", ex);
        }
    }

    private void UserControl_MouseEnter(object sender, MouseEventArgs e)
    {
        var hoverAnimation = (Storyboard)Resources["HoverAnimation"];
        hoverAnimation.Begin(this);
    }

    private void UserControl_MouseLeave(object sender, MouseEventArgs e)
    {
        var leaveAnimation = (Storyboard)Resources["LeaveAnimation"];
        leaveAnimation.Begin(this);
    }

    private async void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        
        try
        {
            var isRunning = _applicationService.IsApplicationRunning(_dockItem.ExecutablePath);
            
            if (isRunning)
            {
                // Bring windows to foreground or minimize
                var processIds = _applicationService.GetRunningProcessIds(_dockItem.ExecutablePath);
                foreach (var processId in processIds)
                {
                    var windows = _windowService.GetWindowsForProcess(processId);
                    foreach (var window in windows)
                    {
                        if (window.IsMinimized)
                        {
                            _windowService.RestoreWindow(window.Handle);
                        }
                        else
                        {
                            _windowService.BringToForeground(window.Handle);
                        }
                    }
                }
            }
            else
            {
                // Launch application
                await _applicationService.LaunchApplicationAsync(_dockItem.ExecutablePath);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error handling dock item click: {ex.Message}", ex);
        }
    }
}

