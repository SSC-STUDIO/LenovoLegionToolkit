#if WINDOWS

using System.Globalization;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.Avalonia.Services;

internal sealed class WindowsAvaloniaSettingsService : IAvaloniaSettingsService
{
    internal static ApplicationSettings SharedApplicationSettings { get; } = new();

    private readonly ApplicationSettings _applicationSettings = SharedApplicationSettings;
    private readonly OsdSettings _osdSettings = new();
    private readonly UpdateCheckSettings _updateSettings = new();
    private readonly IntegrationsSettings _integrationsSettings = new();

    public async Task<AvaloniaSettingsPageData> GetPageAsync(string pageKey) =>
        pageKey switch
        {
            "Appearance" => BuildAppearancePage(),
            "Application" => BuildApplicationPage(),
            "Display" => await BuildDisplayPageAsync().ConfigureAwait(false),
            "SmartKeys" => BuildSmartKeysPage(),
            "Update" => BuildUpdatePage(),
            "Power" => BuildPowerPage(),
            "Integrations" => BuildIntegrationsPage(),
            _ => new AvaloniaSettingsPageData(pageKey, pageKey, string.Empty, [], false, "Unknown settings page."),
        };

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
            case ("Application", "ShowOsd"):
                _osdSettings.Store.ShowOsd = value;
                _osdSettings.SynchronizeStore();
                break;
            case ("Appearance", "ApplyAccentColorToSystem"):
                store.ApplyAccentColorToSystem = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Appearance", "ApplyAccentColorToTheme"):
                store.ApplyAccentColorToTheme = value;
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
            case ("Display", "DontShowNotifications"):
                store.DontShowNotifications = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Display", "NotificationAlwaysOnTop"):
                store.NotificationAlwaysOnTop = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Display", "NotificationOnAllScreens"):
                store.NotificationOnAllScreens = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Display", "NotificationSound"):
                store.Notifications.NotificationSound = value;
                _applicationSettings.SynchronizeStore();
                break;
            case ("Display", "NavigationPaneExpanded"):
                store.NavigationPaneExpanded = value;
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
                if (pageKey == "Display"
                    && optionKey.StartsWith("NavigationItemVisibility:", StringComparison.Ordinal))
                {
                    var navigationKey = optionKey["NavigationItemVisibility:".Length..];
                    if (string.IsNullOrWhiteSpace(navigationKey)
                        || !store.NavigationItemsVisibility.ContainsKey(navigationKey))
                    {
                        throw new KeyNotFoundException($"Unknown navigation item '{navigationKey}'.");
                    }

                    store.NavigationItemsVisibility[navigationKey] = value;
                    _applicationSettings.SynchronizeStore();
                    break;
                }

