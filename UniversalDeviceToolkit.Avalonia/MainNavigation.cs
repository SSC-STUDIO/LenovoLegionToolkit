namespace UniversalDeviceToolkit.Avalonia;

/// <summary>
/// Routes exposed by the Avalonia shell. Keeping the route names in one place
/// lets automation and tray commands navigate without depending on control names.
/// </summary>
public static class MainNavigation
{
    public const string Dashboard = "dashboard";
    public const string Keyboard = "keyboardbacklight";
    public const string Actions = "automation";
    public const string Macro = "macro";
    public const string WindowsOptimization = "windowsoptimization";
    public const string PluginExtensions = "pluginextensions";
    public const string About = "about";
    public const string Settings = "settings";

    public static bool IsKnown(string? route) => route switch
    {
        Dashboard or Keyboard or Actions or Macro or WindowsOptimization or PluginExtensions or About or Settings => true,
        _ => false,
    };
}
