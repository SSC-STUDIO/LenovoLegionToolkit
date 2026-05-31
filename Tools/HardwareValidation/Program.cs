using System.Globalization;
using System.Management;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
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
                "power-mode" => await RunPowerModeAsync(args.Skip(1).ToArray()).ConfigureAwait(false),
                "feature" => await RunFeatureAsync(args.Skip(1).ToArray()).ConfigureAwait(false),
                "godmode" => await RunGodModeAsync(args.Skip(1).ToArray()).ConfigureAwait(false),
                _ => Fail($"Unknown command '{args[0]}'.")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
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
            default:
                return Fail($"Unknown power-mode subcommand '{args[0]}'.");
        }
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

        var targetValue = ClampToStepper(originalStepper.Value, originalStepper.Value.Value + delta);
        var updatedPreset = SetCapabilityStepper(activePreset, capabilityId, originalStepper.Value.WithValue(targetValue));

        var presets = new Dictionary<Guid, GodModePreset>(originalState.Presets)
        {
            [activePresetId] = updatedPreset
        };

        var updatedState = originalState with
        {
            Presets = presets.AsReadOnlyDictionary()
        };

        var beforeHardwareValue = await ReadCapabilityValueAsync(capabilityId).ConfigureAwait(false);
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

            Console.WriteLine($"Capability: {capabilityId}");
            Console.WriteLine($"OriginalPresetValue: {originalStepper.Value.Value}");
            Console.WriteLine($"BeforeHardwareValue: {beforeHardwareValue}");
            Console.WriteLine($"RequestedPresetValue: {targetValue}");
            Console.WriteLine($"PersistedPresetValue: {persistedStepper?.Value ?? -1}");
            Console.WriteLine($"AfterHardwareValue: {afterHardwareValue}");
            Console.WriteLine($"AfterSmartFanMode: {afterPowerMode}");
            Console.WriteLine($"PersistedVerificationPassed: {persistedStepper?.Value == targetValue}");
            Console.WriteLine($"HardwareVerificationPassed: {afterHardwareValue == targetValue}");

            verificationPassed = persistedStepper?.Value == targetValue && afterHardwareValue == targetValue;

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

            var restoredState = await controller.GetStateAsync().ConfigureAwait(false);
            var restoredPreset = restoredState.Presets[restoredState.ActivePresetId];
            var restoredStepper = GetCapabilityStepper(restoredPreset, capabilityId);
            var restoredHardwareValue = await ReadCapabilityValueAsync(capabilityId).ConfigureAwait(false);
            var restoredPowerMode = await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false);
            var restorePassed =
                restoredStepper?.Value == originalStepper.Value.Value &&
                restoredPowerMode == originalPowerMode &&
                (!verificationPassed || restoredHardwareValue == beforeHardwareValue);

            Console.WriteLine($"RestoredPresetValue: {restoredStepper?.Value ?? -1}");
            Console.WriteLine($"RestoredHardwareValue: {restoredHardwareValue}");
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

            var targetValue = GetAlternateStepperValue(originalStepper.Value);
            if (targetValue is null)
                return Fail($"Could not compute an alternate verification value for {capabilityId}.");

            var beforeHardwareValue = await ReadCapabilityValueAsync(capabilityId).ConfigureAwait(false);
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

            Console.WriteLine($"BatchCapabilities: {string.Join(", ", plans.Select(plan => plan.CapabilityId))}");
            Console.WriteLine($"BatchCapabilityCount: {plans.Count}");
            Console.WriteLine($"BatchAfterSmartFanMode: {afterPowerMode}");

            foreach (var plan in plans)
            {
                var persistedStepper = GetCapabilityStepper(persistedPreset, plan.CapabilityId);
                var afterHardwareValue = await ReadCapabilityValueAsync(plan.CapabilityId).ConfigureAwait(false);
                var persistedPassed = persistedStepper?.Value == plan.TargetValue;
                var hardwarePassed = afterHardwareValue == plan.TargetValue;

                if (persistedPassed && hardwarePassed)
                    passedCount++;

                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].OriginalPresetValue: {plan.OriginalStepper.Value}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].BeforeHardwareValue: {plan.BeforeHardwareValue}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].RequestedPresetValue: {plan.TargetValue}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].PersistedPresetValue: {persistedStepper?.Value ?? -1}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].AfterHardwareValue: {afterHardwareValue}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].PersistedVerificationPassed: {persistedPassed}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].HardwareVerificationPassed: {hardwarePassed}");
            }

            var powerModeObservedGodMode = afterPowerMode == (int)PowerModeState.GodMode;
            verificationPassed = passedCount == plans.Count;

            Console.WriteLine($"BatchPassedCount: {passedCount}");
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

            var restoredState = await controller.GetStateAsync().ConfigureAwait(false);
            var restoredPreset = restoredState.Presets[restoredState.ActivePresetId];
            var restoredPowerMode = await WMI.LenovoGameZoneData.GetSmartFanModeAsync().ConfigureAwait(false);
            var restorePassed = restoredPowerMode == originalPowerMode;

            Console.WriteLine($"BatchRestoredSmartFanMode: {restoredPowerMode}");

            foreach (var plan in plans)
            {
                var restoredStepper = GetCapabilityStepper(restoredPreset, plan.CapabilityId);
                var restoredHardwareValue = await ReadCapabilityValueAsync(plan.CapabilityId).ConfigureAwait(false);
                var capabilityRestorePassed =
                    restoredStepper?.Value == plan.OriginalStepper.Value &&
                    (!verificationPassed || restoredHardwareValue == plan.BeforeHardwareValue);

                restorePassed &= capabilityRestorePassed;

                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].RestoredPresetValue: {restoredStepper?.Value ?? -1}");
                Console.WriteLine($"CapabilityResult[{plan.CapabilityId}].RestoredHardwareValue: {restoredHardwareValue}");
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

    private static int? GetAlternateStepperValue(StepperValue value)
    {
        if (value.Steps.Length > 0)
        {
            return value.Steps
                .Distinct()
                .Where(step => step != value.Value)
                .OrderBy(step => Math.Abs(step - value.Value))
                .ThenBy(step => step)
                .Cast<int?>()
                .FirstOrDefault();
        }

        if (value.Step > 0)
        {
            if (value.Value + value.Step <= value.Max)
                return value.Value + value.Step;

            if (value.Value - value.Step >= value.Min)
                return value.Value - value.Step;
        }
        else
        {
            if (value.Value < value.Max)
                return value.Value + 1;

            if (value.Value > value.Min)
                return value.Value - 1;
        }

        return null;
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
        Console.WriteLine("  power-mode get");
        Console.WriteLine("  power-mode set <int>");
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
