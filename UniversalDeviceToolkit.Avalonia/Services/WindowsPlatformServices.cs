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
    private readonly WindowsFeatureHostServices? _featureHost;

    private WindowsPlatformServices(IDeviceAdapter adapter)
    {
        _inner = new DeviceAdapterPlatformServices(adapter);
        _featureHost = WindowsFeatureHostServices.TryCreate();
    }

    public static IPlatformServices Create() => new WindowsPlatformServices(new WindowsDeviceAdapter());

    public Task<IReadOnlyList<FeatureGroupItem>> GetFeatureGroupsAsync() => _inner.GetFeatureGroupsAsync();

    public Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync() => _inner.GetSensorReadingsAsync();

    public Task<DashboardSnapshot> GetDashboardSnapshotAsync() => _inner.GetDashboardSnapshotAsync();

    public Task<bool> IsSupportedLegionMachineAsync() => _inner.IsSupportedLegionMachineAsync();

    public Task<FeaturePageState> GetFeaturePageStateAsync(string routeKey) =>
        _featureHost is null
            ? _inner.GetFeaturePageStateAsync(routeKey)
            : _featureHost.GetStateAsync(routeKey);

    public Task<bool> SetFeatureActionAsync(string routeKey, string actionKey, bool isSelected) =>
        _featureHost is null
            ? _inner.SetFeatureActionAsync(routeKey, actionKey, isSelected)
            : _featureHost.SetActionAsync(routeKey, actionKey, isSelected);

    public Task<KeyboardLightingState?> GetKeyboardLightingStateAsync() =>
        _featureHost is null
            ? _inner.GetKeyboardLightingStateAsync()
            : _featureHost.GetKeyboardLightingStateAsync();

    public Task<bool> SetKeyboardLightingAsync(KeyboardLightingUpdate update) =>
        _featureHost is null
            ? _inner.SetKeyboardLightingAsync(update)
            : _featureHost.SetKeyboardLightingAsync(update);
}

#endif
