using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Pages.Windows;
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
        (await service.SaveMacroSequenceAsync(
            0x60,
            [new MacroEventItem("Keyboard", "Down", 0x41, 0, 0, TimeSpan.Zero)],
            1,
            false,
            false)).Should().BeFalse();
        (await service.ClearMacroSequenceAsync(0x60)).Should().BeFalse();
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

    [Fact]
    public void MacroSlotState_CanCarryRecordedEventDetails()
    {
        var delay = TimeSpan.FromMilliseconds(125);
        var slot = new MacroSlotState(
            0x60,
            1,
            1,
            false,
            false,
            [new MacroEventItem("Keyboard", "Down", 0x41, 0, 0, delay)]);

        slot.Events.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new MacroEventItem("Keyboard", "Down", 0x41, 0, 0, delay));
    }
}

public sealed class MacroEventEditingTests
{
    [Fact]
    public void CreateKeyboardEvent_ProjectsDownKeyEvent()
    {
        var item = MacroEventEditing.CreateKeyboardEvent(0x41, TimeSpan.FromMilliseconds(50));

        item.Should().BeEquivalentTo(new MacroEventItem("Keyboard", "Down", 0x41, 0, 0, TimeSpan.FromMilliseconds(50)));
        MacroEventEditing.IsDelayOnlyEvent(item).Should().BeFalse();
        MacroEventEditing.CanCapture(item).Should().BeTrue();
    }

