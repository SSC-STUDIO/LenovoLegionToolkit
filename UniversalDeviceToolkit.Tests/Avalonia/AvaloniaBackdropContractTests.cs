using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Unit)]
public sealed class AvaloniaBackdropContractTests
{
    [Fact]
    public void MainWindow_ShouldApplyBackdropWithPlatformFallbacks()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "MainWindow.axaml.cs"));

        source.Should().Contain("public void ApplyWindowBackdrop()");
        source.Should().Contain("WindowTransparencyLevel.Mica");
        source.Should().Contain("WindowTransparencyLevel.AcrylicBlur");
        source.Should().Contain("WindowTransparencyLevel.Blur");
        source.Should().Contain("WindowTransparencyLevel.None");
        source.Should().Contain("TransparencyBackgroundFallback");
        source.Should().Contain("ApplyWindowBackdrop();");
    }

    [Fact]
    public void DisplayBackdropSelection_ShouldRefreshTheCurrentWindow()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "SettingsCapabilityView.axaml.cs"));

        source.Should().Contain("_pageKey == \"Display\"");
        source.Should().Contain("option.Key == \"WindowBackdrop\"");
        source.Should().Contain("mainWindow.ApplyWindowBackdrop();");
    }

    [Fact]
    public void ThemeChanges_ShouldReApplyTheWindowBackdrop()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "MainWindow.axaml.cs"));

        source.Should().Contain("AvaloniaThemeManager.Instance.ThemeApplied += OnThemeApplied;");
        source.Should().Contain("OnThemeApplied");
        source.Should().Contain("ApplyWindowBackdrop()");
        source.Should().Contain("AvaloniaThemeManager.Instance.Reapply();");
    }

    [Fact]
    public void UpdateCoordinator_ShouldSurfaceUpdateIndicatorWithoutHardDependency()
    {
        var root = RepositoryPaths.FindRoot();
        var appSource = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));
        var mainWindowSource = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "MainWindow.axaml.cs"));
        var markup = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "MainWindow.axaml"));

        appSource.Should().Contain("SubscribeToUpdateCoordinator");
        appSource.Should().Contain("ShowUpdateDialogAsync");
        mainWindowSource.Should().Contain("SetUpdateAvailable");
        mainWindowSource.Should().Contain("UpdateAvailableButton_Click");
        markup.Should().Contain("x:Name=\"UpdateAvailableButton\"");
        markup.Should().Contain("AvaloniaUpdateAvailableButton");
    }

    [Fact]
    public void TrayRestore_ShouldUseTheSameDeferredSurfaceRefreshPathAsNativeRestore()
    {
        var root = RepositoryPaths.FindRoot();
        var mainWindowSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "MainWindow.axaml.cs"));
        var appSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "App.axaml.cs"));

        mainWindowSource.Should().Contain("internal void RestoreFromTray()");
        mainWindowSource.Should().Contain("Dispatcher.UIThread.Post");
        mainWindowSource.Should().Contain("DispatcherPriority.Render");
        mainWindowSource.Should().Contain("MainContent.InvalidateMeasure();");
        mainWindowSource.Should().Contain("MainContent.InvalidateArrange();");
        mainWindowSource.Should().Contain("QueueWindowSurfaceRefresh();");
        appSource.Should().Contain("mainWindow.RestoreFromTray();");
    }

    [Fact]
    public void TrayTransitions_ShouldRemoveAndRestoreTheTaskbarButton()
    {
        var root = RepositoryPaths.FindRoot();
        var mainWindowSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "MainWindow.axaml.cs"));
        var appSource = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "App.axaml.cs"));

        var minimizeSection = mainWindowSource[mainWindowSource.IndexOf(
            "if (WindowState == WindowState.Minimized", StringComparison.Ordinal)..];
        minimizeSection.IndexOf("ShowInTaskbar = false;", StringComparison.Ordinal)
            .Should().BeLessThan(minimizeSection.IndexOf("Hide();", StringComparison.Ordinal));

        var restoreSection = mainWindowSource[mainWindowSource.IndexOf(
            "internal void RestoreFromTray()", StringComparison.Ordinal)..];
        restoreSection.IndexOf("ShowInTaskbar = true;", StringComparison.Ordinal)
            .Should().BeLessThan(restoreSection.IndexOf("Show();", StringComparison.Ordinal));

        var closingSection = appSource[appSource.IndexOf(
            "if (settings?.MinimizeToTray == true)", StringComparison.Ordinal)..];
        closingSection.IndexOf("window.ShowInTaskbar = false;", StringComparison.Ordinal)
            .Should().BeLessThan(closingSection.IndexOf("window.Hide();", StringComparison.Ordinal));
    }
}
