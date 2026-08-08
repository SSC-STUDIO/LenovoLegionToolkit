using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Avalonia.Windows;
using UniversalDeviceToolkit.Shared.Diagnostics;
using UniversalDeviceToolkit.Shared.Logging;
using UniversalDeviceToolkit.Platform.Linux;
using UniversalDeviceToolkit.Platform.MacOS;
#if WINDOWS
using System.Reflection;
using Autofac;
using UniversalDeviceToolkit.Avalonia.Startup;
using WindowsDeviceAdapter = UniversalDeviceToolkit.Platform.Windows.WindowsDeviceAdapter;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.CLI;
using UniversalDeviceToolkit.Lib.Automation.Pipeline;
using UniversalDeviceToolkit.Lib.Automation.Utils;
using UniversalDeviceToolkit.Lib.Automation.Optimization;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Features.Hybrid;
using UniversalDeviceToolkit.Lib.Features.Hybrid.Notify;
using UniversalDeviceToolkit.Lib.Features.PanelLogo;
using UniversalDeviceToolkit.Lib.Features.WhiteKeyboardBacklight;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.Utils;
using LibResource = UniversalDeviceToolkit.Lib.Resources.Resource;
using AutomationResource = UniversalDeviceToolkit.Lib.Automation.Resources.Resource;
using MacroResource = UniversalDeviceToolkit.Lib.Macro.Resources.Resource;
using PluginResource = UniversalDeviceToolkit.Lib.Plugins.Resources.Resource;
#endif

namespace UniversalDeviceToolkit.Avalonia;

public partial class App : Application
{
    public ICommand ShowCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand ExitCommand { get; }

    private TrayIcon? _trayIcon;
    private int _handlingFatalException;
    private int _shutdownStarted;
    private bool _exceptionHandlersRegistered;
    private object? _pendingUpdateReleaseInfo = null;
    private NativeMenuItem? _trayPipelinesItem;

#if WINDOWS
    private ApplicationSettings? _applicationSettings;
    private AvaloniaNotificationManager? _notificationManager;
    private AvaloniaSingleInstanceGuard? _singleInstanceGuard;
    private AvaloniaOsdOverlayController? _osdOverlay;
    private AvaloniaUpdateCheckCoordinator? _updateCheckCoordinator;
#endif

    /// <summary>Command-line startup switches parsed by Program.cs.</summary>
    public static AvaloniaStartupFlags StartupFlags => AvaloniaStartupFlags.Current;

    public static IPlatformServices PlatformServices { get; private set; } = new UnavailablePlatformServices();

    public App()
    {
        RegisterExceptionHandlers();
        var culture = LocalizationRuntime.Initialize();
        AvaloniaLocalization.ApplyCulture(culture);
#if WINDOWS
        ApplyWindowsResourceCulture(culture);
#endif
        LocalizationRuntime.CultureChanged += OnCultureChanged;
#if WINDOWS
        _applicationSettings = WindowsAvaloniaSettingsService.SharedApplicationSettings;
#endif
        PlatformServices = CreatePlatformServices();
        ShowCommand = new RelayCommand(ShowMainWindow);
        SettingsCommand = new RelayCommand(OpenSettings);
        ExitCommand = new RelayCommand(ExitApplication);
        DataContext = this;
    }

