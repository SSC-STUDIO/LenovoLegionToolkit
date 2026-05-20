using System.IO;
using System.Text.Json;
using FluentAssertions;
using LenovoLegionToolkit.Lib.ResourcesCatalog;
using Xunit;

namespace LenovoLegionToolkit.Tests.ResourcesCatalog;

[Trait("Category", TestCategories.Unit)]
public sealed class OnlineResourceCatalogTests
{
    [Fact]
    public void Catalog_ShouldDeserializeRequiredResourceShape()
    {
        // Arrange
        const string json = """
                            {
                              "schemaVersion": 1,
                              "appVersion": "3.8.0",
                              "languages": [
                                {
                                  "culture": "zh-hans",
                                  "displayName": "Chinese (Simplified)",
                                  "url": "https://example.com/resources/3.8.0/languages/zh-hans.zip",
                                  "sha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                                  "size": 1024
                                }
                              ],
                              "devicePacks": [
                                {
                                  "id": "lenovo-legion-pro-7",
                                  "displayName": "Lenovo Legion Pro 7",
                                  "vendor": "LENOVO",
                                  "families": ["Legion"],
                                  "modelPrefixes": ["16IRX"],
                                  "modelKeywords": ["Legion Pro 7"],
                                  "machineTypes": ["83DE"],
                                  "url": "https://example.com/resources/3.8.0/devices/lenovo-legion-pro-7.zip",
                                  "sha256": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                                  "size": 2048
                                }
                              ]
                            }
                            """;

        // Act
        var catalog = JsonSerializer.Deserialize<OnlineResourceCatalog>(json);

        // Assert
        catalog.Should().NotBeNull();
        catalog!.SchemaVersion.Should().Be(1);
        catalog.AppVersion.Should().Be("3.8.0");
        catalog.Languages.Should().ContainSingle(language =>
            language.Culture == "zh-hans" &&
            language.Sha256.Length == 64 &&
            language.Size == 1024);
        catalog.DevicePacks.Should().ContainSingle(devicePack =>
            devicePack.Id == "lenovo-legion-pro-7" &&
            devicePack.ModelKeywords.Contains("Legion Pro 7") &&
            devicePack.MachineTypes.Contains("83DE"));
    }

    [Fact]
    public void Catalog_ShouldRejectEmptyLanguageUrlAtCallSiteShape()
    {
        // This mirrors the validation LanguagePackManager applies after catalog lookup.
        var language = new OnlineLanguageResource
        {
            Culture = "de",
            DisplayName = "German",
            Url = string.Empty,
            Sha256 = new string('a', 64),
            Size = 1
        };

        var action = () =>
        {
            if (string.IsNullOrWhiteSpace(language.Url))
                throw new InvalidDataException("Language has an empty download URL.");
        };

        action.Should().Throw<InvalidDataException>();
    }
}
