using System;
using System.Linq;
using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.DeviceSupport;
using UniversalDeviceToolkit.WPF.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class StartupDeviceSetupCoordinatorTests
{
    [Fact]
    public void CatalogEvaluation_WithMatchingMachineType_ShouldReturnDevicePack()
    {
        var catalog = new DeviceSupportCatalog
        {
            DevicePacks =
            [
                new DevicePack
                {
                    Id = "lenovo-legion-pro-7",
                    DisplayName = "Lenovo Legion Pro 7",
                    Vendor = "LENOVO",
                    Families = ["Legion"],
                    ModelKeywords = ["Legion Pro 7"],
                    MachineTypes = ["83DE"],
                    EnabledFeatures = ["lenovo-hardware-controls"]
                }
            ]
        };
        var machineInformation = new MachineInformation
        {
            Vendor = "LENOVO",
            MachineType = "83DE",
            Model = "Legion Y9000P IRX9"
        };

        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation, catalog);
        var pack = catalog.DevicePacks.SingleOrDefault(devicePack =>
            devicePack.Id.Equals(availability.DevicePackId, StringComparison.OrdinalIgnoreCase));

        pack.Should().NotBeNull();
        pack!.Id.Should().Be("lenovo-legion-pro-7");
    }

    [Fact]
    public void CatalogEvaluation_WithUnknownDevice_ShouldReturnNull()
    {
        var catalog = new DeviceSupportCatalog
        {
            DevicePacks =
            [
                new DevicePack
                {
                    Id = "lenovo-legion-pro-7",
                    DisplayName = "Lenovo Legion Pro 7",
                    Vendor = "LENOVO",
                    Families = ["Legion"],
                    MachineTypes = ["83DE"],
                    EnabledFeatures = ["lenovo-hardware-controls"]
                }
            ]
        };
        var machineInformation = new MachineInformation
        {
            Vendor = "ACME",
            MachineType = "0000",
            Model = "Generic Laptop"
        };

        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation, catalog);
        var pack = catalog.DevicePacks.SingleOrDefault(devicePack =>
            devicePack.Id.Equals(availability.DevicePackId, StringComparison.OrdinalIgnoreCase));

        pack.Should().BeNull();
    }

    [Fact]
    public void CatalogEvaluation_WithGenericFallbackPack_ShouldReturnBasicPack()
    {
        var catalog = new DeviceSupportCatalog
        {
            DevicePacks =
            [
                new DevicePack
                {
                    Id = "generic-pc-basic",
                    DisplayName = "Generic PC Basic",
                    Vendor = "*",
                    Families = ["Generic PC"],
                    EnabledFeatures = ["plugins", "system-optimization"],
                    HiddenFeatures = ["lenovo-hardware-controls", "power-modes"]
                }
            ]
        };
        var machineInformation = new MachineInformation
        {
            Vendor = "ACME",
            MachineType = "0000",
            Model = "Generic Laptop"
        };

        var availability = LenovoDeviceSupportProvider.Instance.Evaluate(machineInformation, catalog);
        var pack = catalog.DevicePacks.SingleOrDefault(devicePack =>
            devicePack.Id.Equals(availability.DevicePackId, StringComparison.OrdinalIgnoreCase));

        pack.Should().NotBeNull();
        pack!.Id.Should().Be("generic-pc-basic");
    }

    [Fact]
    public void BuildSelectablePacks_ShouldPreferVendorRelatedAndCapListSize()
    {
        var catalog = new DeviceSupportCatalog
        {
            DevicePacks =
            [
                new DevicePack
                {
                    Id = "lenovo-legion-5",
                    DisplayName = "Lenovo Legion 5",
                    Vendor = "LENOVO",
                    ModelKeywords = ["Legion 5"],
                    MachineTypes = ["83F0"],
                    EnabledFeatures = ["lenovo-hardware-controls"]
                },
                new DevicePack
                {
                    Id = "lenovo-loq",
                    DisplayName = "Lenovo LOQ",
                    Vendor = "LENOVO",
                    ModelKeywords = ["LOQ"],
                    MachineTypes = ["83H0"],
                    EnabledFeatures = ["lenovo-hardware-controls"]
                },
                new DevicePack
                {
                    Id = "dell-basic",
                    DisplayName = "Dell Basic",
                    Vendor = "Dell Inc.",
                    VendorAliases = ["Dell"],
                    EnabledFeatures = ["plugins"],
                    HiddenFeatures = ["lenovo-hardware-controls"]
                },
                new DevicePack
                {
                    Id = "hp-basic",
                    DisplayName = "HP Basic",
                    Vendor = "HP",
                    EnabledFeatures = ["plugins"],
                    HiddenFeatures = ["lenovo-hardware-controls"]
                },
                .. Enumerable.Range(0, 40).Select(i => new DevicePack
                {
                    Id = $"other-basic-{i}",
                    DisplayName = $"Other Basic {i:D2}",
                    Vendor = "Other",
                    EnabledFeatures = ["plugins"],
                    HiddenFeatures = ["lenovo-hardware-controls"]
                })
            ]
        };

        var machine = new MachineInformation
        {
            Vendor = "LENOVO",
            MachineType = "83F0",
            Model = "Legion 5 15IRX10"
        };

        var selectable = StartupDeviceSetupCoordinator.BuildSelectablePacks(catalog, machine);

        selectable.Select(p => p.Id).Should().Contain(["lenovo-legion-5", "lenovo-loq"]);
        selectable.Should().OnlyContain(p => p.Id != null);
        // Related Lenovo packs first; remaining basic list is capped so combo stays usable.
        selectable.Count.Should().BeLessThan(catalog.DevicePacks.Count);
        selectable.Count.Should().BeLessThanOrEqualTo(2 + 12 + 24);
        selectable.Take(2).Select(p => p.Id).Should().BeEquivalentTo(["lenovo-legion-5", "lenovo-loq"]);
    }

    [Fact]
    public void PreferredDevicePack_ShouldOverrideAutoDetectUntilCleared()
    {
        var provider = LenovoDeviceSupportProvider.Instance;
        provider.SetInstalledCatalog(null);
        provider.SetPreferredDevicePackId(null);

        var machine = new MachineInformation
        {
            Vendor = "LENOVO",
            MachineType = "83DF",
            Model = "Legion Y9000P IRX9"
        };

        var auto = provider.Evaluate(machine);
        auto.DevicePackId.Should().Be("lenovo-legion-pro-5");
        auto.IsSupported.Should().BeTrue();

        provider.SetPreferredDevicePackId(CatalogDeviceSupportProvider.GenericBasicPackId);
        var forcedBasic = provider.Evaluate(machine);
        forcedBasic.DevicePackId.Should().Be(CatalogDeviceSupportProvider.GenericBasicPackId);
        forcedBasic.IsBasicMode.Should().BeTrue();

        provider.SetPreferredDevicePackId("lenovo-loq");
        var forcedLoq = provider.Evaluate(machine);
        forcedLoq.DevicePackId.Should().Be("lenovo-loq");
        forcedLoq.IsSupported.Should().BeTrue();

        provider.SetPreferredDevicePackId(null);
        var restored = provider.Evaluate(machine);
        restored.DevicePackId.Should().Be("lenovo-legion-pro-5");
    }
}
