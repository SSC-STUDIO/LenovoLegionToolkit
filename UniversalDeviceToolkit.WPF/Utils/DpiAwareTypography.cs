using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace UniversalDeviceToolkit.WPF.Utils;

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

        // WPF already scales layout for DPI. Keep the correction intentionally light (max ~4% shrink)
        // so high-DPI users who raise scaling to READ text are not counteracted.
        return Math.Clamp(1d / Math.Sqrt(dpiScale), 0.96d, 1.04d);
    }

    public static void Apply(Window window)
    {
        var dpiScale = 1d;

        try
        {
            dpiScale = VisualTreeHelper.GetDpi(window).PixelsPerDip;
        }
        catch
        {
            dpiScale = DefaultDpi / DefaultDpi;
        }

        Apply(window.Resources, dpiScale);
    }

    public static void Apply(ResourceDictionary resources, double dpiScale)
    {
        var fontScale = GetFontScaleForDpi(dpiScale);

        foreach (var (key, baseSize) in BaseFontSizes)
            resources[key] = Math.Round(baseSize * fontScale, 1, MidpointRounding.AwayFromZero);
    }
}
