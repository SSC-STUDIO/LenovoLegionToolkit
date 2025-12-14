using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests;

public class ThrottleLastDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_ShouldExecuteTaskAfterDelay()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(100);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(async () => {
            await Task.Delay(10); // Small delay to simulate work
            taskExecuted = true;
        });
        
        // Wait a bit more than the interval to ensure the task completes
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }
    
    [Fact]
    public async Task DispatchAsync_ShouldCancelPreviousTasks()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(200);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        
        var executedTasks = new List<int>();
        
        // Act - Dispatch two tasks in quick succession (concurrently, not sequentially)
        var task1 = dispatcher.DispatchAsync(() => {
            executedTasks.Add(1);
            return Task.CompletedTask;
        });
        
        // Small delay to ensure task1 starts its delay
        await Task.Delay(10);
        
        var task2 = dispatcher.DispatchAsync(() => {
            executedTasks.Add(2);
            return Task.CompletedTask;
        });
        
        // Wait for both tasks to complete
        await Task.WhenAll(task1, task2);
        
        // Wait for the interval to pass to ensure all execution is complete
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert - Only the last task should have executed
        executedTasks.Count.Should().Be(1);
        executedTasks[0].Should().Be(2);
    }
    
    [Fact]
    public async Task DispatchAsync_NullTask_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dispatcher = new ThrottleLastDispatcher(TimeSpan.FromMilliseconds(50));
        
        // Act & Assert
        Func<Task> act = async () => await dispatcher.DispatchAsync(null);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task DispatchAsync_TaskThrowsException_ShouldPropagateException()
    {
        // Arrange
        var dispatcher = new ThrottleLastDispatcher(TimeSpan.FromMilliseconds(50));
        var expectedException = new InvalidOperationException("Test exception");
        
        // Act & Assert
        Func<Task> act = async () => 
            await dispatcher.DispatchAsync(() => Task.FromException(expectedException));
        
        (await act.Should().ThrowAsync<InvalidOperationException>()).And.Should().Be(expectedException);
    }
    
    [Fact]
    public void Dispose_ShouldCancelPendingOperations()
    {
        // Arrange
        var interval = TimeSpan.FromSeconds(10); // Long interval to ensure task is still pending
        var dispatcher = new ThrottleLastDispatcher(interval);
        bool taskExecuted = false;
        
        // Act
        var dispatchTask = dispatcher.DispatchAsync(async () => {
            await Task.Delay(50);
            taskExecuted = true;
        });
        
        // Give it a moment to start
        Task.Delay(10).Wait();
        
        // Dispose to cancel
        dispatcher.Dispose();
        
        // Wait a bit more than expected execution time
        Task.Delay(100).Wait();
        
        // Assert
        taskExecuted.Should().BeFalse();
    }
    
    [Fact]
    public async Task DispatchAsync_ShouldBeThreadSafe()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval);
        var executedTasks = new List<int>();
        var counter = 0;
        
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
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert - At least one task should have executed, and likely only the last one
        executedTasks.Count.Should().BeGreaterThan(0);
        executedTasks.Count.Should().BeLessThanOrEqualTo(10);
    }
    
    [Fact]
    public async Task DispatchAsync_ShouldAllowSequentialExecutionWithEnoughDelay()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval);
        var executedTasks = new List<int>();
        
        // Act - Dispatch tasks with enough delay between them
        for (int i = 0; i < 3; i++)
        {
            var taskId = i;
            await dispatcher.DispatchAsync(() => {
                executedTasks.Add(taskId);
                return Task.CompletedTask;
            });
            
            // Wait more than the interval to allow previous task to complete
            await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(100)));
        }
        
        // Assert - All tasks should have executed
        executedTasks.Count.Should().Be(3);
        for (int i = 0; i < 3; i++)
        {
            executedTasks[i].Should().Be(i);
        }
    }
}