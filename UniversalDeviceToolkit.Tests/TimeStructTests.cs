using FluentAssertions;
using LenovoLegionToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class TimeTests
{
    #region Equality Tests

    [Fact]
    public void Equals_WhenSameValues_ShouldBeEqual()
    {
        var a = new Time(14, 30);
        var b = new Time(14, 30);
        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WhenDifferentHour_ShouldNotBeEqual()
    {
        var a = new Time(14, 30);
        var b = new Time(15, 30);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WhenDifferentMinute_ShouldNotBeEqual()
    {
        var a = new Time(14, 30);
        var b = new Time(14, 31);
        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithBoxedSameValue_ShouldBeEqual()
    {
        var a = new Time(14, 30);
        object b = new Time(14, 30);
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithBoxedDifferentType_ShouldReturnFalse()
    {
        var a = new Time(14, 30);
        a.Equals("not a time").Should().BeFalse();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_EqualValues_ShouldReturnSameHash()
    {
        var a = new Time(14, 30);
        var b = new Time(14, 30);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_LikelyDifferentHash()
    {
        var a = new Time(0, 0);
        var b = new Time(23, 59);
        a.GetHashCode().Should().NotBe(b.GetHashCode());
    }

    #endregion
}
