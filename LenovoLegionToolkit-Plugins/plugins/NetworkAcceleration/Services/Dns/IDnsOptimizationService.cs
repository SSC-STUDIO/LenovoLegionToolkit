using System;
using System.Net;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Dns;

/// <summary>
/// DNS optimization service interface
/// </summary>
public interface IDnsOptimizationService : IDisposable
{
    /// <summary>
    /// Whether DNS optimization is enabled
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Start DNS optimization
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Stop DNS optimization
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Enable or disable DNS optimization
    /// </summary>
    void SetEnabled(bool enabled);

    /// <summary>
    /// Resolve a hostname using optimized DNS (DoH if enabled)
    /// </summary>
    Task<IPAddress[]?> ResolveAsync(string hostname);
}

