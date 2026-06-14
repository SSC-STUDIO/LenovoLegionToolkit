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
        ["FontSizeSmallBody"] = 11d,
        ["FontSizeCaption"] = 12d,
        ["FontSizeBody"] = 13d,
        ["FontSizePageDescription"] = 14d,
        ["FontSizeSubsection"] = 16d,
        ["FontSizeSection"] = 18d,
        ["FontSizeDisplaySection"] = 24d,
        ["FontSizePageTitle"] = 28d,
    };

    public static double GetFontScaleForDpi(double dpiScale)
    {
        if (double.IsNaN(dpiScale) || double.IsInfinity(dpiScale) || dpiScale <= 0)
            return 1d;

        return Math.Clamp(1d / Math.Sqrt(dpiScale), 0.78d, 1.04d);
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
