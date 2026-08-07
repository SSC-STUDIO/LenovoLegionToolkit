using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Guard)]
public sealed class AvaloniaOsdOverlayContractTests
{
    [Fact]
    public void AvaloniaWindowsHost_ShouldRenderAndDisposeTheSharedOsdContract()
    {
        var root = RepositoryPaths.FindRoot();
        var app = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));
        var controller = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Windows",
            "AvaloniaOsdOverlayController.cs"));
        var overlay = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "Windows",
            "AvaloniaOsdOverlayWindow.cs"));

        app.Should().Contain("AvaloniaOsdOverlayController(PlatformServices)");
        app.Should().Contain("_osdOverlay?.Dispose()");
        controller.Should().Contain("OsdChangedMessage");
        controller.Should().Contain("OsdAppearanceChangedMessage");
        controller.Should().Contain("OsdElementChangedMessage");
        controller.Should().Contain("OsdState.Toggle");
        overlay.Should().Contain("GetSensorReadingsAsync");
        overlay.Should().Contain("AvaloniaOsdStyle.Bar");
        overlay.Should().Contain("TempThresholdCritical");
        overlay.Should().Contain("UsageThresholdCritical");
        overlay.Should().Contain("nameof(Position)");
        controller.Should().NotContain("UniversalDeviceToolkit.WPF");
        overlay.Should().NotContain("UniversalDeviceToolkit.WPF");
    }
}
