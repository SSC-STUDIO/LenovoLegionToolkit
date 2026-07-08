using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LenovoLegionToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests;

[Trait("Category", TestCategories.Unit)]
public class PluginSignatureAndAdapterTests
{
    #region PluginSignatureValidationMode Enum Tests

    [Fact]
    public void PluginSignatureValidationMode_Has3Values()
    {
        Enum.GetValues<PluginSignatureValidationMode>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(PluginSignatureValidationMode.RequireSignature)]
    [InlineData(PluginSignatureValidationMode.AllowUnsigned)]
    [InlineData(PluginSignatureValidationMode.DisableValidation)]
    public void PluginSignatureValidationMode_AllValues_ShouldBeDefined(PluginSignatureValidationMode mode)
    {
        Enum.IsDefined(mode).Should().BeTrue();
    }

    #endregion

    #region PluginSignatureSettings Default Tests

    [Fact]
    public void PluginSignatureSettings_Defaults_ShouldHaveExpectedValues()
    {
        var s = new PluginSignatureSettings();
        s.ValidationMode.Should().Be(PluginSignatureValidationMode.RequireSignature);
        s.AllowTestCertificates.Should().BeFalse();
        s.TrustedIssuers.Should().BeEmpty();
        s.AllowedUnsignedPlugins.Should().BeEmpty();
        s.CheckRevocationStatus.Should().BeTrue();
    }

    #endregion

    #region PluginSignatureSettings Static Presets Tests

    [Fact]
    public void PluginSignatureSettings_Production_ShouldHaveStrictValues()
    {
        var s = PluginSignatureSettings.Production;
        s.ValidationMode.Should().Be(PluginSignatureValidationMode.RequireSignature);
        s.AllowTestCertificates.Should().BeFalse();
        s.CheckRevocationStatus.Should().BeTrue();
    }

    [Fact]
    public void PluginSignatureSettings_Development_ShouldHaveRelaxedValues()
    {
        var s = PluginSignatureSettings.Development;
        s.ValidationMode.Should().Be(PluginSignatureValidationMode.AllowUnsigned);
        s.AllowTestCertificates.Should().BeTrue();
        s.CheckRevocationStatus.Should().BeFalse();
    }

    [Fact]
    public void PluginSignatureSettings_Disabled_ShouldHaveNoValidation()
    {
        var s = PluginSignatureSettings.Disabled;
        s.ValidationMode.Should().Be(PluginSignatureValidationMode.DisableValidation);
        s.AllowTestCertificates.Should().BeTrue();
        s.CheckRevocationStatus.Should().BeFalse();
    }

    #endregion

    #region PluginSignatureSettings TryCreateFromEnvironmentValue Tests

