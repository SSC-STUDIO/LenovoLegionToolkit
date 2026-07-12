using System.Globalization;
using System.Resources;

namespace UniversalDeviceToolkit.CLI;

public static class Strings
{
    private static readonly ResourceManager _manager = new(
        "UniversalDeviceToolkit.CLI.Resources.CLI.Resources",
        typeof(Strings).Assembly);

    public static string Get(string key, string fallback) =>
        _manager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;

    public static string Get(string key, string fallback, params object[] args)
    {
        var template = Get(key, fallback);
        return args.Length == 0 ? template : string.Format(CultureInfo.CurrentUICulture, template, args);
    }
}
