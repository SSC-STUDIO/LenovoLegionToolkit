#if WINDOWS

using System.Diagnostics;
using System.Globalization;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Automation.Utils;
using UniversalDeviceToolkit.Lib.Features;
using UniversalDeviceToolkit.Lib.Integrations;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.SoftwareDisabler;
using UniversalDeviceToolkit.Lib.System;
using UniversalDeviceToolkit.Lib.System.Management;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Abstractions.Lifecycle;

namespace UniversalDeviceToolkit.Avalonia.Services;

internal sealed class WindowsAvaloniaSettingsService : IAvaloniaSettingsService
{
    internal static ApplicationSettings SharedApplicationSettings { get; } = new();

    private readonly ApplicationSettings _applicationSettings = SharedApplicationSettings;
    private readonly OsdSettings _osdSettings = new();
    private readonly UpdateCheckSettings _updateSettings = new();
    private readonly IntegrationsSettings _integrationsSettings =
        IoCContainer.TryResolve<IntegrationsSettings>() ?? new IntegrationsSettings();

    public async Task<AvaloniaSettingsPageData> GetPageAsync(string pageKey) =>
        pageKey switch
        {
            "Appearance" => BuildAppearancePage(),
            "Application" => await BuildApplicationPageAsync().ConfigureAwait(false),
            "Display" => await BuildDisplayPageAsync().ConfigureAwait(false),
            "SmartKeys" => await BuildSmartKeysPageAsync().ConfigureAwait(false),
            "Update" => BuildUpdatePage(),
            "Power" => await BuildPowerPageAsync().ConfigureAwait(false),
            "Integrations" => BuildIntegrationsPage(),
            _ => new AvaloniaSettingsPageData(pageKey, pageKey, string.Empty, [], false, "Unknown settings page."),
        };

    public async Task SetToggleAsync(string pageKey, string optionKey, bool value)
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
            case ("Application", "VantageDisabled"):
                await SetSoftwareDisabledAsync<VantageDisabler>(value).ConfigureAwait(false);
                break;
            case ("Application", "LegionZoneDisabled"):
                await SetSoftwareDisabledAsync<LegionZoneDisabler>(value).ConfigureAwait(false);
                break;
            case ("Application", "FnKeysDisabled"):
                await SetSoftwareDisabledAsync<FnKeysDisabler>(value).ConfigureAwait(false);
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
            case ("Display", "NotificationSuccess")
                or ("Display", "NotificationUpdateAvailable")
                or ("Display", "NotificationCapsNumLock")
                or ("Display", "NotificationFnLock")
                or ("Display", "NotificationTouchpadLock")
                or ("Display", "NotificationKeyboardBacklight")
                or ("Display", "NotificationCameraLock")
                or ("Display", "NotificationMicrophone")
                or ("Display", "NotificationPowerMode")
                or ("Display", "NotificationRefreshRate")
                or ("Display", "NotificationACAdapter")
                or ("Display", "NotificationSmartKey")
                or ("Display", "NotificationAutomation"):
                SetNotificationToggle(optionKey, value);
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
            case ("Power", "GodModeFnQSwitchable"):
                await SetGodModeFnQSwitchableAsync(value).ConfigureAwait(false);
                break;
            case ("Integrations", "HWiNFO"):
                var previousHwInfo = _integrationsSettings.Store.HWiNFO;
                _integrationsSettings.Store.HWiNFO = value;
                _integrationsSettings.SynchronizeStore();
                try
                {
                    await (IoCContainer.TryResolve<HWiNFOIntegration>()
                        ?? throw new PlatformNotSupportedException("HWiNFO integration is not initialized."))
                        .StartStopIfNeededAsync().ConfigureAwait(false);
                }
                catch
                {
                    _integrationsSettings.Store.HWiNFO = previousHwInfo;
                    _integrationsSettings.SynchronizeStore();
                    throw;
                }
                break;
            case ("Integrations", "CLI"):
                var previousCli = _integrationsSettings.Store.CLI;
                _integrationsSettings.Store.CLI = value;
                _integrationsSettings.SynchronizeStore();
                try
                {
                    var lifecycle = IoCContainer.TryResolve<ICliHostLifecycle>()
                        ?? throw new PlatformNotSupportedException("The CLI host service is not initialized.");
                    await lifecycle.StartStopIfNeededAsync().ConfigureAwait(false);
                }
                catch
                {
                    _integrationsSettings.Store.CLI = previousCli;
                    _integrationsSettings.SynchronizeStore();
                    throw;
                }
                break;
            case ("Integrations", "CLIPath"):
                SystemPath.SetCLI(value);
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

