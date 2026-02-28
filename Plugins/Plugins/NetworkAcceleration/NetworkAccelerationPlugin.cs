using System;
using System.Diagnostics;
using System.Threading.Tasks;
using LenovoLegionToolkit.Plugins.SDK;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration;

[Plugin(
    id: "network-acceleration",
    name: "Network Acceleration",
    version: "1.0.4",
    description: "Real-time network acceleration and optimization features",
    author: "LenovoLegionToolkit Team",
    MinimumHostVersion = "3.6.1",
    Icon = "Rocket24"
)]
public class NetworkAccelerationPlugin : LenovoLegionToolkit.Plugins.SDK.PluginBase
{
    public override string Id => "network-acceleration";
    public override string Name => NetworkAccelerationText.PluginName;
    public override string Description => NetworkAccelerationText.PluginDescription;
    public override string Icon => "Rocket24";
    public override bool IsSystemPlugin => false;

    private NetworkAccelerationSettings _settings;

    public NetworkAccelerationSettings Settings => _settings;

    public NetworkAccelerationPlugin()
    {
        _settings = LoadSettings();
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
        _settings = NetworkAccelerationSettings.CreateDefault();
        _ = SaveSettingsAsync();
    }

    public bool SetPreferredMode(NetworkAccelerationMode mode)
    {
        _settings.PreferredMode = mode;
        return true;
    }

    public bool SetAutoOptimizeOnStartup(bool value)
    {
        _settings.AutoOptimizeOnStartup = value;
        return true;
    }

    public bool SetResetWinsockOnOptimize(bool value)
    {
        _settings.ResetWinsockOnOptimize = value;
        return true;
    }

    public bool SetResetTcpIpOnOptimize(bool value)
    {
        _settings.ResetTcpIpOnOptimize = value;
        return true;
    }

    public async Task<bool> RunQuickOptimizationAsync()
    {
        var flushResult = await RunCommandAsync("ipconfig", "/flushdns").ConfigureAwait(false);
        if (!flushResult)
            return false;

        if (_settings.ResetWinsockOnOptimize)
        {
            var winsockResult = await RunCommandAsync("netsh", "winsock reset").ConfigureAwait(false);
            if (!winsockResult)
                return false;
        }

        if (_settings.ResetTcpIpOnOptimize)
        {
            var tcpResult = await RunCommandAsync("netsh", "int ip reset").ConfigureAwait(false);
            if (!tcpResult)
                return false;
        }

        return true;
    }

    public async Task<bool> ResetNetworkStackAsync()
    {
        var winsockResult = await RunCommandAsync("netsh", "winsock reset").ConfigureAwait(false);
        var tcpResult = await RunCommandAsync("netsh", "int ip reset").ConfigureAwait(false);
        return winsockResult && tcpResult;
    }

    public async Task SaveSettingsAsync()
    {
        Configuration.SetValue(nameof(NetworkAccelerationSettings.PreferredMode), _settings.PreferredMode.ToString());
        Configuration.SetValue(nameof(NetworkAccelerationSettings.AutoOptimizeOnStartup), _settings.AutoOptimizeOnStartup);
        Configuration.SetValue(nameof(NetworkAccelerationSettings.ResetWinsockOnOptimize), _settings.ResetWinsockOnOptimize);
        Configuration.SetValue(nameof(NetworkAccelerationSettings.ResetTcpIpOnOptimize), _settings.ResetTcpIpOnOptimize);
        await Configuration.SaveAsync().ConfigureAwait(false);
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

    private static async Task<bool> RunCommandAsync(string fileName, string arguments)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync().ConfigureAwait(false);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

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