    [Theory]
    [InlineData("require", PluginSignatureValidationMode.RequireSignature, true)]
    [InlineData("require-signature", PluginSignatureValidationMode.RequireSignature, true)]
    [InlineData("requiresignature", PluginSignatureValidationMode.RequireSignature, true)]
    [InlineData("production", PluginSignatureValidationMode.RequireSignature, true)]
    [InlineData("allowunsigned", PluginSignatureValidationMode.AllowUnsigned, true)]
    [InlineData("allow-unsigned", PluginSignatureValidationMode.AllowUnsigned, true)]
    [InlineData("development", PluginSignatureValidationMode.AllowUnsigned, true)]
    [InlineData("disable", PluginSignatureValidationMode.DisableValidation, true)]
    [InlineData("disabled", PluginSignatureValidationMode.DisableValidation, true)]
    [InlineData("disablevalidation", PluginSignatureValidationMode.DisableValidation, true)]
    [InlineData("disable-validation", PluginSignatureValidationMode.DisableValidation, true)]
    public void TryCreateFromEnvironmentValue_ValidValues_ShouldReturnExpected(string value, PluginSignatureValidationMode expectedMode, bool expectedSuccess)
    {
        var success = PluginSignatureSettings.TryCreateFromEnvironmentValue(value, out var settings);
        success.Should().Be(expectedSuccess);
        settings.ValidationMode.Should().Be(expectedMode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("invalid")]
    [InlineData("random")]
    public void TryCreateFromEnvironmentValue_InvalidValues_ShouldReturnProduction(string? value)
    {
        var success = PluginSignatureSettings.TryCreateFromEnvironmentValue(value, out var settings);
        success.Should().BeFalse();
        settings.ValidationMode.Should().Be(PluginSignatureValidationMode.RequireSignature);
    }

    [Fact]
    public void TryCreateFromEnvironmentValue_CaseInsensitive_ShouldWork()
    {
        var success = PluginSignatureSettings.TryCreateFromEnvironmentValue("DISABLE", out var settings);
        success.Should().BeTrue();
        settings.ValidationMode.Should().Be(PluginSignatureValidationMode.DisableValidation);
    }

    [Fact]
    public void TryCreateFromEnvironmentValue_WithWhitespace_ShouldWork()
    {
        var success = PluginSignatureSettings.TryCreateFromEnvironmentValue("  allowunsigned  ", out var settings);
        success.Should().BeTrue();
        settings.ValidationMode.Should().Be(PluginSignatureValidationMode.AllowUnsigned);
    }

    #endregion

    #region PluginSignatureSettings SetProperties Tests

    [Fact]
    public void PluginSignatureSettings_SetTrustedIssuers_ShouldRetainValues()
    {
        var s = new PluginSignatureSettings
        {
            TrustedIssuers = new[] { "thumb1", "thumb2" }
        };
        s.TrustedIssuers.Should().HaveCount(2);
        s.TrustedIssuers[0].Should().Be("thumb1");
    }

    [Fact]
    public void PluginSignatureSettings_SetAllowedUnsignedPlugins_ShouldRetainValues()
    {
        var s = new PluginSignatureSettings
        {
            AllowedUnsignedPlugins = new[] { "p1", "p2", "p3" }
        };
        s.AllowedUnsignedPlugins.Should().HaveCount(3);
    }

    #endregion

    #region PluginManifestAdapter Tests

    [Fact]
    public void PluginManifestAdapter_ShouldImplementIPlugin()
    {
        var manifest = new PluginManifest
        {
            Id = "test",
            Name = "Test",
            Description = "Desc",
            Icon = "icon",
            IsSystemPlugin = true,
            Dependencies = new[] { "dep1" }
        };
        var adapter = new PluginManifestAdapter(manifest);
        adapter.Should().BeAssignableTo<IPlugin>();
    }

    [Fact]
    public void PluginManifestAdapter_ShouldDelegateToManifest()
    {
        var manifest = new PluginManifest
        {
            Id = "my-plugin",
            Name = "My Plugin",
            Description = "A plugin",
            Icon = "my-icon",
            IsSystemPlugin = false,
            Dependencies = new[] { "dep-a" }
        };
        var adapter = new PluginManifestAdapter(manifest);

        adapter.Id.Should().Be("my-plugin");
        adapter.Name.Should().Be("My Plugin");
        adapter.Description.Should().Be("A plugin");
        adapter.Icon.Should().Be("my-icon");
        adapter.IsSystemPlugin.Should().BeFalse();
        adapter.Dependencies.Should().HaveCount(1);
        adapter.Dependencies![0].Should().Be("dep-a");
    }

    [Fact]
    public void PluginManifestAdapter_Manifest_ShouldReturnOriginal()
    {
        var manifest = new PluginManifest { Id = "x" };
        var adapter = new PluginManifestAdapter(manifest);
        adapter.Manifest.Should().BeSameAs(manifest);
    }

    [Fact]
    public void PluginManifestAdapter_LifecycleMethods_ShouldNotThrow()
    {
        var adapter = new PluginManifestAdapter(new PluginManifest());
        var act = () =>
        {
            adapter.OnInstalled();
            adapter.OnUninstalled();
            adapter.OnShutdown();
            adapter.Stop();
        };
        act.Should().NotThrow();
    }

    [Fact]
    public void PluginManifestAdapter_NullDependencies_ShouldBeNull()
    {
        var manifest = new PluginManifest { Dependencies = null };
        var adapter = new PluginManifestAdapter(manifest);
        adapter.Dependencies.Should().BeNull();
    }

    #endregion

    #region PluginManifestContributions with Optimization Actions Tests

    [Fact]
    public void PluginManifestContributions_WithOptimizationActions_ShouldRetainValues()
    {
        var actions = new List<PluginManifestOptimizationContribution>
        {
            new() { Id = "a1", Key = "k1", Title = "Action 1", Description = "Desc 1", Recommended = true },
            new() { Id = "a2", Key = "k2", Title = "Action 2", Description = "Desc 2", Recommended = false }
        };
        var c = new PluginManifestContributions { OptimizationActions = actions };
        c.OptimizationActions.Should().HaveCount(2);
        c.OptimizationActions![0].Recommended.Should().BeTrue();
        c.OptimizationActions[1].Recommended.Should().BeFalse();
    }

    #endregion

    #region PluginManifest Store Property Tests

    [Fact]
    public void PluginManifest_Store_Set_ShouldRetainValues()
    {
        var m = new PluginManifest
        {
            Store = new PluginManifestStore
            {
                Description = "Store desc",
                Details = "Store details",
                UsageGuide = "Store guide"
            }
        };
        m.Store.Should().NotBeNull();
        m.Store!.Description.Should().Be("Store desc");
    }

    [Fact]
    public void PluginManifest_Localizations_Set_ShouldRetainValues()
    {
        var localizations = new Dictionary<string, PluginManifestLocalization>
        {
            ["en"] = new() { Name = "English", Description = "EN desc" },
            ["zh"] = new() { Name = "\u4E2D\u6587", Description = "ZH desc" }
        };
        var m = new PluginManifest { Localizations = localizations };
        m.Localizations.Should().HaveCount(2);
        m.Localizations!["en"]!.Name.Should().Be("English");
        m.Localizations["zh"]!.Name.Should().Be("\u4E2D\u6587");
    }

    [Fact]
    public void PluginManifest_Contributes_Set_ShouldRetainValues()
    {
        var m = new PluginManifest
        {
            Contributes = new PluginManifestContributions
            {
                FeaturePage = new PluginManifestPageContribution { Class = "FeaturePage", Title = "Features" },
                SettingsPage = new PluginManifestPageContribution { Class = "SettingsPage", Title = "Settings" },
                Runtime = new PluginManifestRuntimeContribution { Class = "Runtime" }
            }
        };
        m.Contributes.Should().NotBeNull();
        m.Contributes!.FeaturePage!.Class.Should().Be("FeaturePage");
        m.Contributes.SettingsPage!.Title.Should().Be("Settings");
        m.Contributes.Runtime!.Class.Should().Be("Runtime");
    }

    #endregion

    #region PluginDownloadProgress Extended Tests

    [Fact]
    public void PluginDownloadProgress_Completed_ShouldHaveAllFieldsSet()
    {
        var p = new PluginDownloadProgress
        {
            PluginId = "p1",
            BytesDownloaded = 1024,
            TotalBytes = 1024,
            ProgressPercentage = 100.0,
            IsCompleted = true,
            LocalFilePath = @"C:\plugins\p1.zip"
        };
        p.IsCompleted.Should().BeTrue();
        p.ProgressPercentage.Should().Be(100.0);
        p.BytesDownloaded.Should().Be(p.TotalBytes);
    }

    #endregion

    #region PluginStoreResponse Extended Tests

    [Fact]
    public void PluginStoreResponse_WithPlugins_ShouldRetainValues()
    {
        var r = new PluginStoreResponse
        {
            Plugins =
            {
                new PluginManifest { Id = "p1", Name = "Plugin 1" },
                new PluginManifest { Id = "p2", Name = "Plugin 2" }
            },
            LastUpdated = "2024-01-15",
            StoreVersion = "2.0.0"
        };
        r.Plugins.Should().HaveCount(2);
        r.LastUpdated.Should().Be("2024-01-15");
        r.StoreVersion.Should().Be("2.0.0");
    }

    #endregion

    #region GitHubFileResponse Extended Tests

    [Fact]
    public void GitHubFileResponse_SetProperties_ShouldRetainValues()
    {
        var g = new GitHubFileResponse
        {
            Name = "plugin.json",
            Path = "plugins/test/plugin.json",
            Sha = "abc123",
            Size = 512,
            Url = "https://api.github.com/repos/test/contents/plugin.json",
            HtmlUrl = "https://github.com/test/blob/main/plugin.json",
            GitUrl = "https://api.github.com/repos/test/git/blobs/abc123",
            DownloadUrl = "https://raw.githubusercontent.com/test/main/plugin.json",
            Type = "file",
            Content = "base64content",
            Encoding = "base64"
        };
        g.Name.Should().Be("plugin.json");
        g.Size.Should().Be(512);
        g.Encoding.Should().Be("base64");
    }

    #endregion
}