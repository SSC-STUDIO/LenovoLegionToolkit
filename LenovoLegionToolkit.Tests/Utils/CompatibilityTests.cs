using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests.Utils;

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
    public void IsSupportedLegionMachine_WithUnsupportedModel_ShouldReturnFalse()
    {
        var machineInformation = CreateMachineInformation("IdeaPad Pro 5 16AKP10");

        var result = Compatibility.IsSupportedLegionMachine(machineInformation);

        result.Should().BeFalse();
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
