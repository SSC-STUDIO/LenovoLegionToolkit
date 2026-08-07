using System.Collections.Generic;
using UniversalDeviceToolkit.Shared.Settings;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Per-plugin language overrides persisted as plugin-languages.json in the
/// shared app-data folder. Mirrors the WPF PluginSettings store shape.
/// </summary>
internal sealed class PluginLanguageSettings : AbstractSettings<PluginLanguageSettings.PluginLanguageSettingsStore>
{
    private readonly string? _settingsFilePath;

    public PluginLanguageSettings() : base("plugin-languages.json")
    {
    }

    internal PluginLanguageSettings(string settingsFilePath) : base("plugin-languages.json")
    {
        _settingsFilePath = settingsFilePath;
    }

    protected override string SettingsFilePath => _settingsFilePath ?? base.SettingsFilePath;

    public override PluginLanguageSettingsStore? LoadStore() => Normalize(base.LoadStore());

    public override async Task<PluginLanguageSettingsStore?> LoadStoreAsync() =>
        Normalize(await base.LoadStoreAsync().ConfigureAwait(false));

    public class PluginLanguageSettingsStore
    {
        /// <summary>
        /// Dictionary mapping plugin ID to culture name (e.g., "zh-Hans", "en").
        /// A missing entry means the plugin uses the application language.
        /// </summary>
        public Dictionary<string, string> PluginLanguages { get; set; } = new();
    }

    private static PluginLanguageSettingsStore? Normalize(PluginLanguageSettingsStore? store)
    {
        if (store is null)
            return null;

        store.PluginLanguages ??= new();
        return store;
    }
}
