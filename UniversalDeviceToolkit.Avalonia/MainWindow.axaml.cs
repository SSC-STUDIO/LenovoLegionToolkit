using global::Avalonia;
using global::Avalonia.Automation;
using global::Avalonia.Controls;
using global::Avalonia.Input;
using global::Avalonia.Interactivity;
using global::Avalonia.Layout;
using global::Avalonia.Media;
using global::Avalonia.Threading;
using System.Text;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Pages.Windows;
using UniversalDeviceToolkit.Avalonia.Services;
#if WINDOWS
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Avalonia.Controls.Shell;
#endif

namespace UniversalDeviceToolkit.Avalonia;

public partial class MainWindow : Window
{
    private readonly IPlatformServices _platformServices;
    private readonly Dictionary<string, PluginNavigationEntry> _pluginNavigationEntries =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _pluginNavigationRefreshLock = new(1, 1);
    private string _activePage = MainNavigation.Dashboard;
    private bool? _keyboardHardwareAvailable;
    private int _windowSurfaceRefreshGeneration;
#if WINDOWS
    private Rect? _lastNormalWindowBounds;
    private bool _windowPlacementRestored;
#endif
#if WINDOWS
    private IPluginManager? _pluginManager;
    private AbstractSoftwareDisabler? _vantageDisabler;
    private AbstractSoftwareDisabler? _legionZoneDisabler;
    private AbstractSoftwareDisabler? _fnKeysDisabler;
    private bool _pluginExtensionsNoticeDismissed;
    private SoftwareStatus _vantageStatus;
    private SoftwareStatus _legionZoneStatus;
    private SoftwareStatus _fnKeysStatus;
#endif

    /// <summary>
    /// Gets the route currently rendered by the shell.
    /// </summary>
    public string ActiveRoute => _activePage;

    public MainWindow(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        InitializeComponent();
        MinimizeGlyph.Text = "\uE921";
        MaximizeGlyph.Text = "\uE922";
        CloseGlyph.Text = "\uE8BB";
        ApplyNavigationPaneState();
        ApplyNavigationVisibility();
        ApplyTextDirection(LocalizationRuntime.CurrentCulture);
        Loaded += OnLoaded;
        
        // Handle window state changes (minimize/restore)
        PropertyChanged += OnWindowPropertyChanged;
        SizeChanged += OnWindowSizeChanged;
        Activated += OnWindowActivated;
        Closed += OnWindowClosed;
        AvaloniaThemeManager.Instance.ThemeApplied += OnThemeApplied;
        AvaloniaThemeManager.Instance.UiScaleChanged += OnUiScaleChanged;
        ApplyUiScale(AvaloniaThemeManager.Instance.UiScaleFactor);
#if WINDOWS
        Opened += OnOpened;
        Closing += OnClosing;
        SubscribeToPluginStateChanges();
        Closed += OnClosed;
        InitializeStatusBanners();
#endif
    }

    private void InitializeStatusBanners()
    {
        VantageWarningBanner.Message = AvaloniaLocalization.GetString(
            "MainWindows_VantageRunning",
            "Lenovo Vantage or its services are running.");
        LegionZoneWarningBanner.Message = AvaloniaLocalization.GetString(
            "MainWindow_LegionZoneRunning",
            "Lenovo Legion Zone or its services are running.");
        FnKeysWarningBanner.Message = AvaloniaLocalization.GetString(
            "MainWindows_FnKeysRunning",
            "Lenovo hotkeys are active.");
        PluginExtensionsBanner.Message = AvaloniaLocalization.GetString(
            "MainWindow_PluginExtensionsDisabledNotice",
            "Plugin extensions are disabled.");
        PluginExtensionsBanner.Closed += (_, _) => _pluginExtensionsNoticeDismissed = true;

#if WINDOWS
        _vantageDisabler = IoCContainer.TryResolve<VantageDisabler>();
        _legionZoneDisabler = IoCContainer.TryResolve<LegionZoneDisabler>();
        _fnKeysDisabler = IoCContainer.TryResolve<FnKeysDisabler>();
        foreach (var disabler in new AbstractSoftwareDisabler?[]
                 {
                     _vantageDisabler,
                     _legionZoneDisabler,
                     _fnKeysDisabler,
                 })
        {
            if (disabler is not null)
                disabler.OnRefreshed += SoftwareDisabler_OnRefreshed;
        }
#endif
        UpdateIndicators();
    }

#if WINDOWS
    private void SoftwareDisabler_OnRefreshed(object? sender, AbstractSoftwareDisabler.AbstractSoftwareDisablerEventArgs e)
    {
        if (ReferenceEquals(sender, _vantageDisabler))
            _vantageStatus = e.Status;
        else if (ReferenceEquals(sender, _legionZoneDisabler))
            _legionZoneStatus = e.Status;
        else if (ReferenceEquals(sender, _fnKeysDisabler))
            _fnKeysStatus = e.Status;

        Dispatcher.UIThread.Post(UpdateIndicators);
    }

