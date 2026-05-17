using System.Text.Json;
using System;
using System.Collections.Generic;
using LenovoLegionToolkit.Lib.Serialization;
using static LenovoLegionToolkit.Lib.Settings.GPUOverclockSettings;

namespace LenovoLegionToolkit.Lib.Settings;

public class GPUOverclockSettings() : AbstractSettings<GPUOverclockSettingsStore>("gpu_oc.json")
{
    public const string DefaultProfileName = "Custom";

    protected override void ConfigureJsonSerializerOptions(JsonSerializerOptions options)
    {
        options.Converters.Add(new GPUOverclockInfoJsonConverter());
    }

    public class GPUOverclockSettingsStore
    {
        public class Profile
        {
            public string Name { get; set; } = DefaultProfileName;
            public GPUOverclockInfo Info { get; set; } = GPUOverclockInfo.Zero;
        }

        public bool Enabled { get; set; }

        // Legacy single-profile value. Keep it for migration and compatibility with existing gpu_oc.json files.
        public GPUOverclockInfo Info { get; set; } = GPUOverclockInfo.Zero;
        public Guid ActiveProfileId { get; set; }
        public Dictionary<Guid, Profile> Profiles { get; set; } = [];
    }
}
