using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class DashboardItemStateContractTests
{
    [Fact]
    public async Task UnavailableHostReturnsStableUnavailableStatesForRequestedItems()
    {
        var services = new UnavailablePlatformServices();

        var states = await services.GetDashboardItemStatesAsync(
            ["PowerMode", "PowerMode", "BatteryMode"]);

        states.Should().HaveCount(2);
        states.Should().OnlyContain(state => !state.IsAvailable);
        states.Select(state => state.Identifier)
            .Should().ContainInOrder("PowerMode", "BatteryMode");
        states.Single(state => state.Identifier == "PowerMode")
            .ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UnavailableHostDoesNotClaimDashboardMutationsSucceeded()
    {
        var services = new UnavailablePlatformServices();

        (await services.SetDashboardItemStateAsync("PowerMode", "Performance"))
            .Should().BeFalse();
    }

    [Fact]
    public async Task UnavailableHostReportsGpuControlsAsUnavailable()
    {
        var services = new UnavailablePlatformServices();

        var gpu = await services.GetDiscreteGpuStateAsync();
        var overclock = await services.GetGpuOverclockStateAsync();

        gpu.IsAvailable.Should().BeFalse();
        gpu.CanKillProcesses.Should().BeFalse();
        gpu.CanRestart.Should().BeFalse();
        gpu.Error.Should().NotBeNullOrWhiteSpace();
        overclock.IsAvailable.Should().BeFalse();
        overclock.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UnavailableHostRejectsGpuMutations()
    {
        var services = new UnavailablePlatformServices();

        (await services.KillDiscreteGpuProcessesAsync()).Should().BeFalse();
        (await services.RestartDiscreteGpuAsync()).Should().BeFalse();
        (await services.TurnOffMonitorsAsync()).Should().BeFalse();
        (await services.SetGpuOverclockAsync(true, 100, 100)).Should().BeFalse();
    }
}
