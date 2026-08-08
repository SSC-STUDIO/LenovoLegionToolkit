using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Abstractions.Macro;
using UniversalDeviceToolkit.ViewModels;
using SharedAutomationPipelineDraft = UniversalDeviceToolkit.ViewModels.AutomationPipelineDraftSnapshot;
using SharedAutomationPipeline = UniversalDeviceToolkit.ViewModels.AutomationPipelineSnapshot;
using SharedAutomationStep = UniversalDeviceToolkit.ViewModels.AutomationStepSnapshot;
using SharedAutomationStepOption = UniversalDeviceToolkit.ViewModels.AutomationStepOptionSnapshot;
using SharedAutomationTriggerOption = UniversalDeviceToolkit.ViewModels.AutomationTriggerOptionSnapshot;
using SharedAutomationWorkspace = UniversalDeviceToolkit.ViewModels.AutomationWorkspaceSnapshot;
using SharedKeyboardColor = UniversalDeviceToolkit.ViewModels.KeyboardBacklightColor;
using SharedKeyboardEffect = UniversalDeviceToolkit.ViewModels.KeyboardBacklightSpectrumEffect;
using SharedKeyboardPreset = UniversalDeviceToolkit.ViewModels.KeyboardBacklightRgbPreset;
using SharedKeyboardState = UniversalDeviceToolkit.ViewModels.KeyboardBacklightWorkspaceState;
using SharedKeyboardUpdate = UniversalDeviceToolkit.ViewModels.KeyboardBacklightWorkspaceUpdate;
using SharedMacroEvent = UniversalDeviceToolkit.ViewModels.MacroEventSnapshot;
using SharedMacroMode = UniversalDeviceToolkit.ViewModels.MacroRecordingMode;
using SharedMacroSlot = UniversalDeviceToolkit.ViewModels.MacroSlotSnapshot;
using SharedMacroState = UniversalDeviceToolkit.ViewModels.MacroWorkspaceSnapshot;

namespace UniversalDeviceToolkit.Avalonia.Services;

internal sealed class PlatformKeyboardBacklightDetectionService(IPlatformServices services)
    : IKeyboardBacklightDetectionService
{
    public async Task<bool> IsSpectrumSupportedAsync()
    {
        var state = await services.GetKeyboardLightingStateAsync().ConfigureAwait(false);
        return state?.Mode.Equals("Spectrum", StringComparison.OrdinalIgnoreCase) == true;
    }

    public async Task<bool> IsRgbSupportedAsync()
    {
        var state = await services.GetKeyboardLightingStateAsync().ConfigureAwait(false);
        return state?.Mode.Equals("RGB", StringComparison.OrdinalIgnoreCase) == true;
    }
}

