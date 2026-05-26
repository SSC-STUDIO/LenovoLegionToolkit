using System;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class BatteryInformationTests
{
    #region BatteryHealth Tests

    [Fact]
    public void BatteryHealth_WhenDesignCapacityPositive_ShouldCalculatePercentage()
    {
        var info = new BatteryInformation(false, 100, 0, 0, 0, 0, 0, 0, 1000, 800, 0, false, null, null, null, null);
        info.BatteryHealth.Should().Be(80.0);
    }

    [Fact]
    public void BatteryHealth_WhenFullChargeEqualsDesign_ShouldReturn100()
    {
        var info = new BatteryInformation(false, 100, 0, 0, 0, 0, 0, 0, 5000, 5000, 0, false, null, null, null, null);
        info.BatteryHealth.Should().Be(100.0);
    }

    [Fact]
    public void BatteryHealth_WhenDesignCapacityZero_ShouldReturnZero()
    {
        var info = new BatteryInformation(false, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, null, null, null, null);
        info.BatteryHealth.Should().Be(0.0);
    }

    [Fact]
    public void BatteryHealth_ShouldRoundToTwoDecimals()
    {
        // 45033 / 50000 = 90.066 â†?90.07
        var info = new BatteryInformation(false, 100, 0, 0, 0, 0, 0, 0, 50000, 45033, 0, false, null, null, null, null);
        info.BatteryHealth.Should().Be(90.07);
    }

    #endregion

    #region WithAvgTemp Tests

    [Fact]
    public void WithAvgTemp_ShouldPreserveAllOriginalFields()
    {
        var original = new BatteryInformation(true, 75, 120, 240, -1500, -1800, -1200, 75, 5000, 4500, 300, false, 35.5, DateTime.MinValue, DateTime.MaxValue, "TestModel");
        var updated = original.WithAvgTemp(42.0);

        updated.IsCharging.Should().Be(original.IsCharging);
        updated.BatteryPercentage.Should().Be(original.BatteryPercentage);
        updated.BatteryLifeRemaining.Should().Be(original.BatteryLifeRemaining);
        updated.DesignCapacity.Should().Be(original.DesignCapacity);
        updated.FullChargeCapacity.Should().Be(original.FullChargeCapacity);
        updated.CycleCount.Should().Be(original.CycleCount);
        updated.BatteryTemperatureC.Should().Be(original.BatteryTemperatureC);
        updated.ModelName.Should().Be(original.ModelName);
    }

    [Fact]
    public void WithAvgTemp_ShouldSetNewTemperature()
    {
        var original = BatteryInformation.Empty;
        var updated = original.WithAvgTemp(42.5);
        updated.AvgTemperatureC.Should().Be(42.5);
    }

    [Fact]
    public void WithAvgTemp_WhenNull_ShouldSetNull()
    {
        var original = BatteryInformation.Empty.WithAvgTemp(42.5);
        var updated = original.WithAvgTemp(null);
        updated.AvgTemperatureC.Should().BeNull();
    }

    #endregion
}
