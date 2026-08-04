using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class MacroWorkspaceContractTests
{
    [Fact]
    public async Task UnavailableHost_ExposesAnExplicitEmptyMacroWorkspace()
    {
        var service = new UnavailablePlatformServices();

        var state = await service.GetMacroWorkspaceAsync();

        state.IsEnabled.Should().BeFalse();
        state.IsRecording.Should().BeFalse();
        state.Slots.Should().BeEmpty();
        (await service.SetMacroEnabledAsync(true)).Should().BeFalse();
        (await service.SetMacroSequenceOptionsAsync(0x60, 1, false, false)).Should().BeFalse();
    }

    [Fact]
    public void MacroSlotState_PreservesSequenceOptions()
    {
        var slot = new MacroSlotState(0x60, 4, 3, true, false);

        slot.Key.Should().Be(0x60);
        slot.EventCount.Should().Be(4);
        slot.RepeatCount.Should().Be(3);
        slot.IgnoreDelays.Should().BeTrue();
        slot.InterruptOnOtherKey.Should().BeFalse();
    }
}
