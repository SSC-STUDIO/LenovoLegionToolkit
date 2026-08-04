#if WINDOWS

using System.Globalization;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.Avalonia.Services;

internal sealed class WindowsAvaloniaSettingsService : IAvaloniaSettingsService
{
    private readonly ApplicationSettings _applicationSettings = new();
    private readonly UpdateCheckSettings _updateSettings = new();
    private readonly IntegrationsSettings _integrationsSettings = new();

    public Task<AvaloniaSettingsPageData> GetPageAsync(string pageKey) =>
        Task.FromResult(pageKey switch
        {
            "Application" => BuildApplicationPage(),
            "Display" => BuildDisplayPage(),
            "SmartKeys" => BuildSmartKeysPage(),
            "Update" => BuildUpdatePage(),
            "Power" => BuildPowerPage(),
            "Integrations" => BuildIntegrationsPage(),
            _ => new AvaloniaSettingsPageData(pageKey, pageKey, string.Empty, [], false, "Unknown settings page."),
        });

    public Task SetToggleAsync(string pageKey, string optionKey, bool value)
    {
        var store = _applicationSettings.Store;
        switch (pageKey, optionKey)
        {
            case ("Application", "MinimizeToTray"):
                store.MinimizeToTray = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Application", "MinimizeOnClose"):
                store.MinimizeOnClose = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Application", "AnimationsEnabled"):
                store.AnimationsEnabled = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Application", "EnableHardwareSensors"):
                store.EnableHardwareSensors = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Application", "DisableUnsupportedHardwareWarning"):
                store.DisableUnsupportedHardwareWarning = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Display", "SynchronizeBrightness"):
                store.SynchronizeBrightnessToAllPowerPlans = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Display", "ForceSoftwareRendering"):
                store.ForceSoftwareRendering = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Update", "IncludePrereleaseUpdates"):
                _updateSettings.Store.IncludePrereleaseUpdates = value;
                _updateSettings.SynchronizeStore();
                break;
            case ("Power", "ResetBatteryOnReboot"):
                store.ResetBatteryOnSinceTimerOnReboot = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Integrations", "HWiNFO"):
                _integrationsSettings.Store.HWiNFO = value;
                _integrationsSettings.SynchronizeStore();
                break;
            case ("Integrations", "CLI"):
                _integrationsSettings.Store.CLI = value;
                _integrationsSettings.SynchronizeStore();
                break;
            default:
                throw new KeyNotFoundException($"Unknown toggle {pageKey}/{optionKey}.");
        }

        return Task.CompletedTask;
    }

