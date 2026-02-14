using System;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers.Sensors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LenovoLegionToolkit.Tests.Controllers;

[TestClass]
[TestCategory(TestCategories.Controller)]
public class SensorsDataTests : UnitTestBase
{
    [TestMethod]
    public void SensorsData_Empty_ShouldHaveEmptyComponents()
    {
        var empty = SensorsData.Empty;

        empty.CPU.Should().Be(SensorData.Empty);
        empty.GPU.Should().Be(SensorData.Empty);
    }

    [TestMethod]
    public void SensorsData_WithValidData_ShouldSetPropertiesCorrectly()
    {
        var cpuData = new SensorData(
            utilization: 50,
            maxUtilization: 100,
            coreClock: 3000,
            maxCoreClock: 4000,
            memoryClock: 0,
            maxMemoryClock: 0,
            temperature: 65,
            maxTemperature: 100,
            wattage: 45,
            voltage: 1.2,
            fanSpeed: 3000,
            maxFanSpeed: 5000);

        var gpuData = new SensorData(
            utilization: 80,
            maxUtilization: 100,
            coreClock: 1500,
            maxCoreClock: 2000,
            memoryClock: 6000,
            maxMemoryClock: 8000,
            temperature: 70,
            maxTemperature: 95,
            wattage: 100,
            voltage: 1.0,
            fanSpeed: 2500,
            maxFanSpeed: 5000);

        var sensorsData = new SensorsData(cpuData, gpuData);

        sensorsData.CPU.Should().Be(cpuData);
        sensorsData.GPU.Should().Be(gpuData);
    }

    [TestMethod]
    public void SensorsData_ToString_ShouldContainCPUAndGPU()
    {
        var data = new SensorsData(SensorData.Empty, SensorData.Empty);
        var str = data.ToString();

        str.Should().Contain("CPU");
        str.Should().Contain("GPU");
    }
}

[TestClass]
[TestCategory(TestCategories.Controller)]
public class SensorDataTests : UnitTestBase
{
    [TestMethod]
    public void SensorData_Empty_ShouldHaveZeroValues()
    {
        var empty = SensorData.Empty;

        empty.Utilization.Should().Be(0);
        empty.CoreClock.Should().Be(0);
        empty.Temperature.Should().Be(0);
        empty.FanSpeed.Should().Be(0);
        empty.Wattage.Should().Be(0);
    }

    [TestMethod]
    public void SensorData_WithAllParameters_ShouldSetPropertiesCorrectly()
    {
        var data = new SensorData(
            utilization: 75,
            maxUtilization: 100,
            coreClock: 3500,
            maxCoreClock: 4500,
            memoryClock: 0,
            maxMemoryClock: 0,
            temperature: 72,
            maxTemperature: 100,
            wattage: 55,
            voltage: 1.25,
            fanSpeed: 3500,
            maxFanSpeed: 5500);

        data.Utilization.Should().Be(75);
        data.MaxUtilization.Should().Be(100);
        data.CoreClock.Should().Be(3500);
        data.MaxCoreClock.Should().Be(4500);
        data.Temperature.Should().Be(72);
        data.MaxTemperature.Should().Be(100);
        data.Wattage.Should().Be(55);
        data.Voltage.Should().Be(1.25);
        data.FanSpeed.Should().Be(3500);
        data.MaxFanSpeed.Should().Be(5500);
    }

    [TestMethod]
    public void SensorData_ToString_ShouldContainKeyMetrics()
    {
        var data = new SensorData(
            utilization: 50,
            maxUtilization: 100,
            coreClock: 3000,
            maxCoreClock: 4000,
            memoryClock: 0,
            maxMemoryClock: 0,
            temperature: 65,
            maxTemperature: 100,
            wattage: 45,
            voltage: 1.2,
            fanSpeed: 3000,
            maxFanSpeed: 5000);

        var str = data.ToString();

        str.Should().Contain("50%");
        str.Should().Contain("3000MHz");
        str.Should().Contain("65C");
        str.Should().Contain("3000RPM");
        str.Should().Contain("45W");
    }

    [TestMethod]
    public void SensorData_WithExtendedParameters_ShouldSetExtendedProperties()
    {
        var data = new SensorData(
            utilization: 50,
            maxUtilization: 100,
            coreClock: 3000,
            maxCoreClock: 4000,
            memoryClock: 0,
            maxMemoryClock: 0,
            temperature: 65,
            maxTemperature: 100,
            wattage: 45,
            voltage: 1.2,
            fanSpeed: 3000,
            maxFanSpeed: 5000);

        data.Utilization.Should().Be(50);
        data.Temperature.Should().Be(65);
        data.Wattage.Should().Be(45);
        data.Voltage.Should().Be(1.2);
    }
}

[TestClass]
[TestCategory(TestCategories.Controller)]
public class ISensorsControllerTests : UnitTestBase
{
    [TestMethod]
    public void ISensorsController_ShouldHaveCorrectMethods()
    {
        var methodNames = new[]
        {
            nameof(ISensorsController.IsSupportedAsync),
            nameof(ISensorsController.PrepareAsync),
            nameof(ISensorsController.GetDataAsync),
            nameof(ISensorsController.GetFanSpeedsAsync),
            nameof(ISensorsController.Dispose)
        };

        foreach (var methodName in methodNames)
        {
            typeof(ISensorsController).GetMethod(methodName).Should().NotBeNull();
        }
    }

    [TestMethod]
    public async Task ISensorsController_GetDataAsync_ShouldHaveDefaultParameter()
    {
        var method = typeof(ISensorsController).GetMethod("GetDataAsync");
        var parameters = method!.GetParameters();

        parameters.Should().HaveCount(1);
        parameters[0].HasDefaultValue.Should().BeTrue();
        parameters[0].DefaultValue.Should().Be(false);
    }
}