    private static IPlatformServices CreatePlatformServices()
    {
#if WINDOWS
        return WindowsPlatformServices.Create();
#else
        if (OperatingSystem.IsLinux())
            return new DeviceAdapterPlatformServices(new LinuxDeviceAdapter());

        if (OperatingSystem.IsMacOS())
            return new DeviceAdapterPlatformServices(new MacOSDeviceAdapter());

        return new UnavailablePlatformServices();
#endif
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
#if WINDOWS
            if (!AcquireSingleInstance())
            {
                desktop.Shutdown();
                base.OnFrameworkInitializationCompleted();
                return;
            }
#endif
            ApplyPersistedTheme();
            if (IsFirstRunLanguageSelection())
                RunFirstRunLanguageGateAsync(desktop);
            else
                CompleteStartup(desktop);
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void CompleteStartup(IClassicDesktopStyleApplicationLifetime desktop)
    {
#if WINDOWS
        _applicationSettings ??= WindowsAvaloniaSettingsService.SharedApplicationSettings;
#endif
        ApplyStartupFlags();
        desktop.MainWindow = new MainWindow(PlatformServices);
#if WINDOWS
        PluginHostContext.SetCurrent(new AvaloniaPluginHostContext(
            () => desktop.MainWindow as MainWindow));
#endif
        // Minimize to tray instead of closing
        desktop.MainWindow.Closing += OnMainWindowClosing;

        // Set up system tray icon programmatically
        SetupTrayIcon();
#if WINDOWS
        if (IoCContainer.TryResolve<UniversalDeviceToolkit.Lib.Notifications.IAppNotificationService>() is { } notificationService)
        {
            _notificationManager = new AvaloniaNotificationManager(
                _applicationSettings,
                () => desktop.MainWindow as MainWindow,
                notificationService);
        }
        _singleInstanceGuard!.StartListener(() =>
            global::Avalonia.Threading.Dispatcher.UIThread.Post(ShowMainWindow));
        _updateCheckCoordinator = AvaloniaUpdateCheckCoordinator.Create();
        SubscribeToUpdateCoordinator(_updateCheckCoordinator);
        RequestAutomaticUpdateCheck();
        _ = StartWindowsHostServicesAsync(desktop.MainWindow as MainWindow);
#endif
        if (StartupFlags.Minimized && desktop.MainWindow is { } window)
        {
            // Applied after the window opens so the persisted placement restore
            // (Opened handler) cannot override the requested minimized state.
            Dispatcher.UIThread.Post(() =>
            {
                if (window.WindowState != WindowState.Minimized)
                    window.WindowState = WindowState.Minimized;
            });
        }

        if (_pendingUpdateReleaseInfo is not null && desktop.MainWindow is MainWindow mainWindow)
            mainWindow.SetUpdateAvailable(_pendingUpdateReleaseInfo);

        Dispatcher.UIThread.Post(CheckPendingCrashReports, DispatcherPriority.Background);
    }

#if WINDOWS
    private static void InitializeWindowsServices(ApplicationSettings settings)
    {
        try
        {
            if (IoCContainer.TryResolve<ApplicationSettings>() is not null)
                return;

            IoCContainer.Initialize(
                builder =>
                {
                    builder.RegisterInstance(settings).As<ApplicationSettings>().SingleInstance();
                    builder.RegisterType<AvaloniaMainThreadDispatcher>()
                        .As<IMainThreadDispatcher>()
                        .SingleInstance();
                },
                new UniversalDeviceToolkit.Lib.IoCModule(),
                new UniversalDeviceToolkit.Lib.Plugins.IoCModule(),
                new UniversalDeviceToolkit.Lib.Automation.IoCModule(),
                new UniversalDeviceToolkit.Lib.Macro.IoCModule(),
                new WindowsOptimizationElevationIoCModule());
        }
        catch
        {
            // A host embedding Avalonia may have already initialized the shared container.
            // The feature bridge will fall back to adapter-only state when resolution fails.
        }
    }
#endif

    /// <summary>
    /// Applies the persisted appearance preferences through the theme manager
    /// (theme variant, accent color, font family and UI scale).
    /// </summary>
    private void ApplyPersistedTheme() => AvaloniaThemeManager.Instance.Apply();

    /// <summary>
    /// Applies command-line startup switches. Safe-start / reset switches are
    /// owned by the Windows startup coordinator and left untouched here.
    /// </summary>
    private void ApplyStartupFlags()
    {
        var flags = StartupFlags;
#if WINDOWS
        if (flags.IsTraceEnabled)
        {
            try
            {
                Log.Instance.IsTraceEnabled = true;
            }
            catch
            {
                // Trace remains best-effort; a logging failure must not block startup.
            }
        }

        try
        {
            IoCContainer.TryResolve<HttpClientFactory>()?
                .SetProxy(flags.ProxyUrl, flags.ProxyUsername, flags.ProxyPassword, flags.ProxyAllowAllCerts);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Failed to apply startup proxy flags.", ex);
        }

        try
        {
            if (IoCContainer.TryResolve<PowerModeFeature>() is { } powerMode)
                powerMode.AllowAllPowerModesOnBattery = flags.AllowAllPowerModesOnBattery;
            if (IoCContainer.TryResolve<RGBKeyboardBacklightController>() is { } rgbKeyboard)
                rgbKeyboard.ForceDisable = flags.ForceDisableRgbKeyboardSupport;
            if (IoCContainer.TryResolve<SpectrumKeyboardBacklightController>() is { } spectrumKeyboard)
                spectrumKeyboard.ForceDisable = flags.ForceDisableSpectrumKeyboardSupport;
            if (flags.ForceDisableLenovoLighting)
            {
                if (IoCContainer.TryResolve<WhiteKeyboardLenovoLightingBacklightFeature>() is { } whiteLighting)
                    whiteLighting.ForceDisable = true;
                if (IoCContainer.TryResolve<PanelLogoLenovoLightingBacklightFeature>() is { } panelLighting)
                    panelLighting.ForceDisable = true;
                if (IoCContainer.TryResolve<PortsBacklightFeature>() is { } portsLighting)
                    portsLighting.ForceDisable = true;
            }
            if (IoCContainer.TryResolve<IGPUModeFeature>() is { } gpuMode)
                gpuMode.ExperimentalGPUWorkingMode = flags.ExperimentalGPUWorkingMode;
            if (IoCContainer.TryResolve<DGPUNotify>() is { } dgpuNotify)
                dgpuNotify.ExperimentalGPUWorkingMode = flags.ExperimentalGPUWorkingMode;
            if (flags.DisableUpdateChecker && IoCContainer.TryResolve<UpdateChecker>() is { } updateChecker)
            {
                updateChecker.Disable = true;
                updateChecker.DisableReason = AvaloniaStartupFlags.DisableUpdateCheckerSwitch;
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Failed to apply startup hardware flags.", ex);
        }
#endif
    }

    /// <summary>
    /// The first-run language selector runs instead of the main window whenever
    /// no language has been persisted yet (missing "lang" marker file).
    /// </summary>
    private static bool IsFirstRunLanguageSelection()
    {
        try
        {
            return !File.Exists(LocalizationRuntime.LanguageFilePath);
        }
        catch
        {
            return false;
        }
    }

    private async void RunFirstRunLanguageGateAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var selector = new AvaloniaLanguageSelectorWindow(AvaloniaLanguagePackServiceFactory.Create());
        desktop.MainWindow = selector;
        selector.Show();
        var outcome = await selector.GateOutcome.ConfigureAwait(true);
        if (outcome == AvaloniaLanguageSelectorWindow.LanguageGateOutcome.Exit)
        {
            desktop.Shutdown();
            return;
        }

        CompleteStartup(desktop);
    }

    private void SetupTrayIcon()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;

        var menu = new NativeMenu
        {
            Items =
            {
                CreateNavigationMenuItem("Nav_Dashboard", "Dashboard", MainNavigation.Dashboard),
                CreateNavigationMenuItem("MainWindow_NavigationItem_Keyboard", "Keyboard", MainNavigation.Keyboard),
                CreateNavigationMenuItem("MainWindow_NavigationItem_Actions", "Automation", MainNavigation.Actions),
                CreateNavigationMenuItem("MainWindow_NavigationItem_Macro", "Macro", MainNavigation.Macro),
                CreateNavigationMenuItem("MainWindow_NavigationItem_WindowsOptimization", "System optimization", MainNavigation.WindowsOptimization),
                CreateNavigationMenuItem("MainWindow_NavigationItem_PluginExtensions", "Plugin Extensions", MainNavigation.PluginExtensions),
                CreateNavigationMenuItem("Nav_About", "About", MainNavigation.About),
                new NativeMenuItemSeparator(),
            }
        };

        _trayPipelinesItem = new NativeMenuItem(AvaloniaLocalization.GetString("Tray_Pipelines", "Automation pipelines"));
        _trayPipelinesItem.IsVisible = false;
        menu.Items.Add(_trayPipelinesItem);
#if WINDOWS
        _ = RefreshTrayPipelinesMenuAsync(_trayPipelinesItem);
#endif
        menu.Items.Add(new NativeMenuItem(AvaloniaLocalization.GetString("Nav_Show", "Show")) { Command = ShowCommand });
        menu.Items.Add(new NativeMenuItem(AvaloniaLocalization.GetString("Nav_Settings", "Settings")) { Command = SettingsCommand });
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(new NativeMenuItem(AvaloniaLocalization.GetString("Nav_Exit", "Exit")) { Command = ExitCommand });

        _trayIcon = new TrayIcon
        {
            Menu = menu,
        };
        if (!StartupFlags.DisableTrayTooltip)
            _trayIcon.ToolTipText = AvaloniaLocalization.GetString("Window_Title", "Universal Device Toolkit");

        // Try to load icon from Avalonia resource; falls back gracefully if unavailable
        try
        {
            var uri = new Uri("avares://UniversalDeviceToolkit.Avalonia/Assets/udt-icon.ico");
            _trayIcon.Icon = new WindowIcon(new global::Avalonia.Media.Imaging.Bitmap(AssetLoader.Open(uri)));
        }
        catch
        {
            // Icon resource not found; tray icon will display without a custom icon
        }
    }

