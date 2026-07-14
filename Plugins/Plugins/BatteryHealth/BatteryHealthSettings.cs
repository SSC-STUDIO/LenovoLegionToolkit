using System;
using System.Collections.ObjectModel;
using System.Management;
using UniversalDeviceToolkit.Plugins.Shared;
using UniversalDeviceToolkit.Plugins.SDK;

namespace UniversalDeviceToolkit.Plugins.BatteryHealth;

/// <summary>
/// Settings for Battery Health plugin.
/// </summary>
public class BatteryHealthSettings
{
    public const int MinThreshold = 0;
    public const int MaxThreshold = 100;
    public const int DefaultLowHealthThreshold = 80;
    public const int DefaultCriticalHealthThreshold = 60;

    public bool EnableRealTimeMonitoring { get; set; } = true;
    public int LowHealthThreshold { get; set; } = DefaultLowHealthThreshold; // < 80% = warning
    public int CriticalHealthThreshold { get; set; } = DefaultCriticalHealthThreshold; // < 60% = critical
    public bool EnableNotification { get; set; } = true;

    /// <summary>
    /// Checks whether the threshold configuration is logically valid:
    /// critical threshold must be less than the low-health threshold,
    /// and both must be within [0, 100].
    /// If invalid, the health classifier silently returns Healthy for
    /// all batteries regardless of actual degradation, hiding failures.
    /// </summary>
    public bool AreThresholdsValid =>
        LowHealthThreshold is >= MinThreshold and <= MaxThreshold &&
        CriticalHealthThreshold is >= MinThreshold and <= MaxThreshold &&
        CriticalHealthThreshold < LowHealthThreshold;

    /// <summary>
    /// If the threshold pair is invalid (e.g. Critical >= Low or out of
    /// range), resets them to safe defaults so health classification
    /// continues to function. Returns true if any values were adjusted.
    /// </summary>
    public bool EnsureValidThresholds()
    {
        var adjusted = false;

        if (LowHealthThreshold is < MinThreshold or > MaxThreshold)
        {
            LowHealthThreshold = DefaultLowHealthThreshold;
            adjusted = true;
        }

        if (CriticalHealthThreshold is < MinThreshold or > MaxThreshold)
        {
            CriticalHealthThreshold = DefaultCriticalHealthThreshold;
            adjusted = true;
        }

        // After range clamp, check the relative ordering.
        // If Critical >= Low, the "Healthy -> Warning -> Critical" cascade
        // in BatteryHealthService.ClassifyStatus collapses and everything
        // is classified as Healthy - a silent misclassification bug.
        if (CriticalHealthThreshold >= LowHealthThreshold)
        {
            // Position Critical at least 1 below Low; floor at MinThreshold.
            // Special case: if Low is 0 (the absolute minimum), there is no
            // valid Critical value lower than it — bump Low up to 1 to make
            // room, which is more conservative (treats 0% as a warning).
            if (LowHealthThreshold <= MinThreshold)
            {
                LowHealthThreshold = MinThreshold + 1;
            }

            CriticalHealthThreshold = Math.Max(MinThreshold, LowHealthThreshold - 20);
            adjusted = true;
        }

        return adjusted;
    }
}
