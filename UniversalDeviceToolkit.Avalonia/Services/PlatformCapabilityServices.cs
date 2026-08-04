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
            CreateFeature("Dashboard_Feature_GPU", "GPU", capabilities.SupportsGpuManagement),
            CreateFeature("Dashboard_Feature_FanControl", "Fan Control", capabilities.SupportsFanControl),
            CreateFeature("Dashboard_Feature_Keyboard", "Keyboard", capabilities.SupportsKeyboardBacklight),
            CreateFeature("Dashboard_Feature_Battery", "Battery", capabilities.SupportsBatteryManagement),
            CreateFeature("Dashboard_Feature_Display", "Display", capabilities.SupportsDisplayControl),
            CreateFeature("Dashboard_Feature_PowerProfile", "Power Profile", capabilities.SupportsPowerProfile),
            new(DashboardLocalization.Get("Dashboard_Feature_SystemTelemetry", "System Telemetry"), platformName, Status(capabilities.SupportsSystemTelemetry)),
        ];

        return Task.FromResult(groups);
    }

    public Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync()
    {
        var status = Status(capabilities.SupportsSystemTelemetry);
        IReadOnlyList<SensorReadingItem> readings =
        [
            new(DashboardLocalization.Get("Dashboard_Sensor_CpuTemperature", "CPU Temperature"), status),
            new(DashboardLocalization.Get("Dashboard_Sensor_GpuTemperature", "GPU Temperature"), capabilities.SupportsGpuManagement ? status : NotSupported()),
            new(DashboardLocalization.Get("Dashboard_Sensor_CpuUsage", "CPU Usage"), status),
            new(DashboardLocalization.Get("Dashboard_Sensor_MemoryUsage", "Memory Usage"), status),
            new(DashboardLocalization.Get("Dashboard_Sensor_FanSpeed", "Fan Speed"), capabilities.SupportsFanControl ? status : NotSupported()),
            new(DashboardLocalization.Get("Dashboard_Sensor_Battery", "Battery"), capabilities.SupportsBatteryManagement ? status : NotSupported()),
        ];

        return Task.FromResult(readings);
    }

    public Task<bool> IsSupportedLegionMachineAsync() => Task.FromResult(false);

    private static FeatureGroupItem CreateFeature(string key, string fallback, bool supported) =>
        new(DashboardLocalization.Get(key, fallback), DashboardLocalization.Get("Dashboard_Description_PlatformCapability", "Platform capability"), Status(supported));

    private static string Status(bool supported) => supported
        ? DashboardLocalization.Get("Dashboard_Status_Available", "Available")
        : NotSupported();

    private static string NotSupported() =>
        DashboardLocalization.Get("Dashboard_Status_NotSupported", "Not supported");
}
