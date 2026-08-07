using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class CustomCleanupRuleContractTests
{
    [Fact]
    public void CustomCleanupRuleEditor_UsesWpfDefaultForNewRules()
    {
        var root = RepositoryPaths.FindRoot();
        var wpfSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.WPF",
            "Pages",
            "WindowsOptimizationPage.Cleanup.cs"));
        var avaloniaSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "FeaturePageView.axaml.cs"));

        wpfSource.Should().Contain("new CustomCleanupRuleViewModel(dialog.SelectedPath, [], false)");
        avaloniaSource.Should().Contain("IsChecked = current?.Recursive ?? false");
    }

    [Fact]
    public void FeaturePageState_PreservesPersistedCustomCleanupRules()
    {
        var rule = new CustomCleanupRuleItem(
            "%TEMP%\\UDT",
            [".log", ".tmp"],
            Recursive: true);

        var state = new FeaturePageState(
            "WindowsOptimization",
            "System optimization",
            "Description",
            "Available",
            "Ready",
            true,
            [],
            [rule]);

        state.CustomCleanupRules.Should().ContainSingle().Which.Should().Be(rule);
        state.CustomCleanupRules![0].Extensions.Should().Equal(".log", ".tmp");
        state.CustomCleanupRules[0].Recursive.Should().BeTrue();
    }

    [Fact]
    public void FeaturePageState_UsesEmptyRuleListWhenNotProvided()
    {
        var state = new FeaturePageState(
            "WindowsOptimization",
            "System optimization",
            "Description",
            "Available",
            "Ready",
            true,
            []);

        state.CustomCleanupRules.Should().BeNull();
    }

    [Fact]
    public void CleanupSummary_FormatsFreedBytesWithWpfWording()
    {
        var summary = AvaloniaProgressToastHelper.FormatCleanupSummary(
            4,
            TimeSpan.FromSeconds(12.4),
            5 * 1024 * 1024);

        summary.Should().Contain("5 MB");
        summary.Should().Contain("12.4");
    }

    [Fact]
    public void CleanupSummary_WithoutMeasuredSize_ReportsItemCountAndDuration()
    {
        var summary = AvaloniaProgressToastHelper.FormatCleanupSummary(
            3,
            TimeSpan.FromSeconds(7.5),
            null);

        summary.Should().Contain("3");
        summary.Should().Contain("7.5");
    }

    [Fact]
    public void CleanupSummary_FormatBytes_ParitiesWpf()
    {
        AvaloniaProgressToastHelper.FormatBytes(0).Should().Be("0 B");
        AvaloniaProgressToastHelper.FormatBytes(1024).Should().Be("1 KB");
        AvaloniaProgressToastHelper.FormatBytes(1536).Should().Be("1.5 KB");
        AvaloniaProgressToastHelper.FormatBytes(1024L * 1024 * 1024 * 3).Should().Be("3 GB");
    }

    [Fact]
    public void ProgressToastHelper_IsNoOpWhenHostHasNoProgressApi()
    {
        AvaloniaProgressToastHelper.Start("System optimization").Should().Be(Guid.Empty);
        AvaloniaProgressToastHelper.Update(Guid.Empty, 42, "Running cleanup...");
        AvaloniaProgressToastHelper.Complete(Guid.Empty);
        AvaloniaProgressToastHelper.Update(Guid.NewGuid(), 100);
        AvaloniaProgressToastHelper.Complete(Guid.NewGuid());
    }
}
