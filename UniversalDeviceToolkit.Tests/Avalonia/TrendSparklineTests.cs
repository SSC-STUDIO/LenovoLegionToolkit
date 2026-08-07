using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
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

    [Fact]
    public void TrendChartSeries_Add_ClampsNegativeAndNonFiniteToZero()
    {
        var series = new TrendChartSeries("upload", Brushes.Red, capacity: 4);

        series.Add(-5);
        series.Add(double.NaN);
        series.Add(double.PositiveInfinity);
        series.Add(42);

        series.Count.Should().Be(4);
        series.EnumerateOrdered().Should().BeEquivalentTo(new[] { 0d, 0d, 0d, 42d });
    }

    [Fact]
    public void TrendChartSeries_Add_TrimsOldestSampleBeyondCapacity()
    {
        var series = new TrendChartSeries("upload", Brushes.Red, capacity: 3);

        series.Add(1);
        series.Add(2);
        series.Add(3);
        series.Add(4);

        series.Count.Should().Be(3);
        series.EnumerateOrdered().Should().BeEquivalentTo(new[] { 2d, 3d, 4d });
    }

    [Fact]
    public void TrendChartSeries_Add_WrapsRingBufferOrder()
    {
        var series = new TrendChartSeries("upload", Brushes.Red, capacity: 2);

        series.Add(10);
        series.Add(20);
        series.Add(30);

        series.EnumerateOrdered().Should().Equal(new[] { 20d, 30d });

        series.Add(40);
        series.Add(50);

        series.EnumerateOrdered().Should().Equal(new[] { 40d, 50d });
    }

    [Fact]
    public void TrendChartSeries_EnumerateOrdered_ReturnsOldestToNewest()
    {
        var series = new TrendChartSeries("download", Brushes.Blue, capacity: 10);
        for (var index = 0; index < 7; index++)
            series.Add(index * 10);

        var ordered = new List<double>(series.EnumerateOrdered());
        ordered.Should().Equal([0d, 10d, 20d, 30d, 40d, 50d, 60d]);
    }

    [Fact]
    public void TrendChartSeries_Clear_ResetsBuffer()
    {
        var series = new TrendChartSeries("upload", Brushes.Red, capacity: 4);
        series.Add(1);
        series.Add(2);

        series.Clear();

        series.Count.Should().Be(0);
        series.EnumerateOrdered().Should().BeEmpty();
        series.ResolvePlotMaximum().Should().BeGreaterThan(0);
    }

    [Fact]
    public void TrendChartSeries_ResolvePlotMaximum_UsesFixedMaximumWhenProvided()
    {
        var series = new TrendChartSeries("upload", Brushes.Red, capacity: 4, maximum: 128);
        series.Add(10);
        series.Add(200);

        series.ResolvePlotMaximum().Should().Be(128);
    }

    [Fact]
    public void TrendChartSeries_ResolvePlotMaximum_AutoScalesToObservedMaximum()
    {
        var series = new TrendChartSeries("upload", Brushes.Red, capacity: 4);
        series.Add(0);
        series.Add(0);
        series.Add(100);

        series.ResolvePlotMaximum().Should().BeApproximately(100 * 1.08, 0.001);
    }

    [Fact]
    public void TrendChartSeries_ObservedMaximum_IgnoresClearedSamples()
    {
        var series = new TrendChartSeries("upload", Brushes.Red, capacity: 4);
        series.Add(10);
        series.Add(90);
        series.Clear();
        series.Add(20);

        series.ObservedMaximum().Should().Be(20);
    }

    [Fact]
    public void TrendChart_BuildPlotPoints_MatchesTrendSparklineMapping()
    {
        var values = new[] { 4d, 8d, 12d, 6d };

        var chart = TrendChart.BuildPlotPoints(values, capacity: 6, width: 200, height: 80, maximum: 12);
        var sparkline = TrendSparkline.BuildPlotPoints(values, capacity: 6, width: 200, height: 80, maximum: 12);

        chart.Should().HaveCount(sparkline.Count);
        for (var index = 0; index < chart.Count; index++)
        {
            chart[index].X.Should().BeApproximately(sparkline[index].X, 0.001);
            chart[index].Y.Should().BeApproximately(sparkline[index].Y, 0.001);
        }
    }

    [Fact]
    public void TrendChart_BuildPlotPoints_HandlesEmptyInput()
    {
        var points = TrendChart.BuildPlotPoints([], capacity: 6, width: 200, height: 80, maximum: 12);

        points.Should().BeEmpty();
    }
}
