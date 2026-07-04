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
            var exitCode = await orchestrator.RunAsync().ConfigureAwait(false);

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
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    internal static async Task InitializePluginsAsync()
    {
        try
        {
            var pluginManager = GetCachedService<IPluginManager>();
            
            // System Optimization and Tools are now default interfaces, not plugins
            // They are registered directly in MainWindow.xaml as NavigationItems
            // No need to register them as plugins

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

        _backgroundInitializationTask = Task.Run(async () =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var initializationTasks = initializationSteps.Select(step => Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await step().ConfigureAwait(false);
                })).ToArray();
                await Task.WhenAll(initializationTasks).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();
                InitMacroController();

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
            var completedTask = await Task.WhenAny(task, Task.Delay(BACKGROUND_INITIALIZATION_WAIT_TIMEOUT_MS)).ConfigureAwait(false);
            if (completedTask != task)
            {
                _backgroundInitializationCancellationTokenSource?.Cancel();
                try { await Task.WhenAny(task, Task.Delay(500)).ConfigureAwait(false); }
                catch { /* Background task cancellation failed - app startup continues */ }
                return;
            }
        }

        try { await task.ConfigureAwait(false); }
        catch { /* Background initialization failed - app continues startup */ }
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

    private async void Application_Exit(object sender, ExitEventArgs e)
    {
        lock (_shutdownLock)
            _inExitHandler = true;

        PluginHostContext.Reset();

        try { await ShutdownAsync(true).ConfigureAwait(false); }
        catch { /* Shutdown failed - continue with exit anyway */ }

        try { Log.Instance.Shutdown(); }
        catch { /* Log shutdown failed - continue with exit */ }

        StopMacroControllerSafely();
        StopSingleInstanceThreadSafely();

        await ForceExitAsync((uint)e.ApplicationExitCode).ConfigureAwait(false);
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

    private async Task ForceExitAsync(uint exitCode)
    {
        await Task.Delay(100).ConfigureAwait(false);
        try { Environment.Exit((int)exitCode); }
        catch { /* Environment.Exit failed - use fallback exit method */ }
        ExitProcess(exitCode);
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
                _backgroundInitializationCancellationTokenSource?.Dispose();
                _backgroundInitializationCancellationTokenSource = null;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Error disposing cancellation token source during shutdown: {ex.Message}");
            }

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
                catch { /* Plugin shutdown failed - continue with other plugins */ }
            })).ToList();

            await Task.WhenAll(shutdownTasks).ConfigureAwait(false);

            await Task.Delay(200).ConfigureAwait(false);

            if (pluginManager is PluginManager manager)
                await manager.PerformPendingDeletionsAsync().ConfigureAwait(false);
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

                    await ShutdownAsync(true).ConfigureAwait(false);
                }
            });
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error in single instance thread dispatcher invoke.", ex);
        }
    }

    private static void ApplyStartupOverrides(Flags flags)
    {
        _ = flags;
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
