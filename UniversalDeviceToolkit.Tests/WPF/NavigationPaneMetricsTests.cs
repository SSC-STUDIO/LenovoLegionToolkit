using FluentAssertions;
using UniversalDeviceToolkit.WPF.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public class NavigationPaneMetricsTests
{
    [Fact]
    public void GetCollapsedWidth_ShouldBePositive()
    {
        NavigationPaneMetrics.GetCollapsedWidth().Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetMaxStretchWidth_AtDesignWidth_ShouldMatchPreferredFloor()
    {
        var preferred = NavigationPaneMetrics.GetPreferredExpandedWidth();
        var max = NavigationPaneMetrics.GetMaxStretchWidth(1300);
        max.Should().Be(preferred);
    }

    [Fact]
    public void GetMaxStretchWidth_LargerWindow_ShouldGrowAbovePreferred()
    {
        var preferred = NavigationPaneMetrics.GetPreferredExpandedWidth();
        var max = NavigationPaneMetrics.GetMaxStretchWidth(1920);
        max.Should().BeGreaterThan(preferred);
        max.Should().BeLessThanOrEqualTo(420);
    }

    [Fact]
    public void GetMaxStretchWidth_UltraWide_ShouldCap()
    {
        var max = NavigationPaneMetrics.GetMaxStretchWidth(3840);
        max.Should().BeLessThanOrEqualTo(420);
        max.Should().BeGreaterThanOrEqualTo(NavigationPaneMetrics.GetPreferredExpandedWidth());
    }

    [Fact]
    public void GetMaxStretchWidth_InvalidWindow_ShouldFallbackToPreferred()
    {
        var preferred = NavigationPaneMetrics.GetPreferredExpandedWidth();
        NavigationPaneMetrics.GetMaxStretchWidth(0).Should().Be(preferred);
        NavigationPaneMetrics.GetMaxStretchWidth(-10).Should().Be(preferred);
        NavigationPaneMetrics.GetMaxStretchWidth(double.NaN).Should().Be(preferred);
    }
}