    private void UpdateIndicators()
    {
        VantageWarningBanner.IsVisible = _vantageStatus == SoftwareStatus.Enabled;
        LegionZoneWarningBanner.IsVisible = _legionZoneStatus == SoftwareStatus.Enabled;
        FnKeysWarningBanner.IsVisible = _fnKeysStatus == SoftwareStatus.Enabled;

        var settings = Services.WindowsAvaloniaSettingsService.SharedApplicationSettings;
        var extensionsEnabled = settings.Store.ExtensionsEnabled;
        PluginExtensionsBanner.IsVisible = !extensionsEnabled && !_pluginExtensionsNoticeDismissed;
    }
#endif

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        AvaloniaThemeManager.Instance.ThemeApplied -= OnThemeApplied;
        AvaloniaThemeManager.Instance.UiScaleChanged -= OnUiScaleChanged;
    }

    // Re-applies the window backdrop whenever the theme manager re-applies
    // appearance preferences so Mica/Acrylic follows the current theme.
    private void OnThemeApplied(object? sender, EventArgs e) => ApplyWindowBackdrop();

    private void OnUiScaleChanged(object? sender, double scale) => ApplyUiScale(scale);

    private void ApplyUiScale(double scale)
    {
        if (ContentScaleTransform is null || !(scale > 0))
            return;

        ContentScaleTransform.LayoutTransform = new ScaleTransform(scale, scale);
    }

    private async void UpdateAvailableButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Application.Current is App app)
            await app.ShowUpdateDialogAsync(this);
    }

    /// <summary>
    /// Shows the update-available pill. The payload comes from the update
    /// coordinator's UpdateAvailableChanged event (consumed through the
    /// reflection bridge in App); its string form is used for the version label.
    /// </summary>
    internal void SetUpdateAvailable(object? releaseInfo)
    {
        var version = releaseInfo?.ToString();
        UpdateAvailableLabel.Text = string.IsNullOrWhiteSpace(version)
            ? Localization.AvaloniaLocalization.GetString("MainWindow_UpdateAvailable", "Update available")
            : string.Format(
                Localization.AvaloniaLocalization.GetString("MainWindow_UpdateAvailableVersion", "Update available ({0})"),
                version);
        UpdateAvailableButton.IsVisible = true;
        global::Avalonia.Automation.AutomationProperties.SetName(UpdateAvailableButton, UpdateAvailableLabel.Text);
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
#if WINDOWS
        TrackNormalWindowBounds();
#endif

        // Force UI refresh when window state changes
        if (e.Property == WindowStateProperty)
        {
            ApplyWindowBackdrop();

            if (WindowState == WindowState.Minimized
                && Application.Current is App app
                && app.MinimizeToTrayEnabled)
            {
                ShowInTaskbar = false;
                Hide();
                return;
            }

            if (WindowState == WindowState.Normal || WindowState == WindowState.Maximized)
            {
                QueueWindowSurfaceRefresh();
            }
        }
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
#if WINDOWS
        TrackNormalWindowBounds();
#endif

        // Trigger layout update when window is resized (including from minimized state)
        InvalidateArrange();
        InvalidateMeasure();
    }

    private void OnWindowActivated(object? sender, EventArgs e) => QueueWindowSurfaceRefresh();

