using Avalonia;
using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Controls;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class TrendSparklineTests
{
    [Fact]
    public void BuildPlotPoints_RightAlignsNewestSamplesToCapacityGrid()
    {
        var points = TrendSparkline.BuildPlotPoints(
            [10, 20],
            capacity: 5,
            width: 100,
            height: 40,
            maximum: 20);

        points.Should().HaveCount(2);
        points[0].X.Should().BeApproximately(75, 0.001);
        points[1].X.Should().BeApproximately(100, 0.001);
        points[1].Y.Should().BeApproximately(1, 0.001);
    }

    [Fact]
    public void BuildPlotPoints_ExtendsSingleSampleWithoutDrawingAZeroSpike()
    {
        var points = TrendSparkline.BuildPlotPoints(
            [12],
            capacity: 6,
            width: 120,
            height: 48,
            maximum: 24);

        points.Should().HaveCount(2);
        points[0].Y.Should().Be(points[1].Y);
        points[1].X.Should().BeApproximately(120, 0.001);
    }
}
