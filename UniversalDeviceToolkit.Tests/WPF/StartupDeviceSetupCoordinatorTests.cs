using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.DeviceSupport;
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
}
