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
    Task<bool> ImportPluginAsync(string zipFilePath);
    Task<PluginCatalogState> GetPluginCatalogAsync(bool forceRefresh = false);
    Task<bool> InstallPluginAsync(string pluginId);
    Task<bool> UpdatePluginAsync(string pluginId);
    Task<IReadOnlyList<CustomCleanupRuleItem>> GetCustomCleanupRulesAsync();
    Task<bool> SaveCustomCleanupRulesAsync(IReadOnlyList<CustomCleanupRuleItem> rules);
    Task<PluginPageState> GetPluginPageStateAsync(string pluginId);
    Task<PluginPageState> GetPluginSettingsPageStateAsync(string pluginId);
    Task<bool> SetFeatureActionAsync(string routeKey, string actionKey, bool isSelected);
    Task<MacroWorkspaceState> GetMacroWorkspaceAsync();
    Task<bool> SetMacroEnabledAsync(bool enabled);
    Task<bool> SetMacroSequenceOptionsAsync(ulong key, int repeatCount, bool ignoreDelays, bool interruptOnOtherKey);
    Task<bool> ClearMacroSequenceAsync(ulong key);
    Task<AutomationWorkspaceState> GetAutomationWorkspaceAsync();
    Task<IReadOnlyList<AutomationTriggerOption>> GetAutomationTriggerOptionsAsync();
    Task<IReadOnlyList<AutomationStepOption>> GetAutomationStepOptionsAsync();
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
    Task<DriverDownloadState> GetDriverDownloadStateAsync();
    Task<DriverDownloadState> SearchDriverPackagesAsync(string source, string machineType, string os, bool onlyUpdates);
    Task<bool> DownloadDriverPackageAsync(string packageId, string destinationFolder);
}

public sealed record FeatureGroupItem(
    string Title,
    string Description,
    string Status,
    string? RouteKey = null)
{
    /// <summary>
    /// Gets whether the dashboard can open a host page for this capability.
    /// Capability snapshots may contain vendor-specific entries that do not
    /// have a corresponding route, so those remain informational cards.
    /// </summary>
    public bool IsNavigable => !string.IsNullOrWhiteSpace(RouteKey);
}

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
    IReadOnlyList<FeatureActionItem> Actions,
    IReadOnlyList<CustomCleanupRuleItem>? CustomCleanupRules = null);

/// <summary>
/// Host-neutral projection of a persisted custom cleanup rule. Extensions are stored
/// without changing their original text; the cleanup service normalizes separators and
/// leading dots when it executes or estimates the rule.
/// </summary>
public sealed record CustomCleanupRuleItem(
    string DirectoryPath,
    IReadOnlyList<string> Extensions,
    bool Recursive);

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
/// Host-neutral projection of the online plugin catalog merged with locally
/// installed and registered extensions. The UI can filter and present this
/// list without depending on the WPF plugin view models.
/// </summary>
public sealed record PluginCatalogState(
    bool IsAvailable,
    string StatusMessage,
    IReadOnlyList<PluginCatalogItem> Plugins);

public sealed record PluginCatalogItem(
    string Id,
    string Name,
    string Description,
    string? Details,
    string Version,
    string Author,
    bool IsInstalled,
    bool IsSystemPlugin,
    string? AvailableUpdateVersion,
    bool SupportsSettingsPage,
    bool SupportsFeaturePage,
    bool SupportsOptimizationActions,
    IReadOnlyList<string> Tags);

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
    bool InterruptOnOtherKey,
    IReadOnlyList<MacroEventItem>? Events = null);

/// <summary>
/// Host-neutral projection of one recorded macro event. Keeping the event fields
/// primitive lets Avalonia render the sequence without taking a dependency on WPF
/// controls while preserving the data needed for an equivalent event summary.
/// </summary>
public sealed record MacroEventItem(
    string Source,
    string Direction,
    uint Key,
    int X,
    int Y,
    TimeSpan Delay);

public sealed record AutomationPipelineItem(
    Guid Id,
    string? Name,
    string? IconName,
    string Trigger,
    int StepCount,
    bool IsAutomatic)
{
    /// <summary>
    /// Stable host-neutral key for editing a known automatic trigger.
    /// Unknown trigger types remain null and are preserved by the host.
    /// </summary>
    public string? TriggerKey { get; init; }

    /// <summary>Serialized trigger configuration used for lossless advanced editing.</summary>
    public string? TriggerConfigurationJson { get; init; }

    /// <summary>Ordered, serialized steps exposed to the cross-platform editor.</summary>
    public IReadOnlyList<AutomationStepItem> Steps { get; init; } = Array.Empty<AutomationStepItem>();

    /// <summary>Whether a matching automatic pipeline stops subsequent pipelines.</summary>
    public bool IsExclusive { get; init; } = true;
}

public sealed record AutomationPipelineDraft(
    Guid? Id,
    string? Name,
    string? IconName,
    bool IsAutomatic)
{
    /// <summary>
    /// Stable trigger key selected by the Avalonia editor for automatic pipelines.
    /// </summary>
    public string? TriggerKey { get; init; }

    /// <summary>Serialized trigger configuration selected by the editor.</summary>
    public string? TriggerConfigurationJson { get; init; }

    /// <summary>Ordered serialized steps to persist with the pipeline.</summary>
    public IReadOnlyList<AutomationStepItem> Steps { get; init; } = Array.Empty<AutomationStepItem>();

    /// <summary>Whether the automatic pipeline is exclusive.</summary>
    public bool IsExclusive { get; init; } = true;
}

/// <summary>
/// Host-neutral automation step descriptor. The JSON payload is produced by the
/// shared automation converter and is therefore lossless for all known step types.
/// </summary>
public sealed record AutomationStepItem(
    string TypeKey,
    string DisplayName,
    string ConfigurationJson);

public sealed record AutomationStepOption(
    string TypeKey,
    string DisplayName,
    string DefaultConfigurationJson);

public sealed record AutomationTriggerOption(
    string Key,
    string DisplayName,
    string? DefaultConfigurationJson = null);

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

public sealed record DriverPackageItem(
    string Id,
    string Title,
    string Description,
    string Version,
    string Category,
    string FileSize,
    bool IsUpdate,
    bool IsRecommended);

public sealed record DriverDownloadState(
    bool IsAvailable,
    bool IsScanning,
    string MachineType,
    string Os,
    string Source,
    IReadOnlyList<DriverPackageItem> Packages,
    string? Error = null);
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
