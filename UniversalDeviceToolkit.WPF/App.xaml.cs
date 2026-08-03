using UniversalDeviceToolkit.Lib.System;
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
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Controllers.Sensors;
using UniversalDeviceToolkit.Lib.Extensions;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Features.Hybrid;
using UniversalDeviceToolkit.Lib.Features.Hybrid.Notify;
using UniversalDeviceToolkit.Lib.Features.PanelLogo;
using UniversalDeviceToolkit.Lib.Features.WhiteKeyboardBacklight;
using UniversalDeviceToolkit.Lib.Integrations;
using UniversalDeviceToolkit.Lib.Listeners;
using UniversalDeviceToolkit.Lib.AutoListeners;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.Lib.Overclocking.Amd;
using UniversalDeviceToolkit.Lib.Services;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.ResourcesCatalog;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Lib.Messaging;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.WPF.CLI;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Pages;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows;
using UniversalDeviceToolkit.WPF.Windows.Osd;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using UniversalDeviceToolkit.WPF.Startup;
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

        private const int BACKGROUND_INITIALIZATION_WAIT_TIMEOUT_MS = 3000;

    private SingleInstanceGuard? _singleInstanceGuard;
    private Task? _backgroundInitializationTask;
    private CancellationTokenSource? _backgroundInitializationCancellationTokenSource;
    private readonly object _shutdownLock = new();
    private Task? _shutdownTask;
    private bool _exitRequested;
    private bool _shutdownInvoked;
    private bool _inExitHandler;
    private bool _exceptionHandlerExecuting;
    private StartupOrchestrator? _orchestrator;

    // Lazily-resolved service caches (service-locator reduction per issue #129).
    // WPF constructs App with a parameterless constructor and the IoC container is
    // initialised later in Application_Startup, so lazy resolution via the shared
    // GetCachedService<T>/TryGetCachedService<T> helpers is the only option.
    // The dictionary is populated on first access and reused thereafter so each
    // registered type is resolved from the IoC container exactly once.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, object?> s_serviceCache = new();

    private static T GetCachedService<T>() where T : class
    {
        return (T)(s_serviceCache.GetOrAdd(typeof(T), _ => IoCContainer.Resolve<T>()) ?? IoCContainer.Resolve<T>());
    }

    private static T? TryGetCachedService<T>() where T : class
    {
        return (T?)s_serviceCache.GetOrAdd(typeof(T), _ => IoCContainer.TryResolve<T>());
    }

    internal static async Task RunInitStepAsync(Func<Task> action, string operationName, bool logOnSuccess = true)
    {
        try
        {
            if (Log.Instance.IsTraceEnabled && logOnSuccess)
                Log.Instance.Trace($"Initializing {operationName}...");

            await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Couldn't initialize {operationName}.", ex);
        }
    }

    private static string T(string key, string fallback) => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

    public new static App Current => (App)Application.Current;

    public Window? OsdWindow;

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            var orchestrator = new StartupOrchestrator(this, e);
            _orchestrator = orchestrator;
            var exitCode = await orchestrator.RunAsync();

            if (exitCode != 0)
                Environment.Exit(exitCode);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(Application_Startup)}.", ex);
        }
    }

    internal void RegisterExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
        AppDomain.CurrentDomain.ProcessExit += AppDomain_ProcessExit;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void AppDomain_ProcessExit(object? sender, EventArgs e)
    {
        // Last-resort cleanup: if Application_Exit was not called (e.g. process killed externally),
        // try to release global hooks here. This is a best-effort attempt because by the time
        // ProcessExit fires, the finalizer thread may have already started.
        try
        {
            StopMacroControllerSafely();

            if (TryGetCachedService<NativeWindowsMessageListener>() is { } nwml)
                nwml.StopAsync().GetAwaiter().GetResult();

            // UserInactivityAutoListener.StopAsync() is protected; dispose instead
            // (AbstractAutoListener.Dispose() calls StopAsync() internally)
            if (TryGetCachedService<UserInactivityAutoListener>() is { } uial)
                ((IDisposable)uial).Dispose();
        }
        catch
        {
            // Best effort only — process is exiting anyway
        }
    }

    internal static async Task InitializePluginsAsync()
    {
        try
        {
            var pluginManager = GetCachedService<IPluginManager>();
            
            // System Optimization and Tools are now default interfaces, not plugins
            // They are registered directly in MainWindow.xaml as NavigationItems
            // No need to register them as plugins

            // Drop retired plugins (migrated to built-in features) before loading assemblies.
            pluginManager.PruneRetiredPlugins();

            // Scan and load plugins from the plugins directory
            // This will automatically discover and register external plugins
            await pluginManager.ScanAndLoadPluginsAsync().ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Plugins initialized successfully.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to initialize plugins.", ex);
        }
    }

    internal static async Task RunStartupDeviceSetupIfNeededAsync(MachineInformation machineInformation, Flags flags)
    {
        try
        {
            await StartupDeviceSetupCoordinator.CreateDefault(CreateStartupHttpClientFactory(flags)).RunIfNeededAsync(machineInformation).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Startup device setup failed; continuing with current compatibility state.", ex);
        }
    }

    internal static LanguagePackManager CreateStartupLanguagePackManager(Flags flags) =>
        new(new OnlineResourceCatalogClient(CreateStartupHttpClientFactory(flags)));

    internal static HttpClientFactory CreateStartupHttpClientFactory(Flags flags)
    {
        var httpClientFactory = new HttpClientFactory();
        httpClientFactory.SetProxy(flags.ProxyUrl, flags.ProxyUsername, flags.ProxyPassword, flags.ProxyAllowAllCerts);
        return httpClientFactory;
    }

    internal static void CheckPendingCrashReports()
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
                    var notificationWindow = new CrashReportNotificationWindow(mostRecentReport)
                    {
                        Owner = Application.Current?.MainWindow as Window,
                        ShowInTaskbar = false
                    };
                    notificationWindow.Show();

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

    internal void StartBackgroundInitialization()
    {
        _backgroundInitializationCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _backgroundInitializationCancellationTokenSource.Token;

        var (initializationSteps, serviceStartSteps) = _orchestrator?.GetBackgroundInitializationSteps() ?? ([], []);
        // Prefer the orchestrator's guard so consecutive-failure tracking is shared.
        var healthGuard = _orchestrator?.HealthGuard ?? new StartupHealthGuard();

        _backgroundInitializationTask = Task.Run(async () =>
        {
            var totalSw = System.Diagnostics.Stopwatch.StartNew();
            var completedCleanly = false;
            // incomplete marker: set before any hardware work; cleared only on clean success.
            StartupHealthGuard.MarkHardwareInitInProgress();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Hardware / WMI / EC steps run strictly serially via StartupInitializationRunner
                // to avoid driver thrash, concurrent WMI load, and post-start system lag.
                // Steps are non-critical so one failure does not abort the rest of the pass.
                // (Safe-start already filtered the list to read-only probes only.)
                var runner = new StartupInitializationRunner(healthGuard, safeStart: false);
                for (var i = 0; i < initializationSteps.Length; i++)
                {
                    var step = initializationSteps[i];
                    var stepName = $"bg-hw-{i}";
                    runner.RegisterStep(stepName, TimeSpan.FromSeconds(45), step, isCritical: false);
                }

                var hwResult = await runner.RunAsync(cancellationToken).ConfigureAwait(false);
                if (Log.Instance.IsTraceEnabled)
                {
                    Log.Instance.Trace(
                        $"Background hardware init via StartupInitializationRunner: success={hwResult.Success}, " +
                        $"failed=[{string.Join(", ", hwResult.FailedSteps)}], elapsed={totalSw.ElapsedMilliseconds}ms.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                InitMacroController();

                // Independent background services: limited parallelism via SemaphoreSlim(2).
                const int maxServiceConcurrency = 2;
                await RunWithLimitedConcurrencyAsync(serviceStartSteps, maxServiceConcurrency, cancellationToken)
                    .ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

#if !DEBUG
                Autorun.Validate();
#endif

                completedCleanly = true;
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Background initialization completed in {totalSw.ElapsedMilliseconds}ms.");
            }
            catch (OperationCanceledException)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Background initialization was cancelled after {totalSw.ElapsedMilliseconds}ms.");
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Background initialization failed after {totalSw.ElapsedMilliseconds}ms.", ex);
            }
            finally
            {
                if (completedCleanly)
                    StartupHealthGuard.ClearHardwareInitInProgress();
            }
        }, cancellationToken);

        _backgroundInitializationTask = _backgroundInitializationTask.ContinueWith(t =>
        {
            if (t.IsFaulted && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Background initialization task completed faulted and was observed.", t.Exception);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    /// <summary>
    /// Runs async work items with a fixed concurrency cap (default 2) to limit
    /// simultaneous WMI/network/service startup without serializing everything.
    /// </summary>
    private static async Task RunWithLimitedConcurrencyAsync(
        IReadOnlyList<Func<Task>> steps,
        int maxConcurrency,
        CancellationToken cancellationToken)
    {
        if (steps.Count == 0)
            return;

        maxConcurrency = Math.Max(1, maxConcurrency);
        using var gate = new System.Threading.SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = new Task[steps.Count];

        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            tasks[i] = Task.Run(async () =>
            {
                await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await step().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Background service start step failed.", ex);
                }
                finally
                {
                    gate.Release();
                }
            }, cancellationToken);
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    private async Task AwaitBackgroundInitializationAsync()
    {
        if (_backgroundInitializationTask is not { } task)
            return;

        if (!task.IsCompleted)
        {
            var completedTask = await Task.WhenAny(task, Task.Delay(BACKGROUND_INITIALIZATION_WAIT_TIMEOUT_MS)).ConfigureAwait(false);
            if (completedTask != task)
            {
                _backgroundInitializationCancellationTokenSource?.Cancel();
                try { await Task.WhenAny(task, Task.Delay(500)).ConfigureAwait(false); }
                catch (Exception ex)
                {
                    Log.Instance.WarningOnce(
                        "bg-init-cancel-wait",
                        "Background initialization cancellation wait failed; startup continues.",
                        ex);
                }
                return;
            }
        }

        try { await task.ConfigureAwait(false); }
        catch (Exception ex)
        {
            Log.Instance.Warning(
                "Background initialization failed; app continues startup.",
                ex);
        }
    }

    /// <summary>
    /// Performs safe shutdown for incompatible systems to prevent process residue
    /// This method ensures all resources are properly cleaned up before exit
    /// </summary>
    internal async Task PerformSafeShutdownForIncompatibleSystemAsync(int? exitCode = null)
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
                    var completedTask = await Task.WhenAny(_backgroundInitializationTask, Task.Delay(1000)).ConfigureAwait(false);
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
                await Log.Instance.ShutdownAsync().ConfigureAwait(false);
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

    private async void Application_Exit(object sender, ExitEventArgs e)
    {
        try
        {
            lock (_shutdownLock)
                _inExitHandler = true;

            PluginHostContext.Reset();

            try { await ShutdownAsync(true); }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Shutdown failed during Application_Exit.", ex);
            }

            try { await Log.Instance.ShutdownAsync().ConfigureAwait(false); }
            catch { /* Log shutdown failed - continue with exit */ }

            StopMacroControllerSafely();
            StopSingleInstanceThreadSafely();

            IoCContainer.Dispose();

            await ForceExitAsync((uint)e.ApplicationExitCode);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(Application_Exit)}.", ex);

            try { ExitProcess((uint)e.ApplicationExitCode); }
            catch { /* last-resort exit */ }
        }
    }

    public void RestartMainWindow()
    {
        if (MainWindow is MainWindow mw)
        {
            mw.SuppressClosingEventHandler = true;
            mw.Close();
        }

        var mainWindow = new MainWindow(GetCachedService<ApplicationSettings>(),
            GetCachedService<IPluginManager>(),
            GetCachedService<SpecialKeyListener>(),
            GetCachedService<VantageDisabler>(),
            GetCachedService<LegionZoneDisabler>(),
            GetCachedService<FnKeysDisabler>(),
            GetCachedService<UpdateChecker>())
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
            if (TryGetCachedService<MacroController>() is { } macroController)
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
    private void StopSingleInstanceThreadSafely() => _singleInstanceGuard?.StopListener();

    /// <summary>
    /// Cleanup single instance resources (mutex and wait handle)
    /// </summary>
    private void CleanupSingleInstanceResources()
    {
        if (_singleInstanceGuard is null)
            return;

        _singleInstanceGuard.Dispose();
        _singleInstanceGuard = null;
    }

    private Task ForceExitAsync(uint exitCode)
    {
        try { Environment.Exit((int)exitCode); }
        catch { /* Environment.Exit failed - use fallback exit method */ }
        ExitProcess(exitCode);
        return Task.CompletedTask;
    }

    private static async Task StopServiceAsync<T>(Func<T, Task> stopAction, string serviceName) where T : class
    {
        try
        {
            if (TryGetCachedService<T>() is not { } service)
                return;

            await stopAction(service).ConfigureAwait(false);
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

        await shutdownTask.ConfigureAwait(false);

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
            try
            {
                _backgroundInitializationCancellationTokenSource?.Cancel();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error cancelling background initialization during shutdown: {ex.Message}");
            }

            StopSingleInstanceThreadSafely();
            CleanupSingleInstanceResources();

            await AwaitBackgroundInitializationAsync().ConfigureAwait(false);

            try
            {
                _backgroundInitializationCancellationTokenSource?.Dispose();
                _backgroundInitializationCancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error disposing cancellation token source during shutdown: {ex.Message}");
            }

            await StopPluginsAsync().ConfigureAwait(false);

            // Stop network acceleration worker and restore system proxy/hosts before other services.
            try
            {
                if (TryGetCachedService<UniversalDeviceToolkit.Lib.Network.INetworkAccelerationService>() is { } networkAcceleration)
                    await networkAcceleration.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error stopping network acceleration during shutdown: {ex.Message}");
            }

            var stopServicesTask = Task.WhenAll(
                StopServiceAsync<AIController>(controller => controller.StopAsync(), "AI controller"),
                StopServiceAsync<RGBKeyboardBacklightController>(controller => controller.SetLightControlOwnerAsync(false), "RGB keyboard controller"),
                StopServiceAsync<SessionLockUnlockListener>(listener => listener.StopAsync(), "session lock/unlock listener"),
                StopServiceAsync<HWiNFOIntegration>(integration => integration.StopAsync(), "HWiNFO integration"),
                StopServiceAsync<IpcServer>(server => server.StopAsync(), "IPC server"),
                StopServiceAsync<BatteryDischargeRateMonitorService>(monitor => monitor.StopAsync(), "battery monitor"),
                StopServiceAsync<LampArrayController>(controller => controller.StopAsync(), "lamp array controller"),
                StopServiceAsync<NativeWindowsMessageListener>(listener => listener.StopAsync(), "native Windows message listener")
            );

            // UserInactivityAutoListener.StopAsync() is protected; dispose it instead
            // (AbstractAutoListener.Dispose() calls StopAsync() internally)
            await StopUserInactivityListenerAsync().ConfigureAwait(false);

            var completedTask = await Task.WhenAny(stopServicesTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
            if (completedTask != stopServicesTask)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Service stop timed out after 2 seconds.");
            }

            await FinalizeRuntimeProfilesAsync().ConfigureAwait(false);

            StopMacroControllerSafely();
            StopSingleInstanceThreadSafely();
            CleanupSingleInstanceResources();

            totalStopwatch.Stop();
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Shutdown completed in {totalStopwatch.ElapsedMilliseconds}ms.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Shutdown error: {ex.Message}");
        }
    }

    private async Task StopPluginsAsync()
    {
        try
        {
            if (TryGetCachedService<IPluginManager>() is not { } pluginManager)
                return;

            var registeredPlugins = pluginManager.GetRegisteredPlugins().ToList();
            if (registeredPlugins.Count == 0)
                return;

            var shutdownTasks = registeredPlugins.Select(plugin => Task.Run(() =>
            {
                try { plugin.OnShutdown(); }
                catch (Exception ex)
                {
                    Log.Instance.Warning(
                        $"Plugin OnShutdown failed; continuing with other plugins. [{plugin.GetType().Name}]",
                        ex);
                }
            })).ToList();

            await Task.WhenAll(shutdownTasks).ConfigureAwait(false);

            await Task.Delay(200).ConfigureAwait(false);

            if (pluginManager is PluginManager manager)
                await manager.PerformPendingDeletionsAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning("Plugin shutdown process failed; continuing app shutdown.", ex);
        }
    }

    /// <summary>
    /// Stops the UserInactivityAutoListener by disposing it.
    /// UserInactivityAutoListener.StopAsync() is protected (called internally by AbstractAutoListener),
    /// so we dispose it instead — Dispose() calls StopAsync() internally and releases keyboard/mouse hooks.
    /// CRITICAL: If these global hooks are not released, the process cannot exit cleanly and the system stutters.
    /// </summary>
    private static async Task StopUserInactivityListenerAsync()
    {
        try
        {
            if (TryGetCachedService<UserInactivityAutoListener>() is not { } listener)
                return;

            // AbstractAutoListener.Dispose() calls StopAsync() which releases WH_KEYBOARD_LL/WH_MOUSE_LL hooks
            await Task.Run(() => ((IDisposable)listener).Dispose()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Instance.Warning(
                "UserInactivityAutoListener dispose failed during shutdown; hooks may linger until process exit.",
                ex);
        }
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
                MessageBox.Show(string.Format(Resource.UnexpectedException, exception?.ToString() ?? T("App_UnhandledException_Unknown", "Unknown exception.")),
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
                MessageBox.Show(string.Format(Resource.UnexpectedException, e.Exception.ToString()),
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


    internal bool EnsureSingleInstance()
    {
        if (_singleInstanceGuard is not null)
            throw new InvalidOperationException("Single instance guard already initialized.");

        _singleInstanceGuard = new SingleInstanceGuard(Dispatcher);

        if (!_singleInstanceGuard.TryAcquire(out _))
        {
            _singleInstanceGuard = null;
            return false;
        }

        _singleInstanceGuard.StartListener(BringMainWindowToForegroundFromSingleInstanceThread);
        return true;
    }

    internal static void ExitDuplicateInstance()
    {
        try { Log.Instance.Shutdown(); }
        catch { /* Logging shutdown failed; duplicate instance must still exit. */ }

        try { Environment.Exit(0); }
        catch { /* Fall back to native process exit. */ }

        ExitProcess(0);
    }

    private void BringMainWindowToForegroundFromSingleInstanceThread()
    {
        if (Current == null || Current.Dispatcher == null)
            return;

        try
        {
            Current.Dispatcher.BeginInvoke(async () =>
            {
                try
                {
                    if (Current.MainWindow is { } window)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Another instance started, bringing this one to front instead...");

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
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Error handling single-instance foreground request.", ex);
                }
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in single instance thread dispatcher invoke.", ex);
        }
    }

    private static async Task FinalizeRuntimeProfilesAsync()
    {
        try
        {
            if (TryGetCachedService<AmdOverclockingController>() is { } amdController && amdController.IsActive())
            {
                amdController.SaveShutdownInfo(new ShutdownInfo
                {
                    Status = "Normal",
                    AbnormalCount = 0
                });
            }

            if (TryGetCachedService<FanCurveManager>() is { } fanManager &&
                await fanManager.IsSupportedAsync().ConfigureAwait(false))
            {
                await fanManager.SetRegisterAsync(false).ConfigureAwait(false);
            }

            if (TryGetCachedService<LampArrayController>() is { } lampArrayController &&
                TryGetCachedService<LampArraySettings>() is { } lampArraySettings)
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
        var controller = GetCachedService<MacroController>();
        controller.Start();
    }

    public void InitOsd()
    {
        MessagingCenter.Subscribe<OsdChangedMessage>(this, message =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                HandleOsdCommand(message.State);
            });
        });

        var osdSettings = GetCachedService<OsdSettings>();

        if (osdSettings.Store.ShowOsd)
        {
            HandleOsdCommand(OsdState.Show);
        }
    }

    private void HandleOsdCommand(OsdState command)
    {
        var osdSettings = GetCachedService<OsdSettings>();
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
