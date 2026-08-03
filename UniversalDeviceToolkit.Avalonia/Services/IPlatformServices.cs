using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Cross-platform facade over platform capability services.
/// The Windows TFM branch reads real Lenovo/Windows telemetry through
/// UniversalDeviceToolkit.Lib; the portable branch returns sample data so
/// linux-x64 / osx-arm64 builds stay functional.
/// </summary>
public interface IPlatformServices
{
    Task<IReadOnlyList<FeatureGroupItem>> GetFeatureGroupsAsync();
    Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync();
    Task<bool> IsSupportedLegionMachineAsync();
}

public sealed record FeatureGroupItem(string Title, string Description, string Status);
public sealed record SensorReadingItem(string Name, string DisplayValue);