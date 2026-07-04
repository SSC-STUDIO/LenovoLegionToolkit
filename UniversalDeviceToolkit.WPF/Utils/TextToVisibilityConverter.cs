using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UniversalDeviceToolkit.WPF.Utils;

    /// <summary>
    /// Converts a non-empty string to <see cref="Visibility.Visible"/>, and <c>null</c> or
    /// whitespace to <see cref="Visibility.Collapsed"/>.
    /// </summary>
    public class TextToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
