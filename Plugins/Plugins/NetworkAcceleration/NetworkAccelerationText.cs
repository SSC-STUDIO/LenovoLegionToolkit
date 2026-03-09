using System.Globalization;
using LenovoLegionToolkit.Plugins.NetworkAcceleration.Resources;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public static class NetworkAccelerationText
{
    public static string PluginName => Resource.PluginName;
    public static string PluginDescription => Resource.PluginDescription;
    public static string PageTitle => Resource.PageTitle;
    public static string SettingsPageTitle => Resource.SettingsPageTitle;
    public static string QuickActionsTitle => Resource.QuickActionsTitle;
    public static string QuickActionsDescription => Resource.QuickActionsDescription;
    public static string RunQuickOptimizationButton => Resource.RunQuickOptimizationButton;
    public static string ResetNetworkStackButton => Resource.ResetNetworkStackButton;
    public static string AdminHint => Resource.AdminHint;
    public static string PreferredModeTitle => Resource.PreferredModeTitle;
    public static string SaveModeButton => Resource.SaveModeButton;
    public static string ModeBalanced => Resource.ModeBalanced;
    public static string ModeGaming => Resource.ModeGaming;
    public static string ModeStreaming => Resource.ModeStreaming;
    public static string StatusQuickOptimizationCompleted => Resource.StatusQuickOptimizationCompleted;
    public static string StatusQuickOptimizationFailed => Resource.StatusQuickOptimizationFailed;
    public static string StatusResetCompleted => Resource.StatusResetCompleted;
    public static string StatusResetFailed => Resource.StatusResetFailed;
    public static string StatusSelectValidMode => Resource.StatusSelectValidMode;
    public static string StatusModeSaved => Resource.StatusModeSaved;
    public static string SettingsTitle => Resource.SettingsTitle;
    public static string SettingsDescription => Resource.SettingsDescription;
    public static string AutoOptimizeOnStartup => Resource.AutoOptimizeOnStartup;
    public static string ResetWinsockOnOptimize => Resource.ResetWinsockOnOptimize;
    public static string ResetTcpIpOnOptimize => Resource.ResetTcpIpOnOptimize;
    public static string SaveSettingsButton => Resource.SaveSettingsButton;
    public static string SettingsSaved => Resource.SettingsSaved;
    public static string LiveTelemetryTitle => Resource.LiveTelemetryTitle;
    public static string LiveTelemetryDescription => Resource.LiveTelemetryDescription;
    public static string CurrentDownloadLabel => Resource.CurrentDownloadLabel;
    public static string CurrentUploadLabel => Resource.CurrentUploadLabel;
    public static string PeakTrafficLabel => Resource.PeakTrafficLabel;
    public static string ActiveAdapterLabel => Resource.ActiveAdapterLabel;
    public static string DownloadLegend => Resource.DownloadLegend;
    public static string UploadLegend => Resource.UploadLegend;
    public static string ChartEmptyState => Resource.ChartEmptyState;
    public static string DownloadTotalLabel => Resource.DownloadTotalLabel;
    public static string UploadTotalLabel => Resource.UploadTotalLabel;
    public static string UpdatedLabel => Resource.UpdatedLabel;
    public static string PreferredModeDescription => Resource.PreferredModeDescription;
    public static string CurrentModeLabel => Resource.CurrentModeLabel;
    public static string ModeBalancedDescription => Resource.ModeBalancedDescription;
    public static string ModeGamingDescription => Resource.ModeGamingDescription;
    public static string ModeStreamingDescription => Resource.ModeStreamingDescription;
    public static string StatusCardTitle => Resource.StatusCardTitle;
    public static string MonitoringStatus => Resource.MonitoringStatus;
    public static string NoActiveAdapter => Resource.NoActiveAdapter;
    public static string MbpsValueFormat => Resource.MbpsValueFormat;
    public static string SettingsSummaryTitle => Resource.SettingsSummaryTitle;
    public static string SettingsSummaryDescription => Resource.SettingsSummaryDescription;
    public static string StateEnabled => Resource.StateEnabled;
    public static string StateDisabled => Resource.StateDisabled;

    public static string FormatRate(double value) => string.Format(CultureInfo.CurrentUICulture, MbpsValueFormat, value);
}
