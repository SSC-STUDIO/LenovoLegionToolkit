#if WINDOWS

using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Controllers.GodMode;
using UniversalDeviceToolkit.Lib.Controllers.Sensors;
using UniversalDeviceToolkit.Lib.Automation.CLI;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Features.Hybrid;
using UniversalDeviceToolkit.Lib.Integrations;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.Lib.Network;
using UniversalDeviceToolkit.Lib.Overclocking.Amd;
using UniversalDeviceToolkit.Lib.Services;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia.Startup;

/// <summary>
/// Runs the Windows host startup work that WPF historically performed after the
/// main window was constructed. Each hardware step is isolated so an unsupported
/// device or integration cannot prevent the rest of Avalonia from becoming usable.
/// </summary>
internal sealed class AvaloniaWindowsStartupCoordinator
{
    private static readonly TimeSpan HardwareStepTimeout = TimeSpan.FromSeconds(45);

    public async Task RunAsync()
    {
        var options = AvaloniaStartupOptions.Load();
        ApplyRequestedRecovery(options);

        // Always heal a stale proxy/hosts configuration, including in safe-start.
        await RunStepAsync("network startup recovery", RecoverNetworkStateAsync).ConfigureAwait(false);

        var healthGuard = new StartupHealthGuard();
        var safeStart = DetermineSafeStart(options, healthGuard);
        var runner = new StartupInitializationRunner(healthGuard, safeStart);
        RegisterHardwareSteps(runner);

        StartupHealthGuard.MarkHardwareInitInProgress();
        var completedCleanly = false;
        try
        {
            var hardwareResult = await runner.RunAsync().ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
            {
                Log.Instance.Trace(
                    $"Avalonia background hardware initialization: success={hardwareResult.Success}, " +
                    $"safeStart={hardwareResult.EnteredSafeMode}, " +
                    $"failed=[{string.Join(", ", hardwareResult.FailedSteps)}], " +
                    $"skipped=[{string.Join(", ", hardwareResult.SkippedSteps)}].");
            }

            if (!hardwareResult.Success)
            {
                StartupHealthGuard.WritePersistedState(
                    StartupHealthGuard.DefaultConsecutiveFailureThreshold,
                    shouldEnterSafeMode: true);
                return;
            }

            completedCleanly = true;
            if (!safeStart)
            {
                StartMacroController();
                StartSmartKeyHandler();
                await StartBackgroundServicesAsync().ConfigureAwait(false);
            }

            StartupHealthGuard.WritePersistedState(0, shouldEnterSafeMode: false);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            StartupHealthGuard.WritePersistedState(
                StartupHealthGuard.DefaultConsecutiveFailureThreshold,
                shouldEnterSafeMode: true);
            Log.Instance.Trace("Avalonia Windows startup coordination failed.", exception);
        }
        finally
        {
            // The WPF host clears this marker after a completed non-critical pass.
            if (completedCleanly)
                StartupHealthGuard.ClearHardwareInitInProgress();
        }
    }

    private static void RegisterHardwareSteps(StartupInitializationRunner runner)
    {
        // The two safe-start steps are read-only. All hardware writes are non-critical
        // and are skipped by the shared runner when recovery mode is active.
        runner.RegisterStep("software status", HardwareStepTimeout, LogSoftwareStatusAsync, isCritical: true);
        runner.RegisterStep("sensors group controller", HardwareStepTimeout, InitializeSensorsAsync, isCritical: true);
        runner.RegisterStep("lamp array controller", HardwareStepTimeout, InitializeLampArrayAsync, isCritical: false);
        runner.RegisterStep("power mode feature", HardwareStepTimeout, InitializePowerModeAsync, isCritical: false);
        runner.RegisterStep("ITS mode feature", HardwareStepTimeout, InitializeItsModeAsync, isCritical: false);
        runner.RegisterStep("battery feature", HardwareStepTimeout, InitializeBatteryAsync, isCritical: false);
        runner.RegisterStep("RGB keyboard controller", HardwareStepTimeout, InitializeRgbKeyboardAsync, isCritical: false);
        runner.RegisterStep("Spectrum keyboard controller", HardwareStepTimeout, InitializeSpectrumKeyboardAsync, isCritical: false);
        runner.RegisterStep("GPU overclock controller", HardwareStepTimeout, InitializeGpuOverclockAsync, isCritical: false);
        runner.RegisterStep("hybrid mode", HardwareStepTimeout, InitializeHybridModeAsync, isCritical: false);
        runner.RegisterStep("fan manager", HardwareStepTimeout, InitializeFanManagerAsync, isCritical: false);
        runner.RegisterStep("AMD overclocking", HardwareStepTimeout, InitializeAmdOverclockingAsync, isCritical: false);
    }

