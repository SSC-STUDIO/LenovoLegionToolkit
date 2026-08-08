using System;
using System.Windows;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Navigation pane width tokens that scale the expanded / max stretch width with the host window.
/// Collapsed width stays fixed so the icon rail remains stable.
/// </summary>
internal static class NavigationPaneMetrics
{
    /// <summary>Design width used when sizing the default expanded rail (matches MainWindow default Width).</summary>
    private const double DesignWindowWidth = 1300;

    /// <summary>Hard cap so the rail never eats the whole dashboard on ultra-wide monitors.</summary>
    private const double AbsoluteMaxExpandedWidth = 420;

    /// <summary>Leave this much horizontal space for the content surface + chrome.</summary>
    private const double MinContentWidth = 700;

    public static double GetCollapsedWidth()
    {
        if (Application.Current?.TryFindResource("NavigationWidthCollapsed") is double width && width > 0)
            return width;
        return 70;
    }

    public static double GetPreferredExpandedWidth()
    {
        if (Application.Current?.TryFindResource("NavigationWidthExpanded") is double width && width > 0)
            return width;
        return 220;
    }

    /// <summary>
    /// Max width the rail may occupy for the given window width.
    /// Scales above the preferred expanded token on larger windows and never exceeds content budget.
    /// </summary>
    public static double GetMaxStretchWidth(double windowWidth)
    {
        var preferred = GetPreferredExpandedWidth();
        if (windowWidth <= 0 || double.IsNaN(windowWidth) || double.IsInfinity(windowWidth))
            return preferred;

        // Linear scale from design width: larger windows allow a wider expanded rail.
        var scaled = preferred * (windowWidth / DesignWindowWidth);

        // Never steal more than the content budget.
        var contentBudget = Math.Max(preferred, windowWidth - MinContentWidth);

        // Soft ratio cap (~28%) so the rail stays secondary to content on ultra-wide layouts.
        var ratioCap = windowWidth * 0.28;

        var upper = Math.Min(AbsoluteMaxExpandedWidth, Math.Min(contentBudget, Math.Max(preferred, ratioCap)));
        return Math.Clamp(scaled, preferred, upper);
    }

    /// <summary>
    /// Target width when the pane is expanded (same as max stretch so drag/expand stay consistent).
    /// </summary>
    public static double GetExpandedWidth(double windowWidth) => GetMaxStretchWidth(windowWidth);
}
