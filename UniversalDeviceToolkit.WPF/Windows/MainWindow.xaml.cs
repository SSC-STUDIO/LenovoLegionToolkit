using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media.Animation;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.Messaging;
using LenovoLegionToolkit.Lib.Messaging.Messages;
using LenovoLegionToolkit.Lib.Plugins;
using PluginConstants = LenovoLegionToolkit.Lib.Plugins.PluginConstants;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Controls.Shell;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Pages;
using UniversalDeviceToolkit.WPF.ViewModels;
using NavigationItem = UniversalDeviceToolkit.WPF.Controls.Custom.NavigationItem;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using Microsoft.Xaml.Behaviors.Core;
using Windows.Win32;
using Windows.Win32.System.Threading;
using Wpf.Ui.Controls;
using Brush = System.Windows.Media.Brush;
#if !DEBUG
using System.Reflection;
using LenovoLegionToolkit.Lib.Extensions;
#endif

#pragma warning disable CA1416

namespace UniversalDeviceToolkit.WPF.Windows
{
public partial class MainWindow
{
    private const int WmNchittest = 0x0084;
    private const int WmNclbuttonUp = 0x00A2;
    private const int HtClient = 1;
    private const int HtMaxButton = 9;
    private const int MaxVisibleStatusNotifications = 4;

    private readonly ApplicationSettings _applicationSettings;
    private readonly IPluginManager _pluginManager;
    private readonly SpecialKeyListener _specialKeyListener;
    private readonly VantageDisabler _vantageDisabler;
    private readonly LegionZoneDisabler _legionZoneDisabler;
    private readonly FnKeysDisabler _fnKeysDisabler;
    private readonly UpdateChecker _updateChecker;

    private TrayHelper? _trayHelper;
    private readonly Dictionary<string, NavigationItem> _pluginNavigationItems = new();
    private readonly Snackbar _snackbar;
    private double _navigationSplitterWidth;

    public bool TrayTooltipEnabled { get; set; } = true;
    public bool SuppressClosingEventHandler { get; set; }

    public Snackbar Snackbar => _snackbar;

    public MainWindow(
        ApplicationSettings applicationSettings,
        IPluginManager pluginManager,
        SpecialKeyListener specialKeyListener,
        VantageDisabler vantageDisabler,
        LegionZoneDisabler legionZoneDisabler,
        FnKeysDisabler fnKeysDisabler,
        UpdateChecker updateChecker)
    {
        _applicationSettings = applicationSettings;
        _pluginManager = pluginManager;
        _specialKeyListener = specialKeyListener;
        _vantageDisabler = vantageDisabler;
        _legionZoneDisabler = legionZoneDisabler;
        _fnKeysDisabler = fnKeysDisabler;
        _updateChecker = updateChecker;

        InitializeComponent();
        _snackbar = CreateMainSnackbar(_snackbarPresenter);
        WireStatusNotificationStack();

        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
        IsVisibleChanged += MainWindow_IsVisibleChanged;
        Loaded += MainWindow_Loaded;
        SourceInitialized += MainWindow_SourceInitialized;
        StateChanged += MainWindow_StateChanged;
        _updateIndicator.MouseLeftButtonDown += UpdateIndicator_Click;
        _updateIndicator.MouseRightButtonDown += UpdateIndicator_Click;

#if DEBUG
        _title.Text += Debugger.IsAttached ? " [DEBUGGER ATTACHED]" : " [DEBUG]";
#else
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        if (version is not null && version.IsBeta())
            _title.Text += " [BETA]";
#endif

        if (Log.Instance.IsTraceEnabled)
        {
            _title.Text += " [LOGGING ENABLED]";
            _openLogIndicator.Visibility = Visibility.Visible;
        }

        Title = _title.Text;

        // Listen to Frame navigation events to update window title to current page title
        _rootFrame.Navigated += RootFrame_Navigated;

        // Subscribe to plugin state changed events
        _pluginManager.PluginStateChanged += PluginManager_PluginStateChanged;

        MessagingCenter.Subscribe<MainWindowVisibilityMessage>(this, message =>
            Dispatcher.Invoke(() => ApplyMainWindowVisibility(message.Action)));
    }