        return;
    }

    public async Task SetSelectionAsync(string pageKey, string optionKey, string value)
    {
        if (pageKey == "Appearance" && optionKey == "Theme")
        {
            _applicationSettings.Store.Theme = ParseEnum<Theme>(value, "Theme");
            _applicationSettings.SynchronizeStore();
            return;
        }

        if (pageKey == "Application" && optionKey == "Autorun")
        {
            Autorun.Set(ParseEnum<AutorunState>(value, "Autorun"));
            return;
        }

        if (pageKey == "Appearance" && optionKey == "ThemeStylePreset")
        {
            _applicationSettings.Store.ThemeStylePreset = ParseEnum<ThemeStylePreset>(value, "ThemeStylePreset");
            _applicationSettings.SynchronizeStore();
            return;
        }

        if (pageKey == "Appearance" && optionKey == "AccentColorSource")
        {
            _applicationSettings.Store.AccentColorSource = ParseEnum<AccentColorSource>(value, "AccentColorSource");
            _applicationSettings.SynchronizeStore();
            return;
        }

        if (pageKey == "Appearance" && optionKey == "TemperatureUnit")
        {
            _applicationSettings.Store.TemperatureUnit = ParseTemperatureUnit(value);
            _applicationSettings.SynchronizeStore();
            return;
        }

        if (pageKey == "Appearance" && optionKey == "AppFontStyle")
        {
            _applicationSettings.Store.AppFontStyle = ParseFontStyle(value);
            _applicationSettings.SynchronizeStore();
            return;
        }

        if (pageKey == "Appearance" && optionKey == "UiScale")
        {
            var step = ParseUiScale(value);
            _applicationSettings.Store.AppTextSize = step.TextSize;
            _applicationSettings.Store.AppScale = step.Scale;
            _applicationSettings.SynchronizeStore();
            return;
        }

        if (pageKey == "Display" && optionKey == "WindowBackdrop")
        {
            _applicationSettings.Store.WindowBackdropStyle = ParseWindowBackdrop(value);
            _applicationSettings.SynchronizeStore();
            return;
        }

        if (pageKey == "Display" && optionKey == "NotificationPosition")
        {
            _applicationSettings.Store.NotificationPosition = ParseEnum<NotificationPosition>(value, "NotificationPosition");
            _applicationSettings.SynchronizeStore();
            return;
        }

        if (pageKey == "Display" && optionKey == "NotificationDuration")
        {
            _applicationSettings.Store.NotificationDuration = ParseEnum<NotificationDuration>(value, "NotificationDuration");
            _applicationSettings.SynchronizeStore();
            return;
        }

        if (pageKey == "Power" && optionKey == "PowerModeMapping")
        {
            _applicationSettings.Store.PowerModeMappingMode = ParsePowerModeMapping(value);
            _applicationSettings.SynchronizeStore();
            return;
        }

        if (pageKey == "Update" && optionKey == "UpdateFrequency")
        {
            _updateSettings.Store.UpdateCheckFrequency = ParseEnum<UpdateCheckFrequency>(value, "UpdateFrequency");
            _updateSettings.SynchronizeStore();
            IoCContainer.TryResolve<UpdateChecker>()?.UpdateMinimumTimeSpanForRefresh();
            return;
        }

        if (pageKey == "SmartKeys" && optionKey == "SmartFnLockFlags")
        {
            _applicationSettings.Store.SmartFnLockFlags = ParseSmartFnLockFlags(value);
            _applicationSettings.SynchronizeStore();
            return;
        }

        if (pageKey == "Display" && optionKey == "RefreshRate")
        {
            if (!int.TryParse(value.Replace(" Hz", string.Empty, StringComparison.OrdinalIgnoreCase), out var frequency))
                throw new ArgumentException($"Invalid refresh rate '{value}'.", nameof(value));

            await new RefreshRateFeature().SetStateAsync(new RefreshRate(frequency)).ConfigureAwait(false);
            return;
        }

        throw new KeyNotFoundException($"Unknown selection {pageKey}/{optionKey}.");
    }

    public async Task SetMultiSelectionAsync(string pageKey, string optionKey, IReadOnlyList<string> values)
    {
        if (pageKey == "Display" && optionKey == "ExcludedRefreshRates")
        {
            var excluded = values
                .Select(ParseRefreshRate)
                .DistinctBy(rate => rate.Frequency)
                .ToList();
            _applicationSettings.Store.ExcludedRefreshRates.Clear();
            _applicationSettings.Store.ExcludedRefreshRates.AddRange(excluded);
            _applicationSettings.SynchronizeStore();
            return;
        }

        if (pageKey != "SmartKeys"
            || (optionKey != "SmartKeySinglePressActions" && optionKey != "SmartKeyDoublePressActions"))
            throw new KeyNotFoundException($"Unknown multi-selection {pageKey}/{optionKey}.");

        var options = await GetManualPipelineOptionsAsync().ConfigureAwait(false);
        var thisAppName = options[0].Name;
        if (values.Any(value => string.Equals(value, thisAppName, StringComparison.Ordinal)))
            values = [];
        var selected = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => options.FirstOrDefault(option => string.Equals(option.Name, value, StringComparison.Ordinal)))
            .Where(option => option != default && option.Id != Guid.Empty)
            .Select(option => option.Id)
            .Distinct()
            .ToList();
        var isDoublePress = optionKey == "SmartKeyDoublePressActions";
        var list = isDoublePress
            ? _applicationSettings.Store.SmartKeyDoublePressActionList
            : _applicationSettings.Store.SmartKeySinglePressActionList;

        list.Clear();
        list.AddRange(selected);
        if (isDoublePress)
            _applicationSettings.Store.SmartKeyDoublePressActionId = selected.Count > 0 ? selected[0] : null;
        else
            _applicationSettings.Store.SmartKeySinglePressActionId = selected.Count > 0 ? selected[0] : null;

        _applicationSettings.SynchronizeStore();
    }

    public async Task SetBootLogoAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("A boot logo image is required.", nameof(filePath));

        if (!await BootLogo.IsSupportedAsync().ConfigureAwait(false))
            throw new PlatformNotSupportedException("Boot logo controls are not available on this device.");

        await BootLogo.EnableAsync(filePath).ConfigureAwait(false);
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

    public async Task InvokeActionAsync(string pageKey, string optionKey)
    {
        if (pageKey == "Display" && optionKey == "BootLogoReset")
        {
            if (!await BootLogo.IsSupportedAsync().ConfigureAwait(false))
                throw new PlatformNotSupportedException("Boot logo controls are not available on this device.");

            await BootLogo.DisableAsync().ConfigureAwait(false);
            return;
        }

        if (pageKey == "Power")
        {
            switch (optionKey)
            {
                case "OpenPowerModes":
                    OpenShellUri("ms-settings:powersleep");
                    return;
                case "OpenPowerPlans":
                case "OpenPowerPlansControlPanel":
                    OpenControlPanel("/name Microsoft.PowerOptions");
                    return;
                default:
                    throw new KeyNotFoundException($"Unknown power action {optionKey}.");
            }
        }

        if (pageKey != "Update" || optionKey != "CheckForUpdates")
            throw new KeyNotFoundException($"Unknown action {pageKey}/{optionKey}.");

        var updateChecker = IoCContainer.TryResolve<UpdateChecker>()
            ?? throw new PlatformNotSupportedException("The update checker is not initialized.");

        if (updateChecker.Disable)
            throw new InvalidOperationException(updateChecker.DisableReason ?? "Update checks are disabled for this session.");

        await updateChecker.CheckAsync(forceCheck: true).ConfigureAwait(false);
    }

    private async Task<AvaloniaSettingsPageData> BuildSmartKeysPageAsync()
    {
        var pipelines = await GetManualPipelineOptionsAsync().ConfigureAwait(false);
        var singleSelected = GetSelectedPipelineNames(_applicationSettings.Store.SmartKeySinglePressActionList, _applicationSettings.Store.SmartKeySinglePressActionId, pipelines);
        var doubleSelected = GetSelectedPipelineNames(_applicationSettings.Store.SmartKeyDoublePressActionList, _applicationSettings.Store.SmartKeyDoublePressActionId, pipelines);

        return new AvaloniaSettingsPageData(
            "SmartKeys",
            "Smart Keys",
            "Configure Fn-lock and Smart Key behavior.",
            [
                new(
                    "SmartFnLockFlags",
                    "Fn-lock modifier keys",
                    "Choose which modifier keys are required when toggling Fn-lock.",
                    AvaloniaSettingEditor.Selection,
                    true,
                    Values: GetSmartFnLockValues(_applicationSettings.Store.SmartFnLockFlags),
                    SelectedValue: FormatSmartFnLockFlags(_applicationSettings.Store.SmartFnLockFlags)),
                new(
                    "SmartKeySinglePressActions",
                    "Smart Key single-press action",
                    "Choose the manual pipelines triggered by a single press of the Smart Key.",
                    AvaloniaSettingEditor.MultiSelection,
                    pipelines.Count > 0,
                    Values: pipelines.Select(item => item.Name).ToArray(),
                    SelectedValues: singleSelected,
                    Warning: pipelines.Count == 0 ? "No manual automation pipelines are configured." : null),
                new(
                    "SmartKeyDoublePressActions",
                    "Smart Key double-press action",
                    "Choose the manual pipelines triggered by a double press of the Smart Key.",
                    AvaloniaSettingEditor.MultiSelection,
                    pipelines.Count > 0,
                    Values: pipelines.Select(item => item.Name).ToArray(),
                    SelectedValues: doubleSelected,
                    Warning: pipelines.Count == 0 ? "No manual automation pipelines are configured." : null),
            ],
            true);
    }

    private async Task<IReadOnlyList<(Guid Id, string Name)>> GetManualPipelineOptionsAsync()
    {
        var options = new List<(Guid Id, string Name)> { (Guid.Empty, "This app") };
        var automation = IoCContainer.TryResolve<AutomationProcessor>();
        if (automation is null)
            return options;

        var pipelines = await automation.GetPipelinesAsync().ConfigureAwait(false);
        options.AddRange(pipelines
            .Where(pipeline => pipeline.Trigger is null)
            .OrderBy(pipeline => pipeline.Name, StringComparer.OrdinalIgnoreCase)
            .Select(pipeline =>
            {
                var name = PipelineNameLocalizer.LocalizeStoredName(pipeline.Name) ?? pipeline.Name;
                return (pipeline.Id, string.IsNullOrWhiteSpace(name) ? $"Pipeline {pipeline.Id:D}" : name);
            }));
        return options;
    }

    private static IReadOnlyList<string> GetSelectedPipelineNames(IReadOnlyList<Guid> ids, Guid? fallbackId, IReadOnlyList<(Guid Id, string Name)> options)
    {
        var selected = ids
            .Select(id => options.FirstOrDefault(option => option.Id == id).Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
        if (selected.Count == 0 && fallbackId is Guid id && id != Guid.Empty)
        {
            var name = options.FirstOrDefault(option => option.Id == id).Name;
            if (!string.IsNullOrWhiteSpace(name))
                selected.Add(name);
        }

        return selected.Count == 0 ? [options[0].Name] : selected;
    }

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

    private async Task<AvaloniaSettingsPageData> BuildApplicationPageAsync()
    {
        var store = _applicationSettings.Store;
        var (vantage, legionZone, fnKeys) = await GetSoftwareStatusesAsync().ConfigureAwait(false);
        return new AvaloniaSettingsPageData(
            "Application",
            "Application Behavior",
            "Configure how the application behaves on startup and during use.",
            [
                new("Autorun", "Launch at startup", "Automatically start the application when Windows starts.", AvaloniaSettingEditor.Selection, true,
                    Values: Enum.GetValues<AutorunState>().Select(value => value.ToString()).ToArray(), SelectedValue: Autorun.State.ToString()),
                new("MinimizeToTray", "Minimize to system tray", "Keep the application running in the system tray when it is minimized or closed.", AvaloniaSettingEditor.Toggle, true, store.MinimizeToTray),
                new("MinimizeOnClose", "Minimize on close", "Hide the window instead of exiting when the close button is pressed.", AvaloniaSettingEditor.Toggle, true, store.MinimizeOnClose),
                new("VantageDisabled", "Disable Lenovo Vantage", "Stop Lenovo Vantage services while Universal Device Toolkit controls the device.", AvaloniaSettingEditor.Toggle, vantage != SoftwareStatus.NotFound, vantage == SoftwareStatus.Disabled,
                    Warning: vantage == SoftwareStatus.NotFound ? "Lenovo Vantage was not detected." : null),
                new("LegionZoneDisabled", "Disable Legion Zone", "Stop Legion Zone services while Universal Device Toolkit controls the device.", AvaloniaSettingEditor.Toggle, legionZone != SoftwareStatus.NotFound, legionZone == SoftwareStatus.Disabled,
                    Warning: legionZone == SoftwareStatus.NotFound ? "Legion Zone was not detected." : null),
                new("FnKeysDisabled", "Disable Lenovo Fn keys service", "Stop the Lenovo hotkey service when Smart Keys are managed by this application.", AvaloniaSettingEditor.Toggle, fnKeys != SoftwareStatus.NotFound, fnKeys == SoftwareStatus.Disabled,
                    Warning: fnKeys == SoftwareStatus.NotFound ? "The Lenovo Fn keys service was not detected." : null),
                new("AnimationsEnabled", "Enable animations", "Use page and control transition animations throughout the application.", AvaloniaSettingEditor.Toggle, true, store.AnimationsEnabled),
                new("EnableHardwareSensors", "Enable hardware sensors", "Poll supported hardware sensors for dashboard readings.", AvaloniaSettingEditor.Toggle, true, store.EnableHardwareSensors),
                new("DisableUnsupportedHardwareWarning", "Disable compatibility warning", "Hide the warning shown when hardware-specific features are unavailable.", AvaloniaSettingEditor.Toggle, true, store.DisableUnsupportedHardwareWarning),
                new("ShowOsd", "Show on-screen display", "Show hardware status changes in the on-screen display.", AvaloniaSettingEditor.Toggle, true, _osdSettings.Store.ShowOsd),
            ],
            true);
    }

    private static async Task<(SoftwareStatus Vantage, SoftwareStatus LegionZone, SoftwareStatus FnKeys)> GetSoftwareStatusesAsync()
    {
        var vantage = IoCContainer.TryResolve<VantageDisabler>();
        var legionZone = IoCContainer.TryResolve<LegionZoneDisabler>();
        var fnKeys = IoCContainer.TryResolve<FnKeysDisabler>();
        var tasks = new[]
        {
            vantage?.GetStatusAsync() ?? Task.FromResult(SoftwareStatus.NotFound),
            legionZone?.GetStatusAsync() ?? Task.FromResult(SoftwareStatus.NotFound),
            fnKeys?.GetStatusAsync() ?? Task.FromResult(SoftwareStatus.NotFound),
        };
        await Task.WhenAll(tasks).ConfigureAwait(false);
        return (tasks[0].Result, tasks[1].Result, tasks[2].Result);
    }

    private static async Task SetSoftwareDisabledAsync<T>(bool disabled) where T : AbstractSoftwareDisabler
    {
        var disabler = IoCContainer.TryResolve<T>()
            ?? throw new PlatformNotSupportedException($"{typeof(T).Name} is not initialized.");
        if (disabled)
            await disabler.DisableAsync().ConfigureAwait(false);
        else
            await disabler.EnableAsync().ConfigureAwait(false);
    }

    private async Task<AvaloniaSettingsPageData> BuildDisplayPageAsync()
    {
        var store = _applicationSettings.Store;
        var refreshRates = await GetRefreshRatesAsync().ConfigureAwait(false);
        var currentRefreshRate = await GetCurrentRefreshRateAsync().ConfigureAwait(false);
        var bootLogoSupported = await BootLogo.IsSupportedAsync().ConfigureAwait(false);

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
                new("ExcludedRefreshRates", "Excluded refresh rates", "Do not use selected refresh rates for special-key cycling.", AvaloniaSettingEditor.MultiSelection, refreshRates.Length > 0,
                    Values: refreshRates,
                    SelectedValues: store.ExcludedRefreshRates.Select(rate => rate.DisplayName).ToArray(),
                    Warning: refreshRates.Length == 0 ? "No built-in display refresh rates were detected." : null),
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
                new("NotificationSuccess", "Success notifications", "Show successful operation notifications.", AvaloniaSettingEditor.Toggle, true, store.Notifications.SuccessNotifications),
                new("NotificationUpdateAvailable", "Update notifications", "Show notifications when a new application version is available.", AvaloniaSettingEditor.Toggle, true, store.Notifications.UpdateAvailable),
                new("NotificationCapsNumLock", "Caps and Num Lock notifications", "Show Caps Lock and Num Lock changes.", AvaloniaSettingEditor.Toggle, true, store.Notifications.CapsNumLock),
                new("NotificationFnLock", "Fn Lock notifications", "Show Fn Lock changes.", AvaloniaSettingEditor.Toggle, true, store.Notifications.FnLock),
                new("NotificationTouchpadLock", "Touchpad notifications", "Show touchpad lock changes.", AvaloniaSettingEditor.Toggle, true, store.Notifications.TouchpadLock),
                new("NotificationKeyboardBacklight", "Keyboard backlight notifications", "Show keyboard backlight changes.", AvaloniaSettingEditor.Toggle, true, store.Notifications.KeyboardBacklight),
                new("NotificationCameraLock", "Camera notifications", "Show camera state changes.", AvaloniaSettingEditor.Toggle, true, store.Notifications.CameraLock),
                new("NotificationMicrophone", "Microphone notifications", "Show microphone state changes.", AvaloniaSettingEditor.Toggle, true, store.Notifications.Microphone),
                new("NotificationPowerMode", "Power mode notifications", "Show power mode changes.", AvaloniaSettingEditor.Toggle, true, store.Notifications.PowerMode),
                new("NotificationRefreshRate", "Refresh rate notifications", "Show refresh rate changes.", AvaloniaSettingEditor.Toggle, true, store.Notifications.RefreshRate),
                new("NotificationACAdapter", "AC adapter notifications", "Show AC adapter changes.", AvaloniaSettingEditor.Toggle, true, store.Notifications.ACAdapter),
                new("NotificationSmartKey", "Smart Key notifications", "Show Smart Key actions.", AvaloniaSettingEditor.Toggle, true, store.Notifications.SmartKey),
                new("NotificationAutomation", "Automation notifications", "Show automation notifications.", AvaloniaSettingEditor.Toggle, true, store.Notifications.AutomationNotification),
                new("NavigationPaneExpanded", "Expanded navigation", "Keep the main navigation pane expanded.", AvaloniaSettingEditor.Toggle, true, store.NavigationPaneExpanded),
                new("Overdrive", "Display overdrive", "Enable panel overdrive on supported hardware.", AvaloniaSettingEditor.Toggle, false, Warning: "Display overdrive requires the Lenovo display adapter."),
                new("BootLogo", "Custom boot logo", "Choose and install a custom UEFI boot logo image.", AvaloniaSettingEditor.Action, bootLogoSupported, ActionText: "Choose image", Warning: bootLogoSupported ? null : "Boot logo controls are not available on this device."),
                new("BootLogoReset", "Restore default boot logo", "Remove the custom UEFI boot logo and restore the firmware default.", AvaloniaSettingEditor.Action, bootLogoSupported, ActionText: "Restore", Warning: bootLogoSupported ? null : "Boot logo controls are not available on this device."),
                ..navigationOptions,
            ],
            true);
    }

    private AvaloniaSettingsPageData BuildUpdatePage()
    {
        var store = _updateSettings.Store;
        var updateChecker = IoCContainer.TryResolve<UpdateChecker>();
        var isUpdateCheckerEnabled = updateChecker is not null && !updateChecker.Disable;
        var disabledReason = updateChecker?.Disable == true
            ? updateChecker.DisableReason ?? "Update checks are disabled for this session."
            : updateChecker is null
                ? "The update checker is not initialized in this host."
                : null;
        var frequencies = Enum.GetValues<UpdateCheckFrequency>()
            .Select(value => value.ToString())
            .ToArray();
        return new AvaloniaSettingsPageData(
            "Update",
            "Update",
            "Choose how Universal Device Toolkit checks for new releases.",
            [
                new("CheckForUpdates", "Check for updates", "Check for a newer release immediately.", AvaloniaSettingEditor.Action, isUpdateCheckerEnabled, ActionText: "Check now", Warning: disabledReason),
                new("UpdateFrequency", "Update check frequency", "How often automatic update checks run.", AvaloniaSettingEditor.Selection, isUpdateCheckerEnabled, Values: frequencies, SelectedValue: store.UpdateCheckFrequency.ToString(), Warning: disabledReason),
                new("IncludePrereleaseUpdates", "Include prerelease updates", "Offer preview releases in addition to stable releases.", AvaloniaSettingEditor.Toggle, isUpdateCheckerEnabled, store.IncludePrereleaseUpdates, Warning: disabledReason),
                new("RepositoryOwner", "Repository owner", "Override the update repository owner in debug builds.", AvaloniaSettingEditor.Text, isUpdateCheckerEnabled, TextValue: store.UpdateRepositoryOwner ?? "", Warning: disabledReason),
                new("RepositoryName", "Repository name", "Override the update repository name in debug builds.", AvaloniaSettingEditor.Text, isUpdateCheckerEnabled, TextValue: store.UpdateRepositoryName ?? "", Warning: disabledReason),
            ],
            true);
    }

    private async Task<AvaloniaSettingsPageData> BuildPowerPageAsync()
    {
        var store = _applicationSettings.Store;
        var mapping = FormatPowerModeMapping(store.PowerModeMappingMode);
        var powerModeFeature = IoCContainer.TryResolve<PowerModeFeature>();
        var powerModeSupported = false;
        try
        {
            powerModeSupported = powerModeFeature is not null && await powerModeFeature.IsSupportedAsync().ConfigureAwait(false);
        }
        catch
        {
            // Capability probing is best effort; unavailable hardware remains explicit in the UI.
        }

        var godModeSupported = false;
        var godModeEnabled = false;
        try
        {
            var machine = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
            godModeSupported = machine.Features[CapabilityID.GodModeFnQSwitchable];
            if (godModeSupported)
                godModeEnabled = await WMI.LenovoOtherMethod.GetFeatureValueAsync(CapabilityID.GodModeFnQSwitchable).ConfigureAwait(false) == 1;
        }
        catch
        {
            // Keep the control hidden when WMI cannot provide a reliable state.
        }

        var availabilityWarning = powerModeSupported
            ? null
            : "Power mode controls are not available on this device.";

        return new AvaloniaSettingsPageData(
            "Power",
            "Power",
            "Configure power mode mapping and battery behavior.",
            [
                new("GodModeFnQSwitchable", "GodMode Fn+Q switch", "Allow Fn+Q to switch into the GodMode power profile.", AvaloniaSettingEditor.Toggle, godModeSupported, godModeEnabled,
                    Warning: godModeSupported ? null : "This capability is not exposed by the current device."),
                new("PowerModeMapping", "Power mode mapping", "Choose how device power modes map to Windows.", AvaloniaSettingEditor.Selection, powerModeSupported,
                    Values: Enum.GetValues<PowerModeMappingMode>().Select(FormatPowerModeMapping).ToArray(), SelectedValue: mapping, Warning: availabilityWarning),
                new("ResetBatteryOnReboot", "Reset battery timer on reboot", "Reset the battery since timer after Windows restarts.", AvaloniaSettingEditor.Toggle, true, store.ResetBatteryOnSinceTimerOnReboot),
                new("OpenPowerModes", "Windows power modes", "Open the Windows power mode controls.", AvaloniaSettingEditor.Action, powerModeSupported, ActionText: "Open", Warning: availabilityWarning),
                new("OpenPowerPlans", "Windows power plans", "Open the classic Windows power plan controls.", AvaloniaSettingEditor.Action, powerModeSupported, ActionText: "Open", Warning: availabilityWarning),
                new("OpenPowerPlansControlPanel", "Power options control panel", "Open the Windows Power Options control panel.", AvaloniaSettingEditor.Action, true, ActionText: "Open"),
            ],
            true);
    }

    private static async Task SetGodModeFnQSwitchableAsync(bool enabled)
    {
        await WMI.LenovoOtherMethod.SetFeatureValueAsync(
            CapabilityID.GodModeFnQSwitchable,
            enabled ? 1 : 0).ConfigureAwait(false);
    }

    private static void OpenShellUri(string uri)
    {
        using var _ = Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }

    private static void OpenControlPanel(string arguments)
    {
        using var _ = Process.Start(new ProcessStartInfo("control", arguments) { UseShellExecute = true });
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

    private void SetNotificationToggle(string optionKey, bool value)
    {
        switch (optionKey)
        {
            case "NotificationSuccess": _applicationSettings.Store.Notifications.SuccessNotifications = value; break;
            case "NotificationUpdateAvailable": _applicationSettings.Store.Notifications.UpdateAvailable = value; break;
            case "NotificationCapsNumLock": _applicationSettings.Store.Notifications.CapsNumLock = value; break;
            case "NotificationFnLock": _applicationSettings.Store.Notifications.FnLock = value; break;
            case "NotificationTouchpadLock": _applicationSettings.Store.Notifications.TouchpadLock = value; break;
            case "NotificationKeyboardBacklight": _applicationSettings.Store.Notifications.KeyboardBacklight = value; break;
            case "NotificationCameraLock": _applicationSettings.Store.Notifications.CameraLock = value; break;
            case "NotificationMicrophone": _applicationSettings.Store.Notifications.Microphone = value; break;
            case "NotificationPowerMode": _applicationSettings.Store.Notifications.PowerMode = value; break;
            case "NotificationRefreshRate": _applicationSettings.Store.Notifications.RefreshRate = value; break;
            case "NotificationACAdapter": _applicationSettings.Store.Notifications.ACAdapter = value; break;
            case "NotificationSmartKey": _applicationSettings.Store.Notifications.SmartKey = value; break;
            case "NotificationAutomation": _applicationSettings.Store.Notifications.AutomationNotification = value; break;
            default: throw new KeyNotFoundException($"Unknown notification option '{optionKey}'.");
        }
    }

    private static RefreshRate ParseRefreshRate(string value)
    {
        var normalized = value.Replace(" Hz", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var frequency) && frequency > 0
            ? new RefreshRate(frequency)
            : throw new ArgumentException($"Invalid refresh rate '{value}'.", nameof(value));
    }

    private static string FormatTemperatureUnit(TemperatureUnit value) => value switch
    {
        TemperatureUnit.F => "\u00B0F",
        _ => "\u00B0C",
    };

    private static IReadOnlyList<string> GetSmartFnLockValues(ModifierKey current)
    {
        var values = new List<string> { "Off", "Alt", "Alt + Ctrl + Shift" };
        var formattedCurrent = FormatSmartFnLockFlags(current);
        if (!values.Contains(formattedCurrent, StringComparer.OrdinalIgnoreCase))
            values.Add(formattedCurrent);
        return values;
    }

    private static string FormatSmartFnLockFlags(ModifierKey value) => value switch
    {
        ModifierKey.None => "Off",
        ModifierKey.Alt => "Alt",
        ModifierKey.Alt | ModifierKey.Ctrl | ModifierKey.Shift => "Alt + Ctrl + Shift",
        _ => string.Join(" + ", new[]
        {
            value.HasFlag(ModifierKey.Alt) ? "Alt" : null,
            value.HasFlag(ModifierKey.Ctrl) ? "Ctrl" : null,
            value.HasFlag(ModifierKey.Shift) ? "Shift" : null,
        }.Where(part => part is not null)),
    };

    private static ModifierKey ParseSmartFnLockFlags(string value)
    {
        var normalized = value.Trim();
        if (string.Equals(normalized, "Off", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, nameof(ModifierKey.None), StringComparison.OrdinalIgnoreCase))
            return ModifierKey.None;

        var result = ModifierKey.None;
        var parts = normalized.Replace('+', ',').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            result |= part.Trim().ToLowerInvariant() switch
            {
                "alt" => ModifierKey.Alt,
                "ctrl" or "control" => ModifierKey.Ctrl,
                "shift" => ModifierKey.Shift,
                _ => throw new ArgumentException($"Unknown Smart Fn-lock modifier '{part}'.", nameof(value)),
            };
        }

        return result == ModifierKey.None
            ? throw new ArgumentException($"Unknown Smart Fn-lock value '{value}'.", nameof(value))
            : result;
    }

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
        var cliHostAvailable = IoCContainer.TryResolve<ICliHostLifecycle>() is not null;
        return new AvaloniaSettingsPageData(
            "Integrations",
            "Integrations",
            "Connect Universal Device Toolkit to supported external tools and services.",
            [
                new("HWiNFO", "HWiNFO integration", "Expose hardware sensor data through HWiNFO when available.", AvaloniaSettingEditor.Toggle, true, store.HWiNFO),
                new("CLI", "CLI interface", "Enable the local command-line interface.", AvaloniaSettingEditor.Toggle, cliHostAvailable, store.CLI,
                    Warning: cliHostAvailable ? null : "The CLI host service is not initialized for this Avalonia host."),
                new("CLIPath", "Add CLI to PATH", "Add or remove the command-line tools from the current user's PATH.", AvaloniaSettingEditor.Toggle, true, SystemPath.HasCLI()),
            ],
            true);
    }
}

#endif
