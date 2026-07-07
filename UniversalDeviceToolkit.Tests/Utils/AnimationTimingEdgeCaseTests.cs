using System;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Unit)]
public class AnimationTimingEdgeCaseTests
{
    [Fact]
    public void ComputeUiAnimationDuration_ZeroBaseSeconds_ShouldReturnZero()
    {
        AnimationTiming.ComputeUiAnimationDuration(0, true, 1.0)
            .Should().Be(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    [InlineData(0.5)]
    public void ComputeUiAnimationDuration_EnabledSpeedOne_ShouldReturnBaseSeconds(double baseSeconds)
    {
        var result = AnimationTiming.ComputeUiAnimationDuration(baseSeconds, true, 1.0);
        result.TotalSeconds.Should().BeApproximately(baseSeconds, 0.001);
    }

    [Theory]
    [InlineData(2.0)]
    [InlineData(4.0)]
    public void ComputeUiAnimationDuration_HigherSpeed_ShouldReduceDuration(double speed)
    {
        var result = AnimationTiming.ComputeUiAnimationDuration(1.0, true, speed);
        result.TotalSeconds.Should().BeApproximately(1.0 / speed, 0.001);
    }

    [Fact]
    public void ComputeUiAnimationDuration_Disabled_ShouldAlwaysReturnZeroRegardlessOfSpeed()
    {
        AnimationTiming.ComputeUiAnimationDuration(10, false, 5.0)
            .Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void ComputeUiAnimationDuration_VeryLargeSpeed_ShouldApproachZero()
    {
        var result = AnimationTiming.ComputeUiAnimationDuration(1.0, true, 1000.0);
        result.TotalMilliseconds.Should().BeApproximately(1.0, 0.1);
    }

    [Fact]
    public void ComputeUiAnimationDuration_FractionalSpeed_ShouldScaleCorrectly()
    {
        var result = AnimationTiming.ComputeUiAnimationDuration(2.0, true, 0.5);
        result.TotalSeconds.Should().BeApproximately(4.0, 0.001);
    }
}
