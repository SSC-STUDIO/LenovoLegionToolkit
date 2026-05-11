using System.Windows;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.WPF.Utils;

public static class AnimationHelper
{
    public static void UpdateAnimationParameters(ApplicationSettings settings)
    {
        var animationsEnabled = settings.Store.AnimationsEnabled;
        var animationSpeed = settings.Store.AnimationSpeed;

        UpdateDuration("AnimationDurationFast", 0.1, animationsEnabled, animationSpeed);
        UpdateDuration("AnimationDurationMedium", 0.2, animationsEnabled, animationSpeed);
        UpdateDuration("AnimationDurationSlow", 0.3, animationsEnabled, animationSpeed);
    }

    private static void UpdateDuration(string key, double baseSeconds, bool enabled, double speed)
    {
        var duration = AnimationTiming.ComputeUiAnimationDuration(baseSeconds, enabled, speed);
        Application.Current.Resources[key] = new Duration(duration);
    }
}
