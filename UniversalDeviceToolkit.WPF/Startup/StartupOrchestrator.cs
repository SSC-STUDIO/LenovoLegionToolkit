using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Integrations;
using LenovoLegionToolkit.Lib.Listeners;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Features.Hybrid;
using LenovoLegionToolkit.Lib.Features.Hybrid.Notify;
using LenovoLegionToolkit.Lib.Features.PanelLogo;
using LenovoLegionToolkit.Lib.Features.WhiteKeyboardBacklight;
using LenovoLegionToolkit.Lib.Plugins;
using LenovoLegionToolkit.Lib.ResourcesCatalog;
using LenovoLegionToolkit.Lib.Services;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Macro;
using LenovoLegionToolkit.Lib.Controllers.Sensors;
using LenovoLegionToolkit.Lib.Overclocking.Amd;
using UniversalDeviceToolkit.WPF.CLI;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using WinFormsApp = System.Windows.Forms.Application;
using WinFormsHighDpiMode = System.Windows.Forms.HighDpiMode;

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

        private readonly Flags _flags;
        private ApplicationSettings? _settings;

        public StartupOrchestrator(Flags flags)
        {
            _flags = flags ?? throw new ArgumentNullException(nameof(flags));
        }

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

        public async Task<int> RunAsync(StartupEventArgs e)
        {
            try
            {
                if (!await EnsureSingleInstanceAsync())
                    return 0;

                await ConfigureCompatibilityAsync();
                await SetupIoCAsync();
                await InitializePluginsAsync();
                await CreateMainWindowAsync();
                await StartBackgroundServicesAsync();
                await ShowMainWindowAsync();
                await InitializeOsdAsync();

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
                Log.Instance.Error($"Startup critical failure: {ex.Message}", ex);
                return 1;
            }
        }

        private async Task<bool> EnsureSingleInstanceAsync()
        {
            if (!App.Current.EnsureSingleInstance())
            {
                App.ExitDuplicateInstance();
                return false;
            }

            await Task.CompletedTask;
            return true;
        }

        private async Task ConfigureCompatibilityAsync()
        {
            await LocalizationHelper.SetLanguageAsync(true, App.CreateStartupLanguagePackManager(_flags));

            var applicationSettings = new ApplicationSettings();
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
                catch
                {
                }

                var errorWindow = new CompatibilityCheckErrorWindow(ex);
                errorWindow.ShowDialog();

                await App.Current.PerformSafeShutdownForIncompatibleSystemAsync(200);
                throw new StartupAbortException(200);
            }
        }

        private Task SetupIoCAsync()
        {
            WinFormsApp.SetHighDpiMode(WinFormsHighDpiMode.PerMonitorV2);
            RenderOptions.ProcessRenderMode = RenderingCompatibilityHelper.GetPreferredRenderMode(_settings);

            IoCContainer.Initialize(
                new LenovoLegionToolkit.Lib.IoCModule(),
                new LenovoLegionToolkit.Lib.Plugins.IoCModule(),
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

        private async Task InitializePluginsAsync()
        {
            await App.InitializePluginsAsync();
            LocalizationHelper.SetPluginResourceCultures();
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

        private Task StartBackgroundServicesAsync()
        {
            App.Current.StartBackgroundInitialization();
            return Task.CompletedTask;
        }

        private Task ShowMainWindowAsync()
        {
            if (_flags.Minimized)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Sending MainWindow to tray...");

                if (Application.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.WindowState = WindowState.Minimized;
                    mainWindow.Show();
                    mainWindow.SendToTray();
                }
            }
            else
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("Showing MainWindow...");

                Application.Current.MainWindow?.Show();
            }

            _ = Application.Current.Dispatcher.BeginInvoke(App.CheckPendingCrashReports, DispatcherPriority.Background);

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

                    _ = await controller.IsSupportedAsync().ConfigureAwait(false);
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
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Ensuring god mode state is applied...");

                        await feature.EnsureGodModeStateIsAppliedAsync();

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
                    if (await feature.IsSupportedAsync().ConfigureAwait(false))
                        await feature.SetStateAsync(await feature.GetStateAsync().ConfigureAwait(false)).ConfigureAwait(false);
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
                    if (!await fanManager.IsSupportedAsync().ConfigureAwait(false))
                        return;

                    await fanManager.InitializeAsync().ConfigureAwait(false);

                    var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
                    if (mi.LegionSeries <= LegionSeries.Legion_Legacy)
                    {
                        if (await powerMode.GetStateAsync().ConfigureAwait(false) != PowerModeState.GodMode)
                            return;
                    }

                    if (fanSettings.Store.Entries.Count == 0)
                        await fanSettings.SynchronizeStoreAsync().ConfigureAwait(false);

                    await fanManager.LoadAndApply(fanSettings.Store.Entries).ConfigureAwait(false);
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

                    await controller.InitializeAsync().ConfigureAwait(false);

                    if (!controller.DoNotApply)
                        await controller.ApplyInternalProfileAsync().ConfigureAwait(false);
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
