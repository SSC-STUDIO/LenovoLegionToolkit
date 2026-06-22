using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Extensions;
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
using UniversalDeviceToolkit.WPF.CLI;
using UniversalDeviceToolkit.WPF.Extensions;
using UniversalDeviceToolkit.WPF.Utils;
using UniversalDeviceToolkit.WPF.Windows;
using UniversalDeviceToolkit.WPF.Windows.Utils;
using WinFormsApp = System.Windows.Forms.Application;
using WinFormsHighDpiMode = System.Windows.Forms.HighDpiMode;

namespace UniversalDeviceToolkit.WPF.Startup
{
    /// <summary>
    /// Orchestrates the application startup flow in a structured, testable manner.
    /// Extracted from <see cref="App.Application_Startup"/> to eliminate the <c>async void</c>
    /// pattern and enable testing of individual startup phases.
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupOrchestrator"/> class.
        /// </summary>
        /// <param name="flags">The command-line flags parsed at startup.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="flags"/> is null.</exception>
        public StartupOrchestrator(Flags flags)
        {
            _flags = flags ?? throw new ArgumentNullException(nameof(flags));
        }

        /// <summary>
        /// Runs the full application startup sequence.
        /// </summary>
        /// <param name="e">The startup event arguments containing command-line arguments.</param>
        /// <returns>0 on success; non-zero on abort or failure.</returns>
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

        /// <summary>
        /// Ensures only one instance of the application is running.
        /// If another instance is detected, signals it and exits silently.
        /// </summary>
        /// <returns>True if this instance should continue; false if a duplicate was found.</returns>
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

        /// <summary>
        /// Configures device compatibility settings during startup.
        /// Checks system compatibility, shows unsupported hardware warning if needed,
        /// and handles startup device setup.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="StartupAbortException">Thrown when the system is incompatible and the user chose not to continue.</exception>
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

        /// <summary>
        /// Sets up the IoC container and configures all application services.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Initializes the plugin system.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task InitializePluginsAsync()
        {
            await App.InitializePluginsAsync();
            LocalizationHelper.SetPluginResourceCultures();
        }

        /// <summary>
        /// Creates the main application window and applies theme settings.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Starts background services that run after the main window is created.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task StartBackgroundServicesAsync()
        {
            App.Current.StartBackgroundInitialization();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Shows the main window and checks for pending crash reports from previous sessions.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Initializes the on-screen display (OSD) system.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        private Task InitializeOsdAsync()
        {
            App.Current.InitOsd();
            return Task.CompletedTask;
        }
    }
}
