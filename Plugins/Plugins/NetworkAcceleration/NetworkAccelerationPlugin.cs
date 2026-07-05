using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Plugins.SDK;
using LenovoLegionToolkit.Plugins.Shared;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

[Plugin(
    id: "network-acceleration",
    name: "Network Acceleration",
    version: "1.1.9",
    description: "Real-time network acceleration and optimization features",
    author: "SSC-STUDIO",
    MinimumHostVersion = "3.6.1",
    Icon = "Rocket24"
)]
public class NetworkAccelerationPlugin : LenovoLegionToolkit.Plugins.SDK.PluginBase, IAppStartupPlugin
{
    private static readonly ProcessRunner SharedProcessRunner = new();

    public override string Id => "network-acceleration";
    public override string Name => NetworkAccelerationText.PluginName;
    public override string Description => NetworkAccelerationText.PluginDescription;
    public override string Icon => "Rocket24";
    public override bool IsSystemPlugin => false;

    private NetworkAccelerationSettings _settings;
    private readonly object _settingsLock = new();
    private readonly NetworkAccelerationRuntime _runtime = new();

    public NetworkAccelerationSettings Settings
    {
        get
        {
            lock (_settingsLock)
            {
                return _settings.Clone();
            }
        }
    }
    public NetworkAccelerationRuntime Runtime => _runtime;

    public NetworkAccelerationPlugin()
    {
        PluginLog.Configure(
            isTraceEnabled: () => LenovoLegionToolkit.Lib.Utils.Log.Instance.IsTraceEnabled,
            trace: (message, exception) => LenovoLegionToolkit.Lib.Utils.Log.Instance.Trace(message, exception));

        lock (_settingsLock)
        {
            _settings = LoadSettings();
        }
    }

    public override object? GetFeatureExtension()
    {
        return new NetworkAccelerationPluginPage(this);
    }

    public override object? GetSettingsPage()
    {
        return new NetworkAccelerationSettingsPluginPage(this);
    }

    public override void OnInstalled()
    {
        lock (_settingsLock)
        {
            _settings = NetworkAccelerationSettings.CreateDefault();
        }
        RunBackgroundTask(nameof(OnInstalled), SaveSettingsAsync);
    }

    public void OnAppStarted()
    {
        _runtime.Start();

        bool autoOptimize;
        lock (_settingsLock)
        {
            autoOptimize = _settings.AutoOptimizeOnStartup;
        }

        if (autoOptimize)
            RunBackgroundTask(nameof(OnAppStarted), () => RunQuickOptimizationAsync(GetRuntimeCancellationToken()));
    }

    public override void OnShutdown()
    {
        _runtime.Stop();
    }

    public override void Stop()
    {
        _runtime.Stop();
    }

    protected override CancellationToken GetRuntimeCancellationToken()
    {
        return _runtime.GetCancellationToken();
    }

    public bool SetPreferredMode(NetworkAccelerationMode mode)
    {
        lock (_settingsLock)
        {
            _settings = _settings.With(preferredMode: mode);
        }
        return true;
    }

    public bool SetAutoOptimizeOnStartup(bool value)
    {
        lock (_settingsLock)
        {
            _settings = _settings.With(autoOptimizeOnStartup: value);
        }
        return true;
    }

    public bool SetResetWinsockOnOptimize(bool value)
    {
        lock (_settingsLock)
        {
            _settings = _settings.With(resetWinsockOnOptimize: value);
        }
        return true;
    }

    public bool SetResetTcpIpOnOptimize(bool value)
    {
        lock (_settingsLock)
        {
            _settings = _settings.With(resetTcpIpOnOptimize: value);
        }
        return true;
    }

    public NetworkOptimizationPlan GetOptimizationPlan()
    {
        lock (_settingsLock)
        {
            return GetOptimizationPlan(_settings);
        }
    }

