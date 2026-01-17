using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Hosts;

/// <summary>
/// Hosts file service interface
/// </summary>
public interface IHostsFileService : IDisposable
{
    /// <summary>
    /// Update hosts file with domain mappings
    /// </summary>
    Task UpdateHostsAsync(Dictionary<string, string>? domainMappings = null);

    /// <summary>
    /// Restore hosts file to original state
    /// </summary>
    Task RestoreHostsAsync();

    /// <summary>
    /// Check if hosts file contains entries added by this service
    /// </summary>
    bool ContainsServiceEntries();
}

