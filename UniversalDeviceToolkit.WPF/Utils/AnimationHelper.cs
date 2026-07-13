using System;
using System.Windows;
using System.Windows.Media.Animation;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.WPF.Utils;

public static class AnimationHelper
{
    public static void UpdateAnimationParameters(ApplicationSettings settings)
    {
        var animationsEnabled = settings.Store.AnimationsEnabled;
        var animationSpeed = settings.Store.AnimationSpeed;

        UpdateDuration("AnimationDurationFast", 0.1, animationsEnabled, animationSpeed);
        UpdateDuration("AnimationDurationMedium", 0.2, animationsEnabled, animationSpeed);
        UpdateDuration("AnimationDurationSlow", 0.3, animationsEnabled, animationSpeed);
        // Skeleton 流光: ~1.55s at speed 1.0; default AnimationSpeed 2.0 → ~0.8s (snappy cascade).
        // Soft SineEase + staggered delays keep it polished without a multi-second crawl.
        UpdateDuration("AnimationDurationShimmer", 1.55, animationsEnabled, animationSpeed);
    }

    private static void UpdateDuration(string key, double baseSeconds, bool enabled, double speed)
    {
        Application.Current.Resources[key] = new Duration(AnimationTiming.ComputeUiAnimationDuration(baseSeconds, enabled, speed));
    }
}