    public Task SetSelectionAsync(string pageKey, string optionKey, string value)
    {
        if (pageKey == "Display" && optionKey == "WindowBackdrop")
        {
            _applicationSettings.Store.WindowBackdropStyle = value switch
            {
                "Mica" => WindowBackdropStyle.Windows,
                "Acrylic" => WindowBackdropStyle.macOS,
                _ => WindowBackdropStyle.Off,
            };
            _applicationSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Power" && optionKey == "PowerModeMapping")
        {
            _applicationSettings.Store.PowerModeMappingMode = value switch
            {
                "Windows power plans" => PowerModeMappingMode.WindowsPowerPlan,
                _ => PowerModeMappingMode.WindowsPowerMode,
            };
            _applicationSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Update" && optionKey == "UpdateFrequency")
        {
            _updateSettings.Store.UpdateCheckFrequency = Enum.Parse<UpdateCheckFrequency>(value, ignoreCase: true);
            _updateSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        throw new KeyNotFoundException($"Unknown selection {pageKey}/{optionKey}.");
    }

    public Task SetTextAsync(string pageKey, string optionKey, string? value)
    {
        if (pageKey != "Update" || (optionKey != "RepositoryOwner" && optionKey != "RepositoryName"))
            throw new KeyNotFoundException($"Unknown text option {pageKey}/{optionKey}.");

        if (optionKey == "RepositoryOwner")
            _updateSettings.Store.UpdateRepositoryOwner = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        else
            _updateSettings.Store.UpdateRepositoryName = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        _updateSettings.SynchronizeStore();
        return Task.CompletedTask;
    }

    private AvaloniaSettingsPageData BuildSmartKeysPage() => new(
        "SmartKeys",
        "Smart Keys",
        "Configure Fn-lock and Smart Key behavior.",
        [new AvaloniaSettingOption(
            "SmartKeyHardware",
            "Smart Key hardware actions",
            "Hardware-specific Smart Key actions are exposed by the Windows device adapter.",
            AvaloniaSettingEditor.Toggle,
            false,
            Warning: "No compatible Smart Key device was detected.")],
        false,
        "The current machine does not expose the Lenovo Smart Key adapter.");

    private AvaloniaSettingsPageData BuildApplicationPage()
    {
        var store = _applicationSettings.Store;
        return new AvaloniaSettingsPageData(
            "Application",
            "Application Behavior",
            "Configure how the application behaves on startup and during use.",
            [
                new("LaunchAtStartup", "Launch at startup", "Automatically start the application when Windows starts.", AvaloniaSettingEditor.Toggle, false, Warning: "Startup registration is managed by the Windows host shell."),
                new("MinimizeToTray", "Minimize to system tray", "Keep the application running in the system tray when it is minimized or closed.", AvaloniaSettingEditor.Toggle, true, store.MinimizeToTray),
                new("MinimizeOnClose", "Minimize on close", "Hide the window instead of exiting when the close button is pressed.", AvaloniaSettingEditor.Toggle, true, store.MinimizeOnClose),
                new("AnimationsEnabled", "Enable animations", "Use page and control transition animations throughout the application.", AvaloniaSettingEditor.Toggle, true, store.AnimationsEnabled),
                new("EnableHardwareSensors", "Enable hardware sensors", "Poll supported hardware sensors for dashboard readings.", AvaloniaSettingEditor.Toggle, true, store.EnableHardwareSensors),
                new("DisableUnsupportedHardwareWarning", "Disable compatibility warning", "Hide the warning shown when hardware-specific features are unavailable.", AvaloniaSettingEditor.Toggle, true, store.DisableUnsupportedHardwareWarning),
            ],
            true);
    }

    private AvaloniaSettingsPageData BuildDisplayPage()
    {
        var store = _applicationSettings.Store;
        var backdrop = store.WindowBackdropStyle switch
        {
            WindowBackdropStyle.Windows => "Mica",
            WindowBackdropStyle.macOS => "Acrylic",
            _ => "Off",
        };
        return new AvaloniaSettingsPageData(
            "Display",
            "Display",
            "Adjust display-related settings for your device.",
            [
                new("RefreshRate", "Refresh rate", "Display refresh-rate control requires a compatible Lenovo panel.", AvaloniaSettingEditor.Selection, false, Values: new[] { "60 Hz", "120 Hz", "144 Hz", "165 Hz" }, SelectedValue: "60 Hz", Warning: "No safe cross-platform refresh-rate adapter is available."),
                new("SynchronizeBrightness", "Synchronize brightness to all power plans", "Keep brightness synchronized across Windows power plans.", AvaloniaSettingEditor.Toggle, true, store.SynchronizeBrightnessToAllPowerPlans),
                new("ForceSoftwareRendering", "Force software rendering", "Use software rendering for the application window.", AvaloniaSettingEditor.Toggle, true, store.ForceSoftwareRendering),
                new("WindowBackdrop", "Window backdrop", "Choose the backdrop style used by the application window.", AvaloniaSettingEditor.Selection, true, Values: new[] { "Mica", "Acrylic", "Off" }, SelectedValue: backdrop),
                new("Overdrive", "Display overdrive", "Enable panel overdrive on supported hardware.", AvaloniaSettingEditor.Toggle, false, Warning: "Display overdrive requires the Lenovo display adapter."),
            ],
            true);
    }

    private AvaloniaSettingsPageData BuildUpdatePage()
    {
        var store = _updateSettings.Store;
        var frequencies = Enum.GetValues<UpdateCheckFrequency>()
            .Select(value => value.ToString())
            .ToArray();
        return new AvaloniaSettingsPageData(
            "Update",
            "Update",
            "Choose how Universal Device Toolkit checks for new releases.",
            [
                new("UpdateFrequency", "Update check frequency", "How often automatic update checks run.", AvaloniaSettingEditor.Selection, true, Values: frequencies, SelectedValue: store.UpdateCheckFrequency.ToString()),
                new("IncludePrereleaseUpdates", "Include prerelease updates", "Offer preview releases in addition to stable releases.", AvaloniaSettingEditor.Toggle, true, store.IncludePrereleaseUpdates),
                new("RepositoryOwner", "Repository owner", "Override the update repository owner in debug builds.", AvaloniaSettingEditor.Text, true, TextValue: store.UpdateRepositoryOwner ?? ""),
                new("RepositoryName", "Repository name", "Override the update repository name in debug builds.", AvaloniaSettingEditor.Text, true, TextValue: store.UpdateRepositoryName ?? ""),
            ],
            true);
    }

    private AvaloniaSettingsPageData BuildPowerPage()
    {
        var store = _applicationSettings.Store;
        var mapping = store.PowerModeMappingMode == PowerModeMappingMode.WindowsPowerPlan
            ? "Windows power plans"
            : "Windows power mode";
        return new AvaloniaSettingsPageData(
            "Power",
            "Power",
            "Configure power mode mapping and battery behavior.",
            [
                new("PowerModeMapping", "Power mode mapping", "Choose how device power modes map to Windows.", AvaloniaSettingEditor.Selection, true, Values: new[] { "Windows power mode", "Windows power plans" }, SelectedValue: mapping),
                new("ResetBatteryOnReboot", "Reset battery timer on reboot", "Reset the battery since timer after Windows restarts.", AvaloniaSettingEditor.Toggle, true, store.ResetBatteryOnSinceTimerOnReboot),
            ],
            true);
    }

    private AvaloniaSettingsPageData BuildIntegrationsPage()
    {
        var store = _integrationsSettings.Store;
        return new AvaloniaSettingsPageData(
            "Integrations",
            "Integrations",
            "Connect Universal Device Toolkit to supported external tools and services.",
            [
                new("HWiNFO", "HWiNFO integration", "Expose hardware sensor data through HWiNFO when available.", AvaloniaSettingEditor.Toggle, true, store.HWiNFO),
                new("CLI", "CLI interface", "Enable the local command-line interface.", AvaloniaSettingEditor.Toggle, true, store.CLI),
            ],
            true);
    }
}

#endif
