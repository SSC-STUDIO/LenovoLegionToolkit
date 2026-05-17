using System;
using Windows.Devices.Lights;
using Windows.UI;

namespace LenovoLegionToolkit.Lib.Utils.LampEffects;

public class StaticEffect : BaseLampEffect
{
    public override string Name => "Static";

    public StaticEffect(Color color)
    {
        Parameters["Color"] = color;
    }

    public override Color GetColorForLamp(int lampIndex, double time, LampInfo lampInfo, int totalLamps)
    {
        return (Color)Parameters["Color"];
    }
}

public class BreatheEffect : BaseLampEffect
{
    public override string Name => "Breathe";

    public BreatheEffect(Color color, double period = 3.0)
    {
        Parameters["Color"] = color;
        Parameters["Period"] = period;
    }

    public override Color GetColorForLamp(int lampIndex, double time, LampInfo lampInfo, int totalLamps)
    {
        var color = (Color)Parameters["Color"];
        var period = (double)Parameters["Period"];
        
        var t = time % period / period;
        var intensity = Math.Sin(t * Math.PI * 2) * 0.5 + 0.5;
        intensity = EaseInOut(intensity);
        
        intensity = 0.2 + intensity * 0.8;
        
        return Color.FromArgb(255,
            (byte)(color.R * intensity),
            (byte)(color.G * intensity),
            (byte)(color.B * intensity));
    }
}

public class WaveEffect : BaseLampEffect
{
    public override string Name => "Wave";

    public WaveEffect(Color color1, Color color2, double period = 2.0, WaveDirection direction = WaveDirection.LeftToRight)
    {
        Parameters["Color1"] = color1;
        Parameters["Color2"] = color2;
        Parameters["Period"] = period;
        Parameters["Direction"] = direction;
    }

    public override Color GetColorForLamp(int lampIndex, double time, LampInfo lampInfo, int totalLamps)
    {
        var color1 = (Color)Parameters["Color1"];
        var color2 = (Color)Parameters["Color2"];
        var period = (double)Parameters["Period"];
        var direction = (WaveDirection)Parameters["Direction"];

        double position = direction switch
        {
            WaveDirection.LeftToRight => lampInfo.Position.X,
            WaveDirection.RightToLeft => -lampInfo.Position.X,
            WaveDirection.TopToBottom => lampInfo.Position.Y,
            WaveDirection.BottomToTop => -lampInfo.Position.Y,
            _ => lampInfo.Position.X
        };

        var wave1 = Math.Sin((time / period + position / 500.0) * Math.PI * 2) * 0.5 + 0.5;
        var wave2 = Math.Sin((time / period + position / 800.0) * Math.PI * 2) * 0.3 + 0.5;
        var wave = (wave1 + wave2) / 2.0;
        
        wave = EaseInOut(wave);
        
        return LerpColor(color1, color2, wave);
    }
}

public enum WaveDirection
{
    LeftToRight,
    RightToLeft,
    TopToBottom,
    BottomToTop
}

public class RainbowEffect : BaseLampEffect
{
    public override string Name => "Rainbow";

    public RainbowEffect(double period = 5.0, bool spatial = true)
    {
        Parameters["Period"] = period;
        Parameters["Spatial"] = spatial;
    }

    public override Color GetColorForLamp(int lampIndex, double time, LampInfo lampInfo, int totalLamps)
    {
        var period = (double)Parameters["Period"];
        var spatial = (bool)Parameters["Spatial"];

        double hue;
        if (spatial)
        {
            hue = time / period * 360 + lampInfo.Position.X / 5.0 + lampInfo.Position.Y / 10.0;
        }
        else
        {
            hue = time / period * 360;
        }

        var saturation = 0.9 + Math.Sin(time * 2) * 0.1;
        var value = 0.9 + Math.Sin(time * 3) * 0.1;

        return HsvToRgb(hue, saturation, value);
    }
}
