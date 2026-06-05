using LenovoLegionToolkit.Lib.System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Controllers.Sensors;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Features.Hybrid;
using LenovoLegionToolkit.Lib.Features.Hybrid.Notify;
using LenovoLegionToolkit.Lib.Features.PanelLogo;
using LenovoLegionToolkit.Lib.Features.WhiteKeyboardBacklight;
using LenovoLegionToolkit.Lib.Integrations;
using LenovoLegionToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Lib.Macro;
using LenovoLegionToolkit.Lib.Overclocking.Amd;
using LenovoLegionToolkit.Lib.Services;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.ResourcesCatalog;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Lib.Messaging;
using LenovoLegionToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.WPF.CLI;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Pages;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows;
using UniversalDeviceToolkit.WPF.Windows.Osd;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using WinFormsApp = System.Windows.Forms.Application;
using WinFormsHighDpiMode = System.Windows.Forms.HighDpiMode;

namespace UniversalDeviceToolkit.WPF
{
public partial class App
    {
        [LibraryImport("kernel32.dll")]
        private static partial void ExitProcess(uint uExitCode);

        private const string MUTEX_NAME = AppIdentity.CompactName + "_Mutex_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const string EVENT_NAME = AppIdentity.CompactName + "_Event_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const string ACK_EVENT_NAME = AppIdentity.CompactName + "_AckEvent_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const string LEGACY_MUTEX_NAME = AppIdentity.LegacyCompactName + "_Mutex_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const string LEGACY_EVENT_NAME = AppIdentity.LegacyCompactName + "_Event_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const string LEGACY_ACK_EVENT_NAME = AppIdentity.LegacyCompactName + "_AckEvent_6efcc882-924c-4cbc-8fec-f45c25696f98";
    private const int BACKGROUND_INITIALIZATION_WAIT_TIMEOUT_MS = 3000;
    private const int SINGLE_INSTANCE_ACTIVATION_TIMEOUT_MS = 1200;
    private const string RECOVERY_SINGLE_INSTANCE_SUFFIX = "_Recovery";

    private Mutex? _singleInstanceMutex;
    private Mutex? _legacySingleInstanceMutex;
    private EventWaitHandle? _singleInstanceWaitHandle;
    private EventWaitHandle? _legacySingleInstanceWaitHandle;
    private EventWaitHandle? _singleInstanceAckWaitHandle;
    private EventWaitHandle? _legacySingleInstanceAckWaitHandle;
    private bool _singleInstanceMutexOwned;
    private bool _legacySingleInstanceMutexOwned;
    private Thread? _singleInstanceThread;
    private Task? _backgroundInitializationTask;
    private CancellationTokenSource? _backgroundInitializationCancellationTokenSource;
    private readonly object _shutdownLock = new();
    private Task? _shutdownTask;
    private bool _exitRequested;
    private bool _shutdownInvoked;
    private bool _inExitHandler;
    private bool _exceptionHandlerExecuting;

    private static string T(string key, string fallback) => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    public new static App Current => (App)Application.Current;

    public Window? OsdWindow;

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

        ApplyStartupOverrides(flags);

        // Ensure native shell logger writes to the same log file
        Environment.SetEnvironmentVariable("LLT_LOG_PATH", Log.Instance.LogPath);

        AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Flags: {flags}");

        if (!EnsureSingleInstance())
        {
            ExitDuplicateInstance();
            return;
        }

        await LocalizationHelper.SetLanguageAsync(true, CreateStartupLanguagePackManager(flags));

        // Note: ApplicationSettings is created here before IoC initialization for compatibility check.
        // This is safe because ApplicationSettings uses a shared storage mechanism, so changes will
        // be reflected in the IoC-resolved instance later. However, we should use the IoC instance
        // after initialization for consistency.
        var applicationSettings = new ApplicationSettings();