#if WINDOWS
    private void OnOpened(object? sender, EventArgs e)
    {
        RestoreWindowPlacement();
        TrackNormalWindowBounds();
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e) => SaveWindowPlacement();

    private void RestoreWindowPlacement()
    {
        if (_windowPlacementRestored)
            return;

        _windowPlacementRestored = true;
        var settings = Services.WindowsAvaloniaSettingsService.SharedApplicationSettings;
        var result = AvaloniaWindowPlacementCoordinator.Restore(
            settings.Store.WindowPlacement,
            settings.Store.WindowSize,
            new Size(MinWidth, MinHeight),
            GetScreenWorkAreas());
        if (result is not { } restored)
            return;

        ApplyLogicalBounds(restored.Bounds);
        _lastNormalWindowBounds = restored.Bounds;

        // A minimized state is never persisted. Set Maximized only after the
        // normal bounds are applied so a later close can restore them correctly.
        if (restored.IsMaximized)
            WindowState = WindowState.Maximized;
    }

    private void SaveWindowPlacement()
    {
        try
        {
            var normalBounds = _lastNormalWindowBounds ?? GetCurrentLogicalBounds();
            if (normalBounds.Width <= 0 || normalBounds.Height <= 0)
                return;

            var settings = Services.WindowsAvaloniaSettingsService.SharedApplicationSettings;
            settings.Store.WindowSize = new UniversalDeviceToolkit.Lib.WindowSize(
                normalBounds.Width,
                normalBounds.Height);
            settings.Store.WindowPlacement = AvaloniaWindowPlacementCoordinator.Capture(
                normalBounds,
                WindowState == WindowState.Maximized);
            settings.SynchronizeStore();
        }
        catch
        {
            // Closing must never be blocked by an unavailable display or a
            // transient settings-file failure.
        }
    }

    private void TrackNormalWindowBounds()
    {
        if (WindowState == WindowState.Normal && _windowPlacementRestored)
            _lastNormalWindowBounds = GetCurrentLogicalBounds();
    }

    private Rect GetCurrentLogicalBounds()
    {
        var scaling = RenderScaling > 0 ? RenderScaling : 1d;
        return new Rect(Position.X / scaling, Position.Y / scaling, Width, Height);
    }

    private IReadOnlyList<AvaloniaWindowPlacementCoordinator.ScreenWorkArea> GetScreenWorkAreas()
    {
        if (Screens is null)
            return [];

        var primary = Screens.Primary;
        return Screens.All.Select(screen =>
        {
            var scaling = screen.Scaling > 0 ? screen.Scaling : 1d;
            var workArea = screen.WorkingArea;
            return new AvaloniaWindowPlacementCoordinator.ScreenWorkArea(
                new Rect(
                    workArea.X / scaling,
                    workArea.Y / scaling,
                    workArea.Width / scaling,
                    workArea.Height / scaling),
                screen == primary);
        }).ToArray();
    }

    private void ApplyLogicalBounds(Rect bounds)
    {
        Width = Math.Max(MinWidth, bounds.Width);
        Height = Math.Max(MinHeight, bounds.Height);

        var targetScreen = GetScreenWorkAreas()
            .FirstOrDefault(screen => screen.Bounds.Contains(bounds.Center));
        var scaling = targetScreen.Bounds.Width > 0
            ? GetScreenScaling(targetScreen.Bounds)
            : (RenderScaling > 0 ? RenderScaling : 1d);
        Position = new PixelPoint(
            (int)Math.Round(bounds.X * scaling),
            (int)Math.Round(bounds.Y * scaling));
    }

    private double GetScreenScaling(Rect logicalWorkArea)
    {
        if (Screens is null)
            return RenderScaling > 0 ? RenderScaling : 1d;

        foreach (var screen in Screens.All)
        {
            var scaling = screen.Scaling > 0 ? screen.Scaling : 1d;
            var workArea = screen.WorkingArea;
            if (Math.Abs(workArea.X / scaling - logicalWorkArea.X) < 0.01
                && Math.Abs(workArea.Y / scaling - logicalWorkArea.Y) < 0.01)
                return scaling;
        }

        return RenderScaling > 0 ? RenderScaling : 1d;
    }
