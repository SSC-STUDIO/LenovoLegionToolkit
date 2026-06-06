using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Controllers.Sensors;
using LenovoLegionToolkit.Lib.Utils;
using Moq;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Controllers;

[Trait("Category", TestCategories.Controller)]
public class SensorsDataTests : UnitTestBase
{
    [Fact]
    public void SensorsData_Empty_ShouldHaveEmptyComponents()
    {
        var empty = SensorsData.Empty;

        empty.CPU.Should().Be(SensorData.Empty);
        empty.GPU.Should().Be(SensorData.Empty);
    }

    [Fact]
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

    [Fact]
    public void SensorsData_ToString_ShouldContainCPUAndGPU()
    {
        var data = new SensorsData(SensorData.Empty, SensorData.Empty);
        var str = data.ToString();

        str.Should().Contain("CPU");
        str.Should().Contain("GPU");
    }
}


[Trait("Category", TestCategories.Controller)]
public class SensorDataTests : UnitTestBase
{
    [Fact]
    public void SensorData_Empty_ShouldHaveZeroValues()
    {
        var empty = SensorData.Empty;

        empty.Utilization.Should().Be(-1);
        empty.CoreClock.Should().Be(-1);
        empty.Temperature.Should().Be(-1);
        empty.FanSpeed.Should().Be(-1);
        empty.Wattage.Should().Be(-1);
    }

    [Fact]
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

    [Fact]
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

    [Fact]
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


[Trait("Category", TestCategories.Controller)]
public class ISensorsControllerTests : UnitTestBase
{
    [Fact]
    public void ISensorsController_ShouldHaveCorrectMethods()
    {
        var methodNames = new[]
        {
            nameof(ISensorsController.IsSupportedAsync),
            nameof(ISensorsController.PrepareAsync),
            nameof(ISensorsController.GetDataAsync),
            nameof(ISensorsController.GetFanSpeedsAsync)
        };

        foreach (var methodName in methodNames)
        {
            typeof(ISensorsController).GetMethod(methodName).Should().NotBeNull();
        }

        typeof(IDisposable).IsAssignableFrom(typeof(ISensorsController)).Should().BeTrue();
    }

    [Fact]
    public async Task ISensorsController_GetDataAsync_ShouldHaveDefaultParameter()
    {
        var method = typeof(ISensorsController).GetMethod("GetDataAsync");
        var parameters = method!.GetParameters();

        parameters.Should().HaveCount(1);
        parameters[0].HasDefaultValue.Should().BeTrue();
        parameters[0].DefaultValue.Should().Be(false);
    }
}


[Trait("Category", TestCategories.Controller)]
public class GenericSensorsControllerTests : UnitTestBase
{
    private class FallbackSensorsController : AbstractSensorsController
    {
        private readonly int _cpuUtilization;
        private readonly int _cpuTemperature;
        private readonly int _cpuCoreClock;
        private readonly int _cpuWattage;
        private readonly double _cpuVoltage;
        private readonly int _gpuUtilization;
        private readonly int _gpuTemperature;
        private readonly int _gpuCoreClock;
        private readonly int _gpuMemoryClock;
        private readonly int _gpuWattage;
        private readonly double _gpuVoltage;

        internal FallbackSensorsController(
            GPUController gpuController,
            int cpuUtilization,
            int cpuTemperature,
            int cpuCoreClock,
            int gpuUtilization,
            int gpuTemperature,
            int gpuCoreClock,
            int gpuMemoryClock = -1,
            int cpuWattage = 0,
            double cpuVoltage = 0,
            int gpuWattage = 0,
            double gpuVoltage = 0)
            : base(gpuController)
        {
            _cpuUtilization = cpuUtilization;
            _cpuTemperature = cpuTemperature;
            _cpuCoreClock = cpuCoreClock;
            _cpuWattage = cpuWattage;
            _cpuVoltage = cpuVoltage;
            _gpuUtilization = gpuUtilization;
            _gpuTemperature = gpuTemperature;
            _gpuCoreClock = gpuCoreClock;
            _gpuMemoryClock = gpuMemoryClock;
            _gpuWattage = gpuWattage;
            _gpuVoltage = gpuVoltage;
        }

        public override Task<bool> IsSupportedAsync() => Task.FromResult(true);

        protected override Task<int> GetCpuCurrentTemperatureAsync() => Task.FromResult(-1);
        protected override Task<int> GetGpuCurrentTemperatureAsync() => Task.FromResult(-1);
        protected override Task<int> GetCpuCurrentFanSpeedAsync() => Task.FromResult(-1);
        protected override Task<int> GetGpuCurrentFanSpeedAsync() => Task.FromResult(-1);
        protected override Task<int> GetCpuMaxFanSpeedAsync() => Task.FromResult(-1);
        protected override Task<int> GetGpuMaxFanSpeedAsync() => Task.FromResult(-1);
        protected override Task<int> GetCpuMaxCoreClockAsync() => Task.FromResult(5000);
        protected override int GetCpuUtilization(int maxUtilization) => -1;
        protected override int GetCpuCoreClock() => -1;
        protected override Task<GPUInfo> GetGPUInfoAsync() => Task.FromResult(GPUInfo.Empty);
        protected override Task<LibreHardwareMonitorReadings?> GetLibreHardwareMonitorReadingsAsync() =>
            Task.FromResult<LibreHardwareMonitorReadings?>(new LibreHardwareMonitorReadings(
                _cpuUtilization,
                _cpuTemperature,
                _cpuCoreClock,
                _cpuWattage,
                _cpuVoltage,
                _gpuUtilization,
                _gpuTemperature,
                _gpuCoreClock,
                _gpuMemoryClock,
                _gpuWattage,
                _gpuVoltage));
    }

