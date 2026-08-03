using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
    private string GetPluginsDirectory() => PluginPaths.GetPluginsDirectory();

    /// <summary>
    /// Check for plugin updates.
    /// </summary>
    public async Task<List<PluginManifest>> CheckForUpdatesAsync(
        List<PluginManifest> installedPlugins,
        bool forceRefresh = false)
    {
        var availablePlugins = await FetchAvailablePluginsAsync(forceRefresh).ConfigureAwait(false);
        var updates = new List<PluginManifest>();

        foreach (var installed in installedPlugins)
        {
            var available = availablePlugins.FirstOrDefault(p =>
                string.Equals(p.Id, installed.Id, StringComparison.OrdinalIgnoreCase));
            if (available is null)
                continue;

            if (PluginVersionParser.IsNewerThan(available.Version, installed.Version))
                updates.Add(available);
        }

        return updates;
    }
}
