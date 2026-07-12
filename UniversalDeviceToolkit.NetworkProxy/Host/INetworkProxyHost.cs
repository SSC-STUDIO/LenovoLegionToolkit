namespace UniversalDeviceToolkit.NetworkProxy.Host;

/// <summary>Loopback HTTP/CONNECT proxy host lifecycle.</summary>
public interface INetworkProxyHost : IAsyncDisposable
{
    bool IsRunning { get; }

    int ListenPort { get; }

    string StatusSummary { get; }

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync();
}
