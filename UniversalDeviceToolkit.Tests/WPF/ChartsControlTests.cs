using FluentAssertions;
using UniversalDeviceToolkit.WPF.Controls.Charts;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

// Console (dashboard) chart controls. TrendSeries is a plain ring-buffer POCO and
// is exercised directly; the visual controls (RadialGaugeControl / TrendChartControl /
// Charts.xaml) are verified by inspecting their source/markup as text, matching the
// established console-UI test pattern (no WPF visuals are instantiated here).
[Trait("Category", TestCategories.Unit)]
public class ChartsControlTests
{
    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(2, 2)]
    [InlineData(60, 60)]
    public void TrendSeries_ShouldClampCapacityToAtLeastTwo(int requested, int expectedCapacity)
    {
        new TrendSeries("k", requested).Capacity.Should().Be(expectedCapacity);
    }

    [Fact]
    public void TrendSeries_ShouldSaturateCountAtCapacityWhenOverfilled()
    {
        var series = new TrendSeries("cpu", 3);
        series.Count.Should().Be(0);

        series.Add(1);
        series.Add(2);
        series.Count.Should().Be(2);

        // Overfill: head wraps, count caps at capacity.
        series.Add(3);
        series.Add(4);
        series.Add(5);
        series.Count.Should().Be(3);
    }

    [Fact]
    public void TrendSeries_ShouldEnumerateOldestToNewestAfterWrapAround()
    {
        var series = new TrendSeries("gpu", 3);
        series.Add(1);
        series.Add(2);
        series.Add(3);
        // Wrap: 1 is overwritten by 4, so window becomes 2,3,4 oldest->newest.
        series.Add(4);

        series.EnumerateOrdered().Should().Equal(2.0, 3.0, 4.0);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-5.0)]
    public void TrendSeries_ShouldClampInvalidOrNegativeSamplesToZero(double sample)
    {
        var series = new TrendSeries("k", 4);
        series.Add(sample);

        series.EnumerateOrdered().Should().Equal(0.0);
    }

    [Fact]
    public void TrendSeries_ObservedMaximum_ShouldReturnLargestSampleOrZeroWhenEmpty()
    {
        var empty = new TrendSeries("k", 4);
        empty.ObservedMaximum().Should().Be(0.0);

        empty.Add(10);
        empty.Add(42);
        empty.Add(7);
        empty.ObservedMaximum().Should().Be(42.0);
    }

    [Fact]
    public void TrendSeries_Clear_ShouldResetCountAndWindow()
    {
        var series = new TrendSeries("k", 4);
        series.Add(1);
        series.Add(2);

        series.Clear();

        series.Count.Should().Be(0);
        series.EnumerateOrdered().Should().BeEmpty();
        series.ObservedMaximum().Should().Be(0.0);
    }

    [Fact]
    public void ChartsMarkup_ShouldKeepTemplatePartsForGaugeRendering()
    {
        var xaml = ReadWpfText("Styles", "Charts.xaml");

        xaml.Should()
            .Contain("x:Name=\"PART_Track\"")
            .And.Contain("x:Name=\"PART_Glow\"")
            .And.Contain("x:Name=\"PART_Value\"")
            .And.Contain("x:Name=\"PART_Tip\"")
            .And.Contain("x:Name=\"PART_ValueText\"")
            .And.Contain("x:Name=\"PART_CaptionText\"");
    }

    [Fact]
    public void ChartsMarkup_ShouldUseCanonicalThemeTextBrushesAndGlow()
    {
        var xaml = ReadWpfText("Styles", "Charts.xaml");

        xaml.Should()
            .Contain("Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"")
            .And.Contain("Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"")
            .And.Contain("RingBrush\" Value=\"{DynamicResource ChartUtilizationBrush}\"")
            .And.Contain("x:Name=\"PART_Glow\"");
    }

    [Fact]
    public void RadialGaugeSource_ShouldKeepOpenBottomRingGeometryConstants()
    {
        var source = ReadWpfText("Controls", "Charts", "RadialGaugeControl.cs");

        source.Should()
            .Contain("StartAngle = 135.0")
            .And.Contain("SweepAngle = 270.0")
            .And.Contain("AnimationMs = 350.0")
            // cubic ease-out used for the value-arc sweep animation.
            .And.Contain("1 - Math.Pow(1 - t, 3)");
    }

    [Fact]
    public void TrendChartSource_ShouldDrawDenseBandAndRightAlignedGrowth()
    {
        var source = ReadWpfText("Controls", "Charts", "TrendChartControl.cs");

        // Horizontal guides + baseline only — no outer rectangular plot frame.
        source.Should().Contain("DrawGridlines");
        source.Should().NotContain("DrawPlotFrame");
        source.Should().Contain("height - 0.5");

        // Right-aligned growth: newest on the right, history expands leftward.
        source.Should().Contain("BuildPlotPoints");
        source.Should().Contain("startSlot");
        source.Should().Contain("capacity - count");

        // Multi-series fills stay translucent and all crest lines render after fills.
        source.Should().Contain("Color.FromArgb(76");
        source.Should().Contain("drawFill: true, drawLine: false");
        source.Should().Contain("drawFill: false, drawLine: true");
        source.Should().Contain("tailWidth");
        source.Should().Contain("areaCtx.BezierTo");
        source.Should().NotContain("areaCtx.LineTo(new Point(points[^1].X, height)");
    }

    [Fact]
    public void BuildPlotPoints_WhenSingleSample_ShouldExtendFlatSegmentLeft()
    {
        var series = new TrendSeries("util", 10);
        series.Add(50);
        series.Maximum = 100;

        var points = TrendChartControl.BuildPlotPoints(series, width: 100, height: 40, max: 100);

        // Capacity 10 → step = 100/9; single sample at right edge, flat segment one step left.
        var step = 100.0 / 9.0;
        points.Should().HaveCount(2);
        points[0].X.Should().BeApproximately(100.0 - step, 0.01);
        points[1].X.Should().BeApproximately(100.0, 0.01);
        // Both points at same Y (vertical start, no diagonal ease-in from baseline).
        // Y = height - (value/max * (height-2)) - 1 = 40 - 0.5*38 - 1 = 20.0
        points[0].Y.Should().BeApproximately(20.0, 0.01);
        points[1].Y.Should().BeApproximately(20.0, 0.01);
    }

    [Fact]
    public void BuildPlotPoints_WhenMultipleSamples_ShouldRightAlignNewestAtRightEdge()
    {
        var series = new TrendSeries("util", 5);
        series.Add(0);
        series.Add(100);
        series.Add(50);

        var points = TrendChartControl.BuildPlotPoints(series, width: 80, height: 40, max: 100);

        // Capacity 5 → step = 20; 3 samples occupy slots 2,3,4 → x = 40, 60, 80.
        // No diagonal ease-in: exactly 3 points (one per sample).
        points.Should().HaveCount(3);
        points[0].X.Should().BeApproximately(40.0, 0.01);
        points[1].X.Should().BeApproximately(60.0, 0.01);
        points[2].X.Should().BeApproximately(80.0, 0.01);
        // Peak sample (value=100, index 1) is highest on the plot (lowest Y).
        points[1].Y.Should().BeLessThan(points[0].Y);
        points[1].Y.Should().BeLessThan(points[2].Y);
    }

    private static string ReadWpfText(params string[] relativeSegments)
    {
        var root = RepositoryPaths.FindRoot();
        var segments = new[] { root, "UniversalDeviceToolkit.WPF" }.Concat(relativeSegments).ToArray();
        return File.ReadAllText(Path.Combine(segments));
    }

}
