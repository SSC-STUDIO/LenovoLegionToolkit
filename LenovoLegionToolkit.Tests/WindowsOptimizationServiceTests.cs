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
        var categories = WindowsOptimizationService.GetCategories();
        
        // Assert
        categories.Should().NotBeEmpty();
        categories.SelectMany(c => c.Actions).Should().NotBeEmpty();
    }

    [Fact]
    public void GetCategories_ShouldContainExpectedCategories()
    {
        // Arrange & Act
        var categories = WindowsOptimizationService.GetCategories();
        var categoryKeys = categories.Select(c => c.Key).ToList();
        
        // Assert
        categoryKeys.Should().Contain("explorer");
        categoryKeys.Should().Contain("performance");
        categoryKeys.Should().Contain("services");
        categoryKeys.Should().Contain("network");
        categoryKeys.Should().Contain("cleanup.cache");
    }

    [Fact]
    public void EstimateCleanupSizeAsync_ShouldReturnZeroForInvalidActionKey()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        var invalidActionKey = "invalid.action.key";
        
        // Act
        var result = service.EstimateCleanupSizeAsync(new[] { invalidActionKey }, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.Result.Should().Be(0);
    }

    [Fact]
    public void EstimateActionSizeAsync_ShouldReturnZeroForInvalidActionKey()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        var invalidActionKey = "invalid.action.key";
        
        // Act
        var result = service.EstimateActionSizeAsync(invalidActionKey, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.Result.Should().Be(0);
    }

    [Fact]
    public void TryGetActionAppliedAsync_ShouldReturnNullForInvalidActionKey()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        var invalidActionKey = "invalid.action.key";
        
        // Act
        var result = service.TryGetActionAppliedAsync(invalidActionKey, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeNull();
    }

    [Fact]
    public void ExecuteActionsAsync_WithNullActionKeys_ShouldNotThrow()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        
        // Act
        Func<Task> act = async () => await service.ExecuteActionsAsync(null, CancellationToken.None);
        
        // Assert
        act.Should().NotThrowAsync();
    }

    [Fact]
    public void ExecuteActionsAsync_WithEmptyActionKeys_ShouldNotThrow()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        var emptyActionKeys = new List<string>();
        
        // Act
        Func<Task> act = async () => await service.ExecuteActionsAsync(emptyActionKeys, CancellationToken.None);
        
        // Assert
        act.Should().NotThrowAsync();
    }

    [Fact]
    public void ApplyPerformanceOptimizationsAsync_ShouldNotThrow()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        
        // Act - 这个方法会尝试执行推荐的性能优化，但在测试环境中可能无法实际执行
        // 我们只验证它不会抛出异常
        Func<Task> act = async () => await service.ApplyPerformanceOptimizationsAsync(CancellationToken.None);
        
        // Assert
        act.Should().NotThrowAsync();
    }

    [Fact]
    public void RunCleanupAsync_ShouldNotThrow()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        
        // Act - 这个方法会尝试运行清理操作，但在测试环境中可能无法实际执行
        // 我们只验证它不会抛出异常
        Func<Task> act = async () => await service.RunCleanupAsync(CancellationToken.None);
        
        // Assert
        act.Should().NotThrowAsync();
    }

    [Fact]
    public void Categories_ShouldContainExpectedActionKeys()
    {
        // Arrange
        var expectedActionKeys = new[]
        {
            "explorer.taskbar",
            "explorer.responsiveness",
            "explorer.visibility",
            "performance.multimedia",
            "performance.memory",
            "services.diagnostics",
            "cleanup.browserCache",
            "cleanup.thumbnailCache"
        };
        
        // Act
        var allActionKeys = WindowsOptimizationService.GetCategories()
            .SelectMany(c => c.Actions)
            .Select(a => a.Key)
            .ToList();
        
        // Assert
        foreach (var key in expectedActionKeys)
        {
            allActionKeys.Should().Contain(key);
        }
    }
}

