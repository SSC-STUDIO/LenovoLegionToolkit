using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading; 
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Optimization;
using Moq;
using Xunit;

namespace LenovoLegionToolkit.Tests;

public class WindowsOptimizationServiceTests
{
    [Fact]
    public void GetCategories_ShouldReturnNonEmptyList()
    {
        // Arrange & Act
        var service = new WindowsOptimizationService(new WindowsCleanupService(new TestApplicationSettings()));
        var categories = service.GetCategories();
        
        // Assert
        categories.Should().NotBeEmpty();
        categories.SelectMany(c => c.Actions).Should().NotBeEmpty();
    }

    [Fact]
    public void GetCategories_ShouldContainExpectedCategories()
    {
        // Arrange & Act
        var service = new WindowsOptimizationService(new WindowsCleanupService(new TestApplicationSettings()));
        var categories = service.GetCategories();
        var categoryKeys = categories.Select(c => c.Key).ToList();
        
        // Assert
        categoryKeys.Should().Contain("explorer");
        categoryKeys.Should().Contain("performance");
        categoryKeys.Should().Contain("services");
        categoryKeys.Should().Contain("network");
        categoryKeys.Should().Contain("cleanup.cache");
    }

    [Fact]
    public async Task EstimateCleanupSizeAsync_ShouldReturnNonZeroForValidActionKey()
    {
        // Arrange
        var service = new WindowsOptimizationService(new WindowsCleanupService(new TestApplicationSettings()));
        var validActionKey = "cleanup.tempFiles"; // Use a valid action key instead of category key
        
        // Act
        var result = await service.EstimateCleanupSizeAsync(new[] { validActionKey }, CancellationToken.None);
        
        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetAllActionKeys_ShouldReturnExpectedKeys()
    {
        // Arrange
        var service = new WindowsOptimizationService(new WindowsCleanupService(new TestApplicationSettings()));
        
        // Act
        var allActionKeys = service.GetCategories()
            .SelectMany(c => c.Actions)
            .Select(a => a.Key)
            .ToList();
        
        // Assert
        allActionKeys.Should().NotBeEmpty();
        
        // Check for critical action keys that should exist based on the provider
        allActionKeys.Should().Contain("explorer.taskbar");
        allActionKeys.Should().Contain("performance.powerPlan");
        allActionKeys.Should().Contain("services.diagnostics");
        allActionKeys.Should().Contain("network.acceleration");
        allActionKeys.Should().Contain("cleanup.tempFiles");
        allActionKeys.Should().Contain("cleanup.custom");
        
        // Verify expected categories are represented by their actions
        allActionKeys.Should().Contain(k => k.StartsWith("explorer."));
        allActionKeys.Should().Contain(k => k.StartsWith("performance."));
        allActionKeys.Should().Contain(k => k.StartsWith("services."));
        allActionKeys.Should().Contain(k => k.StartsWith("network."));
        allActionKeys.Should().Contain(k => k.StartsWith("cleanup."));
    }

    [Fact]
    public async Task ApplyPerformanceOptimizationsAsync_ShouldNotThrow()
    {
        // Arrange
        var service = new WindowsOptimizationService(new WindowsCleanupService(new TestApplicationSettings()));
        
        // Act & Assert - 这个方法会尝试执行推荐的性能优化，但在测试环境中可能无法实际执行
        // 我们只验证它不会抛出异常
        await service.Invoking(s => s.ApplyPerformanceOptimizationsAsync(CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task RunCleanupAsync_ShouldNotThrow()
    {
        // Arrange
        var service = new WindowsOptimizationService(new WindowsCleanupService(new TestApplicationSettings()));
        
        // Act & Assert - 这个方法会尝试运行清理操作，但在测试环境中可能无法实际执行
        await service.Invoking(s => s.RunCleanupAsync(CancellationToken.None))
            .Should().NotThrowAsync();
    }
}
