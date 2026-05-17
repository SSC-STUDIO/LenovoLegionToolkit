using System.Collections.Generic;
using LenovoLegionToolkit.Lib.Utils;
using static LenovoLegionToolkit.Lib.Settings.FanCurveSettings;

namespace LenovoLegionToolkit.Lib.Settings;


public class FanCurveSettings() : AbstractSettings<FanCurveSettingsStore>("fan_curves.json")
{
    public class FanCurveSettingsStore
    {
        public List<FanCurveEntry> Entries { get; set; } = [];
    }
}
