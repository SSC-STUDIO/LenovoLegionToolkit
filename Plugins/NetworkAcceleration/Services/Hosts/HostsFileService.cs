using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Plugins.NetworkAcceleration.Services.Hosts;

/// <summary>
/// Hosts file service implementation
/// </summary>
public class HostsFileService : IHostsFileService
{
    private const string HostsFilePath = @"C:\Windows\System32\drivers\etc\hosts";
    private const string ServiceTag = "# LenovoLegionToolkit Network Acceleration";

    public async Task UpdateHostsAsync(Dictionary<string, string>? domainMappings = null)
    {
        if (domainMappings == null || domainMappings.Count == 0)
        {
            return;
        }

        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Updating hosts file...");

            // Read existing hosts file
            var lines = new List<string>();
            if (File.Exists(HostsFilePath))
            {
                lines = (await File.ReadAllLinesAsync(HostsFilePath)).ToList();
            }

            // Remove old service entries
            lines.RemoveAll(line => line.Contains(ServiceTag));

            // Add new entries
            lines.Add(string.Empty);
            lines.Add(ServiceTag);
            foreach (var mapping in domainMappings)
            {
                lines.Add($"{mapping.Value}\t{mapping.Key}\t{ServiceTag}");
            }

            // Write back to file (requires admin privileges)
            await File.WriteAllLinesAsync(HostsFilePath, lines);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Updated hosts file with {domainMappings.Count} entries.");
        }
        catch (UnauthorizedAccessException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Access denied when updating hosts file. Administrator privileges required.");
            throw;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error updating hosts file: {ex.Message}", ex);
            throw;
        }
    }

    public async Task RestoreHostsAsync()
    {
        try
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Restoring hosts file...");

            if (!File.Exists(HostsFilePath))
            {
                return;
            }

            // Read existing hosts file
            var lines = (await File.ReadAllLinesAsync(HostsFilePath))
                .Where(line => !line.Contains(ServiceTag))
                .ToList();

            // Write back without service entries
            await File.WriteAllLinesAsync(HostsFilePath, lines);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Hosts file restored.");
        }
        catch (UnauthorizedAccessException)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Access denied when restoring hosts file. Administrator privileges required.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error restoring hosts file: {ex.Message}", ex);
        }
    }

    public bool ContainsServiceEntries()
    {
        try
        {
            if (!File.Exists(HostsFilePath))
            {
                return false;
            }

            var content = File.ReadAllText(HostsFilePath);
            return content.Contains(ServiceTag);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error checking hosts file: {ex.Message}", ex);
            return false;
        }
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