    private static NativeMenuItem CreateNavigationMenuItem(string localizationKey, string fallback, string route)
    {
        var label = AvaloniaLocalization.GetString(localizationKey, fallback);
        return new NativeMenuItem(label)
        {
            Command = new RelayCommand(() => NavigateMainWindow(route)),
        };
    }

    private static void NavigateMainWindow(string route)
    {
        if (Application.Current is not App { ApplicationLifetime: IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow mainWindow } })
            return;

        mainWindow.RestoreFromTray();
        mainWindow.Navigate(route);
    }

#if WINDOWS
    /// <summary>
    /// Best-effort manual-pipeline quick actions under the tray "Automation
    /// pipelines" submenu (WPF TrayHelper parity). Manual pipelines are those
    /// without a trigger; the list is refreshed on every tray rebuild.
    /// </summary>
    private async Task RefreshTrayPipelinesMenuAsync(NativeMenuItem item)
    {
        var submenu = new NativeMenu();
        try
        {
            var automation = IoCContainer.TryResolve<AutomationProcessor>();
            if (automation is null)
            {
                item.IsVisible = false;
                return;
            }

            var pipelines = await automation.GetPipelinesAsync().ConfigureAwait(true);
            foreach (var pipeline in pipelines.Where(p => p.Trigger is null))
            {
                var displayName = PipelineNameLocalizer.LocalizeStoredName(pipeline.Name)
                    ?? pipeline.Name
                    ?? AvaloniaLocalization.GetString("Unnamed", "Unnamed");
                var menuItem = new NativeMenuItem(displayName);
                var captured = pipeline;
                menuItem.Click += (_, _) => _ = RunPipelineFromTrayAsync(captured);
                submenu.Items.Add(menuItem);
            }

            if (submenu.Items.Count > 0)
            {
                item.Menu = submenu;
                item.IsVisible = true;
            }
            else
            {
                item.Menu = null;
                item.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Failed to refresh tray automation pipelines.", ex);
            item.Menu = null;
            item.IsVisible = false;
        }
    }

    private static async Task RunPipelineFromTrayAsync(AutomationPipeline pipeline)
    {
        try
        {
            if (IoCContainer.TryResolve<AutomationProcessor>() is { } automation)
                await automation.RunNowAsync(pipeline).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Failed to run automation pipeline from tray.", ex);
        }
    }
#endif

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        AvaloniaLocalization.ApplyCulture(e.Culture);
#if WINDOWS
        ApplyWindowsResourceCulture(e.Culture);
        // Keep per-plugin language overrides intact. Applying the app culture
        // directly to every loaded plugin resource would overwrite overrides
        // until a settings page happened to create PluginLanguageService.
        PluginLanguageService.Current.ApplyForAllLoadedPlugins();
#endif

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is MainWindow mainWindow)
        {
            mainWindow.RefreshForCulture();
            SetupTrayIcon();
        }
    }

