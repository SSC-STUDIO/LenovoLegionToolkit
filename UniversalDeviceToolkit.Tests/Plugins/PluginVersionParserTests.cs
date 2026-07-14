using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
public sealed class PluginVersionParserTests
{
    [Theory]
    [InlineData("1.0.17", true, "1.0.17")]
    [InlineData("v1.0.17", true, "1.0.17")]
    [InlineData("V2.3.4", true, "2.3.4")]
    [InlineData("  v1.2.3  ", true, "1.2.3")]
    [InlineData("1.2", true, "1.2")]
    [InlineData("10.0.0.1", true, "10.0.0.1")]
    [InlineData("", false, null)]
    [InlineData("   ", false, null)]
    [InlineData("not-a-version", false, null)]
    [InlineData("v", false, null)]
    [InlineData("1.0.0-beta", false, null)]
    public void TryParse_ShouldNormalizePluginVersions(string? raw, bool expectedSuccess, string? expectedVersion)
    {
        var success = PluginVersionParser.TryParse(raw, out var version);

        success.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            version.Should().Be(Version.Parse(expectedVersion!));
        }
        else
        {
            version.Should().Be(new Version(0, 0, 0, 0));
        }
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_ForNull()
    {
        var success = PluginVersionParser.TryParse(null, out var version);

        success.Should().BeFalse();
        version.Should().Be(new Version(0, 0, 0, 0));
    }

    [Theory]
    [InlineData("1.0.18", "1.0.17", true)]
    [InlineData("v1.0.18", "1.0.17", true)]
    [InlineData("1.0.18", "v1.0.17", true)]
    [InlineData("2.0.0", "1.9.9", true)]
    [InlineData("1.0.17", "1.0.17", false)]
    [InlineData("v1.0.17", "1.0.17", false)]
    [InlineData("1.0.17", "v1.0.17", false)]
    [InlineData("1.0.16", "1.0.17", false)]
    [InlineData("v1.0.16", "v1.0.17", false)]
    [InlineData("not-a-version", "1.0.17", false)]
    [InlineData("1.0.17", "not-a-version", false)]
    [InlineData("", "1.0.17", false)]
    [InlineData("1.0.17", "", false)]
    public void IsNewerThan_ShouldOnlyReportRealUpgrades(string? candidate, string? baseline, bool expected)
    {
        PluginVersionParser.IsNewerThan(candidate, baseline).Should().Be(expected);
    }

    [Fact]
    public void IsNewerThan_ShouldReturnFalse_WhenEitherSideIsNull()
    {
        PluginVersionParser.IsNewerThan(null, "1.0.0").Should().BeFalse();
        PluginVersionParser.IsNewerThan("1.0.1", null).Should().BeFalse();
        PluginVersionParser.IsNewerThan(null, null).Should().BeFalse();
    }

    [Fact]
    public void ResolveInstalledVersion_ShouldReturnNull_WhenPluginIdMissing()
    {
        PluginVersionParser.ResolveInstalledVersion(null!, "1.2.3").Should().BeNull();
        PluginVersionParser.ResolveInstalledVersion("", "1.2.3").Should().BeNull();
        PluginVersionParser.ResolveInstalledVersion("   ", "1.2.3").Should().BeNull();
    }

    [Fact]
    public void ResolveInstalledVersion_ShouldFallbackToMetadata_WhenManifestUnavailable()
    {
        // Non-existent plugin id => no installed manifest; metadata should be returned.
        PluginVersionParser.ResolveInstalledVersion("plugin-that-does-not-exist-xyz", "9.8.7")
            .Should().Be("9.8.7");
    }

    [Fact]
    public void ResolveInstalledVersion_ShouldReturnNull_WhenManifestAndMetadataMissing()
    {
        PluginVersionParser.ResolveInstalledVersion("plugin-that-does-not-exist-xyz", null)
            .Should().BeNull();
        PluginVersionParser.ResolveInstalledVersion("plugin-that-does-not-exist-xyz", "  ")
            .Should().BeNull();
    }
}
