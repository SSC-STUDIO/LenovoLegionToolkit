using System.Windows;
using System.Windows.Media;
using UniversalDeviceToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Applies the user-selected app font family by swapping the AppFontFamily resource
/// (referenced as DynamicResource by the typography styles) and the WPF system
/// MessageFontFamilyKey entry that WPF-UI internals bind to. Missing families in a
/// chain are skipped by WPF at render time, so fallback is graceful.
/// </summary>
public static class AppFontManager
{
    public static void Apply(AppFontStyle style)
    {
        var fontFamily = new FontFamily(GetChain(style));

        Application.Current.Resources["AppFontFamily"] = fontFamily;
        Application.Current.Resources[SystemFonts.MessageFontFamilyKey] = fontFamily;
    }

    public static void ApplySaved(ApplicationSettings settings) => Apply(settings.Store.AppFontStyle);

    private static string GetChain(AppFontStyle style) => style switch
    {
        AppFontStyle.FluentVariable => "Segoe UI Variable Text, Segoe UI, Microsoft YaHei UI, Microsoft YaHei",
        AppFontStyle.YaHeiUI => "Microsoft YaHei UI, Segoe UI, Microsoft YaHei",
        AppFontStyle.DengXian => "DengXian, Segoe UI, Microsoft YaHei UI",
        AppFontStyle.NotoSans => "Noto Sans CJK SC, Source Han Sans SC, Segoe UI, Microsoft YaHei UI",
        _ => "Segoe UI, Microsoft YaHei UI, Microsoft YaHei, Noto Sans CJK SC, SimSun"
    };
}
