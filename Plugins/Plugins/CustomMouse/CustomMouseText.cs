using System;
using System.Globalization;
#nullable enable


namespace UniversalDeviceToolkit.Plugins.CustomMouse;

public static class CustomMouseText
{
    public static CultureInfo Culture => Resources.Resource.Culture ?? CultureInfo.CurrentUICulture;

    public static string PluginName => Resources.Resource.PluginName;
    public static string PluginDescription => T(nameof(PluginDescription), "Customize mouse cursor style behavior and mouse settings.");
    public static string SettingsPageTitle => "Cursor & Pointer";
    public static string ProfileStatusLabel => T(nameof(ProfileStatusLabel), "Profile State");
    public static string ProfileReady => T(nameof(ProfileReady), "Ready to apply");
    public static string ApplyButton => T(nameof(ApplyButton), "Apply");
    public static string ResetButton => T(nameof(ResetButton), "Reset");
    public static string QuickActionsTitle => T(nameof(QuickActionsTitle), "Quick Actions");
    public static string QuickActionsDescription => T(nameof(QuickActionsDescription), "Apply or reset your mouse profile settings.");
    public static string StatusResetDefaults => T(nameof(StatusResetDefaults), "Mouse profile reset to defaults.");
    public static string StatusProfileSaved => T(nameof(StatusProfileSaved), "Mouse profile saved.");
    public static string SettingsSubtitle => T(nameof(SettingsSubtitle), "Apply pointer speed and button layout to the active Windows profile.");
    public static string SettingsOverviewTitle => T(nameof(SettingsOverviewTitle), "Current Windows Mouse State");
    public static string WindowsSettingsTitle => T(nameof(WindowsSettingsTitle), "Windows Mouse Settings");
    public static string WindowsSettingsDescription => T(nameof(WindowsSettingsDescription), "Preview the effective pointer behavior before writing it into the current Windows profile.");
    public static string PointerSpeedLabel => T(nameof(PointerSpeedLabel), "Windows Pointer Speed");
    public static string SwapButtonsLabel => T(nameof(SwapButtonsLabel), "Swap left and right mouse buttons");
    public static string AutoThemeLabel => T(nameof(AutoThemeLabel), "Auto-apply custom cursor style by current Windows light/dark theme");
    public static string PointerPreviewLabel => T(nameof(PointerPreviewLabel), "Pointer Speed");
    public static string ButtonLayoutLabel => T(nameof(ButtonLayoutLabel), "Button Layout");
    public static string CursorThemeStatusLabel => T(nameof(CursorThemeStatusLabel), "Cursor Theme");
    public static string EnabledState => T(nameof(EnabledState), "Enabled");
    public static string DisabledState => T(nameof(DisabledState), "Disabled");
    public static string StandardButtonsState => T(nameof(StandardButtonsState), "Standard");
    public static string SwappedButtonsState => T(nameof(SwappedButtonsState), "Swapped");
    public static string AutomaticThemeState => T(nameof(AutomaticThemeState), "Automatic");
    public static string ManualThemeState => T(nameof(ManualThemeState), "Manual");
    public static string CursorHint => T(nameof(CursorHint), "Cursor appearance can be applied from this page or from System Optimization extension actions.");
    public static string ApplyToWindowsButton => T(nameof(ApplyToWindowsButton), "Apply to Windows");
    public static string ApplyCursorThemeNowButton => T(nameof(ApplyCursorThemeNowButton), "Apply Cursor Theme Now");
    public static string SyncFromWindowsButton => T(nameof(SyncFromWindowsButton), "Sync from Windows");
    public static string RestoreWindowsDefaultButton => T(nameof(RestoreWindowsDefaultButton), "Restore Windows Default");
    public static string ReloadButton => T(nameof(ReloadButton), "Reload");
    public static string StatusApplyPointerFail => T(nameof(StatusApplyPointerFail), "Failed to apply pointer speed.");
    public static string StatusApplySwapFail => T(nameof(StatusApplySwapFail), "Failed to apply button swap setting.");
    public static string StatusWindowsApplied => T(nameof(StatusWindowsApplied), "Windows mouse settings applied.");
    public static string StatusCursorApplyFailed => T(nameof(StatusCursorApplyFailed), "Failed to apply custom cursor style. Try running as administrator if your system blocks INF installation.");
    public static string StatusReloaded => T(nameof(StatusReloaded), "Current plugin settings reloaded.");
    public static string StatusSyncedFromWindows => T(nameof(StatusSyncedFromWindows), "Current Windows mouse state synced.");
    public static string StatusWindowsDefaultRestored => T(nameof(StatusWindowsDefaultRestored), "Windows default cursor scheme restored.");
    public static string StatusRestoreWindowsDefaultFailed => T(nameof(StatusRestoreWindowsDefaultFailed), "Failed to restore the Windows default cursor scheme.");

    public static string CursorThemeModeLabel => T(nameof(CursorThemeModeLabel), "Cursor Theme Mode");
    public static string CursorThemeModeAuto => T(nameof(CursorThemeModeAuto), "Follow System");
    public static string CursorThemeModeLight => T(nameof(CursorThemeModeLight), "Light");
    public static string CursorThemeModeDark => T(nameof(CursorThemeModeDark), "Dark");
    public static string CursorThemeModeWindowsDefault => T(nameof(CursorThemeModeWindowsDefault), "Windows Default");
    public static string CursorThemeModeApplied => T(nameof(CursorThemeModeApplied), "Cursor theme mode set to: {0}.");

    public static string FormatCursorApplied(string? theme)
    {
        var format = T(nameof(FormatCursorApplied), "Cursor style applied ({0}).");
        return string.Format(Culture, format, theme ?? "unknown");
    }

    private static readonly System.Resources.ResourceManager ResourceManager =
        new("UniversalDeviceToolkit.Plugins.CustomMouse.Resources.Resource", typeof(CustomMouseText).Assembly);

    private static string T(string key, string fallback)
    {
        return ResourceManager.GetString(key, Culture) ?? fallback;
    }

}
