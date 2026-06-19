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
            .And.Contain("<BlurEffect");
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

    private static string ReadWpfText(params string[] relativeSegments)
    {
        var root = FindRepositoryRoot();
        var segments = new[] { root, "UniversalDeviceToolkit.WPF" }.Concat(relativeSegments).ToArray();
        return File.ReadAllText(Path.Combine(segments));
    }

    private static string FindRepositoryRoot()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("UDT_REPOSITORY_ROOT"),
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var candidate in candidates.Where(static candidate => !string.IsNullOrWhiteSpace(candidate)))
        {
            var current = Path.GetFullPath(candidate!);
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "UniversalDeviceToolkit.sln")))
                    return current;

                current = Directory.GetParent(current)?.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate UniversalDeviceToolkit.sln.");
    }
}
