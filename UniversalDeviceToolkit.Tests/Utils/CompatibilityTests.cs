using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Unit)]
public class CompatibilityTests
{
    [Theory]
    [InlineData("Legion 5 15AKP10")]
    [InlineData("Legion 5 15IRX10")]
    [InlineData("Legion Pro 5 16ADR10")]
    [InlineData("Legion Pro 5 16AFR10")]
    [InlineData("Legion 9 18IAX10")]
    [InlineData("LOQ 17IRX10")]
    public void IsSupportedLegionMachine_With2025GamingModel_ShouldReturnTrue(string model)
    {
        var machineInformation = CreateMachineInformation(model);

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSupportedLegionMachine_WithUnsupportedVendor_ShouldReturnFalse()
    {
        var machineInformation = CreateMachineInformation("Legion Pro 5 16ADR10", vendor: "OTHER");

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSupportedDevice_WithUnsupportedVendor_ShouldReturnFalse()
    {
        var machineInformation = CreateMachineInformation("Generic Laptop", vendor: "OTHER");

        var result = Compatibility.IsSupportedDevice(machineInformation);

        result.Should().BeFalse();
    }

    [Fact]
    public void GetDeviceFeatureAvailability_WithUnsupportedVendor_ShouldReturnGenericBasicPack()
    {
        var machineInformation = CreateMachineInformation("Generic Laptop", vendor: "OTHER");

        var availability = Compatibility.GetDeviceFeatureAvailability(machineInformation);

        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be("generic-pc-basic");
        availability.HiddenFeatures.Should().Contain("lenovo-hardware-controls");
    }

    [Theory]
    [InlineData("ASRock", "B650M Pro RS", "universal-motherboard-basic")]
    [InlineData("Unknown", "Desktop PC", "universal-desktop-basic")]
    [InlineData("", "", "generic-pc-basic")]
    public void GetDeviceFeatureAvailability_WithGenericDevice_ShouldReturnNamedBasicPack(string vendor, string model, string expectedPackId)
    {
        var machineInformation = CreateMachineInformation(model, vendor: vendor);

        var availability = Compatibility.GetDeviceFeatureAvailability(machineInformation);

        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be(expectedPackId);
        availability.EnabledFeatures.Should().Contain(["plugins", "system-optimization"]);
        availability.HiddenFeatures.Should().Contain(["lenovo-hardware-controls", "keyboard-backlight"]);
    }

    [Fact]
    public void GetDeviceFeatureAvailability_WithHardwareInventorySignals_ShouldReturnNamedBasicPack()
    {
        var machineInformation = new MachineInformation
        {
            Vendor = "",
            MachineType = "",
            Model = "",
            SerialNumber = "TEST",
            SupportedPowerModes = [],
            Features = MachineInformation.FeatureData.Unknown,
            Properties = new MachineInformation.PropertyData(),
            Hardware = new()
            {
                ComputerSystem = new()
                {
                    Manufacturer = "To Be Filled By O.E.M.",
                    Model = "System Product Name"
                },
                BaseBoard = new()
                {
                    Manufacturer = "ASRock",
                    Product = "B650M Pro RS"
                }
            }
        };

        var availability = Compatibility.GetDeviceFeatureAvailability(machineInformation);

        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be("universal-motherboard-basic");
    }

    [Fact]
    public void IsSupportedLegionMachine_WithUnsupportedLenovoModel_ShouldReturnBasicMode()
    {
        var machineInformation = CreateMachineInformation("IdeaPad Pro 5 16AKP10");

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Legion Y9000P IRX9")]
    [InlineData("Lenovo LEGION R9000P ACH16")]
    [InlineData("Lenovo R7000P ACH15")]
    [InlineData("Legion Y7000 IRX9")]
    [InlineData("LOQ G5000 16IRH8")]
    public void IsSupportedLegionMachine_WithChineseVariant_ShouldReturnTrue(string model)
    {
        var machineInformation = CreateMachineInformation(model);

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("82AX", "Y7000P 2020H")]
    [InlineData("82B0", "Y7000P2020H")]
    [InlineData("82GR", "Legion Y7000P 2020H")]
    public void GetDeviceFeatureAvailability_WithY7000P2020H_ShouldUseLegion5HardwarePack(string machineType, string model)
    {
        var machineInformation = CreateMachineInformation(model) with { MachineType = machineType };

        var availability = Compatibility.GetDeviceFeatureAvailability(machineInformation);

        availability.IsSupported.Should().BeTrue();
        availability.IsBasicMode.Should().BeFalse();
        availability.DevicePackId.Should().Be("lenovo-legion-5");
    }

    [Theory]
    [InlineData("LEGION 17IRH7")]
    [InlineData("Legion 15IRH8")]
    [InlineData("Legion 15ICH5")]
    [InlineData("LOQ 15IKB4")]
    public void IsSupportedLegionMachine_WithLimitedCompatibilityModel_ShouldReturnTrue(string model)
    {
        var machineInformation = CreateMachineInformation(model);

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSupportedLegionMachine_WithEmptyVendor_ShouldReturnFalse()
    {
        var machineInformation = CreateMachineInformation("Legion 5 15ACH6", vendor: "");

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSupportedLegionMachine_WithNullVendor_ShouldReturnFalse()
    {
        var machineInformation = new MachineInformation
        {
            Vendor = null!,
            MachineType = "83XX",
            Model = "Legion 5 15ACH6",
            SerialNumber = "TEST",
            SupportedPowerModes = [],
            Features = MachineInformation.FeatureData.Unknown,
            Properties = new MachineInformation.PropertyData()
        };

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSupportedLegionMachine_WithEmptyModel_ShouldReturnFalse()
    {
        var machineInformation = CreateMachineInformation("", vendor: "LENOVO");

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSupportedLegionMachine_WithNullModel_ShouldReturnFalse()
    {
        var machineInformation = new MachineInformation
        {
            Vendor = "LENOVO",
            MachineType = "83XX",
            Model = null!,
            SerialNumber = "TEST",
            SupportedPowerModes = [],
            Features = MachineInformation.FeatureData.Unknown,
            Properties = new MachineInformation.PropertyData()
        };

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("LENOVO")]
    [InlineData("lenovo")]
    [InlineData("Lenovo")]
    [InlineData("leNOVO")]
    public void IsSupportedLegionMachine_WithDifferentVendorCase_ShouldReturnTrue(string vendor)
    {
        var machineInformation = CreateMachineInformation("Legion 5 15ACH6", vendor: vendor);

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSupportedLegionMachine_WithMotorolaLegionVendor_ShouldReturnBasicMode()
    {
        var machineInformation = CreateMachineInformation("Legion 5 15ACH6", vendor: "MOTOROLA");

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("Legion Pro 7 NX10")]
    [InlineData("ThinkBook 16P G7 IRX")]
    [InlineData("Legion 5 14AKP10")]
    [InlineData("Legion Slim 5 14AHP10")]
    public void IsSupportedLegionMachine_WithAdditionalUpstreamModelPrefixes_ShouldReturnTrue(string model)
    {
        var machineInformation = CreateMachineInformation(model);

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeTrue();
    }

    [Fact]
    public void SmokeSimulateLegionEnvironmentVariable_WhenSet_ShouldBeDetected()
    {
        try
        {
            Environment.SetEnvironmentVariable("LLT_SMOKE_SIMULATE_LEGION", "1");
            Compatibility.IsSmokeLegionSimulationEnabled.Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("LLT_SMOKE_SIMULATE_LEGION", null);
        }
    }

    [Fact]
    public void SmokeSimulateLegionEnvironmentVariable_WhenNotSet_ShouldBeFalse()
    {
        Environment.SetEnvironmentVariable("LLT_SMOKE_SIMULATE_LEGION", null);
        Compatibility.IsSmokeLegionSimulationEnabled.Should().BeFalse();
    }

    [Fact]
    public void SmokeSimulateLegionEnvironmentVariable_WhenSetToOtherValue_ShouldBeFalse()
    {
        try
        {
            Environment.SetEnvironmentVariable("LLT_SMOKE_SIMULATE_LEGION", "yes");
            Compatibility.IsSmokeLegionSimulationEnabled.Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("LLT_SMOKE_SIMULATE_LEGION", null);
        }
    }

    private static MachineInformation CreateMachineInformation(string model, string vendor = "LENOVO") => new()
    {
        Vendor = vendor,
        MachineType = "83XX",
        Model = model,
        SerialNumber = "TEST",
        SupportedPowerModes = [],
        Features = MachineInformation.FeatureData.Unknown,
        Properties = new MachineInformation.PropertyData()
    };
}
