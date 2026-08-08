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
    /// <summary>
    /// Reads the optional WPF SensorsControl detail metrics without exposing
    /// WPF controls or localized display strings to the Avalonia host.
    /// </summary>
    Task<SensorDetailsSnapshot> GetSensorDetailsAsync();
    Task<DashboardSnapshot> GetDashboardSnapshotAsync();
    /// <summary>
    /// Reads the lightweight battery projection independently from the full
    /// dashboard snapshot. Windows refreshes this surface more frequently than
    /// the configurable sensor polling interval.
    /// </summary>
    Task<DashboardBatteryState> GetDashboardBatteryStateAsync();
    Task<DashboardLayoutState> GetDashboardLayoutAsync();
    Task<bool> SaveDashboardLayoutAsync(DashboardLayoutState layout);
    /// <summary>
    /// Reads the state for standard dashboard controls. Dedicated controls,
    /// such as the discrete GPU and monitor actions, are intentionally omitted
    /// because they expose their own host-neutral state contracts below.
    /// </summary>
    Task<IReadOnlyList<DashboardItemState>> GetDashboardItemStatesAsync(IReadOnlyList<string> itemIdentifiers);
    Task<bool> SetDashboardItemStateAsync(string itemIdentifier, string state);
    /// <summary>
    /// Reads the optional Balance-mode AI configuration exposed by the WPF
    /// dashboard settings window. Hosts that do not support AI mode return an
    /// unavailable state rather than exposing a non-functional command.
    /// </summary>
    Task<BalanceModeSettingsState> GetBalanceModeSettingsAsync();
    Task<bool> SaveBalanceModeSettingsAsync(bool aiModeEnabled);
    Task<GodModeSettingsState> GetGodModeSettingsAsync();
    Task<IReadOnlyList<ushort>?> GetDefaultGodModeFanCurveAsync();
    Task<bool> SetGodModePresetAsync(Guid presetId);
    Task<bool> AddGodModePresetAsync(string name);
    Task<bool> RenameGodModePresetAsync(Guid presetId, string name);
    Task<bool> DeleteGodModePresetAsync(Guid presetId);
    Task<bool> SaveGodModeSettingsAsync(GodModeSettingsUpdate update);
    Task<DiscreteGpuState> GetDiscreteGpuStateAsync();
    Task<bool> KillDiscreteGpuProcessesAsync();
    Task<bool> RestartDiscreteGpuAsync();
    Task<bool> TurnOffMonitorsAsync();
    Task<GpuOverclockState> GetGpuOverclockStateAsync();
    Task<bool> SetGpuOverclockAsync(bool enabled, int coreDeltaMhz, int memoryDeltaMhz);
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
    Task<CleanupExecutionResult> RunSelectedCleanupAsync(IProgress<CleanupProgressState>? progress = null);
    Task<MacroWorkspaceState> GetMacroWorkspaceAsync();
    Task<bool> SetMacroEnabledAsync(bool enabled);
    Task<bool> StartMacroRecordingAsync(ulong key, MacroRecordingMode mode);
    Task<bool> SetMacroSequenceOptionsAsync(ulong key, int repeatCount, bool ignoreDelays, bool interruptOnOtherKey);
    Task<bool> SaveMacroSequenceAsync(
        ulong key,
        IReadOnlyList<MacroEventItem> events,
        int repeatCount,
        bool ignoreDelays,
        bool interruptOnOtherKey);
    Task<bool> ClearMacroSequenceAsync(ulong key);
    Task<AutomationWorkspaceState> GetAutomationWorkspaceAsync();
    Task<IReadOnlyList<AutomationTriggerOption>> GetAutomationTriggerOptionsAsync();
    Task<IReadOnlyList<AutomationStepOption>> GetAutomationStepOptionsAsync();
    Task<bool> SetAutomationEnabledAsync(bool enabled);
    Task<bool> SaveAutomationWorkspaceAsync(IReadOnlyList<AutomationPipelineDraft> pipelines);
    Task<KeyboardLightingState?> GetKeyboardLightingStateAsync();
    Task<bool> SetKeyboardLightingAsync(KeyboardLightingUpdate update);
    Task<bool> ResetKeyboardSpectrumProfileAsync();
    Task<bool> ExportKeyboardSpectrumProfileAsync(string filePath);
    Task<bool> ImportKeyboardSpectrumProfileAsync(string filePath);
    Task<NetworkAccelerationState> GetNetworkAccelerationStateAsync();
    Task<bool> SetNetworkAccelerationEnabledAsync(bool enabled);
    Task<bool> SetNetworkAccelerationModeAsync(string mode);
    Task<bool> SetNetworkAccelerationGroupEnabledAsync(string groupId, bool enabled);
    Task<bool> ToggleNetworkAccelerationAsync();
    Task<string> RunNetworkDiagnosticsAsync();
    Task<string> RestoreNetworkAccelerationAsync();
    Task<NetworkAccelerationRuntimeState> GetNetworkAccelerationRuntimeAsync();
    Task<NetworkNatDiagnosticState> RunNetworkNatDiagnosticAsync(string stunHost);
    Task<NetworkDnsDiagnosticState> RunNetworkDnsDiagnosticAsync(
        string domain,
        string? dnsServer,
        bool useDoh,
        string? dohUrl);
    Task<NetworkIpv6DiagnosticState> RunNetworkIpv6DiagnosticAsync();
    Task<DriverDownloadState> GetDriverDownloadStateAsync();
    Task<DriverDownloadState> SearchDriverPackagesAsync(string source, string machineType, string os, bool onlyUpdates);
    Task<bool> DownloadDriverPackageAsync(string packageId, string destinationFolder);
    Task<DriverDownloadState> SetDriverDownloadPathAsync(string downloadPath);
    Task<DriverDownloadState> SetSelectedDriverPackagesAsync(IReadOnlyCollection<string> packageIds);
    Task<DriverDownloadState> SelectRecommendedDriverPackagesAsync();
    Task<DriverDownloadState> StartSelectedDriverPackagesAsync();
    Task<DriverDownloadState> PauseDriverDownloadsAsync();
    Task<DriverDownloadState> HideDriverPackagesAsync(IReadOnlyCollection<string> packageIds);
    Task<DriverDownloadState> RestoreHiddenDriverPackagesAsync();
}

