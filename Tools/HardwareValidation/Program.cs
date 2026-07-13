using System.Globalization;
using HardwareValidation;
using System.Management;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
using LenovoLegionToolkit.Lib.Controllers.Sensors;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.SoftwareDisabler;
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Utils;

Log.Instance.IsTraceEnabled = true;

var exitCode = await ProgramEntry.RunAsync(args).ConfigureAwait(false);
return exitCode;

static class ProgramEntry
{
    private static readonly TimeSpan PowerModeVerificationTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan PowerModeVerificationPollDelay = TimeSpan.FromMilliseconds(250);

    private static readonly CapabilityID[] DefaultBatchCapabilities =
    [
        CapabilityID.CPULongTermPowerLimit,
        CapabilityID.GPUConfigurableTGP,
        CapabilityID.GPUTemperatureLimit,
    ];

    private enum CapabilityBackend
    {
        CapabilityData,
        LegacyCpuGpuMethods,
    }

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return 0;
        }

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "capabilities" => await PrintCapabilitiesAsync().ConfigureAwait(false),
                "fans" => await RunFansAsync(args.Skip(1).ToArray()).ConfigureAwait(false),
                "power-mode" => await RunPowerModeAsync(args.Skip(1).ToArray()).ConfigureAwait(false),
                "feature" => await RunFeatureAsync(args.Skip(1).ToArray()).ConfigureAwait(false),
                "godmode" => await RunGodModeAsync(args.Skip(1).ToArray()).ConfigureAwait(false),
                _ => Fail($"Unknown command '{args[0]}'.")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }


    private static async Task<int> RunFansAsync(string[] args)
    {
        var samples = ParsePositiveIntArg(args, 0, 8, "samples");
        var delayMs = ParsePositiveIntArg(args, 1, 750, "delay-ms");

        Console.WriteLine($"FanSamples: {samples}");
        Console.WriteLine($"FanDelayMs: {delayMs}");
        NativeFanProbe.Dump();

        for (var fanId = 0; fanId <= 2; fanId++)
        {
            var rpm = await WMI.LenovoFanMethod.FanGetCurrentFanSpeedAsync(fanId).ConfigureAwait(false);
            Console.WriteLine($"RawFanMethod[{fanId}]: {rpm}");
        }

        var fanCount = await WMI.LenovoGameZoneData.TryGetFanCountAsync().ConfigureAwait(false);
        var gameZoneFan1 = await WMI.LenovoGameZoneData.TryGetFan1SpeedAsync().ConfigureAwait(false);
        var gameZoneFan2 = await WMI.LenovoGameZoneData.TryGetFan2SpeedAsync().ConfigureAwait(false);
        var capabilityCpu = await WMI.LenovoOtherMethod.TryGetFeatureValueAsync(CapabilityID.CpuCurrentFanSpeed).ConfigureAwait(false);
        var capabilityGpu = await WMI.LenovoOtherMethod.TryGetFeatureValueAsync(CapabilityID.GpuCurrentFanSpeed).ConfigureAwait(false);
        Console.WriteLine($"RawGameZoneFanCount: Success={fanCount.Success} Value={fanCount.Value}");
        Console.WriteLine($"RawGameZoneFan1: Success={gameZoneFan1.Success} Value={gameZoneFan1.Value}");
        Console.WriteLine($"RawGameZoneFan2: Success={gameZoneFan2.Success} Value={gameZoneFan2.Value}");
        Console.WriteLine($"RawCapabilityCPU: {capabilityCpu}");
        Console.WriteLine($"RawCapabilityGPU: {capabilityGpu}");

        IoCContainer.Initialize(new LenovoLegionToolkit.Lib.IoCModule());
        var sensorsController = IoCContainer.Resolve<SensorsController>();
        Console.WriteLine($"SensorsSupported: {await sensorsController.IsSupportedAsync().ConfigureAwait(false)}");
        await sensorsController.PrepareAsync().ConfigureAwait(false);

        var validCpuSamples = 0;
        var validGpuSamples = 0;
        for (var sample = 1; sample <= samples; sample++)
        {
            var (cpuFanSpeed, gpuFanSpeed) = await sensorsController.GetFanSpeedsAsync().ConfigureAwait(false);
            var data = await sensorsController.GetDataAsync().ConfigureAwait(false);
            if (cpuFanSpeed >= 0)
                validCpuSamples++;
            if (gpuFanSpeed >= 0)
                validGpuSamples++;

            Console.WriteLine(
                $"Sample[{sample}]: CPU={cpuFanSpeed} GPU={gpuFanSpeed} " +
                $"SnapshotCPU={data.CPU.FanSpeed} SnapshotGPU={data.GPU.FanSpeed}");

            if (sample < samples)
                await Task.Delay(delayMs).ConfigureAwait(false);
        }

        Console.WriteLine($"ValidCpuSamples: {validCpuSamples}/{samples}");
        Console.WriteLine($"ValidGpuSamples: {validGpuSamples}/{samples}");
        return validCpuSamples > 0 ? 0 : 2;
    }

    private static int ParsePositiveIntArg(string[] args, int index, int fallback, string name)
    {
        if (index >= args.Length)
            return fallback;

        if (!int.TryParse(args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
            throw new InvalidOperationException($"{name} must be a positive integer.");

        return value;
    }

    private static async Task<int> PrintCapabilitiesAsync()
    {
        var machineInformation = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

        Console.WriteLine($"Machine: {machineInformation.Vendor} {machineInformation.Model} ({machineInformation.MachineType})");
        Console.WriteLine($"SupportedPowerModes: {string.Join(", ", machineInformation.SupportedPowerModes ?? [])}");
        Console.WriteLine($"SupportsGodMode: {machineInformation.Properties.SupportsGodMode}");
        Console.WriteLine($"SupportsGodModeV1: {machineInformation.Properties.SupportsGodModeV1}");
        Console.WriteLine($"SupportsGodModeV2: {machineInformation.Properties.SupportsGodModeV2}");
        Console.WriteLine($"SupportsGodModeV3: {machineInformation.Properties.SupportsGodModeV3}");
        Console.WriteLine($"SupportsGodModeV4: {machineInformation.Properties.SupportsGodModeV4}");
        Console.WriteLine($"FeatureSource: {machineInformation.Features.Source}");
        Console.WriteLine($"Capabilities: {string.Join(", ", machineInformation.Features.All)}");
        return 0;
    }

    private static async Task<int> RunPowerModeAsync(string[] args)
    {
        if (args.Length == 0)
            return Fail("Missing power-mode subcommand.");

        switch (args[0].ToLowerInvariant())
        {
            case "get":
                Console.WriteLine(await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false));
                return 0;
            case "set":
                if (args.Length < 2 || !int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mode))
                    return Fail("power-mode set requires an integer mode value.");

                await WMI.LenovoGameZoneData.SetSmartFanModeAsync(mode).ConfigureAwait(false);
                Console.WriteLine($"SetSmartFanMode: {mode}");
                return 0;
            case "set-verify":
                if (args.Length < 2 || !int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var verifiedMode))
                    return Fail("power-mode set-verify requires an integer mode value.");

                var restore = !args.Skip(2).Any(static arg => arg.Equals("--no-restore", StringComparison.OrdinalIgnoreCase));
                return await SetAndVerifyPowerModeAsync(verifiedMode, restore).ConfigureAwait(false);
            default:
                return Fail($"Unknown power-mode subcommand '{args[0]}'.");
        }
    }

    private static async Task<int> SetAndVerifyPowerModeAsync(int requestedMode, bool restore)
    {
        var beforeMode = await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false);
        var requestedChange = beforeMode != requestedMode;

        Console.WriteLine($"BeforeSmartFanMode: {beforeMode}");
        Console.WriteLine($"RequestedSmartFanMode: {requestedMode}");
        Console.WriteLine($"RestoreRequested: {restore}");
        Console.WriteLine($"PowerModeChangeRequested: {requestedChange}");

        var verificationPassed = false;
        var restorePassed = !restore;
        var afterMode = beforeMode;
        var measuredChangeObserved = false;

        try
        {
            await WMI.LenovoGameZoneData.SetSmartFanModeAsync(requestedMode).ConfigureAwait(false);
            afterMode = await WaitForSmartFanModeAsync(requestedMode, PowerModeVerificationTimeout).ConfigureAwait(false);
            verificationPassed = afterMode == requestedMode;
            measuredChangeObserved = beforeMode != afterMode;

            Console.WriteLine($"AfterSmartFanMode: {afterMode}");
            Console.WriteLine($"PowerModeDelta: {afterMode - beforeMode}");
            Console.WriteLine($"MeasuredPowerModeChangeObserved: {measuredChangeObserved}");
            Console.WriteLine($"PowerModeVerificationPassed: {verificationPassed}");
        }
        finally
        {
            if (restore)
            {
                await WMI.LenovoGameZoneData.SetSmartFanModeAsync(beforeMode).ConfigureAwait(false);
                var restoredMode = await WaitForSmartFanModeAsync(beforeMode, PowerModeVerificationTimeout).ConfigureAwait(false);
                restorePassed = restoredMode == beforeMode;
                Console.WriteLine($"RestoredSmartFanMode: {restoredMode}");
                Console.WriteLine($"RestoreVerificationPassed: {restorePassed}");
            }
        }

        var overallPassed = verificationPassed && (!requestedChange || measuredChangeObserved) && restorePassed;
        Console.WriteLine($"OverallPassed: {overallPassed}");
        return overallPassed ? 0 : 1;
    }

    private static async Task<int> WaitForSmartFanModeAsync(int expectedMode, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        var lastMode = await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false);

        while (lastMode != expectedMode && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(PowerModeVerificationPollDelay).ConfigureAwait(false);
            lastMode = await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false);
        }

        return lastMode;
    }

    private static async Task<int> RunFeatureAsync(string[] args)
    {
        if (args.Length < 2)
            return Fail("feature requires 'get <CapabilityID>' or 'set <CapabilityID> <value>'.");

        if (!Enum.TryParse<CapabilityID>(args[1], ignoreCase: true, out var capabilityId))
            return Fail($"Unknown CapabilityID '{args[1]}'.");

        switch (args[0].ToLowerInvariant())
        {
            case "get":
                Console.WriteLine(await ReadCapabilityValueAsync(capabilityId).ConfigureAwait(false));
                return 0;
            case "set":
                if (args.Length < 3 || !int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    return Fail("feature set requires an integer value.");

                await WriteCapabilityValueAsync(capabilityId, value).ConfigureAwait(false);
                Console.WriteLine($"Set {capabilityId}: {value}");
                return 0;
            default:
                return Fail($"Unknown feature subcommand '{args[0]}'.");
        }
    }

    private static async Task<int> RunGodModeAsync(string[] args)
    {
        if (args.Length == 0)
            return Fail("Missing godmode subcommand.");

        var controller = CreateGodModeController();

        switch (args[0].ToLowerInvariant())
        {
            case "status":
                return await PrintGodModeStatusAsync(controller).ConfigureAwait(false);
            case "verify-current-preset":
                if (args.Length < 3)
                    return Fail("godmode verify-current-preset requires <CapabilityID> <delta>.");

                if (!Enum.TryParse<CapabilityID>(args[1], ignoreCase: true, out var capabilityId))
                    return Fail($"Unknown CapabilityID '{args[1]}'.");

                if (!int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var delta))
                    return Fail($"Invalid delta '{args[2]}'.");

                return await VerifyCurrentPresetAsync(controller, capabilityId, delta).ConfigureAwait(false);
            case "verify-current-preset-batch":
                return await VerifyCurrentPresetBatchAsync(controller, args.Skip(1).ToArray()).ConfigureAwait(false);
            default:
                return Fail($"Unknown godmode subcommand '{args[0]}'.");
        }
    }

    private static async Task<int> PrintGodModeStatusAsync(IGodModeController controller)
    {
        var state = await controller.GetStateAsync().ConfigureAwait(false);
        var activePresetId = await controller.GetActivePresetIdAsync().ConfigureAwait(false);
        var activePresetName = await controller.GetActivePresetNameAsync().ConfigureAwait(false);

        Console.WriteLine($"ActivePresetId: {activePresetId}");
        Console.WriteLine($"ActivePresetName: {activePresetName ?? string.Empty}");
        Console.WriteLine($"PresetCount: {state.Presets.Count}");
        Console.WriteLine($"CurrentPowerMode: {await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false)}");
        return 0;
    }

    private static async Task<int> VerifyCurrentPresetAsync(IGodModeController controller, CapabilityID capabilityId, int delta)
    {
        var originalState = await controller.GetStateAsync().ConfigureAwait(false);
        var originalPowerMode = await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false);
        var activePresetId = originalState.ActivePresetId;

        if (!originalState.Presets.TryGetValue(activePresetId, out var activePreset))
            return Fail($"Active preset '{activePresetId}' was not found.");

        var originalStepper = GetCapabilityStepper(activePreset, capabilityId);
        if (originalStepper is null)
            return Fail($"Active preset does not contain configurable value for {capabilityId}.");

        var beforeHardwareValue = await ReadCapabilityValueAsync(capabilityId).ConfigureAwait(false);
        if (GetMeasurableStepperValue(originalStepper.Value, delta, beforeHardwareValue) is not { } targetValue)
            return Fail($"Could not compute a measurable alternate value for {capabilityId}. Current hardware value is {beforeHardwareValue}.");

        var updatedPreset = SetCapabilityStepper(activePreset, capabilityId, originalStepper.Value.WithValue(targetValue));

        var presets = new Dictionary<Guid, GodModePreset>(originalState.Presets)
        {
            [activePresetId] = updatedPreset
        };

        var updatedState = originalState with
        {
            Presets = presets.AsReadOnlyDictionary()
        };

        var verificationPassed = false;

        try
        {
            await controller.SetStateAsync(updatedState).ConfigureAwait(false);
            await WMI.LenovoGameZoneData.SetSmartFanModeAsync((int)PowerModeState.GodMode).ConfigureAwait(false);
            await controller.ApplyStateAsync().ConfigureAwait(false);

            var persistedState = await controller.GetStateAsync().ConfigureAwait(false);
            var persistedPreset = persistedState.Presets[persistedState.ActivePresetId];
            var persistedStepper = GetCapabilityStepper(persistedPreset, capabilityId);
            var afterHardwareValue = await ReadCapabilityValueAsync(capabilityId).ConfigureAwait(false);
            var afterPowerMode = await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false);
            var requestedHardwareDelta = targetValue - beforeHardwareValue;
            var hardwareValueDelta = afterHardwareValue - beforeHardwareValue;
            var hardwareValueChanged = afterHardwareValue != beforeHardwareValue;
            var measuredVerificationPassed = afterHardwareValue == targetValue && hardwareValueChanged;
            var powerModeVerificationPassed = afterPowerMode == (int)PowerModeState.GodMode;

            Console.WriteLine($"Capability: {capabilityId}");
            Console.WriteLine($"OriginalPresetValue: {originalStepper.Value.Value}");
            Console.WriteLine($"BeforeHardwareValue: {beforeHardwareValue}");
            Console.WriteLine($"RequestedPresetValue: {targetValue}");
            Console.WriteLine($"RequestedHardwareDelta: {requestedHardwareDelta}");
            Console.WriteLine($"PersistedPresetValue: {persistedStepper?.Value ?? -1}");
            Console.WriteLine($"AfterHardwareValue: {afterHardwareValue}");
            Console.WriteLine($"HardwareValueDelta: {hardwareValueDelta}");
            Console.WriteLine($"HardwareValueChanged: {hardwareValueChanged}");
            Console.WriteLine($"AfterSmartFanMode: {afterPowerMode}");
            Console.WriteLine($"PowerModeVerificationPassed: {powerModeVerificationPassed}");
            Console.WriteLine($"PersistedVerificationPassed: {persistedStepper?.Value == targetValue}");
            Console.WriteLine($"HardwareVerificationPassed: {afterHardwareValue == targetValue}");
            Console.WriteLine($"MeasuredVerificationPassed: {measuredVerificationPassed}");

            verificationPassed =
                persistedStepper?.Value == targetValue &&
                measuredVerificationPassed &&
                powerModeVerificationPassed;

            return verificationPassed
                ? 0
                : 2;
        }
        finally
        {
            await controller.SetStateAsync(originalState).ConfigureAwait(false);
            await WMI.LenovoGameZoneData.SetSmartFanModeAsync(originalPowerMode).ConfigureAwait(false);

            if (originalPowerMode == (int)PowerModeState.GodMode)
                await controller.ApplyStateAsync().ConfigureAwait(false);

            await WriteCapabilityValueAsync(capabilityId, beforeHardwareValue).ConfigureAwait(false);

            var restoredState = await controller.GetStateAsync().ConfigureAwait(false);
            var restoredPreset = restoredState.Presets[restoredState.ActivePresetId];
            var restoredStepper = GetCapabilityStepper(restoredPreset, capabilityId);
            var restoredHardwareValue = await ReadCapabilityValueAsync(capabilityId).ConfigureAwait(false);
            var restoredPowerMode = await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false);
            var restoredHardwareDeltaFromBefore = restoredHardwareValue - beforeHardwareValue;
            var restorePassed =
                restoredStepper?.Value == originalStepper.Value.Value &&
                restoredPowerMode == originalPowerMode &&
                (!verificationPassed || restoredHardwareValue == beforeHardwareValue);

            Console.WriteLine($"RestoredPresetValue: {restoredStepper?.Value ?? -1}");
            Console.WriteLine($"RestoredHardwareValue: {restoredHardwareValue}");
            Console.WriteLine($"RestoredHardwareDeltaFromBefore: {restoredHardwareDeltaFromBefore}");
            Console.WriteLine($"RestoredSmartFanMode: {restoredPowerMode}");
            Console.WriteLine($"RestoreVerificationPassed: {restorePassed}");
        }
    }

    private static async Task<int> VerifyCurrentPresetBatchAsync(IGodModeController controller, string[] capabilityNames)
    {
        var originalState = await controller.GetStateAsync().ConfigureAwait(false);
        var originalPowerMode = await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false);
        var activePresetId = originalState.ActivePresetId;

        if (!originalState.Presets.TryGetValue(activePresetId, out var activePreset))
            return Fail($"Active preset '{activePresetId}' was not found.");

        var capabilities = ResolveBatchCapabilities(capabilityNames, activePreset);
        if (capabilities.Count == 0)
            return Fail("No configurable capabilities were available for batch verification.");

        var plans = new List<BatchVerificationPlan>();
        var updatedPreset = activePreset;

        foreach (var capabilityId in capabilities)
        {
            var originalStepper = GetCapabilityStepper(activePreset, capabilityId);
            if (originalStepper is null)
                return Fail($"Active preset does not contain configurable value for {capabilityId}.");

            var beforeHardwareValue = await ReadCapabilityValueAsync(capabilityId).ConfigureAwait(false);
            var targetValue = GetAlternateStepperValue(originalStepper.Value, beforeHardwareValue);
            if (targetValue is null)
                return Fail($"Could not compute an alternate verification value for {capabilityId}. Current hardware value is {beforeHardwareValue}.");

            plans.Add(new BatchVerificationPlan(capabilityId, originalStepper.Value, targetValue.Value, beforeHardwareValue));
            updatedPreset = SetCapabilityStepper(updatedPreset, capabilityId, originalStepper.Value.WithValue(targetValue.Value));
        }

        var presets = new Dictionary<Guid, GodModePreset>(originalState.Presets)
        {
            [activePresetId] = updatedPreset
        };

        var updatedState = originalState with
        {
            Presets = presets.AsReadOnlyDictionary()
        };

        var verificationPassed = false;

        try
        {
            await controller.SetStateAsync(updatedState).ConfigureAwait(false);
            await WMI.LenovoGameZoneData.SetSmartFanModeAsync((int)PowerModeState.GodMode).ConfigureAwait(false);
            await controller.ApplyStateAsync().ConfigureAwait(false);

            var persistedState = await controller.GetStateAsync().ConfigureAwait(false);
            var persistedPreset = persistedState.Presets[persistedState.ActivePresetId];
            var afterPowerMode = await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false);
            var passedCount = 0;
            var changedCount = 0;
            var measuredDeltas = new List<string>();

            Console.WriteLine($"BatchCapabilities: {string.Join(", ", plans.Select(plan => plan.CapabilityId))}");
            Console.WriteLine($"BatchCapabilityCount: {plans.Count}");
            Console.WriteLine($"BatchAfterSmartFanMode: {afterPowerMode}");

            foreach (var plan in plans)
            {
                var persistedStepper = GetCapabilityStepper(persistedPreset, plan.CapabilityId);
                var afterHardwareValue = await ReadCapabilityValueAsync(plan.CapabilityId).ConfigureAwait(false);
                var requestedHardwareDelta = plan.TargetValue - plan.BeforeHardwareValue;
                var hardwareValueDelta = afterHardwareValue - plan.BeforeHardwareValue;
                var persistedPassed = persistedStepper?.Value == plan.TargetValue;
                var hardwarePassed = afterHardwareValue == plan.TargetValue;
                var hardwareValueChanged = afterHardwareValue != plan.BeforeHardwareValue;
                var measuredPassed = hardwarePassed && hardwareValueChanged;

                if (hardwareValueChanged)
                    changedCount++;

                measuredDeltas.Add($"{plan.CapabilityId}={hardwareValueDelta}");

                if (persistedPassed && measuredPassed)
                    passedCount++;

                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].OriginalPresetValue: {plan.OriginalStepper.Value}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].BeforeHardwareValue: {plan.BeforeHardwareValue}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].RequestedPresetValue: {plan.TargetValue}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].RequestedHardwareDelta: {requestedHardwareDelta}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].PersistedPresetValue: {persistedStepper?.Value ?? -1}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].AfterHardwareValue: {afterHardwareValue}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].HardwareValueDelta: {hardwareValueDelta}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].HardwareValueChanged: {hardwareValueChanged}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].PersistedVerificationPassed: {persistedPassed}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].HardwareVerificationPassed: {hardwarePassed}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].MeasuredVerificationPassed: {measuredPassed}");
            }

            var powerModeObservedGodMode = afterPowerMode == (int)PowerModeState.GodMode;
            verificationPassed = passedCount == plans.Count && powerModeObservedGodMode;

            Console.WriteLine($"BatchPassedCount: {passedCount}");
            Console.WriteLine($"BatchMeasuredChangedCount: {changedCount}");
            Console.WriteLine($"BatchMeasuredDeltas: {string.Join(", ", measuredDeltas)}");
            Console.WriteLine($"BatchMeasuredChangeObserved: {changedCount > 0}");
            Console.WriteLine($"BatchPowerModeObservedGodMode: {powerModeObservedGodMode}");
            Console.WriteLine($"BatchVerificationPassed: {verificationPassed}");

            return verificationPassed
                ? 0
                : 2;
        }
        finally
        {
            await controller.SetStateAsync(originalState).ConfigureAwait(false);
            await WMI.LenovoGameZoneData.SetSmartFanModeAsync(originalPowerMode).ConfigureAwait(false);

            if (originalPowerMode == (int)PowerModeState.GodMode)
                await controller.ApplyStateAsync().ConfigureAwait(false);

            foreach (var plan in plans)
                await WriteCapabilityValueAsync(plan.CapabilityId, plan.BeforeHardwareValue).ConfigureAwait(false);

            var restoredState = await controller.GetStateAsync().ConfigureAwait(false);
            var restoredPreset = restoredState.Presets[restoredState.ActivePresetId];
            var restoredPowerMode = await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false);
            var restorePassed = restoredPowerMode == originalPowerMode;

            Console.WriteLine($"BatchRestoredSmartFanMode: {restoredPowerMode}");

            foreach (var plan in plans)
            {
                var restoredStepper = GetCapabilityStepper(restoredPreset, plan.CapabilityId);
                var restoredHardwareValue = await ReadCapabilityValueAsync(plan.CapabilityId).ConfigureAwait(false);
                var restoredHardwareDeltaFromBefore = restoredHardwareValue - plan.BeforeHardwareValue;
                var capabilityRestorePassed =
                    restoredStepper?.Value == plan.OriginalStepper.Value &&
                    (!verificationPassed || restoredHardwareValue == plan.BeforeHardwareValue);

                restorePassed &= capabilityRestorePassed;

                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].RestoredPresetValue: {restoredStepper?.Value ?? -1}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].RestoredHardwareValue: {restoredHardwareValue}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].RestoredHardwareDeltaFromBefore: {restoredHardwareDeltaFromBefore}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].RestoreVerificationPassed: {capabilityRestorePassed}");
            }

            Console.WriteLine($"BatchRestoreVerificationPassed: {restorePassed}");
        }
    }

    private static IGodModeController CreateGodModeController()
    {
        var settings = new GodModeSettings();
        var legionZoneDisabler = new LegionZoneDisabler();
        var vantageDisabler = new VantageDisabler();
        var controllerV1 = new GodModeControllerV1(settings, legionZoneDisabler);
        var controllerV2 = new GodModeControllerV2(settings, vantageDisabler, legionZoneDisabler);
        return new GodModeController(controllerV1, controllerV2);
    }

    private static async Task<int> ReadCapabilityValueAsync(CapabilityID capabilityId)
    {
        var backend = await ResolveCapabilityBackendAsync().ConfigureAwait(false);

        if (backend == CapabilityBackend.CapabilityData)
            return await ReadCapabilityValueFromCapabilityDataAsync(capabilityId).ConfigureAwait(false);

        return capabilityId switch
        {
            CapabilityID.CPULongTermPowerLimit => (await WMI.LenovoCpuMethod.CPUGetLongTermPowerLimitAsync().ConfigureAwait(false)).value,
            CapabilityID.CPUShortTermPowerLimit => (await WMI.LenovoCpuMethod.CPUGetShortTermPowerLimitAsync().ConfigureAwait(false)).value,
            CapabilityID.CPUPeakPowerLimit => (await WMI.LenovoCpuMethod.CPUGetPeakPowerLimitAsync().ConfigureAwait(false)).value,
            CapabilityID.CPUCrossLoadingPowerLimit => (await WMI.LenovoCpuMethod.CPUGetCrossLoadingPowerLimitAsync().ConfigureAwait(false)).value,
            CapabilityID.APUsPPTPowerLimit => (await WMI.LenovoCpuMethod.GetAPUSPPTPowerLimitAsync().ConfigureAwait(false)).value,
            CapabilityID.CPUTemperatureLimit => (await WMI.LenovoCpuMethod.CPUGetTemperatureControlAsync().ConfigureAwait(false)).value,
            CapabilityID.GPUConfigurableTGP => (await WMI.LenovoGpuMethod.GPUGetCTGPPowerLimitAsync().ConfigureAwait(false)).value,
            CapabilityID.GPUPowerBoost => (await WMI.LenovoGpuMethod.GPUGetPPABPowerLimitAsync().ConfigureAwait(false)).value,
            CapabilityID.GPUTemperatureLimit => (await WMI.LenovoGpuMethod.GPUGetTemperatureLimitAsync().ConfigureAwait(false)).value,
            _ => await WMI.LenovoOtherMethod.GetFeatureValueAsync(capabilityId).ConfigureAwait(false)
        };
    }

    private static async Task WriteCapabilityValueAsync(CapabilityID capabilityId, int value)
    {
        var backend = await ResolveCapabilityBackendAsync().ConfigureAwait(false);

        if (backend == CapabilityBackend.CapabilityData)
        {
            await WriteCapabilityValueFromCapabilityDataAsync(capabilityId, value).ConfigureAwait(false);
            return;
        }

        switch (capabilityId)
        {
            case CapabilityID.CPULongTermPowerLimit:
                await WMI.LenovoCpuMethod.CPUSetLongTermPowerLimitAsync(value).ConfigureAwait(false);
                return;
            case CapabilityID.CPUShortTermPowerLimit:
                await WMI.LenovoCpuMethod.CPUSetShortTermPowerLimitAsync(value).ConfigureAwait(false);
                return;
            case CapabilityID.CPUPeakPowerLimit:
                await WMI.LenovoCpuMethod.CPUSetPeakPowerLimitAsync(value).ConfigureAwait(false);
                return;
            case CapabilityID.CPUCrossLoadingPowerLimit:
                await WMI.LenovoCpuMethod.CPUSetCrossLoadingPowerLimitAsync(value).ConfigureAwait(false);
                return;
            case CapabilityID.APUsPPTPowerLimit:
                await WMI.LenovoCpuMethod.SetAPUSPPTPowerLimitAsync(value).ConfigureAwait(false);
                return;
            case CapabilityID.CPUTemperatureLimit:
                await WMI.LenovoCpuMethod.CPUSetTemperatureControlAsync(value).ConfigureAwait(false);
                return;
            case CapabilityID.GPUConfigurableTGP:
                await WMI.LenovoGpuMethod.GPUSetCTGPPowerLimitAsync(value).ConfigureAwait(false);
                return;
            case CapabilityID.GPUPowerBoost:
                await WMI.LenovoGpuMethod.GPUSetPPABPowerLimitAsync(value).ConfigureAwait(false);
                return;
            case CapabilityID.GPUTemperatureLimit:
                await WMI.LenovoGpuMethod.GPUSetTemperatureLimitAsync(value).ConfigureAwait(false);
                return;
            default:
                await WMI.LenovoOtherMethod.SetFeatureValueAsync(capabilityId, value).ConfigureAwait(false);
                return;
        }
    }

    private static async Task<CapabilityBackend> ResolveCapabilityBackendAsync()
    {
        var machineInformation = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
        return machineInformation.Properties.SupportsGodModeV1
            ? CapabilityBackend.LegacyCpuGpuMethods
            : CapabilityBackend.CapabilityData;
    }

    private static Task<int> ReadCapabilityValueFromCapabilityDataAsync(CapabilityID capabilityId)
    {
        var idRaw = (uint)capabilityId & 0xFFFF00FF;
        return WMI.LenovoOtherMethod.GetFeatureValueAsync(idRaw);
    }

    private static Task WriteCapabilityValueFromCapabilityDataAsync(CapabilityID capabilityId, int value)
    {
        var idRaw = (uint)capabilityId & 0xFFFF00FF;
        return WMI.LenovoOtherMethod.SetFeatureValueAsync(idRaw, value);
    }

    private static StepperValue? GetCapabilityStepper(GodModePreset preset, CapabilityID capabilityId)
    {
        return capabilityId switch
        {
            CapabilityID.CPULongTermPowerLimit => preset.CPULongTermPowerLimit,
            CapabilityID.CPUShortTermPowerLimit => preset.CPUShortTermPowerLimit,
            CapabilityID.CPUPeakPowerLimit => preset.CPUPeakPowerLimit,
            CapabilityID.CPUCrossLoadingPowerLimit => preset.CPUCrossLoadingPowerLimit,
            CapabilityID.APUsPPTPowerLimit => preset.APUsPPTPowerLimit,
            CapabilityID.CPUTemperatureLimit => preset.CPUTemperatureLimit,
            CapabilityID.GPUPowerBoost => preset.GPUPowerBoost,
            CapabilityID.GPUConfigurableTGP => preset.GPUConfigurableTGP,
            CapabilityID.GPUTemperatureLimit => preset.GPUTemperatureLimit,
            CapabilityID.GPUTotalProcessingPowerTargetOnAcOffsetFromBaseline => preset.GPUTotalProcessingPowerTargetOnAcOffsetFromBaseline,
            CapabilityID.GPUToCPUDynamicBoost => preset.GPUToCPUDynamicBoost,
            _ => null
        };
    }

    private static GodModePreset SetCapabilityStepper(GodModePreset preset, CapabilityID capabilityId, StepperValue value)
    {
        return capabilityId switch
        {
            CapabilityID.CPULongTermPowerLimit => preset with { CPULongTermPowerLimit = value },
            CapabilityID.CPUShortTermPowerLimit => preset with { CPUShortTermPowerLimit = value },
            CapabilityID.CPUPeakPowerLimit => preset with { CPUPeakPowerLimit = value },
            CapabilityID.CPUCrossLoadingPowerLimit => preset with { CPUCrossLoadingPowerLimit = value },
            CapabilityID.APUsPPTPowerLimit => preset with { APUsPPTPowerLimit = value },
            CapabilityID.CPUTemperatureLimit => preset with { CPUTemperatureLimit = value },
            CapabilityID.GPUPowerBoost => preset with { GPUPowerBoost = value },
            CapabilityID.GPUConfigurableTGP => preset with { GPUConfigurableTGP = value },
            CapabilityID.GPUTemperatureLimit => preset with { GPUTemperatureLimit = value },
            CapabilityID.GPUTotalProcessingPowerTargetOnAcOffsetFromBaseline => preset with { GPUTotalProcessingPowerTargetOnAcOffsetFromBaseline = value },
            CapabilityID.GPUToCPUDynamicBoost => preset with { GPUToCPUDynamicBoost = value },
            _ => preset
        };
    }

    private static IReadOnlyList<CapabilityID> ResolveBatchCapabilities(string[] capabilityNames, GodModePreset preset)
    {
        if (capabilityNames.Length == 0)
        {
            return DefaultBatchCapabilities
                .Where(capabilityId => GetCapabilityStepper(preset, capabilityId) is StepperValue stepper && GetAlternateStepperValue(stepper) is not null)
                .ToArray();
        }

        var capabilities = new List<CapabilityID>();
        foreach (var capabilityName in capabilityNames)
        {
            if (!Enum.TryParse<CapabilityID>(capabilityName, ignoreCase: true, out var capabilityId))
                throw new InvalidOperationException($"Unknown CapabilityID '{capabilityName}'.");

            capabilities.Add(capabilityId);
        }

        return capabilities;
    }

    private static int? GetMeasurableStepperValue(StepperValue value, int delta, int beforeHardwareValue)
    {
        var requestedTarget = ClampToStepper(value, value.Value + delta);
        if (requestedTarget != value.Value && requestedTarget != beforeHardwareValue)
            return requestedTarget;

        return GetAlternateStepperValue(value, beforeHardwareValue);
    }

    private static int? GetAlternateStepperValue(StepperValue value, int? excludedHardwareValue = null)
    {
        return EnumerateStepperValues(value)
            .Where(candidate => candidate != value.Value)
            .Where(candidate => !excludedHardwareValue.HasValue || candidate != excludedHardwareValue.Value)
            .OrderBy(candidate => Math.Abs(candidate - value.Value))
            .ThenBy(candidate => candidate)
            .Cast<int?>()
            .FirstOrDefault();
    }

    private static IEnumerable<int> EnumerateStepperValues(StepperValue value)
    {
        if (value.Steps.Length > 0)
            return value.Steps.Distinct();

        var step = value.Step > 0 ? value.Step : 1;
        var values = new List<int>();
        for (var candidate = value.Min; candidate <= value.Max; candidate += step)
            values.Add(candidate);

        return values;
    }

    private static int ClampToStepper(StepperValue value, int targetValue)
    {
        var clamped = Math.Clamp(targetValue, value.Min, value.Max);
        if (value.Steps.Length > 0)
        {
            return value.Steps
                .OrderBy(step => Math.Abs(step - clamped))
                .ThenBy(step => step)
                .First();
        }

        if (value.Step <= 0)
            return clamped;

        var offset = clamped - value.Min;
        var stepped = value.Min + (int)Math.Round(offset / (double)value.Step, MidpointRounding.AwayFromZero) * value.Step;
        return Math.Clamp(stepped, value.Min, value.Max);
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        PrintUsage();
        return 1;
    }

    private static bool IsHelp(string argument) =>
        argument is "-h" or "--help" or "/?";

    private static void PrintUsage()
    {
        Console.WriteLine("HardwareValidation");
        Console.WriteLine("  capabilities");
        Console.WriteLine("  fans [samples] [delay-ms]");
        Console.WriteLine("  power-mode get");
        Console.WriteLine("  power-mode set <int>");
        Console.WriteLine("  power-mode set-verify <int> [--no-restore]");
        Console.WriteLine("  feature get <CapabilityID>");
        Console.WriteLine("  feature set <CapabilityID> <int>");
        Console.WriteLine("  godmode status");
        Console.WriteLine("  godmode verify-current-preset <CapabilityID> <delta>");
        Console.WriteLine("  godmode verify-current-preset-batch [<CapabilityID> ...]");
    }

    private readonly record struct BatchVerificationPlan(
        CapabilityID CapabilityId,
        StepperValue OriginalStepper,
        int TargetValue,
        int BeforeHardwareValue);
}
