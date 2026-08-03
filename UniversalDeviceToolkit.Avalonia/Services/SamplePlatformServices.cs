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
            new("Power Mode", "System power management", "Sample"),
            new("Fan Control", "Fan speed management", "Sample"),
            new("Display", "Refresh rate control", "Sample"),
            new("GPU", "GPU management", "Sample"),
            new("Battery", "Battery management", "Sample"),
            new("Keyboard", "Backlight control", "Sample"),
        ]);

    public Task<bool> IsSupportedLegionMachineAsync() => Task.FromResult(false);

    public Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync() =>
        Task.FromResult<IReadOnlyList<SensorReadingItem>>(
        [
            new SensorReadingItem("CPU Temperature", "Sample"),
            new SensorReadingItem("GPU Temperature", "Sample"),
            new SensorReadingItem("CPU Usage", "Sample"),
            new SensorReadingItem("Memory Usage", "Sample"),
            new SensorReadingItem("Fan Speed", "Sample"),
            new SensorReadingItem("Battery", "Sample"),
        ]);
}