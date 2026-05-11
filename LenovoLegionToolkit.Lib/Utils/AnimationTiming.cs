using System;

namespace LenovoLegionToolkit.Lib.Utils;

/// <summary>
/// Pure duration math for UI animations (WPF applies <see cref="System.Windows.Duration"/> from these values).
/// </summary>
public static class AnimationTiming
{
    /// <summary>
    /// Computes the animation duration for a keyed timeline segment.
    /// </summary>
    /// <param name="baseSeconds">Nominal length in seconds at speed 1.</param>
    /// <param name="animationsEnabled">When false, returns <see cref="TimeSpan.Zero"/>.</param>
    /// <param name="animationSpeed">Positive scale; values &lt;= 0 are treated as 1.</param>
    public static TimeSpan ComputeUiAnimationDuration(double baseSeconds, bool animationsEnabled, double animationSpeed)
    {
        if (!animationsEnabled)
            return TimeSpan.Zero;

        if (animationSpeed <= 0)
            animationSpeed = 1.0;

        return TimeSpan.FromSeconds(baseSeconds / animationSpeed);
    }
}
