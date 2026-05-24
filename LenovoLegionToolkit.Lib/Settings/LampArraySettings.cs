// Derived from Lenovo Legion Toolkit.
// Original project copyright: Copyright (C) Bartosz Cichecki and contributors.
// Upstream sync copyright: Copyright (C) 2026 LenovoLegionToolkit-Team.
// Modifications copyright: Copyright (C) 2026 Universal Device Toolkit Contributors.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Settings;

public class LampArraySettings : AbstractSettings<LampArraySettings.LampArraySettingsStore>
{
    public class LampEffectConfig
    {
        public LampEffectType EffectType { get; set; } = LampEffectType.Rainbow;
        public Dictionary<string, object> Parameters { get; set; } = [];
    }

    public class LampArraySettingsStore
    {
        public double Brightness { get; set; } = 1.0;
        public double Speed { get; set; } = 1.0;
        public bool SmoothTransition { get; set; } = true;
        public LampEffectConfig? DefaultEffect { get; set; }
        public Dictionary<int, LampEffectConfig> PerLampEffects { get; set; } = [];
    }

    public LampArraySettings() : base("lamp_array.json") { }

    public override LampArraySettingsStore? LoadStore() => Normalize(base.LoadStore());

    public override async Task<LampArraySettingsStore?> LoadStoreAsync() =>
        Normalize(await base.LoadStoreAsync().ConfigureAwait(false));

    public void ExportToFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var json = JsonSerializer.Serialize(Store, JsonSerializerOptions);
        File.WriteAllText(path, json);
    }

    public void ImportFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException("Profile file not found.", path);

        var json = File.ReadAllText(path);
        var imported = JsonSerializer.Deserialize<LampArraySettingsStore>(json, JsonSerializerOptions);

        if (imported is null)
            throw new InvalidOperationException("Failed to deserialize profile.");

        imported = Normalize(imported) ?? new LampArraySettingsStore();

        var store = Store;
        store.Brightness = imported.Brightness;
        store.Speed = imported.Speed;
        store.SmoothTransition = imported.SmoothTransition;
        store.DefaultEffect = imported.DefaultEffect;
        store.PerLampEffects = imported.PerLampEffects ?? [];
        SynchronizeStore();
    }

    private static LampArraySettingsStore? Normalize(LampArraySettingsStore? store)
    {
        if (store is null)
            return null;

        store.PerLampEffects ??= [];
        return store;
    }
}
