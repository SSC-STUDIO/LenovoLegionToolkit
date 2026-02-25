using System;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.ReverseProxy;

/// <summary>
/// Reverse proxy service interface
/// </summary>
public interface IReverseProxyService : IDisposable
{
    /// <summary>
    /// Whether the proxy service is currently running
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Start the reverse proxy service
    /// </summary>
    Task<bool> StartAsync();

    /// <summary>
    /// Stop the reverse proxy service
    /// </summary>
    Task<bool> StopAsync();

    /// <summary>
    /// Get the proxy port
    /// </summary>
    ushort ProxyPort { get; }

    /// <summary>
    /// Get the proxy IP address
    /// </summary>
    string ProxyIp { get; }
}

