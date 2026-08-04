#if WINDOWS

using System.Collections.Generic;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Platform.Windows;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Compatibility facade for callers that used the old Avalonia Windows service.
/// The dashboard now consumes the shared read-only device snapshot.
/// </summary>
public sealed class WindowsPlatformServices : IPlatformServices
{
    private readonly DeviceAdapterPlatformServices _inner;

    private WindowsPlatformServices(IDeviceAdapter adapter)
    {
        _inner = new DeviceAdapterPlatformServices(adapter);
    }

    public static IPlatformServices Create() => new WindowsPlatformServices(new WindowsDeviceAdapter());

    public Task<IReadOnlyList<FeatureGroupItem>> GetFeatureGroupsAsync() => _inner.GetFeatureGroupsAsync();

    public Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync() => _inner.GetSensorReadingsAsync();

    public Task<DashboardSnapshot> GetDashboardSnapshotAsync() => _inner.GetDashboardSnapshotAsync();

    public Task<bool> IsSupportedLegionMachineAsync() => _inner.IsSupportedLegionMachineAsync();
}

#endif
