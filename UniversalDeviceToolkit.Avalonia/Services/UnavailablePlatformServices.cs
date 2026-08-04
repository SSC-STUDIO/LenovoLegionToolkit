using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Empty production fallback for runtimes without a registered adapter.
/// </summary>
public sealed class UnavailablePlatformServices : IPlatformServices
{
    public Task<IReadOnlyList<FeatureGroupItem>> GetFeatureGroupsAsync() =>
        Task.FromResult<IReadOnlyList<FeatureGroupItem>>(
        [
            new(
                AvaloniaLocalization.GetString("Dashboard_Feature_SystemTelemetry", "System Telemetry"),
                AvaloniaLocalization.GetString("Dashboard_Description_PlatformCapability", "Platform capability"),
                AvaloniaLocalization.GetString("Dashboard_Status_NotSupported", "Not supported")),
        ]);

    public Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync() =>
        Task.FromResult<IReadOnlyList<SensorReadingItem>>(
        [
            new(
                AvaloniaLocalization.GetString("Dashboard_Feature_SystemTelemetry", "System Telemetry"),
                AvaloniaLocalization.GetString("Dashboard_Status_NotSupported", "Not supported")),
        ]);

    public Task<bool> IsSupportedLegionMachineAsync() => Task.FromResult(false);
}
