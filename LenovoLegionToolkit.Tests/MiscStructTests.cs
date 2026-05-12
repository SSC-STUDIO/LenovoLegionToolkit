using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace LenovoLegionToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class DisplayAdvancedColorInfoTests
{
    [Fact]
    public void Properties_ShouldReflectConstructorValues()
    {
        var info = new DisplayAdvancedColorInfo(
            advancedColorSupported: true,
            advancedColorEnabled: false,
            wideColorEnforced: true,
            advancedColorForceDisabled: false);

        info.AdvancedColorSupported.Should().BeTrue();
        info.AdvancedColorEnabled.Should().BeFalse();
        info.WideColorEnforced.Should().BeTrue();
        info.AdvancedColorForceDisabled.Should().BeFalse();
    }

    [Fact]
    public void Properties_AllFalse_ShouldWork()
    {
        var info = new DisplayAdvancedColorInfo(false, false, false, false);
        info.AdvancedColorSupported.Should().BeFalse();
        info.AdvancedColorEnabled.Should().BeFalse();
        info.WideColorEnforced.Should().BeFalse();
        info.AdvancedColorForceDisabled.Should().BeFalse();
    }

    [Fact]
    public void Properties_AllTrue_ShouldWork()
    {
        var info = new DisplayAdvancedColorInfo(true, true, true, true);
        info.AdvancedColorSupported.Should().BeTrue();
        info.AdvancedColorEnabled.Should().BeTrue();
        info.WideColorEnforced.Should().BeTrue();
        info.AdvancedColorForceDisabled.Should().BeTrue();
    }
}

[Trait("Category", TestCategories.Unit)]
public class BrightnessTests
{
    [Fact]
    public void Value_ShouldReflectConstructorByte()
    {
        var b = new Brightness(128);
        b.Value.Should().Be(128);
    }

    [Fact]
    public void Value_Zero_ShouldWork()
    {
        var b = new Brightness(0);
        b.Value.Should().Be(0);
    }

    [Fact]
    public void Value_MaxByte_ShouldWork()
    {
        var b = new Brightness(255);
        b.Value.Should().Be(255);
    }
}

[Trait("Category", TestCategories.Unit)]
public class RangeCapabilityTests
{
    [Fact]
    public void Properties_ShouldReflectConstructorValues()
    {
        var rc = new RangeCapability(
            id: CapabilityID.IGPUMode,
            defaultValue: 50,
            min: 0,
            max: 100,
            step: 5);

        rc.Id.Should().Be(CapabilityID.IGPUMode);
        rc.DefaultValue.Should().Be(50);
        rc.Min.Should().Be(0);
        rc.Max.Should().Be(100);
        rc.Step.Should().Be(5);
    }
}

[Trait("Category", TestCategories.Unit)]
public class DiscreteCapabilityTests
{
    [Fact]
    public void Properties_ShouldReflectConstructorValues()
    {
        var dc = new DiscreteCapability(id: CapabilityID.OverDrive, value: 42);
        dc.Id.Should().Be(CapabilityID.OverDrive);
        dc.Value.Should().Be(42);
    }
}
