using System;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class DeviceStructTests
{
    [Fact]
    public void Properties_ShouldReflectConstructorValues()
    {
        var guid = Guid.NewGuid();
        var device = new Device(
            "TestDevice", "A test", "BusDesc", "DEV\\123", guid, "Display",
            true, false);

        device.Name.Should().Be("TestDevice");
        device.Description.Should().Be("A test");
        device.BusReportedDeviceDescription.Should().Be("BusDesc");
        device.DeviceInstanceId.Should().Be("DEV\\123");
        device.ClassGuid.Should().Be(guid);
        device.ClassName.Should().Be("Display");
        device.IsRemovable.Should().BeTrue();
        device.IsDisconnected.Should().BeFalse();
    }

    [Fact]
    public void Index_ShouldConcatenateAllFields()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        var device = new Device(
            "Name", "Desc", "Bus", "Instance", guid, "Class",
            false, true);

        var index = device.Index;

        index.Should().Contain("Class");
        index.Should().Contain("12345678");
        index.Should().Contain("Bus");
        index.Should().Contain("Desc");
        index.Should().Contain("Name");
        index.Should().Contain("Instance");
    }

    [Fact]
    public void Index_ShouldBeLazyAndCached()
    {
        var device = new Device("N", "D", "B", "I",
            Guid.NewGuid(), "C", false, false);

        var first = device.Index;
        var second = device.Index;

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Index_DifferentDevices_ShouldProduceDifferentStrings()
    {
        var d1 = new Device("A", "B", "C", "D",
            Guid.NewGuid(), "E", false, false);
        var d2 = new Device("X", "Y", "Z", "W",
            Guid.NewGuid(), "Q", false, false);

        d1.Index.Should().NotBe(d2.Index);
    }

    [Fact]
    public void Index_EmptyStrings_ShouldStillProduceString()
    {
        var device = new Device("", "", "", "",
            Guid.Empty, "", false, false);

        var index = device.Index;
        index.Should().NotBeNull();
        index.Should().BeOfType<string>();
    }
}