#endif

    /// <summary>
    /// Restores a hidden tray window through the same redraw path used after a
    /// native minimize/restore transition. This prevents a retained transparent
    /// surface from being shown before the client content has been laid out.
    /// </summary>
    internal void RestoreFromTray()
    {
        ShowInTaskbar = true;
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;

        Show();
        Activate();
        QueueWindowSurfaceRefresh();
    }

    private void QueueWindowSurfaceRefresh()
    {
        var generation = ++_windowSurfaceRefreshGeneration;
        Dispatcher.UIThread.Post(() =>
        {
            if (generation != _windowSurfaceRefreshGeneration
                || !IsVisible
                || WindowState == WindowState.Minimized)
                return;

            ApplyWindowBackdrop();
            MainContent.InvalidateMeasure();
            MainContent.InvalidateArrange();
            InvalidateMeasure();
            InvalidateArrange();
            InvalidateVisual();
        }, DispatcherPriority.Render);
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        ApplyWindowBackdrop();
        ApplyNavigationVisibility();
        // Show DashboardPage by default on startup
        ShowDashboardPage();
        await UpdateHardwareDependentNavigationAsync();
        await RefreshPluginNavigationItemsAsync();
        await RefreshDeviceInfoIndicatorAsync();
    }

    private async Task RefreshDeviceInfoIndicatorAsync()
    {
        try
        {
            var snapshot = await _platformServices.GetDashboardSnapshotAsync().ConfigureAwait(true);
            var deviceName = string.IsNullOrWhiteSpace(snapshot.DeviceName)
                ? null
                : snapshot.DeviceName.Trim();
            DeviceInfoButton.IsVisible = deviceName is not null;
            if (deviceName is not null)
                DeviceInfoLabel.Text = deviceName;
        }
        catch
        {
            DeviceInfoButton.IsVisible = false;
        }
    }

    /// <summary>
    /// Applies the persisted backdrop preference to the current top-level window.
    /// Avalonia accepts the levels in priority order and uses the first level the
    /// platform supports, so unsupported Mica/Acrylic implementations degrade to
    /// blur and finally an opaque window instead of rendering an empty black layer.
    /// </summary>
    public void ApplyWindowBackdrop()
    {
        if (WindowState == WindowState.Minimized)
        {
            TransparencyLevelHint = [WindowTransparencyLevel.None];
            return;
        }

#if WINDOWS
        var style = Services.WindowsAvaloniaSettingsService.SharedApplicationSettings
            .Store.WindowBackdropStyle
            .ToString();

        TransparencyLevelHint = style switch
        {
            "Windows" =>
            [
                WindowTransparencyLevel.Mica,
                WindowTransparencyLevel.AcrylicBlur,
                WindowTransparencyLevel.Blur,
                WindowTransparencyLevel.None,
            ],
            "macOS" =>
            [
                WindowTransparencyLevel.AcrylicBlur,
                WindowTransparencyLevel.Blur,
                WindowTransparencyLevel.None,
            ],
            _ => [WindowTransparencyLevel.None],
        };
#else
        TransparencyLevelHint = [WindowTransparencyLevel.None];
#endif

        if (this.TryFindResource("AppBackgroundBrush", out var fallback)
            && fallback is IBrush brush)
        {
            TransparencyBackgroundFallback = brush;
        }
    }

    private void DashboardButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.Dashboard);
    }

    private async void DeviceInfoButton_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new DeviceInformationWindow();
        await dialog.ShowDialog(this);
    }

    private void TitleBarHost_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            && e.Source is not Button)
        {
            BeginMoveDrag(e);
        }
    }

    private void MinimizeButton_Click(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();

    private void KeyboardButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.Keyboard);
    }

    private void ActionsButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.Actions);
    }

    private void MacroButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.Macro);
    }

    private void WindowsOptimizationButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.WindowsOptimization);
    }

    private void PluginExtensionsButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.PluginExtensions);
    }

    private void AboutButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.About);
    }

    private void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.Settings);
    }

    private void NavigationToggleButton_Click(object? sender, RoutedEventArgs e)
    {
#if WINDOWS
        var settings = Services.WindowsAvaloniaSettingsService.SharedApplicationSettings;
        settings.Store.NavigationPaneExpanded = !settings.Store.NavigationPaneExpanded;
        settings.SynchronizeStore();
#endif
        ApplyNavigationPaneState();
    }

    private void OnPluginActionRequested(string actionKey)
    {
        const string openPrefix = "plugin-open:";
        const string settingsPrefix = "plugin-settings:";
        if (actionKey.StartsWith(openPrefix, StringComparison.OrdinalIgnoreCase))
            Navigate(MainNavigation.CreatePluginRoute(actionKey[openPrefix.Length..]));
        else if (actionKey.StartsWith(settingsPrefix, StringComparison.OrdinalIgnoreCase))
            Navigate(MainNavigation.CreatePluginSettingsRoute(actionKey[settingsPrefix.Length..]));
    }

    private async Task RefreshPluginNavigationItemsAsync(bool forceRefresh = false)
    {
        await _pluginNavigationRefreshLock.WaitAsync().ConfigureAwait(true);
        try
        {
            var catalog = await _platformServices
                .GetPluginCatalogAsync(forceRefresh)
                .ConfigureAwait(true);
            var visiblePlugins = PluginNavigationPolicy.GetVisiblePlugins(catalog);
            var visibleIds = visiblePlugins
                .Select(plugin => plugin.Id.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var staleId in _pluginNavigationEntries.Keys
                         .Where(id => !visibleIds.Contains(id))
                         .ToArray())
            {
                var stale = _pluginNavigationEntries[staleId];
                PluginNavigationItems.Children.Remove(stale.Button);
                _pluginNavigationEntries.Remove(staleId);
            }

            foreach (var plugin in visiblePlugins)
            {
                var pluginId = plugin.Id.Trim();
                if (_pluginNavigationEntries.TryGetValue(pluginId, out var existing))
                {
                    UpdatePluginNavigationEntry(existing, plugin);
                    continue;
                }

                var entry = CreatePluginNavigationEntry(plugin);
                _pluginNavigationEntries.Add(pluginId, entry);
                PluginNavigationItems.Children.Add(entry.Button);
            }

            PluginNavigationItems.IsVisible = _pluginNavigationEntries.Count > 0;
            ApplyNavigationPaneState();
            SetActiveButton(GetNavigationButton(_activePage));
        }
        catch
        {
            // Plugin discovery is optional. A failed catalog read must not make
            // the shell unusable or remove the static Plugin Extensions route.
        }
        finally
        {
            _pluginNavigationRefreshLock.Release();
        }
    }

    internal Task RefreshPluginNavigationAsync(bool forceRefresh = false) =>
        RefreshPluginNavigationItemsAsync(forceRefresh);

