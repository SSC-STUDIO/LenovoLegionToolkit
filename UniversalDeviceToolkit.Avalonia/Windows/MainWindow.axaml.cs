using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Lib.Messaging;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.Lib.Notifications;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Controls.Custom;
using UniversalDeviceToolkit.Avalonia.Controls.Shell;
using UniversalDeviceToolkit.Avalonia.Extensions;
using UniversalDeviceToolkit.Avalonia.Pages;
using NavigationItem = UniversalDeviceToolkit.Avalonia.Controls.Custom.NavigationItem;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Avalonia.Windows.Utils;
using Windows.Win32;
using Windows.Win32.System.Threading;
#if !DEBUG
using System.Reflection;
using UniversalDeviceToolkit.Lib.Extensions;
#endif

#pragma warning disable CA1416

namespace UniversalDeviceToolkit.Avalonia.Windows
{
public partial class MainWindow : BaseWindow
{
    private const int MaxVisibleStatusNotifications = 4;

    private readonly ApplicationSettings _applicationSettings;
    private readonly IPluginManager _pluginManager;
    private PluginInstallNotificationBridge? _pluginInstallNotificationBridge;
    private readonly SpecialKeyListener _specialKeyListener;
    private readonly VantageDisabler _vantageDisabler;
    private readonly LegionZoneDisabler _legionZoneDisabler;
    private readonly FnKeysDisabler _fnKeysDisabler;
    private readonly UpdateChecker _updateChecker;

    private TrayHelper? _trayHelper;
    private readonly Dictionary<string, NavigationItem> _pluginNavigationItems = new();
    private readonly Snackbar _snackbar;
    private double _navigationSplitterWidth;
    private bool _pluginExtensionsNoticeDismissed;
    private bool _pluginExtensionsSettingsPersisted;

    // AVALONIA: WPF Window.RestoreBounds has no Avalonia equivalent; the last normal-state
    // window rect is tracked here for placement persistence.
    private Rect _lastNormalBounds;

    protected override Control? AppScaleTarget => _contentGrid;

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
        PropertyChanged += MainWindow_PropertyChanged;
        Loaded += MainWindow_Loaded;
        SizeChanged += MainWindow_SizeChanged;
        _updateIndicator.PointerPressed += UpdateIndicator_Click;

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
            _openLogIndicator.IsVisible = true;
        }

        Title = _title.Text;

        // Listen to frame content changes to update window title / page entrance animation.
        // AVALONIA: WPF Frame.Navigated replaced by ContentControl.Content PropertyChanged.
        _rootFrame.PropertyChanged += RootFrame_ContentChanged;

        // Subscribe to plugin state changed events
        _pluginManager.PluginStateChanged += PluginManager_PluginStateChanged;

