using System.Text.Json;
using LenovoLegionToolkit.Lib.Serialization;
using static LenovoLegionToolkit.Lib.Settings.GPUOverclockSettings;

namespace LenovoLegionToolkit.Lib.Settings;

public class GPUOverclockSettings() : AbstractSettings<GPUOverclockSettingsStore>("gpu_oc.json")
{
    protected override void ConfigureJsonSerializerOptions(JsonSerializerOptions options)
    {
        options.Converters.Add(new GPUOverclockInfoJsonConverter());
    }

    public class GPUOverclockSettingsStore
    {
        public bool Enabled { get; set; }
        public GPUOverclockInfo Info { get; set; } = GPUOverclockInfo.Zero;
    }
}