                throw new KeyNotFoundException($"Unknown toggle {pageKey}/{optionKey}.");
        }

        return Task.CompletedTask;
    }

    public Task SetSelectionAsync(string pageKey, string optionKey, string value)
    {
        if (pageKey == "Appearance" && optionKey == "Theme")
        {
            _applicationSettings.Store.Theme = ParseEnum<Theme>(value, "Theme");
            _applicationSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Appearance" && optionKey == "ThemeStylePreset")
        {
            _applicationSettings.Store.ThemeStylePreset = ParseEnum<ThemeStylePreset>(value, "ThemeStylePreset");
            _applicationSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Appearance" && optionKey == "AccentColorSource")
        {
            _applicationSettings.Store.AccentColorSource = ParseEnum<AccentColorSource>(value, "AccentColorSource");
            _applicationSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Appearance" && optionKey == "TemperatureUnit")
        {
            _applicationSettings.Store.TemperatureUnit = ParseTemperatureUnit(value);
            _applicationSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Appearance" && optionKey == "AppFontStyle")
        {
            _applicationSettings.Store.AppFontStyle = ParseFontStyle(value);
            _applicationSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Appearance" && optionKey == "UiScale")
        {
            var step = ParseUiScale(value);
            _applicationSettings.Store.AppTextSize = step.TextSize;
            _applicationSettings.Store.AppScale = step.Scale;
            _applicationSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Display" && optionKey == "WindowBackdrop")
        {
            _applicationSettings.Store.WindowBackdropStyle = ParseWindowBackdrop(value);
            _applicationSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Display" && optionKey == "NotificationPosition")
        {
            _applicationSettings.Store.NotificationPosition = ParseEnum<NotificationPosition>(value, "NotificationPosition");
            _applicationSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Display" && optionKey == "NotificationDuration")
        {
            _applicationSettings.Store.NotificationDuration = ParseEnum<NotificationDuration>(value, "NotificationDuration");
            _applicationSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Power" && optionKey == "PowerModeMapping")
        {
            _applicationSettings.Store.PowerModeMappingMode = ParsePowerModeMapping(value);
            _applicationSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Update" && optionKey == "UpdateFrequency")
        {
            _updateSettings.Store.UpdateCheckFrequency = ParseEnum<UpdateCheckFrequency>(value, "UpdateFrequency");
            _updateSettings.SynchronizeStore();
            return Task.CompletedTask;
        }

        if (pageKey == "Display" && optionKey == "RefreshRate")
        {
            if (!int.TryParse(value.Replace(" Hz", string.Empty, StringComparison.OrdinalIgnoreCase), out var frequency))
                throw new ArgumentException($"Invalid refresh rate '{value}'.", nameof(value));

            return new RefreshRateFeature().SetStateAsync(new RefreshRate(frequency));
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

    private AvaloniaSettingsPageData BuildAppearancePage()
    {
        var store = _applicationSettings.Store;
        var uiScale = GetCurrentUiScaleStep(store);

        return new AvaloniaSettingsPageData(
            "Appearance",
            "Appearance",
            "Customize the look and feel of the application.",
            [
                new("Theme", "Theme", "Choose the application color theme.", AvaloniaSettingEditor.Selection, true,
                    Values: Enum.GetValues<Theme>().Select(value => value.ToString()).ToArray(),
                    SelectedValue: store.Theme.ToString()),
                new("ThemeStylePreset", "Theme style preset", "Choose the visual style preset used by the application.", AvaloniaSettingEditor.Selection, true,
                    Values: Enum.GetValues<ThemeStylePreset>().Select(value => value.ToString()).ToArray(),
                    SelectedValue: store.ThemeStylePreset.ToString()),
                new("AccentColorSource", "Accent color source", "Choose whether the system or a custom accent color is used.", AvaloniaSettingEditor.Selection, true,
                    Values: Enum.GetValues<AccentColorSource>().Select(value => value.ToString()).ToArray(),
                    SelectedValue: store.AccentColorSource.ToString()),
                new("ApplyAccentColorToSystem", "Apply accent color to Windows", "Write the selected accent color to the Windows system accent.", AvaloniaSettingEditor.Toggle, true, store.ApplyAccentColorToSystem),
                new("ApplyAccentColorToTheme", "Apply accent color to the theme", "Use the selected accent color across the application theme.", AvaloniaSettingEditor.Toggle, true, store.ApplyAccentColorToTheme),
                new("TemperatureUnit", "Temperature unit", "Choose the unit used for sensor and status readings.", AvaloniaSettingEditor.Selection, true,
                    Values: Enum.GetValues<TemperatureUnit>().Select(FormatTemperatureUnit).ToArray(),
                    SelectedValue: FormatTemperatureUnit(store.TemperatureUnit)),
                new("AppFontStyle", "Application font", "Choose the font family used by the application.", AvaloniaSettingEditor.Selection, true,
                    Values: Enum.GetValues<AppFontStyle>().Select(FormatFontStyle).ToArray(),
                    SelectedValue: FormatFontStyle(store.AppFontStyle)),
                new("UiScale", "Interface scale", "Choose the text and layout scale used by the application.", AvaloniaSettingEditor.Selection, true,
                    Values: GetUiScaleSteps().Select(FormatUiScale).ToArray(),
                    SelectedValue: FormatUiScale(uiScale)),
            ],
            true);
    }

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
                new("ShowOsd", "Show on-screen display", "Show hardware status changes in the on-screen display.", AvaloniaSettingEditor.Toggle, true, _osdSettings.Store.ShowOsd),
            ],
            true);
    }

    private async Task<AvaloniaSettingsPageData> BuildDisplayPageAsync()
    {
        var store = _applicationSettings.Store;
        var refreshRates = await GetRefreshRatesAsync().ConfigureAwait(false);
        var currentRefreshRate = await GetCurrentRefreshRateAsync().ConfigureAwait(false);

        var navigationOptions = store.NavigationItemsVisibility
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new AvaloniaSettingOption(
                $"NavigationItemVisibility:{pair.Key}",
                FormatNavigationTitle(pair.Key),
                $"Show the {FormatNavigationTitle(pair.Key)} entry in the main navigation.",
                AvaloniaSettingEditor.Toggle,
                true,
                pair.Value))
            .ToArray();

        return new AvaloniaSettingsPageData(
            "Display",
            "Display",
            "Adjust display-related settings for your device.",
            [
                new("RefreshRate", "Refresh rate", "Choose a supported refresh rate for the built-in display.", AvaloniaSettingEditor.Selection, refreshRates.Length > 0, Values: refreshRates, SelectedValue: currentRefreshRate, Warning: refreshRates.Length == 0 ? "No supported built-in display refresh rates were detected." : null),
                new("SynchronizeBrightness", "Synchronize brightness to all power plans", "Keep brightness synchronized across Windows power plans.", AvaloniaSettingEditor.Toggle, true, store.SynchronizeBrightnessToAllPowerPlans),
                new("ForceSoftwareRendering", "Force software rendering", "Use software rendering for the application window.", AvaloniaSettingEditor.Toggle, true, store.ForceSoftwareRendering),
                new("WindowBackdrop", "Window backdrop", "Choose the backdrop style used by the application window.", AvaloniaSettingEditor.Selection, true,
                    Values: Enum.GetValues<WindowBackdropStyle>().Select(FormatWindowBackdrop).ToArray(),
                    SelectedValue: FormatWindowBackdrop(store.WindowBackdropStyle)),
                new("DontShowNotifications", "Disable notifications", "Hide on-screen application notifications.", AvaloniaSettingEditor.Toggle, true, store.DontShowNotifications),
                new("NotificationPosition", "Notification position", "Choose where on-screen notifications are displayed.", AvaloniaSettingEditor.Selection, true,
                    Values: Enum.GetValues<NotificationPosition>().Select(value => value.ToString()).ToArray(),
                    SelectedValue: store.NotificationPosition.ToString()),
                new("NotificationDuration", "Notification duration", "Choose how long on-screen notifications remain visible.", AvaloniaSettingEditor.Selection, true,
                    Values: Enum.GetValues<NotificationDuration>().Select(value => value.ToString()).ToArray(),
                    SelectedValue: store.NotificationDuration.ToString()),
                new("NotificationAlwaysOnTop", "Keep notifications on top", "Keep notifications above other windows.", AvaloniaSettingEditor.Toggle, true, store.NotificationAlwaysOnTop),
                new("NotificationOnAllScreens", "Show notifications on all screens", "Show notifications on every connected display.", AvaloniaSettingEditor.Toggle, true, store.NotificationOnAllScreens),
                new("NotificationSound", "Notification sound", "Play a short sound for in-app notifications.", AvaloniaSettingEditor.Toggle, true, store.Notifications.NotificationSound),
                new("NavigationPaneExpanded", "Expanded navigation", "Keep the main navigation pane expanded.", AvaloniaSettingEditor.Toggle, true, store.NavigationPaneExpanded),
                new("Overdrive", "Display overdrive", "Enable panel overdrive on supported hardware.", AvaloniaSettingEditor.Toggle, false, Warning: "Display overdrive requires the Lenovo display adapter."),
                ..navigationOptions,
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
        var mapping = FormatPowerModeMapping(store.PowerModeMappingMode);
        return new AvaloniaSettingsPageData(
            "Power",
            "Power",
            "Configure power mode mapping and battery behavior.",
            [
                new("PowerModeMapping", "Power mode mapping", "Choose how device power modes map to Windows.", AvaloniaSettingEditor.Selection, true,
                    Values: Enum.GetValues<PowerModeMappingMode>().Select(FormatPowerModeMapping).ToArray(), SelectedValue: mapping),
                new("ResetBatteryOnReboot", "Reset battery timer on reboot", "Reset the battery since timer after Windows restarts.", AvaloniaSettingEditor.Toggle, true, store.ResetBatteryOnSinceTimerOnReboot),
            ],
            true);
    }

    private static async Task<string[]> GetRefreshRatesAsync()
    {
        try
        {
            var states = await new RefreshRateFeature().GetAllStatesAsync().ConfigureAwait(false);
            return states.Select(state => state.DisplayName).ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static async Task<string?> GetCurrentRefreshRateAsync()
    {
        try
        {
            var state = await new RefreshRateFeature().GetStateAsync().ConfigureAwait(false);
            return state.Frequency > 0 ? state.DisplayName : null;
        }
        catch
        {
            return null;
        }
    }

    private static string FormatTemperatureUnit(TemperatureUnit value) => value switch
    {
        TemperatureUnit.F => "\u00B0F",
        _ => "\u00B0C",
    };

    private static TemperatureUnit ParseTemperatureUnit(string value) =>
        value.Contains('F', StringComparison.OrdinalIgnoreCase)
            ? TemperatureUnit.F
            : TemperatureUnit.C;

    private static string FormatFontStyle(AppFontStyle value) => value switch
    {
        AppFontStyle.FluentVariable => "Segoe UI Variable",
        AppFontStyle.YaHeiUI => "Microsoft YaHei UI",
        AppFontStyle.DengXian => "DengXian",
        AppFontStyle.NotoSans => "Noto Sans CJK SC",
        AppFontStyle.SimHei => "SimHei",
        AppFontStyle.SimSun => "SimSun",
        AppFontStyle.KaiTi => "KaiTi",
        _ => "Default",
    };

    private static AppFontStyle ParseFontStyle(string value) => value switch
    {
        "Segoe UI Variable" => AppFontStyle.FluentVariable,
        "Microsoft YaHei UI" => AppFontStyle.YaHeiUI,
        "DengXian" => AppFontStyle.DengXian,
        "Noto Sans CJK SC" => AppFontStyle.NotoSans,
        "SimHei" => AppFontStyle.SimHei,
        "SimSun" => AppFontStyle.SimSun,
        "KaiTi" => AppFontStyle.KaiTi,
        _ => AppFontStyle.Default,
    };

    private static IReadOnlyList<(AppTextSize TextSize, AppScale Scale)> GetUiScaleSteps() =>
    [
        (AppTextSize.Compact, AppScale.Small),
        (AppTextSize.Standard, AppScale.Standard),
        (AppTextSize.Large, AppScale.Large),
        (AppTextSize.ExtraLarge, AppScale.ExtraLarge),
    ];

    private static (AppTextSize TextSize, AppScale Scale) GetCurrentUiScaleStep(ApplicationSettings.ApplicationSettingsStore store)
    {
        var current = GetUiScaleSteps()
            .FirstOrDefault(step => step.TextSize == store.AppTextSize && step.Scale == store.AppScale);
        return current == default
            ? (AppTextSize.Standard, AppScale.Standard)
            : current;
    }

    private static string FormatUiScale((AppTextSize TextSize, AppScale Scale) step) =>
        step.Scale == AppScale.Standard ? $"{(int)step.Scale}% (Default)" : $"{(int)step.Scale}%";

    private static (AppTextSize TextSize, AppScale Scale) ParseUiScale(string value)
    {
        var scale = value.Split('%', 2)[0].Trim();
        return scale switch
        {
            "90" => (AppTextSize.Compact, AppScale.Small),
            "100" => (AppTextSize.Standard, AppScale.Standard),
            "110" => (AppTextSize.Large, AppScale.Large),
            "125" => (AppTextSize.ExtraLarge, AppScale.ExtraLarge),
            _ => throw new ArgumentException($"Unknown UI scale '{value}'.", nameof(value)),
        };
    }

    private static string FormatWindowBackdrop(WindowBackdropStyle value) => value switch
    {
        WindowBackdropStyle.Windows => "Mica",
        WindowBackdropStyle.macOS => "Acrylic",
        _ => "Off",
    };

    private static WindowBackdropStyle ParseWindowBackdrop(string value) => value switch
    {
        "Mica" or nameof(WindowBackdropStyle.Windows) => WindowBackdropStyle.Windows,
        "Acrylic" or nameof(WindowBackdropStyle.macOS) => WindowBackdropStyle.macOS,
        _ => WindowBackdropStyle.Off,
    };

    private static string FormatPowerModeMapping(PowerModeMappingMode value) => value switch
    {
        PowerModeMappingMode.WindowsPowerPlan => "Windows power plans",
        _ => "Windows power mode",
    };

    private static PowerModeMappingMode ParsePowerModeMapping(string value) => value switch
    {
        "Windows power plans" or nameof(PowerModeMappingMode.WindowsPowerPlan) => PowerModeMappingMode.WindowsPowerPlan,
        "Windows power mode" or nameof(PowerModeMappingMode.WindowsPowerMode) => PowerModeMappingMode.WindowsPowerMode,
        _ => throw new ArgumentException($"Unknown power mode mapping '{value}'.", nameof(value)),
    };

    private static string FormatNavigationTitle(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return key;

        var builder = new System.Text.StringBuilder(key.Length + 8);
        builder.Append(char.ToUpperInvariant(key[0]));
        for (var i = 1; i < key.Length; i++)
        {
            if (char.IsUpper(key[i]))
                builder.Append(' ');
            builder.Append(key[i]);
        }

        return builder.ToString();
    }

    private static T ParseEnum<T>(string value, string optionKey) where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException($"Unknown {optionKey} value '{value}'.", nameof(value));

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
