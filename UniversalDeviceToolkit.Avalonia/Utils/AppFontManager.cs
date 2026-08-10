using Avalonia.Media;
using UniversalDeviceToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Applies the user-selected app font family by swapping the AppFontFamily resource
/// (referenced as DynamicResource by the typography styles). Missing families in a
/// chain are skipped by Avalonia at render time, so fallback is graceful.
/// </summary>
public static class AppFontManager
{
    public static void Apply(AppFontStyle style)
    {
        var fontFamily = FontFamily.Parse(GetChain(style));

        Application.Current.Resources["AppFontFamily"] = fontFamily;
    }

    public static void ApplySaved(ApplicationSettings settings) => Apply(settings.Store.AppFontStyle);

    private static string GetChain(AppFontStyle style) => style switch
    {
        AppFontStyle.FluentVariable => "Segoe UI Variable Text, Segoe UI, Microsoft YaHei UI, Microsoft YaHei",
        AppFontStyle.YaHeiUI => "Microsoft YaHei UI, Segoe UI, Microsoft YaHei",
        AppFontStyle.DengXian => "DengXian, Segoe UI, Microsoft YaHei UI",
        AppFontStyle.NotoSans => "Noto Sans CJK SC, Source Han Sans SC, Segoe UI, Microsoft YaHei UI",
        // Windows-builtin CJK faces so every option renders Chinese visibly differently.
        AppFontStyle.SimHei => "SimHei, Microsoft YaHei UI, Segoe UI",
        AppFontStyle.SimSun => "SimSun, NSimSun, Microsoft YaHei UI, Segoe UI",
        AppFontStyle.KaiTi => "KaiTi, Microsoft YaHei UI, Segoe UI",
        _ => "Segoe UI, Microsoft YaHei UI, Microsoft YaHei, Noto Sans CJK SC, SimSun"
    };
}
