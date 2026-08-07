using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class WindowsOptimizationPendingStateContractTests
{
    [Fact]
    public void OptimizationCommands_KeepSelectionAndExecutionAsSeparateActions()
    {
        FeatureActionContract.OptimizationApplyRecommendedActionKey.Should().Be("optimization-apply-recommended");
        FeatureActionContract.OptimizationApplySelectedActionKey.Should().Be("optimization-apply-selected");
        FeatureActionContract.OptimizationClearSelectionActionKey.Should().Be("optimization-clear-selection");

        var root = RepositoryPaths.FindRoot();
        var page = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "FeaturePageView.axaml.cs"));

        page.Should().NotContain("RevertAppliedOptimizationActionsAsync");
        page.Should().Contain("item.IsSelected != item.IsApplied");
        page.Should().Contain("ApplySelectedButton.IsEnabled = hasPendingOptimizationChanges");
    }

    [Fact]
    public void OptimizationHost_RecordsPendingIntentThenAppliesItAsABatch()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "WindowsFeatureHostServices.cs"));

        source.Should().Contain("_pendingOptimizationActions[action.Key] = isSelected;");
        source.Should().Contain("SelectRecommendedOptimizationActionsAsync");
        source.Should().Contain("ClearOptimizationSelectionAsync");
        source.Should().Contain("ApplyPendingOptimizationActionsAsync");
        source.Should().Contain("_pendingOptimizationActions.Clear();");
        source.Should().NotContain("ExecuteRecommendedOptimizationAsync");
    }

    [Fact]
    public void CleanupResults_RepresentSuccessPartialFailureAndFailure()
    {
        var succeeded = new CleanupExecutionResult(2, 2, 0, 1024, TimeSpan.FromSeconds(1), []);
        var partial = new CleanupExecutionResult(2, 1, 1, 512, TimeSpan.FromSeconds(1), []);
        var failed = new CleanupExecutionResult(2, 0, 2, 0, TimeSpan.FromSeconds(1), []);

        succeeded.Succeeded.Should().BeTrue();
        succeeded.HasPartialFailure.Should().BeFalse();
        partial.Succeeded.Should().BeFalse();
        partial.HasPartialFailure.Should().BeTrue();
        failed.Succeeded.Should().BeFalse();
        failed.HasPartialFailure.Should().BeFalse();
    }

    [Fact]
    public void CleanupHost_ExecutesEachSelectedActionAndReportsItsOutcome()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "WindowsFeatureHostServices.cs"));
        var page = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "FeaturePageView.axaml.cs"));

        source.Should().Contain("EstimateActionSizeAsync(actionKey");
        source.Should().Contain("ExecuteCleanupAsync([actionKey]");
        source.Should().Contain("new CleanupActionResult(actionKey, title, false");
        source.Should().Contain("progress?.Report(new CleanupProgressState");
        page.Should().Contain("RunSelectedCleanupAsync(progress)");
        page.Should().Contain("WindowsOptimizationPage_CleanupPartialSummary");
    }

    [Fact]
    public void FeatureActions_ExposeActualStateForDirtySelectionComparison()
    {
        var action = new FeatureActionItem(
            "test",
            "Test",
            "Description",
            "Pending apply",
            true,
            true,
            true,
            IsApplied: false);

        action.IsSelected.Should().BeTrue();
        action.IsApplied.Should().BeFalse();
    }
}
