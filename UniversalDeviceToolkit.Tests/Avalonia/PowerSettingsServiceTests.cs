#if WINDOWS

using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class PowerSettingsServiceTests
{
    [Fact]
    public async Task PowerPage_OnlyShowsActionsForTheSelectedPowerMapping()
    {
        var service = AvaloniaSettingsServiceFactory.Create();

        var page = await service.GetPageAsync("Power");
        var mapping = page.Options.Single(option => option.Key == "PowerModeMapping");
        var powerModes = page.Options.Single(option => option.Key == "OpenPowerModes");
        var powerPlans = page.Options.Single(option => option.Key == "OpenPowerPlans");
        var controlPanel = page.Options.Single(option => option.Key == "OpenPowerPlansControlPanel");

        if (!mapping.IsEnabled)
        {
            powerModes.IsVisible.Should().BeFalse();
            powerPlans.IsVisible.Should().BeFalse();
            controlPanel.IsVisible.Should().BeFalse();
            return;
        }

        mapping.SelectedValue.Should().BeOneOf("Windows power mode", "Windows power plans");
        var usesPowerMode = mapping.SelectedValue == "Windows power mode";
        powerModes.IsVisible.Should().Be(usesPowerMode);
        powerPlans.IsVisible.Should().Be(!usesPowerMode);
        controlPanel.IsVisible.Should().Be(!usesPowerMode);
    }
}

#endif
