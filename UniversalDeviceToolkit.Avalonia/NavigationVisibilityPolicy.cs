namespace UniversalDeviceToolkit.Avalonia;

/// <summary>
/// Defines the optional shell entries whose visibility is controlled by the
/// application's navigation settings. Dashboard and Settings are intentionally
/// omitted because they are permanent shell anchors.
/// </summary>
public static class NavigationVisibilityPolicy
{
    public static IReadOnlyList<NavigationVisibilityEntry> Entries { get; } =
    [
        new("keyboard", MainNavigation.Keyboard, "MainWindow_NavigationItem_Keyboard", "Keyboard"),
        new("automation", MainNavigation.Actions, "MainWindow_NavigationItem_Actions", "Actions"),
        new("macro", MainNavigation.Macro, "MainWindow_NavigationItem_Macro", "Macro"),
        new("windowsOptimization", MainNavigation.WindowsOptimization, "MainWindow_NavigationItem_WindowsOptimization", "System optimization"),
        new("pluginExtensions", MainNavigation.PluginExtensions, "MainWindow_NavigationItem_PluginExtensions", "Plugin Extensions"),
        new("about", MainNavigation.About, "Nav_About", "About"),
    ];

    public static bool IsVisible(
        string route,
        IReadOnlyDictionary<string, bool>? settings)
    {
        var entry = Entries.FirstOrDefault(item =>
            item.Route.Equals(route, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return true;

        return settings is null
            || !settings.TryGetValue(entry.Key, out var isVisible)
            || isVisible;
    }
}

public sealed record NavigationVisibilityEntry(
    string Key,
    string Route,
    string TitleKey,
    string TitleFallback);
