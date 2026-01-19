#if !DEBUG
using LenovoLegionToolkit.Lib.System;
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Automation;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Features.Hybrid;
using LenovoLegionToolkit.Lib.Features.Hybrid.Notify;
using LenovoLegionToolkit.Lib.Features.PanelLogo;
using LenovoLegionToolkit.Lib.Features.WhiteKeyboardBacklight;
using LenovoLegionToolkit.Lib.Integrations;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.Macro;
using LenovoLegionToolkit.Lib.Services;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.CLI;
using LenovoLegionToolkit.WPF.Extensions;
using LenovoLegionToolkit.WPF.Pages;
using LenovoLegionToolkit.WPF.Resources;
using LenovoLegionToolkit.WPF.Utils;
using LenovoLegionToolkit.WPF.Windows;
using LenovoLegionToolkit.WPF.Windows.Utils;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using WinFormsApp = System.Windows.Forms.Application;
using WinFormsHighDpiMode = System.Windows.Forms.HighDpiMode;

namespace LenovoLegionToolkit.WPF;

public partial class App
{
    private const string MUTEX_NAME = "LenovoLegionToolkit_Mutex_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const string EVENT_NAME = "LenovoLegionToolkit_Event_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const int BACKGROUND_INITIALIZATION_WAIT_TIMEOUT_MS = 3000;

    private Mutex? _singleInstanceMutex;
    private EventWaitHandle? _singleInstanceWaitHandle;
    private Task? _backgroundInitializationTask;
    private CancellationTokenSource? _backgroundInitializationCancellationTokenSource;
    private readonly object _shutdownLock = new();
    private Task? _shutdownTask;
    private bool _exitRequested;
    private bool _shutdownInvoked;
    private bool _inExitHandler;

    public new static App Current => (App)Application.Current;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
#if DEBUG
        if (Debugger.IsAttached)
        {
            Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName)
                .Where(p => p.Id != Environment.ProcessId)
                .ForEach(p =>
                {
                    p.Kill();
                    p.WaitForExit();
                });
        }
