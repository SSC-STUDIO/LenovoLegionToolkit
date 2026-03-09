using System.Globalization;
using LenovoLegionToolkit.Plugins.CustomMouse.Resources;

namespace LenovoLegionToolkit.Plugins.CustomMouse;

public static class CustomMouseText
{
    public static string PluginName => Resource.PluginName;
    public static string PluginDescription => Resource.PluginDescription;
    public static string PageTitle => Resource.PageTitle;
    public static string SettingsPageTitle => Resource.SettingsPageTitle;
    public static string FeatureSubtitle => Resource.FeatureSubtitle;
    public static string DpiLabel => Resource.DpiLabel;
    public static string PollingRateLabel => Resource.PollingRateLabel;
    public static string ApplyButton => Resource.ApplyButton;
    public static string ResetButton => Resource.ResetButton;
    public static string StatusResetDefaults => Resource.StatusResetDefaults;
    public static string StatusInvalidDpi => Resource.StatusInvalidDpi;
    public static string StatusSelectValidPolling => Resource.StatusSelectValidPolling;
    public static string StatusInvalidPolling => Resource.StatusInvalidPolling;
    public static string StatusProfileSaved => Resource.StatusProfileSaved;
    public static string SettingsSubtitle => Resource.SettingsSubtitle;
    public static string PointerSpeedLabel => Resource.PointerSpeedLabel;
    public static string SwapButtonsLabel => Resource.SwapButtonsLabel;
    public static string AutoThemeLabel => Resource.AutoThemeLabel;
    public static string CursorHint => Resource.CursorHint;
    public static string ApplyToWindowsButton => Resource.ApplyToWindowsButton;
    public static string ApplyCursorThemeNowButton => Resource.ApplyCursorThemeNowButton;
    public static string ReloadButton => Resource.ReloadButton;
    public static string StatusApplyPointerFail => Resource.StatusApplyPointerFail;
    public static string StatusApplySwapFail => Resource.StatusApplySwapFail;
    public static string StatusWindowsApplied => Resource.StatusWindowsApplied;
    public static string StatusCursorApplyFailed => Resource.StatusCursorApplyFailed;
    public static string StatusReloaded => Resource.StatusReloaded;

    public static string FormatCursorApplied(string? theme)
    {
        var themeText = string.IsNullOrWhiteSpace(theme) ? Resource.CursorAppliedUnknownTheme : theme;
        return string.Format(CultureInfo.CurrentUICulture, Resource.CursorAppliedFormat, themeText);
    }
}
