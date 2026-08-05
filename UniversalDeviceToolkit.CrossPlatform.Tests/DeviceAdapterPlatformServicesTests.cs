using System.Globalization;
using System.Resources;
using UniversalDeviceToolkit.Avalonia;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Avalonia.Services;
using Xunit;

namespace UniversalDeviceToolkit.CrossPlatform.Tests;

public sealed class DeviceAdapterPlatformServicesTests
{
    private static readonly ResourceManager Resources = new(
        "UniversalDeviceToolkit.Avalonia.Resources.Resource",
        typeof(DeviceAdapterPlatformServices).Assembly);

    [Fact]
    public async Task DashboardServices_ShouldPresentReadOnlySnapshotAndCacheIt()
    {
        var adapter = new FakeDeviceAdapter(CreateSnapshot());
        var services = new DeviceAdapterPlatformServices(adapter);

        var groups = await services.GetFeatureGroupsAsync();
        var readings = await services.GetSensorReadingsAsync();

        Assert.Contains(groups, group => group.Title == Localized("Dashboard_Feature_Device", "Device") && group.Description.Contains("Acme", StringComparison.Ordinal));
        Assert.Contains(groups, group => group.Title == Localized("Dashboard_Feature_DevicePack", "Device support") && group.Status == "Safe basic mode");
        Assert.Contains(groups, group => group.Title == "Read Only Telemetry" && group.Status == Localized("Dashboard_Status_ReadOnly", "Read-only"));
        Assert.Contains(groups, group => group.Title == "Read Only Telemetry"
            && group.RouteKey == MainNavigation.WindowsOptimization
            && group.IsNavigable);
        Assert.Contains(groups, group => group.Title == "Fan Control" && group.Status == Localized("Dashboard_Status_NotSupported", "Not supported"));
        Assert.Contains(readings, reading => reading.Name == "CPU Usage" && reading.DisplayValue == "37 %");
        Assert.Equal(1, adapter.ReadCount);
    }

    [Fact]
    public async Task DashboardServices_ShouldDegradeToSafeBasicModeWhenAdapterFails()
    {
        var services = new DeviceAdapterPlatformServices(new FakeDeviceAdapter(new InvalidOperationException("probe failed")));

        var groups = await services.GetFeatureGroupsAsync();

        Assert.Contains(groups, group => group.Title == Localized("Dashboard_Feature_Device", "Device") && group.Description == Localized("Dashboard_Status_Unknown", "Unknown"));
        Assert.Contains(groups, group => group.Title == "Hardware Identity" && group.Status == Localized("Dashboard_Status_NotSupported", "Not supported"));
        Assert.False(await services.IsSupportedLegionMachineAsync());
    }

    private static DeviceSnapshot CreateSnapshot() => new(
        new DeviceIdentity("windows", "X64", "Acme", "Acme Pro", "Acme Pro", "1.0", "serial", "test"),
        new DeviceSupportInfo("Safe basic mode", "generic-pc-basic", "Universal PC Basic", ["diagnostics"], ["fan-curve"], "safe"),
        [
            new DeviceCapability("read-only-telemetry", true, false, "test", "Read-only telemetry is available."),
            DeviceCapability.Unavailable("fan-control", "No verified fan backend.", "test"),
        ],
        [new SensorReading("CPU Usage", "CPU", 37, "%")],
        null,
        "test");

    private static string Localized(string key, string fallback) =>
        Resources.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;

    private sealed class FakeDeviceAdapter : IDeviceAdapter
    {
        private readonly DeviceSnapshot? _snapshot;
        private readonly Exception? _exception;

        public FakeDeviceAdapter(DeviceSnapshot snapshot) => _snapshot = snapshot;

        public FakeDeviceAdapter(Exception exception) => _exception = exception;

        public int ReadCount { get; private set; }

        public Task<DeviceSnapshot> ReadSnapshotAsync(CancellationToken cancellationToken = default)
        {
            ReadCount++;
            if (_exception is not null)
                throw _exception;

            return Task.FromResult(_snapshot!);
        }

        public string PlatformId => "test";
    }
}