internal sealed class PlatformKeyboardBacklightWorkspace(IPlatformServices services)
    : IKeyboardBacklightWorkspace
{
    public async Task<SharedKeyboardState?> GetStateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ToShared(await services.GetKeyboardLightingStateAsync().ConfigureAwait(false));
    }

    public Task<bool> ApplyAsync(
        SharedKeyboardUpdate update,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.SetKeyboardLightingAsync(ToPlatform(update));
    }

    public Task<bool> ResetSpectrumProfileAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.ResetKeyboardSpectrumProfileAsync();
    }

    public Task<bool> ExportSpectrumProfileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.ExportKeyboardSpectrumProfileAsync(filePath);
    }

    public Task<bool> ImportSpectrumProfileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.ImportKeyboardSpectrumProfileAsync(filePath);
    }

    public static SharedKeyboardState? ToShared(KeyboardLightingState? state) => state is null
        ? null
        : new SharedKeyboardState(
            state.Mode,
            state.Brightness,
            state.LogoEnabled,
            state.SelectedProfile,
            state.SpectrumEffects.Select(ToShared).ToArray(),
            state.RgbPresets.Select(ToShared).ToArray(),
            state.KeyboardLayout,
            state.SpectrumLayout,
            state.KeyboardKeys,
            state.IsBlockedByVantage);

    public static KeyboardLightingState? ToPlatform(SharedKeyboardState? state) => state is null
        ? null
        : new KeyboardLightingState(
            state.Mode,
            state.Brightness,
            state.LogoEnabled,
            state.SelectedProfile,
            state.SpectrumEffects.Select(ToPlatform).ToArray(),
            state.RgbPresets.Select(ToPlatform).ToArray(),
            state.KeyboardLayout,
            state.SpectrumLayout,
            state.KeyboardKeys,
            state.IsBlockedByVantage);

    public static SharedKeyboardUpdate ToShared(KeyboardLightingUpdate update) => new(
        update.Mode,
        update.SelectedProfile,
        update.Brightness,
        update.LogoEnabled,
        update.RgbPreset,
        update.RgbEffect,
        update.RgbSpeed,
        update.RgbBrightness,
        update.RgbZones?.Select(ToShared).ToArray(),
        update.SpectrumEffects?.Select(ToShared).ToArray(),
        update.KeyboardLayout);

    public static KeyboardLightingUpdate ToPlatform(SharedKeyboardUpdate update) => new(
        update.Mode,
        update.SelectedProfile,
        update.Brightness,
        update.LogoEnabled,
        update.RgbPreset,
        update.RgbEffect,
        update.RgbSpeed,
        update.RgbBrightness,
        update.RgbZones?.Select(ToPlatform).ToArray(),
        update.SpectrumEffects?.Select(ToPlatform).ToArray(),
        update.KeyboardLayout);

    private static SharedKeyboardEffect ToShared(KeyboardSpectrumEffectState effect) => new(
        effect.Type,
        effect.Speed,
        effect.Direction,
        effect.ClockwiseDirection,
        effect.Colors.Select(ToShared).ToArray(),
        effect.Keys);

    private static KeyboardSpectrumEffectState ToPlatform(SharedKeyboardEffect effect) => new(
        effect.Type,
        effect.Speed,
        effect.Direction,
        effect.ClockwiseDirection,
        effect.Colors.Select(ToPlatform).ToArray(),
        effect.Keys);

    private static SharedKeyboardPreset ToShared(KeyboardRgbPresetState preset) => new(
        preset.Key,
        preset.DisplayName,
        preset.IsSelected,
        preset.Effect,
        preset.Speed,
        preset.Brightness,
        preset.Zones.Select(ToShared).ToArray());

    private static KeyboardRgbPresetState ToPlatform(SharedKeyboardPreset preset) => new(
        preset.Key,
        preset.DisplayName,
        preset.IsSelected,
        preset.Effect,
        preset.Speed,
        preset.Brightness,
        preset.Zones.Select(ToPlatform).ToArray());

    private static SharedKeyboardColor ToShared(KeyboardColorState color) =>
        new(color.R, color.G, color.B);

    private static KeyboardColorState ToPlatform(SharedKeyboardColor color) =>
        new(color.R, color.G, color.B);
}