    [Fact]
    public void CreateMouseEvent_ProjectsDownButtonEvent()
    {
        var item = MacroEventEditing.CreateMouseEvent(2);

        item.Should().BeEquivalentTo(new MacroEventItem("Mouse", "Down", 2, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void CreateDelayEvent_ProducesDelayOnlyItem()
    {
        var item = MacroEventEditing.CreateDelayEvent(TimeSpan.FromMilliseconds(250));

        MacroEventEditing.IsDelayOnlyEvent(item).Should().BeTrue();
        MacroEventEditing.CanCapture(item).Should().BeFalse();
        MacroEventEditing.FormatEvent(item).Should().Contain("250 ms delay");
    }

    [Fact]
    public void AddEvent_AppendsAndRejectsNulls()
    {
        var events = new List<MacroEventItem>();

        MacroEventEditing.AddEvent(events, MacroEventEditing.CreateKeyboardEvent(0x41)).Should().BeTrue();
        events.Should().ContainSingle();
        MacroEventEditing.AddEvent(null!, MacroEventEditing.CreateKeyboardEvent(0x42)).Should().BeFalse();
        MacroEventEditing.AddEvent(events, null!).Should().BeFalse();
        events.Should().ContainSingle();
    }

    [Fact]
    public void RemoveEventAt_RemovesOnlyValidIndexes()
    {
        var events = new List<MacroEventItem>
        {
            MacroEventEditing.CreateKeyboardEvent(0x41),
            MacroEventEditing.CreateKeyboardEvent(0x42),
        };

        MacroEventEditing.RemoveEventAt(events, 1).Should().BeTrue();
        events.Should().ContainSingle().Which.Key.Should().Be(0x41);
        MacroEventEditing.RemoveEventAt(events, 5).Should().BeFalse();
        MacroEventEditing.RemoveEventAt(events, -1).Should().BeFalse();
    }

    [Fact]
    public void MoveEventUp_AndDown_ReorderWithinBounds()
    {
        var events = new List<MacroEventItem>
        {
            MacroEventEditing.CreateKeyboardEvent(0x41),
            MacroEventEditing.CreateKeyboardEvent(0x42),
            MacroEventEditing.CreateKeyboardEvent(0x43),
        };

        MacroEventEditing.MoveEventDown(events, 0).Should().BeTrue();
        events.Select(item => item.Key).Should().Equal(0x42u, 0x41u, 0x43u);
        MacroEventEditing.MoveEventUp(events, 1).Should().BeTrue();
        events.Select(item => item.Key).Should().Equal(0x41u, 0x42u, 0x43u);
        MacroEventEditing.MoveEventUp(events, 0).Should().BeFalse();
        MacroEventEditing.MoveEventDown(events, 2).Should().BeFalse();
        events.Select(item => item.Key).Should().Equal(0x41u, 0x42u, 0x43u);
    }

    [Fact]
    public void WithDelay_AndWithCapturedInput_PreserveSiblingFields()
    {
        var item = MacroEventEditing.CreateKeyboardEvent(0x41, TimeSpan.FromMilliseconds(10));

        var delayed = MacroEventEditing.WithDelay(item, TimeSpan.FromMilliseconds(500));
        delayed.Delay.Should().Be(TimeSpan.FromMilliseconds(500));
        delayed.Key.Should().Be(0x41);

        var captured = MacroEventEditing.WithCapturedInput(
            item,
            MacroKeyCaptureWindow.CaptureResult.FromMouseButton(3));
        captured.Source.Should().Be("Mouse");
        captured.Direction.Should().Be("Down");
        captured.Key.Should().Be(3);
        captured.Delay.Should().Be(item.Delay);
    }

    [Fact]
    public void FromCapture_ProjectsCapturedInputIntoEvent()
    {
        var item = MacroEventEditing.FromCapture(
            MacroKeyCaptureWindow.CaptureResult.FromKeyboard(0x51),
            TimeSpan.FromMilliseconds(75));

        item.Should().BeEquivalentTo(new MacroEventItem("Keyboard", "Down", 0x51, 0, 0, TimeSpan.FromMilliseconds(75)));
    }

    [Fact]
    public void CreatePress_AndReplaceCapturedPress_PreserveExecutableDownUpPairs()
    {
        var events = MacroEventEditing.CreatePress(MacroKeyCaptureWindow.CaptureResult.FromKeyboard(0x41)).ToList();

        events.Select(item => item.Direction).Should().Equal("Down", "Up");
        MacroEventEditing.ReplaceCapturedPress(
            events,
            0,
            MacroKeyCaptureWindow.CaptureResult.FromMouseButton(2)).Should().BeTrue();
        events.Should().BeEquivalentTo(
        [
            new MacroEventItem("Mouse", "Down", 2, 0, 0, TimeSpan.Zero),
            new MacroEventItem("Mouse", "Up", 2, 0, 0, TimeSpan.Zero),
        ]);
    }

    [Fact]
    public void FormatEvent_RendersTypedSummary()
    {
        MacroEventEditing.FormatEvent(MacroEventEditing.CreateKeyboardEvent(0x41, TimeSpan.FromMilliseconds(125)))
            .Should().Be("Keyboard Down | 65 | +125 ms");
        MacroEventEditing.FormatEvent(MacroEventEditing.CreateMouseEvent(1))
            .Should().Be("Mouse Down | 1 | +0 ms");
        MacroEventEditing.FormatEvent(null!).Should().BeEmpty();
    }
}

public sealed class MacroKeyCaptureWindowContractTests
{
    [Fact]
    public void TryGetKeyCode_MapsLogicalKeysToVirtualKeyCodes()
    {
        MacroKeyCaptureWindow.TryGetKeyCode(Key.A, out var a).Should().BeTrue();
        a.Should().Be(0x41);
        MacroKeyCaptureWindow.TryGetKeyCode(Key.NumPad0, out var numpad).Should().BeTrue();
        numpad.Should().Be(0x60);
        MacroKeyCaptureWindow.TryGetKeyCode(Key.F12, out var f12).Should().BeTrue();
        f12.Should().Be(0x7B);
        MacroKeyCaptureWindow.TryGetKeyCode(Key.LeftShift, out var shift).Should().BeTrue();
        shift.Should().Be(0xA0);
        MacroKeyCaptureWindow.TryGetKeyCode(Key.None, out _).Should().BeFalse();
    }

    [Fact]
    public void CaptureResult_ExposesHostNeutralProjection()
    {
        var key = MacroKeyCaptureWindow.CaptureResult.FromKeyboard(0x42);
        key.Source.Should().Be("Keyboard");
        key.Direction.Should().Be("Down");
        key.Key.Should().Be(0x42);

        var mouse = MacroKeyCaptureWindow.CaptureResult.FromMouseButton(1);
        mouse.Source.Should().Be("Mouse");
        mouse.Key.Should().Be(1);
    }
}
