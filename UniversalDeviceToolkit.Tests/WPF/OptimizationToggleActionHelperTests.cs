using System.Threading.Tasks;
using FluentAssertions;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.WPF.Pages.WindowsOptimization;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class OptimizationToggleActionHelperTests
{
    [Theory]
    [InlineData("custom.mouse.cursor.auto-theme.enable", true, false)]
    [InlineData("shell.integration.disable", false, true)]
    [InlineData("cleanup.browserCache", false, false)]
    public void IsToggleAction_DetectsEnableDisableSuffixes(string key, bool isEnable, bool isDisable)
    {
        OptimizationToggleActionHelper.IsEnableAction(key).Should().Be(isEnable);
        OptimizationToggleActionHelper.IsDisableAction(key).Should().Be(isDisable);
        OptimizationToggleActionHelper.IsToggleAction(key).Should().Be(isEnable || isDisable);
    }

    [Theory]
    [InlineData("custom.mouse.cursor.auto-theme.enable", true, "custom.mouse.cursor.auto-theme.enable")]
    [InlineData("custom.mouse.cursor.auto-theme.enable", false, "custom.mouse.cursor.auto-theme.disable")]
    [InlineData("shell.integration.disable", false, "shell.integration.disable")]
    [InlineData("shell.integration.disable", true, "shell.integration.enable")]
    public void ResolveTargetActionKey_MapsCheckedStateToExpectedAction(string key, bool desiredSelected, string expected)
    {
        OptimizationToggleActionHelper.ResolveTargetActionKey(key, desiredSelected).Should().Be(expected);
    }

    [Fact]
    public void FindTogglePairs_ReturnsEnableDisablePair()
    {
        var enable = CreateAction("custom.mouse.cursor.auto-theme.enable", recommended: true);
        var disable = CreateAction("custom.mouse.cursor.auto-theme.disable", recommended: false);
        var other = CreateAction("cleanup.browserCache", recommended: true);

        var pairs = OptimizationToggleActionHelper.FindTogglePairs([enable, disable, other]);

        pairs.Should().ContainSingle();
        pairs[0].Enable.Should().BeSameAs(enable);
        pairs[0].Disable.Should().BeSameAs(disable);
    }

    [Fact]
    public void FindTogglePair_ReturnsNullForAnUnpairedAction()
    {
        var enable = CreateAction("custom.feature.enable", recommended: true);
        var disable = CreateAction("custom.feature.disable", recommended: false);
        var unrelated = CreateAction("custom.other", recommended: true);

        OptimizationToggleActionHelper.FindTogglePair(unrelated, [enable, disable, unrelated])
            .Should()
            .BeNull();
    }

    [Fact]
    public void GetRecommendedSelectedState_UsesEnableSideRecommendationForVisibleDisableRow()
    {
        var enable = CreateAction("custom.feature.enable", recommended: true);
        var disable = CreateAction("custom.feature.disable", recommended: false);

        OptimizationToggleActionHelper.GetRecommendedSelectedState(disable, [enable, disable])
            .Should()
            .BeTrue();
    }

    [Fact]
    public void CategorySelectRecommended_PreservesRecommendedFeatureStateForDisableRow()
    {
        var enable = CreateAction("custom.feature.enable", recommended: true);
        var disable = CreateAction("custom.feature.disable", recommended: false);
        OptimizationToggleActionHelper.ApplyTogglePairPresentation(true, enable, disable);
        using var category = new OptimizationCategoryViewModel(
            "custom",
            "Custom",
            "Custom",
            "{0} / {1}",
            [enable, disable]);

        category.SelectRecommended();

        disable.IsSelected.Should().BeTrue();
    }

    [Fact]
    public void ApplyTogglePairPresentation_ShowsEnableWhenDisabled()
    {
        var enable = CreateAction("custom.mouse.cursor.auto-theme.enable", recommended: true);
        var disable = CreateAction("custom.mouse.cursor.auto-theme.disable", recommended: false);

        OptimizationToggleActionHelper.ApplyTogglePairPresentation(false, enable, disable);

        enable.IsVisible.Should().BeTrue();
        disable.IsVisible.Should().BeFalse();
        enable.IsSelected.Should().BeFalse();
        disable.IsSelected.Should().BeFalse();
        enable.IsApplied.Should().BeFalse();
        disable.IsApplied.Should().BeFalse();
        enable.CanEdit.Should().BeTrue();
        enable.IsDirty.Should().BeFalse();
        enable.HasRecommendedTag.Should().BeTrue();
        disable.HasRecommendedTag.Should().BeFalse();

        enable.IsSelected = true;
        enable.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void ApplyTogglePairPresentation_ShowsDisableWhenEnabled()
    {
        var enable = CreateAction("custom.mouse.cursor.auto-theme.enable", recommended: true);
        var disable = CreateAction("custom.mouse.cursor.auto-theme.disable", recommended: false);

        OptimizationToggleActionHelper.ApplyTogglePairPresentation(true, enable, disable);

        enable.IsVisible.Should().BeFalse();
        disable.IsVisible.Should().BeTrue();
        enable.IsSelected.Should().BeFalse();
        disable.IsSelected.Should().BeTrue();
        enable.IsApplied.Should().BeFalse();
        disable.IsApplied.Should().BeTrue();
        disable.CanEdit.Should().BeTrue();
        disable.IsDirty.Should().BeFalse();
        enable.HasRecommendedTag.Should().BeTrue();
        disable.HasRecommendedTag.Should().BeFalse();

        disable.IsSelected = false;
        disable.IsDirty.Should().BeTrue();
    }

    [Fact]
    public void ApplyTogglePairPresentation_DoesNotTreatUnknownAsDisabled()
    {
        var enable = CreateAction("custom.mouse.cursor.auto-theme.enable", recommended: true);
        var disable = CreateAction("custom.mouse.cursor.auto-theme.disable", recommended: false);

        OptimizationToggleActionHelper.ApplyTogglePairPresentation(null, enable, disable);

        enable.IsVisible.Should().BeTrue();
        disable.IsVisible.Should().BeFalse();
        enable.IsEnabled.Should().BeFalse();
        disable.IsEnabled.Should().BeFalse();
        enable.IsApplied.Should().BeNull();
        disable.IsApplied.Should().BeNull();
        enable.CanEdit.Should().BeFalse();
        disable.CanEdit.Should().BeFalse();
        enable.CheckState.Should().BeNull();
        disable.CheckState.Should().BeNull();
    }

    [Fact]
    public void CategorySelectionChanged_RefreshesWhenActionAvailabilityChanges()
    {
        var action = CreateAction("custom.feature", recommended: true);
        using var category = new OptimizationCategoryViewModel(
            "custom",
            "Custom",
            "Custom",
            "{0} / {1}",
            [action]);
        var notifications = 0;
        category.SelectionChanged += (_, _) => notifications++;

        action.IsEnabled = false;
        action.IsVisible = false;

        notifications.Should().Be(2);
        category.VisibleActionCount.Should().Be(0);
        category.SelectionSummary.Should().Be("0 / 0");
    }

    private static OptimizationActionViewModel CreateAction(string key, bool recommended)
    {
        var definition = new WindowsOptimizationActionDefinition(
            key,
            key,
            key,
            _ => Task.CompletedTask,
            recommended);

        return new OptimizationActionViewModel(definition, key, key, "Recommended");
    }
}