internal sealed class PlatformMacroWorkspace(IPlatformServices services) : IMacroWorkspace
{
    public async Task<SharedMacroState> GetStateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ToShared(await services.GetMacroWorkspaceAsync().ConfigureAwait(false));
    }

    public Task<bool> SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.SetMacroEnabledAsync(enabled);
    }

    public Task<bool> StartRecordingAsync(
        ulong key,
        SharedMacroMode mode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.StartMacroRecordingAsync(key, ToPlatform(mode));
    }

    public Task<bool> StopRecordingAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.SetFeatureActionAsync("Macro", "macro-stop-recording", true);
    }

    public Task<bool> PlayAsync(ulong key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.SetFeatureActionAsync("Macro", $"macro-key:{key:X}", true);
    }

    public Task<bool> SetSequenceOptionsAsync(
        ulong key,
        int repeatCount,
        bool ignoreDelays,
        bool interruptOnOtherKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.SetMacroSequenceOptionsAsync(
            key,
            repeatCount,
            ignoreDelays,
            interruptOnOtherKey);
    }

    public Task<bool> SaveSequenceAsync(
        ulong key,
        IReadOnlyList<SharedMacroEvent> events,
        int repeatCount,
        bool ignoreDelays,
        bool interruptOnOtherKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.SaveMacroSequenceAsync(
            key,
            events.Select(ToPlatform).ToArray(),
            repeatCount,
            ignoreDelays,
            interruptOnOtherKey);
    }

    public Task<bool> ClearSequenceAsync(ulong key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.ClearMacroSequenceAsync(key);
    }

    public static SharedMacroState ToShared(MacroWorkspaceState state) => new(
        state.IsEnabled,
        state.IsRecording,
        state.Slots.Select(ToShared).ToArray());

    public static MacroWorkspaceState ToPlatform(SharedMacroState state) => new(
        state.IsEnabled,
        state.IsRecording,
        state.Slots.Select(ToPlatform).ToArray());

    private static SharedMacroSlot ToShared(MacroSlotState slot) => new(
        slot.Key,
        slot.RepeatCount,
        slot.IgnoreDelays,
        slot.InterruptOnOtherKey,
        (slot.Events ?? []).Select(ToShared).ToArray());

    private static MacroSlotState ToPlatform(SharedMacroSlot slot) => new(
        slot.Key,
        slot.Events.Count,
        slot.RepeatCount,
        slot.IgnoreDelays,
        slot.InterruptOnOtherKey,
        slot.Events.Select(ToPlatform).ToArray());

    private static SharedMacroEvent ToShared(MacroEventItem macroEvent) => new(
        macroEvent.Source,
        macroEvent.Direction,
        macroEvent.Key,
        macroEvent.X,
        macroEvent.Y,
        macroEvent.Delay);

    private static MacroEventItem ToPlatform(SharedMacroEvent macroEvent) => new(
        macroEvent.Source,
        macroEvent.Direction,
        macroEvent.Key,
        macroEvent.X,
        macroEvent.Y,
        macroEvent.Delay);

    private static global::UniversalDeviceToolkit.Avalonia.Services.MacroRecordingMode ToPlatform(SharedMacroMode mode) => mode switch
    {
        SharedMacroMode.KeyboardMouse => global::UniversalDeviceToolkit.Avalonia.Services.MacroRecordingMode.KeyboardMouse,
        SharedMacroMode.KeyboardMouseMovement => global::UniversalDeviceToolkit.Avalonia.Services.MacroRecordingMode.KeyboardMouseMovement,
        _ => global::UniversalDeviceToolkit.Avalonia.Services.MacroRecordingMode.Keyboard,
    };
}

/// <summary>
/// Avalonia uses the workspace service for real macro operations. This tiny
/// controller keeps the legacy shared ViewModel constructor usable on hosts
/// where the Windows hook controller is intentionally unavailable.
/// </summary>
internal sealed class PlatformMacroController : IMacroController
{
    public bool IsEnabled { get; private set; }

    public void SetEnabled(bool enabled) => IsEnabled = enabled;
}

internal sealed class PlatformAutomationWorkspace(IPlatformServices services) : IAutomationWorkspace
{
    public async Task<AutomationWorkspaceSnapshot> GetStateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stateTask = services.GetAutomationWorkspaceAsync();
        var triggerTask = services.GetAutomationTriggerOptionsAsync();
        var stepTask = services.GetAutomationStepOptionsAsync();
        await Task.WhenAll(stateTask, triggerTask, stepTask).ConfigureAwait(false);