        try
        {
                var machineInformation = await Compatibility.GetMachineInformationAsync();
                await RunStartupDeviceSetupIfNeededAsync(machineInformation, flags);

                // Check compatibility - IsCompatibleAsync already includes basic compatibility check
                var (isCompatible, mi) = await MachineCompatibility.IsCompatibleAsync();

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

                        // Perform safe shutdown for incompatible systems to prevent process residue
                        await PerformSafeShutdownForIncompatibleSystemAsync(202).ConfigureAwait(false);
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
                
                // Perform safe shutdown for compatibility check errors to prevent process residue
                await PerformSafeShutdownForIncompatibleSystemAsync(200).ConfigureAwait(false);
                return;
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Starting... [version={Assembly.GetEntryAssembly()?.GetName().Version}, build={Assembly.GetEntryAssembly()?.GetBuildDateTimeString()}, os={Environment.OSVersion}, dotnet={Environment.Version}]");

        WinFormsApp.SetHighDpiMode(WinFormsHighDpiMode.PerMonitorV2);
        RenderOptions.ProcessRenderMode = RenderingCompatibilityHelper.GetPreferredRenderMode(applicationSettings);

        IoCContainer.Initialize(
            new LenovoLegionToolkit.Lib.IoCModule(),
            new LenovoLegionToolkit.Lib.Plugins.IoCModule(),
            new UniversalDeviceToolkit.Lib.Automation.IoCModule(),
            new UniversalDeviceToolkit.Lib.Macro.IoCModule(),
            new IoCModule()
        );

        PluginHostContext.SetCurrent(new MainAppPluginHostContext(() => Application.Current?.MainWindow));

        IoCContainer.Resolve<HttpClientFactory>().SetProxy(flags.ProxyUrl, flags.ProxyUsername, flags.ProxyPassword, flags.ProxyAllowAllCerts);
        IoCContainer.Resolve<LanguagePackManager>().ProcessPendingUninstall();

        IoCContainer.Resolve<PowerModeFeature>().AllowAllPowerModesOnBattery = flags.AllowAllPowerModesOnBattery;
        IoCContainer.Resolve<RGBKeyboardBacklightController>().ForceDisable = flags.ForceDisableRgbKeyboardSupport;
        IoCContainer.Resolve<SpectrumKeyboardBacklightController>().ForceDisable = flags.ForceDisableSpectrumKeyboardSupport;
        IoCContainer.Resolve<WhiteKeyboardLenovoLightingBacklightFeature>().ForceDisable = flags.ForceDisableLenovoLighting;
        IoCContainer.Resolve<PanelLogoLenovoLightingBacklightFeature>().ForceDisable = flags.ForceDisableLenovoLighting;
        IoCContainer.Resolve<PortsBacklightFeature>().ForceDisable = flags.ForceDisableLenovoLighting;
        IoCContainer.Resolve<IGPUModeFeature>().ExperimentalGPUWorkingMode = flags.ExperimentalGPUWorkingMode;
        IoCContainer.Resolve<DGPUNotify>().ExperimentalGPUWorkingMode = flags.ExperimentalGPUWorkingMode;
        var updateChecker = IoCContainer.Resolve<UpdateChecker>();
        updateChecker.Disable = flags.DisableUpdateChecker;
        updateChecker.DisableReason = flags.DisableUpdateChecker ? Flags.DisableUpdateCheckerSwitch : null;

        // Initialize plugins
        await InitializePluginsAsync();
        
        // Apply plugin-specific language settings after plugins are loaded
        LocalizationHelper.SetPluginResourceCultures();

        StartBackgroundInitialization();

        var mainWindow = new MainWindow(IoCContainer.Resolve<ApplicationSettings>(),
            IoCContainer.Resolve<IPluginManager>(),
            IoCContainer.Resolve<SpecialKeyListener>(),
            IoCContainer.Resolve<VantageDisabler>(),
            IoCContainer.Resolve<LegionZoneDisabler>(),
            IoCContainer.Resolve<FnKeysDisabler>(),
            IoCContainer.Resolve<UpdateChecker>())
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            TrayTooltipEnabled = !flags.DisableTrayTooltip
        };
        MainWindow = mainWindow;
        PluginHostContext.SetCurrent(new MainAppPluginHostContext(() => MainWindow as Window));

        var persistedSettings = IoCContainer.Resolve<ApplicationSettings>();
        IoCContainer.Resolve<ThemeManager>().Apply();
        AnimationHelper.UpdateAnimationParameters(persistedSettings);

        // Check for unsent crash reports from previous session
        // This shows a modal dialog before the main window appears
        CheckPendingCrashReports();

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

        InitOsd();
    }

    private static async Task InitializePluginsAsync()
    {
        try
        {
            var pluginManager = IoCContainer.Resolve<IPluginManager>();
            
            // System Optimization and Tools are now default interfaces, not plugins
            // They are registered directly in MainWindow.xaml as NavigationItems
            // No need to register them as plugins

            // Scan and load plugins from the plugins directory
            // This will automatically discover and register external plugins
            await pluginManager.ScanAndLoadPluginsAsync();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugins initialized successfully.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to initialize plugins.", ex);
        }
    }

    private static async Task RunStartupDeviceSetupIfNeededAsync(MachineInformation machineInformation, Flags flags)
    {
        try
        {
            await StartupDeviceSetupCoordinator.CreateDefault(CreateStartupHttpClientFactory(flags)).RunIfNeededAsync(machineInformation);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Startup device setup failed; continuing with current compatibility state.", ex);
        }
    }

    private static LanguagePackManager CreateStartupLanguagePackManager(Flags flags) =>
        new(new OnlineResourceCatalogClient(CreateStartupHttpClientFactory(flags)));

    private static HttpClientFactory CreateStartupHttpClientFactory(Flags flags)
    {
        var httpClientFactory = new HttpClientFactory();
        httpClientFactory.SetProxy(flags.ProxyUrl, flags.ProxyUsername, flags.ProxyPassword, flags.ProxyAllowAllCerts);
        return httpClientFactory;
    }

