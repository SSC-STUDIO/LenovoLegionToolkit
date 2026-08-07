using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Guard)]
public sealed class AvaloniaStartupDeviceSetupContractTests
{
    [Fact]
    public void AvaloniaWindowsHost_ShouldRunFirstRunDevicePackSelectionBeforeCompatibilityGate()
    {
        var root = RepositoryPaths.FindRoot();
        var app = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));
        var coordinator = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Startup",
            "AvaloniaStartupDeviceSetupCoordinator.cs"));
        var window = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Pages",
            "Windows",
            "AvaloniaDeviceSetupWindow.cs"));

        app.Should().Contain("AvaloniaStartupDeviceSetupCoordinator()");
        app.IndexOf("AvaloniaStartupDeviceSetupCoordinator()", StringComparison.Ordinal)
            .Should().BeLessThan(app.IndexOf("AvaloniaStartupCompatibilityCoordinator()", StringComparison.Ordinal));
        coordinator.Should().Contain("device-setup");
        coordinator.Should().Contain("DevicePackManager");
        coordinator.Should().Contain("SetInstalledCatalog");
        coordinator.Should().Contain("SetPreferredDevicePackId");
        coordinator.Should().Contain("InstallAsync(result.DevicePackId)");
        coordinator.Should().Contain("owner.IsEnabled = false");
        window.Should().Contain("AvaloniaDeviceSetupPackSelector");
        window.Should().Contain("AvaloniaDeviceSetupConfirmButton");
        window.Should().Contain("AvaloniaDeviceSetupSkipButton");
        coordinator.Should().NotContain("UniversalDeviceToolkit.WPF");
        window.Should().NotContain("UniversalDeviceToolkit.WPF");
    }
}
