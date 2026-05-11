using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace LenovoLegionToolkit.Tests.Utils;

[Trait("Category", TestCategories.Unit)]
public class AnimationTimingTests
{
    [Fact]
    public void ComputeUiAnimationDuration_WhenDisabled_ReturnsZero()
    {
        AnimationTiming.ComputeUiAnimationDuration(0.2, false, 2.0)
            .Should().Be(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(1.0, 0.1, 100)]
    [InlineData(2.0, 0.2, 100)]
    [InlineData(0.5, 0.3, 600)]
    public void ComputeUiAnimationDuration_WhenEnabled_ScalesBySpeed(double speed, double baseSeconds, int expectedMilliseconds)
    {
        var actual = AnimationTiming.ComputeUiAnimationDuration(baseSeconds, true, speed);

        actual.TotalMilliseconds.Should().BeApproximately(expectedMilliseconds, 0.5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ComputeUiAnimationDuration_WhenSpeedNonPositive_UsesSpeedOne(double speed)
    {
        var actual = AnimationTiming.ComputeUiAnimationDuration(0.25, true, speed);

        actual.TotalMilliseconds.Should().BeApproximately(250, 0.5);
    }
}
