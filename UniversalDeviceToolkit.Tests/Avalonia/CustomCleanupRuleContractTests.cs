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
}
