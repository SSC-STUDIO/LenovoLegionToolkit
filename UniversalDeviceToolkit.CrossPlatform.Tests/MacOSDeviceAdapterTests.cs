using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Abstractions.Platform;
using UniversalDeviceToolkit.Platform.MacOS;
using Xunit;

namespace UniversalDeviceToolkit.CrossPlatform.Tests;

public sealed class MacOSDeviceAdapterTests
{
    [Fact]
    public async Task ReadSnapshot_ShouldExposeAppleIdentityTelemetryAndReadOnlyCapabilities()
    {
        var runner = new FakeCommandRunner(new Dictionary<string, PlatformCommandResult>
        {
            ["sysctl -n hw.model"] = Success("MacBookPro18,3"),
            ["uname -m"] = Success("arm64"),
            ["system_profiler SPHardwareDataType"] = Success("""
                Hardware:

                    Hardware Overview:

                      Model Name: MacBook Pro
                      Chip: Apple M1 Pro
                      Serial Number (system): MAC-SERIAL
                """),
            ["sysctl -n hw.ncpu"] = Success("10"),
            ["sysctl -n hw.memsize"] = Success("17179869184"),
            ["pmset -g batt"] = Success("Now drawing from 'Battery Power'\\n -InternalBattery-0 (id=1)\\t86%; discharging; 3:42 remaining present: true"),
        });
        var packs = new[]
        {
            new DevicePackDefinition
            {
                Id = "apple-basic",
                DisplayName = "Apple Basic",
                Vendor = "Apple Inc.",
                ModelKeywords = ["MacBook Pro"],
            },
        };

        var snapshot = await new MacOSDeviceAdapter(runner, packs).ReadSnapshotAsync();

        Assert.Equal("macos", snapshot.Identity.Platform);
        Assert.Equal("arm64", snapshot.Identity.Architecture);
        Assert.Equal("Apple Inc.", snapshot.Identity.Vendor);
        Assert.Equal("MacBook Pro MacBookPro18,3", snapshot.Identity.Model);
        Assert.Equal("MAC-SERIAL", snapshot.Identity.SerialNumber);
        Assert.Equal("apple-basic", snapshot.Support.DevicePackId);
        Assert.Contains(snapshot.SensorReadings, reading => reading.Name == "Logical CPUs" && reading.Value == 10);
        Assert.Contains(snapshot.SensorReadings, reading => reading.Name == "Battery Charge" && reading.Value == 86);
        Assert.Contains(snapshot.Capabilities, capability => capability.Id == "read-only-telemetry" && capability.IsAvailable && !capability.CanWrite);
        Assert.Contains(snapshot.Capabilities, capability => capability.Id == "fan-control" && !capability.IsAvailable);
        Assert.Equal("Battery Power", snapshot.PowerStatus);
    }

    [Fact]
    public async Task ReadSnapshot_WhenCommandsFail_ShouldDegradeWithoutWrites()
    {
        var snapshot = await new MacOSDeviceAdapter(
            new FakeCommandRunner(new Dictionary<string, PlatformCommandResult>())).ReadSnapshotAsync();

        Assert.Empty(snapshot.Identity.Model);
        Assert.False(snapshot.Capabilities.Single(capability => capability.Id == "hardware-identity").IsAvailable);
        Assert.False(snapshot.Capabilities.Single(capability => capability.Id == "read-only-telemetry").IsAvailable);
        Assert.DoesNotContain(snapshot.Capabilities, capability => capability.CanWrite);
    }

    private static PlatformCommandResult Success(string output) => new(0, output, string.Empty);

    private sealed class FakeCommandRunner(IReadOnlyDictionary<string, PlatformCommandResult> results) : IPlatformCommandRunner
    {
        public PlatformCommandResult Run(string fileName, params string[] arguments)
        {
            var key = string.Join(' ', new[] { fileName }.Concat(arguments));
            return results.TryGetValue(key, out var result)
                ? result
                : new PlatformCommandResult(-1, string.Empty, "missing fake command");
        }
    }
}