/// <summary>
/// Keeps dashboard state routing aligned across the Windows host, the portable
/// adapter and the unavailable-host fallback. Dedicated cards must not be sent
/// through the standard option/toggle state contract.
/// </summary>
public static class DashboardItemStateRouting
{
    private static readonly HashSet<string> DedicatedControlIdentifiers = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "DiscreteGpu",
        "OverclockDiscreteGpu",
        "TurnOffMonitors",
    };

    public static bool IsDedicatedControl(string? identifier) =>
        !string.IsNullOrWhiteSpace(identifier)
        && DedicatedControlIdentifiers.Contains(identifier);
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
    string? Category = null,
    FeatureActionStatusKind StatusKind = FeatureActionStatusKind.Neutral,
    bool IsApplied = false,
    bool IsRecommendedTag = false,
    string? CategoryPluginId = null);

/// <summary>
/// One cleanup step as observed by the Avalonia host. A failure is recorded per
/// action so the UI can mirror the WPF partial-result summary instead of
/// reporting a whole batch as successful.
/// </summary>
public sealed record CleanupActionResult(
    string ActionKey,
    string Title,
    bool Succeeded,
    long FreedBytes,
    string? Error = null);

public sealed record CleanupProgressState(
    int CompletedCount,
    int TotalCount,
    string ActionTitle,
    long FreedBytes);

