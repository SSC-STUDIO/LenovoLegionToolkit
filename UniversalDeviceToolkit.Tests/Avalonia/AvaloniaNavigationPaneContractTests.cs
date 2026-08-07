using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Unit)]
public sealed class AvaloniaNavigationPaneContractTests
{
    [Fact]
    public void MainWindowMarkup_ShouldExposeAStableNavigationPaneToggle()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "MainWindow.axaml"));

        markup.Should().Contain("x:Name=\"NavigationPane\"");
        markup.Should().Contain("AvaloniaNavigationPane");
        markup.Should().Contain("x:Name=\"NavigationToggleButton\"");
        markup.Should().Contain("AvaloniaNavigationPaneToggle");
        markup.Should().Contain("IconIdentifier=\"ArrowLeft24\"");
    }

    [Fact]
    public void MainWindowCode_ShouldApplyPersistedExpandedStateAndCollapsedRail()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "MainWindow.axaml.cs"));

        source.Should().Contain("NavigationPaneExpanded");
        source.Should().Contain("NavigationPane.Width = expanded ? 280 : 72");
        source.Should().Contain("NavigationHeader.IsVisible = expanded");
        source.Should().Contain("NavigationToggleIcon.IconIdentifier = expanded ? \"ArrowLeft24\" : \"ArrowRight24\"");
        source.Should().Contain("ApplyNavigationPaneState();");
    }

    [Fact]
    public void DisplayNavigationSetting_ShouldRefreshTheShellImmediately()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsCapabilityView.axaml.cs"));

        source.Should().Contain("option.Key == \"NavigationPaneExpanded\"");
        source.Should().Contain("mainWindow.ApplyNavigationPaneState();");
    }

    [Fact]
    public void MainWindowMarkup_ShouldKeepContentSurfaceInsideTheScaleHost()
    {
        var root = RepositoryPaths.FindRoot();
        var markup = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "MainWindow.axaml"));

        markup.Should().Contain("LayoutTransformControl x:Name=\"ContentScaleTransform\"");
        markup.Should().Contain("AvaloniaContentScaleHost");
        markup.Should().Contain("ContentControl x:Name=\"MainContent\"");
        markup.Should().Contain("AvaloniaMainContent");
        markup.IndexOf("ContentScaleTransform", StringComparison.Ordinal)
            .Should().BeLessThan(markup.IndexOf("MainContent", StringComparison.Ordinal));
    }

    [Fact]
    public void TrayMenu_ShouldExposeNavigationRoutesAndPipelinesEntry()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));

        source.Should().Contain("CreateNavigationMenuItem(\"Nav_Dashboard\", \"Dashboard\", MainNavigation.Dashboard)");
        source.Should().Contain("MainNavigation.Keyboard");
        source.Should().Contain("MainNavigation.Actions");
        source.Should().Contain("MainNavigation.Macro");
        source.Should().Contain("MainNavigation.WindowsOptimization");
        source.Should().Contain("MainNavigation.PluginExtensions");
        source.Should().Contain("MainNavigation.About");
        source.Should().Contain("Tray_Pipelines");
        source.Should().Contain("mainWindow.Navigate(route)");
    }
}
