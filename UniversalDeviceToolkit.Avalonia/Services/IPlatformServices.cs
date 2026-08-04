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
    Task<DashboardSnapshot> GetDashboardSnapshotAsync();
    Task<bool> IsSupportedLegionMachineAsync();
    Task<FeaturePageState> GetFeaturePageStateAsync(string routeKey);
    Task<bool> SetFeatureActionAsync(string routeKey, string actionKey, bool isSelected);
}

public sealed record FeatureGroupItem(string Title, string Description, string Status);

/// <summary>
/// UI-facing state for a feature route. The page model deliberately keeps action state
/// separate from the dashboard capability snapshot so Avalonia can expose the same
/// loading/selection/error contract as the WPF pages.
/// </summary>
public sealed record FeaturePageState(
    string RouteKey,
    string Title,
    string Description,
    string StatusTitle,
    string StatusMessage,
    bool IsAvailable,
    IReadOnlyList<FeatureActionItem> Actions);

public sealed record FeatureActionItem(
    string Key,
    string Title,
    string Description,
    string Status,
    bool IsEnabled,
    bool IsSelected,
    bool IsToggle);
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

/// <summary>
/// Refreshable dashboard payload. History is owned by the view model so adapters only need to
/// provide a coherent current snapshot and never retain UI state.
/// </summary>
public sealed record DashboardSnapshot(
    string DeviceName,
    string DeviceSupport,
    string PowerStatus,
    IReadOnlyList<FeatureGroupItem> FeatureGroups,
    IReadOnlyList<SensorReadingItem> SensorReadings,
    DateTimeOffset CapturedAtUtc);