        MessagingCenter.Subscribe<MainWindowVisibilityMessage>(this, message =>
            Dispatcher.UIThread.Post(() => ApplyMainWindowVisibility(message.Action)));
    }

    /// <summary>
    /// AVALONIA: WPF SourceInitialized replaced by OnOpened (the HwndSource hook was dropped —
    /// Avalonia borderless windows are fully client-area and the custom TitleBar drags via
    /// BeginMoveDrag, so the WM_NCHITTEST interception is unnecessary).
    /// </summary>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        RestoreWindowPlacement();
    }

    private void ApplyMainWindowVisibility(MainWindowVisibilityAction action)
    {
        switch (action)
        {
            case MainWindowVisibilityAction.Show:
                this.SetTaskbarVisibility(true);
                if (WindowState == WindowState.Minimized)
                    WindowState = WindowState.Normal;
                Show();
                Activate();
                break;
            case MainWindowVisibilityAction.Hide:
                SendToTray();
                break;
        }
    }

    private static Snackbar CreateMainSnackbar(SnackbarPresenter presenter) =>
        NotificationToastFactory.Create(presenter);

    private void NavigationSplitter_DragDelta(object? sender, VectorEventArgs e)
    {
        var collapsedWidth = NavigationPaneMetrics.GetCollapsedWidth();
        // Max stretch scales with the window so large screens can pull the rail further.
        var maxStretchWidth = NavigationPaneMetrics.GetMaxStretchWidth(Bounds.Width);
        var currentWidth = _navigationSplitterWidth > 0 ? _navigationSplitterWidth : _navigationStore.Bounds.Width;
        _navigationSplitterWidth = Math.Clamp(currentWidth + e.Vector.X, collapsedWidth, maxStretchWidth);

        // AVALONIA: WPF BeginAnimation(WidthProperty, null) replaced by clearing Transitions
        // so a pending expand/collapse transition cannot animate the rail during the drag.
        _navigationStore.Transitions = null;
        _navigationStore.Width = _navigationSplitterWidth;
        _navigationStore.MinWidth = _navigationSplitterWidth;
        _navigationStore.MaxWidth = _navigationSplitterWidth;
    }

    private void NavigationSplitter_DragCompleted(object? sender, VectorEventArgs e)
    {
        var collapsedWidth = NavigationPaneMetrics.GetCollapsedWidth();
        var expandedWidth = NavigationPaneMetrics.GetExpandedWidth(Bounds.Width);
        var threshold = collapsedWidth + ((expandedWidth - collapsedWidth) / 2);
        var shouldExpand = (_navigationSplitterWidth > 0 ? _navigationSplitterWidth : _navigationStore.Bounds.Width) >= threshold;
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
        var from = _navigationStore.Bounds.Width > 0 ? _navigationStore.Bounds.Width : targetWidth;
        if (Math.Abs(from - targetWidth) < 0.5)
            return;

        // AVALONIA: WPF DoubleAnimation/BeginAnimation replaced by Avalonia Transitions
        // (same pattern as NavigationStore.UpdateNavigationWidth).
        var duration = Application.Current?.TryFindResource("AnimationDurationMedium", out var value) == true && value is TimeSpan ts && ts > TimeSpan.Zero
            ? ts
            : TimeSpan.FromMilliseconds(220);

        _navigationStore.Transitions = new Transitions
        {
            new DoubleTransition { Property = Layoutable.WidthProperty, Duration = duration },
            new DoubleTransition { Property = Layoutable.MinWidthProperty, Duration = duration },
            new DoubleTransition { Property = Layoutable.MaxWidthProperty, Duration = duration }
        };

        _navigationStore.Width = targetWidth;
        _navigationStore.MinWidth = targetWidth;
        _navigationStore.MaxWidth = targetWidth;

        // Clear the transitions once the rail has settled so later width updates are instant.
        _ = Task.Delay(duration).ContinueWith(_ => Dispatcher.UIThread.Post(() =>
        {
            if (_navigationStore.IsLoaded)
                _navigationStore.Transitions = null;
        }));
    }

    private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (WindowState == WindowState.Normal)
            _lastNormalBounds = new Rect(Position.X, Position.Y, Width, Height);

        if (!e.WidthChanged || !IsLoaded)
            return;

        // Skip per-pixel rail updates during live drag (Explorer does not reflow chrome each move).
        // Applied once on mouse-up via RefreshChromeAfterLiveResize.
        if (WindowResizeStabilityHelper.IsLiveResizing(this))
            return;

        _navigationStore.RefreshWidthForHostWindow();
    }

    /// <summary>Called after live edge-resize ends so deferred layout matches the final size.</summary>
    internal void RefreshChromeAfterLiveResize()
    {
        if (!IsLoaded)
            return;

        _navigationStore.RefreshWidthForHostWindow();
    }

    private void RootFrame_ContentChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // AVALONIA: WPF Frame.Navigated replaced by ContentControl.Content PropertyChanged.
        // Pages are UserControl (no WPF Page.Title), so the window title stays as the app name.
        if (e.Property != ContentControl.ContentProperty)
            return;

        var appName = AppIdentity.DisplayName;
        Title = appName;
        _title.Text = appName;

        if (e.NewValue is Control entranceTarget)
            PageEntranceAnimator.Play(entranceTarget);
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        UpdateNavigationVisibility();

        SmartKeyHelper.Instance.BringToForeground = () => Dispatcher.UIThread.Post(BringToForeground);

        _specialKeyListener.Changed += SpecialKeyListener_Changed;

        _contentGrid.IsVisible = true;

        try
        {
            _pluginInstallNotificationBridge = new PluginInstallNotificationBridge(UniversalDeviceToolkit.Lib.IoCContainer.Resolve<PluginInstallCoordinator>());
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "plugin-install-toast-bridge",
                "Failed to start plugin install notification bridge.",
                ex);
        }

        _ = LoadDeviceInfo();
        UpdateIndicators();
        _ = CheckForUpdates();

        // AVALONIA: WPF InputBindings replaced by InputElement.KeyBindings.
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.Tab, KeyModifiers.Control),
            Command = new RelayCommand(_navigationStore.NavigateToNext)
        });
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.Tab, KeyModifiers.Control | KeyModifiers.Shift),
            Command = new RelayCommand(_navigationStore.NavigateToPrevious)
        });

        var key = (int)Key.D1;
        foreach (var item in _navigationStore.Items.OfType<NavigationItem>())
        {
            if (item.PageTag != null)
            {
                KeyBindings.Add(new KeyBinding
                {
                    Gesture = new KeyGesture((Key)key++, KeyModifiers.Control),
                    Command = new RelayCommand(() => _navigationStore.Navigate(item.PageTag))
                });
            }
        }

        // Re-apply sidebar labels from the active UI culture. x:Static can freeze
        // the wrong satellite (e.g. Chinese OS default) if culture was applied late.
        RefreshNavigationLabels();
        RefreshTitleBarAutomationNames();

        _ = UpdateHardwareDependentNavigationAsync();
        _ = InitializeTrayAsync();
    }

    private void RefreshTitleBarAutomationNames()
    {
        // AVALONIA: Wpf.Ui TitleBarButton (ButtonType) no longer exists; the custom TitleBar
        // builds caption Buttons that are distinguished by AutomationProperties.Name.
        foreach (var button in _mainTitleBar.GetVisibleChildrenOfType<Avalonia.Controls.Button>())
        {
            var (automationId, name) = AutomationProperties.GetName(button) switch
            {
                "Minimize" => ("TitleBarMinimizeButton", "Minimize"),
                "Maximize" => ("TitleBarMaximizeButton", "Maximize"),
                "Restore" => ("TitleBarRestoreButton", "Restore"),
                "Close" => ("TitleBarCloseButton", "Close"),
                "Help" => ("TitleBarHelpButton", "Help"),
                _ => (null, null),
            };

            if (automationId is null || name is null)
                continue;

            button.SetValue(AutomationProperties.AutomationIdProperty, automationId);
            button.SetValue(AutomationProperties.NameProperty, name);
        }
    }

    /// <summary>
    /// Sets built-in navigation item Content/ToolTip from the current
    /// <see cref="Resource.Culture"/> so English mode never keeps Chinese sidebar text.
    /// </summary>
    internal void RefreshNavigationLabels()
    {
        var culture = Resource.Culture ?? System.Globalization.CultureInfo.CurrentUICulture;

        void Set(NavigationItem? item, string key, string englishFallback)
        {
            if (item is null)
                return;
            var text = LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, englishFallback, culture);
            item.Content = text;
            ToolTip.SetTip(item, text);
        }

        Set(_dashboardItem, "MainWindow_NavigationItem_Dashboard", "Dashboard");
        Set(_keyboardItem, "MainWindow_NavigationItem_Keyboard", "Keyboard");
        Set(_automationItem, "MainWindow_NavigationItem_Actions", "Actions");
        Set(_macroItem, "MainWindow_NavigationItem_Macro", "Macro");
        Set(_windowsOptimizationItem, "MainWindow_NavigationItem_WindowsOptimization", "System optimization");
        Set(_pluginExtensionsItem, "MainWindow_NavigationItem_PluginExtensions", "Plugin Extensions");
        Set(_settingsItem, "MainWindow_NavigationItem_Settings", "Settings");
        Set(_aboutItem, "MainWindow_NavigationItem_About", "About");
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
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_navigationStore.Items.Contains(_keyboardItem))
                        _navigationStore.Items.Remove(_keyboardItem);

                    UpdateNavigationVisibility();
                });
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

    private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
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
        PropertyChanged -= MainWindow_PropertyChanged;
        Loaded -= MainWindow_Loaded;
        _updateIndicator.PointerPressed -= UpdateIndicator_Click;
        _pluginInstallNotificationBridge?.Dispose();

        // Unsubscribe from frame content changes
        if (_rootFrame is not null)
            _rootFrame.PropertyChanged -= RootFrame_ContentChanged;

        // Unsubscribe from plugin manager events
        if (_pluginManager is not null)
            _pluginManager.PluginStateChanged -= PluginManager_PluginStateChanged;

        // Unsubscribe from disablers
        _vantageDisabler.OnRefreshed -= VantageDisabler_OnRefreshed;
        _legionZoneDisabler.OnRefreshed -= LegionZoneDisabler_OnRefreshed;
        _fnKeysDisabler.OnRefreshed -= FnKeysDisabler_OnRefreshed;

        // Unsubscribe from special key listener
        _specialKeyListener.Changed -= SpecialKeyListener_Changed;

        // Weak reference table to subscribers; GC can reclaim unsubscribed objects automatically.
        MessagingCenter.Unsubscribe<MainWindowVisibilityMessage>(this);

        _trayHelper?.Dispose();
        _trayHelper = null;
    }

    private int _stateTransitionGeneration;

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Window state changed to {WindowState}");

        RefreshTitleBarAutomationNames();

        // A state change can interrupt the native move/resize loop before Windows sends
        // WM_EXITSIZEMOVE. Release a possible cached client surface first.
        WindowResizeStabilityHelper.RestoreIfNeeded(this);

        switch (WindowState)
        {
            case WindowState.Minimized:
                SetEfficiencyMode(true);
                SendToTray();
                break;
            case WindowState.Normal:
                _lastNormalBounds = new Rect(Position.X, Position.Y, Width, Height);
                SetEfficiencyMode(false);
                QueueContentRefreshAfterStateTransition();
                break;
            case WindowState.Maximized:
                SetEfficiencyMode(false);
                QueueContentRefreshAfterStateTransition();
                break;
        }
    }

    /// <summary>
    /// Schedules one clean layout pass after a native maximize or restore completes.
    /// </summary>
    private void QueueContentRefreshAfterStateTransition()
    {
        if (!IsLoaded)
            return;

        try
        {
            // A generation counter ensures that only the latest state transition refreshes the UI.
            var generation = ++_stateTransitionGeneration;
            Dispatcher.UIThread.InvokeAsync(new Action(() =>
            {
                if (generation != _stateTransitionGeneration || !IsLoaded || WindowState == WindowState.Minimized)
                    return;

                RefreshContentAfterStateTransition();
                _navigationStore.RefreshWidthForHostWindow();
            }), DispatcherPriority.Loaded);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("State-transition content refresh could not be queued.", ex);
            RefreshContentAfterStateTransition();
        }
    }

    private void RefreshContentAfterStateTransition()
    {
        try
        {
            if (Content is Control content)
            {
                // Do not let a retained transition animation keep the client tree invisible.
                // AVALONIA: WPF BeginAnimation(UIElement.OpacityProperty, null) replaced by
                // clearing Transitions and restoring opacity directly.
                content.Transitions = null;
                content.Opacity = 1.0;
                content.InvalidateVisual();
                content.InvalidateMeasure();
                content.InvalidateArrange();
            }
        }
        catch { /* non-fatal */ }
    }

    private void MainWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // AVALONIA: WPF IsVisibleChanged/StateChanged replaced by PropertyChanged filtering.
        if (e.Property == Visual.IsVisibleProperty)
            MainWindow_IsVisibleChanged(sender, e);
        else if (e.Property == WindowStateProperty)
            MainWindow_StateChanged(sender, EventArgs.Empty);
    }

    private void MainWindow_IsVisibleChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!IsVisible)
            return;

        _ = CheckForUpdates();
    }

    private void OpenLogIndicator_Click(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        OpenLog();
    }

    // AVALONIA: removed OpenLogIndicator_PreviewMouseLeftButtonUp — the custom title bar
    // buttons raise Click for left-click and keyboard; WPF's preview-handled suppression
    // of the bubbling Click has no Avalonia equivalent (keeping both would double-open).

    private void OpenLogIndicator_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
            return;

        e.Handled = true;
        OpenLog();
    }

    private void DeviceInfoIndicator_Click(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        ShowDeviceInfoWindow();
    }

    // AVALONIA: removed DeviceInfoIndicator_PreviewMouseLeftButtonUp — see OpenLogIndicator.

    private void DeviceInfoIndicator_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not Key.Enter and not Key.Space)
            return;

        e.Handled = true;
        ShowDeviceInfoWindow();
    }

    private void UpdateIndicator_Click(object? sender, PointerPressedEventArgs e)
    {
        // AVALONIA: WPF MouseLeftButtonDown/MouseRightButtonDown merged into PointerPressed.
        var properties = e.GetCurrentPoint(_updateIndicator).Properties;
        if (!properties.IsLeftButtonPressed && !properties.IsRightButtonPressed)
            return;

        ShowUpdateWindow();
    }

    private void UpdateIndicator_KeyDown(object? sender, KeyEventArgs e)
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
            _deviceInfoIndicator.IsVisible = true;
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
            Dispatcher.UIThread.Post(BringToForeground);
    }

    private void VantageDisabler_OnRefreshed(object? sender, AbstractSoftwareDisabler.AbstractSoftwareDisablerEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _vantageIndicator.IsVisible = e.Status == SoftwareStatus.Enabled ? true : false;
            EnforceStatusNotificationLimit();
        });
    }

    private void LegionZoneDisabler_OnRefreshed(object? sender, AbstractSoftwareDisabler.AbstractSoftwareDisablerEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _legionZoneIndicator.IsVisible = e.Status == SoftwareStatus.Enabled ? true : false;
            EnforceStatusNotificationLimit();
        });
    }

    private void FnKeysDisabler_OnRefreshed(object? sender, AbstractSoftwareDisabler.AbstractSoftwareDisablerEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _fnKeysIndicator.IsVisible = e.Status == SoftwareStatus.Enabled ? true : false;
            EnforceStatusNotificationLimit();
        });
    }

    private void WireStatusNotificationStack()
    {
        foreach (var banner in _statusNotificationStack.Children.OfType<AppStatusBanner>())
            banner.PropertyChanged += StatusBanner_PropertyChanged;

        if (_pluginExtensionsIndicator is not null)
        {
            // Closed only fires from the close button (AppStatusBanner.Hide), not from initial Collapsed.
            _pluginExtensionsIndicator.Closed += (_, _) => _pluginExtensionsNoticeDismissed = true;
        }
    }

    private void StatusBanner_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.IsVisibleProperty)
            EnforceStatusNotificationLimit();
    }

    private void EnforceStatusNotificationLimit()
    {
        var banners = _statusNotificationStack.Children.OfType<AppStatusBanner>().ToList();
        var visible = banners.Where(banner => banner.IsVisible).ToList();
        if (visible.Count <= MaxVisibleStatusNotifications)
            return;

        var overflow = visible.Count - MaxVisibleStatusNotifications;
        foreach (var banner in visible.Where(banner => !banner.IsPersistent).Take(overflow))
            banner.IsVisible = false;
    }

    public async Task CheckForUpdates(bool manualCheck = false)
    {
        try
        {
            var result = await _updateChecker.CheckAsync(manualCheck);
            if (result is null)
            {
                _updateIndicator.IsVisible = false;

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
                _updateIndicator.IsVisible = true;
                EnforceStatusNotificationLimit();

                if (WindowState == WindowState.Minimized)
                    MessagingCenter.Publish(new NotificationMessage(NotificationType.UpdateAvailable, versionNumber));
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Check for updates failed.", ex);

            _updateIndicator.IsVisible = false;
            EnforceStatusNotificationLimit();
        }
    }

    private void RestoreWindowPlacement()
    {
        var placement = _applicationSettings.Store.WindowPlacement;

        // Settings written before position persistence only carry WindowSize.
        if (!placement.HasValue)
        {
            RestoreSize();
            return;
        }

        var saved = placement.Value;

        // Hand-edited / corrupt settings can carry non-finite or non-positive values.
        if (!IsValidPlacement(saved))
            return;

        // Min-size floor (MinWidth/MinHeight = 1024x640) applied before any clamping.
        var width = Math.Max(MinWidth, saved.Width);
        var height = Math.Max(MinHeight, saved.Height);

        ScreenHelper.UpdateScreenInfos();

        var center = new Point(saved.Left + width / 2, saved.Top + height / 2);
        if (!IsOnConnectedDisplay(center))
        {
            // Saved position sits on a disconnected display (e.g. unplugged monitor) —
            // restore the size centered on the primary work area instead of losing the window.
            Width = width;
            Height = height;
            CenterOnPrimaryScreen();
            ApplySavedWindowState(saved.IsMaximized);
            return;
        }

        // Clamp into the current virtual screen so resolution/DPI changes or a shifted
        // virtual-screen origin can never push the window past the reachable edge.
        var bounds = ClampToVirtualScreen(new Rect(saved.Left, saved.Top, width, height));
        Position = new PixelPoint((int)bounds.Left, (int)bounds.Top);
        Width = bounds.Width;
        Height = bounds.Height;

        ApplySavedWindowState(saved.IsMaximized);
    }

    private static bool IsValidPlacement(Lib.WindowPlacement placement) =>
        double.IsFinite(placement.Left)
        && double.IsFinite(placement.Top)
        && double.IsFinite(placement.Width)
        && double.IsFinite(placement.Height)
        && placement.Width > 0
        && placement.Height > 0;

    private static bool IsOnConnectedDisplay(Point point)
    {
        foreach (var screen in ScreenHelper.GetScreensSnapshot())
        {
            if (screen.WorkArea.Contains(point))
                return true;
        }

        return false;
    }

    // AVALONIA: WPF SystemParameters.VirtualScreen* replaced by GetSystemMetrics.
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private static Rect ClampToVirtualScreen(Rect bounds)
    {
        var virtualWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        var virtualHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);
        var virtualLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var virtualTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
        var width = Math.Min(bounds.Width, virtualWidth);
        var height = Math.Min(bounds.Height, virtualHeight);
        var left = Math.Clamp(bounds.Left, virtualLeft,
            virtualLeft + virtualWidth - width);
        var top = Math.Clamp(bounds.Top, virtualTop,
            virtualTop + virtualHeight - height);
        return new Rect(left, top, width, height);
    }

    private void ApplySavedWindowState(bool isMaximized)
    {
        // Only Maximized is ever restored — reopening straight into Minimized would
        // send the window to the tray and look like the app failed to launch.
        if (isMaximized)
            WindowState = WindowState.Maximized;
    }

    private void RestoreSize()
    {
        if (!_applicationSettings.Store.WindowSize.HasValue)
            return;

        Width = Math.Max(MinWidth, _applicationSettings.Store.WindowSize.Value.Width);
        Height = Math.Max(MinHeight, _applicationSettings.Store.WindowSize.Value.Height);

        CenterOnPrimaryScreen();
    }

    private void CenterOnPrimaryScreen()
    {
        ScreenHelper.UpdateScreenInfos();
        var primaryScreen = ScreenHelper.PrimaryScreen;

        if (!primaryScreen.HasValue)
            return;

        var desktopWorkingArea = primaryScreen.Value.WorkArea;
        Position = new PixelPoint(
            (int)((desktopWorkingArea.Width - Width) / 2 + desktopWorkingArea.Left),
            (int)((desktopWorkingArea.Height - Height) / 2 + desktopWorkingArea.Top));
    }

    private async Task SaveSizeAsync()
    {
        // AVALONIA: WPF Window.RestoreBounds has no Avalonia equivalent; _lastNormalBounds
        // holds the normal-state rect when the window is maximized/minimized.
        var bounds = WindowState != WindowState.Normal && _lastNormalBounds != default
            ? _lastNormalBounds
            : new Rect(Position.X, Position.Y, Width, Height);

        _applicationSettings.Store.WindowSize = new(bounds.Width, bounds.Height);
        _applicationSettings.Store.WindowPlacement = new Lib.WindowPlacement(
            bounds.Left, bounds.Top, bounds.Width, bounds.Height,
            WindowState == WindowState.Maximized);

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Saving window placement asynchronously...");

        await _applicationSettings.SynchronizeStoreAsync();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Window placement saved asynchronously.");
    }

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
        var window = new DeviceInformationWindow();
        window.ShowDialog(this);
    }

    public void ShowUpdateWindow()
    {
        var window = new UpdateWindow();
        window.ShowDialog(this);
    }

    public void SendToTray()
    {
        if (!_applicationSettings.Store.MinimizeToTray)
            return;

        SetEfficiencyMode(true);
        this.SetTaskbarVisibility(false);
        Hide();
    }

    public void UpdateNavigationVisibility()
    {
        UpdateWindowsOptimizationNavigationVisibility();
        UpdateInstalledPluginsNavigationItems(); // Ensure installed plugins have navigation items on startup
        UpdateNavigationItemsVisibilityFromSettings();
        // UpdatePluginExtensionsNavigationVisibility must be called AFTER UpdateNavigationItemsVisibilityFromSettings
        // to ensure it has the latest visibility settings
        UpdatePluginExtensionsNavigationVisibility();

        // Persist the one-time migration from the former hidden plugin extensions default.
        if (!_pluginExtensionsSettingsPersisted)
        {
            _pluginExtensionsSettingsPersisted = true;
            try
            {
                _applicationSettings.SynchronizeStore();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Failed to persist plugin-extensions navigation default.", ex);
            }
        }
    }

    private void UpdateNavigationItemsVisibilityFromSettings()
    {
        var visibilitySettings = _applicationSettings.Store.NavigationItemsVisibility;

        SetNavigationItemVisibility(_keyboardItem, "keyboardBacklight", visibilitySettings);
        SetNavigationItemVisibility(_automationItem, "automation", visibilitySettings);
        SetNavigationItemVisibility(_macroItem, "macro", visibilitySettings);
        SetNavigationItemVisibility(_windowsOptimizationItem, "windowsOptimization", visibilitySettings);

        SetNavigationItemVisibility(_aboutItem, "about", visibilitySettings);
    }

    private void SetNavigationItemVisibility(NavigationItem? item, string pageTag, Dictionary<string, bool> visibilitySettings)
    {
        if (item != null)
        {
            var shouldShow = GetNavigationItemVisibility(pageTag, visibilitySettings);
            item.IsVisible = shouldShow ? true : false;
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

        // All optional navigation entries, including Plugin Extensions, default to visible.
        return true;
    }

    private void UpdatePluginExtensionsNavigationVisibility()
    {
        // Controlled by navigation items visibility settings; default is visible.
        var visibilitySettings = _applicationSettings.Store.NavigationItemsVisibility;
        var shouldShow = GetNavigationItemVisibility("pluginExtensions", visibilitySettings);

        if (_pluginExtensionsItem != null)
        {
            _pluginExtensionsItem.IsVisible = shouldShow
                ? true
                : false;
        }

        // Persistent notice while the nav entry is off. Dismissible for the session; shown again next launch if still off.
        if (_pluginExtensionsIndicator != null)
        {
            if (shouldShow)
            {
                // Enabling the nav entry clears any session dismiss so the notice can return if turned off again.
                _pluginExtensionsNoticeDismissed = false;
                _pluginExtensionsIndicator.IsVisible = false;
            }
            else if (_pluginExtensionsNoticeDismissed)
            {
                _pluginExtensionsIndicator.IsVisible = false;
            }
            else
            {
                _pluginExtensionsIndicator.IsVisible = true;
            }

            EnforceStatusNotificationLimit();
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
        Dispatcher.UIThread.Invoke(() =>
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
                    // Single Click path only (NavigationStore skips plugin: tags). Avoid
                    // PreviewMouseLeftButtonUp + Click double-navigation races.
                    navItem.Click += (_, _) => NavigateToPluginPage(pluginId);
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

            // AVALONIA: removed Title = plugin?.Name ?? pluginId — PluginPageWrapper is a
            // UserControl (no WPF Page.Title); the window title stays as the app name.
            var page = new PluginPageWrapper(pluginId);

            _rootFrame.Content = page;
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
