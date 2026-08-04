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
    Task<PluginPageState> GetPluginPageStateAsync(string pluginId);
    Task<bool> SetFeatureActionAsync(string routeKey, string actionKey, bool isSelected);
    Task<MacroWorkspaceState> GetMacroWorkspaceAsync();
    Task<bool> SetMacroEnabledAsync(bool enabled);
    Task<bool> SetMacroSequenceOptionsAsync(ulong key, int repeatCount, bool ignoreDelays, bool interruptOnOtherKey);
    Task<AutomationWorkspaceState> GetAutomationWorkspaceAsync();
    Task<bool> SetAutomationEnabledAsync(bool enabled);
    Task<bool> SaveAutomationWorkspaceAsync(IReadOnlyList<AutomationPipelineDraft> pipelines);
    Task<KeyboardLightingState?> GetKeyboardLightingStateAsync();
    Task<bool> SetKeyboardLightingAsync(KeyboardLightingUpdate update);
    Task<NetworkAccelerationState> GetNetworkAccelerationStateAsync();
    Task<bool> SetNetworkAccelerationEnabledAsync(bool enabled);
    Task<bool> SetNetworkAccelerationModeAsync(string mode);
    Task<bool> SetNetworkAccelerationGroupEnabledAsync(string groupId, bool enabled);
    Task<bool> ToggleNetworkAccelerationAsync();
    Task<string> RunNetworkDiagnosticsAsync();
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
    bool IsToggle,
    string? Category = null);

/// <summary>
/// Host-neutral description of a plugin entry page. WPF plugins can expose a
/// WPF control that Avalonia cannot embed; those entries remain routable and
/// receive an explicit compatibility state instead of a placeholder page.
/// </summary>
public sealed record PluginPageState(
    string PluginId,
    string Title,
    string Description,
    string? IconIdentifier,
    bool IsInstalled,
    bool HasFeaturePage,
    bool IsAvaloniaPage,
    string StatusMessage,
    object? Content = null);

/// <summary>
/// Editing projection for the shared automation store. The Avalonia host preserves
/// every existing pipeline while allowing names, deletion, ordering and manual
/// quick-action creation to round-trip through the same store as WPF.
/// </summary>
public sealed record AutomationWorkspaceState(
    bool IsEnabled,
    IReadOnlyList<AutomationPipelineItem> Pipelines);

public sealed record MacroWorkspaceState(
    bool IsEnabled,
    bool IsRecording,
    IReadOnlyList<MacroSlotState> Slots);

public sealed record MacroSlotState(
    ulong Key,
    int EventCount,
    int RepeatCount,
    bool IgnoreDelays,
    bool InterruptOnOtherKey);

public sealed record AutomationPipelineItem(
    Guid Id,
    string? Name,
    string? IconName,
    string Trigger,
    int StepCount,
    bool IsAutomatic);

public sealed record AutomationPipelineDraft(
    Guid? Id,
    string? Name,
    string? IconName,
    bool IsAutomatic);

public sealed record KeyboardColorState(byte R, byte G, byte B)
{
    public string Hex => $"#{R:X2}{G:X2}{B:X2}";
}

public sealed record KeyboardSpectrumEffectState(
    string Type,
    string Speed,
    string Direction,
    string ClockwiseDirection,
    IReadOnlyList<KeyboardColorState> Colors,
    IReadOnlyList<ushort> Keys);

public sealed record KeyboardRgbPresetState(
    string Key,
    string DisplayName,
    bool IsSelected,
    string Effect,
    string Speed,
    string Brightness,
    IReadOnlyList<KeyboardColorState> Zones);

public sealed record KeyboardLightingState(
    string Mode,
    int Brightness,
    bool LogoEnabled,
    int SelectedProfile,
    IReadOnlyList<KeyboardSpectrumEffectState> SpectrumEffects,
    IReadOnlyList<KeyboardRgbPresetState> RgbPresets);

public sealed record KeyboardLightingUpdate(
    string Mode,
    int? SelectedProfile = null,
    int? Brightness = null,
    bool? LogoEnabled = null,
    string? RgbPreset = null,
    string? RgbEffect = null,
    string? RgbSpeed = null,
    string? RgbBrightness = null,
    IReadOnlyList<KeyboardColorState>? RgbZones = null,
    IReadOnlyList<KeyboardSpectrumEffectState>? SpectrumEffects = null);

public sealed record NetworkAccelerationGroupState(
    string Id,
    string DisplayName,
    string Description,
    bool IsEnabled,
    bool IsFavorite,
    int DomainCount);

public sealed record NetworkAccelerationState(
    bool IsAvailable,
    bool IsBackendReady,
    bool IsEnabled,
    bool IsRunning,
    string Mode,
    string Status,
    int ListenPort,
    IReadOnlyList<NetworkAccelerationGroupState> Groups,
    string? Diagnostics = null);
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
