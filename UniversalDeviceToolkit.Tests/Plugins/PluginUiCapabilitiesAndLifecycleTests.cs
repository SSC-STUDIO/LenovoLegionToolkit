using System;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
public class PluginUiCapabilitiesAndLifecycleTests
{
    #region PluginUiCapabilities Default Tests

    [Fact]
    public void PluginUiCapabilities_Default_ShouldBeAllFalse()
    {
        var caps = default(PluginUiCapabilities);
        caps.SupportsSettingsPage.Should().BeFalse();
        caps.SupportsFeaturePage.Should().BeFalse();
        caps.SupportsOptimizationCategory.Should().BeFalse();
        caps.SupportsWebPage.Should().BeFalse();
        caps.HasAny.Should().BeFalse();
    }

    [Fact]
    public void PluginUiCapabilities_HasAny_WhenSettingsTrue_ShouldBeTrue()
    {
        var caps = new PluginUiCapabilities { SupportsSettingsPage = true };
        caps.HasAny.Should().BeTrue();
    }

    [Fact]
    public void PluginUiCapabilities_HasAny_WhenFeatureTrue_ShouldBeTrue()
    {
        var caps = new PluginUiCapabilities { SupportsFeaturePage = true };
        caps.HasAny.Should().BeTrue();
    }

    [Fact]
    public void PluginUiCapabilities_HasAny_WhenWebPageTrue_ShouldBeTrue()
    {
        var caps = new PluginUiCapabilities { SupportsWebPage = true };
        caps.HasAny.Should().BeTrue();
    }

    [Fact]
    public void PluginUiCapabilities_HasAny_WhenOptimizationTrue_ShouldBeTrue()
    {
        var caps = new PluginUiCapabilities { SupportsOptimizationCategory = true };
        caps.HasAny.Should().BeTrue();
    }

    [Fact]
    public void PluginUiCapabilities_HasAny_WhenAllFalse_ShouldBeFalse()
    {
        var caps = new PluginUiCapabilities
        {
            SupportsSettingsPage = false,
            SupportsFeaturePage = false,
            SupportsOptimizationCategory = false,
            SupportsWebPage = false
        };
        caps.HasAny.Should().BeFalse();
    }

    #endregion

    #region PluginUiCapabilities Merge Tests

    [Fact]
    public void PluginUiCapabilities_Merge_NoOverlap_ShouldUnion()
    {
        var a = new PluginUiCapabilities { SupportsSettingsPage = true };
        var b = new PluginUiCapabilities { SupportsFeaturePage = true };
        var merged = a.Merge(b);
        merged.SupportsSettingsPage.Should().BeTrue();
        merged.SupportsFeaturePage.Should().BeTrue();
        merged.SupportsOptimizationCategory.Should().BeFalse();
        merged.HasAny.Should().BeTrue();
    }

    [Fact]
    public void PluginUiCapabilities_Merge_BothEmpty_ShouldBeEmpty()
    {
        var a = new PluginUiCapabilities();
        var b = new PluginUiCapabilities();
        var merged = a.Merge(b);
        merged.HasAny.Should().BeFalse();
    }

    [Fact]
    public void PluginUiCapabilities_Merge_BothSame_ShouldKeepTrue()
    {
        var a = new PluginUiCapabilities { SupportsOptimizationCategory = true };
        var b = new PluginUiCapabilities { SupportsOptimizationCategory = true };
        var merged = a.Merge(b);
        merged.SupportsOptimizationCategory.Should().BeTrue();
    }

    [Fact]
    public void PluginUiCapabilities_Merge_AllThree_ShouldHaveAll()
    {
        var a = new PluginUiCapabilities { SupportsSettingsPage = true, SupportsFeaturePage = true };
        var b = new PluginUiCapabilities { SupportsOptimizationCategory = true };
        var merged = a.Merge(b);
        merged.HasAny.Should().BeTrue();
        merged.SupportsSettingsPage.Should().BeTrue();
        merged.SupportsFeaturePage.Should().BeTrue();
        merged.SupportsOptimizationCategory.Should().BeTrue();
        merged.SupportsWebPage.Should().BeFalse();
    }

    [Fact]
    public void PluginUiCapabilities_Merge_WhenWebPageTrue_ShouldKeepWebPage()
    {
        var a = new PluginUiCapabilities { SupportsWebPage = true };
        var b = new PluginUiCapabilities { SupportsSettingsPage = true };
        var merged = a.Merge(b);
        merged.SupportsWebPage.Should().BeTrue();
        merged.SupportsSettingsPage.Should().BeTrue();
        merged.HasAny.Should().BeTrue();
    }

    #endregion

    #region PluginUiCapabilityResolver ResolveFromManifest Tests

    [Fact]
    public void ResolveFromManifest_Null_ShouldReturnDefault()
    {
        var caps = PluginUiCapabilityResolver.ResolveFromManifest(null);
        caps.HasAny.Should().BeFalse();
    }

