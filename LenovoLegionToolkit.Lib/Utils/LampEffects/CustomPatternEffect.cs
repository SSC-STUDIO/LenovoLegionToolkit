using System;
using System.Collections.Generic;
using Windows.Devices.Lights;
using Windows.UI;

namespace LenovoLegionToolkit.Lib.Utils.LampEffects;

public class CustomPatternEffect : ILampEffect
{
    private readonly Dictionary<int, Color> _colors = new();

    public string Name => "Custom Pattern";
    public Dictionary<string, object> Parameters { get; } = new();

    public Color GetColorForLamp(int lampIndex, double time, LampInfo lampInfo, int totalLamps)
    {
        return _colors.TryGetValue(lampIndex, out var color) ? color : Color.FromArgb(0, 0, 0, 0);
    }

    public void Reset()
    {
    }

    public void SetColor(int index, Color color)
    {
        _colors[index] = color;
    }
    
    public void Clear()
    {
        _colors.Clear();
    }
}
