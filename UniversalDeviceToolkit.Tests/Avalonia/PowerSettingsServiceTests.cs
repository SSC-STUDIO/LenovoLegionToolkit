#if WINDOWS

using FluentAssertions;
using UniversalDeviceToolkit.Lib;
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

        mapping.SelectedValue.Should().BeOneOf("Disabled", "Windows power mode", "Windows power plans");
        var usesPowerMode = mapping.SelectedValue == "Windows power mode";
        var isDisabled = mapping.SelectedValue == "Disabled";
        powerModes.IsVisible.Should().Be(usesPowerMode);
        powerPlans.IsVisible.Should().Be(!usesPowerMode && !isDisabled);
        controlPanel.IsVisible.Should().Be(!usesPowerMode && !isDisabled);
    }

    [Fact]
    public async Task PowerModeMapping_AllSharedValues_DisplayAndRoundTrip()
    {
        var service = AvaloniaSettingsServiceFactory.Create();
        var initialPage = await service.GetPageAsync("Power");
        var initialMapping = initialPage.Options.Single(option => option.Key == "PowerModeMapping");
        var initialValue = initialMapping.SelectedValue;
        var mappingValues = initialMapping.Values!;
        var modes = Enum.GetValues<PowerModeMappingMode>();

        mappingValues.Should().HaveCount(modes.Length);
        mappingValues.Should().OnlyHaveUniqueItems();

        try
        {
            foreach (var mode in modes)
            {
                await service.SetSelectionAsync("Power", "PowerModeMapping", mode.ToString());

                var page = await service.GetPageAsync("Power");
                var mapping = page.Options.Single(option => option.Key == "PowerModeMapping");

                mapping.SelectedValue.Should().Be(mappingValues[(int)mode]);
                mapping.Values.Should().Equal(mappingValues);

                var powerModes = page.Options.Single(option => option.Key == "OpenPowerModes");
                var powerPlans = page.Options.Single(option => option.Key == "OpenPowerPlans");
                var controlPanel = page.Options.Single(option => option.Key == "OpenPowerPlansControlPanel");
                if (mode == PowerModeMappingMode.Disabled)
                {
                    powerModes.IsVisible.Should().BeFalse();
                    powerPlans.IsVisible.Should().BeFalse();
                    controlPanel.IsVisible.Should().BeFalse();
                }
            }
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(initialValue))
                await service.SetSelectionAsync("Power", "PowerModeMapping", initialValue);
        }
    }
}

#endif
