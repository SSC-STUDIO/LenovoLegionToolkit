using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.DeviceSupport;
using Xunit;

namespace UniversalDeviceToolkit.Tests.DeviceSupport;

[Trait("Category", TestCategories.Unit)]
public sealed class LenovoDeviceSupportProviderTests
{
    public LenovoDeviceSupportProviderTests()
    {
        LenovoDeviceSupportProvider.Instance.SetInstalledCatalog(null);
    }

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

    [Theory]
    [InlineData("82AX", "Y7000P 2020H")]
    [InlineData("82B0", "Y7000P2020H")]
    [InlineData("82GR", "Legion Y7000P 2020H")]
    [InlineData("0000", "Lenovo Y7000P2020H")]
    public void Evaluate_WhenY7000P2020HMatches_ShouldEnableLegion5HardwareSupport(string machineType, string model)
    {
        // Arrange
        var machineInformation = new MachineInformation
        {
            Vendor = "LENOVO",
            MachineType = machineType,
            Model = model
        };

        // Act
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        // Assert
        availability.IsSupported.Should().BeTrue();
        availability.IsBasicMode.Should().BeFalse();
        availability.DevicePackId.Should().Be("lenovo-legion-5");
        availability.EnabledFeatures.Should().Contain("lenovo-hardware-controls");
    }

    [Theory]
    [InlineData("83GS", "LOQ 15IRX9", "lenovo-loq")]
    [InlineData("83E1", "Legion Go", "lenovo-legion-go")]
    [InlineData("83N0", "Legion Go S", "lenovo-legion-go")]
    public void Evaluate_WhenRecentLenovoGamingMatches_ShouldEnableHardwarePack(
        string machineType,
        string model,
        string expectedPackId)
    {
        var machineInformation = new MachineInformation
        {
            Vendor = "LENOVO",
            MachineType = machineType,
            Model = model
        };

        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        availability.IsSupported.Should().BeTrue();
        availability.IsBasicMode.Should().BeFalse();
        availability.DevicePackId.Should().Be(expectedPackId);
        availability.EnabledFeatures.Should().Contain("lenovo-hardware-controls");
    }

    [Fact]
    public void Evaluate_WhenNonLenovoDevice_ShouldUseBasicMode()
    {
        // Arrange
        var machineInformation = new MachineInformation
        {
            Vendor = "Unknown Vendor",
            MachineType = "0000",
            Model = "Generic PC"
        };

        // Act
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        // Assert
        availability.IsSupported.Should().BeFalse();
        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be("generic-pc-basic");
        availability.EnabledFeatures.Should().Contain(["plugins", "system-optimization"]);
        availability.HiddenFeatures.Should().Contain("lenovo-hardware-controls");
    }