#if WINDOWS
    private static void ApplyWindowsResourceCulture(System.Globalization.CultureInfo culture)
    {
        // Shared Windows services use these generated Resource classes directly;
        // keep them in lockstep with the Avalonia localizer after every language change.
        LibResource.Culture = culture;
        AutomationResource.Culture = culture;
        MacroResource.Culture = culture;
        PluginResource.Culture = culture;
    }
#endif

    private void OnMainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is not Window window)
            return;

#if WINDOWS
        var settings = _applicationSettings?.Store;
        var action = AvaloniaDesktopLifecyclePolicy.ResolveCloseAction(
            IsExiting,
            settings?.MinimizeOnClose == true,
            settings?.MinimizeToTray == true);
#else
        var action = AvaloniaDesktopLifecyclePolicy.ResolveCloseAction(
            IsExiting,
            minimizeOnClose: false,
            minimizeToTray: false);
#endif

        switch (action)
        {
            case MainWindowCloseAction.AllowClose:
                return;
            case MainWindowCloseAction.Minimize:
                e.Cancel = true;
                window.WindowState = WindowState.Minimized;
                return;
            case MainWindowCloseAction.HideToTray:
                e.Cancel = true;
                window.ShowInTaskbar = false;
                window.Hide();
                return;
            case MainWindowCloseAction.ExitApplication:
                // Avalonia would otherwise close the last window directly and
                // bypass the Windows service/plug-in shutdown sequence.
                e.Cancel = true;
                ExitApplication();
                return;
        }
    }

    internal bool MinimizeToTrayEnabled
    {
        get
        {
#if WINDOWS
            return _applicationSettings?.Store.MinimizeToTray == true;
#else
            return false;
#endif
        }
    }

    private bool IsExiting { get; set; }

    private void ShowMainWindow()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is MainWindow mainWindow)
            {
                mainWindow.RestoreFromTray();
                RequestAutomaticUpdateCheck();
                return;
            }

            var window = desktop.MainWindow;
            if (window is null)
                return;

            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;

            window.Show();
            window.Activate();
            window.InvalidateVisual();
            RequestAutomaticUpdateCheck();
        }
    }

    private void RequestAutomaticUpdateCheck()
    {
#if WINDOWS
        if (StartupFlags.DisableUpdateChecker)
            return;

        _ = _updateCheckCoordinator?.CheckAsync();
#endif
    }

    private void OpenSettings()
    {
        ShowMainWindow();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is MainWindow mainWindow)
        {
            mainWindow.ShowSettingsPage();
        }
    }

    private void ExitApplication() => ExitApplication(null);

    private void ExitApplication(int? exitCode) => _ = ExitApplicationAsync(exitCode);

    private async Task ExitApplicationAsync(int? exitCode)
    {
        if (Interlocked.CompareExchange(ref _shutdownStarted, 1, 0) != 0)
            return;

        IsExiting = true;
        UnregisterExceptionHandlers();
#if WINDOWS
        _osdOverlay?.Dispose();
        _osdOverlay = null;
        try
        {
            await new AvaloniaWindowsShutdownCoordinator().StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Avalonia Windows host shutdown failed.", ex);
        }

        _notificationManager?.Dispose();
        _notificationManager = null;
        _singleInstanceGuard?.Dispose();
        _singleInstanceGuard = null;
        PluginHostContext.Reset();
        try
        {
            IoCContainer.TryResolve<UniversalDeviceToolkit.Abstractions.Lifecycle.ICliHostLifecycle>()?
                .StopAsync()
                .GetAwaiter()
                .GetResult();
        }
        catch
        {
            // Process teardown remains best effort; the named pipe is scoped to this process.
        }
#endif
        _trayIcon?.Dispose();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (exitCode is { } code)
                desktop.Shutdown(code);
            else
                desktop.Shutdown();
        }
        else if (exitCode is { } code)
            Environment.ExitCode = code;
    }

    /// <summary>
    /// The update coordinator exposes its availability through the
    /// UpdateAvailableChanged / ShowUpdateAsync API owned by the update-check
    /// agent. The bridge consumes those members reflectively so this shell
    /// compiles and runs safely whether or not the API is present yet.
    /// </summary>
