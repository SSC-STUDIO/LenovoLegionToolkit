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
    public static string PreferredModeTitle => T(nameof(PreferredModeTitle), "Preferred Mode");
    public static string SaveModeButton => T(nameof(SaveModeButton), "Save Mode");
    public static string ModeBalanced => T(nameof(ModeBalanced), "Balanced");
    public static string ModeGaming => T(nameof(ModeGaming), "Gaming");
    public static string ModeStreaming => T(nameof(ModeStreaming), "Streaming");
    public static string StatusQuickOptimizationCompleted => T(nameof(StatusQuickOptimizationCompleted), "Quick optimization completed.");
    public static string StatusQuickOptimizationFailed => T(nameof(StatusQuickOptimizationFailed), "Quick optimization failed. Please run as administrator.");
    public static string StatusResetCompleted => T(nameof(StatusResetCompleted), "Network stack reset completed.");
    public static string StatusResetFailed => T(nameof(StatusResetFailed), "Network stack reset failed. Please run as administrator.");
    public static string StatusSelectValidMode => T(nameof(StatusSelectValidMode), "Select a valid mode.");
    public static string StatusModeSaved => T(nameof(StatusModeSaved), "Preferred mode saved.");
    public static string SettingsTitle => T(nameof(SettingsTitle), "Quick Optimization Behavior");
    public static string SettingsDescription => T(nameof(SettingsDescription), "Choose which recovery actions will be included when quick optimization runs.");
    public static string AutoOptimizeOnStartup => T(nameof(AutoOptimizeOnStartup), "Auto optimize on startup");
    public static string ResetWinsockOnOptimize => T(nameof(ResetWinsockOnOptimize), "Reset Winsock during quick optimization");
    public static string ResetTcpIpOnOptimize => T(nameof(ResetTcpIpOnOptimize), "Reset TCP/IP stack during quick optimization");
    public static string SaveSettingsButton => T(nameof(SaveSettingsButton), "Save Settings");
    public static string SettingsSaved => T(nameof(SettingsSaved), "Network acceleration settings saved.");

    private static readonly System.Resources.ResourceManager ResourceManager =
        new("LenovoLegionToolkit.Plugins.NetworkAcceleration.Resources.Resource", typeof(NetworkAccelerationText).Assembly);

    private static string T(string key, string fallback)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;
    }

}