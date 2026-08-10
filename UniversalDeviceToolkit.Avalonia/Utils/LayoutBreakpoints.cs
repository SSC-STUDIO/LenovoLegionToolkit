namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Centralized layout breakpoint definitions for responsive UI.
/// Ensures consistency across all responsive components in the application.
/// </summary>
internal static class LayoutBreakpoints
{
    // ── Window Size Constraints ───────────────────────────────────────
    
    /// <summary>Minimum window width (hard limit)</summary>
    public const double WindowMinWidth = 1024;
    
    /// <summary>Minimum window height (hard limit)</summary>
    public const double WindowMinHeight = 640;
    
    // ── Dashboard Layout Breakpoints ──────────────────────────────────
    
    /// <summary>Dashboard ultra-wide layout threshold (3 columns, maximum spacing)</summary>
    public const double DashboardUltraWide = 2000;
    
    /// <summary>Dashboard wide layout threshold (3 columns)</summary>
    public const double DashboardWide = 1500;
    
    /// <summary>Dashboard standard layout threshold (2 columns)</summary>
    public const double DashboardStandard = 1000;
    
    // Below DashboardStandard: 1 column layout
    
    // ── Sensor Control Layout Breakpoints ─────────────────────────────
    
    /// <summary>Sensors ultra-wide mode (largest gauges, tallest trends, widest progress bars)</summary>
    public const double SensorsUltraWide = 2000;
    
    /// <summary>Sensors wide mode (large gauges, tall trends, wide progress bars)</summary>
    public const double SensorsWide = 1500;
    
    /// <summary>Sensors standard mode (medium gauges, standard trends)</summary>
    public const double SensorsStandard = 900;
    
    // Below SensorsStandard: Compact mode (small gauges, hidden model names)
    
    // ── Navigation Pane Metrics ───────────────────────────────────────
    
    /// <summary>Design reference width for navigation pane calculations</summary>
    public const double NavigationDesignWidth = 1300;
    
    /// <summary>Minimum content width to preserve when navigation is expanded</summary>
    public const double NavigationMinContentWidth = 700;
    
    /// <summary>Absolute maximum width for expanded navigation pane</summary>
    public const double NavigationMaxExpandedWidth = 420;
    
    // ── Component Size Tokens ─────────────────────────────────────────
    
    // Progress Bar Maximum Widths
    public const double ProgressBarUltraWideMax = 400;
    public const double ProgressBarWideMax = 320;
    public const double ProgressBarStandardMax = 260;
    public const double ProgressBarCompactMax = 260; // Same as standard
    
    // Gauge Sizes
    public const double GaugeSizeUltraWide = 130;
    public const double GaugeSizeStandard = 110;
    public const double GaugeSizeCompact = 88;
    
    // Trend Chart Heights
    public const double TrendHeightUltraWide = 180;
    public const double TrendHeightWide = 150;
    public const double TrendHeightStandard = 120;
    
    // ── Helper Methods ────────────────────────────────────────────────
    
    /// <summary>
    /// Gets the appropriate gauge size for the given layout mode.
    /// </summary>
    public static double GetGaugeSize(bool isCompact, bool isUltraWide)
    {
        if (isCompact) return GaugeSizeCompact;
        if (isUltraWide) return GaugeSizeUltraWide;
        return GaugeSizeStandard;
    }
    
    /// <summary>
    /// Gets the appropriate trend chart height for the given layout mode.
    /// </summary>
    public static double GetTrendHeight(bool isWide, bool isUltraWide)
    {
        if (isUltraWide) return TrendHeightUltraWide;
        if (isWide) return TrendHeightWide;
        return TrendHeightStandard;
    }
    
    /// <summary>
    /// Gets the appropriate progress bar maximum width for the given layout mode.
    /// </summary>
    public static double GetProgressBarMaxWidth(bool isWide, bool isUltraWide)
    {
        if (isUltraWide) return ProgressBarUltraWideMax;
        if (isWide) return ProgressBarWideMax;
        return ProgressBarStandardMax;
    }
}
