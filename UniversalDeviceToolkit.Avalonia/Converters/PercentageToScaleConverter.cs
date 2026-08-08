using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UniversalDeviceToolkit.Avalonia.Converters;

public sealed class PercentageToScaleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var percent = value switch
        {
            double d => d,
            float f => f,
            int i => i,
            _ => 0d,
        };

        return Math.Clamp(percent / 100d, 0d, 1d);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
