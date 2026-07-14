using System.Drawing;
using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class ResolutionStructTests
{
    [Fact]
    public void Constructor_ShouldSetWidthAndHeight()
    {
        var res = new Resolution(1920, 1080);
        res.Width.Should().Be(1920);
        res.Height.Should().Be(1080);
    }

    [Fact]
    public void DisplayName_ShouldFormatCorrectly()
    {
        var res = new Resolution(2560, 1440);
        // Use \u00D7 so the expected multiplication sign cannot be corrupted by source encoding.
        res.DisplayName.Should().Be("2560 \u00D7 1440");
        res.DisplayName.Should().Be(string.Format(
            UniversalDeviceToolkit.Lib.Resources.Resource.Resolution_DisplayName_Format,
            2560,
            1440));
    }

    [Fact]
    public void ToString_ShouldUseXFormat()
    {
        var res = new Resolution(1920, 1080);
        res.ToString().Should().Be("1920x1080");
    }

    [Fact]
    public void Equals_SameValues_ShouldBeEqual()
    {
        var r1 = new Resolution(1920, 1080);
        var r2 = new Resolution(1920, 1080);
        r1.Equals(r2).Should().BeTrue();
        (r1 == r2).Should().BeTrue();
        (r1 != r2).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentValues_ShouldNotBeEqual()
    {
        var r1 = new Resolution(1920, 1080);
        var r2 = new Resolution(2560, 1440);
        r1.Equals(r2).Should().BeFalse();
        (r1 != r2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_SameValues_ShouldMatch()
    {
        var r1 = new Resolution(1920, 1080);
        var r2 = new Resolution(1920, 1080);
        r1.GetHashCode().Should().Be(r2.GetHashCode());
    }

    [Fact]
    public void CompareTo_Wider_ShouldBeGreater()
    {
        var r1 = new Resolution(1920, 1080);
        var r2 = new Resolution(2560, 1440);
        r1.CompareTo(r2).Should().BeLessThan(0);
        r2.CompareTo(r1).Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_SameWidthDifferentHeight_ShouldCompareByHeight()
    {
        var r1 = new Resolution(1920, 1080);
        var r2 = new Resolution(1920, 1440);
        r1.CompareTo(r2).Should().BeLessThan(0);
    }

    [Fact]
    public void CompareTo_Equal_ShouldReturnZero()
    {
        var r1 = new Resolution(1920, 1080);
        var r2 = new Resolution(1920, 1080);
        r1.CompareTo(r2).Should().Be(0);
    }

    [Fact]
    public void Conversion_ToSize_ShouldWork()
    {
        var res = new Resolution(1920, 1080);
        Size size = res;
        size.Width.Should().Be(1920);
        size.Height.Should().Be(1080);
    }

    [Fact]
    public void Conversion_FromSize_ShouldWork()
    {
        var size = new Size(1920, 1080);
        var res = (Resolution)size;
        res.Width.Should().Be(1920);
        res.Height.Should().Be(1080);
    }
}
