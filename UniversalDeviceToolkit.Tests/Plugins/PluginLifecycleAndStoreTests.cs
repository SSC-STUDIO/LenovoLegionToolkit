using System;
using System.Collections.Generic;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Plugins;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Plugins;

[Trait("Category", TestCategories.Unit)]
public class PluginLifecycleAndStoreTests
{
    #region PluginTransitionRejectionReason Enum Tests

    [Fact]
    public void PluginTransitionRejectionReason_Has3Values()
    {
        Enum.GetValues<PluginTransitionRejectionReason>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(PluginTransitionRejectionReason.None)]
    [InlineData(PluginTransitionRejectionReason.IllegalTransition)]
    [InlineData(PluginTransitionRejectionReason.UnknownState)]
    public void PluginTransitionRejectionReason_AllValues_ShouldBeDefined(PluginTransitionRejectionReason reason)
    {
        Enum.IsDefined(reason).Should().BeTrue();
    }

    [Fact]
    public void PluginTransitionRejectionReason_None_ShouldBeZero()
    {
        ((int)PluginTransitionRejectionReason.None).Should().Be(0);
    }

    #endregion

    #region PluginTransitionResult Tests

    [Fact]
    public void PluginTransitionResult_Allow_ShouldSetAllowed()
    {
        var result = PluginTransitionResult.Allow(PluginState.NotInstalled, PluginState.Installed);
        result.IsAllowed.Should().BeTrue();
        result.From.Should().Be(PluginState.NotInstalled);
        result.To.Should().Be(PluginState.Installed);
        result.Reason.Should().Be(PluginTransitionRejectionReason.None);
    }

    [Fact]
    public void PluginTransitionResult_Reject_ShouldSetRejected()
    {
        var result = PluginTransitionResult.Reject(
            PluginState.Enabled,
            PluginState.Installed,
            PluginTransitionRejectionReason.IllegalTransition);
        result.IsAllowed.Should().BeFalse();
        result.From.Should().Be(PluginState.Enabled);
        result.To.Should().Be(PluginState.Installed);
        result.Reason.Should().Be(PluginTransitionRejectionReason.IllegalTransition);
    }

    [Fact]
    public void PluginTransitionResult_Reject_UnknownState_ShouldSetReason()
    {
        var result = PluginTransitionResult.Reject(
            PluginState.Error,
            (PluginState)999,
            PluginTransitionRejectionReason.UnknownState);
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Be(PluginTransitionRejectionReason.UnknownState);
    }

    #endregion

    #region PluginLifecycleStateMachine Allowed Transitions Tests

    private readonly PluginLifecycleStateMachine _sm = new();

    [Theory]
    [InlineData(PluginState.NotInstalled, PluginState.Installed)]
    [InlineData(PluginState.NotInstalled, PluginState.Error)]
    [InlineData(PluginState.Installed, PluginState.Enabled)]
    [InlineData(PluginState.Installed, PluginState.Disabled)]
    [InlineData(PluginState.Installed, PluginState.NotInstalled)]
    [InlineData(PluginState.Installed, PluginState.Error)]
    [InlineData(PluginState.Enabled, PluginState.Installed)]
    [InlineData(PluginState.Enabled, PluginState.Disabled)]
    [InlineData(PluginState.Enabled, PluginState.NotInstalled)]
    [InlineData(PluginState.Enabled, PluginState.Error)]
    [InlineData(PluginState.Disabled, PluginState.Enabled)]
    [InlineData(PluginState.Disabled, PluginState.Installed)]
    [InlineData(PluginState.Disabled, PluginState.NotInstalled)]
    [InlineData(PluginState.Disabled, PluginState.Error)]
    [InlineData(PluginState.Error, PluginState.Installed)]
    [InlineData(PluginState.Error, PluginState.NotInstalled)]
    public void Validate_AllowedTransitions_ShouldReturnAllowed(PluginState from, PluginState to)
    {
        var result = _sm.Validate(from, to);
        result.IsAllowed.Should().BeTrue();
        result.Reason.Should().Be(PluginTransitionRejectionReason.None);
    }

