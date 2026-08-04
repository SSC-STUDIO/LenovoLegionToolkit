using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Cross-platform facade used by the Avalonia dashboard.
/// Platform adapters report only capabilities that were actually detected.
/// </summary>
public interface IPlatformServices
{
    Task<IReadOnlyList<FeatureGroupItem>> GetFeatureGroupsAsync();
    Task<IReadOnlyList<SensorReadingItem>> GetSensorReadingsAsync();
    Task<bool> IsSupportedLegionMachineAsync();
}

public sealed record FeatureGroupItem(string Title, string Description, string Status);
/// <summary>
/// Read-only sensor data prepared for the dashboard. Value and Unit are kept
/// separately so the UI can render a stable metric row and an optional gauge
/// without parsing localized display text.
/// </summary>
public sealed record SensorReadingItem(
    string Name,
    string DisplayValue,
    string Category = "",
    double? Value = null,
    string Unit = "")
{
    public bool HasProgress => Value is >= 0 and <= 100 &&
                                Unit.Contains('%', StringComparison.Ordinal);

    public double ProgressPercent => Math.Clamp(Value ?? 0, 0, 100);

    public string CategoryLabel => string.IsNullOrWhiteSpace(Category) ? "Sensor" : Category;
}
