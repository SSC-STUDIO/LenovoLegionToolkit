using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Controllers.Sensors;

using Moq;
using Xunit;

namespace LenovoLegionToolkit.Tests;

public class AbstractSensorsControllerTests
{
    // 测试子类，实现抽象方法
    private class MockSensorsController : AbstractSensorsController
    {
        public MockSensorsController(GPUController gpuController) : base(gpuController)
        {
        }

        public override Task<bool> IsSupportedAsync() => Task.FromResult(true);
        protected override Task<int> GetCpuCurrentTemperatureAsync() => Task.FromResult(50);
        protected override Task<int> GetGpuCurrentTemperatureAsync() => Task.FromResult(60);
        protected override Task<int> GetCpuCurrentFanSpeedAsync() => Task.FromResult(1000);
        protected override Task<int> GetGpuCurrentFanSpeedAsync() => Task.FromResult(1500);
        protected override Task<int> GetCpuMaxFanSpeedAsync() => Task.FromResult(3000);
        protected override Task<int> GetGpuMaxFanSpeedAsync() => Task.FromResult(4000);
    }

    [Fact]
    public async Task GetDataAsync_ShouldReturnCachedData_WhenCacheIsValid()
    {
        // Arrange
        var mockGPUController = new Mock<GPUController>();
        mockGPUController.Setup(g => g.IsSupported()).Returns(false);
        mockGPUController.Setup(g => g.GetLastKnownStateAsync()).ReturnsAsync(GPUState.Unknown);
        
        var controller = new MockSensorsController(mockGPUController.Object);
        
        // Act - 第一次调用，生成缓存
        var data1 = await controller.GetDataAsync();
        
        // Act - 第二次调用，应该使用缓存
        var data2 = await controller.GetDataAsync();
        
        // Assert
        data1.Should().Be(data2); // 应该返回相同的对象引用（缓存）
        data1.CPU.Temperature.Should().Be(50);
        data1.GPU.Temperature.Should().Be(60);
    }

    [Fact]
    public async Task GetDataAsync_ShouldUpdateCache_WhenCacheExpires()
    {
        // Arrange
        var mockGPUController = new Mock<GPUController>();
        mockGPUController.Setup(g => g.IsSupported()).Returns(false);
        mockGPUController.Setup(g => g.GetLastKnownStateAsync()).ReturnsAsync(GPUState.Unknown);
        
        var controller = new MockSensorsController(mockGPUController.Object);
        
        // Act - 第一次调用，生成缓存
        var data1 = await controller.GetDataAsync();
        
        // Act - 等待缓存过期（100ms）
        await Task.Delay(150);
        
        // Act - 第二次调用，应该更新缓存
        var data2 = await controller.GetDataAsync();
        
        // Assert
        data1.Should().NotBe(data2); // 应该返回不同的对象引用（缓存已更新）
        data2.CPU.Temperature.Should().Be(50);
        data2.GPU.Temperature.Should().Be(60);
    }

    [Fact]
    public async Task CacheAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var mockGPUController = new Mock<GPUController>();
        mockGPUController.Setup(g => g.IsSupported()).Returns(false);
        mockGPUController.Setup(g => g.GetLastKnownStateAsync()).ReturnsAsync(GPUState.Unknown);
        
        var controller = new MockSensorsController(mockGPUController.Object);
        
        // Act - 并发调用GetDataAsync多次
        var tasks = new List<Task<SensorsData>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(controller.GetDataAsync());
        }
        
        var results = await Task.WhenAll(tasks);
        
        // Assert - 所有调用都应该成功，没有异常
        results.Should().NotBeEmpty();
        
        // Assert - 至少有一些调用应该使用缓存（返回相同的对象引用）
        var distinctResults = results.Distinct().Count();
        distinctResults.Should().BeLessThan(results.Length);
    }

    [Fact]
    public async Task FanSpeedsAsync_ShouldReturnFromCache_WhenCacheIsValid()
    {
        // Arrange
        var mockGPUController = new Mock<GPUController>();
        mockGPUController.Setup(g => g.IsSupported()).Returns(false);
        mockGPUController.Setup(g => g.GetLastKnownStateAsync()).ReturnsAsync(GPUState.Unknown);
        
        var controller = new MockSensorsController(mockGPUController.Object);
        
        // Act - 第一次调用GetDataAsync，生成缓存
        await controller.GetDataAsync();
        
        // Act - 调用GetFanSpeedsAsync，应该使用缓存
        var fanSpeeds = await controller.GetFanSpeedsAsync();
        
        // Assert
        fanSpeeds.cpuFanSpeed.Should().Be(1000);
        fanSpeeds.gpuFanSpeed.Should().Be(1500);
    }
}

