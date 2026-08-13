using UniversalDeviceToolkit.Lib.Plugins;

namespace UniversalDeviceToolkit.Tests.PluginFixture;

public sealed class LoaderFixturePlugin : IPlugin, IAppStartupPlugin
{
    private bool _installationPrepared;
    public int StartCallCount { get; private set; }
    public int StopCallCount { get; private set; }
    public int InstalledCallCount { get; private set; }
    public int UninstalledCallCount { get; private set; }
    public string Id => "loader-fixture";
    public string Name => "Loader fixture";
    public string Description => "Exercises real plugin loader context arbitration.";
    public string Icon => string.Empty;
    public bool IsSystemPlugin => false;
    public string[]? Dependencies => null;

    public void OnInstalled()
    {
        InstalledCallCount++;
        _installationPrepared = true;
    }

    public void OnUninstalled()
    {
        UninstalledCallCount++;
        _installationPrepared = false;
    }

    public void Stop()
    {
        StopCallCount++;
    }

    public void OnShutdown()
    {
    }

    public void OnAppStarted()
    {
        StartCallCount++;
        const string failOnceVariable = "UDT_LOADER_FIXTURE_FAIL_START_ONCE";
        if (string.Equals(
                Environment.GetEnvironmentVariable(failOnceVariable),
                "1",
                StringComparison.Ordinal))
        {
            Environment.SetEnvironmentVariable(failOnceVariable, null);
            throw new InvalidOperationException("Requested one-time fixture startup failure.");
        }
        if (!_installationPrepared)
            throw new InvalidOperationException("Installation lifecycle was not prepared.");
    }
}
