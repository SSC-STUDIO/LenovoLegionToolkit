using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Shared.Settings;
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
    public async Task DedicatedDashboardControlsUseTheirOwnStateContracts()
    {
        var services = new UnavailablePlatformServices();

        var states = await services.GetDashboardItemStatesAsync(
            ["DiscreteGpu", "OverclockDiscreteGpu", "TurnOffMonitors", "PowerMode"]);

        states.Select(state => state.Identifier)
            .Should().Equal("PowerMode");
        states.Should().NotContain(state =>
            DashboardItemStateRouting.IsDedicatedControl(state.Identifier));
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

    [Fact]
    public async Task UnavailableHostResolvesEveryDefaultGroupItemToAStableState()
    {
        var services = new UnavailablePlatformServices();

        var identifiers = AvaloniaDashboardPreferences.CreateDefaultGroups()
            .SelectMany(group => group.Items)
            .Where(identifier => !DashboardItemStateRouting.IsDedicatedControl(identifier))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var states = await services.GetDashboardItemStatesAsync(identifiers);

        states.Should().HaveCount(identifiers.Length);
        states.Should().OnlyContain(state => !state.IsAvailable);
    }

    [Fact]
    public void DefaultDashboardGroupsResolveToKnownDescriptors()
    {
        var identifiers = AvaloniaDashboardPreferences.CreateDefaultGroups()
            .SelectMany(group => group.Items)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var identifier in identifiers)
        {
            var descriptor = DashboardItemDescriptors.Get(identifier);
            descriptor.FallbackTitle.Should().NotBeNullOrWhiteSpace();
        }

        identifiers.Should().OnlyHaveUniqueItems();
        identifiers.Should().BeSubsetOf(DashboardItemDescriptors.AllIdentifiers);
    }
}
