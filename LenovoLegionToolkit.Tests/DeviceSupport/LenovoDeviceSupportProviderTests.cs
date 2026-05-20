using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.DeviceSupport;
using Xunit;

namespace LenovoLegionToolkit.Tests.DeviceSupport;

[Trait("Category", TestCategories.Unit)]
public sealed class LenovoDeviceSupportProviderTests
{
    [Fact]
    public void Evaluate_WhenLegionMachineTypeMatches_ShouldEnableSupportedMode()
    {
        // Arrange
        var machineInformation = new MachineInformation
        {
            Vendor = "LENOVO",
            MachineType = "83DE",
            Model = "Legion Y9000P IRX9"
        };

        // Act
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        // Assert
        availability.IsSupported.Should().BeTrue();
        availability.IsBasicMode.Should().BeFalse();
        availability.DevicePackId.Should().Be("lenovo-legion-pro-7");
        availability.EnabledFeatures.Should().Contain("lenovo-hardware-controls");
    }

    [Fact]
    public void Evaluate_WhenNonLenovoDevice_ShouldUseBasicMode()
    {
        // Arrange
        var machineInformation = new MachineInformation
        {
            Vendor = "DELL",
            MachineType = "0000",
            Model = "Generic PC"
        };

        // Act
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        // Assert
        availability.IsSupported.Should().BeFalse();
        availability.IsBasicMode.Should().BeTrue();
        availability.EnabledFeatures.Should().Contain(["plugins", "system-optimization"]);
        availability.HiddenFeatures.Should().Contain("lenovo-hardware-controls");
    }
}
