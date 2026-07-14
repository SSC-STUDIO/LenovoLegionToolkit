using UniversalDeviceToolkit.Lib.Settings;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace UniversalDeviceToolkit.WPF.Settings;

/// <summary>
/// Plugin-specific settings (like language per plugin)
/// </summary>
public class PluginSettings : AbstractSettings<PluginSettings.PluginSettingsStore>
{
    public PluginSettings() : base("plugins.json")
    {
    }

    public class PluginSettingsStore
    {
        /// <summary>
        /// Dictionary mapping plugin ID to culture name (e.g., "zh-hans", "en")
        /// If a plugin doesn't have an entry, it uses the application's default language
        /// </summary>
        public Dictionary<string, string> PluginLanguages { get; set; } = new();
    }

    public override PluginSettingsStore? LoadStore() => Normalize(base.LoadStore());

    public override async Task<PluginSettingsStore?> LoadStoreAsync() =>
        Normalize(await base.LoadStoreAsync().ConfigureAwait(false));

    /// <summary>
    /// Get the culture for a specific plugin
    /// Returns null if plugin should use application default language
    /// </summary>
    public CultureInfo? GetPluginCulture(string pluginId)
    {
        if (Store.PluginLanguages.TryGetValue(pluginId, out var cultureName) && !string.IsNullOrWhiteSpace(cultureName))
        {
            try
            {
                return new CultureInfo(cultureName);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Set the culture for a specific plugin
    /// Pass null to use application default language
    /// </summary>
    public void SetPluginCulture(string pluginId, CultureInfo? cultureInfo)
    {
        if (cultureInfo == null)
        {
            Store.PluginLanguages.Remove(pluginId);
        }
        else
        {
            Store.PluginLanguages[pluginId] = cultureInfo.Name;
        }
        SynchronizeStore();
    }

    private static PluginSettingsStore? Normalize(PluginSettingsStore? store)
    {
        if (store is null)
            return null;

        store.PluginLanguages ??= new();
        return store;
    }
}