    internal static NetworkOptimizationPlan GetOptimizationPlan(NetworkAccelerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var steps = new List<NetworkOptimizationStep>
        {
            new("FlushDns", "ipconfig.exe", "/flushdns", true)
        };

        if (settings.ResetWinsockOnOptimize || settings.PreferredMode == NetworkAccelerationMode.Gaming)
            steps.Add(new("ResetWinsock", "netsh.exe", "winsock reset", settings.ResetWinsockOnOptimize));

        if (settings.ResetTcpIpOnOptimize || settings.PreferredMode == NetworkAccelerationMode.Streaming)
            steps.Add(new("ResetTcpIp", "netsh.exe", "int ip reset", settings.ResetTcpIpOnOptimize));

        return new NetworkOptimizationPlan(settings.PreferredMode, steps);
    }

    public Task<bool> RunQuickOptimizationAsync()
    {
        return RunQuickOptimizationAsync(CancellationToken.None);
    }

    public async Task<bool> RunQuickOptimizationAsync(CancellationToken cancellationToken)
    {
        foreach (var step in GetOptimizationPlan().Steps)
        {
            var result = await RunCommandAsync(step.ExecutableName, step.Arguments, cancellationToken).ConfigureAwait(false);
            if (!result)
                return false;
        }

        return true;
    }

    public Task<bool> ResetNetworkStackAsync()
    {
        return ResetNetworkStackAsync(CancellationToken.None);
    }

    public async Task<bool> ResetNetworkStackAsync(CancellationToken cancellationToken)
    {
        var winsockResult = await RunCommandAsync("netsh.exe", "winsock reset", cancellationToken).ConfigureAwait(false);
        var tcpResult = await RunCommandAsync("netsh.exe", "int ip reset", cancellationToken).ConfigureAwait(false);
        return winsockResult && tcpResult;
    }

    public async Task SaveSettingsAsync()
    {
        NetworkAccelerationSettings settingsToPersist;
        lock (_settingsLock)
        {
            settingsToPersist = _settings.Clone();
        }
        Configuration.SetValue(nameof(NetworkAccelerationSettings.PreferredMode), settingsToPersist.PreferredMode.ToString());
        Configuration.SetValue(nameof(NetworkAccelerationSettings.AutoOptimizeOnStartup), settingsToPersist.AutoOptimizeOnStartup);
        Configuration.SetValue(nameof(NetworkAccelerationSettings.ResetWinsockOnOptimize), settingsToPersist.ResetWinsockOnOptimize);
        Configuration.SetValue(nameof(NetworkAccelerationSettings.ResetTcpIpOnOptimize), settingsToPersist.ResetTcpIpOnOptimize);
        await Configuration.SaveAsync().ConfigureAwait(false);
    }

    public async Task ApplySettingsAsync(NetworkAccelerationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_settingsLock)
        {
            _settings = settings.Clone();
        }
        await SaveSettingsAsync().ConfigureAwait(false);
    }

    private NetworkAccelerationSettings LoadSettings()
    {
        var modeRaw = Configuration.GetValue(nameof(NetworkAccelerationSettings.PreferredMode), NetworkAccelerationMode.Balanced.ToString());
        if (!Enum.TryParse(modeRaw, true, out NetworkAccelerationMode mode))
            mode = NetworkAccelerationMode.Balanced;

        return new NetworkAccelerationSettings
        {
            PreferredMode = mode,
            AutoOptimizeOnStartup = Configuration.GetValue(nameof(NetworkAccelerationSettings.AutoOptimizeOnStartup), false),
            ResetWinsockOnOptimize = Configuration.GetValue(nameof(NetworkAccelerationSettings.ResetWinsockOnOptimize), true),
            ResetTcpIpOnOptimize = Configuration.GetValue(nameof(NetworkAccelerationSettings.ResetTcpIpOnOptimize), false),
        };
    }

    private static async Task<bool> RunCommandAsync(string executableName, string arguments, CancellationToken cancellationToken)
    {
        var executablePath = ResolveTrustedSystemExecutablePath(executableName);
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            PluginLog.Trace($"NetworkAcceleration: Unable to resolve trusted system command path for {executableName}.");
            return false;
        }

        var result = await SharedProcessRunner.RunProcessAsync(
            executablePath,
            arguments,
            cancellationToken,
            Constants.ProcessTimeoutSeconds).ConfigureAwait(false);

        if (!result.Success)
        {
            PluginLog.Trace(
                $"NetworkAcceleration: Command failed: {Path.GetFileName(executablePath)} {arguments}. Error: {result.Error}");
        }

        return result.Success;
    }

    private static string? ResolveTrustedSystemExecutablePath(string executableName)
    {
        if (string.IsNullOrWhiteSpace(executableName))
            return null;

        var normalizedName = executableName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? executableName
            : $"{executableName}.exe";

        var systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
        if (string.IsNullOrWhiteSpace(systemDirectory))
            return null;

        var candidate = Path.Combine(systemDirectory, normalizedName);
        return File.Exists(candidate) ? candidate : null;
    }

    private static void RunBackgroundTask(string operationName, Func<Task> action)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                PluginLog.Trace($"NetworkAcceleration: Background operation '{operationName}' was cancelled.");
            }
            catch (Exception ex)
            {
                PluginLog.Trace($"NetworkAcceleration: Background operation '{operationName}' failed: {ex.Message}", ex);
            }
        });
    }
}

