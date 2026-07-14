using System;

namespace UniversalDeviceToolkit.WPF.Utils;

internal static class SkeletonAnimationTokens
{
    // Matches 4.x AppSkeletonShimmerBlockStyle storyboard (AnimationDurationShimmer + ±1.2 travel).
    internal const double DurationSeconds = 1.65;
    internal const double SweepFrom = -1.2;
    internal const double SweepTo = 1.2;
    // Tight stagger so rows feel like one wave, not chaotic desync.
    internal const double StaggerStepSeconds = 0.035;
    internal const double StaggerMaxSeconds = 0.18;
}
