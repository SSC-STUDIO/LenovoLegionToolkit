using System;
using System.Globalization;
#nullable enable


namespace LenovoLegionToolkit.Plugins.BatteryHealth;

public static class BatteryHealthText
{
    public static CultureInfo Culture => Resources.Resource.Culture ?? CultureInfo.CurrentUICulture;

    public static string PluginName => T(nameof(PluginName), "Battery Health");
    public static string PluginDescription => T(nameof(PluginDescription), "Monitor battery health, cycle count, and capacity wear for your Universal Device Toolkit.");
    public static string FeaturePageTitle => T(nameof(FeaturePageTitle), "Battery Health");
    public static string FeaturePageDescription => T(nameof(FeaturePageDescription), "Track battery wear, cycle count, and capacity degradation in real time.");
    public static string SettingsPageTitle => T(nameof(SettingsPageTitle), "Battery Health Settings");
    public static string SettingsPageSubtitle => T(nameof(SettingsPageSubtitle), "Configure monitoring thresholds and notifications for battery health.");
    public static string OverviewTitle => T(nameof(OverviewTitle), "Battery Health Overview");
    public static string OverviewDescription => T(nameof(OverviewDescription), "Real-time battery diagnostics powered by WMI.");
    public static string HealthPercentLabel => T(nameof(HealthPercentLabel), "Health");
    public static string CycleCountLabel => T(nameof(CycleCountLabel), "Cycle Count");
    public static string ChargeRemainingLabel => T(nameof(ChargeRemainingLabel), "Charge Remaining");
    public static string WearPercentLabel => T(nameof(WearPercentLabel), "Wear");
    public static string DesignedCapacityLabel => T(nameof(DesignedCapacityLabel), "Design Capacity");
    public static string FullChargeCapacityLabel => T(nameof(FullChargeCapacityLabel), "Full Charge Capacity");
    public static string CapacityUnitMwh => T(nameof(CapacityUnitMwh), "mWh");
    public static string HealthStateLabel => T(nameof(HealthStateLabel), "Status");
    public static string StatusHealthy => T(nameof(StatusHealthy), "Healthy");
    public static string StatusWarning => T(nameof(StatusWarning), "Warning");
    public static string StatusCritical => T(nameof(StatusCritical), "Critical");
    public static string StatusNoBattery => T(nameof(StatusNoBattery), "No Battery");
    public static string StatusUnknown => T(nameof(StatusUnknown), "Unknown");
    public static string NoBatteryMessage => T(nameof(NoBatteryMessage), "No battery detected on this device.");
    public static string LoadingMessage => T(nameof(LoadingMessage), "Loading battery data...");
    public static string RefreshFailedMessage => T(nameof(RefreshFailedMessage), "Failed to read battery data.");
    public static string RefreshButton => T(nameof(RefreshButton), "Refresh");
    public static string MonitoringCardTitle => T(nameof(MonitoringCardTitle), "Monitoring");
    public static string MonitoringCardDescription => T(nameof(MonitoringCardDescription), "Enable real-time battery health monitoring.");
    public static string EnableMonitoringLabel => T(nameof(EnableMonitoringLabel), "Enable real-time monitoring");
    public static string ThresholdsCardTitle => T(nameof(ThresholdsCardTitle), "Health Thresholds");
    public static string LowHealthThresholdLabel => T(nameof(LowHealthThresholdLabel), "Low health threshold");
    public static string CriticalHealthThresholdLabel => T(nameof(CriticalHealthThresholdLabel), "Critical health threshold");
    public static string NotificationsCardTitle => T(nameof(NotificationsCardTitle), "Notifications");
    public static string NotificationsCardDescription => T(nameof(NotificationsCardDescription), "Show a notification when battery health drops below a threshold.");
    public static string EnableNotificationLabel => T(nameof(EnableNotificationLabel), "Enable notifications");
    public static string SaveButton => T(nameof(SaveButton), "Save");
    public static string ReloadButton => T(nameof(ReloadButton), "Reload");
    public static string SettingsSaved => T(nameof(SettingsSaved), "Settings saved.");
    public static string SettingsReloaded => T(nameof(SettingsReloaded), "Settings reloaded.");
    public static string SettingsInvalidThresholds => T(nameof(SettingsInvalidThresholds), "Critical threshold must be lower than the low threshold.");

    public static string FormatHealthPercent(int value)
    {
        var format = T(nameof(FormatHealthPercent), "{0}%");
        return string.Format(Culture, format, value);
    }

    public static string FormatWearPercent(int value)
    {
        var format = T(nameof(FormatWearPercent), "{0}%");
        return string.Format(Culture, format, value);
    }

    public static string FormatChargeRemaining(int value)
    {
        var format = T(nameof(FormatChargeRemaining), "{0}%");
        return string.Format(Culture, format, value);
    }

    public static string FormatCapacityMWh(long value)
    {
        var format = T(nameof(FormatCapacityMWh), "{0} mWh");
        return string.Format(Culture, format, value);
    }

    public static string FormatCycleCount(int value)
    {
        var format = T(nameof(FormatCycleCount), "{0} cycles");
        return string.Format(Culture, format, value);
    }

    public static string FormatThresholdValue(int value)
    {
        var format = T(nameof(FormatThresholdValue), "{0}%");
        return string.Format(Culture, format, value);
    }

    public static string GetStatusText(BatteryHealthStatus status) => status switch
    {
        BatteryHealthStatus.Healthy => StatusHealthy,
        BatteryHealthStatus.Warning => StatusWarning,
        BatteryHealthStatus.Critical => StatusCritical,
        BatteryHealthStatus.NoBattery => StatusNoBattery,
        _ => StatusUnknown
    };

    private static readonly System.Resources.ResourceManager ResourceManager =
        new("LenovoLegionToolkit.Plugins.BatteryHealth.Resources.Resource", typeof(BatteryHealthText).Assembly);

    private static string T(string key, string fallback)
    {
        return ResourceManager.GetString(key, Culture) ?? fallback;
    }
}
