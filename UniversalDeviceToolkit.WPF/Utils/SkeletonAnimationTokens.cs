using System;

namespace UniversalDeviceToolkit.WPF.Utils;

internal static class SkeletonAnimationTokens
{
    // Classic 4.x-style calm cycle: long enough to look fluid, not a hard wipe.
    internal const double DurationSeconds = 1.7;
    internal const double SweepFrom = -1.25;
    internal const double SweepTo = 1.25;
    // Wider stagger than the ultra-tight wave — fewer borders peak on the same frame (smoother GPU).
    internal const double StaggerStepSeconds = 0.055;
    internal const double StaggerMaxSeconds = 0.32;
}
