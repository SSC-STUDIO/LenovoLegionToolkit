using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UniversalDeviceToolkit.Avalonia.Utils;

/// <summary>
/// Converts a non-empty string to <c>true</c>, and <c>null</c> or whitespace to <c>false</c>.
/// </summary>
public class TextToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !string.IsNullOrWhiteSpace(value as string);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
