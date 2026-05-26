using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.DeviceSupport;
using Xunit;

namespace UniversalDeviceToolkit.Tests.DeviceSupport;

[Trait("Category", TestCategories.Unit)]
public sealed class DeviceSupportModelSimulationTests
{
    public DeviceSupportModelSimulationTests()
    {
        LenovoDeviceSupportProvider.Instance.SetInstalledCatalog(null);
    }

    [Theory]
    [MemberData(nameof(MultiBrandBasicModeScenarios))]
    public void Evaluate_WithSimulatedBrandModel_ShouldUseExpectedBasicPack(MachineInformation machineInformation, string expectedPackId)
    {
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        availability.IsSupported.Should().BeFalse();
        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be(expectedPackId);
        availability.EnabledFeatures.Should().Contain(["plugins", "system-optimization", "language", "theme"]);
        availability.HiddenFeatures.Should().Contain(["lenovo-hardware-controls", "power-modes", "gpu-overclock", "fan-curve"]);
    }

    [Theory]
    [MemberData(nameof(HardwareSignalScenarios))]
    public void Evaluate_WithSimulatedHardwareInventorySignals_ShouldUseExpectedBasicPack(MachineInformation machineInformation, string expectedPackId)
    {
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be(expectedPackId);
        availability.HiddenFeatures.Should().Contain("lenovo-hardware-controls");
    }

    [Fact]
    public void Evaluate_WithSimulatedLegionMachineType_ShouldStillEnableLenovoHardwarePack()
    {
        var machineInformation = MachineInformationTestData.Create(
            "LENOVO",
            "Legion Y9000P IRX9",
            "83DE");

        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        availability.IsSupported.Should().BeTrue();
        availability.IsBasicMode.Should().BeFalse();
        availability.DevicePackId.Should().Be("lenovo-legion-pro-7");
        availability.EnabledFeatures.Should().Contain("lenovo-hardware-controls");
        availability.HiddenFeatures.Should().NotContain("lenovo-hardware-controls");
    }

    public static TheoryData<MachineInformation, string> MultiBrandBasicModeScenarios() => new()
    {
        { MachineInformationTestData.Create("ASUSTeK COMPUTER INC.", "ROG Strix SCAR 18 G835"), "asus-basic" },
        { MachineInformationTestData.Create("ASUS", "TUF Gaming A16 FA608"), "asus-basic" },
        { MachineInformationTestData.Create("ASUSTEK COMPUTER INCORPORATED", "Zenbook S 14 UX5406"), "asus-basic" },
        { MachineInformationTestData.Create("MECHREVO", "Jiaolong 16 Pro"), "mechrevo-basic" },
        { MachineInformationTestData.Create("Mechanical Revolution", "Kuangshi 16 Super"), "mechrevo-basic" },
        { MachineInformationTestData.Create("HP", "OMEN Max 16"), "hp-basic" },
        { MachineInformationTestData.Create("HP Inc.", "Victus 16"), "hp-basic" },
        { MachineInformationTestData.Create("Hewlett-Packard Company", "ZBook Fury 16 G11"), "hp-basic" },
        { MachineInformationTestData.Create("Dell Inc.", "Alienware m18 R2"), "dell-basic" },
        { MachineInformationTestData.Create("Dell Computer Corporation", "Dell G16 7630"), "dell-basic" },
        { MachineInformationTestData.Create("Acer Incorporated", "Predator Helios Neo 16"), "acer-basic" },
        { MachineInformationTestData.Create("Acer", "Nitro V 16"), "acer-basic" },
        { MachineInformationTestData.Create("Xiaomi Corporation", "Xiaomi Book Pro 16"), "xiaomi-basic" },
        { MachineInformationTestData.Create("TIMI", "Redmi G Pro 2024"), "xiaomi-basic" },
        { MachineInformationTestData.Create("Huawei Technologies Co., Ltd.", "MateBook GT 14"), "huawei-basic" },
        { MachineInformationTestData.Create("HUAWEI", "Qingyun L540"), "huawei-basic" }
    };

    public static TheoryData<MachineInformation, string> HardwareSignalScenarios() => new()
    {
        {
            MachineInformationTestData.WithComputerSystem(
                "",
                "",
                "ASUSTeK COMPUTER INC.",
                "System Product Name",
                "ROG Flow"),
            "asus-basic"
        },
        {
            MachineInformationTestData.WithBaseBoard(
                "",
                "",
                "MECHREVO",
                "Jiaolong Series"),
            "mechrevo-basic"
        },
        {
            MachineInformationTestData.WithComputerSystem(
                "",
                "",
                "HP",
                "OMEN Transcend Laptop 14"),
            "hp-basic"
        },
        {
            MachineInformationTestData.WithComputerSystem(
                "",
                "",
                "Dell Inc.",
                "Alienware m18 R2"),
            "dell-basic"
        },
        {
            MachineInformationTestData.WithBaseBoard(
                "",
                "",
                "Acer",
                "Nitro ANV16"),
            "acer-basic"
        },
        {
            MachineInformationTestData.WithComputerSystem(
                "",
                "",
                "TIMI",
                "RedmiBook Pro 15"),
            "xiaomi-basic"
        },
        {
            MachineInformationTestData.WithComputerSystem(
                "",
                "",
                "Huawei Technologies Co., Ltd.",
                "MateBook D 16"),
            "huawei-basic"
        },
        {
            MachineInformationTestData.WithChassis(
                "Unknown",
                "System Product Name",
                "",
                [3]),
            "universal-desktop-basic"
        }
    };
}
