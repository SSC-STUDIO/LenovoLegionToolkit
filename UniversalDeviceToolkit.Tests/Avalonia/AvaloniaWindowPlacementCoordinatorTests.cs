using Avalonia;
using FluentAssertions;
using UniversalDeviceToolkit.Avalonia.Services;
using UniversalDeviceToolkit.Lib;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Unit)]
public sealed class AvaloniaWindowPlacementCoordinatorTests
{
    private static readonly AvaloniaWindowPlacementCoordinator.ScreenWorkArea[] Screens =
    [
        new(new Rect(0, 0, 1920, 1040), true),
        new(new Rect(1920, 0, 1920, 1040), false),
    ];

    [Fact]
    public void Restore_ValidPlacement_PreservesBoundsAndMaximizedState()
    {
        var result = AvaloniaWindowPlacementCoordinator.Restore(
            new WindowPlacement(160, 120, 1280, 760, true),
            null,
            new Size(900, 600),
            Screens);

        result.Should().Be(new AvaloniaWindowPlacementCoordinator.RestoreResult(
            new Rect(160, 120, 1280, 760), true));
    }

    [Fact]
    public void Restore_OffDisplayPlacement_CentersOnPrimaryScreen()
    {
        var result = AvaloniaWindowPlacementCoordinator.Restore(
            new WindowPlacement(8000, 200, 1200, 700, false),
            null,
            new Size(900, 600),
            Screens);

        result.Should().Be(new AvaloniaWindowPlacementCoordinator.RestoreResult(
            new Rect(360, 170, 1200, 700), false));
    }

    [Fact]
    public void Restore_ConnectedPlacement_ClampsToVirtualDesktopAndMinimumSize()
    {
        var result = AvaloniaWindowPlacementCoordinator.Restore(
            new WindowPlacement(3100, 700, 100, 100, false),
            null,
            new Size(900, 600),
            Screens);

        result.Should().Be(new AvaloniaWindowPlacementCoordinator.RestoreResult(
            new Rect(2940, 440, 900, 600), false));
    }

    [Fact]
    public void Restore_LegacySize_CentersOnPrimaryWithoutMaximizing()
    {
        var result = AvaloniaWindowPlacementCoordinator.Restore(
            null,
            new WindowSize(1200, 700),
            new Size(900, 600),
            Screens);

        result.Should().Be(new AvaloniaWindowPlacementCoordinator.RestoreResult(
            new Rect(360, 170, 1200, 700), false));
    }

    [Fact]
    public void Restore_InvalidPlacement_DoesNotApplyCorruptValues()
    {
        var result = AvaloniaWindowPlacementCoordinator.Restore(
            new WindowPlacement(double.NaN, 0, 1200, 700, false),
            null,
            new Size(900, 600),
            Screens);

        result.Should().BeNull();
    }

    [Fact]
    public void MainWindow_UsesPlacementCoordinatorForOpenedAndClosingLifecycle()
    {
        var root = RepositoryPaths.FindRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "MainWindow.axaml.cs"));

        source.Should().Contain("Opened += OnOpened;");
        source.Should().Contain("Closing += OnClosing;");
        source.Should().Contain("RestoreWindowPlacement();");
        source.Should().Contain("SaveWindowPlacement();");
        source.Should().Contain("AvaloniaWindowPlacementCoordinator.Restore(");
        source.Should().Contain("AvaloniaWindowPlacementCoordinator.Capture(");
    }
}
