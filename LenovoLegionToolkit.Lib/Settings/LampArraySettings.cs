using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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
        public LampEffectConfig DefaultEffect { get; set; } = new();
        public Dictionary<int, LampEffectConfig> PerLampEffects { get; set; } = [];
    }

    public LampArraySettings() : base("lamp_array.json") { }

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

        var store = Store;
        store.Brightness = imported.Brightness;
        store.Speed = imported.Speed;
        store.SmoothTransition = imported.SmoothTransition;
        store.DefaultEffect = imported.DefaultEffect;
        store.PerLampEffects = imported.PerLampEffects;
        SynchronizeStore();
    }
}
