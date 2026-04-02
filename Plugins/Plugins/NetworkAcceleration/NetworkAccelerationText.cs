using System;
using System.Globalization;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public static class NetworkAccelerationText
{
    public static CultureInfo Culture => Resources.Resource.Culture ?? CultureInfo.CurrentUICulture;

    public static string PluginName => T(nameof(PluginName), "Network Acceleration");
    public static string PluginDescription => T(nameof(PluginDescription), "Real-time network acceleration and optimization features.");
    public static string PageTitle => T(nameof(PageTitle), "Network Acceleration");
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
    public static string ServiceStateLabel => T(nameof(ServiceStateLabel), "服务状态");
    public static string ServiceStateRunning => T(nameof(ServiceStateRunning), "运行中");
    public static string ServiceStateStopped => T(nameof(ServiceStateStopped), "已停止");
    public static string SessionLabel => T(nameof(SessionLabel), "会话");
    public static string SessionNotStarted => T(nameof(SessionNotStarted), "未启动");
    public static string StartServiceButton => T(nameof(StartServiceButton), "启动加速");
    public static string StopServiceButton => T(nameof(StopServiceButton), "停止");
    public static string RefreshButton => T(nameof(RefreshButton), "刷新");
    public static string MenuButton => T(nameof(MenuButton), "菜单");
    public static string AccelerationTargetsTitle => T(nameof(AccelerationTargetsTitle), "加速目标");
    public static string AccelerationTargetsDescription => T(nameof(AccelerationTargetsDescription), "选择一个配置后即可一键启动，布局和交互参考 Watt Toolkit。");
    public static string ModeBalancedTargetTitle => T(nameof(ModeBalancedTargetTitle), "均衡线路");
    public static string ModeBalancedTargetDescription => T(nameof(ModeBalancedTargetDescription), "适合日常浏览、下载和混合流量场景，整体表现更稳定。");
    public static string ModeGamingTargetTitle => T(nameof(ModeGamingTargetTitle), "低延迟线路");
    public static string ModeGamingTargetDescription => T(nameof(ModeGamingTargetDescription), "优先保证联机游戏、语音和匹配场景的响应速度。");
    public static string ModeStreamingTargetTitle => T(nameof(ModeStreamingTargetTitle), "高吞吐线路");
    public static string ModeStreamingTargetDescription => T(nameof(ModeStreamingTargetDescription), "优先保证串流、更新和大文件传输时的持续带宽。");
    public static string RecommendedForLabel => T(nameof(RecommendedForLabel), "推荐场景");
    public static string OptimizationFocusLabel => T(nameof(OptimizationFocusLabel), "优化重点");
    public static string ModeBalancedRecommendedFor => T(nameof(ModeBalancedRecommendedFor), "日常综合使用");
    public static string ModeBalancedFocus => T(nameof(ModeBalancedFocus), "均衡 DNS 与套接字清理");
    public static string ModeGamingRecommendedFor => T(nameof(ModeGamingRecommendedFor), "联机游戏");
    public static string ModeGamingFocus => T(nameof(ModeGamingFocus), "低延迟优先与 Winsock 重置");
    public static string ModeStreamingRecommendedFor => T(nameof(ModeStreamingRecommendedFor), "串流与下载");
    public static string ModeStreamingFocus => T(nameof(ModeStreamingFocus), "高吞吐优先与 TCP/IP 协议栈重置");
    public static string PresetStateReady => T(nameof(PresetStateReady), "就绪");
    public static string PresetStateActive => T(nameof(PresetStateActive), "已启用");
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
    public static string StatusServiceStarted => T(nameof(StatusServiceStarted), "加速已启动。");
    public static string StatusServiceStopped => T(nameof(StatusServiceStopped), "加速已停止。");
    public static string StatusRefreshed => T(nameof(StatusRefreshed), "数据已刷新。");
    public static string SettingsPendingSave => T(nameof(SettingsPendingSave), "设置已变更，保存后生效。");
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
        return ResourceManager.GetString(key, Culture) ?? fallback;
    }

}