    private static void CheckPendingCrashReports()
    {
        try
        {
            // Clean up old crash reports first (older than 30 days)
            CrashReportHelper.CleanupOldCrashReports(30);

            var reports = CrashReportHelper.GetUnsentCrashReports().ToList();
            if (reports.Count <= 0)
                return;

            // Log that we found pending crash reports
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Found {reports.Count} pending crash report(s).");

            // Show crash report notification for the most recent report
            var mostRecentReport = reports.OrderByDescending(r =>
            {
                var info = new FileInfo(r);
                return info.CreationTimeUtc;
            }).FirstOrDefault();

            if (mostRecentReport != null)
            {
                try
                {
                    var notificationWindow = new CrashReportNotificationWindow(mostRecentReport);
                    notificationWindow.ShowDialog();

                    // Delete other reports (keep only the most recent one shown)
                    foreach (var otherReport in reports.Where(r => r != mostRecentReport))
                    {
                        CrashReportHelper.DeleteCrashReport(otherReport);
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to show crash report notification: {ex.Message}", ex);

                    // Delete all reports if we can't show the notification
                    foreach (var report in reports)
                    {
                        CrashReportHelper.DeleteCrashReport(report);
                    }
                }
            }
        }
        catch { /* Ignore crash report checking errors */ }
    }

    private void StartBackgroundInitialization()
    {
        _backgroundInitializationCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _backgroundInitializationCancellationTokenSource.Token;

        var initializationSteps = new Func<Task>[]
        {
            LogSoftwareStatusAsync,
            InitLampArrayControllerAsync,
            InitSensorsGroupControllerFeatureAsync,
            InitPowerModeFeatureAsync,
            InitItsModeFeatureAsync,
            InitBatteryFeatureAsync,
            InitRgbKeyboardControllerAsync,
            InitSpectrumKeyboardControllerAsync,
            InitGpuOverclockControllerAsync,
            InitHybridModeAsync,
            InitFanManagerAsync,
            InitAmdOverclockingAsync,
            InitAutomationProcessorAsync
        };

        var serviceStartSteps = new Func<Task>[]
        {
            () => IoCContainer.Resolve<AIController>().StartIfNeededAsync(),
            () => IoCContainer.Resolve<HWiNFOIntegration>().StartStopIfNeededAsync(),
            () => IoCContainer.Resolve<IpcServer>().StartStopIfNeededAsync(),
            () => IoCContainer.Resolve<BatteryDischargeRateMonitorService>().StartStopIfNeededAsync(),
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
            }
        }, cancellationToken);

        _backgroundInitializationTask = _backgroundInitializationTask.ContinueWith(t =>
        {
            if (t.IsFaulted && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Background initialization task completed faulted and was observed.", t.Exception);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
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
                _backgroundInitializationCancellationTokenSource?.Cancel();
                try { await Task.WhenAny(task, Task.Delay(500)); }
                catch { /* Background task cancellation failed - app startup continues */ }
                return;
            }
        }

        try { await task; }
        catch { /* Background initialization failed - app continues startup */ }
    }

    /// <summary>
    /// Performs safe shutdown for incompatible systems to prevent process residue
    /// This method ensures all resources are properly cleaned up before exit
    /// </summary>
    private async Task PerformSafeShutdownForIncompatibleSystemAsync(int? exitCode = null)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Starting safe shutdown for incompatible system...");

            // Cancel any background initialization that might be running
            _backgroundInitializationCancellationTokenSource?.Cancel();

            // Wait for background tasks to complete with timeout
            if (_backgroundInitializationTask != null)
            {
                try
                {
                    var completedTask = await Task.WhenAny(_backgroundInitializationTask, Task.Delay(1000));
                    if (completedTask != _backgroundInitializationTask)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Background initialization did not complete in time, continuing with shutdown...");
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error waiting for background initialization during safe shutdown: {ex.Message}");
                }
            }

            StopSingleInstanceThreadSafely();
            CleanupSingleInstanceResources();

            // CRITICAL: Stop MacroController to release keyboard hook
            // If the hook is not released, the process cannot exit cleanly
            StopMacroControllerSafely();

            // Cancel and dispose the cancellation token source
            try
            {
                _backgroundInitializationCancellationTokenSource?.Cancel();
                _backgroundInitializationCancellationTokenSource?.Dispose();
                _backgroundInitializationCancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error disposing cancellation token source during safe shutdown: {ex.Message}");
            }

            // Flush and shutdown the log system
            try
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Flushing and shutting down log system...");

