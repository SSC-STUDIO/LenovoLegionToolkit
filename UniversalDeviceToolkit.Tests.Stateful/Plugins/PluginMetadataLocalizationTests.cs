using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Collection(TestCollections.Localization)]
[Trait("Category", TestCategories.Unit)]
public class PluginMetadataLocalizationTests
{
    [Fact]
    public void GetDisplayName_ShouldReturnLocalizedNameForMatchingCulture()
    {
        var metadata = CreateMetadata();

        metadata.GetDisplayName(new CultureInfo("zh-Hans")).Should().Be("光标与指针");
    }

    [Fact]
    public void GetDisplayName_ShouldFallbackToDefaultForMissingCulture()
    {
        var metadata = CreateMetadata();

        metadata.GetDisplayName(new CultureInfo("ja-JP")).Should().Be("Cursor & Pointer");
    }

    [Fact]
    public void GetDisplayDescriptionAndTags_ShouldUseLocalizedValuesAndFallbackToDefault()
    {
        var metadata = CreateMetadata();

        metadata.GetDisplayDescription(new CultureInfo("zh-Hans")).Should().Be("自定义鼠标光标样式行为与鼠标设置。");
        metadata.GetDisplayTags(new CultureInfo("zh-Hans")).Should().Equal("鼠标", "自定义", "光标", "生产力");
        metadata.GetDisplayDescription(new CultureInfo("ja-JP")).Should().Be("Customize mouse cursor style behavior and mouse settings.");
        metadata.GetDisplayTags(new CultureInfo("ja-JP")).Should().Equal("mouse", "customization", "cursor", "productivity");
    }

    [Fact]
    public void GetDisplayValues_ShouldNormalizeRegionalCultureToCanonicalLanguage()
    {
        var metadata = new PluginMetadata
        {
            Name = "Fallback Name",
            LocalizedNames = new Dictionary<string, string>
            {
                ["zh-Hant"] = "繁體名稱",
                ["en"] = "English Name"
            }
        };

        metadata.GetDisplayName(new CultureInfo("zh-TW")).Should().Be("繁體名稱");
        metadata.GetDisplayName(new CultureInfo("de-DE")).Should().Be("English Name");
    }

    private static PluginMetadata CreateMetadata()
    {
        return new PluginMetadata
        {
            Name = "Fallback Name",
            Description = "Fallback Description",
            Tags = new[] { "fallback" },
            LocalizedNames = new Dictionary<string, string>
            {
                ["default"] = "Cursor & Pointer",
                ["zh-Hans"] = "光标与指针"
            },
            LocalizedDescriptions = new Dictionary<string, string>
            {
                ["default"] = "Customize mouse cursor style behavior and mouse settings.",
                ["zh-Hans"] = "自定义鼠标光标样式行为与鼠标设置。"
            },
            LocalizedTags = new Dictionary<string, IReadOnlyList<string>>
            {
                ["default"] = new[] { "mouse", "customization", "cursor", "productivity" },
                ["zh-Hans"] = new[] { "鼠标", "自定义", "光标", "生产力" }
            }
        };
    }
}
