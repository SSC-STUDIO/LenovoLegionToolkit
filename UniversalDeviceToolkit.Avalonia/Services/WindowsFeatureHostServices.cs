#if WINDOWS

using System.Globalization;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Abstractions.Macro;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Controllers;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.Lib.Optimization;
using UniversalDeviceToolkit.Lib.Plugins;
using LibResource = UniversalDeviceToolkit.Lib.Resources.Resource;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Bridges Avalonia feature routes to the same Windows services used by the WPF host.
/// The bridge is optional: if the full service container cannot be created, callers retain
/// the read-only device-adapter state instead of preventing the shell from starting.
/// </summary>
internal sealed class WindowsFeatureHostServices
{
    private readonly IKeyboardBacklightDetectionService _keyboard;
    private readonly SpectrumKeyboardBacklightController? _spectrum;
    private readonly RGBKeyboardBacklightController? _rgb;
    private readonly IMacroController _macro;
    private readonly AutomationProcessor _automation;
    private readonly IPluginManager _plugins;
    private readonly WindowsOptimizationService? _optimization;
    private readonly SemaphoreSlim _automationInitializationLock = new(1, 1);
    private readonly object _macroRecordingLock = new();
    private ulong? _macroRecordingKey;
    private List<MacroEvent>? _macroRecordingEvents;
    private bool _automationInitialized;
    private static readonly ulong[] MacroKeys = [0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69];

    private WindowsFeatureHostServices(
        IKeyboardBacklightDetectionService keyboard,
        SpectrumKeyboardBacklightController? spectrum,
        RGBKeyboardBacklightController? rgb,
        IMacroController macro,
        AutomationProcessor automation,
        IPluginManager plugins,
        WindowsOptimizationService? optimization)
    {
        _keyboard = keyboard;
        _spectrum = spectrum;
        _rgb = rgb;
        _macro = macro;
        _automation = automation;
        _plugins = plugins;
        _optimization = optimization;

        if (_macro is MacroController macroController)
        {
            macroController.RecorderReceived += MacroController_RecorderReceived;
            macroController.RecorderStopped += MacroController_RecorderStopped;
        }
    }

    public static WindowsFeatureHostServices? TryCreate()
    {
        try
        {
            return new WindowsFeatureHostServices(
                IoCContainer.Resolve<IKeyboardBacklightDetectionService>(),
                IoCContainer.TryResolve<SpectrumKeyboardBacklightController>(),
                IoCContainer.TryResolve<RGBKeyboardBacklightController>(),
                IoCContainer.Resolve<IMacroController>(),
                IoCContainer.Resolve<AutomationProcessor>(),
                IoCContainer.Resolve<IPluginManager>(),
                IoCContainer.TryResolve<WindowsOptimizationService>());
        }
        catch
        {
            return null;
        }
    }

    public async Task<FeaturePageState> GetStateAsync(string routeKey)
    {
        return routeKey switch
        {
            "Keyboard" => await GetKeyboardStateAsync(),
            "Macro" => GetMacroState(),
            "Actions" => await GetAutomationStateAsync(),
            "PluginExtensions" => GetPluginState(),
            "WindowsOptimization" => await GetOptimizationStateAsync(),
            _ => throw new ArgumentOutOfRangeException(nameof(routeKey), routeKey, "Unknown feature route."),
        };
    }

    public async Task<bool> SetActionAsync(string routeKey, string actionKey, bool isSelected)
    {
        try
        {
            return await SetActionCoreAsync(routeKey, actionKey, isSelected).ConfigureAwait(false);
        }
        catch
        {
            // Feature cards are user-triggered controls. Report a rejected action
            // to the page so it can keep the current state and show its tooltip;
            // never surface a host-service exception from an async UI event.
            return false;
        }
    }

