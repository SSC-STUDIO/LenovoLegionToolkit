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
        "Rebuild the right-click menu experience with managed Shell controls, motion tuning, and a live preview.",
        "使用托管的 Shell 控件、动效调节和实时预览重新打造右键菜单体验。",
        "使用受管的 Shell 控制、動效調整與即時預覽重新打造右鍵選單體驗。");

    public static string EnableButton => T("Enable", "启用", "啟用");
    public static string DisableButton => T("Disable", "禁用", "停用");
    public static string OpenStyleSettingsButton => T("Open Config Folder", "打开配置目录", "開啟設定資料夾");
    public static string OpenStyleShortButton => T("Open Folder", "打开目录", "開啟資料夾");
    public static string RefreshButton => T("Refresh", "刷新", "重新整理");
    public static string ResetButton => T("Reset Defaults", "恢复默认", "還原預設");
    public static string ApplyButton => T("Apply Changes", "应用更改", "套用變更");
    public static string OptimizationHint => T(
        "You can also access shell actions from Windows Optimization.",
        "你也可以在系统优化页面中使用 Shell 动作。",
        "你也可以在系統最佳化頁面使用 Shell 動作。");

    public static string StatusDetected => T("Nilesoft Shell detected.", "已检测到 Nilesoft Shell。", "已偵測到 Nilesoft Shell。");
    public static string StatusNotDetected => T("Nilesoft Shell was not detected.", "未检测到 Nilesoft Shell。", "未偵測到 Nilesoft Shell。");
    public static string StatusRegistered => T("The menu enhancement is active.", "右键菜单增强当前已启用。", "右鍵選單增強目前已啟用。");
    public static string StatusUnregistered => T("The menu enhancement is currently disabled.", "右键菜单增强当前已关闭。", "右鍵選單增強目前已關閉。");
    public static string StatusDetailDefault => T("The plugin now manages its own Shell profile and generated configuration files.", "插件现在会自行管理 Shell 配置档和生成的配置文件。", "外掛現在會自行管理 Shell 設定檔與產生的設定檔。");
    public static string StatusEnabledBadge => T("Enabled", "已启用", "已啟用");
    public static string StatusDisabledBadge => T("Disabled", "已关闭", "已關閉");
    public static string StatusMissingBadge => T("Missing", "未安装", "未安裝");
    public static string StatusUnknownBadge => T("Checking", "检查中", "檢查中");
    public static string PathLabel => T("Path", "路径", "路徑");
    public static string NotFound => T("Not found", "未找到", "未找到");
    public static string ManagedConfigLabel => T("Managed config", "托管配置", "受管設定");
    public static string ManagedConfigNotReady => T("Managed config will be created after Shell is detected.", "检测到 Shell 后会创建托管配置。", "偵測到 Shell 後會建立受管設定。");

    public static string StatusEnableCompleted => T("Enable command completed.", "启用命令已完成。", "啟用命令已完成。");
    public static string StatusEnableFailed => T("Enable command failed.", "启用命令失败。", "啟用命令失敗。");
    public static string StatusDisableCompleted => T("Disable command completed.", "禁用命令已完成。", "停用命令已完成。");
    public static string StatusDisableFailed => T("Disable command failed.", "禁用命令失败。", "停用命令失敗。");
    public static string StatusOpenedStyleSettings => T("Opened the managed config folder.", "已打开托管配置目录。", "已開啟受管設定資料夾。");
    public static string ProfileSavedShellMissing => T("Profile saved, but Shell was not found on this system.", "配置已保存，但当前系统未找到 Shell。", "設定已儲存，但目前系統未找到 Shell。");
    public static string ApplyCompletedEnabled => T("Profile applied and Shell enhancement enabled.", "配置已应用，右键菜单增强已启用。", "設定已套用，右鍵選單增強已啟用。");
    public static string ApplyCompletedDisabled => T("Profile applied and Shell enhancement disabled.", "配置已应用，右键菜单增强已关闭。", "設定已套用，右鍵選單增強已關閉。");
    public static string FallbackLoadError => T("The enhanced settings UI could not be loaded.", "增强设置界面未能加载。", "增強設定介面未能載入。");

    public static string AppearanceSection => T("Behavior", "行为", "行為");
    public static string AppearanceSectionHint => T("Tune delay, motion, and overall rendering style before writing the managed config.", "在写入托管配置之前，先调节延迟、动效和整体渲染风格。", "在寫入受管設定前，先調整延遲、動效與整體渲染風格。");
    public static string PaletteSection => T("Palette", "配色", "配色");
    public static string PaletteSectionHint => T("Adjust the menu palette directly in the plugin and keep the generated theme consistent.", "直接在插件中调整菜单配色，并保持生成主题一致。", "直接在外掛中調整選單配色，並保持產生主題一致。");
    public static string PreviewSection => T("Live Preview", "实时预览", "即時預覽");
    public static string PreviewHint => T("This preview mirrors the generated Shell theme so you can tune it before applying.", "这个预览会模拟生成的 Shell 主题，方便你在应用前调整。", "這個預覽會模擬產生的 Shell 主題，方便你在套用前調整。");
    public static string PathsSection => T("Config Status", "配置状态", "設定狀態");

    public static string MotionToggleLabel => T("Enable motion accents", "启用动效点缀", "啟用動效點綴");
    public static string ShadowToggleLabel => T("Enable shadow depth", "启用阴影层次", "啟用陰影層次");
    public static string ColorSchemeLabel => T("Color scheme", "配色模式", "配色模式");
    public static string ColorSchemeAuto => T("Follow system", "跟随系统", "跟隨系統");
    public static string ColorSchemeLight => T("Always light", "始终浅色", "永遠淺色");
    public static string ColorSchemeDark => T("Always dark", "始终深色", "永遠深色");
    public static string VisualEffectLabel => T("Backdrop effect", "背景效果", "背景效果");
    public static string EffectNone => T("None", "无", "無");
    public static string EffectTransparent => T("Transparent", "透明", "透明");
    public static string EffectBlur => T("Blur", "模糊", "模糊");
    public static string EffectAcrylic => T("Acrylic", "亚克力", "壓克力");
    public static string ShowDelayLabel => T("Submenu show delay", "子菜单显示延迟", "子選單顯示延遲");
    public static string ShowDelayValueFormat => T("{0} ms", "{0} 毫秒", "{0} 毫秒");
    public static string ShadowStrengthLabel => T("Shadow opacity", "阴影透明度", "陰影透明度");
    public static string ShadowStrengthValueFormat => T("{0}%", "{0}%", "{0}%");
    public static string AccentColorLabel => T("Accent color", "强调色", "強調色");
    public static string BackgroundColorLabel => T("Background color", "背景色", "背景色");
    public static string HoverColorLabel => T("Hover color", "悬停色", "懸停色");
    public static string TextColorLabel => T("Text color", "文字颜色", "文字顏色");
    public static string MutedColorLabel => T("Muted text color", "弱化文字颜色", "弱化文字顏色");
    public static string TintColorLabel => T("Effect tint color", "效果染色色", "效果染色色");

    public static string PreviewPrimary => T("Performance Mode", "性能模式", "效能模式");
    public static string PreviewPrimaryHint => T("Pinned quick action with the active accent.", "带有当前强调色的固定快捷操作。", "帶有目前強調色的固定快捷操作。");
    public static string PreviewSecondary => T("Hybrid Graphics", "混合显卡", "混合顯卡");
    public static string PreviewSecondaryHint => T("Hover state for secondary menu actions.", "二级菜单动作的悬停状态。", "次級選單動作的懸停狀態。");
    public static string PreviewTertiary => T("Open Toolkit Dashboard", "打开工具箱面板", "開啟工具箱面板");
    public static string PreviewTertiaryHint => T("Neutral item using the generated background palette.", "使用生成背景配色的普通菜单项。", "使用產生背景配色的一般選單項。");

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
