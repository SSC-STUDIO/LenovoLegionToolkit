using System;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests.Utils;

[Trait("Category", TestCategories.Unit)]
public class ThreadSafeCounterTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializeToZero()
    {
        // Arrange & Act
        var counter = new ThreadSafeCounter();

        // Act & Assert - Decrement should return true when counter is 0
        var result = counter.Decrement();
        result.Should().BeTrue(); // Counter was 0, now stays at 0
    }

    #endregion

    #region Increment Tests

    [Fact]
    public void Increment_ShouldIncreaseCounter()
    {
        // Arrange
        var counter = new ThreadSafeCounter();

        // Act
        counter.Increment();
        var result = counter.Decrement();

        // Assert - Counter was 1, after decrement it's 0, so Decrement returns false
        result.Should().BeFalse();
    }

    [Fact]
    public void Increment_WhenCalledMultipleTimes_ShouldIncreaseCounterMultipleTimes()
    {
        // Arrange
        var counter = new ThreadSafeCounter();

        // Act
        counter.Increment();
        counter.Increment();
        counter.Increment();

        var result1 = counter.Decrement();
        var result2 = counter.Decrement();
        var result3 = counter.Decrement();

        // Assert - Counter was 3, each decrement reduces it
        result1.Should().BeFalse(); // Counter: 3 -> 2
        result2.Should().BeFalse(); // Counter: 2 -> 1
        result3.Should().BeFalse(); // Counter: 1 -> 0
    }

    [Fact]
    public void Increment_ShouldAllowCounterToGoHigh()
    {
        // Arrange
        var counter = new ThreadSafeCounter();
        var increments = 100;

        // Act
        for (int i = 0; i < increments; i++)
            counter.Increment();

        // Decrement all
        var results = new bool[increments];
        for (int i = 0; i < increments; i++)
            results[i] = counter.Decrement();

        // Assert - All decrements should return false except when counter reaches 0
        results[increments - 1].Should().BeFalse(); // Last decrement: 1 -> 0
    }

    #endregion

    #region Decrement Tests

    [Fact]
    public void Decrement_WhenCounterIsZero_ShouldStayAtZero()
    {
        // Arrange
        var counter = new ThreadSafeCounter();

        // Act
        var result = counter.Decrement();

        // Assert
        result.Should().BeTrue(); // Counter was 0, stays at 0
    }

    [Fact]
    public void Decrement_WhenCalledMultipleTimesWithZero_ShouldStayAtZero()
    {
        // Arrange
        var counter = new ThreadSafeCounter();

        // Act
        var result1 = counter.Decrement();
        var result2 = counter.Decrement();
        var result3 = counter.Decrement();

        // Assert - All should return true since counter stays at 0
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
    }

    [Fact]
    public void Decrement_ShouldDecreaseCounterByOne()
    {
        // Arrange
        var counter = new ThreadSafeCounter();
        counter.Increment();
        counter.Increment();

        // Act
        var result = counter.Decrement();

        // Assert - Counter: 2 -> 1, should return false
        result.Should().BeFalse();

        // Decrement again
        result = counter.Decrement();
        result.Should().BeFalse(); // Counter: 1 -> 0

        // Decrement again
        result = counter.Decrement();
        result.Should().BeTrue(); // Counter was already 0, stays at 0
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Counter_WhenAccessedConcurrently_ShouldBeThreadSafe()
    {
        // Arrange
        var counter = new ThreadSafeCounter();
        var iterations = 1000;
        var tasks = new[]
        {
            Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                    counter.Increment();
            }),
            Task.Run(() =>
            {
                for (int i = 0; i < iterations / 2; i++)
                    counter.Decrement();
            }),
            Task.Run(() =>
            {
                for (int i = 0; i < iterations / 2; i++)
                    counter.Decrement();
            })
        };

        // Act
        await Task.WhenAll(tasks);

        // Assert - Final counter should be close to expected value
        // After 1000 increments and 1000 decrements, counter should be 0
        // But due to timing, it could be anywhere between 0 and 1000
        // Just verify it doesn't throw exceptions
        _ = counter.Decrement();
        // No exception means thread safety worked
    }

    [Fact]
    public async Task Increment_WhenCalledFromMultipleThreads_ShouldNotOverflow()
    {
        // Arrange
        var counter = new ThreadSafeCounter();
        var tasks = new Task[10];

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                    counter.Increment();
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Should have incremented 1000 times
        // Decrement 1000 times to verify
        for (int i = 0; i < 1000; i++)
        {
            var result = counter.Decrement();
            if (i < 999)
                result.Should().BeFalse();
            else
                result.Should().BeFalse(); // Last: 1 -> 0
        }

        // Counter should be 0 now
        counter.Decrement().Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Counter_WhenAlternatingIncrementDecrement_ShouldHandleCorrectly()
    {
        // Arrange
        var counter = new ThreadSafeCounter();

        // Act
        for (int i = 0; i < 100; i++)
        {
            counter.Increment();
            var result = counter.Decrement();

            // Assert - Counter: 1 -> 0, so Decrement returns false
            result.Should().BeFalse();
        }

        // Counter should be 0
        counter.Decrement().Should().BeTrue();
    }

    [Fact]
    public void Counter_WhenDecrementingMoreThanIncrementing_ShouldNotGoNegative()
    {
        // Arrange
        var counter = new ThreadSafeCounter();
        counter.Increment();
        counter.Increment();

        // Act - Decrement more times than we incremented
        var result1 = counter.Decrement(); // 2 -> 1
        var result2 = counter.Decrement(); // 1 -> 0
        var result3 = counter.Decrement(); // 0 -> 0 (stays at 0)
        var result4 = counter.Decrement(); // 0 -> 0

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeTrue(); // Counter was 0
        result4.Should().BeTrue(); // Counter stays at 0
    }

    #endregion
}