#if WINDOWS
    private void SubscribeToPluginStateChanges()
    {
        _pluginManager = IoCContainer.TryResolve<IPluginManager>();
        if (_pluginManager is not null)
            _pluginManager.PluginStateChanged += PluginManagerOnPluginStateChanged;
    }

    private void PluginManagerOnPluginStateChanged(object? sender, PluginEventArgs args) =>
        Dispatcher.UIThread.Post(() => _ = RefreshPluginNavigationItemsAsync());

    private void OnClosed(object? sender, EventArgs args)
    {
        if (_pluginManager is not null)
            _pluginManager.PluginStateChanged -= PluginManagerOnPluginStateChanged;
        _pluginManager = null;

        foreach (var disabler in new AbstractSoftwareDisabler?[]
                 {
                     _vantageDisabler,
                     _legionZoneDisabler,
                     _fnKeysDisabler,
                 })
        {
            if (disabler is not null)
                disabler.OnRefreshed -= SoftwareDisabler_OnRefreshed;
        }
    }
#endif

    private PluginNavigationEntry CreatePluginNavigationEntry(PluginCatalogItem plugin)
    {
        var displayName = GetPluginDisplayName(plugin);
        var label = new Controls.LocalizedTextBlock
        {
            Text = displayName,
            OverflowMode = LocalizedOverflowMode.Ellipsis,
            MaxLines = 1,
            MinWidth = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        var button = new Button
        {
            Tag = plugin.Id,
            Margin = new Thickness(0, 0, 0, 0),
            Content = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                ColumnSpacing = 12,
            },
        };
        button.Classes.Add("navButton");
        ToolTip.SetTip(button, displayName);
        var content = (Grid)button.Content;
        var iconSize = this.TryFindResource("IconSizeMD", out var resourceIconSize)
            && resourceIconSize is double resolvedIconSize
            ? resolvedIconSize
            : 20d;
        content.Children.Add(new Controls.NavigationIcon
        {
            IconIdentifier = "Apps24",
            FontSize = iconSize,
        });
        Grid.SetColumn(label, 1);
        content.Children.Add(label);

        AutomationProperties.SetAutomationId(button, CreatePluginAutomationId(plugin.Id));
        AutomationProperties.SetName(button, displayName);
        button.Click += (_, _) => Navigate(MainNavigation.CreatePluginRoute(plugin.Id));
        return new PluginNavigationEntry(button, label, plugin.Id);
    }

    private static void UpdatePluginNavigationEntry(
        PluginNavigationEntry entry,
        PluginCatalogItem plugin)
    {
        var displayName = GetPluginDisplayName(plugin);
        entry.Label.Text = displayName;
        ToolTip.SetTip(entry.Button, displayName);
        AutomationProperties.SetName(entry.Button, displayName);
    }

    private static string GetPluginDisplayName(PluginCatalogItem plugin) =>
        string.IsNullOrWhiteSpace(plugin.Name) ? plugin.Id.Trim() : plugin.Name.Trim();

    private static string CreatePluginAutomationId(string pluginId)
    {
        var builder = new StringBuilder("AvaloniaPluginNavItem_");
        foreach (var character in pluginId)
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        return builder.ToString();
    }

    private void ShowDashboardPage()
    {
        _activePage = MainNavigation.Dashboard;
        MainContent.Content = new DashboardPage(_platformServices, Navigate);
        SetActiveButton(DashboardButton);
    }

    private void ShowAboutPage()
    {
        _activePage = MainNavigation.About;
        MainContent.Content = new AboutPage();
        SetActiveButton(AboutButton);
    }

    public void ShowSettingsPage()
    {
        _activePage = MainNavigation.Settings;
        MainContent.Content = new SettingsPage(_platformServices);
        SetActiveButton(SettingsButton);
    }

    private void ShowFeaturePage(string route)
    {
        MainContent.Content = route switch
        {
            MainNavigation.Keyboard => new KeyboardBacklightPage(_platformServices),
            MainNavigation.Actions => new ActionsPage(_platformServices),
            MainNavigation.Macro => new MacroPage(_platformServices),
            MainNavigation.WindowsOptimization => new WindowsOptimizationPage(
                _platformServices,
                OnPluginActionRequested),
            MainNavigation.PluginExtensions => new PluginExtensionsPage(
                _platformServices,
                OnPluginActionRequested,
                () => _ = RefreshPluginNavigationItemsAsync(forceRefresh: true)),
            _ => throw new ArgumentOutOfRangeException(nameof(route), route, "Unknown feature route."),
        };
        _activePage = route;
        SetActiveButton(GetNavigationButton(route));
        if (string.Equals(route, MainNavigation.PluginExtensions, StringComparison.OrdinalIgnoreCase))
            _ = RefreshPluginNavigationItemsAsync();
    }

    private void ShowPluginPage(string route)
    {
        var isSettings = MainNavigation.TryGetPluginSettingsId(route, out var settingsPluginId);
        if (!isSettings && !MainNavigation.TryGetPluginId(route, out settingsPluginId))
            return;

        var pluginId = settingsPluginId;
        _activePage = isSettings
            ? MainNavigation.CreatePluginSettingsRoute(pluginId)
            : MainNavigation.CreatePluginRoute(pluginId);
        MainContent.Content = new PluginHostedPage(
            _platformServices,
            pluginId,
            () => Navigate(MainNavigation.PluginExtensions),
            isSettings);
        SetActiveButton(GetNavigationButton(route));
    }

    /// <summary>
    /// Navigates to a route supported by this host. Unknown routes are ignored so
    /// plugin or WPF-only links cannot leave the content surface in a blank state.
    /// </summary>
    public void Navigate(string? route)
    {
        if (MainNavigation.TryGetPluginId(route, out _)
            || MainNavigation.TryGetPluginSettingsId(route, out _))
        {
            ShowPluginPage(route!.Trim());
            return;
        }

        switch (route?.Trim().ToLowerInvariant())
        {
            case MainNavigation.Dashboard:
                ShowDashboardPage();
                break;
            case MainNavigation.About:
                ShowAboutPage();
                break;
            case MainNavigation.Settings:
                ShowSettingsPage();
                break;
            case MainNavigation.Keyboard:
            case MainNavigation.Actions:
            case MainNavigation.Macro:
            case MainNavigation.WindowsOptimization:
            case MainNavigation.PluginExtensions:
                ShowFeaturePage(route!.Trim().ToLowerInvariant());
                break;
        }
    }

    public void RefreshForCulture()
    {
        ApplyTextDirection(LocalizationRuntime.CurrentCulture);
        Title = Localization.AvaloniaLocalization.GetString("Window_Title", "Universal Device Toolkit");

        ApplyNavigationVisibility();
        Navigate(_activePage);
        _ = UpdateHardwareDependentNavigationAsync();
        _ = RefreshPluginNavigationItemsAsync();

        // Settings stores are persisted without change events; re-applying the
        // persisted appearance on every shell refresh keeps theme, accent,
        // font and scale in sync without a dedicated settings watcher.
        AvaloniaThemeManager.Instance.Reapply();
    }

    /// <summary>
    /// Applies the persisted optional-navigation settings to the shell. This is
    /// kept on the window so settings changes take effect without restarting.
    /// </summary>
    public void ApplyNavigationVisibility()
    {
#if WINDOWS
        var settings = Services.WindowsAvaloniaSettingsService.SharedApplicationSettings
            .Store.NavigationItemsVisibility;
#else
        IReadOnlyDictionary<string, bool>? settings = null;
#endif

        SetNavigationVisibility(KeyboardButton, MainNavigation.Keyboard, settings, _keyboardHardwareAvailable);
        SetNavigationVisibility(ActionsButton, MainNavigation.Actions, settings);
        SetNavigationVisibility(MacroButton, MainNavigation.Macro, settings);
        SetNavigationVisibility(WindowsOptimizationButton, MainNavigation.WindowsOptimization, settings);
        SetNavigationVisibility(PluginExtensionsButton, MainNavigation.PluginExtensions, settings);
        SetNavigationVisibility(AboutButton, MainNavigation.About, settings);
        ApplyNavigationPaneState();

        var activeBaseRoute = MainNavigation.TryGetPluginId(_activePage, out _)
            || MainNavigation.TryGetPluginSettingsId(_activePage, out _)
            ? MainNavigation.PluginExtensions
            : _activePage;
        if (!NavigationVisibilityPolicy.IsVisible(activeBaseRoute, settings, _keyboardHardwareAvailable)
            && !string.Equals(_activePage, MainNavigation.Dashboard, StringComparison.OrdinalIgnoreCase))
        {
            ShowDashboardPage();
        }
    }

    private static void SetNavigationVisibility(
        Button button,
        string route,
        IReadOnlyDictionary<string, bool>? settings,
        bool? keyboardHardwareAvailable = null)
    {
        button.IsVisible = NavigationVisibilityPolicy.IsVisible(
            route,
            settings,
            keyboardHardwareAvailable);
    }

    private async Task UpdateHardwareDependentNavigationAsync()
    {
        try
        {
            // Use the same host-neutral capability state as the Keyboard page so
            // the shell does not expose a control that the current device cannot use.
            var state = await _platformServices
                .GetFeaturePageStateAsync("Keyboard")
                .ConfigureAwait(true);
            _keyboardHardwareAvailable = state.IsAvailable;
            ApplyNavigationVisibility();
        }
        catch
        {
            // Keep the entry visible when capability detection fails. The page can
            // still explain the unavailable state, matching the WPF fail-open path.
            _keyboardHardwareAvailable = null;
        }
    }

    /// <summary>
    /// Applies the persisted navigation-pane state to the Avalonia shell. The
    /// collapsed rail keeps icons and automation names while removing labels so
    /// the content surface receives the same usable width as the WPF host.
    /// </summary>
    public void ApplyNavigationPaneState()
    {
#if WINDOWS
        var expanded = Services.WindowsAvaloniaSettingsService.SharedApplicationSettings
            .Store.NavigationPaneExpanded;
#else
        const bool expanded = true;
#endif

        NavigationPane.Width = expanded ? 220 : 70;
        NavigationStack.Margin = expanded
            ? new Thickness(16, 12)
            : new Thickness(8, 12);
        NavigationPaneHost.Text = expanded ? "220" : "70";

        foreach (var label in new[]
                 {
                     DashboardLabel,
                     KeyboardLabel,
                     ActionsLabel,
                     MacroLabel,
                     WindowsOptimizationLabel,
                     PluginExtensionsLabel,
                     AboutLabel,
                     SettingsLabel,
                 })
        {
            label.IsVisible = expanded;
        }

        foreach (var entry in _pluginNavigationEntries.Values)
            entry.Label.IsVisible = expanded;

        foreach (var button in new[]
                 {
                     DashboardButton,
                     KeyboardButton,
                     ActionsButton,
                     MacroButton,
                     WindowsOptimizationButton,
                     PluginExtensionsButton,
                     SettingsButton,
                     AboutButton,
                 })
        {
            button.HorizontalContentAlignment = expanded
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Center;
            button.Padding = expanded ? new Thickness(16, 11) : new Thickness(10);
        }

        foreach (var entry in _pluginNavigationEntries.Values)
        {
            entry.Button.HorizontalContentAlignment = expanded
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Center;
            entry.Button.Padding = expanded ? new Thickness(16, 11) : new Thickness(10);
            entry.Button.IsVisible = PluginExtensionsButton.IsVisible;
        }

        NavigationToggleIcon.IconIdentifier = expanded ? "ArrowLeft24" : "ArrowRight24";
        var toggleText = expanded
            ? AvaloniaLocalization.GetString("SettingsPage_NavigationItems_Title", "Collapse navigation pane")
            : AvaloniaLocalization.GetString("SettingsPage_NavigationItems_Title", "Expand navigation pane");
        ToolTip.SetTip(NavigationToggleButton, toggleText);
        AutomationProperties.SetName(NavigationToggleButton, toggleText);
    }

    private void ApplyTextDirection(System.Globalization.CultureInfo culture)
    {
        FlowDirection = LocalizationCatalog.IsRightToLeft(culture)
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
    }

    private void SetActiveButton(Button activeButton)
    {
        foreach (var btn in new[]
                 {
                     DashboardButton,
                     KeyboardButton,
                     ActionsButton,
                     MacroButton,
                     WindowsOptimizationButton,
                     PluginExtensionsButton,
                     SettingsButton,
                     AboutButton,
                 })
        {
            btn.Classes.Set("active", btn == activeButton);
        }

        foreach (var entry in _pluginNavigationEntries.Values)
            entry.Button.Classes.Set("active", entry.Button == activeButton);
    }

    private Button GetNavigationButton(string route) => route switch
    {
        MainNavigation.Dashboard => DashboardButton,
        MainNavigation.Keyboard => KeyboardButton,
        MainNavigation.Actions => ActionsButton,
        MainNavigation.Macro => MacroButton,
        MainNavigation.WindowsOptimization => WindowsOptimizationButton,
        MainNavigation.PluginExtensions => PluginExtensionsButton,
        MainNavigation.Settings => SettingsButton,
        MainNavigation.About => AboutButton,
        _ when MainNavigation.TryGetPluginId(route, out var pluginId)
            && _pluginNavigationEntries.TryGetValue(pluginId, out var entry) => entry.Button,
        _ when MainNavigation.TryGetPluginSettingsId(route, out var settingsPluginId)
            && _pluginNavigationEntries.TryGetValue(settingsPluginId, out var settingsEntry) => settingsEntry.Button,
        _ when MainNavigation.TryGetPluginId(route, out _)
            || MainNavigation.TryGetPluginSettingsId(route, out _) => PluginExtensionsButton,
        _ => DashboardButton,
    };

    private sealed record PluginNavigationEntry(
        Button Button,
        Controls.LocalizedTextBlock Label,
        string PluginId);

}