                Log.Instance.Flush();
                Log.Instance.Shutdown();
            }
            catch (Exception ex)
            {
                // Log shutdown failure to console as fallback
                Console.WriteLine($"Error during log shutdown: {ex.Message}");
            }

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Safe shutdown for incompatible system completed.");

            // If an exit code is provided, force exit now to prevent process residue
            if (exitCode.HasValue)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Forcing exit via ExitProcess({exitCode.Value}) from safe shutdown...");

                ExitProcess((uint)exitCode.Value);
                Environment.Exit(exitCode.Value);
            }
        }
        catch (Exception ex)
        {
            // As a last resort, log to console
            Console.WriteLine($"Critical error during safe shutdown: {ex.Message}");

            // If we have an exit code, try to exit even if cleanup failed
            if (exitCode.HasValue)
            {
                ExitProcess((uint)exitCode.Value);
                Environment.Exit(exitCode.Value);
            }
        }
    }

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        lock (_shutdownLock)
            _inExitHandler = true;

        PluginHostContext.Reset();

        try { ShutdownAsync(true).GetAwaiter().GetResult(); }
        catch { /* Shutdown failed - continue with exit anyway */ }

        try { Log.Instance.Shutdown(); }
        catch { /* Log shutdown failed - continue with exit */ }

        try { _singleInstanceMutex?.Close(); }
        catch { /* Mutex cleanup failed - continue with exit */ }
        try { _legacySingleInstanceMutex?.Close(); }
        catch { /* Legacy mutex cleanup failed - continue with exit */ }

        StopMacroControllerSafely();
        StopSingleInstanceThreadSafely();

        ForceExit((uint)e.ApplicationExitCode);
    }

    public void RestartMainWindow()
    {
        if (MainWindow is MainWindow mw)
        {
            mw.SuppressClosingEventHandler = true;
            mw.Close();
        }

        var mainWindow = new MainWindow(IoCContainer.Resolve<ApplicationSettings>(),
            IoCContainer.Resolve<IPluginManager>(),
            IoCContainer.Resolve<SpecialKeyListener>(),
            IoCContainer.Resolve<VantageDisabler>(),
            IoCContainer.Resolve<LegionZoneDisabler>(),
            IoCContainer.Resolve<FnKeysDisabler>(),
            IoCContainer.Resolve<UpdateChecker>())
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        MainWindow = mainWindow;
        PluginHostContext.SetCurrent(new MainAppPluginHostContext(() => MainWindow as Window));
        mainWindow.Show();
    }

    /// <summary>
    /// Stops MacroController safely with error handling
    /// CRITICAL: The keyboard hook MUST be released or the process cannot exit
    /// </summary>
    private static void StopMacroControllerSafely()
    {
        try
        {
            if (IoCContainer.TryResolve<MacroController>() is { } macroController)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Stopping MacroController...");
                macroController.Stop();
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"MacroController stopped.");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error stopping MacroController: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Stops the single instance thread safely with timeout
    /// </summary>
    private void StopSingleInstanceThreadSafely()
    {
        if (_singleInstanceThread != null && _singleInstanceThread.IsAlive)
        {
            try
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Stopping single instance thread...");

                _singleInstanceWaitHandle?.Dispose();
                _singleInstanceWaitHandle = null;
                _legacySingleInstanceWaitHandle?.Dispose();
                _legacySingleInstanceWaitHandle = null;
                _singleInstanceAckWaitHandle?.Dispose();
                _singleInstanceAckWaitHandle = null;
                _legacySingleInstanceAckWaitHandle?.Dispose();
                _legacySingleInstanceAckWaitHandle = null;

                if (!_singleInstanceThread.Join(500))
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Single instance thread did not finish in time.");
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error stopping single instance thread: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Cleanup single instance resources (mutex and wait handle)
    /// </summary>
    private void CleanupSingleInstanceResources()
    {
        try
        {
            if (_singleInstanceMutexOwned && _singleInstanceMutex != null)
            {
                void ReleaseMutex()
                {
                    if (_singleInstanceMutexOwned && _singleInstanceMutex != null)
                    {
                        _singleInstanceMutex.ReleaseMutex();
                        _singleInstanceMutexOwned = false;
                    }
                }

                if (Dispatcher.CheckAccess())
                {
                    ReleaseMutex();
                }
                else if (!Dispatcher.HasShutdownStarted && !Dispatcher.HasShutdownFinished)
                {
                    Dispatcher.Invoke(ReleaseMutex);
                }
                else
                {
                    _singleInstanceMutexOwned = false;
                }
            }

            _singleInstanceMutex?.Close();
            _singleInstanceMutex = null;
            if (_legacySingleInstanceMutexOwned && _legacySingleInstanceMutex != null)
            {
                _legacySingleInstanceMutex.ReleaseMutex();
                _legacySingleInstanceMutexOwned = false;
            }

            _legacySingleInstanceMutex?.Close();
            _legacySingleInstanceMutex = null;
        }
        catch (ApplicationException ex) when (ex.Message.Contains("Object synchronization method", StringComparison.OrdinalIgnoreCase))
        {
            _singleInstanceMutexOwned = false;
            _singleInstanceMutex?.Close();
            _singleInstanceMutex = null;
            _legacySingleInstanceMutexOwned = false;
            _legacySingleInstanceMutex?.Close();
            _legacySingleInstanceMutex = null;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Single instance mutex was not owned by the current thread; closed without explicit release.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error disposing single instance mutex: {ex.Message}", ex);
        }

        try
        {
            _singleInstanceWaitHandle?.Dispose();
            _singleInstanceWaitHandle = null;
            _legacySingleInstanceWaitHandle?.Dispose();
            _legacySingleInstanceWaitHandle = null;
            _singleInstanceAckWaitHandle?.Dispose();
            _singleInstanceAckWaitHandle = null;
            _legacySingleInstanceAckWaitHandle?.Dispose();
            _legacySingleInstanceAckWaitHandle = null;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error disposing wait handle: {ex.Message}", ex);
        }
    }

    private void ForceExit(uint exitCode)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            Thread.Sleep(100);
            try { Environment.Exit((int)exitCode); }
            catch { /* Environment.Exit failed - use fallback exit method */ }
            ExitProcess(exitCode);
        });
    }

    private static async Task StopServiceAsync<T>(Func<T, Task> stopAction, string serviceName) where T : class
    {
        try
        {
            if (IoCContainer.TryResolve<T>() is not { } service)
                return;

            await stopAction(service);
        }
        catch { /* Service stop failed during shutdown - continue cleanup */ }
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
            StopMacroControllerSafely();

            if (Dispatcher.CheckAccess())
                Shutdown();
            else
                await Dispatcher.InvokeAsync(Shutdown);
        }
    }

    private async Task PerformShutdownAsync()
    {
        var totalStopwatch = Stopwatch.StartNew();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Shutdown started.");

        try
        {
            _backgroundInitializationCancellationTokenSource?.Cancel();
            StopSingleInstanceThreadSafely();
            CleanupSingleInstanceResources();

            await AwaitBackgroundInitializationAsync().ConfigureAwait(false);

            await StopPluginsAsync().ConfigureAwait(false);

            var stopServicesTask = Task.WhenAll(
                StopServiceAsync<AIController>(controller => controller.StopAsync(), "AI controller"),
                StopServiceAsync<RGBKeyboardBacklightController>(controller => controller.SetLightControlOwnerAsync(false), "RGB keyboard controller"),
                StopServiceAsync<SessionLockUnlockListener>(listener => listener.StopAsync(), "session lock/unlock listener"),
                StopServiceAsync<HWiNFOIntegration>(integration => integration.StopAsync(), "HWiNFO integration"),
                StopServiceAsync<IpcServer>(server => server.StopAsync(), "IPC server"),
                StopServiceAsync<BatteryDischargeRateMonitorService>(monitor => monitor.StopAsync(), "battery monitor"),
                StopServiceAsync<LampArrayController>(controller => controller.StopAsync(), "lamp array controller")
            );

            stopServicesTask.Wait(TimeSpan.FromSeconds(2));

            await FinalizeRuntimeProfilesAsync().ConfigureAwait(false);

            StopMacroControllerSafely();
            StopSingleInstanceThreadSafely();
            CleanupSingleInstanceResources();

            totalStopwatch.Stop();
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Shutdown completed in {totalStopwatch.ElapsedMilliseconds}ms.");
        }
        catch (Exception ex) when (Log.Instance.IsTraceEnabled)
        {
            Log.Instance.Trace($"Shutdown error: {ex.Message}");
        }
    }

    private async Task StopPluginsAsync()
    {
        try
        {
            if (IoCContainer.TryResolve<IPluginManager>() is not { } pluginManager)
                return;

            var registeredPlugins = pluginManager.GetRegisteredPlugins().ToList();
            if (registeredPlugins.Count == 0)
                return;

            var shutdownTasks = registeredPlugins.Select(plugin => Task.Run(() =>
            {
                try { plugin.OnShutdown(); }
                catch { /* Plugin shutdown failed - continue with other plugins */ }
            })).ToList();

            await Task.WhenAll(shutdownTasks).ConfigureAwait(false);

            await Task.Delay(200).ConfigureAwait(false);

            if (pluginManager is PluginManager manager)
                manager.PerformPendingDeletions();
        }
        catch { /* Plugin shutdown process failed - continue with app shutdown */ }
    }

    private void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Prevent infinite recursion - use FailFast on re-entry since state is corrupted
        if (_exceptionHandlerExecuting)
        {
            Environment.FailFast("Fatal error: re-entered AppDomain_UnhandledException", new Exception("Re-entry detected"));
            return;
        }

        _exceptionHandlerExecuting = true;

        try
        {
            var exception = e.ExceptionObject as Exception;
            var osVersion = Environment.OSVersion;
            var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
            var managedMemory = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;

            Log.Instance.ErrorReport($"AppDomain_UnhandledException [OS={osVersion}, Assembly={assemblyVersion}, ManagedMemory={managedMemory:N0}, WorkingSet={workingSet:N0}]", exception ?? new Exception($"Unknown exception caught: {e.ExceptionObject}"));
            Log.Instance.Trace($"Unhandled exception occurred.", exception);

            // Save crash report BEFORE showing message box
            CrashReportHelper.SaveCrashReport(exception, "AppDomain");

            // Try to show message box, but don't let it cause infinite recursion
            try
            {
                MessageBox.Show(string.Format(Resource.UnexpectedException, exception?.ToStringDemystified() ?? T("App_UnhandledException_Unknown", "Unknown exception.")),
                    T("App_UnhandledException_AppDomain_Title", "Application Domain Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // If MessageBox fails, just log and exit
                Log.Instance.Trace($"Failed to show error dialog, forcing exit.");
            }
        }
        catch
        {
            // If even logging fails, just exit
        }
        finally
        {
            // CRITICAL: Stop MacroController to release keyboard hook before exit
            StopMacroControllerSafely();

            Log.Instance.Flush();

            // Force exit to prevent hanging
            try
            {
                Shutdown(100);
            }
            catch
            {
                try
                {
                    Environment.Exit(100);
                }
                catch
                {
                    Environment.FailFast("Fatal unhandled exception in AppDomain", e.ExceptionObject as Exception);
                }
            }
        }
    }

    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Prevent infinite recursion - use FailFast on re-entry since state is corrupted
        if (_exceptionHandlerExecuting)
        {
            e.Handled = true;
            Environment.FailFast("Fatal error: re-entered Application_DispatcherUnhandledException", new Exception("Re-entry detected"));
            return;
        }

        _exceptionHandlerExecuting = true;
        e.Handled = true; // Mark as handled to prevent further propagation

        try
        {
            var osVersion = Environment.OSVersion;
            var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
            var managedMemory = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;

            Log.Instance.ErrorReport($"Application_DispatcherUnhandledException [OS={osVersion}, Assembly={assemblyVersion}, ManagedMemory={managedMemory:N0}, WorkingSet={workingSet:N0}]", e.Exception);
            Log.Instance.Trace($"Unhandled exception occurred.", e.Exception);

            // Save crash report BEFORE showing message box
            CrashReportHelper.SaveCrashReport(e.Exception, "Dispatcher");

            // Try to show message box, but don't let it cause infinite recursion
            try
            {
                MessageBox.Show(string.Format(Resource.UnexpectedException, e.Exception.ToStringDemystified()),
                    T("App_UnhandledException_Dispatcher_Title", "Application Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // If MessageBox fails, just log and exit
                Log.Instance.Trace($"Failed to show error dialog, forcing exit.");
            }
        }
        catch
        {
            // If even logging fails, just exit
        }
        finally
        {
            // CRITICAL: Stop MacroController to release keyboard hook before exit
            StopMacroControllerSafely();

            Log.Instance.Flush();

            // Force exit to prevent hanging
            try
            {
                Shutdown(101);
            }
            catch
            {
                try
                {
                    Environment.Exit(101);
                }
                catch
                {
                    Environment.FailFast("Fatal unhandled exception in Dispatcher", e.Exception);
                }
            }
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            var osVersion = Environment.OSVersion;
            var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "Unknown";
            var managedMemory = GC.GetTotalMemory(false);
            var workingSet = Environment.WorkingSet;

            // Log the unobserved task exception
            Log.Instance.ErrorReport($"TaskScheduler_UnobservedTaskException [OS={osVersion}, Assembly={assemblyVersion}, ManagedMemory={managedMemory:N0}, WorkingSet={workingSet:N0}]", e.Exception);
            Log.Instance.Trace($"Unobserved task exception occurred.", e.Exception);

            // Save crash report
            CrashReportHelper.SaveCrashReport(e.Exception, "TaskScheduler");

            // Mark as observed to prevent the process from terminating
            // Note: In .NET 5+, unobserved task exceptions don't terminate the process by default,
            // but we mark as observed for safety
            e.SetObserved();
        }
        catch
        {
            // If even this fails, mark as observed to prevent termination
            e.SetObserved();
        }
    }


    private bool EnsureSingleInstance()
    {
        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Checking for other instances...");

        var mutexName = ResolveSingleInstanceObjectName(MUTEX_NAME);
        var eventName = ResolveSingleInstanceObjectName(EVENT_NAME);
        var ackEventName = ResolveSingleInstanceObjectName(ACK_EVENT_NAME);
        var legacyMutexName = ResolveSingleInstanceObjectName(LEGACY_MUTEX_NAME);
        var legacyEventName = ResolveSingleInstanceObjectName(LEGACY_EVENT_NAME);
        var legacyAckEventName = ResolveSingleInstanceObjectName(LEGACY_ACK_EVENT_NAME);
        _singleInstanceMutex = new Mutex(true, mutexName, out var isOwned);
        _singleInstanceMutexOwned = isOwned;
        _singleInstanceWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, eventName);
        _singleInstanceAckWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, ackEventName);
        _legacySingleInstanceMutex = new Mutex(true, legacyMutexName, out var legacyIsOwned);
        _legacySingleInstanceMutexOwned = legacyIsOwned;
        _legacySingleInstanceWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, legacyEventName);
        _legacySingleInstanceAckWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, legacyAckEventName);

        if (!isOwned || !legacyIsOwned)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Another instance running, signaling existing instance...");

            if (SignalAndWaitForSingleInstanceActivation())
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Another instance acknowledged activation, closing...");

                Shutdown();
                return false;
            }

            if (TrySwitchToRecoverySingleInstance(mutexName, eventName, ackEventName, legacyMutexName, legacyEventName, legacyAckEventName))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Existing instance did not acknowledge activation; continuing with recovery single-instance guard.");
            }
            else
            {
                Shutdown();
                return false;
            }
        }

        _singleInstanceThread = new Thread(() =>
        {
            try
            {
                while (WaitForSingleInstanceSignal())
                    BringMainWindowToForegroundFromSingleInstanceThread();
            }
            catch (ObjectDisposedException)
            {
                // Expected when wait handle is disposed during shutdown
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error in single instance thread.", ex);
            }
        })
        {
            IsBackground = true,
            Name = "SingleInstanceThread"
        };
        _singleInstanceThread.Start();
        return true;
    }

    private static void ExitDuplicateInstance()
    {
        try { Log.Instance.Shutdown(); }
        catch { /* Logging shutdown failed; duplicate instance must still exit. */ }

        try { Environment.Exit(0); }
        catch { /* Fall back to native process exit. */ }

        ExitProcess(0);
    }

    private bool WaitForSingleInstanceSignal()
    {
        var handles = new[] { _singleInstanceWaitHandle, _legacySingleInstanceWaitHandle }
            .Where(handle => handle is not null)
            .Cast<WaitHandle>()
            .ToArray();

        if (handles.Length == 0)
            return false;

        return WaitHandle.WaitAny(handles) != WaitHandle.WaitTimeout;
    }

    private bool SignalAndWaitForSingleInstanceActivation()
    {
        _singleInstanceAckWaitHandle?.Reset();
        _legacySingleInstanceAckWaitHandle?.Reset();

        _singleInstanceWaitHandle?.Set();
        _legacySingleInstanceWaitHandle?.Set();

        var handles = new[] { _singleInstanceAckWaitHandle, _legacySingleInstanceAckWaitHandle }
            .Where(handle => handle is not null)
            .Cast<WaitHandle>()
            .ToArray();

        return handles.Length > 0
               && WaitHandle.WaitAny(handles, SINGLE_INSTANCE_ACTIVATION_TIMEOUT_MS) != WaitHandle.WaitTimeout;
    }

    private void SignalSingleInstanceActivationAck()
    {
        try { _singleInstanceAckWaitHandle?.Set(); }
        catch { /* Activation acknowledgement is best effort. */ }

        try { _legacySingleInstanceAckWaitHandle?.Set(); }
        catch { /* Activation acknowledgement is best effort. */ }
    }

    private bool TrySwitchToRecoverySingleInstance(
        string mutexName,
        string eventName,
        string ackEventName,
        string legacyMutexName,
        string legacyEventName,
        string legacyAckEventName)
    {
        CleanupSingleInstanceResources();

        var recoveryMutexName = mutexName + RECOVERY_SINGLE_INSTANCE_SUFFIX;
        var recoveryEventName = eventName + RECOVERY_SINGLE_INSTANCE_SUFFIX;
        var recoveryAckEventName = ackEventName + RECOVERY_SINGLE_INSTANCE_SUFFIX;
        var recoveryLegacyMutexName = legacyMutexName + RECOVERY_SINGLE_INSTANCE_SUFFIX;
        var recoveryLegacyEventName = legacyEventName + RECOVERY_SINGLE_INSTANCE_SUFFIX;
        var recoveryLegacyAckEventName = legacyAckEventName + RECOVERY_SINGLE_INSTANCE_SUFFIX;

        _singleInstanceMutex = new Mutex(true, recoveryMutexName, out var recoveryIsOwned);
        _singleInstanceMutexOwned = recoveryIsOwned;
        _singleInstanceWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, recoveryEventName);
        _singleInstanceAckWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, recoveryAckEventName);
        _legacySingleInstanceMutex = new Mutex(true, recoveryLegacyMutexName, out var recoveryLegacyIsOwned);
        _legacySingleInstanceMutexOwned = recoveryLegacyIsOwned;
        _legacySingleInstanceWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, recoveryLegacyEventName);
        _legacySingleInstanceAckWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, recoveryLegacyAckEventName);

        if (recoveryIsOwned && recoveryLegacyIsOwned)
            return true;

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Recovery single-instance guard is already owned, signaling recovery instance...");

        if (!SignalAndWaitForSingleInstanceActivation() && Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Recovery instance did not acknowledge activation.");

        CleanupSingleInstanceResources();
        return false;
    }

    private void BringMainWindowToForegroundFromSingleInstanceThread()
    {
        if (Current == null || Current.Dispatcher == null)
            return;

        try
        {
            Current.Dispatcher.BeginInvoke(async () =>
            {
                if (Current.MainWindow is { } window)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Another instance started, bringing this one to front instead...");

                    SignalSingleInstanceActivationAck();

                    try
                    {
                        window.BringToForeground();
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Failed to bring existing main window to foreground.", ex);
                    }
                }
                else
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"!!! PANIC !!! This instance is missing main window. Shutting down.");

                    await ShutdownAsync(true);
                }
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in single instance thread dispatcher invoke.", ex);
        }
    }

    private static string ResolveSingleInstanceObjectName(string baseName)
    {
#if UDT_TEST_HOOKS
        var isolationKey = ResolveSingleInstanceIsolationKey();
        if (string.IsNullOrWhiteSpace(isolationKey))
            return baseName;

        var sanitizedKey = string.Concat(isolationKey
            .Trim()
            .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_'));

        return string.IsNullOrWhiteSpace(sanitizedKey)
            ? baseName
            : $"{baseName}_{sanitizedKey}";
#else
        return baseName;
#endif
    }

#if UDT_TEST_HOOKS
    private static string? ResolveSingleInstanceIsolationKey()
    {
        var overridePath = Environment.GetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(overridePath))
            return null;

        try
        {
            return Path.GetFullPath(overridePath);
        }
        catch
        {
            return overridePath;
        }
    }
