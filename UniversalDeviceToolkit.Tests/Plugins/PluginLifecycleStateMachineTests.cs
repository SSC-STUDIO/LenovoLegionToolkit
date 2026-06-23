using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
public class PluginLifecycleStateMachineTests
{
    private readonly PluginLifecycleStateMachine _stateMachine = new();

    [Fact]
    public void CanTransition_FromNotInstalled_ToInstalled_ShouldBeTrue()
    {
        _stateMachine.CanTransition(PluginState.NotInstalled, PluginState.Installed)
            .Should().BeTrue();
    }

    [Fact]
    public void CanTransition_FromInstalled_ToEnabled_ShouldBeTrue()
    {
        _stateMachine.CanTransition(PluginState.Installed, PluginState.Enabled)
            .Should().BeTrue();
    }

    [Fact]
    public void CanTransition_FromInstalled_ToDisabled_ShouldBeTrue()
    {
        _stateMachine.CanTransition(PluginState.Installed, PluginState.Disabled)
            .Should().BeTrue();
    }

    [Fact]
    public void CanTransition_FromEnabled_ToInstalled_ShouldBeTrue()
    {
        _stateMachine.CanTransition(PluginState.Enabled, PluginState.Installed)
            .Should().BeTrue();
    }

    [Fact]
    public void CanTransition_FromEnabled_ToNotInstalled_ShouldBeTrue()
    {
        _stateMachine.CanTransition(PluginState.Enabled, PluginState.NotInstalled)
            .Should().BeTrue();
    }

    [Fact]
    public void CanTransition_FromEnabled_ToDisabled_ShouldBeTrue()
    {
        _stateMachine.CanTransition(PluginState.Enabled, PluginState.Disabled)
            .Should().BeTrue();
    }

    [Fact]
    public void CanTransition_FromDisabled_ToEnabled_ShouldBeTrue()
    {
        _stateMachine.CanTransition(PluginState.Disabled, PluginState.Enabled)
            .Should().BeTrue();
    }

    [Fact]
    public void CanTransition_FromError_ToInstalled_ShouldBeTrue()
    {
        _stateMachine.CanTransition(PluginState.Error, PluginState.Installed)
            .Should().BeTrue();
    }

    [Fact]
    public void CanTransition_FromError_ToEnabled_ShouldBeFalse()
    {
        // Cannot skip the Installed baseline; Error must recover to Installed first.
        _stateMachine.CanTransition(PluginState.Error, PluginState.Enabled)
            .Should().BeFalse();
    }

    [Fact]
    public void CanTransition_FromNotInstalled_ToEnabled_ShouldBeFalse()
    {
        // Must go through Installed before reaching Enabled.
        _stateMachine.CanTransition(PluginState.NotInstalled, PluginState.Enabled)
            .Should().BeFalse();
    }

    [Fact]
    public void CanTransition_FromNotInstalled_ToDisabled_ShouldBeFalse()
    {
        _stateMachine.CanTransition(PluginState.NotInstalled, PluginState.Disabled)
            .Should().BeFalse();
    }

    [Fact]
    public void CanTransition_SameState_ShouldBeFalse()
    {
        foreach (var state in System.Enum.GetValues<PluginState>())
        {
            _stateMachine.CanTransition(state, state).Should().BeFalse(
                $"self-transition from {state} to {state} should be rejected");
        }
    }

    [Fact]
    public void Validate_ReturnsCorrectReasonForIllegalTransition()
    {
        var result = _stateMachine.Validate(PluginState.NotInstalled, PluginState.Enabled);
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Be(PluginTransitionRejectionReason.IllegalTransition);
        result.From.Should().Be(PluginState.NotInstalled);
        result.To.Should().Be(PluginState.Enabled);
    }

    [Fact]
    public void TryTransition_AppliesValidTransitionAndReturnsTrue()
    {
        var current = PluginState.NotInstalled;
        var applied = _stateMachine.TryTransition("test", ref current, PluginState.Installed);
        applied.Should().BeTrue();
        current.Should().Be(PluginState.Installed);
    }

    [Fact]
    public void TryTransition_LeavesStateUnchangedWhenRejected()
    {
        var current = PluginState.NotInstalled;
        var applied = _stateMachine.TryTransition("test", ref current, PluginState.Enabled);
        applied.Should().BeFalse();
        current.Should().Be(PluginState.NotInstalled);
    }

    [Fact]
    public void TryTransition_EmptyPluginId_ReturnsFalse()
    {
        var current = PluginState.NotInstalled;
        _stateMachine.TryTransition("", ref current, PluginState.Installed).Should().BeFalse();
        _stateMachine.TryTransition(null, ref current, PluginState.Installed).Should().BeFalse();
        current.Should().Be(PluginState.NotInstalled);
    }

    [Fact]
    public void CanTransition_FromAnyState_ToError_ShouldBeTrueForNonErrorStates()
    {
        // Recoverable failure: any non-Error state can move to Error.
        var nonErrorStates = new[]
        {
            PluginState.NotInstalled,
            PluginState.Installed,
            PluginState.Enabled,
            PluginState.Disabled
        };
        foreach (var state in nonErrorStates)
        {
            _stateMachine.CanTransition(state, PluginState.Error)
                .Should().BeTrue($"{state} should be able to transition to Error");
        }
    }
}
