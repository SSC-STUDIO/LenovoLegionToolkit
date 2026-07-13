using System;

namespace UniversalDeviceToolkit.WPF.Utils;

internal static class SkeletonAnimationTokens
{
    // Slightly longer cycle reads smoother; sweep range keeps a continuous 流光 band.
    internal const double DurationSeconds = 1.55;
    internal const double SweepFrom = -1.05;
    internal const double SweepTo = 1.05;
    // Tight stagger so rows feel like one wave, not chaotic desync.
    internal const double StaggerStepSeconds = 0.035;
    internal const double StaggerMaxSeconds = 0.18;
}
