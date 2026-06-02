using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Settings;

public class OsdSettings() : AbstractSettings<OsdSettings.OsdSettingsStore>("osd.json")
{
    public class OsdSettingsStore
    {
        public bool ShowOsd { get; set; }
        public double OsdRefreshInterval { get; set; } = 1;
        public int SelectedStyleIndex { get; set; } = 0;
        public List<OsdItem> Items { get; set; } = Enum.GetValues<OsdItem>().ToList();

        public double BackgroundOpacity { get; set; } = 0.6;
        public string BackgroundColor { get; set; } = "#1E1E1E";
        public int FontSize { get; set; } = 12;
        public int CornerRadiusTop { get; set; } = 6;
        public int CornerRadiusBottom { get; set; } = 6;
        public bool IsLocked { get; set; } = false;
        public double? PanelPositionX { get; set; }
        public double? PanelPositionY { get; set; }
        public double? BarPositionX { get; set; }
        public double? BarPositionY { get; set; }

        public int TempThresholdWarning { get; set; } = 75;
        public int TempThresholdCritical { get; set; } = 90;
        public int UsageThresholdWarning { get; set; } = 70;
        public int UsageThresholdCritical { get; set; } = 90;
        public int FpsThresholdCritical { get; set; } = 30;
        public int LowFpsDeltaThreshold { get; set; } = 30;

        public string CategoryColor { get; set; } = "#2196F3";
        public string LabelColor { get; set; } = "#ADFF2F";
        public string ValueColor { get; set; } = "#FFFFFF";
        public string WarningColor { get; set; } = "#FFFF00";
        public string CriticalColor { get; set; } = "#FF0000";
        public int SnapThreshold { get; set; } = 20;
    }

    public override OsdSettingsStore? LoadStore() => Normalize(base.LoadStore());

    public override async Task<OsdSettingsStore?> LoadStoreAsync() =>
        Normalize(await base.LoadStoreAsync().ConfigureAwait(false));

    private static OsdSettingsStore? Normalize(OsdSettingsStore? store)
    {
        if (store is null)
            return null;

        store.Items = NormalizeItems(store.Items);
        return store;
    }

    private static List<OsdItem> NormalizeItems(List<OsdItem>? items)
    {
        if (items is null)
            return Enum.GetValues<OsdItem>().ToList();

        return items
            .Where(Enum.IsDefined)
            .Distinct()
            .ToList();
    }
}