    private void ApplyMainWindowVisibility(MainWindowVisibilityAction action)
    {
        switch (action)
        {
            case MainWindowVisibilityAction.Show:
                Show();
                if (WindowState == WindowState.Minimized)
                    WindowState = WindowState.Normal;
                Activate();
                break;
            case MainWindowVisibilityAction.Hide:
                SendToTray();
                break;
        }
    }

    private static Snackbar CreateMainSnackbar(SnackbarPresenter presenter) =>
        NotificationToastFactory.Create(presenter);

    private void NavigationSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var collapsedWidth = GetNavigationWidthResource("NavigationWidthCollapsed", 70);
        var expandedWidth = GetNavigationWidthResource("NavigationWidthExpanded", 220);
        var currentWidth = _navigationSplitterWidth > 0 ? _navigationSplitterWidth : _navigationStore.ActualWidth;
        _navigationSplitterWidth = Math.Clamp(currentWidth + e.HorizontalChange, collapsedWidth, expandedWidth);

        _navigationStore.BeginAnimation(WidthProperty, null);
        _navigationStore.BeginAnimation(MinWidthProperty, null);
        _navigationStore.BeginAnimation(MaxWidthProperty, null);
        _navigationStore.Width = _navigationSplitterWidth;
        _navigationStore.MinWidth = _navigationSplitterWidth;
        _navigationStore.MaxWidth = _navigationSplitterWidth;
    }

    private void NavigationSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        var collapsedWidth = GetNavigationWidthResource("NavigationWidthCollapsed", 70);
        var expandedWidth = GetNavigationWidthResource("NavigationWidthExpanded", 220);
        var threshold = collapsedWidth + ((expandedWidth - collapsedWidth) / 2);
        var shouldExpand = (_navigationSplitterWidth > 0 ? _navigationSplitterWidth : _navigationStore.ActualWidth) >= threshold;
        _navigationSplitterWidth = 0;

        if (_navigationStore.IsExpanded != shouldExpand)
        {
            _navigationStore.IsExpanded = shouldExpand;
            return;
        }

