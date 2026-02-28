using System;
using System.Globalization;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

public static class ShellIntegrationText
{
    public static string PluginName => T("Shell Integration", "Shell 集成", "Shell 整合");
    public static string PluginDescription => T(
        "Integrate Lenovo Legion Toolkit with Windows shell context menu.",
        "将 Lenovo Legion Toolkit 集成到 Windows 右键菜单。",
        "將 Lenovo Legion Toolkit 整合到 Windows 右鍵選單。");

    public static string SettingsPageTitle => T("Shell Integration", "Shell 集成", "Shell 整合");

    public static string Subtitle => T(
        "Manage Nilesoft Shell registration and open style editor.",
        "管理 Nilesoft Shell 注册状态并打开样式编辑器。",
        "管理 Nilesoft Shell 註冊狀態並開啟樣式編輯器。");

    public static string EnableButton => T("Enable", "启用", "啟用");
    public static string DisableButton => T("Disable", "禁用", "停用");
    public static string OpenStyleSettingsButton => T("Open Style Settings", "打开样式设置", "開啟樣式設定");
    public static string OpenStyleShortButton => T("Open Style", "打开样式", "開啟樣式");
    public static string OptimizationHint => T(
        "You can also access shell actions from Windows Optimization.",
        "你也可以在系统优化页面中使用 Shell 动作。",
        "你也可以在系統最佳化頁面使用 Shell 動作。");

    public static string StatusDetected => T("Nilesoft Shell detected.", "已检测到 Nilesoft Shell。", "已偵測到 Nilesoft Shell。");
    public static string StatusNotDetected => T("Nilesoft Shell was not detected.", "未检测到 Nilesoft Shell。", "未偵測到 Nilesoft Shell。");
    public static string PathLabel => T("Path", "路径", "路徑");
    public static string NotFound => T("Not found", "未找到", "未找到");

    public static string StatusEnableCompleted => T("Enable command completed.", "启用命令已完成。", "啟用命令已完成。");
    public static string StatusEnableFailed => T("Enable command failed.", "启用命令失败。", "啟用命令失敗。");
    public static string StatusDisableCompleted => T("Disable command completed.", "禁用命令已完成。", "停用命令已完成。");
    public static string StatusDisableFailed => T("Disable command failed.", "禁用命令失败。", "停用命令失敗。");
    public static string StatusOpenedStyleSettings => T("Opened style settings.", "已打开样式设置。", "已開啟樣式設定。");

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
