using Avalonia;
using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Pages;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class DashboardLayoutParityTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(320, 1)]
    [InlineData(699, 1)]
    [InlineData(700, 2)]
    [InlineData(899, 2)]
    [InlineData(1100, 2)]
    [InlineData(1101, 3)]
    [InlineData(2400, 3)]
    public void GetColumnCountForWidth_UsesTheAssignedBreakpoints(double width, int expected)
    {
        DashboardColumnLayout.GetColumnCountForWidth(width).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0, 100, 0.0)]
    [InlineData(50, 0, 100, 0.5)]
    [InlineData(100, 0, 100, 1.0)]
    [InlineData(150, 0, 100, 1.0)]
    [InlineData(-25, 0, 100, 0.0)]
    [InlineData(double.NaN, 0, 100, 0.0)]
    [InlineData(double.PositiveInfinity, 0, 100, 0.0)]
    [InlineData(50, 20, 80, 0.5)]
    [InlineData(80, 20, 20, 0.0)]
    public void RadialGauge_NormalizeRatio_ClampsToUnitRange(
        double value,
        double minimum,
        double maximum,
        double expected)
    {
        RadialGauge.NormalizeRatio(value, minimum, maximum)
            .Should().BeApproximately(expected, 1e-9);
    }

    [Fact]
    public void RadialGauge_PointOnCircle_TracksTheClockwiseArcSpan()
    {
        var center = new Point(50, 50);

        var start = RadialGauge.PointOnCircle(center, 40, 135);
        var end = RadialGauge.PointOnCircle(center, 40, 135 + 270);

        start.X.Should().BeLessThan(center.X);
        start.Y.Should().BeGreaterThan(center.Y);
        end.X.Should().BeGreaterThan(center.X);
        end.Y.Should().BeGreaterThan(center.Y);
        RadialGauge.PointOnCircle(center, 40, 0).X.Should().Be(90);
        RadialGauge.PointOnCircle(center, 40, 90).Y.Should().Be(90);
    }

    [Fact]
    public void FanCurveEditor_GetValueY_MapsValuesToScreenY()
    {
        FanCurveEditor.GetValueY(50, 0, 100, top: 10, bottom: 110)
            .Should().BeApproximately(60, 1e-9);
        FanCurveEditor.GetValueY(0, 0, 100, 10, 110).Should().Be(110);
        FanCurveEditor.GetValueY(100, 0, 100, 10, 110).Should().Be(10);
        FanCurveEditor.GetValueY(150, 0, 100, 10, 110).Should().Be(10);
    }

    [Fact]
    public void FanCurveEditor_GetPointValue_IsTheInverseOfGetValueY()
    {
        FanCurveEditor.GetPointValue(60, 0, 100, top: 10, bottom: 110)
            .Should().BeApproximately(50, 1e-9);
    }

    [Fact]
    public void FanCurveEditor_ClampPoint_CapsEarlierPointsToKeepCurveNonDecreasing()
    {
        var values = new[] { 30.0, 40, 50, 60, 70, 80, 90, 100, 110, 120 };

        FanCurveEditor.ClampPoint(values, index: 4, value: 35, minimum: 0, maximum: 65535);

        values.Should().Equal(30, 35, 35, 35, 35, 80, 90, 100, 110, 120);
    }

    [Fact]
    public void FanCurveEditor_ClampPoint_RaisesLaterPointsToKeepCurveNonDecreasing()
    {
        var values = new[] { 30.0, 40, 50, 60, 70, 80, 90, 100, 110, 120 };

        FanCurveEditor.ClampPoint(values, index: 4, value: 200, minimum: 0, maximum: 65535);

        values.Should().Equal(30, 40, 50, 60, 200, 200, 200, 200, 200, 200);
    }

    [Fact]
    public void FanCurveEditor_ClampPoint_ClampsToRangeAndCapsEarlierPoints()
    {
        var values = new[] { 0.0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        FanCurveEditor.ClampPoint(values, index: 5, value: 90000, minimum: 0, maximum: 65535);

        values.Should().Equal(0, 0, 0, 0, 0, 65535, 65535, 65535, 65535, 65535);
    }
}