#endif

    private static void ApplyStartupOverrides(Flags flags)
    {
        _ = flags;
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

    private static async Task InitItsModeFeatureAsync()
    {
        await RunWithErrorHandlingAsync(
            async () =>
            {
                var feature = IoCContainer.Resolve<ITSModeFeature>();
                if (await feature.IsSupportedAsync().ConfigureAwait(false))
                    await feature.SetStateAsync(await feature.GetStateAsync().ConfigureAwait(false)).ConfigureAwait(false);
            },
            "ITS mode feature",
            false
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

    private static async Task InitSensorsGroupControllerFeatureAsync()
    {
        await RunWithErrorHandlingAsync(
            async () =>
            {
                var settings = IoCContainer.Resolve<ApplicationSettings>();
                if (!settings.Store.EnableHardwareSensors)
                    return;

                _ = await IoCContainer.Resolve<SensorsGroupController>().IsSupportedAsync().ConfigureAwait(false);
            },
            "sensors group controller",
            false
        );
    }

    private static async Task InitLampArrayControllerAsync()
    {
        await RunWithErrorHandlingAsync(
            async () =>
            {
                var controller = IoCContainer.Resolve<LampArrayController>();
                var settings = IoCContainer.Resolve<LampArraySettings>();
                await controller.InitializeAsync(settings).ConfigureAwait(false);
            },
            "lamp array controller",
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

    private static async Task InitFanManagerAsync()
    {
        await RunWithErrorHandlingAsync(
            async () =>
            {
                var fanManager = IoCContainer.Resolve<FanCurveManager>();
                if (!await fanManager.IsSupportedAsync().ConfigureAwait(false))
                    return;

                await fanManager.InitializeAsync().ConfigureAwait(false);

                var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
                if (mi.LegionSeries <= LegionSeries.Legion_Legacy)
                {
                    var powerMode = IoCContainer.Resolve<PowerModeFeature>();
                    if (await powerMode.GetStateAsync().ConfigureAwait(false) != PowerModeState.GodMode)
                        return;
                }

                var fanSettings = IoCContainer.Resolve<FanCurveSettings>();
                if (fanSettings.Store.Entries.Count == 0)
                    await fanSettings.SynchronizeStoreAsync().ConfigureAwait(false);

                await fanManager.LoadAndApply(fanSettings.Store.Entries).ConfigureAwait(false);
            },
            "fan manager",
            false
        );
    }

    private static async Task InitAmdOverclockingAsync()
    {
        await RunWithErrorHandlingAsync(
            async () =>
            {
                var controller = IoCContainer.Resolve<AmdOverclockingController>();
                if (!controller.IsActive())
                    return;

                await controller.InitializeAsync().ConfigureAwait(false);

                if (!controller.DoNotApply)
                    await controller.ApplyInternalProfileAsync().ConfigureAwait(false);
            },
            "AMD overclocking",
            false
        );
    }

    private static async Task FinalizeRuntimeProfilesAsync()
    {
        try
        {
            if (IoCContainer.TryResolve<AmdOverclockingController>() is { } amdController && amdController.IsActive())
            {
                amdController.SaveShutdownInfo(new ShutdownInfo
                {
                    Status = "Normal",
                    AbnormalCount = 0
                });
            }

            if (IoCContainer.TryResolve<FanCurveManager>() is { } fanManager &&
                await fanManager.IsSupportedAsync().ConfigureAwait(false))
            {
                await fanManager.SetRegisterAsync(false).ConfigureAwait(false);
            }

            if (IoCContainer.TryResolve<LampArrayController>() is { } lampArrayController &&
                IoCContainer.TryResolve<LampArraySettings>() is { } lampArraySettings)
            {
                lampArrayController.SaveSettings(lampArraySettings);
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Runtime profile finalization failed: {ex.Message}", ex);
        }
    }

    private static void InitMacroController()
    {
        var controller = IoCContainer.Resolve<MacroController>();
        controller.Start();
    }

    public void InitOsd()
    {
        MessagingCenter.Subscribe<OsdChangedMessage>(this, message =>
        {
            Dispatcher.Invoke(() =>
            {
                HandleOsdCommand(message.State);
            });
        });

        var osdSettings = IoCContainer.Resolve<OsdSettings>();

        if (osdSettings.Store.ShowOsd)
        {
            HandleOsdCommand(OsdState.Show);
        }
    }

    private void HandleOsdCommand(OsdState command)
    {
        var osdSettings = IoCContainer.Resolve<OsdSettings>();
        bool shouldBeBar = osdSettings.Store.SelectedStyleIndex == 1;

        switch (command)
        {
            case OsdState.Hidden:
                if (OsdWindow != null)
                {
                    OsdWindow.Hide();
                }
                break;

            case OsdState.Show:
                EnsureCorrectOsdStyle(shouldBeBar);
                OsdWindow?.Show();
                break;

            case OsdState.Toggle:
                if (OsdWindow is { IsVisible: true })
                {
                    OsdWindow.Hide();
                }
                else
                {
                    EnsureCorrectOsdStyle(shouldBeBar);
                    OsdWindow?.Show();
                }
                break;
        }

        osdSettings.Store.ShowOsd = OsdWindow?.IsVisible ?? false;
        osdSettings.SynchronizeStore();
    }

    private void EnsureCorrectOsdStyle(bool shouldBeBar)
    {
        if (OsdWindow != null && (OsdWindow is OsdBarWindow) != shouldBeBar)
        {
            OsdWindow.Close();
            OsdWindow = null;
        }

        EnsureOsdWindowCreated(shouldBeBar);
    }

    private void EnsureOsdWindowCreated(bool isBar)
    {
        if (OsdWindow != null)
        {
            return;
        }

        OsdWindow = isBar ? new OsdBarWindow() : new OsdPanelWindow();
        OsdWindow.Closed += (_, _) => OsdWindow = null;
    }

}
}
