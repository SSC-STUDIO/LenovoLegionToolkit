#if WINDOWS

using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class AvaloniaSmartKeyHandlerTests
{
    private const double WindowMilliseconds = 500;

    [Fact]
    public void IsDoublePress_FirstPressIsNeverDouble()
    {
        SmartKeyPressClassifier.IsDoublePress(
                DateTime.MinValue,
                DateTime.UtcNow,
                WindowMilliseconds)
            .Should().BeFalse();
    }

    [Fact]
    public void IsDoublePress_PressWithinWindowIsDouble()
    {
        var first = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        SmartKeyPressClassifier.IsDoublePress(
                first,
                first.AddMilliseconds(200),
                WindowMilliseconds)
            .Should().BeTrue();
    }

    [Fact]
    public void IsDoublePress_PressAfterWindowExpiryIsSingle()
    {
        var first = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        SmartKeyPressClassifier.IsDoublePress(
                first,
                first.AddMilliseconds(600),
                WindowMilliseconds)
            .Should().BeFalse();
    }

    [Fact]
    public void IsDoublePress_PressExactlyAtWindowBoundaryIsSingle()
    {
        var first = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        SmartKeyPressClassifier.IsDoublePress(
                first,
                first.AddMilliseconds(500),
                WindowMilliseconds)
            .Should().BeFalse();
    }

    [Fact]
    public void IsDoublePress_TriplePressPatternProducesDoubleThenSingle()
    {
        var start = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var first = SmartKeyPressClassifier.IsDoublePress(DateTime.MinValue, start, WindowMilliseconds);
        var second = SmartKeyPressClassifier.IsDoublePress(start, start.AddMilliseconds(120), WindowMilliseconds);
        var third = SmartKeyPressClassifier.IsDoublePress(start.AddMilliseconds(120), start.AddMilliseconds(760), WindowMilliseconds);

        first.Should().BeFalse();
        second.Should().BeTrue();
        third.Should().BeFalse();
    }

    [Fact]
    public void Resolve_EmptyListIsSeededWithCurrentAction()
    {
        var actionId = Guid.NewGuid();
        var actions = new List<Guid>();

        var (current, next) = SmartKeyActionSelector.Resolve(actionId, actions);

        actions.Should().ContainSingle().Which.Should().Be(actionId);
        current.Should().Be(actionId);
        next.Should().Be(actionId);
    }

    [Fact]
    public void Resolve_CurrentActionRotatesToNext()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var actions = new List<Guid> { first, second };

        var (current, next) = SmartKeyActionSelector.Resolve(first, actions);

        current.Should().Be(first);
        next.Should().Be(second);
    }

    [Fact]
    public void Resolve_LastActionWrapsAroundToFirst()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var actions = new List<Guid> { first, second };

        var (current, next) = SmartKeyActionSelector.Resolve(second, actions);

        current.Should().Be(second);
        next.Should().Be(first);
    }

    [Fact]
    public void Resolve_UnknownCurrentActionStartsAtFirstAction()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var actions = new List<Guid> { first, second };

        var (current, next) = SmartKeyActionSelector.Resolve(Guid.NewGuid(), actions);

        current.Should().Be(first);
        next.Should().Be(second);
    }

    [Fact]
    public void Resolve_RepeatedResolutionRotatesThroughEveryAction()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var third = Guid.NewGuid();
        var actions = new List<Guid> { first, second, third };

        var firstRun = SmartKeyActionSelector.Resolve(first, actions);
        var secondRun = SmartKeyActionSelector.Resolve(firstRun.Next, actions);
        var thirdRun = SmartKeyActionSelector.Resolve(secondRun.Next, actions);

        firstRun.Current.Should().Be(first);
        firstRun.Next.Should().Be(second);
        secondRun.Current.Should().Be(second);
        secondRun.Next.Should().Be(third);
        thirdRun.Current.Should().Be(third);
        thirdRun.Next.Should().Be(first);
    }
}

#endif
