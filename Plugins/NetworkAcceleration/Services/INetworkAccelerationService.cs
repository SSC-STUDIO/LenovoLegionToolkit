using System;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services;

/// <summary>
/// Network acceleration service interface
/// </summary>
public interface INetworkAccelerationService : IDisposable
{
    /// <summary>
    /// Whether the service is currently running
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Start the network acceleration service
    /// </summary>
    Task<bool> StartAsync();

    /// <summary>
    /// Stop the network acceleration service
    /// </summary>
    Task<bool> StopAsync();

    /// <summary>
    /// Get current service status
    /// </summary>
    string GetStatus();

    /// <summary>
    /// Get service status description
    /// </summary>
    string GetStatusDescription();
}