        var state = await stateTask.ConfigureAwait(false);
        var triggers = await triggerTask.ConfigureAwait(false);
        var steps = await stepTask.ConfigureAwait(false);
        return new AutomationWorkspaceSnapshot(
            state.IsEnabled,
            state.Pipelines.Select(ToShared).ToArray(),
            triggers.Select(option => new SharedAutomationTriggerOption(
                option.Key,
                option.DisplayName,
                option.DefaultConfigurationJson)).ToArray(),
            steps.Select(option => new SharedAutomationStepOption(
                option.TypeKey,
                option.DisplayName,
                option.DefaultConfigurationJson)).ToArray());
    }

    public Task<bool> SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.SetAutomationEnabledAsync(enabled);
    }

    public Task<bool> SaveAsync(
        IReadOnlyList<SharedAutomationPipelineDraft> pipelines,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.SaveAutomationWorkspaceAsync(pipelines.Select(ToPlatform).ToArray());
    }

    public Task<bool> RunAsync(Guid pipelineId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return services.SetFeatureActionAsync("Actions", $"automation-pipeline:{pipelineId:D}", true);
    }

    public static AutomationWorkspaceState ToPlatform(SharedAutomationWorkspace state) =>
        new(
            state.IsEnabled,
            state.Pipelines.Select(ToPlatform).ToArray());

    public static IReadOnlyList<AutomationTriggerOption> ToPlatformTriggers(
        IReadOnlyList<SharedAutomationTriggerOption> options) =>
        options.Select(option => new AutomationTriggerOption(
            option.Key,
            option.DisplayName,
            option.DefaultConfigurationJson)).ToArray();

    public static IReadOnlyList<AutomationStepOption> ToPlatformSteps(
        IReadOnlyList<SharedAutomationStepOption> options) =>
        options.Select(option => new AutomationStepOption(
            option.TypeKey,
            option.DisplayName,
            option.DefaultConfigurationJson)).ToArray();

    public static SharedAutomationPipelineDraft ToShared(AutomationPipelineDraft draft) =>
        new(
            draft.Id,
            draft.Name,
            draft.IconName,
            draft.IsAutomatic,
            draft.IsExclusive,
            draft.TriggerKey,
            draft.TriggerConfigurationJson,
            draft.Steps.Select(ToShared).ToArray());

    public static AutomationPipelineItem ToPlatform(SharedAutomationPipeline pipeline) =>
        new(
            pipeline.Id,
            pipeline.Name,
            pipeline.IconName,
            pipeline.Trigger,
            pipeline.Steps.Count,
            pipeline.IsAutomatic)
        {
            TriggerKey = pipeline.TriggerKey,
            TriggerConfigurationJson = pipeline.TriggerConfigurationJson,
            IsExclusive = pipeline.IsExclusive,
            Steps = pipeline.Steps.Select(ToPlatform).ToArray(),
        };

    private static SharedAutomationPipeline ToShared(AutomationPipelineItem pipeline) =>
        new(
            pipeline.Id,
            pipeline.Name,
            pipeline.IconName,
            pipeline.Trigger,
            pipeline.IsAutomatic,
            pipeline.IsExclusive,
            pipeline.TriggerKey,
            pipeline.TriggerConfigurationJson,
            pipeline.Steps.Select(ToShared).ToArray());

    private static SharedAutomationStep ToShared(AutomationStepItem step) =>
        new(step.TypeKey, step.DisplayName, step.ConfigurationJson);

    private static AutomationPipelineDraft ToPlatform(SharedAutomationPipelineDraft draft) =>
        new(draft.Id, draft.Name, draft.IconName, draft.IsAutomatic)
        {
            TriggerKey = draft.TriggerKey,
            TriggerConfigurationJson = draft.TriggerConfigurationJson,
            IsExclusive = draft.IsExclusive,
            Steps = draft.Steps.Select(ToPlatform).ToArray(),
        };

    private static AutomationStepItem ToPlatform(SharedAutomationStep step) =>
        new(step.TypeKey, step.DisplayName, step.ConfigurationJson);
}
