using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Guard)]
public sealed class AvaloniaStartupParityContractTests
{
    [Fact]
    public void AvaloniaWindowsHost_ShouldRunTheWpfStartupServicesWithoutWpfUiDependencies()
    {
        var root = RepositoryPaths.FindRoot();
        var app = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));
        var coordinator = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Startup",
            "AvaloniaWindowsStartupCoordinator.cs"));
        var featureHost = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Services",
            "WindowsFeatureHostServices.cs"));

        app.Should().Contain("new AvaloniaWindowsStartupCoordinator().RunAsync()");
        app.Should().Contain("StartAutomationForHostAsync()");
        coordinator.Should().Contain("EnsureCleanSystemStateOnStartupAsync");
        coordinator.Should().Contain("StartupInitializationRunner");
        coordinator.Should().Contain("StartupHealthGuard.MarkHardwareInitInProgress()");
        coordinator.Should().Contain("StartupHealthGuard.ClearHardwareInitInProgress()");
        coordinator.Should().Contain("IsHardwareInitInProgressMarkerPresent()");
        coordinator.Should().Contain("--safe-start");
        coordinator.Should().Contain("--reset-hardware-state");
        coordinator.Should().Contain("--reset-network-state");
        coordinator.Should().Contain("EnsureGodModeStateIsAppliedAsync");
        coordinator.Should().Contain("EnsureCorrectBatteryModeIsSetAsync");
        coordinator.Should().Contain("StartAuroraIfNeededAsync");
        coordinator.Should().Contain("EnsureOverclockIsAppliedAsync");
        coordinator.Should().Contain("EnsureDGPUEjectedIfNeededAsync");
        coordinator.Should().Contain("LoadAndApply(settings.Store.Entries)");
        coordinator.Should().Contain("StartIfNeededAsync");
        coordinator.Should().Contain("StartStopIfNeededAsync");
        coordinator.Should().Contain("IpcServer");
        coordinator.Should().Contain("MacroController");
        featureHost.Should().Contain("_automation.RunOnStartup()");
        featureHost.Should().Contain("_automationStartupInvoked");
        coordinator.Should().NotContain("UniversalDeviceToolkit.WPF");
    }
}
