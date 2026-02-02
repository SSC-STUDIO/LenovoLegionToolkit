using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LenovoLegionToolkit.Lib.Settings;

namespace LenovoLegionToolkit.WPF.Utils;

public static class AnimationHelper
{
    public static void UpdateAnimationParameters(ApplicationSettings settings)
    {
        var animationsEnabled = settings.Store.AnimationsEnabled;
        var animationSpeed = settings.Store.AnimationSpeed;

        if (animationSpeed <= 0) animationSpeed = 1.0;

        // Update durations based on speed and enabled status
        UpdateDuration("AnimationDurationFast", 0.1, animationsEnabled, animationSpeed);
        UpdateDuration("AnimationDurationMedium", 0.2, animationsEnabled, animationSpeed);
        UpdateDuration("AnimationDurationSlow", 0.3, animationsEnabled, animationSpeed);
    }

    private static void UpdateDuration(string key, double baseSeconds, bool enabled, double speed)
    {
        if (!enabled)
        {
            Application.Current.Resources[key] = new Duration(TimeSpan.Zero);
            return;
        }

        var duration = TimeSpan.FromSeconds(baseSeconds / speed);
        Application.Current.Resources[key] = new Duration(duration);
    }

    public static void SetupPerformanceMonitoring()
    {
        CompositionTarget.Rendering += (s, e) =>
        {
            // This is a placeholder for performance monitoring logic.
            // In a real scenario, we would calculate the frame rate here
            // and log if it drops below a certain threshold during animations.
        };
    }
}
