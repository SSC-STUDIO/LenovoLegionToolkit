using FluentAssertions;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.WPF.Utils;
using Xunit;

namespace UniversalDeviceToolkit.Tests.WPF;

[Trait("Category", TestCategories.Unit)]
public sealed class BackdropSurfaceTests
{
    [Theory]
    [InlineData(WindowBackdropStyle.Windows, 0.18, 1.0, 1.0)]
    [InlineData(WindowBackdropStyle.macOS, 0.08, 1.0, 1.0)]
    public void ActiveBackdrop_ShouldApplyMaterialOnlyToShellChrome(
        WindowBackdropStyle style,
        double expectedShell,
        double expectedContent,
        double expectedCard)
    {
        var opacities = RenderingCompatibilityHelper.GetBackdropSurfaceOpacities(style, isBackdropActive: true);

        opacities.Shell.Should().Be(expectedShell);
        opacities.Content.Should().Be(expectedContent);
        opacities.Card.Should().Be(expectedCard);
    }

    [Fact]
    public void InactiveBackdrop_ShouldKeepAllSurfacesOpaque()
    {
        var opacities = RenderingCompatibilityHelper.GetBackdropSurfaceOpacities(
            WindowBackdropStyle.Windows,
            isBackdropActive: false);

        opacities.Should().Be(new RenderingCompatibilityHelper.BackdropSurfaceOpacities(1.0, 1.0, 1.0));
    }

    [Fact]
    public void MainWindow_AndNavigation_ShouldNotStackBackdropFills()
    {
        var root = RepositoryPaths.FindRoot();
        var mainWindow = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Windows", "MainWindow.xaml"));
        var navigation = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Styles", "NavigationStore.xaml"));

        ExtractElement(mainWindow, "x:Name=\"_mainTitleBar\"").Should().Contain("Background=\"Transparent\"");
        ExtractElement(mainWindow, "x:Name=\"_contentGrid\"").Should().Contain("Background=\"Transparent\"");
        navigation.Should().Contain("<Setter Property=\"Background\" Value=\"Transparent\" />");
    }

    [Fact]
    public void StateTransitions_ShouldNotFreezeTheEntireMainWindowClient()
    {
        var root = RepositoryPaths.FindRoot();
        var mainWindow = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Windows", "MainWindow.xaml.cs"));
        var resizeHelper = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Utils", "WindowResizeStabilityHelper.cs"));

        mainWindow.Should().Contain("QueueContentRefreshAfterStateTransition");
        mainWindow.Should().Contain("WindowResizeStabilityHelper.RestoreIfNeeded(this)");
        mainWindow.Should().NotContain("BeginStateTransitionSmooth");
        mainWindow.Should().NotContain("new BitmapCache");
        resizeHelper.Should().NotContain("WmSizing");
    }

    [Fact]
    public void LiveResize_ShouldKeepTheWindowBackdropConnected()
    {
        var root = RepositoryPaths.FindRoot();
        var resizeHelper = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Utils", "WindowResizeStabilityHelper.cs"));

        resizeHelper.Should().NotContain("WindowBackdropType = WindowBackdropType.None");
        resizeHelper.Should().NotContain("SetResourceReference(Window.BackgroundProperty");
        resizeHelper.Should().NotContain("new BitmapCache");
    }

    [Fact]
    public void RestoreFromMinimize_ShouldRestoreNormalStateBeforeShowingWindow()
    {
        var root = RepositoryPaths.FindRoot();
        var mainWindow = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Windows", "MainWindow.xaml.cs"));
        var windowExtensions = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Extensions", "WindowExtensions.cs"));

        mainWindow.Should().NotContain("QueueContentRecoveryAfterMinimize");
        mainWindow.Should().NotContain("_contentGrid.Visibility = Visibility.Collapsed");

        var restoreState = windowExtensions.IndexOf("window.WindowState = WindowState.Normal", StringComparison.Ordinal);
        var showWindow = windowExtensions.IndexOf("window.Show();", StringComparison.Ordinal);
        restoreState.Should().BeGreaterThanOrEqualTo(0);
        showWindow.Should().BeGreaterThan(restoreState);
    }

    [Fact]
    public void MinimizeToTray_ShouldRemoveTheTaskbarButtonBeforeHiding()
    {
        var root = RepositoryPaths.FindRoot();
        var mainWindow = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Windows", "MainWindow.xaml.cs"));

        var windowExtensions = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.WPF", "Extensions", "WindowExtensions.cs"));
        var sendToTray = mainWindow[mainWindow.IndexOf("public void SendToTray()", StringComparison.Ordinal)..];
        var hideWindow = sendToTray.IndexOf("Hide();", StringComparison.Ordinal);
        var removeTaskbarButton = sendToTray.IndexOf("SetTaskbarVisibility(false)", StringComparison.Ordinal);

        removeTaskbarButton.Should().BeGreaterThanOrEqualTo(0);
        hideWindow.Should().BeGreaterThan(removeTaskbarButton);
        mainWindow.Should().Contain("case MainWindowVisibilityAction.Show:");
        mainWindow.Should().Contain("SetTaskbarVisibility(true)");
        windowExtensions.Should().Contain("WS_EX_APPWINDOW");
        windowExtensions.Should().Contain("WS_EX_TOOLWINDOW");
        windowExtensions.Should().Contain("SWP_FRAMECHANGED");
    }

    private static string ExtractElement(string xaml, string marker)
    {
        var start = xaml.IndexOf(marker, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);

        return xaml[start..Math.Min(xaml.Length, start + 320)];
    }
}
