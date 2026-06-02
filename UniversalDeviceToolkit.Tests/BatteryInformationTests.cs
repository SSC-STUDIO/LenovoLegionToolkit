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
        // 45033 / 50000 = 90.066 -> 90.07
        var info = new BatteryInformation(false, 100, 0, 0, 0, 0, 0, 0, 50000, 45033, 0, false, null, null, null, null);
        info.BatteryHealth.Should().Be(90.07);
    }

    #endregion
}
