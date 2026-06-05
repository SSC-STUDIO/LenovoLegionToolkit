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

    [Theory]
    [InlineData("ROG", "G835", "asus-basic")]
    [InlineData("TUF Gaming", "FA608", "asus-basic")]
    [InlineData("Alienware", "m18 R2", "dell-basic")]
    [InlineData("OMEN", "Max 16", "hp-basic")]
    [InlineData("Victus", "16", "hp-basic")]
    [InlineData("Predator", "PHN16", "acer-basic")]
    [InlineData("Nitro", "ANV16", "acer-basic")]
    [InlineData("AORUS", "16X", "gigabyte-basic")]
    [InlineData("ERAZER", "Beast X40", "medion-basic")]
    public void Evaluate_WhenDmiVendorUsesGamingSubBrandAndModelUsesSku_ShouldUseParentBasicPack(
        string vendor,
        string model,
        string expectedPackId)
    {
        var machineInformation = MachineInformationTestData.Create(vendor, model);

        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        availability.IsSupported.Should().BeFalse();
        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be(expectedPackId);
        availability.EnabledFeatures.Should().Contain(["plugins", "system-optimization", "language", "theme"]);
        availability.HiddenFeatures.Should().Contain(["lenovo-hardware-controls", "power-modes", "gpu-overclock", "fan-curve"]);
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
        { MachineInformationTestData.Create("LENOVO", "IdeaPad Flex 5 Chromebook Plus"), "lenovo-chromebook-basic" },
        { MachineInformationTestData.Create("Google LLC", "Pixelbook Go"), "google-chromebook-basic" },
        { MachineInformationTestData.Create("SAMSUNG ELECTRONICS CO., LTD.", "Galaxy Chromebook Plus"), "samsung-basic" },
        { MachineInformationTestData.Create("Xiaomi Corporation", "Xiaomi Book Pro 16"), "xiaomi-basic" },
        { MachineInformationTestData.Create("TIMI", "Redmi G Pro 2024"), "xiaomi-basic" },
        { MachineInformationTestData.Create("Huawei Technologies Co., Ltd.", "MateBook GT 14"), "huawei-basic" },
        { MachineInformationTestData.Create("HUAWEI", "Qingyun L540"), "huawei-basic" },
        { MachineInformationTestData.Create("Apple Inc.", "MacBook Air"), "apple-basic" },
        { MachineInformationTestData.Create("Honor Device Co., Ltd.", "MagicBook X 16"), "honor-basic" },
        { MachineInformationTestData.Create("LG", "LG gram Pro 16"), "lg-basic" },
        { MachineInformationTestData.Create("Framework Computer Inc.", "Framework Laptop 13"), "framework-basic" },
        { MachineInformationTestData.Create("Hasee", "ZhanShen T8"), "hasee-basic" },
        { MachineInformationTestData.Create("THUNDEROBOT", "911 MT"), "thunderobot-basic" },
        { MachineInformationTestData.Create("MACHENIKE", "L16"), "machenike-basic" },
        { MachineInformationTestData.Create("COLORFUL", "Evol P15"), "colorful-basic" },
        { MachineInformationTestData.Create("MAIBENBEN", "Xiaomai 6"), "maibenben-basic" },
        { MachineInformationTestData.Create("Valve Corporation", "Steam Deck"), "valve-handheld-basic" },
        { MachineInformationTestData.Create("GPD", "GPD Win Mini"), "gpd-handheld-basic" },
        { MachineInformationTestData.Create("AYANEO", "AYANEO Air Plus"), "ayaneo-handheld-basic" },
        { MachineInformationTestData.Create("ONEXPLAYER", "OneXPlayer Mini Pro"), "one-netbook-handheld-basic" },
        { MachineInformationTestData.Create("MINISFORUM", "UM890 Pro"), "minisforum-basic" },
        { MachineInformationTestData.Create("AZW", "Beelink GTR7"), "beelink-basic" },
        { MachineInformationTestData.Create("GEEKOM", "MiniAir 12"), "geekom-basic" },
        { MachineInformationTestData.Create("ZOTAC", "ZBOX CI669"), "zotac-basic" },
        { MachineInformationTestData.Create("System76", "Lemur Pro lemp13"), "system76-basic" },
        { MachineInformationTestData.Create("Star Labs Systems", "StarBook"), "star-labs-basic" },
        { MachineInformationTestData.Create("SLIMBOOK", "Executive 16"), "slimbook-basic" },
        { MachineInformationTestData.Create("TUXEDO", "Stellaris 16"), "xmg-schenker-basic" },
        { MachineInformationTestData.Create("NEC Personal Computers, Ltd.", "LAVIE NEXTREME Carbon"), "nec-lavie-basic" },
        { MachineInformationTestData.Create("Sharp Corporation", "Mebius Chromebook"), "sharp-basic" },
        { MachineInformationTestData.Create("Monster Notebook", "Tulpar T7 V20"), "monster-tulpar-basic" },
        { MachineInformationTestData.Create("Dream Machines", "RG4070-16"), "dream-machines-basic" },
        { MachineInformationTestData.Create("PC Specialist Ltd", "Recoil 17"), "pcspecialist-basic" },
        { MachineInformationTestData.Create("Eurocom Corporation", "Nightsky ARX15"), "eurocom-basic" },
        { MachineInformationTestData.Create("Eluktronics", "MECH-17 GP3"), "eluktronics-basic" },
        { MachineInformationTestData.Create("MAINGEAR", "Vector Pro 2"), "maingear-basic" },
        { MachineInformationTestData.Create("ORIGIN PC", "EON16-S"), "origin-pc-basic" },
        { MachineInformationTestData.Create("Corsair", "Voyager a1600"), "corsair-basic" },
        { MachineInformationTestData.Create("iBUYPOWER", "Y60 Gaming Desktop"), "cyberpower-ibuypower-basic" },
        { MachineInformationTestData.Create("Casper Bilgisayar", "Excalibur G870"), "casper-excalibur-basic" },
        { MachineInformationTestData.Create("AVITA", "LIBER V14"), "nexstgo-avita-basic" },
        { MachineInformationTestData.Create("Positivo Tecnologia", "Motion C4500"), "positivo-basic" },
        { MachineInformationTestData.Create("Wortmann AG", "TERRA MOBILE 1517"), "wortmann-terra-basic" },
        { MachineInformationTestData.Create("GMKtec", "NucBox K8 Plus"), "gmktec-basic" },
        { MachineInformationTestData.Create("MORE FINE", "M600 Mini PC"), "morefine-basic" },
        { MachineInformationTestData.Create("ACEMAGICIAN", "Tank03"), "acemagic-basic" },
        { MachineInformationTestData.Create("AOOSTAR", "GEM12 Pro"), "aoostar-basic" },
        { MachineInformationTestData.Create("TRIGKEY", "Mini PC S7"), "regional-mini-pc-basic" }
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
            MachineInformationTestData.WithComputerSystem(
                "",
                "",
                "Apple Inc.",
                "MacBookPro18,3",
                "MacBook Pro"),
            "apple-basic"
        },
        {
            MachineInformationTestData.WithComputerSystem(
                "",
                "",
                "AZW",
                "SER8"),
            "beelink-basic"
        },
        {
            MachineInformationTestData.WithComputerSystem(
                "",
                "",
                "Valve Corporation",
                "Steam Deck"),
            "valve-handheld-basic"
        },
        {
            MachineInformationTestData.WithComputerSystem(
                "",
                "",
                "Notebook",
                "oryp13"),
            "system76-basic"
        },
        {
            MachineInformationTestData.WithBaseBoard(
                "",
                "",
                "StarLabs",
                "StarLite Mk V"),
            "star-labs-basic"
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
