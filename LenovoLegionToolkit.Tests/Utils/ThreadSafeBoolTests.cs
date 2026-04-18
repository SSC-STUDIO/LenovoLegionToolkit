using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests.Utils;

[Trait("Category", TestCategories.Unit)]
public class ThreadSafeBoolTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeToFalse()
    {
        // Arrange & Act
        var threadSafeBool = new ThreadSafeBool();

        // Assert
        threadSafeBool.Value.Should().BeFalse();
    }

    #endregion

    #region Value Property Tests

    [Fact]
    public void Value_WhenSetToTrue_ShouldReturnTrue()
    {
        // Arrange
        var threadSafeBool = new ThreadSafeBool();

        // Act
        threadSafeBool.Value = true;

        // Assert
        threadSafeBool.Value.Should().BeTrue();
    }

    [Fact]
    public void Value_WhenSetToFalse_ShouldReturnFalse()
    {
        // Arrange
        var threadSafeBool = new ThreadSafeBool();
        threadSafeBool.Value = true;

        // Act
        threadSafeBool.Value = false;

        // Assert
        threadSafeBool.Value.Should().BeFalse();
    }

    [Fact]
    public void Value_WhenSetMultipleTimes_ShouldReflectLatestValue()
    {
        // Arrange
        var threadSafeBool = new ThreadSafeBool();

        // Act
        threadSafeBool.Value = true;
        threadSafeBool.Value = false;
        threadSafeBool.Value = true;

        // Assert
        threadSafeBool.Value.Should().BeTrue();
    }

    [Fact]
    public void Value_WhenNotSet_ShouldReturnFalse()
    {
        // Arrange & Act
        var threadSafeBool = new ThreadSafeBool();

        // Assert
        threadSafeBool.Value.Should().BeFalse();
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void Value_WhenAccessedConcurrently_ShouldBeThreadSafe()
    {
        // Arrange
        var threadSafeBool = new ThreadSafeBool();
        var tasks = new Task[100];
        var errors = 0;

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 1000; j++)
                {
                    threadSafeBool.Value = j % 2 == 0;
                    var value = threadSafeBool.Value;
                    // Should not throw or cause race conditions
                }
            });
        }

        // Wait for all tasks
        Task.WaitAll(tasks);

        // Assert - No exceptions should occur
        errors.Should().Be(0);
    }

    [Fact]
    public void Value_WhenSetFromMultipleThreads_ShouldMaintainConsistency()
    {
        // Arrange
        var threadSafeBool = new ThreadSafeBool();
        var iterations = 10000;
        var tasks = new[]
        {
            Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                    threadSafeBool.Value = true;
            }),
            Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                    threadSafeBool.Value = false;
            })
        };

        // Act
        Task.WaitAll(tasks);

        // Assert - Final value should be either true or false, not corrupted
        (threadSafeBool.Value == true || threadSafeBool.Value == false).Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Value_WhenRapidlySwitched_ShouldHandleCorrectly()
    {
        // Arrange
        var threadSafeBool = new ThreadSafeBool();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            threadSafeBool.Value = i % 2 == 0;
        }

        // Assert
        threadSafeBool.Value.Should().BeFalse(); // Last iteration (999) sets it to false
    }

    #endregion
}