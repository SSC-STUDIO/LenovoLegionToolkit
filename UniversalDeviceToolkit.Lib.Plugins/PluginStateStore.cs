using System;
using System.Collections.Generic;
using UniversalDeviceToolkit.Shared.Settings;

namespace UniversalDeviceToolkit.Lib.Plugins;

/// <summary>
/// Portable plugin state store (installed/pending-deletion extension ids) for
/// non-Windows builds, where the Windows ApplicationSettings class is not
/// compiled. Persists to a JSON file next to the regular settings files.
/// Windows builds keep the ApplicationSettings-backed store (PluginManager.cs).
/// </summary>
public sealed class PluginStateStore : AbstractSettings<PluginStateStore.PluginStateStoreData>
{
    public sealed class PluginStateStoreData
    {
        public List<string> InstalledExtensions { get; set; } = [];
        public List<string> PendingDeletionExtensions { get; set; } = [];
    }

    public PluginStateStore() : base("plugin-state.json")
    {
    }

    public override PluginStateStoreData? LoadStore()
    {
        var store = base.LoadStore();
        if (store is not null)
        {
            store.InstalledExtensions ??= [];
            store.PendingDeletionExtensions ??= [];
        }
        return store;
    }
}