    [Theory]
    [InlineData("LENOVO", "ThinkPad X1 Carbon Gen 12", "lenovo-thinkpad-basic")]
    [InlineData("LENOVO", "ThinkCentre Neo 50q", "lenovo-thinkcentre-basic")]
    [InlineData("LENOVO", "ThinkStation P8", "lenovo-thinkstation-basic")]
    [InlineData("LENOVO", "IdeaCentre 5", "lenovo-ideacentre-basic")]
    [InlineData("LENOVO", "Legion Tower 7i", "lenovo-legion-desktop-basic")]
    [InlineData("LENOVO", "XiaoXin Pro 16", "lenovo-xiaoxin-basic")]
    [InlineData("LENOVO", "小新 Pro 16 2024", "lenovo-xiaoxin-basic")]
    [InlineData("LENOVO", "Lenovo V15 G4", "lenovo-v-series-basic")]
    [InlineData("Dell Inc.", "Alienware m18", "dell-basic")]
    [InlineData("Dell", "Latitude 7450", "dell-basic")]
    [InlineData("ASUSTeK COMPUTER INC.", "ROG Zephyrus G16", "asus-basic")]
    [InlineData("ASUS", "ExpertBook B9", "asus-basic")]
    [InlineData("HP", "OMEN Transcend 14", "hp-basic")]
    [InlineData("HP Inc.", "Spectre x360", "hp-basic")]
    [InlineData("Acer", "Predator Helios Neo", "acer-basic")]
    [InlineData("Micro-Star International Co., Ltd.", "MSI Raider 18", "msi-basic")]
    [InlineData("MSI", "Summit E16", "msi-basic")]
    [InlineData("Microsoft Corporation", "Surface Laptop Studio", "microsoft-surface-basic")]
    [InlineData("Gigabyte Technology Co., Ltd.", "AORUS 16X", "gigabyte-basic")]
    [InlineData("Razer Inc.", "Razer Blade 16", "razer-basic")]
    [InlineData("Samsung", "Galaxy Book4 Pro", "samsung-basic")]
    [InlineData("Apple Inc.", "MacBook Pro", "apple-basic")]
    [InlineData("Huawei Technologies Co., Ltd.", "MateBook X Pro", "huawei-basic")]
    [InlineData("Redmi", "RedmiBook Pro 16", "xiaomi-basic")]
    [InlineData("realme", "realme Book Prime", "realme-basic")]
    [InlineData("Infinix Mobility Limited", "INBook X2", "infinix-basic")]
    [InlineData("Honor Device Co., Ltd.", "MagicBook Pro 16", "honor-basic")]
    [InlineData("LG", "LG gram 17", "lg-basic")]
    [InlineData("Framework Computer Inc.", "Framework Laptop 16", "framework-basic")]
    [InlineData("Panasonic Corporation", "TOUGHBOOK 55", "panasonic-basic")]
    [InlineData("TOSHIBA", "Tecra A40", "dynabook-basic")]
    [InlineData("FUJITSU CLIENT COMPUTING LIMITED", "LIFEBOOK U9313", "fujitsu-basic")]
    [InlineData("VAIO", "VAIO SX14", "vaio-basic")]
    [InlineData("Gateway", "Gateway 14.1 Ultra Slim", "gateway-basic")]
    [InlineData("CHUWI", "CoreBook X", "chuwi-basic")]
    [InlineData("TECLAST", "F15 Plus", "teclast-basic")]
    [InlineData("Jumper", "EZbook X3", "jumper-basic")]
    [InlineData("MEDION AG", "ERAZER Beast X40", "medion-basic")]
    [InlineData("XMG", "XMG Neo 16", "xmg-schenker-basic")]
    [InlineData("Hasee", "ZhanShen Z8", "hasee-basic")]
    [InlineData("THUNDEROBOT", "911 Zero", "thunderobot-basic")]
    [InlineData("MACHENIKE", "F117", "machenike-basic")]
    [InlineData("COLORFUL", "Evol X15", "colorful-basic")]
    [InlineData("MAIBENBEN", "MaiBook X", "maibenben-basic")]
    [InlineData("TUXEDO", "InfinityBook Pro 14", "xmg-schenker-basic")]
    [InlineData("Valve Corporation", "Steam Deck", "valve-handheld-basic")]
    [InlineData("GPD", "GPD WIN 4", "gpd-handheld-basic")]
    [InlineData("AYANEO", "AYANEO NEXT", "ayaneo-handheld-basic")]
    [InlineData("AYN", "Odin2 Pro", "ayaneo-handheld-basic")]
    [InlineData("ANBERNIC", "RG556", "anbernic-handheld-basic")]
    [InlineData("Retroid", "Retroid Pocket 5", "retroid-handheld-basic")]
    [InlineData("Orange Pi", "Orange Pi Neo", "orange-pi-handheld-basic")]
    [InlineData("ONEXPLAYER", "OneXPlayer 2 Pro", "one-netbook-handheld-basic")]
    [InlineData("MINISFORUM", "Venus Series UM790 Pro", "minisforum-basic")]
    [InlineData("AZW", "Beelink SER8", "beelink-basic")]
    [InlineData("GEEKOM", "Mini IT13", "geekom-basic")]
    [InlineData("ZOTAC", "ZBOX MAGNUS", "zotac-basic")]
    [InlineData("System76", "Oryx Pro oryp13", "system76-basic")]
    [InlineData("Notebook", "oryp13", "system76-basic")]
    [InlineData("Star Labs Systems", "StarFighter", "star-labs-basic")]
    [InlineData("SLIMBOOK", "KDE Slimbook VII", "slimbook-basic")]
    [InlineData("Tongfang", "Eluktronics MECH-16", "clevo-tongfang-basic")]
    [InlineData("Monster Notebook", "Tulpar T7 V20", "monster-tulpar-basic")]
    [InlineData("Dream Machines", "RG4070-16", "dream-machines-basic")]
    [InlineData("PC Specialist Ltd", "Recoil 17", "pcspecialist-basic")]
    [InlineData("Eurocom Corporation", "Nightsky ARX15", "eurocom-basic")]
    [InlineData("ORIGIN PC", "EON16-S", "origin-pc-basic")]
    [InlineData("iBUYPOWER", "Y60 Gaming Desktop", "cyberpower-ibuypower-basic")]
    [InlineData("Casper Bilgisayar", "Excalibur G870", "casper-excalibur-basic")]
    [InlineData("AVITA", "LIBER V14", "nexstgo-avita-basic")]
    [InlineData("Positivo Tecnologia", "Motion C4500", "positivo-basic")]
    [InlineData("Wortmann AG", "TERRA MOBILE 1517", "wortmann-terra-basic")]
    [InlineData("MeLE", "Quieter4C", "mele-basic")]
    [InlineData("NiPoGi", "AK1 Plus Mini PC", "regional-mini-pc-basic")]
    [InlineData("PELADN", "HA-4 Mini PC", "regional-mini-pc-basic")]
    [InlineData("BMAX", "BMAX X15", "bmax-ninkear-basic")]
    [InlineData("Ninkear", "Ninkear N15 Pro", "bmax-ninkear-basic")]
    [InlineData("ASRock", "Z790 Taichi", "universal-motherboard-basic")]
    [InlineData("Super Micro Computer, Inc.", "X13 Workstation", "universal-workstation-basic")]
    [InlineData("Default string", "System Product Name", "universal-desktop-basic")]
    [InlineData("To Be Filled By O.E.M.", "To Be Filled By O.E.M.", "universal-motherboard-basic")]
    public void Evaluate_WhenBasicDevicePackMatches_ShouldHideHardwareControls(string vendor, string model, string expectedPackId)
    {
        // Arrange
        var machineInformation = new MachineInformation
        {
            Vendor = vendor,
            MachineType = "0000",
            Model = model
        };

        // Act
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        // Assert
        availability.IsSupported.Should().BeFalse();
        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be(expectedPackId);
        availability.EnabledFeatures.Should().Contain(["plugins", "system-optimization", "language"]);
        availability.HiddenFeatures.Should().Contain(["lenovo-hardware-controls", "power-modes", "gpu-overclock"]);
    }