#if WINDOWS
    private void SubscribeToUpdateCoordinator(AvaloniaUpdateCheckCoordinator? coordinator)
    {
        if (coordinator is null)
            return;

        try
        {
            var eventInfo = coordinator.GetType().GetEvent("UpdateAvailableChanged");
            if (eventInfo is null
                || eventInfo.EventHandlerType is not { IsGenericType: true } handlerType
                || handlerType.GetGenericArguments() is not [{ } payloadType])
                return;

            var binder = (IUpdateEventBinder)Activator.CreateInstance(
                typeof(UpdateEventBinder<>).MakeGenericType(payloadType))!;
            binder.Attach(coordinator, eventInfo, OnUpdateAvailableChanged);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Update coordinator event bridge is unavailable.", ex);
        }
    }

    private void OnUpdateAvailableChanged(object? releaseInfo)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _pendingUpdateReleaseInfo = releaseInfo;
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow mainWindow })
                mainWindow.SetUpdateAvailable(releaseInfo);
        });
    }

    private interface IUpdateEventBinder
    {
        void Attach(object source, EventInfo eventInfo, Action<object?> payloadHandler);
    }

    private sealed class UpdateEventBinder<T> : IUpdateEventBinder
    {
        public void Attach(object source, EventInfo eventInfo, Action<object?> payloadHandler)
        {
            eventInfo.AddEventHandler(source, new Action<T>(payload => payloadHandler(payload)));
        }
    }
