using System;
using System.Globalization;

namespace LenovoLegionToolkit.Plugins.ShellIntegration;

public static class ShellIntegrationText
{
    public static string PluginName => T(nameof(PluginName), "Shell Integration");
    public static string PluginDescription => T(nameof(PluginDescription), "Integrate Lenovo Legion Toolkit with Windows shell context menu.");
    public static string SettingsPageTitle => T(nameof(SettingsPageTitle), "Shell Integration");
    public static string Subtitle => T(nameof(Subtitle), "Manage Nilesoft Shell registration and open style editor.");
    public static string EnableButton => T(nameof(EnableButton), "Enable");
    public static string DisableButton => T(nameof(DisableButton), "Disable");
    public static string OpenStyleSettingsButton => T(nameof(OpenStyleSettingsButton), "Open Style Settings");
    public static string OpenStyleShortButton => T(nameof(OpenStyleShortButton), "Open Style");
    public static string OpenShellFolderButton => T(nameof(OpenShellFolderButton), "Open Shell Folder");
    public static string OpenConfigButton => T(nameof(OpenConfigButton), "Open Config File");
    public static string OptimizationHint => T(nameof(OptimizationHint), "You can also access shell actions from Windows Optimization.");
    public static string StatusDetected => T(nameof(StatusDetected), "Nilesoft Shell detected.");
    public static string StatusNotDetected => T(nameof(StatusNotDetected), "Nilesoft Shell was not detected.");
    public static string PathLabel => T(nameof(PathLabel), "Path");
    public static string NotFound => T(nameof(NotFound), "Not found");
    public static string StatusEnableCompleted => T(nameof(StatusEnableCompleted), "Enable command completed.");
    public static string StatusEnableFailed => T(nameof(StatusEnableFailed), "Enable command failed.");
    public static string StatusDisableCompleted => T(nameof(StatusDisableCompleted), "Disable command completed.");
    public static string StatusDisableFailed => T(nameof(StatusDisableFailed), "Disable command failed.");
    public static string StatusOpenedStyleSettings => T(nameof(StatusOpenedStyleSettings), "Opened style settings.");
    public static string StatusOpenedShellFolder => T(nameof(StatusOpenedShellFolder), "Opened shell folder.");
    public static string StatusOpenedConfig => T(nameof(StatusOpenedConfig), "Opened config file.");
    public static string StatusShellFolderNotFound => T(nameof(StatusShellFolderNotFound), "Shell folder not found.");
    public static string StatusConfigNotFound => T(nameof(StatusConfigNotFound), "Config file not found.");
    public static string VersionLabel => T(nameof(VersionLabel), "Version");

    private static readonly System.Resources.ResourceManager ResourceManager =
        new("LenovoLegionToolkit.Plugins.ShellIntegration.Resources.Resource", typeof(ShellIntegrationText).Assembly);

    private static string T(string key, string fallback)
    {
        return ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? fallback;
    }

}