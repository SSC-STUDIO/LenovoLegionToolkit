using System;
using System.Collections.Generic;
using Windows.Devices.Lights;
using Windows.UI;

namespace LenovoLegionToolkit.Lib.Utils.LampEffects;

public interface ILampEffect
{
    string Name { get; }
    Dictionary<string, object> Parameters { get; }

    Color GetColorForLamp(int lampIndex, double time, LampInfo lampInfo, int totalLamps);
    void Reset();
}
