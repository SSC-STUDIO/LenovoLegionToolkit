using System.Linq;
using System.Windows;
using System.Windows.Media;
using FluentAssertions;
using UniversalDeviceToolkit.WPF.Controls;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class FanCurveControlTests
{
    [Fact]
    public void CreatePolylineSegments_ShouldVisitEveryPointInOrder()
    {
        var points = Enumerable.Range(0, 10)
            .Select(index => new Point(index * 20, 220 - index * 12))
            .ToArray();

        var segments = FanCurveControl.CreatePolylineSegments(points);

        segments.Should().HaveCount(points.Length - 1);
        segments.OfType<LineSegment>().Select(segment => segment.Point).Should().Equal(points.Skip(1));
        segments.Should().NotContain(segment => segment is BezierSegment);
    }

    [Fact]
    public void CreateAreaFigure_ShouldUseTheSameNodesAsTheCurve()
    {
        var points = new[]
        {
            new Point(10, 180),
            new Point(30, 160),
            new Point(50, 120),
        };

        var figure = FanCurveControl.CreateAreaFigure(points, baselineY: 220);

        figure.StartPoint.Should().Be(new Point(10, 220));
        figure.IsFilled.Should().BeTrue();
        figure.IsClosed.Should().BeTrue();
        figure.Segments.OfType<LineSegment>().Select(segment => segment.Point)
            .Should().Equal(points.Append(new Point(50, 220)));
    }

    [Fact]
    public void CreatePolylineSegments_ShouldKeepMonotonicNodeOrder()
    {
        var points = new[]
        {
            new Point(10, 180),
            new Point(30, 160),
            new Point(50, 160),
            new Point(70, 120),
        };

        var segments = FanCurveControl.CreatePolylineSegments(points).OfType<LineSegment>().ToArray();

        segments.Select(segment => segment.Point.X).Should().Equal(30, 50, 70);
        segments.Select(segment => segment.Point.Y).Should().Equal(160, 160, 120);
    }

    [Theory]
    [InlineData(0, 220)]
    [InlineData(5, 120)]
    [InlineData(10, 20)]
    public void GetValueY_ShouldMapEverySliderValueToThePlotRange(double value, double expectedY)
    {
        FanCurveControl.GetValueY(value, minimum: 0, maximum: 10, top: 20, bottom: 220)
            .Should().Be(expectedY);
    }
}
