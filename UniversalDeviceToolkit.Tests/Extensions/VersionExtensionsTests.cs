using FluentAssertions;
using LenovoLegionToolkit.Lib.Extensions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Extensions;

[Trait("Category", TestCategories.Unit)]
public class VersionExtensionsTests
{
    [Fact]
    public void IsBeta_WithDefaultVersion_ShouldReturnTrue()
    {
        var version = new Version(0, 0, 1, 0);
        version.IsBeta().Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 0, 99, 0)]
    [InlineData(3, 6, 99, 0)]
    [InlineData(0, 0, 99, 5)]
    public void IsBeta_WithBuild99_ShouldReturnTrue(int major, int minor, int build, int revision)
    {
        new Version(major, minor, build, revision).IsBeta().Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 0, 0, 0)]
    [InlineData(3, 6, 15, 0)]
    [InlineData(0, 0, 0, 0)]
    [InlineData(0, 0, 1, 1)]
    [InlineData(0, 0, 2, 0)]
    public void IsBeta_WithReleaseVersions_ShouldReturnFalse(int major, int minor, int build, int revision)
    {
        new Version(major, minor, build, revision).IsBeta().Should().BeFalse();
    }
}