    [Theory]
    [InlineData("DELL INCORPORATED", "Alienware m18", "dell-basic")]
    [InlineData("ASUSTEK COMPUTER INC", "ROG Zephyrus G16", "asus-basic")]
    [InlineData("MICRO STAR INTERNATIONAL CO LTD", "Raider 18 HX", "msi-basic")]
    [InlineData("Samsung Electronics Co Ltd", "Galaxy Book4 Pro", "samsung-basic")]
    [InlineData("Apple Computer Inc", "MacBook Air", "apple-basic")]
    [InlineData("GIGA-BYTE Technology Co Ltd", "AORUS 16X", "gigabyte-basic")]
    [InlineData("Dynabook Incorporated", "Portégé X40", "dynabook-basic")]
    [InlineData("ASROCK INC", "B650M Pro RS", "universal-motherboard-basic")]
    [InlineData("SUPER MICRO COMPUTER INC", "SYS-741GE", "universal-motherboard-basic")]
    public void Evaluate_WhenVendorUsesDifferentDmiFormatting_ShouldStillMatchBasicPack(string vendor, string model, string expectedPackId)
    {
        // Arrange
        var machineInformation = new MachineInformation
        {
            Vendor = vendor,
            MachineType = "0000",
            Model = model
        };

        // Act
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        // Assert
        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be(expectedPackId);
    }

    [Fact]
    public void Evaluate_WhenVendorIsEmpty_ShouldReturnNamedGenericBasicPack()
    {
        // Arrange
        var machineInformation = new MachineInformation
        {
            Vendor = "",
            MachineType = "",
            Model = ""
        };

        // Act
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        // Assert
        availability.IsSupported.Should().BeFalse();
        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be("generic-pc-basic");
        availability.EnabledFeatures.Should().Contain(["plugins", "system-optimization", "theme"]);
        availability.HiddenFeatures.Should().Contain(["lenovo-hardware-controls", "keyboard-backlight"]);
    }

    [Fact]
    public void Evaluate_WhenVendorIsEmptyButBaseBoardIdentifiesCustomPc_ShouldUseMotherboardPack()
    {
        // Arrange
        var machineInformation = new MachineInformation
        {
            Vendor = "",
            MachineType = "",
            Model = "",
            Hardware = new()
            {
                BaseBoard = new()
                {
                    Manufacturer = "ASRock",
                    Product = "B650M Pro RS"
                }
            }
        };

        // Act
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        // Assert
        availability.IsSupported.Should().BeFalse();
        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be("universal-motherboard-basic");
        availability.HiddenFeatures.Should().Contain("lenovo-hardware-controls");
    }

