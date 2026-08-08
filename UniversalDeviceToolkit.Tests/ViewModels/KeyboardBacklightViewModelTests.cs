using FluentAssertions;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Abstractions.Macro;
using UniversalDeviceToolkit.ViewModels;
using Xunit;

namespace UniversalDeviceToolkit.Tests.ViewModels;

public sealed class KeyboardBacklightViewModelTests
{
    [Fact]
    public async Task RejectedLightingUpdate_PreservesTheLastAcceptedState()
    {
        var initial = new KeyboardBacklightWorkspaceState(
            "Spectrum",
            4,
            false,
            1,
            [],
            []);
        var workspace = new FakeKeyboardWorkspace(initial) { AcceptUpdates = false };
        var viewModel = new KeyboardBacklightViewModel(new FakeKeyboardDetection(), workspace);

        await viewModel.LoadWorkspaceAsync();
        var accepted = await viewModel.SetSpectrumBrightnessAsync(12);

        accepted.Should().BeFalse();
        viewModel.State.Should().Be(initial);
        workspace.LastUpdate!.Brightness.Should().Be(9);
        viewModel.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    private sealed class FakeKeyboardDetection : IKeyboardBacklightDetectionService
    {
        public Task<bool> IsSpectrumSupportedAsync() => Task.FromResult(true);
        public Task<bool> IsRgbSupportedAsync() => Task.FromResult(false);
    }

    private sealed class FakeKeyboardWorkspace(KeyboardBacklightWorkspaceState state) : IKeyboardBacklightWorkspace
    {
        public bool AcceptUpdates { get; set; } = true;
        public KeyboardBacklightWorkspaceUpdate? LastUpdate { get; private set; }

        public Task<KeyboardBacklightWorkspaceState?> GetStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<KeyboardBacklightWorkspaceState?>(state);

        public Task<bool> ApplyAsync(KeyboardBacklightWorkspaceUpdate update, CancellationToken cancellationToken = default)
        {
            LastUpdate = update;
            return Task.FromResult(AcceptUpdates);
        }

        public Task<bool> ResetSpectrumProfileAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ExportSpectrumProfileAsync(string filePath, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ImportSpectrumProfileAsync(string filePath, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}

public sealed class MacroViewModelTests
{
    [Fact]
    public async Task RejectedEnableUpdate_RollsBackTheToggle()
    {
        var workspace = new FakeMacroWorkspace { AcceptUpdates = false };
        var viewModel = new MacroViewModel(new FakeMacroController(), workspace);
        await viewModel.LoadWorkspaceAsync();

        var accepted = await viewModel.SetEnabledAsync(true);

        accepted.Should().BeFalse();
        viewModel.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task LoadedSequence_CanBeReorderedAndSavedThroughTheWorkspace()
    {
        var workspace = new FakeMacroWorkspace
        {
            State = new MacroWorkspaceSnapshot(
                false,
                false,
                [new MacroSlotSnapshot(
                    0x60,
                    1,
                    false,
                    false,
                    [
                        new MacroEventSnapshot("Keyboard", "Down", 0x41, 0, 0, TimeSpan.Zero),
                        new MacroEventSnapshot("Keyboard", "Up", 0x41, 0, 0, TimeSpan.Zero),
                    ])]),
        };
        var viewModel = new MacroViewModel(new FakeMacroController(), workspace);
        await viewModel.LoadWorkspaceAsync();
        var slot = viewModel.FindSlot(0x60)!;

        slot.MoveEvent(1, -1).Should().BeTrue();
        await viewModel.SaveSequenceAsync(slot);

        workspace.SavedEvents.Should().Equal(
            slot.Events.Select(item => item.Key));
    }

    private sealed class FakeMacroController : IMacroController
    {
        public bool IsEnabled { get; private set; }
        public void SetEnabled(bool enabled) => IsEnabled = enabled;
    }

    private sealed class FakeMacroWorkspace : IMacroWorkspace
    {
        public MacroWorkspaceSnapshot State { get; set; } = new(false, false, []);
        public bool AcceptUpdates { get; set; } = true;
        public IReadOnlyList<uint> SavedEvents { get; private set; } = [];

        public Task<MacroWorkspaceSnapshot> GetStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<bool> SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default) => Task.FromResult(AcceptUpdates);
        public Task<bool> StartRecordingAsync(ulong key, MacroRecordingMode mode, CancellationToken cancellationToken = default) => Task.FromResult(AcceptUpdates);
        public Task<bool> StopRecordingAsync(CancellationToken cancellationToken = default) => Task.FromResult(AcceptUpdates);
        public Task<bool> PlayAsync(ulong key, CancellationToken cancellationToken = default) => Task.FromResult(AcceptUpdates);
        public Task<bool> SetSequenceOptionsAsync(ulong key, int repeatCount, bool ignoreDelays, bool interruptOnOtherKey, CancellationToken cancellationToken = default) => Task.FromResult(AcceptUpdates);

        public Task<bool> SaveSequenceAsync(ulong key, IReadOnlyList<MacroEventSnapshot> events, int repeatCount, bool ignoreDelays, bool interruptOnOtherKey, CancellationToken cancellationToken = default)
        {
            SavedEvents = events.Select(item => item.Key).ToArray();
            return Task.FromResult(AcceptUpdates);
        }

        public Task<bool> ClearSequenceAsync(ulong key, CancellationToken cancellationToken = default) => Task.FromResult(AcceptUpdates);
    }
}

public sealed class AutomationWorkspaceViewModelTests
{
    [Fact]
    public async Task RejectedEnableUpdate_RollsBackAndSaveKeepsDirtyDraft()
    {
        var workspace = new FakeAutomationWorkspace
        {
            AcceptUpdates = false,
            State = new AutomationWorkspaceSnapshot(
                false,
                [],
                [new AutomationTriggerOptionSnapshot("startup", "Startup")],
                [new AutomationStepOptionSnapshot("Delay", "Delay", "{}")]),
        };
        var viewModel = new AutomationWorkspaceViewModel(workspace);
        await viewModel.LoadAsync();

        (await viewModel.SetEnabledAsync(true)).Should().BeFalse();
        viewModel.IsEnabled.Should().BeFalse();

        var draft = viewModel.AddManualPipeline("Quick action");
        viewModel.IsDirty.Should().BeTrue();
        (await viewModel.SaveAsync([draft.ToDraft(isNew: true)])).Should().BeFalse();
        viewModel.IsDirty.Should().BeTrue();
    }

    private sealed class FakeAutomationWorkspace : IAutomationWorkspace
    {
        public bool AcceptUpdates { get; set; } = true;
        public AutomationWorkspaceSnapshot State { get; set; } = new(false, [], [], []);

        public Task<AutomationWorkspaceSnapshot> GetStateAsync(CancellationToken cancellationToken = default) => Task.FromResult(State);
        public Task<bool> SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default) => Task.FromResult(AcceptUpdates);
        public Task<bool> SaveAsync(IReadOnlyList<AutomationPipelineDraftSnapshot> pipelines, CancellationToken cancellationToken = default) => Task.FromResult(AcceptUpdates);
        public Task<bool> RunAsync(Guid pipelineId, CancellationToken cancellationToken = default) => Task.FromResult(AcceptUpdates);
    }
}
