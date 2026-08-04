using System.IO;
using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

/// <summary>
/// CI gate: the layout-budget script exists, targets WPF CardHeaderControl subtitles,
/// and can be invoked by the CI pipeline.
/// </summary>
[Trait("Category", TestCategories.Unit)]
[Trait("Category", TestCategories.Guard)]
public sealed class L10nLayoutBudgetGuardTests
{
    [Fact]
    public void AssertL10nLayoutBudgetScript_ShouldExistAndReferenceWpfResources()
    {
        var script = Path.Combine(RepositoryPaths.FindRoot(), "Scripts", "Assert-L10nLayoutBudget.ps1");
        File.Exists(script).Should().BeTrue();
        var text = File.ReadAllText(script);
        text.Should().Contain("UniversalDeviceToolkit.WPF");
        text.Should().Contain("Resource.resx");
        text.Should().Contain("CardHeaderControl");
        text.Should().Contain("AdaptiveTextBlock");
    }

    [Fact]
    public void HostPages_ShouldDeclareSemanticOverflowHandling()
    {
        var root = RepositoryPaths.FindRoot();
        var wpfSettings = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Pages", "SettingsPage.xaml"));
        var navigation = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Styles", "NavigationStore.xaml"));
        var avaloniaSettings = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "Pages", "SettingsPage.axaml"));
        var avaloniaWindow = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "MainWindow.axaml"));

        wpfSettings.Should().Contain("LocalizedOverflowBehavior.IsEnabled");
        wpfSettings.Should().Contain("AutomationProperties.Name=\"{Binding Title}\"");
        navigation.Should().Contain("LocalizedOverflowBehavior.Mode=\"Ellipsis\"");
        navigation.Should().Contain("LocalizedOverflowBehavior.Mode=\"Wrap\"");
        avaloniaSettings.Should().Contain("LocalizedTextBlock");
        avaloniaSettings.Should().Contain("OverflowMode=\"Ellipsis\"");
        avaloniaWindow.Should().Contain("LocalizedTextBlock");
    }
}
