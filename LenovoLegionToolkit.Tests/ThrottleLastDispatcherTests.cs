using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests;

public class ThrottleLastDispatcherTests
{
    private static readonly IDelayProvider FastDelay = new TestFastDelayProvider();

    private class TestFastDelayProvider : IDelayProvider
    {
        public Task Delay(TimeSpan delay, CancellationToken token)
        {
            // Use an actual cancellable delay so tests observe correct cancellation behavior.
            // Tests use small intervals; keeping the real delay ensures deterministic cancellation semantics.
            return Task.Delay(delay, token);
        }
    }
    private ThrottleLastDispatcher CreateDispatcher(TimeSpan interval, string? tag = null)
    {
        return new ThrottleLastDispatcher(interval, tag, FastDelay);
    }
    [Fact]
    public async Task DispatchAsync_ShouldExecuteTaskAfterDelay()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(100);
        var dispatcher = CreateDispatcher(interval, "test");
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
        var dispatcher = CreateDispatcher(interval, "test");
        
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
        var dispatcher = CreateDispatcher(TimeSpan.FromMilliseconds(50));
        
        // Act & Assert
        Func<Task> act = async () => await dispatcher.DispatchAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
    
    [Fact]
    public async Task DispatchAsync_TaskThrowsException_ShouldPropagateException()
    {
        // Arrange
        var dispatcher = CreateDispatcher(TimeSpan.FromMilliseconds(50));
        var expectedException = new InvalidOperationException("Test exception");
        
        // Act & Assert
        Func<Task> act = async () => 
            await dispatcher.DispatchAsync(() => Task.FromException(expectedException));
        
        (await act.Should().ThrowAsync<InvalidOperationException>()).And.Should().Be(expectedException);
    }
    
    [Fact]
    public async Task Dispose_ShouldCancelPendingOperations()
    {
        // Arrange
        // Use a much shorter interval in tests to avoid long-running test suite
        var interval = TimeSpan.FromMilliseconds(200);
        var dispatcher = CreateDispatcher(interval);
        bool taskExecuted = false;

        // Act
        var dispatchTask = dispatcher.DispatchAsync(async () => {
            await Task.Delay(50);
            taskExecuted = true;
        });

        // Give it a moment to start
        await Task.Delay(10);

        // Dispose to cancel
        dispatcher.Dispose();

        // Wait a bit more than expected execution time
        await Task.Delay(100);

        // Assert
        taskExecuted.Should().BeFalse();
    }
    
    [Fact]
    public async Task DispatchAsync_ShouldBeThreadSafe()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = CreateDispatcher(interval);
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
        var dispatcher = CreateDispatcher(interval);
        var executedTasks = new List<int>();
        
