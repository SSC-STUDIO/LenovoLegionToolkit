using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Guard)]
public sealed class AvaloniaCompatibilityStartupContractTests
{
    [Fact]
    public void AvaloniaWindowsHost_ShouldGateHardwareServicesOnCompatibilityDecision()
    {
        var root = RepositoryPaths.FindRoot();
        var app = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));
        var coordinator = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Startup",
            "AvaloniaStartupCompatibilityCoordinator.cs"));
        var dialog = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "Windows",
            "AvaloniaUnsupportedHardwareWindow.cs"));

        app.Should().Contain("AvaloniaStartupCompatibilityCoordinator()");
        app.Should().Contain("ExitApplication(202)");
        coordinator.Should().Contain("Compatibility.IsCompatibleAsync()");
        coordinator.Should().Contain("DisableUnsupportedHardwareWarning");
        coordinator.Should().Contain("ShowDialog<bool>");
        dialog.Should().Contain("AvaloniaUnsupportedHardwareContinueButton");
        dialog.Should().Contain("AvaloniaUnsupportedHardwareExitButton");
        coordinator.Should().NotContain("UniversalDeviceToolkit.WPF");
        dialog.Should().NotContain("UniversalDeviceToolkit.WPF");
    }

    [Fact]
    public void AvaloniaWindowsHost_ShouldShowErrorWindowBeforeFailingStartupOnException()
    {
        var root = RepositoryPaths.FindRoot();
        var coordinator = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Startup",
            "AvaloniaStartupCompatibilityCoordinator.cs"));
        var errorWindow = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Windows",
            "AvaloniaCompatibilityCheckErrorWindow.cs"));

        coordinator.Should().Contain("Log.Instance.Error(\"Avalonia hardware compatibility check failed.\"");
        coordinator.Should().Contain("AvaloniaCompatibilityCheckErrorWindow");
        coordinator.Should().Contain("ShowDialog(owner)");
        coordinator.Should().Contain("return false");
        errorWindow.Should().Contain("AvaloniaCompatibilityCheckErrorWindow");
        errorWindow.Should().Contain("Log.Instance.LogPath");
        errorWindow.Should().Contain("Environment.ProcessPath");
        errorWindow.Should().Contain("Environment.Exit(201)");
        errorWindow.Should().Contain("Close");
        errorWindow.Should().NotContain("UniversalDeviceToolkit.WPF");
        coordinator.Should().NotContain("UniversalDeviceToolkit.WPF");
    }
}
