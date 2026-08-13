using FluentAssertions;
using UniversalDeviceToolkit.Lib.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Utils;

[Trait("Category", TestCategories.Unit)]
public sealed class PluginCatalogTagsTests
{
    [Theory]
    [InlineData("5.0.2", "plugin-catalog")]
    [InlineData("6.0.0", "plugin-catalog")]
    [InlineData("6.0.0+abc123", "plugin-catalog")]
    [InlineData("v6.0.0", "plugin-catalog")]
    [InlineData("6.0.0-preview.1", "plugin-catalog-preview")]
    [InlineData("v6.0.0-preview.1", "plugin-catalog-preview")]
    [InlineData("6.0.0-preview.1+deadbeef", "plugin-catalog-preview")]
    public void ResolveTag_UsesPrereleaseHyphenLikeReleaseWorkflow(string informationalVersion, string expectedTag)
    {
        PluginCatalogTags.ResolveTag(informationalVersion).Should().Be(expectedTag);
    }

    [Fact]
    public void IsCatalogTag_RecognizesStableAndPreview()
    {
        PluginCatalogTags.IsCatalogTag("plugin-catalog").Should().BeTrue();
        PluginCatalogTags.IsCatalogTag("plugin-catalog-preview").Should().BeTrue();
        PluginCatalogTags.IsCatalogTag("v6.0.0-preview.1").Should().BeFalse();
        PluginCatalogTags.IsCatalogTag("latest").Should().BeFalse();
    }

    [Fact]
    public void StoreAndApiUrls_UseTheResolvedTag()
    {
        PluginCatalogTags.StoreDownloadUrl(PluginCatalogTags.Stable)
            .Should().Be("https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog/store.json");
        PluginCatalogTags.StoreDownloadUrl(PluginCatalogTags.Preview)
            .Should().Be("https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/plugin-catalog-preview/store.json");
        PluginCatalogTags.ReleasesApiUrl(PluginCatalogTags.Preview)
            .Should().Be("https://api.github.com/repos/SSC-STUDIO/UniversalDeviceToolkit/releases/tags/plugin-catalog-preview");
    }
}