    private async Task<bool> SetActionCoreAsync(string routeKey, string actionKey, bool isSelected)
    {
        switch (routeKey)
        {
            case "Keyboard" when actionKey == "keyboard-spectrum-brightness-up" && _spectrum is not null:
                await AdjustSpectrumBrightnessAsync(_spectrum, 1).ConfigureAwait(false);
                return true;
            case "Keyboard" when actionKey == "keyboard-spectrum-brightness-down" && _spectrum is not null:
                await AdjustSpectrumBrightnessAsync(_spectrum, -1).ConfigureAwait(false);
                return true;
            case "Keyboard" when actionKey == "keyboard-spectrum-logo" && _spectrum is not null:
                await _spectrum.SetLogoStatusAsync(isSelected).ConfigureAwait(false);
                return true;
            case "Keyboard" when actionKey.StartsWith("keyboard-rgb-preset:", StringComparison.OrdinalIgnoreCase)
                                 && _rgb is not null
                                 && Enum.TryParse<RGBKeyboardBacklightPreset>(actionKey["keyboard-rgb-preset:".Length..], true, out var preset):
                await _rgb.SetPresetAsync(preset).ConfigureAwait(false);
                return true;
            case "Macro" when actionKey == "macro-controller":
                _macro.SetEnabled(isSelected);
                return true;
            case "Macro" when actionKey == "macro-record" && _macro is MacroController recordingController:
                return StartMacroRecording(recordingController, 0x60);
            case "Macro" when FeatureActionContract.TryParseMacroRecordKey(actionKey, out var recordingKey)
                                 && _macro is MacroController recordingController:
                return StartMacroRecording(recordingController, recordingKey);
            case "Macro" when actionKey == "macro-stop-recording" && _macro is MacroController stoppingController:
                if (!stoppingController.IsRecording)
                    return false;

                stoppingController.StopRecording();
                return true;
            case "Macro" when FeatureActionContract.TryParseMacroPlayKey(actionKey, out var macroKey)
                                 && _macro is MacroController playbackController:
                return playbackController.TryPlaySequence(macroKey);
            case "Actions" when actionKey == "automation-enabled":
                await EnsureAutomationInitializedAsync().ConfigureAwait(false);
                await _automation.SetEnabledAsync(isSelected).ConfigureAwait(false);
                return true;
            case "Actions" when actionKey.StartsWith("automation-pipeline:", StringComparison.OrdinalIgnoreCase):
                if (!Guid.TryParse(actionKey["automation-pipeline:".Length..], out var pipelineId))
                    return false;

                await EnsureAutomationInitializedAsync().ConfigureAwait(false);
                var pipeline = (await _automation.GetPipelinesAsync().ConfigureAwait(false))
                    .FirstOrDefault(candidate => candidate.Id == pipelineId);
                if (pipeline is null)
                    return false;

                await _automation.RunNowAsync(pipeline).ConfigureAwait(false);
                return true;
            case "PluginExtensions" when actionKey == "plugin-refresh":
                await _plugins.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);
                return true;
            case "PluginExtensions" when actionKey == "plugin-check-updates":
                await _plugins.CheckForUpdatesAsync().ConfigureAwait(false);
                return true;
            case "PluginExtensions" when actionKey.StartsWith("plugin-reload:", StringComparison.OrdinalIgnoreCase):
                var reloadId = actionKey["plugin-reload:".Length..];
                if (string.IsNullOrWhiteSpace(reloadId) || !_plugins.IsInstalled(reloadId))
                    return false;

                await _plugins.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);
                return true;
            case "PluginExtensions" when actionKey.StartsWith("plugin-install:", StringComparison.OrdinalIgnoreCase):
                var installId = actionKey["plugin-install:".Length..];
                if (string.IsNullOrWhiteSpace(installId))
                    return false;

                _plugins.InstallPlugin(installId);
                return true;
            case "PluginExtensions" when actionKey.StartsWith("plugin-uninstall:", StringComparison.OrdinalIgnoreCase):
                var uninstallId = actionKey["plugin-uninstall:".Length..];
                if (string.IsNullOrWhiteSpace(uninstallId) || !_plugins.IsInstalled(uninstallId))
                    return false;

                return _plugins.UninstallPlugin(uninstallId);
            case "WindowsOptimization" when _optimization is not null:
                var action = _optimization.GetCategories()
                    .SelectMany(category => category.Actions)
                    .FirstOrDefault(candidate => candidate.Key.Equals(actionKey, StringComparison.OrdinalIgnoreCase));
                if (action is null)
                    return false;

