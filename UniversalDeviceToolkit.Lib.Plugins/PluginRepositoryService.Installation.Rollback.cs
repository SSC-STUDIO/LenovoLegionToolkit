using System;
using System.IO;
using System.Threading.Tasks;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Lib.Plugins;

public partial class PluginRepositoryService
{
    /// <summary>
    /// Restores the previous plugin directory after an interrupted installation.
    /// Rollback is isolated from the install flow so cleanup failures cannot hide
    /// the original installation failure.
    /// </summary>
    private static Task RestorePluginDirectoryAsync(string pluginDir, string? backupDir, string pluginId)
    {
        try
        {
            if (Directory.Exists(pluginDir))
                Directory.Delete(pluginDir, true);

            if (!string.IsNullOrWhiteSpace(backupDir) && Directory.Exists(backupDir))
            {
                Directory.Move(backupDir, pluginDir);

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Rolled back plugin directory for {pluginId} from backup {backupDir}.");
            }
        }
        catch (Exception restoreEx)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to roll back plugin directory for {pluginId}: {restoreEx.Message}", restoreEx);
        }

        return Task.CompletedTask;
    }
}