public sealed record NetworkOptimizationStep(
    string Key,
    string ExecutableName,
    string Arguments,
    bool Required);

public sealed record NetworkOptimizationPlan(
    NetworkAccelerationMode Mode,
    IReadOnlyList<NetworkOptimizationStep> Steps);

public class NetworkAccelerationPluginPage : LenovoLegionToolkit.Plugins.SDK.IPluginPage
{
    private readonly NetworkAccelerationPlugin _plugin;

    public NetworkAccelerationPluginPage(NetworkAccelerationPlugin plugin)
    {
        _plugin = plugin;
    }

    public string PageTitle => NetworkAccelerationText.PageTitle;
    public string? PageIcon => "Rocket24";

    public object CreatePage()
    {
        return new NetworkAccelerationControl(_plugin);
    }
}

public class NetworkAccelerationSettingsPluginPage : LenovoLegionToolkit.Plugins.SDK.IPluginPage
{
    private readonly NetworkAccelerationPlugin _plugin;

    public NetworkAccelerationSettingsPluginPage(NetworkAccelerationPlugin plugin)
    {
        _plugin = plugin;
    }

    public string PageTitle => NetworkAccelerationText.SettingsPageTitle;
    public string? PageIcon => "Settings24";

    public object CreatePage()
    {
        return new NetworkAccelerationSettingsControl(_plugin);
    }
}

public enum NetworkAccelerationMode
{
    Balanced,
    Gaming,
    Streaming
}

public class NetworkAccelerationSettings
{
    public NetworkAccelerationMode PreferredMode { get; set; } = NetworkAccelerationMode.Balanced;
    public bool AutoOptimizeOnStartup { get; set; }
    public bool ResetWinsockOnOptimize { get; set; } = true;
    public bool ResetTcpIpOnOptimize { get; set; }

    public NetworkAccelerationSettings With(
        NetworkAccelerationMode? preferredMode = null,
        bool? autoOptimizeOnStartup = null,
        bool? resetWinsockOnOptimize = null,
        bool? resetTcpIpOnOptimize = null)
    {
        return new NetworkAccelerationSettings
        {
            PreferredMode = preferredMode ?? PreferredMode,
            AutoOptimizeOnStartup = autoOptimizeOnStartup ?? AutoOptimizeOnStartup,
            ResetWinsockOnOptimize = resetWinsockOnOptimize ?? ResetWinsockOnOptimize,
            ResetTcpIpOnOptimize = resetTcpIpOnOptimize ?? ResetTcpIpOnOptimize
        };
    }

    public NetworkAccelerationSettings Clone()
    {
        return With();
    }

    public static NetworkAccelerationSettings CreateDefault()
    {
        return new NetworkAccelerationSettings
        {
            PreferredMode = NetworkAccelerationMode.Balanced,
            AutoOptimizeOnStartup = false,
            ResetWinsockOnOptimize = true,
            ResetTcpIpOnOptimize = false
        };
    }
}
