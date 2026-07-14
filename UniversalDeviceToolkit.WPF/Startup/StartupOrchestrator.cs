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
using UniversalDeviceToolkit.Lib;
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
using UniversalDeviceToolkit.Lib.Messaging;
using UniversalDeviceToolkit.Lib.Messaging.Messages;
using UniversalDeviceToolkit.Lib.Network;
using UniversalDeviceToolkit.Lib.Overclocking.Amd;
using UniversalDeviceToolkit.Lib.Plugins;
using UniversalDeviceToolkit.Lib.ResourcesCatalog;
using UniversalDeviceToolkit.Lib.Services;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.WPF.CLI;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using WinFormsApp = System.Windows.Forms.Application;
using WinFormsHighDpiMode = System.Windows.Forms.HighDpiMode;
using SafeStartupHealthGuard = UniversalDeviceToolkit.Lib.Utils.StartupHealthGuard;
using SafeStartupStep = UniversalDeviceToolkit.Lib.Utils.StartupStep;
using SafeStartupRunner = UniversalDeviceToolkit.Lib.Utils.StartupInitializationRunner;
using SafeStartupResult = UniversalDeviceToolkit.Lib.Utils.StartupInitializationResult;

namespace UniversalDeviceToolkit.WPF.Startup
{
    public class StartupOrchestrator
    {
        [Serializable]
        private class StartupAbortException : Exception
        {
            public int ExitCode { get; }

            public StartupAbortException(int exitCode)
                : base($"Startup aborted with exit code {exitCode}")
            {
                ExitCode = exitCode;
            }
        }

        private static readonly string SafeStartBootNotificationMessage =
            "Last startup encountered repeated failures — switched to safe-start this run.";

        private readonly App _app;
        private readonly Flags _flags;
        private ApplicationSettings? _settings;
        private SafeStartupHealthGuard? _startupHealthGuard;
        private HardwareStateRecoveryService? _hardwareStateRecoveryService;
        private bool _shouldEnterSafeMode;
        private IReadOnlyList<string> _skippedStartupSteps = Array.Empty<string>();

        public StartupOrchestrator(App app, StartupEventArgs startupEventArgs)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            ArgumentNullException.ThrowIfNull(startupEventArgs);
            _flags = new Flags(startupEventArgs.Args);
        }

        /// <summary>
        /// Returns the <see cref="UniversalDeviceToolkit.Lib.Utils.StartupHealthGuard"/> instance created by
        /// <see cref="RunAsync"/> so background work (such as
        /// <c>App.StartBackgroundInitialization</c>) can route through the same
        /// timeout + failure-tracking machinery as the foreground steps.
        /// </summary>
        public SafeStartupHealthGuard? HealthGuard => _startupHealthGuard;

        /// <summary>
        /// Returns true when the orchestrator decided to honor either an
        /// explicit <c>--safe-start</c> switch or a persisted "previous run
        /// was unhealthy" signal. Callers use this to short-circuit optional
        /// background work.
        /// </summary>
        public bool ShouldEnterSafeMode => _shouldEnterSafeMode;

        /// <summary>
        /// Names of startup steps the orchestrator skipped because safe-start
        /// mode was active. Useful for diagnostics surfaces (toasts / log
        /// banners).
        /// </summary>
        public IReadOnlyList<string> SkippedSteps => _skippedStartupSteps;

