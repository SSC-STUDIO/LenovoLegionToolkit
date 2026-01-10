using System;

namespace LenovoLegionToolkit.Lib.Settings;

public class UpdateCheckSettings() : AbstractSettings<UpdateCheckSettings.UpdateCheckSettingsStore>("update_check.json")
{
    public class UpdateCheckSettingsStore
    {
        public DateTime? LastUpdateCheckDateTime { get; set; }
        public UpdateCheckFrequency UpdateCheckFrequency { get; set; }
        public string? UpdateRepositoryOwner { get; set; }
        public string? UpdateRepositoryName { get; set; }
    }

    protected override UpdateCheckSettingsStore Default => new()
    {
        LastUpdateCheckDateTime = null,
        UpdateCheckFrequency = UpdateCheckFrequency.PerDay,
        UpdateRepositoryOwner = null, // null means use default
        UpdateRepositoryName = null // null means use default
    };
}
