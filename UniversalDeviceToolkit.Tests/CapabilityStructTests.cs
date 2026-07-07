using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class CapabilityStructTests
{
    #region DiscreteCapability Tests

    [Fact]
    public void DiscreteCapability_Constructor_ShouldSetIdAndValue()
    {
        var cap = new DiscreteCapability(CapabilityID.IGPUMode, 42);
        cap.Id.Should().Be(CapabilityID.IGPUMode);
        cap.Value.Should().Be(42);
    }

    [Fact]
    public void DiscreteCapability_Default_ShouldHaveDefaults()
    {
        var cap = new DiscreteCapability();
        cap.Id.Should().Be(default);
        cap.Value.Should().Be(0);
    }

    [Fact]
    public void DiscreteCapability_NegativeValue_ShouldBeAccepted()
    {
        var cap = new DiscreteCapability(CapabilityID.IGPUMode, -10);
        cap.Value.Should().Be(-10);
    }

    #endregion

    #region RangeCapability Tests

    [Fact]
    public void RangeCapability_Constructor_ShouldSetAllFields()
    {
        var range = new RangeCapability(CapabilityID.IGPUMode, 50, 0, 100, 5);
        range.Id.Should().Be(CapabilityID.IGPUMode);
        range.DefaultValue.Should().Be(50);
        range.Min.Should().Be(0);
        range.Max.Should().Be(100);
        range.Step.Should().Be(5);
    }

    [Fact]
    public void RangeCapability_Default_ShouldHaveDefaults()
    {
        var range = new RangeCapability();
        range.Id.Should().Be(default);
        range.DefaultValue.Should().Be(0);
        range.Min.Should().Be(0);
        range.Max.Should().Be(0);
        range.Step.Should().Be(0);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(50, 0, 100)]
    [InlineData(0, -50, 50)]
    public void RangeCapability_BoundaryValues_ShouldWork(int def, int min, int max)
    {
        var range = new RangeCapability(CapabilityID.IGPUMode, def, min, max, 1);
        range.DefaultValue.Should().Be(def);
        range.Min.Should().Be(min);
        range.Max.Should().Be(max);
    }

    #endregion
}

