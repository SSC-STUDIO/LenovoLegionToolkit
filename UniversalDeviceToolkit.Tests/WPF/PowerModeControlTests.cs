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
        var machineInformation = new MachineInformation
        {
            Properties = new MachineInformation.PropertyData { SupportsGodModeV2 = true }
        };

        PowerModeControl.ShouldShowConfigButton(PowerModeState.Performance, machineInformation).Should().BeTrue();
    }

    [Fact]
    public void ShouldShowConfigButton_WhenHardwareReportsGodModeButPropertiesDoNot_ShouldReturnTrue()
    {
        var machineInformation = new MachineInformation
        {
            SupportedPowerModes = [PowerModeState.Quiet, PowerModeState.Balance, PowerModeState.Performance, PowerModeState.GodMode],
            Properties = new MachineInformation.PropertyData()
        };

        PowerModeControl.ShouldShowConfigButton(PowerModeState.GodMode, machineInformation).Should().BeTrue();
        PowerModeControl.ShouldShowConfigButton(PowerModeState.Performance, machineInformation).Should().BeTrue();
    }

    [Fact]
    public void ShouldShowConfigButton_WhenBalanceSupportsAIMode_ShouldReturnTrue()
    {
        var machineInformation = new MachineInformation
        {
            Properties = new MachineInformation.PropertyData { SupportsAIMode = true }
        };

        PowerModeControl.ShouldShowConfigButton(PowerModeState.Balance, machineInformation).Should().BeTrue();
    }

    [Fact]
    public void ShouldShowConfigButton_WhenQuietWithoutSpecialSupport_ShouldReturnFalse()
    {
        var machineInformation = new MachineInformation
        {
            Properties = new MachineInformation.PropertyData()
        };

        PowerModeControl.ShouldShowConfigButton(PowerModeState.Quiet, machineInformation).Should().BeFalse();
    }
}