        public (Func<Task>[] initializationSteps, Func<Task>[] serviceStartSteps) GetBackgroundInitializationSteps()
        {
            var vantageDisabler = IoCContainer.Resolve<VantageDisabler>();
            var legionZoneDisabler = IoCContainer.Resolve<LegionZoneDisabler>();
            var fnKeysDisabler = IoCContainer.Resolve<FnKeysDisabler>();
            var applicationSettings = IoCContainer.Resolve<ApplicationSettings>();
            var sensorsGroupController = IoCContainer.Resolve<SensorsGroupController>();
            var lampArrayController = IoCContainer.Resolve<LampArrayController>();
            var lampArraySettings = IoCContainer.Resolve<LampArraySettings>();
            var powerModeFeature = IoCContainer.Resolve<PowerModeFeature>();
            var itsModeFeature = IoCContainer.Resolve<ITSModeFeature>();
            var batteryFeature = IoCContainer.Resolve<BatteryFeature>();
            var rgbKeyboardController = IoCContainer.Resolve<RGBKeyboardBacklightController>();
            var spectrumKeyboardController = IoCContainer.Resolve<SpectrumKeyboardBacklightController>();
            var gpuOverclockController = IoCContainer.Resolve<GPUOverclockController>();
            var hybridModeFeature = IoCContainer.Resolve<HybridModeFeature>();
            var fanCurveManager = IoCContainer.Resolve<FanCurveManager>();
            var fanCurveSettings = IoCContainer.Resolve<FanCurveSettings>();
            var amdOverclockingController = IoCContainer.Resolve<AmdOverclockingController>();
            var automationProcessor = IoCContainer.Resolve<AutomationProcessor>();

            // Safe-start: only light read-only probes. Never re-apply God Mode / OC / fans / Hybrid / automation.
            if (_shouldEnterSafeMode)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Safe-start: skipping hardware re-apply and third-party integrations.");

                Func<Task>[] safeBg =
                [
                    () => LogSoftwareStatusAsync(vantageDisabler, legionZoneDisabler, fnKeysDisabler),
                    () => InitSensorsGroupControllerFeatureAsync(applicationSettings, sensorsGroupController)
                ];
                // IPC still useful for CLI diagnostics; skip AI / HWiNFO / battery sample loops under safe-start.
                Func<Task>[] safePost =
                [
                    () => IoCContainer.Resolve<IpcServer>().StartStopIfNeededAsync()
                ];
                _skippedStartupSteps =
                [
                    "lamp-array", "power-mode", "its-mode", "battery-feature", "rgb-keyboard",
                    "spectrum-keyboard", "gpu-overclock", "hybrid-mode", "fan-manager",
                    "amd-overclock", "automation-processor", "ai-controller", "hwinfo", "battery-monitor"
                ];
                return (safeBg, safePost);
            }