    [Fact]
    public void ResolveFromManifest_NoContributes_ShouldReturnDefault()
    {
        var manifest = new PluginManifest();
        var caps = PluginUiCapabilityResolver.ResolveFromManifest(manifest);
        caps.HasAny.Should().BeFalse();
    }

    [Fact]
    public void ResolveFromManifest_WithSettingsPage_ShouldSupportSettings()
    {
        var manifest = new PluginManifest
        {
            Contributes = new PluginManifestContributions
            {
                SettingsPage = new PluginManifestPageContribution { Class = "Settings", Title = "Settings" }
            }
        };
        var caps = PluginUiCapabilityResolver.ResolveFromManifest(manifest);
        caps.SupportsSettingsPage.Should().BeTrue();
        caps.SupportsFeaturePage.Should().BeFalse();
    }

    [Fact]
    public void ResolveFromManifest_WithFeaturePage_ShouldSupportFeature()
    {
        var manifest = new PluginManifest
        {
            Contributes = new PluginManifestContributions
            {
                FeaturePage = new PluginManifestPageContribution { Class = "Features", Title = "Features" }
            }
        };
        var caps = PluginUiCapabilityResolver.ResolveFromManifest(manifest);
        caps.SupportsFeaturePage.Should().BeTrue();
        caps.SupportsSettingsPage.Should().BeFalse();
    }

    [Fact]
    public void ResolveFromManifest_WithBoth_ShouldSupportBoth()
    {
        var manifest = new PluginManifest
        {
            Contributes = new PluginManifestContributions
            {
                SettingsPage = new PluginManifestPageContribution { Class = "S", Title = "S" },
                FeaturePage = new PluginManifestPageContribution { Class = "F", Title = "F" }
            }
        };
        var caps = PluginUiCapabilityResolver.ResolveFromManifest(manifest);
        caps.SupportsSettingsPage.Should().BeTrue();
        caps.SupportsFeaturePage.Should().BeTrue();
    }

    [Fact]
    public void ResolveFromManifest_WithOptimizationActions_ShouldSupportOptimization()
    {
        var manifest = new PluginManifest
        {
            Contributes = new PluginManifestContributions
            {
                OptimizationActions = new System.Collections.Generic.List<PluginManifestOptimizationContribution>
                {
                    new() { Id = "opt1", Title = "Optimization 1" }
                }
            }
        };
        var caps = PluginUiCapabilityResolver.ResolveFromManifest(manifest);
        caps.SupportsOptimizationCategory.Should().BeTrue();
    }

    [Fact]
    public void ResolveFromManifest_OptimizationActions_NoId_ShouldNotSupport()
    {
        var manifest = new PluginManifest
        {
            Contributes = new PluginManifestContributions
            {
                OptimizationActions = new System.Collections.Generic.List<PluginManifestOptimizationContribution>
                {
                    new() { Title = "No ID" }
                }
            }
        };
        var caps = PluginUiCapabilityResolver.ResolveFromManifest(manifest);
        caps.SupportsOptimizationCategory.Should().BeFalse();
    }

    [Fact]
    public void ResolveFromManifest_OptimizationActions_NoTitle_ShouldNotSupport()
    {
        var manifest = new PluginManifest
        {
            Contributes = new PluginManifestContributions
            {
                OptimizationActions = new System.Collections.Generic.List<PluginManifestOptimizationContribution>
                {
                    new() { Id = "opt1" }
                }
            }
        };
        var caps = PluginUiCapabilityResolver.ResolveFromManifest(manifest);
        caps.SupportsOptimizationCategory.Should().BeFalse();
    }

    #endregion

    #region PluginUiCapabilityResolver SupportsOptimizationActions Tests

    [Fact]
    public void SupportsOptimizationActions_Null_ShouldReturnFalse()
    {
        PluginUiCapabilityResolver.SupportsOptimizationActions(null).Should().BeFalse();
    }

    [Fact]
    public void SupportsOptimizationActions_NoContributes_ShouldReturnFalse()
    {
        PluginUiCapabilityResolver.SupportsOptimizationActions(new PluginManifest()).Should().BeFalse();
    }

    [Fact]
    public void SupportsOptimizationActions_EmptyActions_ShouldReturnFalse()
    {
        var manifest = new PluginManifest
        {
            Contributes = new PluginManifestContributions
            {
                OptimizationActions = new System.Collections.Generic.List<PluginManifestOptimizationContribution>()
            }
        };
        PluginUiCapabilityResolver.SupportsOptimizationActions(manifest).Should().BeFalse();
    }

