using System;
using System.Globalization;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public static class NetworkAccelerationText
{
    public static CultureInfo Culture => Resources.Resource.Culture ?? CultureInfo.CurrentUICulture;

    public static string PluginName => T(nameof(PluginName), "Network Acceleration (Legacy)");
    public static string PluginDescription => T(nameof(PluginDescription), "Deprecated: network diagnostics and selective proxy acceleration are now built into Universal Device Toolkit. This legacy plugin is retained only for settings migration.");
    public static string PageTitle => T(nameof(PageTitle), "Network Acceleration (Legacy)");
    public static string HeroBadgeTitle => T(nameof(HeroBadgeTitle), "Traffic Pulse");
    public static string FeatureOverviewTitle => T(nameof(FeatureOverviewTitle), "Traffic Overview");
    public static string FeatureOverviewDescription => T(nameof(FeatureOverviewDescription), "Monitor live throughput, active adapter status, and quick recovery actions in one place.");
    public static string SettingsPageTitle => T(nameof(SettingsPageTitle), "Network Settings");
    public static string QuickActionsTitle => T(nameof(QuickActionsTitle), "Quick Network Actions");
    public static string QuickActionsDescription => T(nameof(QuickActionsDescription), "Run quick optimization commands for lower latency and cleaner socket state.");
    public static string RunQuickOptimizationButton => T(nameof(RunQuickOptimizationButton), "Run Quick Optimization");
    public static string ResetNetworkStackButton => T(nameof(ResetNetworkStackButton), "Reset Network Stack");
    public static string AdminHint => T(nameof(AdminHint), "Some operations may require administrator permission.");
    public static string LiveTelemetryTitle => T(nameof(LiveTelemetryTitle), "Live Telemetry");
    public static string LiveTelemetryDescription => T(nameof(LiveTelemetryDescription), "Live telemetry updates every few seconds to reflect current traffic.");
    public static string CurrentDownloadLabel => T(nameof(CurrentDownloadLabel), "Current Download");
    public static string CurrentUploadLabel => T(nameof(CurrentUploadLabel), "Current Upload");
    public static string PeakTrafficLabel => T(nameof(PeakTrafficLabel), "Peak Traffic");
    public static string ActiveAdapterLabel => T(nameof(ActiveAdapterLabel), "Active Adapter");
    public static string ServiceStateLabel => T(nameof(ServiceStateLabel), "Service Status");
    public static string ServiceStateRunning => T(nameof(ServiceStateRunning), "Running");
    public static string ServiceStateStopped => T(nameof(ServiceStateStopped), "Stopped");
    public static string SessionLabel => T(nameof(SessionLabel), "Session");
    public static string SessionNotStarted => T(nameof(SessionNotStarted), "Not Started");
    public static string StartServiceButton => T(nameof(StartServiceButton), "Start Acceleration");
    public static string StopServiceButton => T(nameof(StopServiceButton), "Stop");
    public static string RefreshButton => T(nameof(RefreshButton), "Refresh");
    public static string MenuButton => T(nameof(MenuButton), "Menu");
    public static string AccelerationTargetsTitle => T(nameof(AccelerationTargetsTitle), "Acceleration Targets");
    public static string AccelerationTargetsDescription => T(nameof(AccelerationTargetsDescription), "Select a configuration and start with one click. Layout and interactions based on Watt Toolkit.");
    public static string ModeBalancedTargetTitle => T(nameof(ModeBalancedTargetTitle), "Balanced Route");
    public static string ModeBalancedTargetDescription => T(nameof(ModeBalancedTargetDescription), "Suitable for everyday browsing, downloading, and mixed traffic scenarios with more stable overall performance.");
    public static string ModeGamingTargetTitle => T(nameof(ModeGamingTargetTitle), "Low Latency Route");
    public static string ModeGamingTargetDescription => T(nameof(ModeGamingTargetDescription), "Prioritize response speed for online gaming, voice, and matchmaking scenarios.");
    public static string ModeStreamingTargetTitle => T(nameof(ModeStreamingTargetTitle), "High Throughput Route");
    public static string ModeStreamingTargetDescription => T(nameof(ModeStreamingTargetDescription), "Prioritize sustained bandwidth for streaming, updates, and large file transfers.");
    public static string RecommendedForLabel => T(nameof(RecommendedForLabel), "Recommended For");
    public static string OptimizationFocusLabel => T(nameof(OptimizationFocusLabel), "Optimization Focus");
    public static string PlannedStepsLabel => T(nameof(PlannedStepsLabel), "Planned Steps");
    public static string PlannedStepsTitle => T(nameof(PlannedStepsTitle), "Optimization Plan");
    public static string PlannedStepsDescription => T(nameof(PlannedStepsDescription), "Preview the exact commands quick optimization will run with the current mode and toggles.");
    public static string ModeBalancedRecommendedFor => T(nameof(ModeBalancedRecommendedFor), "General daily use");
    public static string ModeBalancedFocus => T(nameof(ModeBalancedFocus), "Balanced DNS & socket cleanup");
    public static string ModeGamingRecommendedFor => T(nameof(ModeGamingRecommendedFor), "Online Gaming");
    public static string ModeGamingFocus => T(nameof(ModeGamingFocus), "Low latency priority & Winsock reset");
    public static string ModeStreamingRecommendedFor => T(nameof(ModeStreamingRecommendedFor), "Streaming & Downloads");
    public static string ModeStreamingFocus => T(nameof(ModeStreamingFocus), "High throughput priority & TCP/IP stack reset");
    public static string PresetStateReady => T(nameof(PresetStateReady), "Ready");
    public static string PresetStateActive => T(nameof(PresetStateActive), "Active");
    public static string DownloadTotalLabel => T(nameof(DownloadTotalLabel), "Total Downloaded");
    public static string UploadTotalLabel => T(nameof(UploadTotalLabel), "Total Uploaded");
    public static string UpdatedLabel => T(nameof(UpdatedLabel), "Updated");
    public static string PreferredModeTitle => T(nameof(PreferredModeTitle), "Preferred Mode");
    public static string PreferredModeDescription => T(nameof(PreferredModeDescription), "Select how network traffic is prioritized.");
    public static string SaveModeButton => T(nameof(SaveModeButton), "Save Mode");
    public static string ModeBalanced => T(nameof(ModeBalanced), "Balanced");
    public static string ModeGaming => T(nameof(ModeGaming), "Gaming");
    public static string ModeStreaming => T(nameof(ModeStreaming), "Streaming");
    public static string ModeBalancedDescription => T(nameof(ModeBalancedDescription), "Balanced profile for everyday connectivity.");
    public static string ModeGamingDescription => T(nameof(ModeGamingDescription), "Prioritize latency-sensitive traffic for gaming.");
    public static string ModeStreamingDescription => T(nameof(ModeStreamingDescription), "Favor steady throughput for streaming and downloads.");
    public static string StatusQuickOptimizationCompleted => T(nameof(StatusQuickOptimizationCompleted), "Quick optimization completed.");
    public static string StatusQuickOptimizationFailed => T(nameof(StatusQuickOptimizationFailed), "Quick optimization failed. Please run as administrator.");
    public static string StatusResetCompleted => T(nameof(StatusResetCompleted), "Network stack reset completed.");
    public static string StatusResetFailed => T(nameof(StatusResetFailed), "Network stack reset failed. Please run as administrator.");
    public static string StatusSelectValidMode => T(nameof(StatusSelectValidMode), "Select a valid mode.");
    public static string StatusModeSaved => T(nameof(StatusModeSaved), "Preferred mode saved.");
    public static string StatusServiceStarted => T(nameof(StatusServiceStarted), "Acceleration started.");
    public static string StatusServiceStopped => T(nameof(StatusServiceStopped), "Acceleration stopped.");
    public static string StatusRefreshed => T(nameof(StatusRefreshed), "Data refreshed.");
    public static string SettingsPendingSave => T(nameof(SettingsPendingSave), "Settings changed, save to apply.");
    public static string MonitoringStatus => T(nameof(MonitoringStatus), "Monitoring network activity.");
    public static string NoActiveAdapter => T(nameof(NoActiveAdapter), "No active network adapter.");
    public static string SettingsTitle => T(nameof(SettingsTitle), "Quick Optimization Behavior");
    public static string SettingsDescription => T(nameof(SettingsDescription), "Choose which recovery actions will be included when quick optimization runs.");
    public static string SettingsSummaryTitle => T(nameof(SettingsSummaryTitle), "Settings Summary");
    public static string SettingsSummaryDescription => T(nameof(SettingsSummaryDescription), "Configure quick optimization and monitoring preferences.");
    public static string CurrentModeLabel => T(nameof(CurrentModeLabel), "Current Mode");
    public static string StatusCardTitle => T(nameof(StatusCardTitle), "Status");
    public static string AutoOptimizeOnStartup => T(nameof(AutoOptimizeOnStartup), "Auto optimize on startup");
    public static string ResetWinsockOnOptimize => T(nameof(ResetWinsockOnOptimize), "Reset Winsock during quick optimization");
    public static string ResetTcpIpOnOptimize => T(nameof(ResetTcpIpOnOptimize), "Reset TCP/IP stack during quick optimization");
    public static string SaveSettingsButton => T(nameof(SaveSettingsButton), "Save Settings");
    public static string SettingsSaved => T(nameof(SettingsSaved), "Network acceleration settings saved.");
    public static string StateEnabled => T(nameof(StateEnabled), "Enabled");
    public static string StateDisabled => T(nameof(StateDisabled), "Disabled");
    public static string StepFlushDns => T(nameof(StepFlushDns), "Flush DNS cache");
    public static string StepResetWinsock => T(nameof(StepResetWinsock), "Reset Winsock catalog");
    public static string StepResetTcpIp => T(nameof(StepResetTcpIp), "Reset TCP/IP stack");
    public static string StepSourceRequired => T(nameof(StepSourceRequired), "explicitly enabled");
    public static string StepSourceModeDriven => T(nameof(StepSourceModeDriven), "included by selected mode");
    public static string OptimizationPlanStepFormat => T(nameof(OptimizationPlanStepFormat), "{0}. {1}  [{3}]\n{2}");
    public static string NoPlannedOptimizationSteps => T(nameof(NoPlannedOptimizationSteps), "No optimization steps are currently planned.");
    public static string MbpsValueFormat => T(nameof(MbpsValueFormat), "{0:0.0} Mbps");
    public static string ErrorPrefix => T(nameof(ErrorPrefix), "Error");

    private static readonly System.Resources.ResourceManager ResourceManager =
        new("LenovoLegionToolkit.Plugins.NetworkAcceleration.Resources.Resource", typeof(NetworkAccelerationText).Assembly);

    private static string T(string key, string fallback)
    {
        return ResourceManager.GetString(key, Culture) ?? fallback;
    }

}