public sealed record CleanupExecutionResult(
    int RequestedCount,
    int SucceededCount,
    int FailedCount,
    long FreedBytes,
    TimeSpan Elapsed,
    IReadOnlyList<CleanupActionResult> Actions)
{
    public bool Succeeded => RequestedCount > 0 && FailedCount == 0;
    public bool HasPartialFailure => SucceededCount > 0 && FailedCount > 0;
}

/// <summary>
/// Semantic visual state for a feature action. The UI uses this instead of
/// assuming that a translated status string has a particular value.
/// </summary>
public enum FeatureActionStatusKind
{
    Neutral,
    Info,
    Success,
    Warning,
    Critical,
}

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

public enum MacroRecordingMode
{
    Keyboard,
    KeyboardMouse,
    KeyboardMouseMovement,
}

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
    IReadOnlyList<KeyboardRgbPresetState> RgbPresets,
    string KeyboardLayout = "Ansi",
    string SpectrumLayout = "KeyboardOnly",
    IReadOnlyList<ushort>? KeyboardKeys = null,
    bool IsBlockedByVantage = false);

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
    IReadOnlyList<KeyboardSpectrumEffectState>? SpectrumEffects = null,
    string? KeyboardLayout = null);

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
/// Read-only proxy-worker telemetry. Values remain structured so the Avalonia
/// host can render the same traffic and endpoint summaries as WPF without
/// parsing localized status strings.
/// </summary>
public sealed record NetworkAccelerationRuntimeState(
    bool IsAvailable,
    bool IsRunning,
    long BytesUploaded,
    long BytesDownloaded,
    int ActiveConnections,
    long TotalConnections,
    string HealthStatus,
    string Status,
    IReadOnlyList<NetworkAccelerationConnectionState> Connections,
    IReadOnlyList<NetworkAccelerationDestinationState> Destinations);

public sealed record NetworkAccelerationConnectionState(
    string Host,
    int Port,
    string Protocol,
    string State,
    long BytesUploaded,
    long BytesDownloaded,
    long? ConnectLatencyMs,
    string? Error);

public sealed record NetworkAccelerationDestinationState(
    string Host,
    int Port,
    int ActiveConnections,
    long TotalConnections,
    long BytesUploaded,
    long BytesDownloaded,
    long? LastConnectLatencyMs,
    string LastState);

public sealed record NetworkNatDiagnosticState(
    bool IsAvailable,
    string Type,
    string? LocalIp,
    string? PublicIp,
    bool InternetAvailable,
    string? Error = null);

public sealed record NetworkDnsProbeState(
    string Channel,
    bool Success,
    IReadOnlyList<string> Addresses,
    long ElapsedMs,
    string? Error = null);

public sealed record NetworkDnsDiagnosticState(
    bool IsAvailable,
    IReadOnlyList<NetworkDnsProbeState> Probes,
    string? Error = null);

public sealed record NetworkIpv6DiagnosticState(
    bool IsAvailable,
    bool Supported,
    string? Address,
    string? Error = null);

public sealed record DriverPackageItem(
    string Id,
    string Title,
    string Description,
    string Version,
    string Category,
    string FileSize,
    bool IsUpdate,
    bool IsRecommended,
    DateTime ReleaseDate = default,
    bool IsSelected = false,
    DriverPackageStatus Status = DriverPackageStatus.NotStarted,
    float Progress = 0,
    string? Error = null);

public enum DriverPackageStatus
{
    NotStarted,
    Queued,
    Downloading,
    Paused,
    Completed,
    Failed,
}

public sealed record DriverDownloadState(
    bool IsAvailable,
    bool IsScanning,
    string MachineType,
    string Os,
    string Source,
    IReadOnlyList<DriverPackageItem> Packages,
    string? Error = null,
    string DownloadPath = "",
    bool OnlyShowUpdates = false,
    int HiddenPackageCount = 0,
    bool IsQueueRunning = false);
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
/// Optional detail metrics shown when a dashboard telemetry card is expanded.
/// Nullable values preserve the distinction between an unsupported metric and
/// a valid zero reading. Values use invariant units so each UI host can format
/// them according to its current language and temperature preferences.
/// </summary>
public sealed record SensorDetailsSnapshot
{
    public static SensorDetailsSnapshot Empty { get; } = new();

