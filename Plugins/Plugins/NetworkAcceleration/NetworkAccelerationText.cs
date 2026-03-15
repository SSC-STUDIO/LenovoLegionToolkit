using System;
using System.Globalization;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public static class NetworkAccelerationText
{
    public static string PluginName => T(nameof(PluginName), "Network Acceleration");
    public static string PluginDescription => T(nameof(PluginDescription), "Real-time network acceleration and optimization features.");
    public static string PageTitle => T(nameof(PageTitle), "Network Acceleration");
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
    public static string DownloadLegend => T(nameof(DownloadLegend), "Download");
    public static string UploadLegend => T(nameof(UploadLegend), "Upload");
    public static string ChartEmptyState => T(nameof(ChartEmptyState), "Waiting for telemetry...");
    public static string TrafficMixTitle => T(nameof(TrafficMixTitle), "Traffic Mix");
    public static string TrafficMixDescription => T(nameof(TrafficMixDescription), "Current download and upload share based on the latest sample.");
    public static string DownloadShareLabel => T(nameof(DownloadShareLabel), "Download Share");
    public static string UploadShareLabel => T(nameof(UploadShareLabel), "Upload Share");
    public static string BurstHistoryTitle => T(nameof(BurstHistoryTitle), "Burst History");
    public static string BurstHistoryDescription => T(nameof(BurstHistoryDescription), "Recent combined throughput spikes from the latest telemetry window.");
    public static string RollingAverageLabel => T(nameof(RollingAverageLabel), "Rolling Avg");
    public static string BurstPeakLabel => T(nameof(BurstPeakLabel), "Burst Peak");
    public static string BurstChartEmptyState => T(nameof(BurstChartEmptyState), "Need more samples to render burst history.");
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
    public static string MbpsValueFormat => T(nameof(MbpsValueFormat), "{0:0.0} Mbps");

    private static readonly System.Resources.ResourceManager ResourceManager =
        new("LenovoLegionToolkit.Plugins.NetworkAcceleration.Resources.Resource", typeof(NetworkAccelerationText).Assembly);

    private static string T(string key, string fallback)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;
    }

}
