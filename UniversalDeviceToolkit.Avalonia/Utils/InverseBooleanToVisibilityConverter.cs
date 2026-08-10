using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Inverts a boolean. Avalonia has no <c>Visibility</c> enum; visibility is the bool
/// <c>IsVisible</c> property, so this converter simply negates.
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !(value is bool b && b);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !(value is bool b && b);
}