    [Fact]
    public void SupportsOptimizationActions_ValidActions_ShouldReturnTrue()
    {
        var manifest = new PluginManifest
        {
            Contributes = new PluginManifestContributions
            {
                OptimizationActions = new System.Collections.Generic.List<PluginManifestOptimizationContribution>
                {
                    new() { Id = "a", Title = "A" }
                }
            }
        };
        PluginUiCapabilityResolver.SupportsOptimizationActions(manifest).Should().BeTrue();
    }

    #endregion

    #region PluginUiCapabilityResolver GetOptimizationActionId Tests

    [Fact]
    public void GetOptimizationActionId_Null_ShouldReturnEmpty()
    {
        PluginUiCapabilityResolver.GetOptimizationActionId(null).Should().BeEmpty();
    }

    [Fact]
    public void GetOptimizationActionId_WithId_ShouldReturnId()
    {
        var action = new PluginManifestOptimizationContribution { Id = "my-id" };
        PluginUiCapabilityResolver.GetOptimizationActionId(action).Should().Be("my-id");
    }

    [Fact]
    public void GetOptimizationActionId_WithKeyFallback_ShouldReturnKey()
    {
        var action = new PluginManifestOptimizationContribution { Key = "my-key" };
        PluginUiCapabilityResolver.GetOptimizationActionId(action).Should().Be("my-key");
    }

    [Fact]
    public void GetOptimizationActionId_WithBoth_ShouldPreferId()
    {
        var action = new PluginManifestOptimizationContribution { Id = "id-val", Key = "key-val" };
        PluginUiCapabilityResolver.GetOptimizationActionId(action).Should().Be("id-val");
    }

    #endregion

    #region PluginUiCapabilityResolver ReadInstalledManifest Tests

    [Fact]
    public void ReadInstalledManifest_Null_ShouldReturnNull()
    {
        PluginUiCapabilityResolver.ReadInstalledManifest(null!).Should().BeNull();
    }

    [Fact]
    public void ReadInstalledManifest_Empty_ShouldReturnNull()
    {
        PluginUiCapabilityResolver.ReadInstalledManifest("").Should().BeNull();
    }

    [Fact]
    public void ReadInstalledManifest_Whitespace_ShouldReturnNull()
    {
        PluginUiCapabilityResolver.ReadInstalledManifest("  ").Should().BeNull();
    }

    [Fact]
    public void ReadInstalledManifest_NonExistent_ShouldReturnNull()
    {
        PluginUiCapabilityResolver.ReadInstalledManifest("nonexistent_plugin_xyz_123").Should().BeNull();
    }

    #endregion

    #region PluginUiCapabilityResolver ResolveFromInstalledManifest Tests

    [Fact]
    public void ResolveFromInstalledManifest_Null_ShouldReturnDefault()
    {
        var caps = PluginUiCapabilityResolver.ResolveFromInstalledManifest(null!);
        caps.HasAny.Should().BeFalse();
    }

    [Fact]
    public void ResolveFromInstalledManifest_Empty_ShouldReturnDefault()
    {
        var caps = PluginUiCapabilityResolver.ResolveFromInstalledManifest("");
        caps.HasAny.Should().BeFalse();
    }

    [Fact]
    public void ResolveFromInstalledManifest_NonExistent_ShouldReturnDefault()
    {
        var caps = PluginUiCapabilityResolver.ResolveFromInstalledManifest("nonexistent_plugin_xyz_123");
        caps.HasAny.Should().BeFalse();
    }

    #endregion

    #region PluginTransitionRejectionReason Value Tests

    [Fact]
    public void PluginTransitionRejectionReason_IllegalTransition_ShouldBeOne()
    {
        ((int)PluginTransitionRejectionReason.IllegalTransition).Should().Be(1);
    }

    [Fact]
    public void PluginTransitionRejectionReason_UnknownState_ShouldBeTwo()
    {
        ((int)PluginTransitionRejectionReason.UnknownState).Should().Be(2);
    }

    #endregion

    #region PluginTransitionResult Record Behavior Tests

    [Fact]
    public void PluginTransitionResult_Equality_SameValues_ShouldBeEqual()
    {
        var a = PluginTransitionResult.Allow(PluginState.Installed, PluginState.Enabled);
        var b = PluginTransitionResult.Allow(PluginState.Installed, PluginState.Enabled);
        a.Should().Be(b);
    }

    [Fact]
    public void PluginTransitionResult_Inequality_DifferentFrom_ShouldNotBeEqual()
    {
        var a = PluginTransitionResult.Allow(PluginState.Installed, PluginState.Enabled);
        var b = PluginTransitionResult.Allow(PluginState.Disabled, PluginState.Enabled);
        a.Should().NotBe(b);
    }

    [Fact]
    public void PluginTransitionResult_Inequality_DifferentTo_ShouldNotBeEqual()
    {
        var a = PluginTransitionResult.Allow(PluginState.Installed, PluginState.Enabled);
        var b = PluginTransitionResult.Allow(PluginState.Installed, PluginState.Disabled);
        a.Should().NotBe(b);
    }

    #endregion
}