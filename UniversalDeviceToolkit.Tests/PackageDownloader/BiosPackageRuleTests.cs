using FluentAssertions;
using LenovoLegionToolkit.Lib.PackageDownloader.Detectors.Rules;
using Xunit;

namespace UniversalDeviceToolkit.Tests.PackageDownloader;

[Trait("Category", TestCategories.Unit)]
public sealed class BiosPackageRuleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bad-level")]
    [InlineData("ABCD")]
    [InlineData("12")]
    public void TryParseLevel_WithMalformedLevel_ShouldReturnFalse(string? level)
    {
        // Act
        var result = BiosPackageRule.TryParseLevel(level, out var prefix, out var version);

        // Assert
        result.Should().BeFalse();
        prefix.Should().BeEmpty();
        version.Should().Be(0);
    }

    [Fact]
    public void TryParseLevel_WithValidLevel_ShouldReturnPrefixAndVersion()
    {
        // Act
        var result = BiosPackageRule.TryParseLevel("GKCN64WW", out var prefix, out var version);

        // Assert
        result.Should().BeTrue();
        prefix.Should().Be("GKCN");
        version.Should().Be(64);
    }
}
