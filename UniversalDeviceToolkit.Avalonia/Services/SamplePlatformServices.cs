using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Portable fallback provider. No UniversalDeviceToolkit.Lib dependency, so this
/// compiles for every TFM. Keeps linux-x64 / osx-arm64 builds runnable.
/// </summary>
public sealed class SamplePlatformServices : IPlatformServices
{
    public Task<IReadOnlyList<FeatureGroupItem>> GetFeatureGroupsAsync() =>
        Task.FromResult<IReadOnlyList<FeatureGroupItem>>(
        [
            new(DashboardLocalization.Get("Dashboard_Feature_PowerMode", "Power Mode"), DashboardLocalization.Get("Dashboard_Description_PowerManagement", "System power management"), DashboardLocalization.Get("Dashboard_Status_Sample", "Sample")),
            new(DashboardLocalization.Get("Dashboard_Feature_FanControl", "Fan Control"), DashboardLocalization.Get("Dashboard_Description_FanManagement", "Fan speed management"), DashboardLocalization.Get("Dashboard_Status_Sample", "Sample")),
            new(DashboardLocalization.Get("Dashboard_Feature_Display", "Display"), DashboardLocalization.Get("Dashboard_Description_RefreshRate", "Refresh rate control"), DashboardLocalization.Get("Dashboard_Status_Sample", "Sample")),
            new(DashboardLocalization.Get("Dashboard_Feature_GPU", "GPU"), DashboardLocalization.Get("Dashboard_Description_GpuManagement", "GPU management"), DashboardLocalization.Get("Dashboard_Status_Sample", "Sample")),
            new(DashboardLocalization.Get("Dashboard_Feature_Battery", "Battery"), DashboardLocalization.Get("Dashboard_Description_BatteryManagement", "Battery management"), DashboardLocalization.Get("Dashboard_Status_Sample", "Sample")),
            new(DashboardLocalization.Get("Dashboard_Feature_Keyboard", "Keyboard"), DashboardLocalization.Get("Dashboard_Description_BacklightControl", "Backlight control"), DashboardLocalization.Get("Dashboard_Status_Sample", "Sample")),
        ]);

    public Task<bool> IsSupportedLegionMachineAsync() => Task.FromResult(false);

    public Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync() =>
        Task.FromResult<IReadOnlyList<SensorReadingItem>>(
        [
            new SensorReadingItem(DashboardLocalization.Get("Dashboard_Sensor_CpuTemperature", "CPU Temperature"), DashboardLocalization.Get("Dashboard_Status_Sample", "Sample")),
            new SensorReadingItem(DashboardLocalization.Get("Dashboard_Sensor_GpuTemperature", "GPU Temperature"), DashboardLocalization.Get("Dashboard_Status_Sample", "Sample")),
            new SensorReadingItem(DashboardLocalization.Get("Dashboard_Sensor_CpuUsage", "CPU Usage"), DashboardLocalization.Get("Dashboard_Status_Sample", "Sample")),
            new SensorReadingItem(DashboardLocalization.Get("Dashboard_Sensor_MemoryUsage", "Memory Usage"), DashboardLocalization.Get("Dashboard_Status_Sample", "Sample")),
            new SensorReadingItem(DashboardLocalization.Get("Dashboard_Sensor_FanSpeed", "Fan Speed"), DashboardLocalization.Get("Dashboard_Status_Sample", "Sample")),
            new SensorReadingItem(DashboardLocalization.Get("Dashboard_Sensor_Battery", "Battery"), DashboardLocalization.Get("Dashboard_Status_Sample", "Sample")),
        ]);
}
