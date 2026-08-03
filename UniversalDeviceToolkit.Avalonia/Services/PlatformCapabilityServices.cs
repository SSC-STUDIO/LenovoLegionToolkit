using System.Collections.Generic;
using System.Threading.Tasks;
using PlatformServices = UniversalDeviceToolkit.Abstractions.Platform.IPlatformServices;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Adapts the shared platform capability contract to the Avalonia dashboard
/// contract. The platform projects own detection; Avalonia only presents it.
/// </summary>
public sealed class PlatformCapabilityServices(PlatformServices capabilities) : IPlatformServices
{
    public Task<IReadOnlyList<FeatureGroupItem>> GetFeatureGroupsAsync()
    {
        var platformName = capabilities.PlatformName;
        IReadOnlyList<FeatureGroupItem> groups =
        [
            CreateFeature("GPU", capabilities.SupportsGpuManagement),
            CreateFeature("Fan Control", capabilities.SupportsFanControl),
            CreateFeature("Keyboard", capabilities.SupportsKeyboardBacklight),
            CreateFeature("Battery", capabilities.SupportsBatteryManagement),
            CreateFeature("Display", capabilities.SupportsDisplayControl),
            CreateFeature("Power Profile", capabilities.SupportsPowerProfile),
            new("System Telemetry", platformName, Status(capabilities.SupportsSystemTelemetry)),
        ];

        return Task.FromResult(groups);
    }

    public Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync()
    {
        var status = Status(capabilities.SupportsSystemTelemetry);
        IReadOnlyList<SensorReadingItem> readings =
        [
            new("CPU Temperature", status),
            new("GPU Temperature", capabilities.SupportsGpuManagement ? status : "Not supported"),
            new("CPU Usage", status),
            new("Memory Usage", status),
            new("Fan Speed", capabilities.SupportsFanControl ? status : "Not supported"),
            new("Battery", capabilities.SupportsBatteryManagement ? status : "Not supported"),
        ];

        return Task.FromResult(readings);
    }

    public Task<bool> IsSupportedLegionMachineAsync() => Task.FromResult(false);

    private static FeatureGroupItem CreateFeature(string title, bool supported) =>
        new(title, "Platform capability", Status(supported));

    private static string Status(bool supported) => supported ? "Available" : "Not supported";
}