        AnimateNavigationWidth(shouldExpand ? expandedWidth : collapsedWidth);
    }

    private void AnimateNavigationWidth(double targetWidth)
    {
        var from = _navigationStore.ActualWidth > 0 ? _navigationStore.ActualWidth : targetWidth;
        if (Math.Abs(from - targetWidth) < 0.5)
            return;

        var animation = new DoubleAnimation
        {
            From = from,
            To = targetWidth,
            Duration = Application.Current.TryFindResource("AnimationDurationMedium") is Duration duration
                ? duration
                : new Duration(TimeSpan.FromMilliseconds(220)),
            EasingFunction = Application.Current.TryFindResource("AnimationEasingCubicOut") as IEasingFunction,
            FillBehavior = FillBehavior.Stop
        };
        animation.Completed += (_, _) =>
        {
            _navigationStore.BeginAnimation(WidthProperty, null);
            _navigationStore.BeginAnimation(MinWidthProperty, null);
            _navigationStore.BeginAnimation(MaxWidthProperty, null);
            _navigationStore.Width = targetWidth;
            _navigationStore.MinWidth = targetWidth;
            _navigationStore.MaxWidth = targetWidth;
        };

        _navigationStore.BeginAnimation(WidthProperty, animation);
        _navigationStore.BeginAnimation(MinWidthProperty, animation.Clone());
        _navigationStore.BeginAnimation(MaxWidthProperty, animation.Clone());
    }

    private static double GetNavigationWidthResource(string key, double fallback)
    {
        return Application.Current.TryFindResource(key) is double width ? width : fallback;
    }
    private void RootFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
        // When page navigation is complete, update window title to: App Name - Page Title
        var appName = AppIdentity.DisplayName;

        if (e.Content is Page page && !string.IsNullOrWhiteSpace(page.Title))
        {
            Title = $"{appName} - {page.Title}";
            _title.Text = $"{appName} - {page.Title}";
        }
        else
        {
            Title = appName;
            _title.Text = appName;
        }

        if (e.Content is FrameworkElement entranceTarget)
            PageEntranceAnimator.Play(entranceTarget);
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        RestoreSize();

        if (PresentationSource.FromVisual(this) is HwndSource source)
            source.AddHook(MainWindowHwndSourceHook);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateNavigationVisibility();

        SmartKeyHelper.Instance.BringToForeground = () => Dispatcher.Invoke(BringToForeground);

        _specialKeyListener.Changed += SpecialKeyListener_Changed;

        _contentGrid.Visibility = Visibility.Visible;
        ShellChromeHelper.ApplyContentSurfaceEffects(_contentSurfaceBorder, _applicationSettings);

        _ = LoadDeviceInfo();
        UpdateIndicators();
        _ = CheckForUpdates();

        InputBindings.Add(new KeyBinding(new ActionCommand(_navigationStore.NavigateToNext), Key.Tab, ModifierKeys.Control));
        InputBindings.Add(new KeyBinding(new ActionCommand(_navigationStore.NavigateToPrevious), Key.Tab, ModifierKeys.Control | ModifierKeys.Shift));

        var key = (int)Key.D1;
        foreach (var item in _navigationStore.Items.OfType<NavigationItem>())
        {
            if (item.PageTag != null)
                InputBindings.Add(new KeyBinding(new ActionCommand(() => _navigationStore.Navigate(item.PageTag)), (Key)key++, ModifierKeys.Control));
        }
        
        // Set the plugin extensions navigation item text
        if (_pluginExtensionsItem != null)
        {
            _pluginExtensionsItem.Content = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, "MainWindow_NavigationItem_PluginExtensions", "Plugin Extensions", Resource.Culture);
        }

        _ = UpdateHardwareDependentNavigationAsync();
        _ = InitializeTrayAsync();
    }

    private async Task UpdateHardwareDependentNavigationAsync()
    {
        try
        {
            var mi = await MachineCompatibility.GetMachineInformationAsync();
            var deviceAvailability = MachineCompatibility.GetDeviceFeatureAvailability(mi);

            var hideKeyboardBacklight =
                deviceAvailability.HiddenFeatures.Contains("keyboard-backlight") ||
                !await KeyboardBacklightPage.IsSupportedAsync();

            if (hideKeyboardBacklight)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (_navigationStore.Items.Contains(_keyboardItem))
                        _navigationStore.Items.Remove(_keyboardItem);

                    UpdateNavigationVisibility();
                }).Task;
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to update hardware-dependent navigation.", ex);
        }
    }

    private async Task InitializeTrayAsync()
    {
        try
        {
            var trayHelper = new TrayHelper(_navigationStore, BringToForeground, TrayTooltipEnabled);
            await trayHelper.InitializeAsync();
            trayHelper.MakeVisible();
            _trayHelper = trayHelper;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to initialize tray helper.", ex);
        }
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Closing started...");

        var suppressClosingEventHandler = SuppressClosingEventHandler;
        var minimizeOnClose = !suppressClosingEventHandler && _applicationSettings.Store.MinimizeOnClose;

        // Cancel before awaiting persistence so the close request cannot slip through.
        if (minimizeOnClose)
            e.Cancel = true;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await SaveSizeAsync();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"SaveSize failed during closing.", ex);
        }
        finally
        {
            stopwatch.Stop();
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"SaveSize completed in {stopwatch.ElapsedMilliseconds}ms");

        try
        {
            if (suppressClosingEventHandler)
                return;

            if (minimizeOnClose)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Minimizing...");

                WindowState = WindowState.Minimized;
                return;
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Closing...");

            await App.Current.ShutdownAsync(true);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed while handling window close.", ex);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs args)
    {
        // Unsubscribe from window events
        Closing -= MainWindow_Closing;
        Closed -= MainWindow_Closed;
        IsVisibleChanged -= MainWindow_IsVisibleChanged;
        Loaded -= MainWindow_Loaded;
        SourceInitialized -= MainWindow_SourceInitialized;
        StateChanged -= MainWindow_StateChanged;
        _updateIndicator.MouseLeftButtonDown -= UpdateIndicator_Click;
        _updateIndicator.MouseRightButtonDown -= UpdateIndicator_Click;

        // Unsubscribe from frame navigation events
        if (_rootFrame is not null)
            _rootFrame.Navigated -= RootFrame_Navigated;

        // Unsubscribe from plugin manager events
        if (_pluginManager is not null)
            _pluginManager.PluginStateChanged -= PluginManager_PluginStateChanged;

        // Unsubscribe from disablers
        _vantageDisabler.OnRefreshed -= VantageDisabler_OnRefreshed;
        _legionZoneDisabler.OnRefreshed -= LegionZoneDisabler_OnRefreshed;
        _fnKeysDisabler.OnRefreshed -= FnKeysDisabler_OnRefreshed;

        // Unsubscribe from special key listener
        _specialKeyListener.Changed -= SpecialKeyListener_Changed;

        _trayHelper?.Dispose();
        _trayHelper = null;
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Window state changed to {WindowState}");

        switch (WindowState)
        {
            case WindowState.Minimized:
                SetEfficiencyMode(true);
                SendToTray();
                break;
            case WindowState.Normal:
                SetEfficiencyMode(false);
                BringToForeground();
                break;
        }
    }

    private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsVisible)
            return;

        _ = CheckForUpdates();
    }

    private void MainTitleBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TitleBar titleBar || e.ChangedButton != MouseButton.Left)
            return;

        var point = e.GetPosition(titleBar);
        var maximizeButtonLeft = titleBar.ActualWidth - 96;
        var maximizeButtonRight = titleBar.ActualWidth - 48;

        if (point.X < maximizeButtonLeft || point.X >= maximizeButtonRight)
            return;

        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
        e.Handled = true;
    }

    private void OpenLogIndicator_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        OpenLog();
    }

    private void OpenLogIndicator_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        OpenLog();
    }

    private void OpenLogIndicator_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
            return;

        e.Handled = true;
        OpenLog();
    }

    private void DeviceInfoIndicator_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        ShowDeviceInfoWindow();
    }

    private void DeviceInfoIndicator_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        ShowDeviceInfoWindow();
    }

    private void DeviceInfoIndicator_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
            return;

        e.Handled = true;
        ShowDeviceInfoWindow();
    }

    private void UpdateIndicator_Click(object sender, MouseButtonEventArgs e) => ShowUpdateWindow();

    private void UpdateIndicator_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
            return;

        ShowUpdateWindow();
    }

    private async Task LoadDeviceInfo()
    {
        try
        {
            var mi = await MachineCompatibility.GetMachineInformationAsync();
            _deviceInfoIndicatorText.Text = mi.Model;
            _deviceInfoIndicator.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to load device info: {ex.Message}", ex);
        }
    }

    private void UpdateIndicators()
    {
        _vantageDisabler.OnRefreshed += VantageDisabler_OnRefreshed;
        _legionZoneDisabler.OnRefreshed += LegionZoneDisabler_OnRefreshed;
        _fnKeysDisabler.OnRefreshed += FnKeysDisabler_OnRefreshed;

        Task.Run(async () =>
        {
            try
            {
                _ = await _vantageDisabler.GetStatusAsync();
                _ = await _legionZoneDisabler.GetStatusAsync();
                _ = await _fnKeysDisabler.GetStatusAsync();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to update disabler statuses.", ex);
            }
        });
    }

    private void SpecialKeyListener_Changed(object? sender, SpecialKeyListener.ChangedEventArgs e)
    {
        if (e.SpecialKey == SpecialKey.FnN)
            Dispatcher.BeginInvoke(BringToForeground);
    }

    private void VantageDisabler_OnRefreshed(object? sender, AbstractSoftwareDisabler.AbstractSoftwareDisablerEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _vantageIndicator.Visibility = e.Status == SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;
            EnforceStatusNotificationLimit();
        });
    }

    private void LegionZoneDisabler_OnRefreshed(object? sender, AbstractSoftwareDisabler.AbstractSoftwareDisablerEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _legionZoneIndicator.Visibility = e.Status == SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;
            EnforceStatusNotificationLimit();
        });
    }

    private void FnKeysDisabler_OnRefreshed(object? sender, AbstractSoftwareDisabler.AbstractSoftwareDisablerEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _fnKeysIndicator.Visibility = e.Status == SoftwareStatus.Enabled ? Visibility.Visible : Visibility.Collapsed;
            EnforceStatusNotificationLimit();
        });
    }

    private void WireStatusNotificationStack()
    {
        foreach (var banner in _statusNotificationStack.Children.OfType<AppStatusBanner>())
            banner.IsVisibleChanged += (_, _) => EnforceStatusNotificationLimit();
    }

    private void EnforceStatusNotificationLimit()
    {
        var banners = _statusNotificationStack.Children.OfType<AppStatusBanner>().ToList();
        var visible = banners.Where(banner => banner.Visibility == Visibility.Visible).ToList();
        if (visible.Count <= MaxVisibleStatusNotifications)
            return;

        var overflow = visible.Count - MaxVisibleStatusNotifications;
        foreach (var banner in visible.Where(banner => !banner.IsPersistent).Take(overflow))
            banner.Visibility = Visibility.Collapsed;
    }

    public async Task CheckForUpdates(bool manualCheck = false)
    {
        try
        {
            var result = await _updateChecker.CheckAsync(manualCheck);
            if (result is null)
            {
                _updateIndicator.Visibility = Visibility.Collapsed;

                if (manualCheck && WindowState != WindowState.Minimized)
                {
                    switch (_updateChecker.Status)
                    {
                        case UpdateCheckStatus.Success:
                            await SnackbarHelper.ShowAsync(Resource.MainWindow_CheckForUpdates_Success_Title);
                            break;
                        case UpdateCheckStatus.RateLimitReached:
                            await SnackbarHelper.ShowAsync(Resource.MainWindow_CheckForUpdates_Error_Title, Resource.MainWindow_CheckForUpdates_Error_ReachedRateLimit_Message, SnackbarType.Error);
                            break;
                        case UpdateCheckStatus.Error:
                            await SnackbarHelper.ShowAsync(Resource.MainWindow_CheckForUpdates_Error_Title, Resource.MainWindow_CheckForUpdates_Error_Unknown_Message, SnackbarType.Error);
                            break;
                    }
                }
            }
            else
            {
                var versionNumber = result.ToString(3);

                _updateIndicator.Message =
                    string.Format(Resource.MainWindow_UpdateAvailableWithVersion, versionNumber);
                _updateIndicator.Visibility = Visibility.Visible;
                EnforceStatusNotificationLimit();

                if (WindowState == WindowState.Minimized)
                    MessagingCenter.Publish(new NotificationMessage(NotificationType.UpdateAvailable, versionNumber));
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Check for updates failed.", ex);
            
            _updateIndicator.Visibility = Visibility.Collapsed;
            EnforceStatusNotificationLimit();
        }
    }

    private void RestoreSize()
    {
        if (!_applicationSettings.Store.WindowSize.HasValue)
            return;

        Width = Math.Max(MinWidth, _applicationSettings.Store.WindowSize.Value.Width);
        Height = Math.Max(MinHeight, _applicationSettings.Store.WindowSize.Value.Height);

        ScreenHelper.UpdateScreenInfos();
        var primaryScreen = ScreenHelper.PrimaryScreen;

        if (!primaryScreen.HasValue)
            return;

        var desktopWorkingArea = primaryScreen.Value.WorkArea;
        Left = (desktopWorkingArea.Width - Width) / 2 + desktopWorkingArea.Left;
        Top = (desktopWorkingArea.Height - Height) / 2 + desktopWorkingArea.Top;
    }

    private void SaveSize()
    {
        _applicationSettings.Store.WindowSize = WindowState != WindowState.Normal
            ? new(RestoreBounds.Width, RestoreBounds.Height)
            : new(Width, Height);
        _applicationSettings.SynchronizeStore();
    }

    private async Task SaveSizeAsync()
    {
        _applicationSettings.Store.WindowSize = WindowState != WindowState.Normal
            ? new(RestoreBounds.Width, RestoreBounds.Height)
            : new(Width, Height);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Saving window size asynchronously...");

        await _applicationSettings.SynchronizeStoreAsync();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Window size saved asynchronously.");
    }

#if DEBUG
    [Conditional("DEBUG")]
    private void DebugBreakpoint(string location)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"DEBUG BREAKPOINT: {location}");

        if (Debugger.IsAttached)
            Debugger.Break();
    }