#endif

    /// <summary>
    /// Opens the update dialog owned by the update coordinator. Returns without
    /// doing anything when the coordinator or its ShowUpdateAsync API is absent.
    /// </summary>
    internal async Task ShowUpdateDialogAsync(MainWindow owner)
    {
#if WINDOWS
        var coordinator = _updateCheckCoordinator;
        if (coordinator is null)
            return;

        try
        {
            var method = coordinator.GetType().GetMethod("ShowUpdateAsync", new[] { typeof(Window) });
            if (method is null)
                return;

            if (method.ReturnType == typeof(Task))
                await ((Task)method.Invoke(coordinator, [owner])!).ConfigureAwait(true);
            else if (method.ReturnType == typeof(void))
                method.Invoke(coordinator, [owner]);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Failed to show the update dialog.", ex);
        }
#endif
    }

    private void RegisterExceptionHandlers()
    {
        if (_exceptionHandlersRegistered)
            return;

        _exceptionHandlersRegistered = true;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
    }

    private void UnregisterExceptionHandlers()
    {
        if (!_exceptionHandlersRegistered)
            return;

        _exceptionHandlersRegistered = false;
        AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        Dispatcher.UIThread.UnhandledException -= OnDispatcherUnhandledException;
    }

    private void OnAppDomainUnhandledException(object? sender, UnhandledExceptionEventArgs args)
    {
        var exception = args.ExceptionObject as Exception
            ?? new InvalidOperationException($"Unknown unhandled exception: {args.ExceptionObject}");
        HandleFatalException(exception, "AppDomain", 100);
    }

    private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs args)
    {
        args.Handled = true;
        HandleFatalException(args.Exception, "Dispatcher", 101);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        try
        {
            SharedLog.Error("Avalonia unobserved task exception.", args.Exception);
            CrashReportStore.Save(args.Exception, "TaskScheduler");
        }
        finally
        {
            args.SetObserved();
        }
    }

    private void HandleFatalException(Exception exception, string source, int exitCode)
    {
        if (Interlocked.CompareExchange(ref _handlingFatalException, 1, 0) != 0)
        {
            Environment.FailFast($"Fatal error: re-entered Avalonia {source} exception handler", exception);
            return;
        }

        try
        {
            SharedLog.Error($"Avalonia {source} unhandled exception.", exception);
            CrashReportStore.Save(exception, source);
        }
        catch
        {
            // A fatal exception must still close the host if reporting itself fails.
        }
        finally
        {
            try
            {
                if (Dispatcher.UIThread.CheckAccess())
                    ExitApplication(exitCode);
                else
                    Dispatcher.UIThread.Post(() => ExitApplication(exitCode));
            }
            catch
            {
                Environment.Exit(exitCode);
            }
        }
    }

    private void CheckPendingCrashReports()
    {
        try
        {
            CrashReportStore.CleanupOld();
            var reports = CrashReportStore.GetUnsent();
            if (reports.Count == 0)
                return;

            var mostRecent = reports
                .OrderByDescending(path => File.GetCreationTimeUtc(path))
                .FirstOrDefault();
            if (string.IsNullOrWhiteSpace(mostRecent))
                return;

            foreach (var report in reports.Where(path => !string.Equals(path, mostRecent, StringComparison.OrdinalIgnoreCase)))
                CrashReportStore.Delete(report);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: Window owner })
                new AvaloniaCrashReportWindow(mostRecent).Show(owner);
        }
        catch (Exception exception)
        {
            SharedLog.Warning("Failed to show pending Avalonia crash report.", exception);
        }
    }