    private static bool DetermineSafeStart(AvaloniaStartupOptions options, StartupHealthGuard healthGuard)
    {
        var persistedFailures = StartupHealthGuard.ReadPersistedConsecutiveFailureCount();
        var interruptedHardwareInitialization = StartupHealthGuard.IsHardwareInitInProgressMarkerPresent();
        var safeStart = options.SafeStart
            || persistedFailures >= StartupHealthGuard.DefaultConsecutiveFailureThreshold
            || interruptedHardwareInitialization;

        if (interruptedHardwareInitialization)
            StartupHealthGuard.ClearHardwareInitInProgress();

        if (safeStart)
        {
            healthGuard.MarkShouldEnterSafeMode();
            Log.Instance.Info("Avalonia safe-start active: non-critical hardware initialization is skipped.");
        }

        healthGuard.ResetFailureCount();
        return safeStart;
    }

    private static void ApplyRequestedRecovery(AvaloniaStartupOptions options)
    {
        if (!options.ResetHardwareState && !options.ResetNetworkState)
            return;

        var recovery = new HardwareStateRecoveryService();
        if (options.ResetHardwareState)
            recovery.TryResetHardware(out _, options.RestoreProcessorMinState);
        if (options.ResetNetworkState)
            recovery.TryResetNetwork(out _);
    }

    private static async Task LogSoftwareStatusAsync()
    {
        if (!Log.Instance.IsTraceEnabled)
            return;

        var vantage = IoCContainer.TryResolve<VantageDisabler>();
        var legionZone = IoCContainer.TryResolve<LegionZoneDisabler>();
        var fnKeys = IoCContainer.TryResolve<FnKeysDisabler>();
        if (vantage is null || legionZone is null || fnKeys is null)
            return;

        var states = await Task.WhenAll(
            vantage.GetStatusAsync(),
            legionZone.GetStatusAsync(),
            fnKeys.GetStatusAsync()).ConfigureAwait(false);
        Log.Instance.Trace($"Vantage status: {states[0]}");
        Log.Instance.Trace($"LegionZone status: {states[1]}");
        Log.Instance.Trace($"FnKeys status: {states[2]}");
    }

    private static void StartMacroController()
    {
        try
        {
            IoCContainer.TryResolve<MacroController>()?.Start();
        }
        catch (Exception exception)
        {
            Log.Instance.Trace("Avalonia macro controller startup failed.", exception);
        }
    }

    private static void StartSmartKeyHandler()
    {
        try
        {
            AvaloniaSmartKeyHandler.Start();
        }
        catch (Exception exception)
        {
            Log.Instance.Trace("Avalonia smart key handler startup failed.", exception);
        }
    }

