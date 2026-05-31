using FluentAssertions;
using LenovoLegionToolkit.Lib;
using UniversalDeviceToolkit.WPF.Controls.Dashboard;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class PowerModeControlTests
{
    [Fact]
    public void ShouldShowConfigButton_WhenPerformanceSupportsGodMode_ShouldReturnTrue()
    {
        var properties = new MachineInformation.PropertyData
        {
            SupportsGodModeV2 = true
        };

        PowerModeControl.ShouldShowConfigButton(PowerModeState.Performance, properties).Should().BeTrue();
    }

    [Fact]
    public void ShouldShowConfigButton_WhenBalanceSupportsAIMode_ShouldReturnTrue()
    {
        var properties = new MachineInformation.PropertyData
        {
            SupportsAIMode = true
        };

        PowerModeControl.ShouldShowConfigButton(PowerModeState.Balance, properties).Should().BeTrue();
    }

    [Fact]
    public void ShouldShowConfigButton_WhenQuietWithoutSpecialSupport_ShouldReturnFalse()
    {
        PowerModeControl.ShouldShowConfigButton(PowerModeState.Quiet, new MachineInformation.PropertyData()).Should().BeFalse();
    }
}
