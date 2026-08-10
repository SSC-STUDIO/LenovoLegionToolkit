using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Identity converter for booleans (Avalonia has no Visibility enum; visibility is
/// expressed with the bool <c>IsVisible</c> property).
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? b : value is not null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && b;
}
