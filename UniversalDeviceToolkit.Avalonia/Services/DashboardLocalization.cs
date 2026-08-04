using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Services;

internal static class DashboardLocalization
{
    public static string Get(string key, string fallback) =>
        AvaloniaLocalization.GetString(key, fallback);

    public static string Format(string key, string fallback, params object[] args) =>
        string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            Get(key, fallback),
            args);
}
