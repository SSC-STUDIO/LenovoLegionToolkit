using System;
using System.Threading.Tasks;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests;

public class RetryHelperTests
{
    [Fact]
    public async Task RetryAsync_SucceedsAfterRetries()
    {
        var calls = 0;

        await RetryHelper.RetryAsync(async () =>
        {
            calls++;
            if (calls < 3)
                throw new InvalidOperationException();
            await Task.CompletedTask;
        }, maximumRetries: 5, timeout: TimeSpan.Zero);

        calls.Should().Be(3);
    }

    [Fact]
    public async Task RetryAsync_ThrowsMaximumRetriesReachedException()
    {
        var calls = 0;

        Func<Task> action = async () =>
        {
            calls++;
            throw new InvalidOperationException();
        };

        await Assert.ThrowsAsync<MaximumRetriesReachedException>(() => RetryHelper.RetryAsync(action, maximumRetries: 2, timeout: TimeSpan.Zero));
        calls.Should().Be(2);
    }

    [Fact]
    public async Task RetryAsync_DoesNotRetryForNonMatchingException()
    {
        var calls = 0;

        Func<Task> action = () =>
        {
            calls++;
            throw new InvalidOperationException();
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => RetryHelper.RetryAsync(action, maximumRetries: 3, matchingException: ex => ex is ArgumentException));
        calls.Should().Be(1);
    }
}