    [Fact]
    public void Evaluate_WhenDmiModelIsGenericButChassisIdentifiesDesktop_ShouldUseDesktopPack()
    {
        // Arrange
        var machineInformation = new MachineInformation
        {
            Vendor = "Unknown",
            MachineType = "",
            Model = "Default string",
            Hardware = new()
            {
                Chassis = new()
                {
                    ChassisTypes = [3]
                }
            }
        };

        // Act
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        // Assert
        availability.IsSupported.Should().BeFalse();
        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be("universal-desktop-basic");
    }

    [Theory]
    [InlineData("ACME", "Unknown Laptop", "generic-pc-basic")]
    [InlineData("Unknown", "Desktop PC", "universal-desktop-basic")]
    [InlineData("Custom", "Custom Workstation", "universal-workstation-basic")]
    public void Evaluate_WhenOnlyGenericClassMatches_ShouldUseUniversalBasicPack(string vendor, string model, string expectedPackId)
    {
        // Arrange
        var machineInformation = new MachineInformation
        {
            Vendor = vendor,
            MachineType = "0000",
            Model = model
        };

        // Act
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        // Assert
        availability.IsSupported.Should().BeFalse();
        availability.IsBasicMode.Should().BeTrue();
        availability.DevicePackId.Should().Be(expectedPackId);
        availability.HiddenFeatures.Should().Contain(["lenovo-hardware-controls", "power-modes", "fan-curve"]);
    }

    [Fact]
    public void Evaluate_WhenMachineTypeMatchesHardwarePack_ShouldPreferItOverLenovoBasicKeyword()
    {
        // Arrange
        var machineInformation = new MachineInformation
        {
            Vendor = "LENOVO",
            MachineType = "83DH",
            Model = "Lenovo Slim 5"
        };

        // Act
        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation);

        // Assert
        availability.IsSupported.Should().BeTrue();
        availability.IsBasicMode.Should().BeFalse();
        availability.DevicePackId.Should().Be("lenovo-legion-slim-5");
        availability.EnabledFeatures.Should().Contain("lenovo-hardware-controls");
    }

    [Fact]
    public void Evaluate_WhenInstalledCatalogMatches_ShouldEnableInstalledPack()
    {
        // Arrange
        var provider = LenovoDeviceSupportProvider.Instance;
        provider.SetInstalledCatalog(new DeviceSupportCatalog
        {
            DevicePacks =
            [
                new DevicePack
                {
                    Id = "lenovo-custom-installed",
                    DisplayName = "Lenovo Custom Installed",
                    Vendor = "LENOVO",
                    Families = ["Lenovo"],
                    MachineTypes = ["9999"],
                    EnabledFeatures = ["lenovo-hardware-controls"]
                }
            ]
        });
        var machineInformation = new MachineInformation
        {
            Vendor = "LENOVO",
            MachineType = "9999",
            Model = "Lenovo Future Device"
        };

        try
        {
            // Act
            var availability = provider.Evaluate(machineInformation);

            // Assert
            availability.IsSupported.Should().BeTrue();
            availability.IsBasicMode.Should().BeFalse();
            availability.DevicePackId.Should().Be("lenovo-custom-installed");
        }
        finally
        {
            provider.SetInstalledCatalog(null);
        }
    }

    [Fact]
    public void Evaluate_WhenInstalledCatalogMatchesOnlyFamilySignal_ShouldUseInstalledPack()
    {
        // Arrange
        var provider = LenovoDeviceSupportProvider.Instance;
        provider.SetInstalledCatalog(new DeviceSupportCatalog
        {
            DevicePacks =
            [
                new DevicePack
                {
                    Id = "asus-rog-family-installed",
                    DisplayName = "ASUS ROG Family Installed",
                    Vendor = "ASUS",
                    Families = ["ROG"],
                    EnabledFeatures = ["plugins", "system-optimization"],
                    HiddenFeatures = ["lenovo-hardware-controls"]
                }
            ]
        });
        var machineInformation = MachineInformationTestData.WithComputerSystem(
            "ASUS",
            "",
            "ASUSTeK COMPUTER INC.",
            "",
            "ROG");

        try
        {
            // Act
            var availability = provider.Evaluate(machineInformation);

            // Assert
            availability.IsSupported.Should().BeFalse();
            availability.IsBasicMode.Should().BeTrue();
            availability.DevicePackId.Should().Be("asus-rog-family-installed");
            availability.HiddenFeatures.Should().Contain("lenovo-hardware-controls");
        }
        finally
        {
            provider.SetInstalledCatalog(null);
        }
    }
}
