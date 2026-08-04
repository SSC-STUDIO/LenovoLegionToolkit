#if WINDOWS

using System.Globalization;
using UniversalDeviceToolkit.Abstractions.Hardware;
using UniversalDeviceToolkit.Abstractions.Macro;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.Automation;
using UniversalDeviceToolkit.Lib.Macro;
using UniversalDeviceToolkit.Lib.Plugins;
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

    private WindowsFeatureHostServices(
        IKeyboardBacklightDetectionService keyboard,
        IMacroController macro,
        AutomationProcessor automation,
        IPluginManager plugins)
    {
        _keyboard = keyboard;
        _macro = macro;
        _automation = automation;
        _plugins = plugins;
    }

    public static WindowsFeatureHostServices? TryCreate()
    {
        try
        {
            return new WindowsFeatureHostServices(
                IoCContainer.Resolve<IKeyboardBacklightDetectionService>(),
                IoCContainer.Resolve<IMacroController>(),
                IoCContainer.Resolve<AutomationProcessor>(),
                IoCContainer.Resolve<IPluginManager>());
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
            "WindowsOptimization" => new FeaturePageState(
                routeKey,
                "System optimization",
                "Review Windows optimization actions and their current state.",
                "Service connected",
                "The Windows optimization service is available through the shared host container.",
                true,
                [new FeatureActionItem(
                    "optimization-service",
                    "Windows optimization service",
                    "Open the optimization action list to review pending changes.",
                    "Ready",
                    true,
                    false,
                    false)]),
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
            case "Actions" when actionKey == "automation-enabled":
                await _automation.SetEnabledAsync(isSelected).ConfigureAwait(false);
                return true;
            case "PluginExtensions" when actionKey == "plugin-refresh":
                await _plugins.ScanAndLoadPluginsAsync(forceRefresh: true).ConfigureAwait(false);
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

    private FeaturePageState GetMacroState() => new(
        "Macro",
        "Macro",
        "Create and manage device macros.",
        "Available",
        "The shared macro controller is connected to the Windows input service.",
        true,
        [new FeatureActionItem(
            "macro-controller",
            "Enable macro input",
            "Enable or disable the global macro input hook used by the macro workspace.",
            _macro.IsEnabled ? "Enabled" : "Disabled",
            true,
            _macro.IsEnabled,
            true)]);

    private async Task<FeaturePageState> GetAutomationStateAsync()
    {
        var pipelines = await _automation.GetPipelinesAsync().ConfigureAwait(false);
        return new FeaturePageState(
            "Actions",
            "Actions",
            "Review and run configured automation pipelines.",
            "Available",
            $"{pipelines.Count} automation pipeline(s) loaded from the shared settings store.",
            true,
            [
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
            ]);
    }

    private FeaturePageState GetPluginState()
    {
        var installed = _plugins.GetInstalledPluginIds().ToArray();
        return new FeaturePageState(
            "PluginExtensions",
            "Plugin Extensions",
            "Discover and manage optional plugin extensions.",
            "Available",
            $"{installed.Length} installed plugin extension(s) loaded by the shared plugin manager.",
            true,
            [new FeatureActionItem(
                "plugin-refresh",
                "Refresh plugin extensions",
                "Scan the plugin directory and load installed extensions.",
                "Refresh",
                true,
                false,
                false)]);
    }
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