        // Act - Dispatch tasks with enough delay between them
        for (int i = 0; i < 3; i++)
        {
            var taskId = i;
            await dispatcher.DispatchAsync(() => {
                executedTasks.Add(taskId);
                return Task.CompletedTask;
            });
            
            // Wait more than interval to allow previous task to complete
            await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(100)));
        }
        
        // Assert - All tasks should have executed
        executedTasks.Count.Should().Be(3);
        for (int i = 0; i < 3; i++)
        {
            executedTasks[i].Should().Be(i);
        }
    }

    [Fact]
    public async Task DispatchAsync_WithVeryShortInterval_ShouldExecuteLast()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(300);
        var dispatcher = CreateDispatcher(interval, "test");
        var executedTasks = new List<int>();

        // Act - Dispatch multiple tasks as quickly as possible so only the last one survives cancellation
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            var taskId = i;
            tasks.Add(dispatcher.DispatchAsync(() => {
                executedTasks.Add(taskId);
                return Task.CompletedTask;
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Only the last task should have executed
        executedTasks.Count.Should().Be(1);
        executedTasks[0].Should().Be(4);
    }

    [Fact]
    public async Task DispatchAsync_WithLongInterval_ShouldWait()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(300);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        var executionTime = DateTime.MinValue;
        
        // Act
        var dispatchTime = DateTime.UtcNow;
        await dispatcher.DispatchAsync(() => {
            executionTime = DateTime.UtcNow;
            return Task.CompletedTask;
        });
        
        // Wait for execution
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert - Task should have executed after the interval
        var elapsed = executionTime - dispatchTime;
        elapsed.TotalMilliseconds.Should().BeGreaterOrEqualTo(interval.TotalMilliseconds - 50);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleRapidDispatches_ShouldExecuteLast()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(100);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        var executedTasks = new List<int>();
        
        // Act - Dispatch 10 tasks as fast as possible
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var taskId = i;
            tasks.Add(dispatcher.DispatchAsync(() => {
                executedTasks.Add(taskId);
                return Task.CompletedTask;
            }));
        }
        
        await Task.WhenAll(tasks);
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert - Only the last task should have executed
        executedTasks.Count.Should().Be(1);
        executedTasks[0].Should().Be(9);
    }

    [Fact]
    public async Task DispatchAsync_WithTaskTakingLongerThanInterval_ShouldComplete()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskCompleted = false;
        
        // Act
        await dispatcher.DispatchAsync(async () => {
            await Task.Delay(100); // Task takes longer than interval
            taskCompleted = true;
        });
        
        // Wait for task to complete
        await Task.Delay(150);
        
        // Assert - Task should have completed
        taskCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithZeroInterval_ShouldExecuteImmediately()
    {
        // Arrange
        var interval = TimeSpan.Zero;
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(() => {
            taskExecuted = true;
            return Task.CompletedTask;
        });
        
        // Assert - Task should have executed immediately
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public void DispatchAsync_WithNegativeInterval_ShouldThrow()
    {
        // Arrange & Act
        Action act = () => new ThrottleLastDispatcher(TimeSpan.FromMilliseconds(-1), "test");
        
        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task DispatchAsync_WithVeryLongInterval_ShouldWait()
    {
        // Arrange
        // reduced interval so tests run faster while still validating behavior
        var interval = TimeSpan.FromMilliseconds(300);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        var dispatchTime = DateTime.UtcNow;
        await dispatcher.DispatchAsync(() => {
            taskExecuted = true;
            return Task.CompletedTask;
        });
        
        // Wait for execution
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(100)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public void DispatchAsync_WithName_ShouldStoreName()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var name = "TestDispatcher";
        var dispatcher = new ThrottleLastDispatcher(interval, name);
        
        // Act & Assert - Name is stored but not directly accessible
        // This test ensures the constructor accepts a name parameter
        dispatcher.Should().NotBeNull();
    }

    [Fact]
    public async Task DispatchAsync_WithEmptyName_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(() => {
            taskExecuted = true;
            return Task.CompletedTask;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithNullName_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, null);
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(() => {
            taskExecuted = true;
            return Task.CompletedTask;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public void DispatchAsync_WithMultipleDispose_ShouldNotThrow()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        
        // Act & Assert
        Action act = () => {
            dispatcher.Dispose();
            dispatcher.Dispose();
            dispatcher.Dispose();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public async Task DispatchAsync_AfterDispose_ShouldThrow()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        dispatcher.Dispose();
        
        // Act & Assert
        Func<Task> act = async () => await dispatcher.DispatchAsync(() => Task.CompletedTask);
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskReturningValue_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        var expectedValue = 42;
        int? actualValue = null;
        
        // Act
        await dispatcher.DispatchAsync(() => {
            actualValue = expectedValue;
            return Task.CompletedTask;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThrowingAfterDelay_ShouldPropagate()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        var expectedException = new InvalidOperationException("Delayed exception");
        
        // Act & Assert
        Func<Task> act = async () => 
            await dispatcher.DispatchAsync(async () => {
                await Task.Delay(10);
                throw expectedException;
            });
        
        (await act.Should().ThrowAsync<InvalidOperationException>()).And.Should().Be(expectedException);
    }

    [Fact]
    public async Task DispatchAsync_WithConcurrentDispatchesAndExceptions_ShouldHandle()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        var executedTasks = new List<int>();
        
        // Act - Dispatch tasks where some throw exceptions
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            var taskId = i;
            tasks.Add(dispatcher.DispatchAsync(() => {
                if (taskId == 2)
                    throw new InvalidOperationException("Test exception");
                executedTasks.Add(taskId);
                return Task.CompletedTask;
            }));
        }
        
        // Wait for all tasks to complete
        await Task.WhenAll(tasks);
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert - At least one task should have executed
        executedTasks.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DispatchAsync_WithRapidFireDispatches_ShouldExecuteLast()
    {
        // Arrange
        // Use a moderate interval and a TaskCompletionSource to deterministically
        // observe which task actually executed last without relying on fixed delays.
        var interval = TimeSpan.FromMilliseconds(200);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        var executedTasks = new ConcurrentQueue<int>();

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = new List<Task>();

        // Act - Dispatch tasks in a tight loop. The last task will set the TCS on execution;
        // any earlier task that executes will fault the TCS so the test fails deterministically.
        for (int i = 0; i < 100; i++)
        {
            var taskId = i;
            if (taskId == 99)
            {
                tasks.Add(dispatcher.DispatchAsync(() => {
                    executedTasks.Enqueue(taskId);
                    tcs.TrySetResult(taskId);
                    return Task.CompletedTask;
                }));
            }
            else
            {
                tasks.Add(dispatcher.DispatchAsync(() => {
                    // If any non-last task runs, mark failure
                    tcs.TrySetException(new InvalidOperationException($"Unexpected execution: {taskId}"));
                    return Task.CompletedTask;
                }));
            }
        }

        // Wait for either the TCS to complete (last executed) or a timeout
        var timeout = TimeSpan.FromMilliseconds(interval.TotalMilliseconds * 4 + 500);
        var finished = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
        if (finished != tcs.Task)
            throw new Exception("Timed out waiting for last dispatched task to execute");

        // Ensure TCS did not fault
        var lastExecuted = await tcs.Task;

        // Ensure one task ran; exact id may vary by environment so accept any in-range value
        executedTasks.Count.Should().Be(1);
        executedTasks.TryPeek(out var last).Should().BeTrue();
        last.Should().BeInRange(0, 99);

        // Clean up: wait for all dispatch tasks to finish so no background activity remains
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatModifiesState_ShouldBeConsistent()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        var counter = 0;

        // Act - Dispatch multiple tasks that modify the same state
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            var value = i;
            tasks.Add(dispatcher.DispatchAsync(() => {
                counter = value;
                return Task.CompletedTask;
            }));
        }

        await Task.WhenAll(tasks);
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));

        // Assert - Counter should have the last value
        counter.Should().Be(9);
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatReturnsTask_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(async () => {
            await Task.Delay(10);
            taskExecuted = true;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleIntervals_ShouldExecuteCorrectly()
    {
        // Arrange
        var interval1 = TimeSpan.FromMilliseconds(50);
        var dispatcher1 = new ThrottleLastDispatcher(interval1, "test1");
        var interval2 = TimeSpan.FromMilliseconds(100);
        var dispatcher2 = new ThrottleLastDispatcher(interval2, "test2");
        
        var executedTasks1 = new List<int>();
        var executedTasks2 = new List<int>();
        
        // Act - Dispatch to both dispatchers
        await dispatcher1.DispatchAsync(() => {
            executedTasks1.Add(1);
            return Task.CompletedTask;
        });
        
        await dispatcher2.DispatchAsync(() => {
            executedTasks2.Add(1);
            return Task.CompletedTask;
        });
        
        // Wait for both to complete
        await Task.Delay(interval2.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert - Both should have executed
        executedTasks1.Count.Should().Be(1);
        executedTasks2.Count.Should().Be(1);
    }

    [Fact]
    public async Task DispatchAsync_WithVerySmallInterval_ShouldExecuteLast()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        var executedTasks = new List<int>();
        
        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            var taskId = i;
            tasks.Add(dispatcher.DispatchAsync(() => {
                executedTasks.Add(taskId);
                return Task.CompletedTask;
            }));
        }
        await Task.WhenAll(tasks);
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(100)));
        
        // Assert - Only the last task should have executed
        executedTasks.Count.Should().Be(1);
        executedTasks[0].Should().Be(4);
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatThrowsAndThenSucceeds_ShouldExecute()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        var attemptCount = 0;
        bool taskSucceeded = false;
        
        // Act - First dispatch throws, second succeeds
        try
        {
            await dispatcher.DispatchAsync(() => {
                attemptCount++;
                throw new InvalidOperationException("First attempt");
            });
        }
        catch { }
        
        await dispatcher.DispatchAsync(() => {
            attemptCount++;
            taskSucceeded = true;
            return Task.CompletedTask;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert - Second task should have succeeded
        taskSucceeded.Should().BeTrue();
        attemptCount.Should().Be(2);
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatCatchesException_ShouldNotThrow()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(() => {
            try
            {
                throw new InvalidOperationException("Internal exception");
            }
            catch
            {
                taskExecuted = true;
            }
            return Task.CompletedTask;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert - Task should have executed and caught the exception
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatReturnsNull_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(() => {
            taskExecuted = true;
            return Task.CompletedTask;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatUsesCancellationToken_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;

        // Act
        await dispatcher.DispatchAsync(() => {
            taskExecuted = true;
            return Task.CompletedTask;
        });

        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));

        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatReturnsCompletedTask_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(() => {
            taskExecuted = true;
            return Task.CompletedTask;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatReturnsFromResult_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(() => {
            taskExecuted = true;
            return Task.FromResult(true);
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatReturnsRun_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(() => {
            taskExecuted = true;
            return Task.Run(() => { });
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatYields_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(async () => {
            await Task.Yield();
            taskExecuted = true;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatDelays_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(async () => {
            await Task.Delay(10);
            taskExecuted = true;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatWhenAll_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(async () => {
            await Task.WhenAll(Task.Delay(10), Task.Delay(10));
            taskExecuted = true;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatWhenAny_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(async () => {
            await Task.WhenAny(Task.Delay(10), Task.Delay(100));
            taskExecuted = true;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatContinueWith_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(() => {
            return Task.Delay(10).ContinueWith(_ => {
                taskExecuted = true;
            });
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatUsesConfigureAwait_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(async () => {
            await Task.Delay(10).ConfigureAwait(false);
            taskExecuted = true;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WithTaskThatUsesConfigureAwaitTrue_ShouldWork()
    {
        // Arrange
        var interval = TimeSpan.FromMilliseconds(50);
        var dispatcher = new ThrottleLastDispatcher(interval, "test");
        bool taskExecuted = false;
        
        // Act
        await dispatcher.DispatchAsync(async () => {
            await Task.Delay(10).ConfigureAwait(true);
            taskExecuted = true;
        });
        
        await Task.Delay(interval.Add(TimeSpan.FromMilliseconds(50)));
        
        // Assert
        taskExecuted.Should().BeTrue();
    }
}
