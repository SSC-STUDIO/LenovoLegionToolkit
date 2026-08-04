using PluginTooling.Core;

namespace PluginWorkbench;

public sealed record PluginWorkbenchLaunchOptions(
    string? RepositoryRoot,
    string? PluginId,
    PluginWorkbenchThemeMode? ThemeMode,
    PluginWorkbenchView? InitialView,
    bool AutoAcceptRuntimeConfirmation)
{
    public static PluginWorkbenchLaunchOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string? repositoryRoot = null;
        string? pluginId = null;
        PluginWorkbenchThemeMode? themeMode = null;
        PluginWorkbenchView? view = null;
        var autoAcceptRuntimeConfirmation =
            string.Equals(
                Environment.GetEnvironmentVariable("LLT_PLUGIN_WORKBENCH_AUTO_ACCEPT_RUNTIME_CONFIRMATION"),
                "1",
                StringComparison.OrdinalIgnoreCase);

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--repository-root" when i + 1 < args.Length:
                    repositoryRoot = args[++i];
                    break;
                case "--plugin-id" when i + 1 < args.Length:
                    pluginId = args[++i];
                    break;
                case "--theme" when i + 1 < args.Length:
                    themeMode = args[++i].ToLowerInvariant() switch
                    {
                        "light" => PluginWorkbenchThemeMode.Light,
                        "dark" => PluginWorkbenchThemeMode.Dark,
                        _ => PluginWorkbenchThemeMode.System,
                    };
                    break;
                case "--view" when i + 1 < args.Length:
                    view = args[++i].ToLowerInvariant() switch
                    {
                        "settings" => PluginWorkbenchView.Settings,
                        "optimization" => PluginWorkbenchView.Optimization,
                        _ => PluginWorkbenchView.Feature,
                    };
                    break;
                case "--auto-accept-runtime-confirmation":
                    autoAcceptRuntimeConfirmation = true;
                    break;
            }
        }

        return new PluginWorkbenchLaunchOptions(repositoryRoot, pluginId, themeMode, view, autoAcceptRuntimeConfirmation);
    }
}
