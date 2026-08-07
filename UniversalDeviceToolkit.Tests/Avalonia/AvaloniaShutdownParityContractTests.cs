using FluentAssertions;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

[Trait("Category", TestCategories.Guard)]
public sealed class AvaloniaShutdownParityContractTests
{
    [Fact]
    public void AvaloniaWindowsHost_ShouldStopTheWindowsServicesItStarts()
    {
        var root = RepositoryPaths.FindRoot();
        var app = File.ReadAllText(Path.Combine(root, "UniversalDeviceToolkit.Avalonia", "App.axaml.cs"));
        var coordinator = File.ReadAllText(Path.Combine(
            root,
            "UniversalDeviceToolkit.Avalonia",
            "Startup",
            "AvaloniaWindowsShutdownCoordinator.cs"));

        app.Should().Contain("AvaloniaWindowsShutdownCoordinator().StopAsync()");
        app.Should().Contain("Interlocked.CompareExchange(ref _shutdownStarted, 1, 0)");
        coordinator.Should().Contain("StopNetworkAccelerationAsync");
        coordinator.Should().Contain("IpcServer");
        coordinator.Should().Contain("MacroController");
        coordinator.Should().Contain("SetLightControlOwnerAsync(false)");
        coordinator.Should().Contain("HWiNFOIntegration");
        coordinator.Should().Contain("BatteryDischargeRateMonitorService");
        coordinator.Should().Contain("NativeWindowsMessageListener");
        coordinator.Should().Contain("plugin.OnShutdown()");
        coordinator.Should().NotContain("UniversalDeviceToolkit.WPF");
    }
}
