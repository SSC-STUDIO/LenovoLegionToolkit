#if WINDOWS

using System.Globalization;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Abstractions.Macro;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
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
    private readonly IMacroController _macro;
    private readonly AutomationProcessor _automation;
    private readonly IPluginManager _plugins;
    private readonly WindowsOptimizationService? _optimization;
    private readonly SemaphoreSlim _automationInitializationLock = new(1, 1);
    private bool _automationInitialized;
    private static readonly ulong[] MacroKeys = [0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69];

    private WindowsFeatureHostServices(
        IKeyboardBacklightDetectionService keyboard,
        IMacroController macro,
        AutomationProcessor automation,
        IPluginManager plugins,
        WindowsOptimizationService? optimization)
    {
        _keyboard = keyboard;
        _macro = macro;
        _automation = automation;
        _plugins = plugins;
        _optimization = optimization;
    }

    public static WindowsFeatureHostServices? TryCreate()
    {
        try
        {
            return new WindowsFeatureHostServices(
                IoCContainer.Resolve<IKeyboardBacklightDetectionService>(),
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
        switch (routeKey)
        {
            case "Macro" when actionKey == "macro-controller":
                _macro.SetEnabled(isSelected);
                return true;
            case "Macro" when actionKey == "macro-record" && _macro is MacroController recordingController:
                if (recordingController.IsRecording)
                    return false;

                recordingController.StartRecording(MacroRecorderSettings.Keyboard);
                return true;
            case "Macro" when actionKey == "macro-stop-recording" && _macro is MacroController stoppingController:
                if (!stoppingController.IsRecording)
                    return false;

                stoppingController.StopRecording();
                return true;
            case "Macro" when actionKey.StartsWith("macro-key:", StringComparison.OrdinalIgnoreCase)
                                 && _macro is MacroController playbackController:
                if (!ulong.TryParse(actionKey["macro-key:".Length..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var macroKey))
                    return false;

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
        return new FeaturePageState(
            "Keyboard",
            "Keyboard",
            "Configure keyboard backlight and keyboard-specific controls.",
            spectrum || rgb ? "Available" : "Unavailable on this device",
            status,
            spectrum || rgb,
            [new FeatureActionItem(
                "keyboard-backlight",
                "Keyboard backlight detection",
                "The shared Windows keyboard service reports the detected backlight mode.",
                status,
                false,
                false,
                false)]);
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
                "macro-record",
                "Record keyboard sequence",
                "Capture keyboard input into the selected macro slot. Stop recording from the same workspace action.",
                controller.IsRecording ? "Recording" : "Record",
                !controller.IsRecording,
                false,
                false));
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
                    true));
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