            // Full path: App runs these serially (hardware) then with limited concurrency (services).
            Func<Task>[] bgSteps =
            [
                () => LogSoftwareStatusAsync(vantageDisabler, legionZoneDisabler, fnKeysDisabler),
                () => InitControllerAsync(lampArrayController, lampArraySettings),
                () => InitSensorsGroupControllerFeatureAsync(applicationSettings, sensorsGroupController),
                () => InitPowerModeFeatureAsync(powerModeFeature),
                () => InitItsModeFeatureAsync(itsModeFeature),
                () => InitBatteryFeatureAsync(batteryFeature),
                () => InitRgbKeyboardControllerAsync(rgbKeyboardController),
                () => InitSpectrumKeyboardControllerAsync(spectrumKeyboardController),
                () => InitGpuOverclockControllerAsync(gpuOverclockController),
                () => InitHybridModeAsync(hybridModeFeature),
                () => InitFanManagerAsync(fanCurveManager, powerModeFeature, fanCurveSettings),
                () => InitAmdOverclockingAsync(amdOverclockingController),
                () => InitAutomationProcessorAsync(automationProcessor)
            ];
            Func<Task>[] postSteps =
            [
                () => IoCContainer.Resolve<AIController>().StartIfNeededAsync(),
                () => IoCContainer.Resolve<HWiNFOIntegration>().StartStopIfNeededAsync(),
                () => IoCContainer.Resolve<IpcServer>().StartStopIfNeededAsync(),
                () => IoCContainer.Resolve<BatteryDischargeRateMonitorService>().StartStopIfNeededAsync(),
            ];
            return (bgSteps, postSteps);
        }

        public async Task<int> RunAsync()
        {
            try
            {
                await InitializeBootstrappingAsync();

                DetermineAndApplySafeStartMode();

                if (!await InitializeSingleInstanceAsync())
                    return 0;

                // Language gate runs AFTER single-instance + logging, and BEFORE
                // IoC / plugins / hardware / MainWindow so first paint is either
                // the language window or (after gate) the real MainWindow.
                if (!await RunLanguageGateAsync())
                    return 0;

                await InitializeIoCAsync();

                // Hardware/network reset needs IoC-resolved controllers (God Mode → Balance,
                // fan curves, lighting, etc.). Runs after IoC, before background init.
                RunStartupResetSwitchesIfRequested(_flags);

                // Heal leftover UDT proxy/hosts from a previous crash. Never auto-start acceleration.
                // Safe-start also runs this (diagnostics + restore only).
                await RunNetworkStartupRecoveryAsync().ConfigureAwait(false);

                // Create MainWindow first (needed for IoC/host context) but do NOT show it
                // until device-setup / compatibility gates finish — otherwise the dashboard
                // paints under a non-modal DeviceSetupWindow ("two UIs at once").
                await CreateMainWindowAsync();
                await CheckCompatibilityAsync();
                await ShowMainWindowAsync();

                await LoadPluginsAsync();
                await StartBackgroundInitAsync();
                await InitializeOsdAsync();

                PersistStartupHealthOutcome();

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Start up complete");

                return 0;
            }
            catch (StartupAbortException abort)
            {
                return abort.ExitCode;
            }
            catch (Exception ex)
            {
                PersistStartupHealthOutcome(ex);
                Log.Instance.Error($"Startup critical failure: {ex.Message}", ex);
                return 1;
            }
        }

        private void RunStartupResetSwitchesIfRequested(Flags flags)
        {
            if (!flags.ResetHardwareState && !flags.ResetNetworkState)
                return;

            try
            {
                _hardwareStateRecoveryService ??= new HardwareStateRecoveryService();

                if (flags.ResetHardwareState)
                {
                    // Optional: --restore-processor-min-state also edits the active
                    // Windows power plan's minimum processor state (AC+DC). Off by default.
                    var ok = _hardwareStateRecoveryService.TryResetHardware(
                        out var hwReport,
                        restoreProcessorMinState: flags.RestoreProcessorMinState);
                    WriteResetReport(hwReport, "Hardware state reset result.");
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace(
                            $"--reset-hardware-state: ok={ok}; restoreProcessorMin={flags.RestoreProcessorMinState}; " +
                            $"report={Environment.NewLine}{hwReport}");
                }

                if (flags.ResetNetworkState)
                {
                    var ok = _hardwareStateRecoveryService.TryResetNetwork(out var networkReport);
                    WriteResetReport(networkReport, "Network state reset result.");
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"--reset-network-state: ok={ok}; report={Environment.NewLine}{networkReport}");
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Hardware / network reset unhandled exception.", ex);
                try { Console.Error.WriteLine($"Hardware / network reset failed: {ex.GetType().Name}: {ex.Message}"); }
                catch { /* Console sink failure must not abort startup */ }
            }
        }

        private void DetermineAndApplySafeStartMode()
        {
            _startupHealthGuard ??= new SafeStartupHealthGuard();
            var persistedFailureCount = SafeStartupHealthGuard.ReadPersistedConsecutiveFailureCount();
            var persistedShouldEnterSafeMode = persistedFailureCount >= SafeStartupHealthGuard.DefaultConsecutiveFailureThreshold;
            var interruptedHardwareInit = SafeStartupHealthGuard.IsHardwareInitInProgressMarkerPresent();

            _shouldEnterSafeMode = _flags.SafeStart || persistedShouldEnterSafeMode || interruptedHardwareInit;

            if (interruptedHardwareInit)
            {
                // Consume the marker so we do not loop forever in safe-start after one good run.
                SafeStartupHealthGuard.ClearHardwareInitInProgress();
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Previous run left hardware_init_in_progress.flag; entering safe-start.");
                Log.Instance.Info("SafeStart mode auto-engaged: previous hardware initialization was interrupted.");
            }

            if (_flags.SafeStart)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("SafeStart mode requested via --safe-start switch; non-critical initialization steps will be skipped.");
                Log.Instance.Info("SafeStart mode requested: skipping non-critical initialization steps.");
            }
            else if (persistedShouldEnterSafeMode)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace(
                        $"Last run reported {persistedFailureCount} consecutive failures (threshold {SafeStartupHealthGuard.DefaultConsecutiveFailureThreshold}); auto-engaging safe-start this run.");

                Log.Instance.Info(
                    $"SafeStart mode auto-engaged: previous run reported {persistedFailureCount} consecutive failures.");

                try
                {
                    _startupHealthGuard.MarkShouldEnterSafeMode();

                    if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
                    {
                        dispatcher.BeginInvoke(new Action(PublishSafeStartBootNotification));
                    }
                    else
                    {
                        PublishSafeStartBootNotification();
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Failed to publish safe-start notification: {ex.Message}", ex);
                }
            }

            try
            {
                _startupHealthGuard.ResetFailureCount();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Resetting health guard on startup failed: {ex.Message}", ex);
            }
        }

        private void PublishSafeStartBootNotification()
        {
            try
            {
                MessagingCenter.Publish(new NotificationMessage(
                    NotificationType.UpdateAvailable,
                    NotificationPriority.High,
                    SafeStartBootNotificationMessage));
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Publish NotificationMessage failed: {ex.Message}", ex);
            }
        }

        private void PersistStartupHealthOutcome(Exception? failure = null)
        {
            try
            {
                var guard = _startupHealthGuard;
                if (guard is null)
                    return;

                if (failure is not null)
                {
                    SafeStartupHealthGuard.WritePersistedState(SafeStartupHealthGuard.DefaultConsecutiveFailureThreshold, shouldEnterSafeMode: true);
                    return;
                }

                SafeStartupHealthGuard.WritePersistedState(0, shouldEnterSafeMode: false);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to persist startup-health outcome: {ex.Message}", ex);
            }
        }

        private static void WriteResetReport(string report, string heading)
        {
            try
            {
                Console.Error.WriteLine(heading);
                Console.Error.WriteLine(report);
            }
            catch
            {
                /* Console sink unavailable - intentionally swallowed. */
            }
        }

        /// <summary>
        /// Restores system proxy / UDT hosts / orphaned NetworkProxy workers if a previous
        /// session left incomplete mutations. Does not re-enable or start acceleration.
        /// </summary>
        private static async Task RunNetworkStartupRecoveryAsync()
        {
            try
            {
                var network = IoCContainer.Resolve<INetworkAccelerationService>();
                await network.EnsureCleanSystemStateOnStartupAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Network startup recovery failed: {ex.Message}", ex);
            }
        }

        private Task InitializeBootstrappingAsync()
        {
#if DEBUG
            if (Debugger.IsAttached)
            {
                Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName)
                    .Where(p => p.Id != Environment.ProcessId)
                    .ForEach(p =>
                    {
                        p.Kill(true);
                        p.WaitForExit();
                    });
            }
