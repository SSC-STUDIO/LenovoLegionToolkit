using System;
using System.Globalization;
using UniversalDeviceToolkit.Avalonia.Localization;

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Thin progress-toast wrapper for the Avalonia host. The notification manager
/// is message-bus driven and exposes no public progress API, so Start/Update/
/// Complete are no-ops and progress is shown inline by the feature page. The
/// formatting helpers keep the cleanup summary and byte text aligned with the
/// WPF host (WindowsOptimizationPage.Cleanup.cs).
/// </summary>
public static class AvaloniaProgressToastHelper
{
    public static Guid Start(string title, string? message = null) => Guid.Empty;

    public static void Update(Guid id, double percent, string? message = null)
    {
    }

    public static void Complete(Guid id)
    {
    }

    /// <summary>
    /// Formats the cleanup completion summary. Freed bytes are optional because
    /// the host action contract only reports the final report state; when the
    /// service exposes a measured size it is included with the WPF wording.
    /// </summary>
    public static string FormatCleanupSummary(int itemCount, TimeSpan elapsed, long? freedBytes)
    {
        var seconds = elapsed.TotalSeconds.ToString("0.0", CultureInfo.CurrentCulture);
        if (freedBytes is { } bytes)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                AvaloniaLocalization.GetString(
                    "WindowsOptimizationPage_CleanupSummary",
                    "Freed {0} in {1}s ({2} items)."),
                FormatBytes(bytes),
                seconds,
                itemCount);
        }

        return string.Format(
            CultureInfo.CurrentCulture,
            AvaloniaLocalization.GetString(
                "WindowsOptimizationPage_CleanupSummaryWithoutSize",
                "Completed {0} items in {1}s."),
            itemCount,
            seconds);
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
            return "0 B";

        var units = new[] { "B", "KB", "MB", "GB", "TB" };
        var value = (double)bytes;
        var index = 0;
        while (value >= 1024 && index < units.Length - 1)
        {
            value /= 1024;
            index++;
        }

        return index == 0
            ? $"{bytes} {units[index]}"
            : $"{value:0.##} {units[index]}";
    }
}
