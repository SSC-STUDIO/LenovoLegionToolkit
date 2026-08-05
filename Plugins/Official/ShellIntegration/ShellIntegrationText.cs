using System;
using System.Globalization;

namespace UniversalDeviceToolkit.Plugins.ShellIntegration;

public static class ShellIntegrationText
{
    public static string PluginName => T(nameof(PluginName), "Nilesoft Shell Manager");
    public static string PluginDescription => T(nameof(PluginDescription), "Manage Nilesoft Shell registration and its UDT-managed configuration. Requires Nilesoft Shell to be installed.");
    public static string SettingsPageTitle => T(nameof(SettingsPageTitle), "Nilesoft Shell Manager");
    public static string Subtitle => T(nameof(Subtitle), "Manage Nilesoft Shell registration and open style editor.");
    public static string OverviewTitle => T(nameof(OverviewTitle), "Shell Integration Overview");
    public static string OverviewDescription => T(nameof(OverviewDescription), "Check registration state, installation path, and jump straight into the packaged Shell tools.");
    public static string RegistrationLabel => T(nameof(RegistrationLabel), "Registration");
    public static string ConfigLabel => T(nameof(ConfigLabel), "Config");
    public static string ActionsTitle => T(nameof(ActionsTitle), "Quick Actions");
    public static string ActionsDescription => T(nameof(ActionsDescription), "Apply registration changes or open the packaged Shell assets used by this plugin.");
    public static string EnableButton => T(nameof(EnableButton), "Enable");
    public static string DisableButton => T(nameof(DisableButton), "Disable");
    public static string OpenStyleSettingsButton => T(nameof(OpenStyleSettingsButton), "Open Style Settings");
    public static string OpenStyleShortButton => T(nameof(OpenStyleShortButton), "Open Style");
    public static string OpenShellFolderButton => T(nameof(OpenShellFolderButton), "Open Shell Folder");
    public static string OpenConfigButton => T(nameof(OpenConfigButton), "Open Config File");
    public static string OpenManagedConfigButton => T(nameof(OpenManagedConfigButton), "Open Managed Config");
    public static string SyncManagedConfigButton => T(nameof(SyncManagedConfigButton), "Sync Managed Config");
    public static string ResetManagedConfigButton => T(nameof(ResetManagedConfigButton), "Reset Managed Config");
    public static string ExportProfileButton => T(nameof(ExportProfileButton), "Export Profile");
    public static string ImportProfileButton => T(nameof(ImportProfileButton), "Import Profile");
    public static string PresetsTitle => T(nameof(PresetsTitle), "Built-in Presets");
    public static string PresetsDescription => T(nameof(PresetsDescription), "Apply a tuned profile, save it locally, and regenerate the managed Shell config.");
    public static string PresetDefaultButton => T(nameof(PresetDefaultButton), "Apply Default");
    public static string PresetCompactDarkButton => T(nameof(PresetCompactDarkButton), "Apply Compact Dark");
    public static string PresetMinimalLightButton => T(nameof(PresetMinimalLightButton), "Apply Minimal Light");
    public static string OptimizationHint => T(nameof(OptimizationHint), "You can also access shell actions from Windows Optimization.");
    public static string StatusDetected => T(nameof(StatusDetected), "Nilesoft Shell detected.");
    public static string StatusRegistrationMissing => T(nameof(StatusRegistrationMissing), "Nilesoft Shell is installed, but registration is missing.");
    public static string StatusNotDetected => T(nameof(StatusNotDetected), "Nilesoft Shell was not detected.");
    public static string RegisteredState => T(nameof(RegisteredState), "Detected");
    public static string MissingState => T(nameof(MissingState), "Missing");
    public static string PathLabel => T(nameof(PathLabel), "Path");
    public static string NotFound => T(nameof(NotFound), "Not found");
    public static string StatusEnableCompleted => T(nameof(StatusEnableCompleted), "Enable command completed.");
    public static string StatusEnableFailed => T(nameof(StatusEnableFailed), "Enable command failed.");
    public static string StatusDisableCompleted => T(nameof(StatusDisableCompleted), "Disable command completed.");
    public static string StatusDisableFailed => T(nameof(StatusDisableFailed), "Disable command failed.");
    public static string StatusOpenedStyleSettings => T(nameof(StatusOpenedStyleSettings), "Opened style settings.");
    public static string StatusOpenedShellFolder => T(nameof(StatusOpenedShellFolder), "Opened shell folder.");
    public static string StatusOpenedConfig => T(nameof(StatusOpenedConfig), "Opened config file.");
    public static string StatusOpenedManagedConfig => T(nameof(StatusOpenedManagedConfig), "Opened managed config folder.");
    public static string StatusShellFolderNotFound => T(nameof(StatusShellFolderNotFound), "Shell folder not found.");
    public static string StatusConfigNotFound => T(nameof(StatusConfigNotFound), "Config file not found.");
    public static string StatusManagedConfigSyncCompleted => T(nameof(StatusManagedConfigSyncCompleted), "Managed config synchronized.");
    public static string StatusManagedConfigSyncFailed => T(nameof(StatusManagedConfigSyncFailed), "Managed config synchronization failed.");
    public static string StatusManagedConfigResetCompleted => T(nameof(StatusManagedConfigResetCompleted), "Managed config reset to defaults.");
    public static string StatusManagedConfigResetFailed => T(nameof(StatusManagedConfigResetFailed), "Managed config reset failed.");
    public static string StatusManagedConfigFolderUnavailable => T(nameof(StatusManagedConfigFolderUnavailable), "Managed config folder unavailable.");
    public static string StatusProfileExportCompleted => T(nameof(StatusProfileExportCompleted), "Profile exported.");
    public static string StatusProfileExportFailed => T(nameof(StatusProfileExportFailed), "Profile export failed.");
    public static string StatusProfileImportCompleted => T(nameof(StatusProfileImportCompleted), "Profile imported.");
    public static string StatusProfileImportFailed => T(nameof(StatusProfileImportFailed), "Profile import failed.");
    public static string StatusPresetAppliedDefault => T(nameof(StatusPresetAppliedDefault), "Applied the default Shell preset.");
    public static string StatusPresetAppliedCompactDark => T(nameof(StatusPresetAppliedCompactDark), "Applied the compact dark Shell preset.");
    public static string StatusPresetAppliedMinimalLight => T(nameof(StatusPresetAppliedMinimalLight), "Applied the minimal light Shell preset.");
    public static string StatusPresetApplyFailed => T(nameof(StatusPresetApplyFailed), "Preset apply failed.");
    public static string StatusWorking => T(nameof(StatusWorking), "Working...");
    public static string ProfileFileDialogFilter => T(nameof(ProfileFileDialogFilter), "JSON Files (*.json)|*.json|All Files (*.*)|*.*");
    public static string VersionLabel => T(nameof(VersionLabel), "Version");
    public static string ErrorPrefix => T(nameof(ErrorPrefix), "Error");
    public static string OpenFileButton => T(nameof(OpenFileButton), "Open File");
    public static string OpenFolderButton => T(nameof(OpenFolderButton), "Open Folder");

    private static readonly System.Resources.ResourceManager ResourceManager =
        new("UniversalDeviceToolkit.Plugins.ShellIntegration.Resources.Resource", typeof(ShellIntegrationText).Assembly);

    private static string T(string key, string fallback)
    {
        var culture = Resources.Resource.Culture ?? CultureInfo.CurrentUICulture;
        return ResourceManager.GetString(key, culture) ?? fallback;
    }

}
