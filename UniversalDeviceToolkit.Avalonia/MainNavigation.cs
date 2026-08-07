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
    public const string PluginRoutePrefix = "plugin:";
    public const string PluginSettingsRoutePrefix = "plugin-settings:";
    public const string About = "about";
    public const string Settings = "settings";

    public static string CreatePluginRoute(string pluginId) =>
        string.IsNullOrWhiteSpace(pluginId)
            ? throw new ArgumentException("A plugin ID is required.", nameof(pluginId))
            : PluginRoutePrefix + pluginId.Trim();

    public static string CreatePluginSettingsRoute(string pluginId) =>
        string.IsNullOrWhiteSpace(pluginId)
            ? throw new ArgumentException("A plugin ID is required.", nameof(pluginId))
            : PluginSettingsRoutePrefix + pluginId.Trim();

    public static bool TryGetPluginId(string? route, out string pluginId)
    {
        pluginId = string.Empty;
        if (string.IsNullOrWhiteSpace(route)
            || !route.StartsWith(PluginRoutePrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var candidate = route[PluginRoutePrefix.Length..].Trim();
        if (candidate.Length == 0)
            return false;

        pluginId = candidate;
        return true;
    }

    public static bool TryGetPluginSettingsId(string? route, out string pluginId)
    {
        pluginId = string.Empty;
        if (string.IsNullOrWhiteSpace(route)
            || !route.StartsWith(PluginSettingsRoutePrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var candidate = route[PluginSettingsRoutePrefix.Length..].Trim();
        if (candidate.Length == 0)
            return false;

        pluginId = candidate;
        return true;
    }

    public static bool IsKnown(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
            return false;

        var normalized = route.Trim();
        if (TryGetPluginId(normalized, out _) || TryGetPluginSettingsId(normalized, out _))
            return true;

        return normalized.Equals(Dashboard, StringComparison.OrdinalIgnoreCase)
            || normalized.Equals(Keyboard, StringComparison.OrdinalIgnoreCase)
            || normalized.Equals(Actions, StringComparison.OrdinalIgnoreCase)
            || normalized.Equals(Macro, StringComparison.OrdinalIgnoreCase)
            || normalized.Equals(WindowsOptimization, StringComparison.OrdinalIgnoreCase)
            || normalized.Equals(PluginExtensions, StringComparison.OrdinalIgnoreCase)
            || normalized.Equals(About, StringComparison.OrdinalIgnoreCase)
            || normalized.Equals(Settings, StringComparison.OrdinalIgnoreCase);
    }
}