#endif

            Log.Instance.IsTraceEnabled = _flags.IsTraceEnabled;
            Environment.SetEnvironmentVariable("LLT_LOG_PATH", Log.Instance.LogPath);
            _app.RegisterExceptionHandlers();

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Flags: {_flags}");

            return Task.CompletedTask;
        }

        private async Task<bool> InitializeSingleInstanceAsync()
        {
            if (!App.Current.EnsureSingleInstance())
            {
                App.ExitDuplicateInstance();
                return false;
            }

            await Task.CompletedTask;
            return true;
        }

        private async Task<bool> RunLanguageGateAsync()
        {
            var allowOfflineEnglish = _shouldEnterSafeMode || _flags.SafeStart;
            var languagePackManager = App.CreateStartupLanguagePackManager(_flags);
            var outcome = await LocalizationHelper.SetLanguageAsync(true, languagePackManager, allowOfflineEnglish);

            if (outcome == LanguageGateOutcome.Exit)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Language gate exited; shutting down without creating MainWindow.");
                return false;
            }

            // Load settings after language is known so theme/render prefs apply to MainWindow.
            _settings = new ApplicationSettings();
            return true;
        }

        private async Task CheckCompatibilityAsync()
        {
            // Language selection already completed in RunLanguageGateAsync before MainWindow.
            var applicationSettings = _settings ?? new ApplicationSettings();
            _settings = applicationSettings;

            try
            {
                var machineInformation = await Compatibility.GetMachineInformationAsync();
                await App.RunStartupDeviceSetupIfNeededAsync(machineInformation, _flags);

                var (isCompatible, mi) = await MachineCompatibility.IsCompatibleAsync();

                if (!isCompatible)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"Incompatible system detected. [Vendor={mi.Vendor}, Model={mi.Model}, MachineType={mi.MachineType}, BIOS={mi.BiosVersion}]");

                    var suppressWarning = applicationSettings.Store.DisableUnsupportedHardwareWarning;
                    var shouldContinue = false;

                    if (suppressWarning)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace("Compatibility warning suppressed via application settings.");

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

                        await App.Current.PerformSafeShutdownForIncompatibleSystemAsync(202);
                        throw new StartupAbortException(202);
                    }
                }
            }
            catch (StartupAbortException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"Failed to check device compatibility: {ex.Message}", ex);

                if (Log.Instance.IsTraceEnabled)
                {
                    Log.Instance.Trace($"Compatibility check exception details:", ex);
                    if (ex.InnerException != null)
                        Log.Instance.Trace($"Inner exception: {ex.InnerException.Message}", ex.InnerException);
                    Log.Instance.Trace($"Stack trace: {ex.StackTrace}");
                }

                try
                {
                    Log.Instance.Flush();
                }
                catch (Exception flushEx)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace("Failed to flush log during error handling", flushEx);
                }

                var errorWindow = new CompatibilityCheckErrorWindow(ex);
                errorWindow.ShowDialog();

                await App.Current.PerformSafeShutdownForIncompatibleSystemAsync(200);
                throw new StartupAbortException(200);
            }
        }

        private Task InitializeIoCAsync()
        {
            WinFormsApp.SetHighDpiMode(WinFormsHighDpiMode.PerMonitorV2);
            RenderOptions.ProcessRenderMode = RenderingCompatibilityHelper.GetPreferredRenderMode(_settings);

            IoCContainer.Initialize(
                new UniversalDeviceToolkit.Lib.IoCModule(),
                new UniversalDeviceToolkit.Lib.Plugins.IoCModule(),
                new UniversalDeviceToolkit.Lib.Automation.IoCModule(),
                new UniversalDeviceToolkit.Lib.Macro.IoCModule(),
                new IoCModule()
            );

            PluginHostContext.SetCurrent(new MainAppPluginHostContext(() => Application.Current?.MainWindow));

            IoCContainer.Resolve<HttpClientFactory>().SetProxy(_flags.ProxyUrl, _flags.ProxyUsername, _flags.ProxyPassword, _flags.ProxyAllowAllCerts);
            IoCContainer.Resolve<LanguagePackManager>().ProcessPendingUninstall();

            IoCContainer.Resolve<PowerModeFeature>().AllowAllPowerModesOnBattery = _flags.AllowAllPowerModesOnBattery;
            IoCContainer.Resolve<RGBKeyboardBacklightController>().ForceDisable = _flags.ForceDisableRgbKeyboardSupport;
            IoCContainer.Resolve<SpectrumKeyboardBacklightController>().ForceDisable = _flags.ForceDisableSpectrumKeyboardSupport;
            IoCContainer.Resolve<WhiteKeyboardLenovoLightingBacklightFeature>().ForceDisable = _flags.ForceDisableLenovoLighting;
            IoCContainer.Resolve<PanelLogoLenovoLightingBacklightFeature>().ForceDisable = _flags.ForceDisableLenovoLighting;
            IoCContainer.Resolve<PortsBacklightFeature>().ForceDisable = _flags.ForceDisableLenovoLighting;
            IoCContainer.Resolve<IGPUModeFeature>().ExperimentalGPUWorkingMode = _flags.ExperimentalGPUWorkingMode;
            IoCContainer.Resolve<DGPUNotify>().ExperimentalGPUWorkingMode = _flags.ExperimentalGPUWorkingMode;
            var updateChecker = IoCContainer.Resolve<UpdateChecker>();
            updateChecker.Disable = _flags.DisableUpdateChecker;
            updateChecker.DisableReason = _flags.DisableUpdateChecker ? Flags.DisableUpdateCheckerSwitch : null;

            return Task.CompletedTask;
        }

        private async Task LoadPluginsAsync()
        {
            if (_shouldEnterSafeMode)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Safe-start active; skipping plugin discovery and loading.");
                return;
            }

            var applicationSettings = _settings ?? IoCContainer.Resolve<ApplicationSettings>();
            if (!applicationSettings.Store.ExtensionsEnabled)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Extensions disabled in settings; skipping plugin directory scan.");
                return;
            }

            if (!HasInstalledPlugins())
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("No installed plugins found; skipping plugin directory scan.");
                return;
            }

            await App.InitializePluginsAsync();
            LocalizationHelper.SetPluginResourceCultures();
        }

        private static bool HasInstalledPlugins()
        {
            return PluginPaths.GetAllPossiblePluginsDirectories()
                .Where(Directory.Exists)
                .SelectMany(path => Directory.EnumerateDirectories(path))
                .Any(PluginPaths.ContainsPlugin);
        }

        private Task CreateMainWindowAsync()
        {
            var mainWindow = new MainWindow(IoCContainer.Resolve<ApplicationSettings>(),
                IoCContainer.Resolve<IPluginManager>(),
                IoCContainer.Resolve<SpecialKeyListener>(),
                IoCContainer.Resolve<VantageDisabler>(),
                IoCContainer.Resolve<LegionZoneDisabler>(),
                IoCContainer.Resolve<FnKeysDisabler>(),
                IoCContainer.Resolve<UpdateChecker>())
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                TrayTooltipEnabled = !_flags.DisableTrayTooltip
            };

            Application.Current.MainWindow = mainWindow;
            PluginHostContext.SetCurrent(new MainAppPluginHostContext(() => Application.Current?.MainWindow));

            IoCContainer.Resolve<ThemeManager>().Apply();
            AnimationHelper.UpdateAnimationParameters(IoCContainer.Resolve<ApplicationSettings>());

            return Task.CompletedTask;
        }

        private Task StartBackgroundInitAsync()
        {
            App.Current.StartBackgroundInitialization();
            return Task.CompletedTask;
        }

        private Task ShowMainWindowAsync()
        {
            if (Application.Current.MainWindow is not MainWindow mainWindow)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("MainWindow not yet created when ShowMainWindowAsync was called.");
                return Task.CompletedTask;
            }

            // Apply RDP-aware rendering compatibility BEFORE showing the window
            // so first paint does not stall on a graphics path that may not
            // be available over a remote desktop session.
            mainWindow.SourceInitialized += (_, _) =>
                RenderingCompatibilityHelper.ApplyWindowRenderingCompatibility(
                    mainWindow,
                    PresentationSource.FromVisual(mainWindow) as HwndSource,
                    IoCContainer.Resolve<ApplicationSettings>());

            void ShowAction()
            {
                if (_flags.Minimized)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace("Sending MainWindow to tray...");

                    mainWindow.WindowState = WindowState.Minimized;
                    mainWindow.Show();
                    mainWindow.SendToTray();
                }
                else
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace("Showing MainWindow...");

                    // Show() on the dispatcher at Normal priority so the
                    // window is presented in the very next render cycle and
                    // is not queued behind the still-running async setup.
                    mainWindow.Show();
                    mainWindow.Activate();

                    if (mainWindow.WindowState == WindowState.Minimized)
                        mainWindow.WindowState = WindowState.Normal;
                }
            }

            if (Application.Current.Dispatcher.CheckAccess())
            {
                ShowAction();
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(ShowAction),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            }

            _ = Application.Current.Dispatcher.BeginInvoke(App.CheckPendingCrashReports,
                System.Windows.Threading.DispatcherPriority.Background);

            return Task.CompletedTask;
        }

        private Task InitializeOsdAsync()
        {
            App.Current.InitOsd();
            return Task.CompletedTask;
        }

        private static Task RunWithErrorHandlingAsync(Func<Task> action, string operationName, bool logOnSuccess = true)
        {
            return App.RunInitStepAsync(action, operationName, logOnSuccess);
        }

        private static async Task LogSoftwareStatusAsync(VantageDisabler vantageDisabler, LegionZoneDisabler legionZoneDisabler, FnKeysDisabler fnKeysDisabler)
        {
            if (!Log.Instance.IsTraceEnabled)
                return;

            var statuses = await Task.WhenAll(
                vantageDisabler.GetStatusAsync(),
                legionZoneDisabler.GetStatusAsync(),
                fnKeysDisabler.GetStatusAsync()
            );

            Log.Instance.Trace($"Vantage status: {statuses[0]}");
            Log.Instance.Trace($"LegionZone status: {statuses[1]}");
            Log.Instance.Trace($"FnKeys status: {statuses[2]}");
        }

        private static async Task InitControllerAsync(LampArrayController controller, LampArraySettings settings)
        {
            await RunWithErrorHandlingAsync(
                () => controller.InitializeAsync(settings),
                "lamp array controller",
                false
            );
        }

        private static async Task InitSensorsGroupControllerFeatureAsync(ApplicationSettings settings, SensorsGroupController controller)
        {
            await RunWithErrorHandlingAsync(
                async () =>
                {
                    if (!settings.Store.EnableHardwareSensors)
                        return;

                    _ = await controller.IsSupportedAsync();
                },
                "sensors group controller",
                false
            );
        }

        private static async Task InitPowerModeFeatureAsync(PowerModeFeature feature)
        {
            await RunWithErrorHandlingAsync(
                async () =>
                {
                    if (await feature.IsSupportedAsync())
                    {
                        // P1: refuse out-of-range God Mode presets (backup + skip hardware write).
                        var godModeSettings = IoCContainer.TryResolve<GodModeSettings>();
                        var recovery = new HardwareStateRecoveryService();
                        var godModeValid = HardwareConfigRangeValidator.TryValidateOrBackupGodMode(
                            godModeSettings,
                            filename => recovery.TryBackupFile(filename, out _),
                            msg =>
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace(msg);
                                else
                                    Log.Instance.Warning(msg);
                            });

                        if (godModeValid)
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"Ensuring god mode state is applied...");

                            await feature.EnsureGodModeStateIsAppliedAsync();
                        }
                        else if (Log.Instance.IsTraceEnabled)
                        {
                            Log.Instance.Trace("Skipping EnsureGodModeStateIsAppliedAsync due to invalid God Mode range.");
                        }

                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Ensuring correct power plan is set...");

                        await feature.EnsureCorrectWindowsPowerSettingsAreSetAsync();
                    }
                },
                "power mode feature",
                false
            );
        }

        private static async Task InitItsModeFeatureAsync(ITSModeFeature feature)
        {
            await RunWithErrorHandlingAsync(
                async () =>
                {
                    if (await feature.IsSupportedAsync())
                        await feature.SetStateAsync(await feature.GetStateAsync());
                },
                "ITS mode feature",
                false
            );
        }

        private static async Task InitBatteryFeatureAsync(BatteryFeature feature)
        {
            await RunWithErrorHandlingAsync(
                async () =>
                {
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

        private static async Task InitRgbKeyboardControllerAsync(RGBKeyboardBacklightController controller)
        {
            await RunWithErrorHandlingAsync(
                async () =>
                {
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

        private static async Task InitSpectrumKeyboardControllerAsync(SpectrumKeyboardBacklightController controller)
        {
            await RunWithErrorHandlingAsync(
                async () =>
                {
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

        private static async Task InitGpuOverclockControllerAsync(GPUOverclockController controller)
        {
            await RunWithErrorHandlingAsync(
                async () =>
                {
                    if (await controller.IsSupportedAsync())
                    {
                        // P1: refuse out-of-range GPU OC (backup + disable + skip hardware write).
                        var gpuSettings = IoCContainer.TryResolve<GPUOverclockSettings>();
                        var recovery = new HardwareStateRecoveryService();
                        var gpuValid = HardwareConfigRangeValidator.TryValidateOrBackupGpuOverclock(
                            controller,
                            gpuSettings,
                            maxCoreDeltaMhz: GPUOverclockController.GetMaxCoreDeltaMhz(),
                            maxMemoryDeltaMhz: HardwareConfigRangeValidator.DefaultMaxGpuMemoryDeltaMhz,
                            tryBackupFile: filename => recovery.TryBackupFile(filename, out _),
                            log: msg =>
                            {
                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace(msg);
                                else
                                    Log.Instance.Warning(msg);
                            });

                        if (!gpuValid)
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace("Skipping EnsureOverclockIsAppliedAsync due to invalid GPU OC range.");
                            return;
                        }

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

        private static async Task InitHybridModeAsync(HybridModeFeature feature)
        {
            await RunWithErrorHandlingAsync(
                async () =>
                {
                    await feature.EnsureDGPUEjectedIfNeededAsync();
                },
                "hybrid mode"
            );
        }

        private static async Task InitFanManagerAsync(FanCurveManager fanManager, PowerModeFeature powerMode, FanCurveSettings fanSettings)
        {
            await RunWithErrorHandlingAsync(
                async () =>
                {
                    if (!await fanManager.IsSupportedAsync())
                        return;

                    await fanManager.InitializeAsync();

                    var mi = await Compatibility.GetMachineInformationAsync();
                    if (mi.LegionSeries <= LegionSeries.Legion_Legacy)
                    {
                        if (await powerMode.GetStateAsync() != PowerModeState.GodMode)
                            return;
                    }

                    // P1: refuse out-of-range fan curves (backup + clear + skip hardware write).
                    var recovery = new HardwareStateRecoveryService();
                    var fansValid = HardwareConfigRangeValidator.TryValidateOrBackupFanCurves(
                        fanSettings,
                        filename => recovery.TryBackupFile(filename, out _),
                        msg =>
                        {
                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace(msg);
                            else
                                Log.Instance.Warning(msg);
                        });

                    if (!fansValid)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace("Skipping fan curve apply due to invalid range.");
                        return;
                    }

                    if (fanSettings.Store.Entries.Count == 0)
                        await fanSettings.SynchronizeStoreAsync();

                    await fanManager.LoadAndApply(fanSettings.Store.Entries);
                },
                "fan manager",
                false
            );
        }

        private static async Task InitAmdOverclockingAsync(AmdOverclockingController controller)
        {
            await RunWithErrorHandlingAsync(
                async () =>
                {
                    if (!controller.IsActive())
                        return;

                    await controller.InitializeAsync();

                    if (!controller.DoNotApply)
                        await controller.ApplyInternalProfileAsync();
                },
                "AMD overclocking",
                false
            );
        }

        private static async Task InitAutomationProcessorAsync(AutomationProcessor automationProcessor)
        {
            await RunWithErrorHandlingAsync(
                async () =>
                {
                    await automationProcessor.InitializeAsync();
                    automationProcessor.RunOnStartup();
                },
                "automation processor"
            );
        }
    }
}