#else
    [Conditional("DEBUG")]
    private void DebugBreakpoint(string location) { }
#endif

    private void BringToForeground() => WindowExtensions.BringToForeground(this);

    private static void OpenLog()
    {
        try
        {
            if (!Directory.Exists(Folders.AppData))
                return;

            using var process = Process.Start("explorer", Log.Instance.LogPath);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to open log.", ex);
        }
    }

    private void ShowDeviceInfoWindow()
    {
        var window = new DeviceInformationWindow { Owner = this };
        window.ShowDialog();
    }

    private IntPtr MainWindowHwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmNclbuttonUp && wParam.ToInt32() == HtMaxButton)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            handled = true;
            return IntPtr.Zero;
        }

        if (msg != WmNchittest)
            return IntPtr.Zero;

        var screenPoint = GetScreenPointFromLParam(lParam);
        if (IsScreenPointOverElement(_openLogIndicator, screenPoint)
            || IsScreenPointOverElement(_deviceInfoIndicator, screenPoint))
        {
            handled = true;
            return new IntPtr(HtClient);
        }

        return IntPtr.Zero;
    }

    private static Point GetScreenPointFromLParam(IntPtr lParam)
    {
        var value = lParam.ToInt64();
        return new Point((short)(value & 0xffff), (short)((value >> 16) & 0xffff));
    }

    private bool IsScreenPointOverElement(FrameworkElement element, Point screenPoint)
    {
        if (!element.IsVisible || !element.IsHitTestVisible || element.ActualWidth <= 0 || element.ActualHeight <= 0)
            return false;

        var topLeft = element.PointToScreen(new Point(0, 0));
        var bottomRight = element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight));

        return screenPoint.X >= topLeft.X
               && screenPoint.X <= bottomRight.X
               && screenPoint.Y >= topLeft.Y
               && screenPoint.Y <= bottomRight.Y;
    }

    public void ShowUpdateWindow()
    {
        var window = new UpdateWindow { Owner = this };
        window.ShowDialog();
    }

    public void SendToTray()
    {
        if (!_applicationSettings.Store.MinimizeToTray)
            return;

        SetEfficiencyMode(true);
        Hide();
        ShowInTaskbar = true;
    }

    public void UpdateNavigationVisibility()
    {
        UpdateWindowsOptimizationNavigationVisibility();
        UpdateInstalledPluginsNavigationItems(); // Ensure installed plugins have navigation items on startup
        UpdateNavigationItemsVisibilityFromSettings();
        // UpdatePluginExtensionsNavigationVisibility must be called AFTER UpdateNavigationItemsVisibilityFromSettings
        // to ensure it has the latest visibility settings
        UpdatePluginExtensionsNavigationVisibility();
    }

    private void UpdateNavigationItemsVisibilityFromSettings()
    {
        var visibilitySettings = _applicationSettings.Store.NavigationItemsVisibility;

        SetNavigationItemVisibility(_keyboardItem, "keyboardBacklight", visibilitySettings);
        SetNavigationItemVisibility(_automationItem, "automation", visibilitySettings);
        SetNavigationItemVisibility(_macroItem, "macro", visibilitySettings);
        SetNavigationItemVisibility(_windowsOptimizationItem, "windowsOptimization", visibilitySettings);
        SetNavigationItemVisibility(_networkAccelerationItem, "networkAcceleration", visibilitySettings);

        SetNavigationItemVisibility(_aboutItem, "about", visibilitySettings);
    }

    private void SetNavigationItemVisibility(NavigationItem? item, string pageTag, Dictionary<string, bool> visibilitySettings)
    {
        if (item != null)
        {
            var shouldShow = GetNavigationItemVisibility(pageTag, visibilitySettings);
            item.Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private bool GetNavigationItemVisibility(string pageTag, Dictionary<string, bool> visibilitySettings)
    {
        // Dashboard and settings must always be visible
        if (pageTag == "dashboard" || pageTag == "settings")
            return true;

        // keyboardBacklight should use the keyboard key
        if (pageTag == "keyboardBacklight")
            pageTag = "keyboard";

        if (visibilitySettings.TryGetValue(pageTag, out var visibility))
            return visibility;

        // Visible by default
        return true;
    }

    private void UpdatePluginExtensionsNavigationVisibility()
    {
        // Control plugin extensions navigation item visibility based on navigation items visibility settings
        // Plugin extensions should be visible by default, just like other navigation items
        // Only controlled by navigation items visibility settings, not by ExtensionsEnabled
        var visibilitySettings = _applicationSettings.Store.NavigationItemsVisibility;
        var shouldShow = GetNavigationItemVisibility("pluginExtensions", visibilitySettings);
        
        if (_pluginExtensionsItem != null)
        {
            _pluginExtensionsItem.Visibility = shouldShow 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }
    }

    public void UpdateWindowsOptimizationNavigationVisibility()
    {
        if (_windowsOptimizationItem == null)
            return;

        // Windows optimization interface is now the default interface, ensure it's in the navigation items list
        var isInItems = _navigationStore.Items.Contains(_windowsOptimizationItem);
        
        if (!isInItems)
        {
            // Find the position of the Macro navigation item and insert after it
            var macroItem = _navigationStore.Items.OfType<NavigationItem>().FirstOrDefault(item => item.PageTag == "macro");
            if (macroItem != null)
            {
                var macroIndex = _navigationStore.Items.IndexOf(macroItem);
                _navigationStore.Items.Insert(macroIndex + 1, _windowsOptimizationItem);
            }
            else
            {
                _navigationStore.Items.Add(_windowsOptimizationItem);
            }
        }
        
        // Visibility is controlled by UpdateNavigationItemsVisibilityFromSettings
    }

    private void PluginManager_PluginStateChanged(object? sender, PluginEventArgs e)
    {
        // When plugin status changes, update navigation bar visibility and plugin navigation items
        Dispatcher.Invoke(() =>
        {
            UpdateInstalledPluginsNavigationItems();
            UpdateNavigationVisibility();
        });
    }

    /// <summary>
    /// Update navigation items for installed plugins
    /// </summary>
    public void UpdateInstalledPluginsNavigationItems()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"UpdateInstalledPluginsNavigationItems started");
            }

            // Get all installed plugin IDs
            var installedPluginIds = _pluginManager.GetInstalledPluginIds().ToList();
            // Get all registered plugins
            var registeredPlugins = _pluginManager.GetRegisteredPlugins().ToList();
            
            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"  - Installed plugin IDs: [{string.Join(", ", installedPluginIds)}]");
                Log.Instance.Trace($"  - Registered plugins: [{string.Join(", ", registeredPlugins.Select(p => p.Id))}]");
            }
            
            // Only show installed plugins that actually provide a feature page.
            // This prevents blank plugin pages in sidebar navigation.
            var pluginsToShow = registeredPlugins
                .Where(p => installedPluginIds.Contains(p.Id, StringComparer.OrdinalIgnoreCase))
                .Where(ProvidesFeaturePage)
                .ToList();

            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"  - Plugins to show: [{string.Join(", ", pluginsToShow.Select(p => p.Id))}]");
            }

            // Remove navigation items for uninstalled plugins
            var pluginIdsToRemove = _pluginNavigationItems.Keys
                .Where(id => !pluginsToShow.Any(p => p.Id == id))
                .ToList();

            foreach (var pluginId in pluginIdsToRemove)
            {
                if (_pluginNavigationItems.TryGetValue(pluginId, out var navItem))
                {
                    _navigationStore.Items.Remove(navItem);
                    _pluginNavigationItems.Remove(pluginId);
                    if (Log.Instance.IsTraceEnabled)
                    {
                        Log.Instance.Trace($"  - Removed navigation item for plugin: {pluginId}");
                    }
                }
            }

            // Add or update navigation items for installed plugins
            foreach (var plugin in pluginsToShow)
            {
                if (!_pluginNavigationItems.ContainsKey(plugin.Id))
                {
                    if (Log.Instance.IsTraceEnabled)
                    {
                        Log.Instance.Trace($"  - Adding navigation item for plugin: {plugin.Id} (Name: {plugin.Name}, IsSystemPlugin: {plugin.IsSystemPlugin})");
                    }

                    // Get plugin metadata for version info
                    var pluginMetadata = _pluginManager.GetPluginMetadata(plugin.Id);
                    var pluginDisplayName = plugin.Name;
                    
                    // Create new navigation item for this plugin with icon
                    var navItem = new NavigationItem
                    {
                        Content = pluginDisplayName,
                        PageTag = $"plugin:{plugin.Id}",
                        PageType = typeof(PluginPageWrapper),
                        Tag = pluginMetadata // Store metadata in Tag for later use
                    };
                    
                    // Set icon from plugin's Icon property
                    if (!string.IsNullOrWhiteSpace(plugin.Icon))
                    {
                        navItem.Icon = GetSymbolFromString(plugin.Icon);
                    }
                    else
                    {
                        // Default icon if plugin doesn't specify one
                        navItem.Icon = SymbolRegular.Apps24;
                    }

                    AutomationProperties.SetAutomationId(navItem, $"PluginNavItem_{plugin.Id}");

                    // Register the page tag mapping
                    PluginPageWrapper.RegisterPluginPageTag($"plugin:{plugin.Id}", plugin.Id);
                    var pluginId = plugin.Id;
                    navItem.Click += (_, _) => NavigateToPluginPage(pluginId);
                    navItem.PreviewMouseLeftButtonUp += (_, _) => NavigateToPluginPage(pluginId);
                    navItem.KeyDown += (_, e) =>
                    {
                        if (e.Key != Key.Enter && e.Key != Key.Space)
                            return;

                        e.Handled = true;
                        NavigateToPluginPage(pluginId);
                    };

                    // Find the position to insert (after windows optimization item, before plugin extensions item)
                    var insertIndex = -1;
                    if (_windowsOptimizationItem != null && _navigationStore.Items.Contains(_windowsOptimizationItem))
                    {
                        insertIndex = _navigationStore.Items.IndexOf(_windowsOptimizationItem);
                    }
                    else
                    {
                        var macroItem = _navigationStore.Items.OfType<NavigationItem>().FirstOrDefault(item => item.PageTag == "macro");
                        if (macroItem != null)
                        {
                            insertIndex = _navigationStore.Items.IndexOf(macroItem);
                        }
                    }

                    // Insert the navigation item
                    if (insertIndex >= 0)
                    {
                        _navigationStore.Items.Insert(insertIndex + 1, navItem);
                    }
                    else
                    {
                        _navigationStore.Items.Add(navItem);
                    }

                    _pluginNavigationItems[plugin.Id] = navItem;
                }
            }

            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace($"UpdateInstalledPluginsNavigationItems completed. Total plugin navigation items: {_pluginNavigationItems.Count}");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating plugin navigation items: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Convert string to SymbolRegular enum
    /// </summary>
    private SymbolRegular GetSymbolFromString(string symbolString)
    {
        if (Enum.TryParse<SymbolRegular>(symbolString, out var symbol))
        {
            return symbol;
        }
        return SymbolRegular.Apps24;
    }

    public bool NavigateToPluginPage(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return false;

        try
        {
            var pageTag = $"plugin:{pluginId}";
            PluginPageWrapper.RegisterPluginPageTag(pageTag, pluginId);
            UpdateInstalledPluginsNavigationItems();

            foreach (var item in _navigationStore.Items.OfType<NavigationItem>()
                         .Concat(_navigationStore.Footer.OfType<NavigationItem>()))
            {
                item.IsActive = string.Equals(item.PageTag, pageTag, StringComparison.OrdinalIgnoreCase);
            }

            var plugin = _pluginManager.GetRegisteredPlugins()
                .FirstOrDefault(p => string.Equals(p.Id, pluginId, StringComparison.OrdinalIgnoreCase));
            var page = new PluginPageWrapper(pluginId)
            {
                Title = plugin?.Name ?? pluginId
            };

            _rootFrame.Navigate(page);
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error navigating to plugin page: {pluginId}", ex);

            return false;
        }
    }

    private static bool ProvidesFeaturePage(IPlugin plugin)
    {
        try
        {
            return PluginPageWrapper.ProvidesFeaturePage(plugin);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to resolve feature-page capability for plugin {plugin.Id}", ex);
            return false;
        }
    }



    private static unsafe void SetEfficiencyMode(bool enabled)
    {
        var ptr = IntPtr.Zero;

        try
        {
            var priorityClass = enabled
                ? PROCESS_CREATION_FLAGS.IDLE_PRIORITY_CLASS
                : PROCESS_CREATION_FLAGS.NORMAL_PRIORITY_CLASS;
            PInvoke.SetPriorityClass(PInvoke.GetCurrentProcess(), priorityClass);

            var state = new PROCESS_POWER_THROTTLING_STATE
            {
                Version = PInvoke.PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                ControlMask = PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask = enabled ? PInvoke.PROCESS_POWER_THROTTLING_EXECUTION_SPEED : 0,
            };

            var size = Marshal.SizeOf<PROCESS_POWER_THROTTLING_STATE>();
            ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(state, ptr, false);

            PInvoke.SetProcessInformation(PInvoke.GetCurrentProcess(),
                PROCESS_INFORMATION_CLASS.ProcessPowerThrottling,
                ptr.ToPointer(),
                (uint)size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}
}