#endif

        var flags = new Flags(e.Args);

        Log.Instance.IsTraceEnabled = flags.IsTraceEnabled;

        // Ensure native shell logger writes to the same log file
        Environment.SetEnvironmentVariable("LLT_LOG_PATH", Log.Instance.LogPath);

        AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Flags: {flags}");

        EnsureSingleInstance();

        await LocalizationHelper.SetLanguageAsync(true);

        // Note: ApplicationSettings is created here before IoC initialization for compatibility check.
        // This is safe because ApplicationSettings uses a shared storage mechanism, so changes will
        // be reflected in the IoC-resolved instance later. However, we should use the IoC instance
        // after initialization for consistency.
        var applicationSettings = new ApplicationSettings();

        if (!flags.SkipCompatibilityCheck)
        {
            try
            {
                // Check compatibility - IsCompatibleAsync already includes basic compatibility check
                var (isCompatible, mi) = await Compatibility.IsCompatibleAsync();

                // If check fails, show the unsupported window only once
                if (!isCompatible)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Incompatible system detected. [Vendor={mi.Vendor}, Model={mi.Model}, MachineType={mi.MachineType}, BIOS={mi.BiosVersion}]");

                    // Use the local instance for reading settings before IoC initialization
                    var suppressWarning = applicationSettings.Store.DisableUnsupportedHardwareWarning;
                    var shouldContinue = false;

                    if (suppressWarning)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Compatibility warning suppressed via application settings.");

                        shouldContinue = true;
                    }
                    else
                    {
                        var unsupportedWindow = new UnsupportedWindow(mi);
                        unsupportedWindow.Show();

                        shouldContinue = await unsupportedWindow.ShouldContinue;
                    }

                    if (shouldContinue)
                    {
                        Log.Instance.IsTraceEnabled = true;

                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Compatibility check OVERRIDE. [Vendor={mi.Vendor}, Model={mi.Model}, MachineType={mi.MachineType}, version={Assembly.GetEntryAssembly()?.GetName().Version}, build={Assembly.GetEntryAssembly()?.GetBuildDateTimeString() ?? string.Empty}]");
                    }
                    else
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Shutting down... [Vendor={mi.Vendor}, Model={mi.Model}, MachineType={mi.MachineType}]");

                        Shutdown(202);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                // Always log error details, regardless of trace flag
                // Use Error level to ensure it's always written to log file
                Log.Instance.Error($"Failed to check device compatibility: {ex.Message}", ex);
                
                // Log additional trace details if trace is enabled
                if (Log.Instance.IsTraceEnabled)
                {
                    Log.Instance.Trace($"Compatibility check exception details:", ex);
                    if (ex.InnerException != null)
                        Log.Instance.Trace($"Inner exception: {ex.InnerException.Message}", ex.InnerException);
                    
                    // Log stack trace for detailed debugging
                    Log.Instance.Trace($"Stack trace: {ex.StackTrace}");
                }
                
                // Force flush log entries to file immediately before showing error dialog
                // This ensures error is written even if program exits soon after
                try
                {
                    Log.Instance.Flush();
                }
                catch
                {
                    // Ignore flush errors - we still want to show the error dialog
                }

                // Show modern error window with detailed information
                var errorWindow = new Windows.Utils.CompatibilityCheckErrorWindow(ex);
                errorWindow.ShowDialog();
                
                Shutdown(200);
                return;
            }
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Starting... [version={Assembly.GetEntryAssembly()?.GetName().Version}, build={Assembly.GetEntryAssembly()?.GetBuildDateTimeString()}, os={Environment.OSVersion}, dotnet={Environment.Version}]");

        WinFormsApp.SetHighDpiMode(WinFormsHighDpiMode.PerMonitorV2);
        RenderOptions.ProcessRenderMode = GetPreferredRenderMode();

        IoCContainer.Initialize(
            new Lib.IoCModule(),
            new Lib.Automation.IoCModule(),
            new Lib.Macro.IoCModule(),
            new IoCModule()
        );

        IoCContainer.Resolve<HttpClientFactory>().SetProxy(flags.ProxyUrl, flags.ProxyUsername, flags.ProxyPassword, flags.ProxyAllowAllCerts);

        IoCContainer.Resolve<PowerModeFeature>().AllowAllPowerModesOnBattery = flags.AllowAllPowerModesOnBattery;
        IoCContainer.Resolve<RGBKeyboardBacklightController>().ForceDisable = flags.ForceDisableRgbKeyboardSupport;
        IoCContainer.Resolve<SpectrumKeyboardBacklightController>().ForceDisable = flags.ForceDisableSpectrumKeyboardSupport;
        IoCContainer.Resolve<WhiteKeyboardLenovoLightingBacklightFeature>().ForceDisable = flags.ForceDisableLenovoLighting;
        IoCContainer.Resolve<PanelLogoLenovoLightingBacklightFeature>().ForceDisable = flags.ForceDisableLenovoLighting;
        IoCContainer.Resolve<PortsBacklightFeature>().ForceDisable = flags.ForceDisableLenovoLighting;
        IoCContainer.Resolve<IGPUModeFeature>().ExperimentalGPUWorkingMode = flags.ExperimentalGPUWorkingMode;
        IoCContainer.Resolve<DGPUNotify>().ExperimentalGPUWorkingMode = flags.ExperimentalGPUWorkingMode;
        IoCContainer.Resolve<UpdateChecker>().Disable = flags.DisableUpdateChecker;

        AutomationPage.EnableHybridModeAutomation = flags.EnableHybridModeAutomation;

        // Initialize plugins
        InitializePlugins();
        
        // Apply plugin-specific language settings after plugins are loaded
        LocalizationHelper.SetPluginResourceCultures();

        StartBackgroundInitialization();

        var mainWindow = new MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            TrayTooltipEnabled = !flags.DisableTrayTooltip,
            DisableConflictingSoftwareWarning = flags.DisableConflictingSoftwareWarning
        };
        MainWindow = mainWindow;

        IoCContainer.Resolve<ThemeManager>().Apply();

        if (flags.Minimized)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Sending MainWindow to tray...");

            mainWindow.WindowState = WindowState.Minimized;
            mainWindow.Show();
            mainWindow.SendToTray();
        }
        else
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Showing MainWindow...");

            mainWindow.Show();
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Start up complete");
    }

    private static RenderMode GetPreferredRenderMode()
    {
        try
        {
            // RenderCapability.Tier stores the rendering tier value in the upper 16 bits
            // We need to right shift by 16 bits to extract the tier value (0-3)
            // where 0 = no hardware acceleration, 1 = partial, 2+ = full hardware acceleration
            var tier = RenderCapability.Tier >> 16;
            return tier >= 2 ? RenderMode.Default : RenderMode.SoftwareOnly;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Falling back to software rendering.", ex);

            return RenderMode.SoftwareOnly;
        }
    }

    private static void InitializePlugins()
    {
        try
        {
            var pluginManager = IoCContainer.Resolve<IPluginManager>();
            
            // System Optimization and Tools are now default interfaces, not plugins
            // They are registered directly in MainWindow.xaml as NavigationItems
            // No need to register them as plugins

            // Scan and load plugins from the plugins directory
            // This will automatically discover and register external plugins
            pluginManager.ScanAndLoadPlugins();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugins initialized successfully.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to initialize plugins.", ex);
        }
    }

    private void StartBackgroundInitialization()
    {
        _backgroundInitializationCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _backgroundInitializationCancellationTokenSource.Token;

        var initializationSteps = new Func<Task>[]
        {
            LogSoftwareStatusAsync,
            InitPowerModeFeatureAsync,
            InitBatteryFeatureAsync,
            InitRgbKeyboardControllerAsync,
            InitSpectrumKeyboardControllerAsync,
            InitGpuOverclockControllerAsync,
            InitHybridModeAsync,
            InitAutomationProcessorAsync
        };

        var serviceStartSteps = new Func<Task>[]
        {
            () => IoCContainer.Resolve<AIController>().StartIfNeededAsync(),
            () => IoCContainer.Resolve<HWiNFOIntegration>().StartStopIfNeededAsync(),
            () => IoCContainer.Resolve<IpcServer>().StartStopIfNeededAsync(),
            () => IoCContainer.Resolve<BatteryDischargeRateMonitorService>().StartStopIfNeededAsync()
        };

        _backgroundInitializationTask = Task.Run(async () =>
        {
            try
            {
                // Check for cancellation before starting initialization steps
                cancellationToken.ThrowIfCancellationRequested();

                // Run initialization steps in parallel where possible to improve startup performance
                var initializationTasks = initializationSteps.Select(step => Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await step().ConfigureAwait(false);
                })).ToArray();
                await Task.WhenAll(initializationTasks).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();
                InitMacroController();

                // Run service start steps in parallel to improve startup performance
                // Skip service starts if cancellation was requested to avoid race conditions during shutdown
                var serviceStartTasks = serviceStartSteps.Select(step => Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await step().ConfigureAwait(false);
                })).ToArray();
                await Task.WhenAll(serviceStartTasks).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