    [Theory]
    [InlineData(PluginState.NotInstalled, PluginState.Enabled)]
    [InlineData(PluginState.NotInstalled, PluginState.Disabled)]
    [InlineData(PluginState.Installed, PluginState.Installed)]
    [InlineData(PluginState.Enabled, PluginState.Enabled)]
    [InlineData(PluginState.Disabled, PluginState.Disabled)]
    [InlineData(PluginState.Error, PluginState.Error)]
    [InlineData(PluginState.Error, PluginState.Enabled)]
    [InlineData(PluginState.Error, PluginState.Disabled)]
    [InlineData(PluginState.Enabled, PluginState.Error)] // this IS allowed
    public void Validate_DisallowedTransitions_ShouldReturnRejected(PluginState from, PluginState to)
    {
        var result = _sm.Validate(from, to);
        if (from == PluginState.Enabled && to == PluginState.Error)
        {
            result.IsAllowed.Should().BeTrue();
            return;
        }
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Be(PluginTransitionRejectionReason.IllegalTransition);
    }

    [Fact]
    public void Validate_UnknownState_ShouldReturnUnknownStateReason()
    {
        var result = _sm.Validate((PluginState)999, PluginState.Installed);
        result.IsAllowed.Should().BeFalse();
        result.Reason.Should().Be(PluginTransitionRejectionReason.UnknownState);
    }

    [Fact]
    public void CanTransition_Allowed_ShouldReturnTrue()
    {
        _sm.CanTransition(PluginState.NotInstalled, PluginState.Installed).Should().BeTrue();
    }

    [Fact]
    public void CanTransition_Disallowed_ShouldReturnFalse()
    {
        _sm.CanTransition(PluginState.NotInstalled, PluginState.Enabled).Should().BeFalse();
    }

    #endregion

    #region PluginLifecycleStateMachine ValidateAndLog Tests

    [Fact]
    public void ValidateAndLog_Allowed_ShouldReturnAllowed()
    {
        var result = _sm.ValidateAndLog("test", PluginState.Installed, PluginState.Enabled);
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void ValidateAndLog_Rejected_ShouldReturnRejected()
    {
        var result = _sm.ValidateAndLog("test", PluginState.Error, PluginState.Enabled);
        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void ValidateAndLog_NullPluginId_ShouldNotThrow()
    {
        var act = () => _sm.ValidateAndLog(null, PluginState.Installed, PluginState.Enabled);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateAndLog_EmptyPluginId_ShouldNotThrow()
    {
        var act = () => _sm.ValidateAndLog("", PluginState.Installed, PluginState.Enabled);
        act.Should().NotThrow();
    }

    #endregion

    #region PluginStateChangedEventArgs Extended Tests

    [Fact]
    public void PluginStateChangedEventArgs_AllCombinations_ShouldRetainValues()
    {
        var args = new PluginStateChangedEventArgs("p", PluginState.Error, PluginState.Installed, null);
        args.PluginId.Should().Be("p");
        args.OldState.Should().Be(PluginState.Error);
        args.NewState.Should().Be(PluginState.Installed);
        args.ErrorMessage.Should().BeNull();
    }

    #endregion

    #region PluginManifest Store Contribution Integration Tests

    [Fact]
    public void PluginManifest_WithAllSections_ShouldRetainValues()
    {
        var m = new PluginManifest
        {
            Id = "full-plugin",
            Name = "Full Plugin",
            Description = "Full desc",
            Details = "Full details",
            UsageGuide = "Full guide",
            Author = "Author",
            Version = "3.0.0",
            MinimumHostVersion = "2.0.0",
            DownloadUrl = "https://example.com/dl",
            FileHash = "sha256hash",
            FileSize = 4096,
            ReleaseDate = "2024-06-01",
            Changelog = "v3.0",
            IsSystemPlugin = true,
            Dependencies = new[] { "dep1", "dep2" },
            Tags = new[] { "tag1" },
            Store = new PluginManifestStore
            {
                Description = "Store desc",
                Details = "Store details",
                UsageGuide = "Store guide"
            },
            Contributes = new PluginManifestContributions
            {
                FeaturePage = new PluginManifestPageContribution { Class = "FP", Title = "Features" },
                SettingsPage = new PluginManifestPageContribution { Class = "SP", Title = "Settings" },
                Runtime = new PluginManifestRuntimeContribution { Class = "RT" },
                OptimizationActions = new List<PluginManifestOptimizationContribution>
                {
                    new() { Id = "opt1", Key = "k1", Title = "Opt 1", Description = "Desc 1", Recommended = true }
                }
            },
            Localizations = new Dictionary<string, PluginManifestLocalization>
            {
                ["en"] = new() { Name = "English", Description = "EN" }
            }
        };
        m.Id.Should().Be("full-plugin");
        m.Store.Should().NotBeNull();
        m.Contributes!.OptimizationActions.Should().HaveCount(1);
        m.Localizations.Should().HaveCount(1);
    }

    #endregion
}