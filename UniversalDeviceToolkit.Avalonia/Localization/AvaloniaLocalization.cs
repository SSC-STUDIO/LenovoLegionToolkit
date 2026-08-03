using System.Resources;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.Avalonia.Localization;

internal static class AvaloniaLocalization
{
    private static readonly ResourceManagerStringLocalizer Localizer = new(
        new ResourceManager(
            "UniversalDeviceToolkit.Avalonia.Resources.Resource",
            typeof(AvaloniaLocalization).Assembly));

    public static IStringLocalizer StringLocalizer => Localizer;

    public static void ApplyCulture(System.Globalization.CultureInfo culture) =>
        Localizer.CurrentCulture = culture;

    public static string GetString(string key, string fallback = "") => Localizer.GetString(key, fallback);
}