    private static async Task StartBackgroundServicesAsync()
    {
        Func<Task>[] steps =
        [
            StartAiControllerAsync,
            StartHwInfoAsync,
            StartIpcServerAsync,
            StartBatteryMonitorAsync,
        ];

        using var gate = new SemaphoreSlim(2, 2);
        var tasks = steps.Select(async step =>
        {
            await gate.WaitAsync().ConfigureAwait(false);
            try
            {
                await RunStepAsync("background service", step).ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static async Task RecoverNetworkStateAsync()
    {
        var network = IoCContainer.TryResolve<INetworkAccelerationService>();
        if (network is not null)
            await network.EnsureCleanSystemStateOnStartupAsync().ConfigureAwait(false);
    }

    private static async Task InitializeLampArrayAsync()
    {
        var controller = IoCContainer.TryResolve<LampArrayController>();
        var settings = IoCContainer.TryResolve<LampArraySettings>();
        if (controller is not null && settings is not null)
            await controller.InitializeAsync(settings).ConfigureAwait(false);
    }

    private static async Task InitializeSensorsAsync()
    {
        var settings = IoCContainer.TryResolve<ApplicationSettings>();
        var controller = IoCContainer.TryResolve<SensorsGroupController>();
        if (settings?.Store.EnableHardwareSensors == true && controller is not null)
            _ = await controller.IsSupportedAsync().ConfigureAwait(false);
    }

    private static async Task InitializePowerModeAsync()
    {
        var feature = IoCContainer.TryResolve<PowerModeFeature>();
        if (feature is null || !await feature.IsSupportedAsync().ConfigureAwait(false))
            return;

        var settings = IoCContainer.TryResolve<GodModeSettings>();
        var recovery = new HardwareStateRecoveryService();
        var isValid = HardwareConfigRangeValidator.TryValidateOrBackupGodMode(
            settings,
            filename => recovery.TryBackupFile(filename, out _),
            message => Log.Instance.Warning(message));
        if (!isValid)
            return;

        await feature.EnsureGodModeStateIsAppliedAsync().ConfigureAwait(false);
        await feature.EnsureCorrectWindowsPowerSettingsAreSetAsync().ConfigureAwait(false);
    }

    private static async Task InitializeItsModeAsync()
    {
        var feature = IoCContainer.TryResolve<ITSModeFeature>();
        if (feature is not null && await feature.IsSupportedAsync().ConfigureAwait(false))
            await feature.SetStateAsync(await feature.GetStateAsync().ConfigureAwait(false)).ConfigureAwait(false);
    }

    private static async Task InitializeBatteryAsync()
    {
        var feature = IoCContainer.TryResolve<BatteryFeature>();
        if (feature is not null && await feature.IsSupportedAsync().ConfigureAwait(false))
            await feature.EnsureCorrectBatteryModeIsSetAsync().ConfigureAwait(false);
    }

    private static async Task InitializeRgbKeyboardAsync()
    {
        var controller = IoCContainer.TryResolve<RGBKeyboardBacklightController>();
        if (controller is not null && await controller.IsSupportedAsync().ConfigureAwait(false))
            await controller.SetLightControlOwnerAsync(true, true).ConfigureAwait(false);
    }

    private static async Task InitializeSpectrumKeyboardAsync()
    {
        var controller = IoCContainer.TryResolve<SpectrumKeyboardBacklightController>();
        if (controller is not null && await controller.IsSupportedAsync().ConfigureAwait(false))
            await controller.StartAuroraIfNeededAsync().ConfigureAwait(false);
    }

    private static async Task InitializeGpuOverclockAsync()
    {
        var controller = IoCContainer.TryResolve<GPUOverclockController>();
        if (controller is null || !await controller.IsSupportedAsync().ConfigureAwait(false))
            return;

        var settings = IoCContainer.TryResolve<GPUOverclockSettings>();
        var recovery = new HardwareStateRecoveryService();
        var isValid = HardwareConfigRangeValidator.TryValidateOrBackupGpuOverclock(
            controller,
            settings,
            GPUOverclockController.GetMaxCoreDeltaMhz(),
            HardwareConfigRangeValidator.DefaultMaxGpuMemoryDeltaMhz,
            filename => recovery.TryBackupFile(filename, out _),
            message => Log.Instance.Warning(message));
        if (isValid)
            await controller.EnsureOverclockIsAppliedAsync().ConfigureAwait(false);
    }

    private static async Task InitializeHybridModeAsync()
    {
        var feature = IoCContainer.TryResolve<HybridModeFeature>();
        if (feature is not null)
            await feature.EnsureDGPUEjectedIfNeededAsync().ConfigureAwait(false);
    }

    private static async Task InitializeFanManagerAsync()
    {
        var manager = IoCContainer.TryResolve<FanCurveManager>();
        var powerMode = IoCContainer.TryResolve<PowerModeFeature>();
        var settings = IoCContainer.TryResolve<FanCurveSettings>();
        if (manager is null || powerMode is null || settings is null || !await manager.IsSupportedAsync().ConfigureAwait(false))
            return;

        await manager.InitializeAsync().ConfigureAwait(false);
        var machine = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
        if (machine.LegionSeries <= LegionSeries.Legion_Legacy
            && await powerMode.GetStateAsync().ConfigureAwait(false) != PowerModeState.GodMode)
            return;

        var recovery = new HardwareStateRecoveryService();
        var isValid = HardwareConfigRangeValidator.TryValidateOrBackupFanCurves(
            settings,
            filename => recovery.TryBackupFile(filename, out _),
            message => Log.Instance.Warning(message));
        if (!isValid)
            return;

        if (settings.Store.Entries.Count == 0)
            await settings.SynchronizeStoreAsync().ConfigureAwait(false);
        await manager.LoadAndApply(settings.Store.Entries).ConfigureAwait(false);
    }

    private static async Task InitializeAmdOverclockingAsync()
    {
        var controller = IoCContainer.TryResolve<AmdOverclockingController>();
        if (controller is null || !controller.IsActive())
            return;

        await controller.InitializeAsync().ConfigureAwait(false);
        if (!controller.DoNotApply)
            await controller.ApplyInternalProfileAsync().ConfigureAwait(false);
    }

    private static async Task StartAiControllerAsync()
    {
        var controller = IoCContainer.TryResolve<AIController>();
        if (controller is not null)
            await controller.StartIfNeededAsync().ConfigureAwait(false);
    }

    private static async Task StartHwInfoAsync()
    {
        var integration = IoCContainer.TryResolve<HWiNFOIntegration>();
        if (integration is not null)
            await integration.StartStopIfNeededAsync().ConfigureAwait(false);
    }

    private static async Task StartIpcServerAsync()
    {
        var server = IoCContainer.TryResolve<IpcServer>();
        if (server is not null)
            await server.StartStopIfNeededAsync().ConfigureAwait(false);
    }

    private static async Task StartBatteryMonitorAsync()
    {
        var monitor = IoCContainer.TryResolve<BatteryDischargeRateMonitorService>();
        if (monitor is not null)
            await monitor.StartStopIfNeededAsync().ConfigureAwait(false);
    }

    private static async Task RunStepAsync(string name, Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Log.Instance.Trace($"Avalonia startup step '{name}' failed.", exception);
        }
    }
}

/// <summary>
/// Avalonia cannot depend on the WPF host's <c>Flags</c> type, but it honors the
/// recovery switches shared by both desktop executables, including args.txt.
/// </summary>
internal sealed class AvaloniaStartupOptions
{
    private const string SafeStartSwitch = "--safe-start";
    private const string ResetHardwareStateSwitch = "--reset-hardware-state";
    private const string ResetNetworkStateSwitch = "--reset-network-state";
    private const string RestoreProcessorMinStateSwitch = "--restore-processor-min-state";

    private AvaloniaStartupOptions(IReadOnlyCollection<string> args)
    {
        SafeStart = args.Contains(SafeStartSwitch, StringComparer.OrdinalIgnoreCase);
        ResetHardwareState = args.Contains(ResetHardwareStateSwitch, StringComparer.OrdinalIgnoreCase);
        ResetNetworkState = args.Contains(ResetNetworkStateSwitch, StringComparer.OrdinalIgnoreCase);
        RestoreProcessorMinState = args.Contains(RestoreProcessorMinStateSwitch, StringComparer.OrdinalIgnoreCase);
    }

    public bool SafeStart { get; }
    public bool ResetHardwareState { get; }
    public bool ResetNetworkState { get; }
    public bool RestoreProcessorMinState { get; }

    public static AvaloniaStartupOptions Load()
    {
        var args = Environment.GetCommandLineArgs().Skip(1).ToList();
        try
        {
            var argsPath = Path.Combine(Folders.AppData, "args.txt");
            if (File.Exists(argsPath))
                args.AddRange(File.ReadAllLines(argsPath));
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            Log.Instance.Trace("Failed to load Avalonia external startup arguments.", exception);
        }

        return new AvaloniaStartupOptions(args);
    }
}

#endif
