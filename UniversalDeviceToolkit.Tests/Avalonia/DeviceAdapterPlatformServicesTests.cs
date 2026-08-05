using System.Threading;
using FluentAssertions;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Avalonia;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.Tests.Avalonia;

public sealed class DeviceAdapterPlatformServicesTests
{
    [Fact]
    public async Task ReadOnlyIdentityAndTelemetryCapabilitiesRemainInformational()
    {
        var services = new DeviceAdapterPlatformServices(new SnapshotAdapter(
            new DeviceCapability[]
            {
                new("hardware-identity", true, false, "test", "Identity is available."),
                new("read-only-telemetry", true, false, "test", "Telemetry is available."),
                new("keyboard-backlight", true, false, "test", "Keyboard data is available."),
            }));

        var groups = await services.GetFeatureGroupsAsync();

        groups.Single(group => group.Description == "Identity is available.").RouteKey.Should().BeNull();
        groups.Single(group => group.Description == "Telemetry is available.").RouteKey.Should().BeNull();
        groups.Single(group => group.Description == "Keyboard data is available.").RouteKey.Should().Be(MainNavigation.Keyboard);
    }

    [Fact]
    public async Task PortableControlPagesStayUnavailableWithoutMatchingCapabilities()
    {
        var services = new DeviceAdapterPlatformServices(new SnapshotAdapter(
            new DeviceCapability[]
            {
                new("hardware-identity", true, false, "test", "Identity is available."),
                new("read-only-telemetry", true, false, "test", "Telemetry is available."),
            }));

        var actions = await services.GetFeaturePageStateAsync("Actions");
        var optimization = await services.GetFeaturePageStateAsync("WindowsOptimization");

        actions.IsAvailable.Should().BeFalse();
        optimization.IsAvailable.Should().BeFalse();
    }

    private sealed class SnapshotAdapter(IReadOnlyList<DeviceCapability> capabilities) : IDeviceAdapter
    {
        public string PlatformId => "test";

        public Task<DeviceSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeviceSnapshot(
                new DeviceIdentity("test", "x64", "Test", "Model", "Product", "", "", "test"),
                new DeviceSupportInfo("Safe basic mode", "test", "Test adapter", ["diagnostics"], [], "Test snapshot."),
                capabilities,
                [],
                null,
                "test"));
    }
}
