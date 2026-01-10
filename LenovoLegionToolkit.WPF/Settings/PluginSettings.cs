using LenovoLegionToolkit.Lib.Settings;
using System.Collections.Generic;
using System.Globalization;

namespace LenovoLegionToolkit.WPF.Settings;

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
}
