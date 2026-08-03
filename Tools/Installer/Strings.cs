using System.Globalization;
using System.Resources;
using UniversalDeviceToolkit.Abstractions.Localization;

namespace UniversalDeviceToolkit.Installer;

internal static class Strings
{
    private static readonly ResourceManagerStringLocalizer Localizer = new(
        new ResourceManager(
            "UniversalDeviceToolkit.Installer.Resources.Resource",
            typeof(Strings).Assembly));

    public static void ApplyCulture(CultureInfo culture) => Localizer.CurrentCulture = culture;

    public static string Get(string key) => Localizer.GetString(key, key);

    public static string Format(string key, params object[] args) =>
        string.Format(Localizer.CurrentCulture, Get(key), args);
}
