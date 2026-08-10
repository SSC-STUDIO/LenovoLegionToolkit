using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Utils;

internal static class DpiAwareTypography
{
    private const double DefaultDpi = 96d;

    private static readonly IReadOnlyDictionary<string, double> BaseFontSizes = new Dictionary<string, double>
    {
        ["FontSizeMicro"] = 10d,
        ["FontSizeDenseCaption"] = 11d,
        ["FontSizeSmallCaption"] = 12d,
        ["FontSizeSmallBody"] = 13d,
        ["FontSizeCaption"] = 14d,
        ["FontSizeBody"] = 15d,
        ["FontSizeTitleBarDeviceInfo"] = 16d,
        ["FontSizePageDescription"] = 16d,
        ["FontSizeSubsection"] = 17d,
        ["FontSizeGaugeValue"] = 18d,
        ["FontSizeSection"] = 19d,
        ["FontSizeMidHeader"] = 20d,
        ["FontSizeDisplaySection"] = 25d,
        ["FontSizePageTitle"] = 29d,
    };

    public static double GetFontScaleForDpi(double dpiScale)
    {
        if (double.IsNaN(dpiScale) || double.IsInfinity(dpiScale) || dpiScale <= 0)
            return 1d;

        // Avalonia already scales layout for DPI. Keep the correction intentionally light (max ~4% shrink)
        // so high-DPI users who raise scaling to READ text are not counteracted.
        return Math.Clamp(1d / Math.Sqrt(dpiScale), 0.96d, 1.04d);
    }

    private static double _userScale = 1d;

    /// <summary>
    /// User-chosen text size multiplier (Settings → Appearance → Text size), orthogonal
    /// to the DPI correction above. Clamped to 0.85–1.35. Setting it re-applies the
    /// font-size tokens on every open window immediately (live, no restart).
    /// </summary>
    public static double UserScale
    {
        get => _userScale;
        set
        {
            var clamped = Math.Clamp(value, 0.85d, 1.35d);
            if (clamped.Equals(_userScale))
                return;

            _userScale = clamped;
            ApplyToAllWindows();
        }
    }

    /// <summary>
    /// Re-applies typography on every open window using that window's current scaling,
    /// so a UserScale change takes effect live. Safe before any window exists.
    /// AVALONIA: Application has no window enumeration API; apply to the main window.
    /// </summary>
    public static void ApplyToAllWindows()
    {
        if (UdtAppContext.MainWindow is { } mainWindow)
            Apply(mainWindow);
    }

    public static void Apply(Window window)
    {
        var dpiScale = 1d;

        try
        {
            dpiScale = window.DesktopScaling;
        }
        catch
        {
            dpiScale = DefaultDpi / DefaultDpi;
        }

        Apply(window.Resources, dpiScale);
    }

    public static void Apply(IResourceDictionary resources, double dpiScale)
    {
        // DPI correction and the user text-size setting are independent knobs multiplied together.
        var fontScale = GetFontScaleForDpi(dpiScale) * UserScale;

        foreach (var (key, baseSize) in BaseFontSizes)
            resources[key] = Math.Round(baseSize * fontScale, 1, MidpointRounding.AwayFromZero);

        // ControlTemplates from app-level dictionaries (nav chrome, charts, empty states,
        // legacy snackbar) resolve DynamicResource font tokens from the APPLICATION scope
        // only, so per-window token writes never reach them. Mirror the scaled tokens into
        // the app resources too, or those templates ignore the text size setting.
        var appResources = Application.Current?.Resources;
        if (appResources is not null && !ReferenceEquals(appResources, resources))
        {
            foreach (var (key, baseSize) in BaseFontSizes)
                appResources[key] = Math.Round(baseSize * fontScale, 1, MidpointRounding.AwayFromZero);
        }
    }
}