    private sealed class CpuWattageFallbackSensorsController(
        GPUController gpuController,
        int performanceCounterWattage,
        int wmiWattage,
        int libreHardwareMonitorWattage)
        : FallbackSensorsController(
            gpuController,
            cpuUtilization: -1,
            cpuTemperature: -1,
            cpuCoreClock: -1,
            gpuUtilization: -1,
            gpuTemperature: -1,
            gpuCoreClock: -1)
    {
        protected override int GetCpuWattageFromPerformanceCounter() => performanceCounterWattage;

        protected override Task<int> GetCpuWattageFromWmiAsync() => Task.FromResult(wmiWattage);

        protected override Task<int> GetCpuWattageFromLibreHardwareMonitorAsync() =>
            Task.FromResult(libreHardwareMonitorWattage);
    }

    private sealed class TestableGenericSensorsController(
        GPUController gpuController,
        Func<Task<bool>> canReadSnapshot,
        IDelayProvider? delayProvider = null)
        : GenericSensorsController(gpuController, delayProvider)
    {
        protected override Task<bool> CanReadGenericSnapshotAsyncCore() => canReadSnapshot();
    }

    [Fact]
    public void GenericSensorsController_ShouldBeAssignableToSensorsController()
    {
        var gpuController = new GPUController(new Mock<IGPUProcessManager>().Object, new Mock<IGPUHardwareManager>().Object);
        var controller = new GenericSensorsController(gpuController);

        controller.Should().BeAssignableTo<ISensorsController>();
    }

    [Fact]
    public async Task GenericSensorsController_GetDataAsync_ShouldNotThrowWhenVendorSensorsAreUnavailable()
    {
        var gpuController = new GPUController(new Mock<IGPUProcessManager>().Object, new Mock<IGPUHardwareManager>().Object);
        using var controller = new GenericSensorsController(gpuController);

        var act = () => controller.GetDataAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GenericSensorsController_IsSupportedAsync_ShouldAllowFallbackOnSupportedMachines()
    {
        var gpuController = new GPUController(new Mock<IGPUProcessManager>().Object, new Mock<IGPUHardwareManager>().Object);
        using var controller = new TestableGenericSensorsController(gpuController, () => Task.FromResult(true));

        var supported = await controller.IsSupportedAsync();

        supported.Should().BeTrue();
    }

    [Fact]
    public async Task GenericSensorsController_IsSupportedAsync_ShouldRetryWhenFirstSnapshotHasNoData()
    {
        var gpuController = new GPUController(new Mock<IGPUProcessManager>().Object, new Mock<IGPUHardwareManager>().Object);
        var delayProvider = new Mock<IDelayProvider>();
        delayProvider
            .Setup(provider => provider.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var attempts = 0;
        using var controller = new TestableGenericSensorsController(
            gpuController,
            () => Task.FromResult(Interlocked.Increment(ref attempts) > 1),
            delayProvider.Object);

        var supported = await controller.IsSupportedAsync();

        supported.Should().BeTrue();
        attempts.Should().Be(2);
        delayProvider.Verify(provider => provider.Delay(TimeSpan.FromMilliseconds(250), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AbstractSensorsController_GetDataAsync_ShouldUseLibreHardwareMonitorFallbackForSummaryMetrics()
    {
        var gpuController = new GPUController(new Mock<IGPUProcessManager>().Object, new Mock<IGPUHardwareManager>().Object);
        using var controller = new FallbackSensorsController(
            gpuController,
            cpuUtilization: 37,
            cpuTemperature: 71,
            cpuCoreClock: 4123,
            gpuUtilization: 84,
            gpuTemperature: 67,
            gpuCoreClock: 1785,
            gpuMemoryClock: 9200);

        var data = await controller.GetDataAsync();

        data.CPU.Utilization.Should().Be(37);
        data.CPU.Temperature.Should().Be(71);
        data.CPU.CoreClock.Should().Be(4123);
        data.GPU.Utilization.Should().Be(84);
        data.GPU.Temperature.Should().Be(67);
        data.GPU.CoreClock.Should().Be(1785);
        data.GPU.MemoryClock.Should().Be(9200);
        data.GPU.MaxCoreClock.Should().Be(1785);
        data.GPU.MaxMemoryClock.Should().Be(9200);
    }

    [Fact]
    public async Task AbstractSensorsController_GetDataAsync_ShouldContinueCpuWattageFallbackWhenWmiReturnsZero()
    {
        var gpuController = new GPUController(new Mock<IGPUProcessManager>().Object, new Mock<IGPUHardwareManager>().Object);
        using var controller = new CpuWattageFallbackSensorsController(
            gpuController,
            performanceCounterWattage: -1,
            wmiWattage: 0,
            libreHardwareMonitorWattage: 42);

        var data = await controller.GetDataAsync(detailed: true);

        data.CPU.Wattage.Should().Be(42);
    }

    [Fact]
    public void IoCModule_ShouldResolveSensorsControllerWithGenericFallback()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<LenovoLegionToolkit.Lib.IoCModule>();
        builder.RegisterModule<TestIoCModule>();

        using var container = builder.Build();

        var controller = container.Resolve<ISensorsController>();
        var genericController = container.Resolve<GenericSensorsController>();

        controller.Should().BeOfType<SensorsController>();
        genericController.Should().NotBeNull();
    }
}