    public bool IsAvailable { get; init; }
    public bool IsIntegratedGpu { get; init; }

    public double? CpuPowerWatts { get; init; }
    public double? CpuCoresPowerWatts { get; init; }
    public double? CpuMemoryPowerWatts { get; init; }
    public double? CpuPlatformPowerWatts { get; init; }
    public double? CpuVoltageVolts { get; init; }
    public double? CpuPCoreClockMHz { get; init; }
    public double? CpuECoreClockMHz { get; init; }
    public double? CpuMemoryUsagePercent { get; init; }
    public double? CpuMemoryUsedGb { get; init; }
    public double? CpuMemoryTotalGb { get; init; }
    public double? CpuMemoryTemperatureCelsius { get; init; }
    public double? CpuSsdTemperature1Celsius { get; init; }
    public double? CpuSsdTemperature2Celsius { get; init; }
    public double? CpuTemperatureMinimumCelsius { get; init; }
    public double? CpuTemperatureMaximumCelsius { get; init; }
    public double? CpuVoltageMinimumVolts { get; init; }
    public double? CpuVoltageMaximumVolts { get; init; }

    public double? GpuMemoryClockMHz { get; init; }
    public double? GpuPowerWatts { get; init; }
    public double? GpuVoltageVolts { get; init; }
    public double? GpuVramUsedGb { get; init; }
    public double? GpuVramTotalGb { get; init; }
    public double? GpuVramUsagePercent { get; init; }
    public double? GpuVramTemperatureCelsius { get; init; }
    public double? GpuHotSpotTemperatureCelsius { get; init; }
    public double? GpuPcieRxBytesPerSecond { get; init; }
    public double? GpuPcieTxBytesPerSecond { get; init; }
    public double? GpuTemperatureMinimumCelsius { get; init; }
    public double? GpuTemperatureMaximumCelsius { get; init; }
    public double? GpuVoltageMinimumVolts { get; init; }
    public double? GpuVoltageMaximumVolts { get; init; }

    public double? BatteryHealthPercent { get; init; }
    public bool BatteryIsCharging { get; init; }
    public bool BatteryIsLowBattery { get; init; }
    public string? BatteryPowerAdapterStatus { get; init; }
    public double? BatteryPercentage { get; init; }
    public double? BatteryLifeRemainingSeconds { get; init; }
    public double? BatteryFullLifeRemainingSeconds { get; init; }
    public double? BatteryRateWatts { get; init; }
    public double? BatteryMinRateWatts { get; init; }
    public double? BatteryMaxRateWatts { get; init; }
    public double? BatteryDesignCapacityWh { get; init; }
    public double? BatteryChargeCapacityWh { get; init; }
    public double? BatteryFullCapacityWh { get; init; }
    public double? BatteryCycleCount { get; init; }
    public double? BatteryTemperatureCelsius { get; init; }
    public DateTimeOffset? BatteryManufactureDate { get; init; }
    public DateTimeOffset? BatteryFirstUseDate { get; init; }
    public DateTimeOffset? BatteryOnBatterySince { get; init; }
    public string? BatteryModelName { get; init; }
}

/// <summary>
/// Lightweight battery state included in the regular dashboard snapshot. It
/// keeps warnings and the status/icon current without forcing the expensive
/// CPU/GPU detail query that is reserved for an expanded card.
/// </summary>
public sealed record DashboardBatteryState
{
    public static DashboardBatteryState Empty { get; } = new();

