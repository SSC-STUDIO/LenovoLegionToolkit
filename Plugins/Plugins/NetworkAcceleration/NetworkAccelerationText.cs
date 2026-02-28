using System;
using System.Globalization;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

public static class NetworkAccelerationText
{
    public static string PluginName => T("Network Acceleration", "网络加速", "網路加速");
    public static string PluginDescription => T(
        "Real-time network acceleration and optimization features.",
        "实时网络加速与优化功能。",
        "即時網路加速與最佳化功能。");

    public static string PageTitle => T("Network Acceleration", "网络加速", "網路加速");
    public static string SettingsPageTitle => T("Network Settings", "网络设置", "網路設定");

    public static string QuickActionsTitle => T("Quick Network Actions", "快速网络操作", "快速網路操作");
    public static string QuickActionsDescription => T(
        "Run quick optimization commands for lower latency and cleaner socket state.",
        "执行快速优化命令，以降低延迟并清理网络套接字状态。",
        "執行快速最佳化命令，以降低延遲並清理網路 Socket 狀態。");

    public static string RunQuickOptimizationButton => T("Run Quick Optimization", "执行快速优化", "執行快速最佳化");
    public static string ResetNetworkStackButton => T("Reset Network Stack", "重置网络协议栈", "重設網路堆疊");
    public static string AdminHint => T(
        "Some operations may require administrator permission.",
        "部分操作可能需要管理员权限。",
        "部分操作可能需要系統管理員權限。");

    public static string PreferredModeTitle => T("Preferred Mode", "首选模式", "偏好模式");
    public static string SaveModeButton => T("Save Mode", "保存模式", "儲存模式");
    public static string ModeBalanced => T("Balanced", "均衡", "平衡");
    public static string ModeGaming => T("Gaming", "游戏", "遊戲");
    public static string ModeStreaming => T("Streaming", "串流", "串流");

    public static string StatusQuickOptimizationCompleted => T("Quick optimization completed.", "快速优化已完成。", "快速最佳化已完成。");
    public static string StatusQuickOptimizationFailed => T("Quick optimization failed. Please run as administrator.", "快速优化失败。请以管理员身份运行。", "快速最佳化失敗。請以系統管理員身分執行。");
    public static string StatusResetCompleted => T("Network stack reset completed.", "网络协议栈重置已完成。", "網路堆疊重設已完成。");
    public static string StatusResetFailed => T("Network stack reset failed. Please run as administrator.", "网络协议栈重置失败。请以管理员身份运行。", "網路堆疊重設失敗。請以系統管理員身分執行。");
    public static string StatusSelectValidMode => T("Select a valid mode.", "请选择有效模式。", "請選擇有效模式。");
    public static string StatusModeSaved => T("Preferred mode saved.", "首选模式已保存。", "偏好模式已儲存。");

    public static string SettingsTitle => T("Quick Optimization Behavior", "快速优化行为", "快速最佳化行為");
    public static string SettingsDescription => T(
        "Choose which recovery actions will be included when quick optimization runs.",
        "选择快速优化运行时要包含的恢复动作。",
        "選擇快速最佳化執行時要包含的修復動作。");

    public static string AutoOptimizeOnStartup => T("Auto optimize on startup", "启动时自动优化", "啟動時自動最佳化");
    public static string ResetWinsockOnOptimize => T("Reset Winsock during quick optimization", "快速优化时重置 Winsock", "快速最佳化時重設 Winsock");
    public static string ResetTcpIpOnOptimize => T("Reset TCP/IP stack during quick optimization", "快速优化时重置 TCP/IP 协议栈", "快速最佳化時重設 TCP/IP 堆疊");
    public static string SaveSettingsButton => T("Save Settings", "保存设置", "儲存設定");
    public static string SettingsSaved => T("Network acceleration settings saved.", "网络加速设置已保存。", "網路加速設定已儲存。");

    private static string T(string en, string zhHans, string zhHant)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        if (culture.StartsWith("zh-hans", StringComparison.OrdinalIgnoreCase) ||
            culture.Equals("zh-cn", StringComparison.OrdinalIgnoreCase) ||
            culture.Equals("zh-sg", StringComparison.OrdinalIgnoreCase))
        {
            return zhHans;
        }

        if (culture.StartsWith("zh-hant", StringComparison.OrdinalIgnoreCase) ||
            culture.Equals("zh-tw", StringComparison.OrdinalIgnoreCase) ||
            culture.Equals("zh-hk", StringComparison.OrdinalIgnoreCase) ||
            culture.Equals("zh-mo", StringComparison.OrdinalIgnoreCase))
        {
            return zhHant;
        }

        return en;
    }
}
