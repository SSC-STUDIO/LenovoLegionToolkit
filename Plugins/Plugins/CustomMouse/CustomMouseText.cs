using System;
using System.Globalization;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

public static class CustomMouseText
{
    public static string PluginName => T("Custom Mouse", "自定义鼠标", "自訂滑鼠");
    public static string PluginDescription => T(
        "Customize mouse cursor style behavior and mouse settings.",
        "自定义鼠标光标样式行为与鼠标设置。",
        "自訂滑鼠游標樣式行為與滑鼠設定。");

    public static string PageTitle => T("Custom Mouse", "自定义鼠标", "自訂滑鼠");
    public static string SettingsPageTitle => T("Mouse Settings", "鼠标设置", "滑鼠設定");

    public static string FeatureSubtitle => T(
        "Tune mouse profile values and apply them to the current Windows session.",
        "调整鼠标配置参数并应用到当前 Windows 会话。",
        "調整滑鼠設定並套用到目前 Windows 工作階段。");

    public static string DpiLabel => "DPI";
    public static string PollingRateLabel => T("Polling Rate", "回报率", "回報率");
    public static string ApplyButton => T("Apply", "应用", "套用");
    public static string ResetButton => T("Reset", "重置", "重設");

    public static string StatusResetDefaults => T("Mouse profile reset to defaults.", "鼠标配置已重置为默认值。", "滑鼠設定已重設為預設值。");
    public static string StatusInvalidDpi => T("Invalid DPI value.", "DPI 数值无效。", "DPI 數值無效。");
    public static string StatusSelectValidPolling => T("Please select a valid polling rate.", "请选择有效的回报率。", "請選擇有效的回報率。");
    public static string StatusInvalidPolling => T("Invalid polling rate.", "回报率无效。", "回報率無效。");
    public static string StatusProfileSaved => T("Mouse profile saved.", "鼠标配置已保存。", "滑鼠設定已儲存。");

    public static string SettingsSubtitle => T(
        "Apply pointer speed and button layout to the active Windows profile.",
        "将指针速度和按键布局应用到当前 Windows 配置。",
        "將指標速度與按鍵配置套用到目前 Windows 設定。");

    public static string PointerSpeedLabel => T("Windows Pointer Speed", "Windows 指针速度", "Windows 指標速度");
    public static string SwapButtonsLabel => T("Swap left and right mouse buttons", "交换鼠标左右键", "交換滑鼠左右鍵");
    public static string AutoThemeLabel => T(
        "Auto-apply custom cursor style by current Windows light/dark theme",
        "根据当前 Windows 明暗主题自动应用自定义光标样式",
        "依照目前 Windows 明暗主題自動套用自訂游標樣式");

    public static string CursorHint => T(
        "Cursor appearance can be applied from this page or from System Optimization extension actions.",
        "可以在本页或系统优化扩展动作中应用光标外观。",
        "可在本頁或系統最佳化擴充動作中套用游標外觀。");

    public static string ApplyToWindowsButton => T("Apply to Windows", "应用到 Windows", "套用到 Windows");
    public static string ApplyCursorThemeNowButton => T("Apply Cursor Theme Now", "立即应用光标主题", "立即套用游標主題");
    public static string ReloadButton => T("Reload", "重新加载", "重新載入");

    public static string StatusApplyPointerFail => T("Failed to apply pointer speed.", "应用指针速度失败。", "套用指標速度失敗。");
    public static string StatusApplySwapFail => T("Failed to apply button swap setting.", "应用按键交换设置失败。", "套用按鍵交換設定失敗。");
    public static string StatusWindowsApplied => T("Windows mouse settings applied.", "Windows 鼠标设置已应用。", "Windows 滑鼠設定已套用。");
    public static string StatusCursorApplyFailed => T(
        "Failed to apply custom cursor style. Try running as administrator if your system blocks INF installation.",
        "应用自定义光标样式失败。如果系统阻止 INF 安装，请尝试以管理员身份运行。",
        "套用自訂游標樣式失敗。若系統封鎖 INF 安裝，請嘗試以系統管理員身分執行。");

    public static string StatusReloaded => T("Current plugin settings reloaded.", "当前插件设置已重新加载。", "目前外掛設定已重新載入。");

    public static string FormatCursorApplied(string? theme)
    {
        var themeText = string.IsNullOrWhiteSpace(theme)
            ? T("unknown", "未知", "未知")
            : theme;

        return T(
            $"Custom cursor style applied for current system theme ({themeText}).",
            $"已为当前系统主题应用自定义光标样式（{themeText}）。",
            $"已為目前系統主題套用自訂游標樣式（{themeText}）。");
    }

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