    public bool IsAvailable { get; init; }
    public bool IsCharging { get; init; }
    public bool IsLowBattery { get; init; }
    public string PowerAdapterStatus { get; init; } = "Unknown";
    public double? Percentage { get; init; }
    public double? LifeRemainingSeconds { get; init; }
    public double? FullLifeRemainingSeconds { get; init; }
    public double? DischargeRateWatts { get; init; }
    public double? MinDischargeRateWatts { get; init; }
    public double? MaxDischargeRateWatts { get; init; }
    public double? DesignCapacityWh { get; init; }
    public double? ChargeCapacityWh { get; init; }
    public double? FullCapacityWh { get; init; }
    public double? HealthPercent { get; init; }
    public double? CycleCount { get; init; }
    public double? TemperatureCelsius { get; init; }
    public DateTimeOffset? ManufactureDate { get; init; }
    public DateTimeOffset? FirstUseDate { get; init; }
    public DateTimeOffset? OnBatterySince { get; init; }
    public string? ModelName { get; init; }
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
    DateTimeOffset CapturedAtUtc,
    DashboardBatteryState? Battery = null);

/// <summary>
/// Host-neutral projection of the WPF dashboard layout document. Group and item
/// identifiers remain stable strings so Avalonia can round-trip the shared
/// dashboard.json without taking a dependency on WPF enum types.
/// </summary>
public sealed record DashboardLayoutState(
    bool ShowSensors,
    int SensorsRefreshIntervalSeconds,
    IReadOnlyList<DashboardGroupState> Groups);

public sealed record DashboardGroupState(
    string Type,
    string? CustomName,
    IReadOnlyList<string> Items);

/// <summary>
/// Host-neutral state for one persisted dashboard item. Options are stable enum
/// names so the Avalonia host can localize them without depending on WPF types.
/// </summary>
public sealed record DashboardItemState(
    string Identifier,
    bool IsAvailable,
    string? CurrentValue,
    IReadOnlyList<string> Options,
    string? ErrorMessage = null);

/// <summary>Host-neutral projection of the WPF Balance-mode AI settings.</summary>
public sealed record BalanceModeSettingsState(
    bool IsAvailable,
    bool IsAIModeEnabled,
    string? ErrorMessage = null);

/// <summary>One editable GodMode numeric parameter projected for Avalonia.</summary>
public sealed record GodModeValueState(
    string Key,
    string Title,
    string Description,
    string Unit,
    int Value,
    int Minimum,
    int Maximum,
    int Step,
    int? DefaultValue);

/// <summary>Host-neutral projection of one WPF GodMode preset.</summary>
public sealed record GodModePresetState(
    Guid Id,
    string Name,
    string? SourcePowerMode,
    IReadOnlyList<GodModeValueState> Values,
    bool? FanFullSpeed,
    int? MinValueOffset,
    int? MaxValueOffset,
    IReadOnlyList<ushort>? FanCurveValues);

/// <summary>Host-neutral projection of the GodMode settings window.</summary>
public sealed record GodModeSettingsState(
    bool IsAvailable,
    string? ErrorMessage,
    Guid ActivePresetId,
    IReadOnlyList<GodModePresetState> Presets,
    bool NeedsVantageDisabled = false,
    bool NeedsLegionZoneDisabled = false);

/// <summary>Editable values sent back from the Avalonia GodMode window.</summary>
public sealed record GodModeSettingsUpdate(
    Guid PresetId,
    IReadOnlyDictionary<string, int> Values,
    bool? FanFullSpeed,
    int? MinValueOffset,
    int? MaxValueOffset,
    IReadOnlyList<ushort>? FanCurveValues = null);

/// <summary>Host-neutral projection of the WPF discrete GPU monitor.</summary>
public sealed record DiscreteGpuState(
    bool IsAvailable,
    string Status,
    string PerformanceState,
    int ProcessCount,
    bool CanKillProcesses,
    bool CanRestart,
    string? Error = null);

/// <summary>Host-neutral projection of GPU overclock settings and limits.</summary>
public sealed record GpuOverclockState(
    bool IsAvailable,
    bool IsEnabled,
    int CoreDeltaMhz,
    int MemoryDeltaMhz,
    int MaxCoreDeltaMhz,
    int MaxMemoryDeltaMhz,
    string? Error = null);
