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
    public async Task EstimateCleanupSizeAsync_ShouldReturnZeroForInvalidActionKey()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        var invalidActionKey = "invalid.action.key";
        
        // Act
        var result = await service.EstimateCleanupSizeAsync(new[] { invalidActionKey }, CancellationToken.None);
        
        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task EstimateActionSizeAsync_ShouldReturnZeroForInvalidActionKey()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        var invalidActionKey = "invalid.action.key";
        
        // Act
        var result = await service.EstimateActionSizeAsync(invalidActionKey, CancellationToken.None);
        
        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task TryGetActionAppliedAsync_ShouldReturnNullForInvalidActionKey()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        var invalidActionKey = "invalid.action.key";
        
        // Act
        var result = await service.TryGetActionAppliedAsync(invalidActionKey, CancellationToken.None);
        
        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteActionsAsync_WithNullActionKeys_ShouldNotThrow()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        
        // Act & Assert
        await service.Invoking(s => s.ExecuteActionsAsync(null, CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteActionsAsync_WithEmptyActionKeys_ShouldNotThrow()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        var emptyActionKeys = new List<string>();
        
        // Act & Assert
        await service.Invoking(s => s.ExecuteActionsAsync(emptyActionKeys, CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task ApplyPerformanceOptimizationsAsync_ShouldNotThrow()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        
        // Act & Assert - 这个方法会尝试执行推荐的性能优化，但在测试环境中可能无法实际执行
        // 我们只验证它不会抛出异常
        await service.Invoking(s => s.ApplyPerformanceOptimizationsAsync(CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task RunCleanupAsync_ShouldNotThrow()
    {
        // Arrange
        var service = new WindowsOptimizationService();
        
        // Act & Assert - 这个方法会尝试运行清理操作，但在测试环境中可能无法实际执行
        // 我们只验证它不会抛出异常
        await service.Invoking(s => s.RunCleanupAsync(CancellationToken.None))
            .Should().NotThrowAsync();
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

    [Fact]
    public void GetCategories_ShouldIncludeAllStaticCategories()
    {
        // Arrange & Act
        var categories = WindowsOptimizationService.GetCategories();
        var categoryKeys = categories.Select(c => c.Key).ToList();
        
        // Assert
        categoryKeys.Should().Contain(new[]
        {
            "explorer",
            "performance",
            "services",
            "network",
            "cleanup.cache",
            "cleanup.systemFiles",
            "cleanup.systemComponents",
            "cleanup.performance",
            "cleanup.largeFiles",
            "cleanup.custom"
        });
    }

    [Fact]
    public void CleanupCacheCategory_ShouldIncludeNewActions()
    {
        // Arrange & Act
        var categories = WindowsOptimizationService.GetCategories();
        var cleanupCache = categories.First(c => c.Key == "cleanup.cache");
        var actionKeys = cleanupCache.Actions.Select(a => a.Key).ToList();
        
        // Assert
        actionKeys.Should().Contain("cleanup.browserCache");
        actionKeys.Should().Contain("cleanup.appLeftovers");
        actionKeys.Should().Contain("cleanup.thumbnailCache");
        actionKeys.Should().Contain("cleanup.remoteDesktopCache");
    }

    [Fact]
    public void LargeFilesCategory_ShouldBePresent()
    {
        // Arrange & Act
        var categories = WindowsOptimizationService.GetCategories();
        var largeFilesCategory = categories.FirstOrDefault(c => c.Key == "cleanup.largeFiles");
        
        // Assert
        largeFilesCategory.Should().NotBeNull();
        largeFilesCategory!.Actions.Should().Contain(a => a.Key == "cleanup.largeFiles");
    }

    /*
    [Fact]
    public async Task EstimateActionSizeAsync_ForLargeFiles_ShouldReturnSize()
    {
        // Arrange
        // Note: This test might return 0 if there are no large files in the scan paths,
        // but it should at least not throw and return a non-negative value.
        var service = new WindowsOptimizationService();
        
        // Act
        var size = await service.EstimateActionSizeAsync("cleanup.largeFiles", CancellationToken.None);
        
        // Assert
        size.Should().BeGreaterThanOrEqualTo(0);
    }
    */
}

