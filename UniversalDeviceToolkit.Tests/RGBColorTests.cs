using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class RGBColorTests
{
    #region Equality Tests

    [Fact]
    public void Equals_WhenSameValues_ShouldBeEqual()
    {
        var a = new RGBColor(255, 128, 0);
        var b = new RGBColor(255, 128, 0);
        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WhenDifferentRed_ShouldNotBeEqual()
    {
        var a = new RGBColor(255, 128, 0);
        var b = new RGBColor(254, 128, 0);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WhenDifferentGreen_ShouldNotBeEqual()
    {
        var a = new RGBColor(255, 128, 0);
        var b = new RGBColor(255, 129, 0);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WhenDifferentBlue_ShouldNotBeEqual()
    {
        var a = new RGBColor(255, 128, 0);
        var b = new RGBColor(255, 128, 1);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithBoxedDifferentType_ShouldReturnFalse()
    {
        var a = new RGBColor(255, 128, 0);
        a.Equals("not a color").Should().BeFalse();
    }

    #endregion

    #region Static Color Constants Tests

    [Fact]
    public void Green_ShouldHaveExpectedValues()
    {
        RGBColor.Green.R.Should().Be(142);
        RGBColor.Green.G.Should().Be(255);
        RGBColor.Green.B.Should().Be(0);
    }

    [Fact]
    public void Red_ShouldHaveExpectedValues()
    {
        RGBColor.Red.R.Should().Be(255);
        RGBColor.Red.G.Should().Be(0);
        RGBColor.Red.B.Should().Be(0);
    }

    [Fact]
    public void White_ShouldHaveExpectedValues()
    {
        RGBColor.White.R.Should().Be(255);
        RGBColor.White.G.Should().Be(255);
        RGBColor.White.B.Should().Be(255);
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_EqualValues_ShouldReturnSameHash()
    {
        var a = new RGBColor(10, 20, 30);
        var b = new RGBColor(10, 20, 30);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldContainComponentValues()
    {
        var color = new RGBColor(100, 200, 50);
        var text = color.ToString();
        text.Should().Contain("100").And.Contain("200").And.Contain("50");
    }

    #endregion
}
