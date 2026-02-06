using System;
using System.Collections.Generic;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using Xunit;

namespace LenovoLegionToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class VersionCheckerTests
{
    private readonly VersionChecker _versionChecker;

    public VersionCheckerTests()
    {
        _versionChecker = new VersionChecker("2.14.0");
    }

    [Fact]
    public void IsCompatible_WithHigherOrEqualVersion_ShouldReturnTrue()
    {
        // Arrange
        var minVersion = "2.13.0";

        // Act
        var result = _versionChecker.IsCompatible(minVersion);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCompatible_WithExactVersion_ShouldReturnTrue()
    {
        // Arrange
        var minVersion = "2.14.0";

        // Act
        var result = _versionChecker.IsCompatible(minVersion);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCompatible_WithLowerVersion_ShouldReturnFalse()
    {
        // Arrange
        var minVersion = "2.15.0";

        // Act
        var result = _versionChecker.IsCompatible(minVersion);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsCompatible_WithEmptyVersion_ShouldReturnTrue()
    {
        // Arrange
        var minVersion = string.Empty;

        // Act
        var result = _versionChecker.IsCompatible(minVersion);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCompatible_WithNullVersion_ShouldReturnTrue()
    {
        // Arrange
        string? minVersion = null;

        // Act
        var result = _versionChecker.IsCompatible(minVersion!);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsCompatible_WithWhitespaceVersion_ShouldReturnTrue()
    {
        // Arrange
        var minVersion = "   ";

        // Act
        var result = _versionChecker.IsCompatible(minVersion);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsUpdateAvailable_WithHigherVersion_ShouldReturnTrue()
    {
        // Arrange
        var currentVersion = "1.0.0";
        var newVersion = "2.0.0";

        // Act
        var result = _versionChecker.IsUpdateAvailable(currentVersion, newVersion);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsUpdateAvailable_WithSameVersion_ShouldReturnFalse()
    {
        // Arrange
        var currentVersion = "2.14.0";
        var newVersion = "2.14.0";

        // Act
        var result = _versionChecker.IsUpdateAvailable(currentVersion, newVersion);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsUpdateAvailable_WithLowerVersion_ShouldReturnFalse()
    {
        // Arrange
        var currentVersion = "3.0.0";
        var newVersion = "2.0.0";

        // Act
        var result = _versionChecker.IsUpdateAvailable(currentVersion, newVersion);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsUpdateAvailable_WithEmptyNewVersion_ShouldReturnFalse()
    {
        // Arrange
        var currentVersion = "1.0.0";
        var newVersion = string.Empty;

        // Act
        var result = _versionChecker.IsUpdateAvailable(currentVersion, newVersion);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CompareVersions_V1GreaterThanV2_ShouldReturnPositive()
    {
        // Arrange
        var version1 = "2.0.0";
        var version2 = "1.0.0";

        // Act
        var result = _versionChecker.CompareVersions(version1, version2);

        // Assert
        result.Should().BePositive();
    }

    [Fact]
    public void CompareVersions_V1LessThanV2_ShouldReturnNegative()
    {
        // Arrange
        var version1 = "1.0.0";
        var version2 = "2.0.0";

        // Act
        var result = _versionChecker.CompareVersions(version1, version2);

        // Assert
        result.Should().BeNegative();
    }

    [Fact]
    public void CompareVersions_SameVersion_ShouldReturnZero()
    {
        // Arrange
        var version1 = "1.0.0";
        var version2 = "1.0.0";

        // Act
        var result = _versionChecker.CompareVersions(version1, version2);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CompareVersions_WithEmptyVersion_ShouldTreatEmptyAsZero()
    {
        // Arrange
        var version1 = "1.0.0";
        var version2 = string.Empty;

        // Act
        var result = _versionChecker.CompareVersions(version1, version2);

        // Assert - empty string is treated as "0.0.0.0", so 1.0.0 > 0.0.0.0
        result.Should().BePositive();
    }

    [Fact]
    public void CheckCompatibility_AllCompatible_ShouldReturnEmptyList()
    {
        // Arrange
        var plugins = new List<PluginManifest>
        {
            Builder.PluginManifest().WithMinimumHostVersion("2.0.0").Build(),
            Builder.PluginManifest().WithMinimumHostVersion("1.0.0").Build()
        };

        // Act
        var result = _versionChecker.CheckCompatibility(plugins);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void CheckCompatibility_SomeIncompatible_ShouldReturnOnlyIncompatible()
    {
        // Arrange
        var plugins = new List<PluginManifest>
        {
            Builder.PluginManifest().WithId("plugin1").WithMinimumHostVersion("2.0.0").Build(),
            Builder.PluginManifest().WithId("plugin2").WithMinimumHostVersion("3.0.0").Build(),
            Builder.PluginManifest().WithId("plugin3").WithMinimumHostVersion("1.0.0").Build()
        };

        // Act
        var result = _versionChecker.CheckCompatibility(plugins);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(p => p.Id == "plugin2");
    }

    [Fact]
    public void GetAvailableUpdates_WithUpdates_ShouldReturnUpdateList()
    {
        // Arrange
        var installedPlugins = new Dictionary<string, string>
        {
            { "plugin1", "1.0.0" },
            { "plugin2", "2.0.0" }
        };

        var availablePlugins = new List<PluginManifest>
        {
            Builder.PluginManifest().WithId("plugin1").WithVersion("1.5.0").Build(),
            Builder.PluginManifest().WithId("plugin2").WithVersion("2.0.0").Build(),
            Builder.PluginManifest().WithId("plugin3").WithVersion("1.0.0").Build()
        };

        // Act
        var result = _versionChecker.GetAvailableUpdates(installedPlugins, availablePlugins);

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(u => u.PluginId == "plugin1");
        result.First(u => u.PluginId == "plugin1").NewVersion.Should().Be("1.5.0");
    }

    [Fact]
    public void GetAvailableUpdates_NoUpdates_ShouldReturnEmptyList()
    {
        // Arrange
        var installedPlugins = new Dictionary<string, string>
        {
            { "plugin1", "1.0.0" }
        };

        var availablePlugins = new List<PluginManifest>
        {
            Builder.PluginManifest().WithId("plugin1").WithVersion("1.0.0").Build()
        };

        // Act
        var result = _versionChecker.GetAvailableUpdates(installedPlugins, availablePlugins);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("2.14.0", "2.14.0", true)]
    [InlineData("2.14.0", "2.13.0", true)]
    [InlineData("2.14.0", "2.15.0", false)]
    [InlineData("2.14.0", "3.0.0", false)]
    [InlineData("2.14.0", "2.14.1", false)]
    public void IsCompatible_WithVariousVersions_ReturnsExpectedResult(
        string currentVersion, string minimumVersion, bool expected)
    {
        // Arrange
        var checker = new VersionChecker(currentVersion);

        // Act
        var result = checker.IsCompatible(minimumVersion);

        // Assert
        result.Should().Be(expected);
    }
}

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginUpdateInfoTests
{
    [Fact]
    public void PluginUpdateInfo_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var updateInfo = new PluginUpdateInfo();

        // Assert
        updateInfo.PluginId.Should().BeEmpty();
        updateInfo.CurrentVersion.Should().BeEmpty();
        updateInfo.NewVersion.Should().BeEmpty();
        updateInfo.DownloadUrl.Should().BeEmpty();
        updateInfo.Changelog.Should().BeEmpty();
        updateInfo.ReleaseDate.Should().BeEmpty();
    }

    [Fact]
    public void PluginUpdateInfo_ShouldStoreValues()
    {
        // Arrange
        var updateInfo = new PluginUpdateInfo
        {
            PluginId = "test-plugin",
            CurrentVersion = "1.0.0",
            NewVersion = "2.0.0",
            DownloadUrl = "https://example.com/update.zip",
            Changelog = "Fixed bugs",
            ReleaseDate = "2026-01-01"
        };

        // Assert
        updateInfo.PluginId.Should().Be("test-plugin");
        updateInfo.CurrentVersion.Should().Be("1.0.0");
        updateInfo.NewVersion.Should().Be("2.0.0");
        updateInfo.DownloadUrl.Should().Be("https://example.com/update.zip");
        updateInfo.Changelog.Should().Be("Fixed bugs");
        updateInfo.ReleaseDate.Should().Be("2026-01-01");
    }
}

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class UpdateCheckResultTests
{
    [Fact]
    public void HasUpdates_WhenNoUpdates_ShouldReturnFalse()
    {
        // Arrange
        var result = new UpdateCheckResult
        {
            AvailableUpdates = new List<PluginUpdateInfo>()
        };

        // Assert
        result.HasUpdates.Should().BeFalse();
    }

    [Fact]
    public void HasUpdates_WhenHasUpdates_ShouldReturnTrue()
    {
        // Arrange
        var result = new UpdateCheckResult
        {
            AvailableUpdates = new List<PluginUpdateInfo>
            {
                new PluginUpdateInfo { PluginId = "plugin1" }
            }
        };

        // Assert
        result.HasUpdates.Should().BeTrue();
    }

    [Fact]
    public void UpdateCheckResult_ShouldStoreValues()
    {
        // Arrange
        var now = DateTime.Now;
        var result = new UpdateCheckResult
        {
            IsSuccess = true,
            ErrorMessage = null,
            LastCheckTime = now,
            AvailableUpdates = new List<PluginUpdateInfo>()
        };

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.LastCheckTime.Should().Be(now);
        result.HasUpdates.Should().BeFalse();
    }

    [Fact]
    public void UpdateCheckResult_WithError_ShouldStoreErrorMessage()
    {
        // Arrange
        var result = new UpdateCheckResult
        {
            IsSuccess = false,
            ErrorMessage = "Network error"
        };

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Network error");
    }
}

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginManifestTests
{
    [Fact]
    public void PluginManifest_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var manifest = new PluginManifest();

        // Assert
        manifest.Id.Should().BeEmpty();
        manifest.Name.Should().BeEmpty();
        manifest.Description.Should().BeEmpty();
        manifest.Version.Should().Be("1.0.0");
        manifest.MinimumHostVersion.Should().Be("1.0.0");
        manifest.DownloadUrl.Should().BeEmpty();
        manifest.IsSystemPlugin.Should().BeFalse();
    }

    [Fact]
    public void PluginManifest_ShouldStoreAllProperties()
    {
        // Arrange & Act
        var manifest = Builder.PluginManifest()
            .WithId("custom-mouse")
            .WithName("Custom Mouse")
            .WithDescription("Custom cursor plugin")
            .WithVersion("1.0.0")
            .WithMinimumHostVersion("2.14.0")
            .WithDownloadUrl("https://example.com/custom-mouse.zip")
            .WithAuthor("LLT Team")
            .AsSystemPlugin()
            .WithFileSize(2048)
            .WithTags("utility", "customization")
            .WithDependencies("dependency1")
            .Build();

        // Assert
        manifest.Id.Should().Be("custom-mouse");
        manifest.Name.Should().Be("Custom Mouse");
        manifest.Description.Should().Be("Custom cursor plugin");
        manifest.Version.Should().Be("1.0.0");
        manifest.MinimumHostVersion.Should().Be("2.14.0");
        manifest.DownloadUrl.Should().Be("https://example.com/custom-mouse.zip");
        manifest.Author.Should().Be("LLT Team");
        manifest.IsSystemPlugin.Should().BeTrue();
        manifest.FileSize.Should().Be(2048);
        manifest.Tags.Should().Contain("utility");
        manifest.Tags.Should().Contain("customization");
        manifest.Dependencies.Should().Contain("dependency1");
    }
}

[Trait("Category", TestCategories.Plugin)]
[Trait("Category", TestCategories.Unit)]
public class PluginMetadataTests
{
    [Fact]
    public void PluginMetadata_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var metadata = new PluginMetadata();

        // Assert
        metadata.Id.Should().BeEmpty();
        metadata.Name.Should().BeEmpty();
        metadata.Description.Should().BeEmpty();
        metadata.Version.Should().Be("1.0.0");
        metadata.MinimumHostVersion.Should().Be("1.0.0");
        metadata.IsSystemPlugin.Should().BeFalse();
    }

    [Fact]
    public void PluginMetadata_ShouldStoreAllProperties()
    {
        // Arrange & Act
        var metadata = Builder.PluginMetadata()
            .WithId("shell-integration")
            .WithName("Shell Integration")
            .WithDescription("Shell enhancements")
            .WithIcon("Apps24")
            .AsSystemPlugin()
            .WithVersion("1.0.0")
            .WithMinimumHostVersion("2.14.0")
            .WithAuthor("LLT Team")
            .WithDependencies("dep1", "dep2")
            .WithFilePath("C:\\plugins\\shell-integration\\dll")
            .Build();

        // Assert
        metadata.Id.Should().Be("shell-integration");
        metadata.Name.Should().Be("Shell Integration");
        metadata.Description.Should().Be("Shell enhancements");
        metadata.Icon.Should().Be("Apps24");
        metadata.IsSystemPlugin.Should().BeTrue();
        metadata.Version.Should().Be("1.0.0");
        metadata.MinimumHostVersion.Should().Be("2.14.0");
        metadata.Author.Should().Be("LLT Team");
        metadata.Dependencies.Should().HaveCount(2);
        metadata.FilePath.Should().Be("C:\\plugins\\shell-integration\\dll");
    }
}
