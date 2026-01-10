using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests;

public class ThrottleFirstDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ShouldExecuteFirstTaskImmediately()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(100);
        var dispatcher = new ThrottleFirstDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(async () => {
            await Task.Delay(10); // Small delay to simulate work
            taskExecuted = true;
        });
        
        // Assert - Task should execute immediately
        taskExecuted.Should().BeTrue();
    }
    
    [Fact]
    public async Task DispatchAsync_ShouldThrottleSubsequentTasks()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(200);
        var dispatcher = new ThrottleFirstDispatcher(interval, "test");
        
        var executedTasks = new List<int>();
        
        // Act - Execute first task
        await dispatcher.DispatchAsync(() => {
            executedTasks.Add(1);
            return Task.CompletedTask;
        });
        
        // Try to execute a second task within the interval
        await dispatcher.DispatchAsync(() => {
            executedTasks.Add(2);
            return Task.CompletedTask;
        });
        
        // Assert - Only the first task should have executed
        executedTasks.Count.Should().Be(1);
        executedTasks[0].Should().Be(1);
    }
    
    [Fact]
    public async Task DispatchAsync_ShouldAllowTasksAfterIntervalExpires()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(100);
        var dispatcher = new ThrottleFirstDispatcher(interval, "test");
        
        var executedTasks = new List<int>();
        
        // Act - Execute first task
        await dispatcher.DispatchAsync(() => {
            executedTasks.Add(1);
            return Task.CompletedTask;
        });
        
        // Wait for the interval to expire
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(10)));
        
        // Execute second task
        await dispatcher.DispatchAsync(() => {
            executedTasks.Add(2);
            return Task.CompletedTask;
        });
        
        // Assert - Both tasks should have executed
        executedTasks.Count.Should().Be(2);
        executedTasks[0].Should().Be(1);
        executedTasks[1].Should().Be(2);
    }
    
    [Fact]
    public async Task DispatchAsync_NullTask_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dispatcher = new ThrottleFirstDispatcher(TimeSpan.FromMilliseconds(50));
        
        // Act & Assert
        Func<Task> act = async () => await dispatcher.DispatchAsync(null);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task DispatchAsync_TaskThrowsException_ShouldPropagateException()
    {
        // Arrange
        var dispatcher = new ThrottleFirstDispatcher(TimeSpan.FromMilliseconds(50));
        var expectedException = new InvalidOperationException("Test exception");
        
        // Act & Assert
        Func<Task> act = async () => 
            await dispatcher.DispatchAsync(() => Task.FromException(expectedException));
        
        (await act.Should().ThrowAsync<InvalidOperationException>()).And.Should().Be(expectedException);
    }
    
    [Fact]
    public async Task ResetAsync_ShouldAllowImmediateExecutionAfterReset()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(200);
        var dispatcher = new ThrottleFirstDispatcher(interval, "test");
        
        var executedTasks = new List<int>();
        
        // Act - Execute first task
        await dispatcher.DispatchAsync(() => {
            executedTasks.Add(1);
            return Task.CompletedTask;
        });
        
        // Reset the dispatcher
        await dispatcher.ResetAsync();
        
        // Execute second task before interval expires
        await dispatcher.DispatchAsync(() => {
            executedTasks.Add(2);
            return Task.CompletedTask;
        });
        
        // Assert - Both tasks should have executed due to reset
        executedTasks.Count.Should().Be(2);
        executedTasks[0].Should().Be(1);
        executedTasks[1].Should().Be(2);
    }
    
    [Fact]
    public async Task Interval_ShouldBeAccessible()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(150);
        var dispatcher = new ThrottleFirstDispatcher(interval);
        
        // Act & Assert
        dispatcher.Interval.Should().Be(interval);
    }
    
    [Fact]
    public async Task DispatchAsync_ShouldBeThreadSafe()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleFirstDispatcher(interval);
        var executedTasks = new List<int>();
        
        // Act - Dispatch multiple tasks concurrently
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var taskId = i;
            tasks.Add(dispatcher.DispatchAsync(() => {
                lock (executedTasks)
                    executedTasks.Add(taskId);
                return Task.CompletedTask;
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Assert - Only one task should have executed due to throttling
        executedTasks.Count.Should().Be(1);
    }
    
    [Fact]
    public async Task DispatchAsync_TaskWithException_ShouldNotUpdateLastEventTime()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(100);
        var dispatcher = new ThrottleFirstDispatcher(interval);
        var exception = new InvalidOperationException("Test exception");
        
        // Act - First task throws exception
        try
        {
            await dispatcher.DispatchAsync(() => Task.FromException(exception));
        }
        catch (InvalidOperationException) { }
        
        // Try to execute another task immediately
        bool secondTaskExecuted = false;
        await dispatcher.DispatchAsync(() => {
            secondTaskExecuted = true;
            return Task.CompletedTask;
        });
        
        // Assert - Second task should have executed because first task failed
        secondTaskExecuted.Should().BeTrue();
    }
}