#if !DEBUG
                Autorun.Validate();
#endif

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Background initialization completed.");
            }
            catch (OperationCanceledException)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Background initialization was cancelled.");
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Background initialization failed.", ex);

                throw;
            }
        }, cancellationToken);
    }

    private async Task AwaitBackgroundInitializationAsync()
    {
        if (_backgroundInitializationTask is not { } task)
            return;

        if (!task.IsCompleted)
        {
            var completedTask = await Task.WhenAny(task, Task.Delay(BACKGROUND_INITIALIZATION_WAIT_TIMEOUT_MS));
            if (completedTask != task)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Background initialization still running, cancelling and proceeding with shutdown.");

                // Cancel the background initialization task to prevent race conditions during shutdown
                _backgroundInitializationCancellationTokenSource?.Cancel();

                try
                {
                    // Give the task a short time to respond to cancellation
                    await Task.WhenAny(task, Task.Delay(500)).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore exceptions during cancellation wait
                }

                return;
            }
        }

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Background initialization failed before shutdown completed.", ex);
        }
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        try
        {
            // Mark that we're in the exit handler to prevent double Shutdown() call
            // The flag is checked inside ShutdownAsync under lock, so we set it here under lock
            // and ShutdownAsync will see it when it checks
            lock (_shutdownLock)
            {
                _inExitHandler = true;
            }

            // ShutdownAsync will check _inExitHandler under lock, so the race condition is resolved
            ShutdownAsync(true).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Shutdown sequence encountered an error.", ex);
        }
        finally
        {
            Log.Instance.Shutdown();
            _singleInstanceMutex?.Close();
        }
    }

    public void RestartMainWindow()
    {
        if (MainWindow is MainWindow mw)
        {
            mw.SuppressClosingEventHandler = true;
            mw.Close();
        }

        var mainWindow = new MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    // Optimized shutdown helper that stops services in parallel with unified error handling
    private static async Task StopServiceAsync<T>(Func<T, Task> stopAction, string serviceName) where T : class
    {
        try
        {
            if (IoCContainer.TryResolve<T>() is { } service)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Stopping {serviceName}...");
                await stopAction(service);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping {serviceName}.", ex);
        }
    }

    // Service stop helper that verifies support before attempting to stop
    private static async Task StopServiceWithSupportCheckAsync<T>(Func<T, Task<bool>> isSupportedAction, Func<T, Task> stopAction, string serviceName) where T : class
    {
        try
        {
            if (IoCContainer.TryResolve<T>() is { } service)
            {
                if (await isSupportedAction(service))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Stopping {serviceName}...");
                    await stopAction(service);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping {serviceName}.", ex);
        }
    }

    public async Task ShutdownAsync(bool exitApplication = false)
    {
        Task shutdownTask;

        lock (_shutdownLock)
        {
            if (_shutdownTask is null)
                _shutdownTask = PerformShutdownAsync();

            if (exitApplication)
                _exitRequested = true;

            shutdownTask = _shutdownTask;
        }

        await shutdownTask;

        bool shouldInvokeShutdown;

        lock (_shutdownLock)
        {
            // Don't call Shutdown() if we're already in the Application_Exit handler
            // as that would cause a double shutdown attempt
            shouldInvokeShutdown = _exitRequested && !_shutdownInvoked && !_inExitHandler;
            if (shouldInvokeShutdown)
                _shutdownInvoked = true;
        }

        if (shouldInvokeShutdown)
        {
            if (Dispatcher.CheckAccess())
                Shutdown();
            else
                await Dispatcher.InvokeAsync(Shutdown);
        }
    }

    private async Task PerformShutdownAsync()
    {
        await AwaitBackgroundInitializationAsync().ConfigureAwait(false);

        // Stop all plugins first (they may depend on services)
        await StopPluginsAsync().ConfigureAwait(false);

        // Stop all services in parallel to speed up shutdown
        await Task.WhenAll(
            StopServiceAsync<AIController>(controller => controller.StopAsync(), "AI controller"),
            StopServiceWithSupportCheckAsync<RGBKeyboardBacklightController>(
                controller => controller.IsSupportedAsync(),
                controller => controller.SetLightControlOwnerAsync(false),
                "RGB keyboard controller"
            ),
            StopServiceWithSupportCheckAsync<SpectrumKeyboardBacklightController>(
                controller => controller.IsSupportedAsync(),
                controller => controller.StopAuroraIfNeededAsync(),
                "Spectrum keyboard controller"
            ),
            StopServiceAsync<NativeWindowsMessageListener>(listener => listener.StopAsync(), "native windows message listener"),
            StopServiceAsync<SessionLockUnlockListener>(listener => listener.StopAsync(), "session lock/unlock listener"),
            StopServiceAsync<HWiNFOIntegration>(integration => integration.StopAsync(), "HWiNFO integration"),
            StopServiceAsync<IpcServer>(server => server.StopAsync(), "IPC server"),
            StopServiceAsync<BatteryDischargeRateMonitorService>(monitor => monitor.StopAsync(), "battery discharge rate monitor service")
        ).ConfigureAwait(false);
    }

    private async Task StopPluginsAsync()
    {
        try
        {
            if (IoCContainer.TryResolve<IPluginManager>() is not { } pluginManager)
                return;

            // Get all registered plugins and call their OnShutdown method
            var registeredPlugins = pluginManager.GetRegisteredPlugins().ToList();
            if (registeredPlugins.Count == 0)
                return;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Shutting down {registeredPlugins.Count} plugin(s)...");

            var shutdownTasks = new List<Task>();

            foreach (var plugin in registeredPlugins)
            {
                shutdownTasks.Add(Task.Run(() =>
                {
                    try
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Calling OnShutdown for plugin: {plugin.Id}");
                        plugin.OnShutdown();
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Error calling OnShutdown for plugin {plugin.Id}: {ex.Message}", ex);
                    }
                }));
            }

            if (shutdownTasks.Count > 0)
            {
                await Task.WhenAll(shutdownTasks).ConfigureAwait(false);
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugin shutdown completed.");

            // Wait a bit to ensure all file handles are released
            // This is necessary because plugins might have background threads or resources that take time to clean up
            await Task.Delay(500).ConfigureAwait(false);

            // Perform pending plugin deletions after all plugins are stopped and file handles are released
            if (pluginManager is PluginManager manager)
            {
                manager.PerformPendingDeletions();
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error during plugin shutdown: {ex.Message}", ex);
        }
    }

    private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;

        Log.Instance.ErrorReport("AppDomain_UnhandledException", exception ?? new Exception($"Unknown exception caught: {e.ExceptionObject}"));
        Log.Instance.Trace($"Unhandled exception occurred.", exception);

        MessageBox.Show(string.Format(Resource.UnexpectedException, exception?.ToStringDemystified() ?? "Unknown exception."),
            "Application Domain Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Shutdown(100);
    }

    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Instance.ErrorReport("Application_DispatcherUnhandledException", e.Exception);
        Log.Instance.Trace($"Unhandled exception occurred.", e.Exception);

        MessageBox.Show(string.Format(Resource.UnexpectedException, e.Exception.ToStringDemystified()),
            "Application Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Shutdown(101);
    }


    private void EnsureSingleInstance()
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Checking for other instances...");

        _singleInstanceMutex = new Mutex(true, MUTEX_NAME, out var isOwned);
        _singleInstanceWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, EVENT_NAME);

        if (!isOwned)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Another instance running, closing...");

            _singleInstanceWaitHandle.Set();
            Shutdown();
            return;
        }

        new Thread(() =>
        {
            while (_singleInstanceWaitHandle.WaitOne())
            {
                Current.Dispatcher.BeginInvoke(async () =>
                {
                    if (Current.MainWindow is { } window)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Another instance started, bringing this one to front instead...");

                        window.BringToForeground();
                    }
                    else
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"!!! PANIC !!! This instance is missing main window. Shutting down.");

                        await ShutdownAsync(true);
                    }
                });
            }
        })
        {
            IsBackground = true
        }.Start();
    }

    private static async Task LogSoftwareStatusAsync()
    {
        if (!Log.Instance.IsTraceEnabled)
            return;

        // Gather software statuses in parallel to improve efficiency
        var statuses = await Task.WhenAll(
            IoCContainer.Resolve<VantageDisabler>().GetStatusAsync(),
            IoCContainer.Resolve<LegionZoneDisabler>().GetStatusAsync(),
            IoCContainer.Resolve<FnKeysDisabler>().GetStatusAsync()
        );

        Log.Instance.Trace($"Vantage status: {statuses[0]}");
        Log.Instance.Trace($"LegionZone status: {statuses[1]}");
        Log.Instance.Trace($"FnKeys status: {statuses[2]}");
    }

    // Generic async helper with error handling to reduce repetition
    private static async Task RunWithErrorHandlingAsync(Func<Task> action, string operationName, bool logOnSuccess = true)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled && logOnSuccess)
                Log.Instance.Trace($"Initializing {operationName}...");

            await action();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't initialize {operationName}.", ex);
        }
    }

    private static async Task InitHybridModeAsync()
    {
        await RunWithErrorHandlingAsync(
            async () =>
            {
                var feature = IoCContainer.Resolve<HybridModeFeature>();
                await feature.EnsureDGPUEjectedIfNeededAsync();
            },
            "hybrid mode"
        );
    }

    private static async Task InitAutomationProcessorAsync()
    {
        await RunWithErrorHandlingAsync(
            async () =>
            {
                var automationProcessor = IoCContainer.Resolve<AutomationProcessor>();
                await automationProcessor.InitializeAsync();
                automationProcessor.RunOnStartup();
            },
            "automation processor"
        );
    }

    private static async Task InitPowerModeFeatureAsync()
    {
        await RunWithErrorHandlingAsync(
            async () =>
            {
                var feature = IoCContainer.Resolve<PowerModeFeature>();
                if (await feature.IsSupportedAsync())
                {
                    // Optimization: cache the support status to avoid multiple IsSupportedAsync calls
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Ensuring god mode state is applied...");

                    await feature.EnsureGodModeStateIsAppliedAsync();

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Ensuring correct power plan is set...");

                    await feature.EnsureCorrectWindowsPowerSettingsAreSetAsync();
                }
            },
            "power mode feature",
            false // Skip success logging because detailed logs exist inside the helper methods
        );
    }

    private static async Task InitBatteryFeatureAsync()
    {
        await RunWithErrorHandlingAsync(
            async () =>
            {
                var feature = IoCContainer.Resolve<BatteryFeature>();
                if (await feature.IsSupportedAsync())
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Ensuring correct battery mode is set...");

                    await feature.EnsureCorrectBatteryModeIsSetAsync();
                }
            },
            "battery feature",
            false
        );
    }

    private static async Task InitRgbKeyboardControllerAsync()
    {
        await RunWithErrorHandlingAsync(
            async () =>
            {
                var controller = IoCContainer.Resolve<RGBKeyboardBacklightController>();
                if (await controller.IsSupportedAsync())
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Setting light control owner and restoring preset...");

                    await controller.SetLightControlOwnerAsync(true, true);
                }
                else
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"RGB keyboard is not supported.");
                }
            },
            "RGB keyboard controller",
            false
        );
    }

    // Optimized initialization routine for the Spectrum keyboard controller
    private static async Task InitSpectrumKeyboardControllerAsync()
    {
        await RunWithErrorHandlingAsync(
            async () =>
            {
                var controller = IoCContainer.Resolve<SpectrumKeyboardBacklightController>();
                if (await controller.IsSupportedAsync())
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Starting Aurora if needed...");

                    var result = await controller.StartAuroraIfNeededAsync();
                    if (result)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Aurora started.");
                    }
                    else
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Aurora not needed.");
                    }
                }
                else
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Spectrum keyboard is not supported.");
                }
            },
            "Spectrum keyboard controller",
            false
        );
    }

    private static async Task InitGpuOverclockControllerAsync()
    {
        await RunWithErrorHandlingAsync(
            async () =>
            {
                var controller = IoCContainer.Resolve<GPUOverclockController>();
                if (await controller.IsSupportedAsync())
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Ensuring GPU overclock is applied...");

                    var result = await controller.EnsureOverclockIsAppliedAsync();
                    if (result)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"GPU overclock applied.");
                    }
                    else
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"GPU overclock not needed.");
                    }
                }
                else
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"GPU overclock is not supported.");
                }
            },
            "GPU overclock controller",
            false
        );
    }

    private static void InitMacroController()
    {
        var controller = IoCContainer.Resolve<MacroController>();
        controller.Start();
    }
}
