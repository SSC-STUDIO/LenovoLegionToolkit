using System.Runtime.InteropServices;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Abstractions.Platform;
using UniversalDeviceToolkit.Platform.Linux;
using Xunit;

namespace UniversalDeviceToolkit.CrossPlatform.Tests;

public sealed class LinuxDeviceAdapterTests
{
    [Fact]
    public async Task ReadSnapshot_ShouldProjectDetectedCapabilitiesAsReadOnly()
    {
        var snapshot = await new LinuxDeviceAdapter(new FakePlatformServices()).ReadSnapshotAsync();

        Assert.Equal("linux", snapshot.Identity.Platform);
        Assert.Equal(RuntimeInformation.OSArchitecture.ToString(), snapshot.Identity.Architecture);
        Assert.Equal(DeviceSupportMatcher.GenericBasicPackId, snapshot.Support.DevicePackId);
        Assert.All(snapshot.Capabilities, capability => Assert.False(capability.CanWrite));
        Assert.Contains(snapshot.Capabilities, capability => capability.Id == "gpu-management" && capability.IsAvailable);
        Assert.Contains(snapshot.Capabilities, capability => capability.Id == "fan-control" && capability.IsAvailable);
        Assert.Contains(snapshot.Capabilities, capability => capability.Id == "read-only-telemetry" && capability.IsAvailable);
        Assert.Empty(snapshot.SensorReadings);
    }

    private sealed class FakePlatformServices : IPlatformServices
    {
        public string PlatformName => "linux";
        public bool SupportsGpuManagement => true;
        public bool SupportsFanControl => true;
        public bool SupportsKeyboardBacklight => true;
        public bool SupportsBatteryManagement => true;
        public bool SupportsDisplayControl => true;
        public bool SupportsPowerProfile => true;
        public bool SupportsSystemTelemetry => true;
    }
}
