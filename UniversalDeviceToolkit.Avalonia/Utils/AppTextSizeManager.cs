using System;
using UniversalDeviceToolkit.Lib.Settings;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Maps the persisted AppTextSize setting to the numeric multiplier applied by
/// DpiAwareTypography.UserScale. Setting UserScale re-applies the font-size tokens
/// on all open windows live; BaseWindow re-applies on load and DPI change.
/// </summary>
public static class AppTextSizeManager
{
    public static void Apply(AppTextSize size) => DpiAwareTypography.UserScale = GetScale(size);

    public static void ApplySaved(ApplicationSettings settings) => Apply(settings.Store.AppTextSize);

    private static double GetScale(AppTextSize size) => size switch
    {
        AppTextSize.Compact => 0.90d,
        AppTextSize.Large => 1.10d,
        AppTextSize.ExtraLarge => 1.25d,
        _ => 1.0d
    };
}

public static class AppScaleManager
{
    public static event EventHandler? ScaleChanged;

    public static double CurrentScale { get; private set; } = 1d;

    public static void Apply(AppScale scale)
    {
        var nextScale = GetScale(scale);
        if (nextScale.Equals(CurrentScale))
            return;

        CurrentScale = nextScale;
        ScaleChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void ApplySaved(ApplicationSettings settings) => Apply(settings.Store.AppScale);

    internal static double GetScale(AppScale scale) =>
        Enum.IsDefined(scale) ? (int)scale / 100d : 1d;
}
