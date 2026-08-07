using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Avalonia.Windows;
using UniversalDeviceToolkit.Shared.Settings;
using UniversalDeviceToolkit.Shared.Diagnostics;
using UniversalDeviceToolkit.Shared.Logging;
using UniversalDeviceToolkit.Platform.Linux;
using UniversalDeviceToolkit.Platform.MacOS;
#if WINDOWS
using Autofac;
using UniversalDeviceToolkit.Avalonia.Startup;
using WindowsDeviceAdapter = UniversalDeviceToolkit.Platform.Windows.WindowsDeviceAdapter;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation.CLI;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Optimization;
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

#if WINDOWS
    private ApplicationSettings? _applicationSettings;
    private AvaloniaNotificationManager? _notificationManager;
    private AvaloniaSingleInstanceGuard? _singleInstanceGuard;
    private AvaloniaOsdOverlayController? _osdOverlay;
    private AvaloniaUpdateCheckCoordinator? _updateCheckCoordinator;
#endif

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
        InitializeWindowsServices(_applicationSettings);
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
#if WINDOWS
            _applicationSettings ??= WindowsAvaloniaSettingsService.SharedApplicationSettings;
#endif
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
            _notificationManager = new AvaloniaNotificationManager(
                _applicationSettings,
                () => desktop.MainWindow as MainWindow,
                IoCContainer.Resolve<UniversalDeviceToolkit.Lib.Notifications.IAppNotificationService>());
            _singleInstanceGuard!.StartListener(() =>
                global::Avalonia.Threading.Dispatcher.UIThread.Post(ShowMainWindow));
            _updateCheckCoordinator = AvaloniaUpdateCheckCoordinator.Create();
            RequestAutomaticUpdateCheck();
            _ = StartWindowsHostServicesAsync(desktop.MainWindow as MainWindow);
#endif
            Dispatcher.UIThread.Post(CheckPendingCrashReports, DispatcherPriority.Background);
        }
        base.OnFrameworkInitializationCompleted();
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

    private void ApplyPersistedTheme()
    {
        var theme = new AvaloniaThemePreferences().Store.Theme;
        RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private void SetupTrayIcon()
    {
        _trayIcon?.Dispose();
        _trayIcon = null;

        var menu = new NativeMenu
        {
            Items =
            {
                new NativeMenuItem(AvaloniaLocalization.GetString("Nav_Show", "Show")) { Command = ShowCommand },
                new NativeMenuItem(AvaloniaLocalization.GetString("Nav_Settings", "Settings")) { Command = SettingsCommand },
                new NativeMenuItemSeparator(),
                new NativeMenuItem(AvaloniaLocalization.GetString("Nav_Exit", "Exit")) { Command = ExitCommand },
            }
        };

        _trayIcon = new TrayIcon
        {
            ToolTipText = AvaloniaLocalization.GetString("Window_Title", "Universal Device Toolkit"),
            Menu = menu,
        };

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

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e)
    {
        AvaloniaLocalization.ApplyCulture(e.Culture);
#if WINDOWS
        ApplyWindowsResourceCulture(e.Culture);
        AvaloniaPluginResourceCulture.Apply(e.Culture);
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
        if (sender is Window window && !IsExiting)
        {
#if WINDOWS
            var settings = _applicationSettings?.Store;
            if (settings?.MinimizeOnClose == true)
            {
                e.Cancel = true;
                window.WindowState = WindowState.Minimized;
                return;
            }

            if (settings?.MinimizeToTray == true)
            {
                e.Cancel = true;
                window.ShowInTaskbar = false;
                window.Hide();
                return;
            }
#endif
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
            Dispatcher.UIThread.Post(() => _ = mainWindow.RefreshPluginNavigationAsync());

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

    private static async Task InitializePluginsAsync()
    {
        try
        {
            var pluginManager = IoCContainer.TryResolve<IPluginManager>();
            if (pluginManager is null)
                return;

            pluginManager.PruneRetiredPlugins();
            await pluginManager.ScanAndLoadPluginsAsync().ConfigureAwait(false);
            AvaloniaPluginResourceCulture.Apply(LocalizationRuntime.CurrentCulture);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace("Avalonia plugin startup failed.", ex);
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