#if WINDOWS
    private bool AcquireSingleInstance()
    {
        _singleInstanceGuard = new AvaloniaSingleInstanceGuard();
        if (_singleInstanceGuard.TryAcquire())
            return true;

        _singleInstanceGuard.Dispose();
        _singleInstanceGuard = null;
        return false;
    }

    private async Task StartWindowsHostServicesAsync(MainWindow? mainWindow)
    {
        // Autofac registers several hardware listeners with AutoActivate. Building
        // that graph synchronously in App's constructor prevents Avalonia from
        // entering its desktop event loop and leaves the process windowless.
        // Finish the host graph after the shell has been created instead.
        try
        {
            await Task.Run(() => InitializeWindowsServices(_applicationSettings!))
                .ConfigureAwait(false);
            if (PlatformServices is WindowsPlatformServices hostPlatformServices)
                hostPlatformServices.InitializeHostServices();

            if (_notificationManager is null
                && mainWindow is not null
                && _applicationSettings is { } applicationSettings
                && IoCContainer.TryResolve<UniversalDeviceToolkit.Lib.Notifications.IAppNotificationService>() is { } notificationService)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _notificationManager ??= new AvaloniaNotificationManager(
                        applicationSettings,
                        () => mainWindow,
                        notificationService);
                });
            }
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Avalonia deferred Windows service initialization failed.", ex);
        }

        await new AvaloniaStartupDeviceSetupCoordinator()
            .RunIfNeededAsync(mainWindow)
            .ConfigureAwait(false);

        if (!await new AvaloniaStartupCompatibilityCoordinator()
                .EnsureCompatibleAsync(mainWindow)
                .ConfigureAwait(false))
        {
            Dispatcher.UIThread.Post(() => ExitApplication(202));
            return;
        }

        await InitializePluginsAsync().ConfigureAwait(false);

        await new AvaloniaWindowsStartupCoordinator().RunAsync().ConfigureAwait(false);
        if (PlatformServices is WindowsPlatformServices windowsPlatformServices)
        {
            try
            {
                await windowsPlatformServices.StartAutomationForHostAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Instance.Trace("Avalonia automation startup failed.", ex);
            }
        }

        if (mainWindow is not null)
            Dispatcher.UIThread.Post(() =>
            {
                _ = mainWindow.RefreshPluginNavigationAsync();
                // Best effort: rebuild the tray menu so newly visible plugin
                // routes and pipeline quick actions stay current.
                SetupTrayIcon();
            });

        Dispatcher.UIThread.Post(() =>
        {
            _osdOverlay ??= new AvaloniaOsdOverlayController(PlatformServices);
            _osdOverlay.Initialize();
        });

        try
        {
            var lifecycle = IoCContainer.TryResolve<UniversalDeviceToolkit.Abstractions.Lifecycle.ICliHostLifecycle>();
            if (lifecycle is not null)
                await lifecycle.StartStopIfNeededAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Avalonia Windows host service startup failed.", ex);
        }
    }

    private async Task InitializePluginsAsync()
    {
        try
        {
            // WPF parity: skip the plugin directory scan entirely when safe-start
            // is active, extensions are disabled, or no plugins are installed.
            if (StartupFlags.SafeStart)
            {
                Log.Instance.Trace("Safe-start active; skipping plugin discovery and loading.");
                return;
            }

            var settings = _applicationSettings?.Store;
            if (settings is not { ExtensionsEnabled: true })
            {
                Log.Instance.Trace("Extensions disabled in settings; skipping plugin directory scan.");
                return;
            }

            if (!HasInstalledPlugins())
            {
                Log.Instance.Trace("No installed plugins found; skipping plugin directory scan.");
                return;
            }

            var pluginManager = IoCContainer.TryResolve<IPluginManager>();
            if (pluginManager is null)
                return;

            pluginManager.PruneRetiredPlugins();
            await pluginManager.ScanAndLoadPluginsAsync().ConfigureAwait(false);
            PluginLanguageService.Current.ApplyForAllLoadedPlugins();
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Avalonia plugin startup failed.", ex);
        }
    }

    private static bool HasInstalledPlugins()
    {
        try
        {
            return PluginPaths.GetAllPossiblePluginsDirectories()
                .Where(Directory.Exists)
                .SelectMany(path => Directory.EnumerateDirectories(path))
                .Any(PluginPaths.ContainsPlugin);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Failed to enumerate installed plugins.", ex);
            return false;
        }
    }
#endif
}

/// <summary>
/// Simple ICommand implementation for tray menu commands.
/// </summary>
internal sealed class RelayCommand : ICommand
{
    private readonly Action _execute;

    public RelayCommand(Action execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