                if (isSelected)
                {
                    await _optimization.ApplyActionAsync(action.Key, CancellationToken.None).ConfigureAwait(false);
                    return true;
                }

                if (action.RollbackAsync is null)
                    return false;

                await _optimization.RevertActionAsync(action.Key, CancellationToken.None).ConfigureAwait(false);
                return true;
            default:
                return false;
        }
    }

    private async Task<FeaturePageState> GetKeyboardStateAsync()
    {
        var spectrum = await _keyboard.IsSpectrumSupportedAsync().ConfigureAwait(false);
        var rgb = !spectrum && await _keyboard.IsRgbSupportedAsync().ConfigureAwait(false);
        var status = spectrum ? "Spectrum supported" : rgb ? "RGB supported" : "No compatible keyboard detected";
        var actions = new List<FeatureActionItem>();

        if (spectrum && _spectrum is not null)
        {
            var brightness = -1;
            try
            {
                brightness = await _spectrum.GetBrightnessAsync().ConfigureAwait(false);
            }
            catch
            {
                // Keep controls visible even when a transient device read fails.
            }

            actions.Add(new FeatureActionItem(
                "keyboard-spectrum-brightness-down",
                "Decrease keyboard brightness",
                brightness >= 0 ? $"Current Spectrum brightness: {brightness}/9." : "Decrease the Spectrum keyboard brightness.",
                "Decrease",
                brightness != 0,
                false,
                false));
            actions.Add(new FeatureActionItem(
                "keyboard-spectrum-brightness-up",
                "Increase keyboard brightness",
                brightness >= 0 ? $"Current Spectrum brightness: {brightness}/9." : "Increase the Spectrum keyboard brightness.",
                "Increase",
                brightness < 9,
                false,
                false));

            try
            {
                var logoEnabled = await _spectrum.GetLogoStatusAsync().ConfigureAwait(false);
                actions.Add(new FeatureActionItem(
                    "keyboard-spectrum-logo",
                    "Keyboard logo lighting",
                    "Turn the Spectrum keyboard logo lighting on or off.",
                    logoEnabled ? "On" : "Off",
                    true,
                    logoEnabled,
                    true));
            }
            catch
            {
                // Logo support varies by device generation.
            }
        }
        else if (rgb && _rgb is not null)
        {
            RGBKeyboardBacklightPreset? selectedPreset = null;
            try
            {
                selectedPreset = (await _rgb.GetStateAsync().ConfigureAwait(false)).SelectedPreset;
            }
            catch
            {
                // Presets remain available even when the current state cannot be read.
            }

            foreach (var preset in Enum.GetValues<RGBKeyboardBacklightPreset>())
            {
                actions.Add(new FeatureActionItem(
                    $"keyboard-rgb-preset:{preset}",
                    $"RGB preset: {preset}",
                    "Apply this RGB keyboard backlight preset.",
                    selectedPreset == preset ? "Selected" : "Apply",
                    true,
                    false,
                    false));
            }
        }

        return new FeaturePageState(
            "Keyboard",
            "Keyboard",
            "Configure keyboard backlight and keyboard-specific controls.",
            spectrum || rgb ? "Available" : "Unavailable on this device",
            status,
            spectrum || rgb,
            actions.Count == 0
                ? [new FeatureActionItem(
                    "keyboard-backlight",
                    "Keyboard backlight detection",
                    "The shared Windows keyboard service reports the detected backlight mode.",
                    status,
                    false,
                    false,
                    false)]
                : actions);
    }

    public async Task<KeyboardLightingState?> GetKeyboardLightingStateAsync()
    {
        if (_spectrum is not null && await _spectrum.IsSupportedAsync().ConfigureAwait(false))
        {
            var profile = await _spectrum.GetProfileAsync().ConfigureAwait(false);
            var brightness = await _spectrum.GetBrightnessAsync().ConfigureAwait(false);
            var logoEnabled = await _spectrum.GetLogoStatusAsync().ConfigureAwait(false);
            var (_, effects) = await _spectrum.GetProfileDescriptionAsync(profile).ConfigureAwait(false);
            return new KeyboardLightingState(
                "Spectrum",
                brightness,
                logoEnabled,
                profile,
                effects.Select(effect => new KeyboardSpectrumEffectState(
                    effect.Type.ToString(),
                    effect.Speed.ToString(),
                    effect.Direction.ToString(),
                    effect.ClockwiseDirection.ToString(),
                    effect.Colors.Select(ToKeyboardColor).ToArray(),
                    effect.Keys)).ToArray(),
                []);
        }

        if (_rgb is not null && await _rgb.IsSupportedAsync().ConfigureAwait(false))
        {
            var state = await _rgb.GetStateAsync().ConfigureAwait(false);
            var presets = Enum.GetValues<RGBKeyboardBacklightPreset>()
                .Select(preset =>
                {
                    var description = state.Presets.GetValueOrDefault(
                        preset,
                        RGBKeyboardBacklightBacklightPresetDescription.Default);
                    var zones = preset == RGBKeyboardBacklightPreset.Off
                        ? []
                        : new[]
                        {
                            ToKeyboardColor(description.Zone1),
                            ToKeyboardColor(description.Zone2),
                            ToKeyboardColor(description.Zone3),
                            ToKeyboardColor(description.Zone4),
                        };
                    return new KeyboardRgbPresetState(
                        preset.ToString(),
                        preset.ToString(),
                        state.SelectedPreset == preset,
                        description.Effect.ToString(),
                        description.Speed.ToString(),
                        description.Brightness.ToString(),
                        zones);
                })
                .ToArray();

            return new KeyboardLightingState("RGB", 0, false, 0, [], presets);
        }

        return null;
    }

    public async Task<bool> SetKeyboardLightingAsync(KeyboardLightingUpdate update)
    {
        if (update.Mode.Equals("Spectrum", StringComparison.OrdinalIgnoreCase) && _spectrum is not null)
        {
            if (!await _spectrum.IsSupportedAsync().ConfigureAwait(false))
                return false;

            if (update.SelectedProfile is { } profile && update.SpectrumEffects is null)
                await _spectrum.SetProfileAsync(profile).ConfigureAwait(false);
            if (update.Brightness is { } brightness)
                await _spectrum.SetBrightnessAsync(Math.Clamp(brightness, 0, 9)).ConfigureAwait(false);
            if (update.LogoEnabled is { } logoEnabled)
                await _spectrum.SetLogoStatusAsync(logoEnabled).ConfigureAwait(false);
            if (update.SpectrumEffects is not null)
            {
                var selectedProfile = update.SelectedProfile ?? await _spectrum.GetProfileAsync().ConfigureAwait(false);
                var effects = new List<SpectrumKeyboardBacklightEffect>();
                foreach (var item in update.SpectrumEffects)
                {
                    if (!Enum.TryParse<SpectrumKeyboardBacklightEffectType>(item.Type, true, out var type)
                        || !Enum.TryParse<SpectrumKeyboardBacklightSpeed>(item.Speed, true, out var speed)
                        || !Enum.TryParse<SpectrumKeyboardBacklightDirection>(item.Direction, true, out var direction)
                        || !Enum.TryParse<SpectrumKeyboardBacklightClockwiseDirection>(item.ClockwiseDirection, true, out var clockwise))
                    {
                        return false;
                    }

                    effects.Add(new SpectrumKeyboardBacklightEffect(
                        type,
                        speed,
                        direction,
                        clockwise,
                        item.Colors.Select(ToRgbColor).ToArray(),
                        item.Keys.ToArray()));
                }

                await _spectrum.SetProfileDescriptionAsync(selectedProfile, effects.ToArray()).ConfigureAwait(false);
            }

            return true;
        }

        if (update.Mode.Equals("RGB", StringComparison.OrdinalIgnoreCase) && _rgb is not null)
        {
            if (!await _rgb.IsSupportedAsync().ConfigureAwait(false))
                return false;

            var state = await _rgb.GetStateAsync().ConfigureAwait(false);
            var selected = state.SelectedPreset;
            if (!string.IsNullOrWhiteSpace(update.RgbPreset)
                && !Enum.TryParse(update.RgbPreset, true, out selected))
            {
                return false;
            }

            if (update.RgbEffect is null && update.RgbSpeed is null
                && update.RgbBrightness is null && update.RgbZones is null)
            {
                await _rgb.SetPresetAsync(selected).ConfigureAwait(false);
                return true;
            }

            var current = state.Presets.GetValueOrDefault(
                selected,
                RGBKeyboardBacklightBacklightPresetDescription.Default);
            if (!Enum.TryParse(update.RgbEffect ?? current.Effect.ToString(), true, out RGBKeyboardBacklightEffect effect)
                || !Enum.TryParse(update.RgbSpeed ?? current.Speed.ToString(), true, out RGBKeyboardBacklightSpeed speed)
                || !Enum.TryParse(update.RgbBrightness ?? current.Brightness.ToString(), true, out RGBKeyboardBacklightBrightness brightness))
            {
                return false;
            }

            var zones = update.RgbZones?.Count >= 4
                ? update.RgbZones.Take(4).Select(ToRgbColor).ToArray()
                : [current.Zone1, current.Zone2, current.Zone3, current.Zone4];
            var presets = new Dictionary<RGBKeyboardBacklightPreset, RGBKeyboardBacklightBacklightPresetDescription>(state.Presets)
            {
                [selected] = new(effect, speed, brightness, zones[0], zones[1], zones[2], zones[3]),
            };
            await _rgb.SetStateAsync(new(selected, presets)).ConfigureAwait(false);
            return true;
        }

        return false;
    }

    private static KeyboardColorState ToKeyboardColor(RGBColor color) => new(color.R, color.G, color.B);

    private static RGBColor ToRgbColor(KeyboardColorState color) => new(color.R, color.G, color.B);

    private static async Task AdjustSpectrumBrightnessAsync(SpectrumKeyboardBacklightController controller, int delta)
    {
        var current = await controller.GetBrightnessAsync().ConfigureAwait(false);
        await controller.SetBrightnessAsync(Math.Clamp(current + delta, 0, 9)).ConfigureAwait(false);
    }

    private bool StartMacroRecording(MacroController controller, ulong key)
    {
        if (controller.IsRecording)
            return false;

        lock (_macroRecordingLock)
        {
            _macroRecordingKey = key;
            _macroRecordingEvents = [];
        }

        controller.StartRecording(MacroRecorderSettings.Keyboard);
        if (controller.IsRecording)
            return true;

        lock (_macroRecordingLock)
        {
            _macroRecordingKey = null;
            _macroRecordingEvents = null;
        }

        return false;
    }

    private void MacroController_RecorderReceived(object? sender, MacroController.RecorderReceivedEventArgs e)
    {
        lock (_macroRecordingLock)
        {
            if (_macroRecordingKey is not null)
                _macroRecordingEvents?.Add(e.MacroEvent);
        }
    }

    private void MacroController_RecorderStopped(object? sender, MacroController.RecorderStoppedEventArgs e)
    {
        ulong? key;
        List<MacroEvent>? events;
        lock (_macroRecordingLock)
        {
            key = _macroRecordingKey;
            events = _macroRecordingEvents;
            _macroRecordingKey = null;
            _macroRecordingEvents = null;
        }

        if (e.Interrupted || key is not { } macroKey || events is null)
            return;

        var controller = _macro as MacroController;
        if (controller is null)
            return;

        var sequences = controller.GetSequences();
        var identifier = new MacroIdentifier(MacroSource.Keyboard, macroKey);
        sequences.TryGetValue(identifier, out var existing);
        sequences[identifier] = new MacroSequence
        {
            RepeatCount = Math.Max(1, existing.RepeatCount),
            IgnoreDelays = existing.IgnoreDelays,
            InterruptOnOtherKey = existing.InterruptOnOtherKey,
            Events = [.. events],
        };
        controller.SetSequences(sequences);
    }

    private FeaturePageState GetMacroState()
    {
        var controller = _macro as MacroController;
        var sequences = controller?.GetSequences();
        var actions = new List<FeatureActionItem>
        {
            new FeatureActionItem(
                "macro-controller",
                "Enable macro input",
                "Enable or disable the global macro input hook used by the macro workspace.",
                _macro.IsEnabled ? "Enabled" : "Disabled",
                true,
                _macro.IsEnabled,
                true),
        };

        if (controller is null)
        {
            actions.Add(new FeatureActionItem(
                "macro-controller-status",
                "Macro workspace",
                "The host macro controller does not expose sequence editing on this adapter.",
                "Unavailable",
                false,
                false,
                false));
        }
        else
        {
            actions.Add(new FeatureActionItem(
                "macro-stop-recording",
                "Stop recording",
                "Stop the active macro recording and persist the captured sequence.",
                "Stop",
                controller.IsRecording,
                false,
                false));

            foreach (var key in MacroKeys)
            {
                var identifier = new MacroIdentifier(MacroSource.Keyboard, key);
                sequences!.TryGetValue(identifier, out var sequence);
                var eventCount = sequence.Events?.Length ?? 0;
                var digit = key - 0x60;
                var title = $"Numpad {digit}";
                var description = eventCount == 0
                    ? "No sequence is stored for this macro slot."
                    : $"{eventCount} recorded event(s), repeats {Math.Max(1, sequence.RepeatCount)} time(s). Click Play to send it through the shared macro player.";
                actions.Add(new FeatureActionItem(
                    $"macro-key:{key:X}",
                    title,
                    description,
                    eventCount == 0 ? "Empty" : "Play",
                    eventCount > 0,
                    false,
                    false));
                actions.Add(new FeatureActionItem(
                    $"macro-record:{key:X}",
                    $"Record Numpad {digit}",
                    $"Capture keyboard input into Numpad {digit}. Stop recording to save the sequence.",
                    controller.IsRecording ? "Recording" : "Record",
                    !controller.IsRecording,
                    false,
                    false));
            }
        }

        var populated = sequences?.Count ?? 0;
        return new FeaturePageState(
            "Macro",
            "Macro",
            "Create and manage device macros.",
            "Available",
            $"The shared macro controller is connected. {populated} keyboard sequence(s) are stored.",
            true,
            actions);
    }

    private async Task<FeaturePageState> GetAutomationStateAsync()
    {
        await EnsureAutomationInitializedAsync().ConfigureAwait(false);
        var pipelines = await _automation.GetPipelinesAsync().ConfigureAwait(false);
        var actions = new List<FeatureActionItem>
        {
            new FeatureActionItem(
                "automation-enabled",
                "Automation service",
                "Enable or disable automation event listeners.",
                _automation.IsEnabled ? "Enabled" : "Disabled",
                true,
                _automation.IsEnabled,
                true),
            new FeatureActionItem(
                "pipeline-count",
                "Configured pipelines",
                "Pipelines are loaded from the same automation store used by WPF.",
                pipelines.Count.ToString(CultureInfo.InvariantCulture),
                false,
                false,
                false),
        };

        foreach (var pipeline in pipelines)
        {
            var name = string.IsNullOrWhiteSpace(pipeline.Name)
                ? $"Pipeline {pipeline.Id.ToString()[..8]}"
                : pipeline.Name!;
            var trigger = pipeline.Trigger?.DisplayName ?? "Manual quick action";
            var stepCount = pipeline.Steps.Count;
            actions.Add(new FeatureActionItem(
                $"automation-pipeline:{pipeline.Id:D}",
                name,
                $"{trigger}. {stepCount} step(s). Run this pipeline using the shared automation processor.",
                "Run",
                true,
                false,
                false));
        }

        return new FeaturePageState(
            "Actions",
            "Actions",
            "Review and run configured automation pipelines.",
            "Available",
            $"{pipelines.Count} automation pipeline(s) loaded from the shared settings store.",
            true,
            actions);
    }

    private async Task EnsureAutomationInitializedAsync()
    {
        if (_automationInitialized)
            return;

        await _automationInitializationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_automationInitialized)
            {
                await _automation.InitializeAsync().ConfigureAwait(false);
                _automationInitialized = true;
            }
        }
        finally
        {
            _automationInitializationLock.Release();
        }
    }

    private FeaturePageState GetPluginState()
    {
        var registered = _plugins.GetRegisteredPlugins().ToArray();
        var installedIds = _plugins.GetInstalledPluginIds()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var pluginIds = registered.Select(plugin => plugin.Id)
            .Concat(installedIds)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var actions = new List<FeatureActionItem>
        {
            new FeatureActionItem(
                "plugin-refresh",
                "Refresh plugin extensions",
                "Scan the plugin directory and load installed extensions.",
                "Refresh",
                true,
                false,
                false),
            new FeatureActionItem(
                "plugin-check-updates",
                "Check for plugin updates",
                "Ask the shared plugin manager for available updates.",
                "Check",
                true,
                false,
                false),
        };

        foreach (var pluginId in pluginIds)
        {
            var plugin = registered.FirstOrDefault(candidate =>
                candidate.Id.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
            var metadata = _plugins.GetPluginMetadata(pluginId);
            var name = metadata?.GetDisplayName(LocalizationRuntime.CurrentCulture)
                ?? plugin?.Name
                ?? pluginId;
            var description = metadata?.GetDisplayDescription(LocalizationRuntime.CurrentCulture)
                ?? plugin?.Description
                ?? "No plugin description was provided by the host.";
            var version = metadata?.Version;
            var author = metadata?.Author;
            var details = string.Join(" ", new[]
            {
                string.IsNullOrWhiteSpace(version) ? null : $"Version {version}.",
                string.IsNullOrWhiteSpace(author) ? null : $"Author: {author}.",
                description,
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
            var installed = _plugins.IsInstalled(pluginId);
            var systemPlugin = metadata?.IsSystemPlugin == true || plugin?.IsSystemPlugin == true;
            actions.Add(new FeatureActionItem(
                installed ? $"plugin-uninstall:{pluginId}" : $"plugin-install:{pluginId}",
                name,
                details,
                installed ? (systemPlugin ? "System" : "Uninstall") : "Install",
                installed ? !systemPlugin : true,
                false,
                false));
            if (installed)
            {
                actions.Add(new FeatureActionItem(
                    $"plugin-reload:{pluginId}",
                    $"Reload {name}",
                    "Rescan the plugin directory and reload this installed extension through the shared plugin manager.",
                    "Reload",
                    true,
                    false,
                    false));
            }
        }

        var installedCount = installedIds.Length;
        return new FeaturePageState(
            "PluginExtensions",
            "Plugin Extensions",
            "Discover and manage optional plugin extensions.",
            "Available",
            $"{installedCount} installed plugin extension(s) loaded by the shared plugin manager.",
            true,
            actions);
    }

    private async Task<FeaturePageState> GetOptimizationStateAsync()
    {
        if (_optimization is null)
        {
            return new FeaturePageState(
                "WindowsOptimization",
                "System optimization",
                "Review Windows optimization actions and their current state.",
                "Unavailable on this device",
                "The Windows optimization service could not be resolved by the host container.",
                false,
                []);
        }

        var actions = new List<FeatureActionItem>();
        foreach (var category in _optimization.GetCategories())
        {
            foreach (var action in category.Actions)
            {
                var applied = false;
                if (action.IsAppliedAsync is not null)
                {
                    try
                    {
                        applied = await action.IsAppliedAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch
                    {
                        // A failed probe keeps the action visible and safely unselected.
                    }
                }

                actions.Add(new FeatureActionItem(
                    action.Key,
                    ResolveResource(action.TitleResourceKey),
                    ResolveResource(action.DescriptionResourceKey),
                    applied ? "Applied" : action.Recommended ? "Recommended" : "Available",
                    true,
                    applied,
                    FeatureActionContract.IsToggleAction(action.RollbackAsync is not null)));
            }
        }

        return new FeaturePageState(
            "WindowsOptimization",
            "System optimization",
            "Review Windows optimization actions and their current state.",
            "Available",
            $"{actions.Count} Windows optimization action(s) loaded from the shared service.",
            true,
            actions);
    }

    private static string ResolveResource(string key) =>
        LibResource.ResourceManager.GetString(key, LocalizationRuntime.CurrentCulture) ?? key;
}

internal sealed class AvaloniaMainThreadDispatcher : IMainThreadDispatcher
{
    public void Dispatch(Action callback) => global::Avalonia.Threading.Dispatcher.UIThread.Invoke(callback);

    public Task DispatchAsync(Func<Task> callback)
    {
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        global::Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                await callback().ConfigureAwait(true);
                completion.TrySetResult(null);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });
        return completion.Task;
    }
}

#endif
