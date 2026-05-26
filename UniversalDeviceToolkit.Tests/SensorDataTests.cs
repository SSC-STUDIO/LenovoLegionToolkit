using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class SensorDataTests
{
    #region Full Constructor

    [Fact]
    public void FullConstructor_ShouldSetAllProperties()
    {
        var data = new SensorData(
            utilization: 50, maxUtilization: 100,
            coreClock: 3000, maxCoreClock: 5000,
            memoryClock: 1200, maxMemoryClock: 2000,
            temperature: 70, maxTemperature: 100,
            wattage: 65, voltage: 1.2,
            fanSpeed: 2500, maxFanSpeed: 5000);

        data.Utilization.Should().Be(50);
        data.MaxUtilization.Should().Be(100);
        data.CoreClock.Should().Be(3000);
        data.MaxCoreClock.Should().Be(5000);
        data.MemoryClock.Should().Be(1200);
        data.MaxMemoryClock.Should().Be(2000);
        data.Temperature.Should().Be(70);
        data.MaxTemperature.Should().Be(100);
        data.Wattage.Should().Be(65);
        data.Voltage.Should().Be(1.2);
        data.FanSpeed.Should().Be(2500);
        data.MaxFanSpeed.Should().Be(5000);
    }

    #endregion

    #region Short Constructor (no wattage/voltage)

    [Fact]
    public void ShortConstructor_ShouldDefaultWattageToNegativeOneAndVoltageToZero()
    {
        var data = new SensorData(
            utilization: 50, maxUtilization: 100,
            coreClock: 3000, maxCoreClock: 5000,
            memoryClock: 1200, maxMemoryClock: 2000,
            temperature: 70, maxTemperature: 100,
            fanSpeed: 2500, maxFanSpeed: 5000);

        data.Wattage.Should().Be(-1);
        data.Voltage.Should().Be(0);
    }

    #endregion

    #region Empty

    [Fact]
    public void Empty_ShouldHaveAllNegativeOneExceptVoltage()
    {
        var empty = SensorData.Empty;
        empty.Utilization.Should().Be(-1);
        empty.MaxUtilization.Should().Be(-1);
        empty.CoreClock.Should().Be(-1);
        empty.MaxCoreClock.Should().Be(-1);
        empty.MemoryClock.Should().Be(-1);
        empty.MaxMemoryClock.Should().Be(-1);
        empty.Temperature.Should().Be(-1);
        empty.MaxTemperature.Should().Be(-1);
        empty.Wattage.Should().Be(-1);
        empty.Voltage.Should().Be(0);
        empty.FanSpeed.Should().Be(-1);
        empty.MaxFanSpeed.Should().Be(-1);
    }

    #endregion

    #region WithMinMax

    [Fact]
    public void WithMinMax_ShouldPreserveOriginalFields()
    {
        var original = new SensorData(
            utilization: 50, maxUtilization: 100,
            coreClock: 3000, maxCoreClock: 5000,
            memoryClock: 1200, maxMemoryClock: 2000,
            temperature: 70, maxTemperature: 100,
            wattage: 65, voltage: 1.2,
            fanSpeed: 2500, maxFanSpeed: 5000);

        var result = original.WithMinMax(0.5, 1.5, 40, 90);

        result.Utilization.Should().Be(50);
        result.CoreClock.Should().Be(3000);
        result.Temperature.Should().Be(70);
        result.Wattage.Should().Be(65);
        result.Voltage.Should().Be(1.2);
        result.FanSpeed.Should().Be(2500);
    }

    [Fact]
    public void WithMinMax_ShouldSetMinMaxFields()
    {
        var original = new SensorData(
            utilization: 50, maxUtilization: 100,
            coreClock: 3000, maxCoreClock: 5000,
            memoryClock: 1200, maxMemoryClock: 2000,
            temperature: 70, maxTemperature: 100,
            wattage: 65, voltage: 1.2,
            fanSpeed: 2500, maxFanSpeed: 5000);

        var result = original.WithMinMax(0.5, 1.5, 40, 90);

        result.MinVoltage.Should().Be(0.5);
        result.MaxVoltage.Should().Be(1.5);
        result.MinTemperature.Should().Be(40);
        result.MaxTemperatureRecord.Should().Be(90);
    }

    [Fact]
    public void WithMinMax_DefaultValuesBeforeCall_ShouldBeZero()
    {
        var original = new SensorData(
            utilization: 50, maxUtilization: 100,
            coreClock: 3000, maxCoreClock: 5000,
            memoryClock: 1200, maxMemoryClock: 2000,
            temperature: 70, maxTemperature: 100,
            wattage: 65, voltage: 1.2,
            fanSpeed: 2500, maxFanSpeed: 5000);

        original.MinVoltage.Should().Be(0);
        original.MaxVoltage.Should().Be(0);
        original.MinTemperature.Should().Be(0);
        original.MaxTemperatureRecord.Should().Be(0);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ShouldContainKeyMetrics()
    {
        var data = new SensorData(
            utilization: 50, maxUtilization: 100,
            coreClock: 3000, maxCoreClock: 5000,
            memoryClock: 1200, maxMemoryClock: 2000,
            temperature: 70, maxTemperature: 100,
            wattage: 65, voltage: 1.2,
            fanSpeed: 2500, maxFanSpeed: 5000);

        var s = data.ToString();
        s.Should().Contain("50%");
        s.Should().Contain("3000MHz");
        s.Should().Contain("70C");
        s.Should().Contain("2500RPM");
        s.Should().Contain("65W");
        s.Should().Contain("1.2V");
    }

    #endregion
}

[Trait("Category", TestCategories.Unit)]
public class SensorsDataTests
{
    [Fact]
    public void Empty_ShouldContainTwoEmptySensorData()
    {
        var empty = SensorsData.Empty;
        empty.CPU.Should().Be(SensorData.Empty);
        empty.GPU.Should().Be(SensorData.Empty);
    }

    [Fact]
    public void ToString_ShouldContainCPUAndGPU()
    {
        var data = new SensorsData(SensorData.Empty, SensorData.Empty);
        var s = data.ToString();
        s.Should().Contain("CPU");
        s.Should().Contain("GPU");
    }
}
