using System;
using System.Globalization;
using System.Windows.Data;

namespace UniversalDeviceToolkit.WPF.Utils;

    /// <summary>
    /// Returns <c>true</c> if the bound <see cref="double"/> value is greater than or equal to the
    /// configured <see cref="MinimumWidth"/>; otherwise <c>false</c>.
    /// </summary>
    public class MinimumWidthConverter : IValueConverter
{
    public double MinimumWidth { get; set; } = 1200;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            return width >= MinimumWidth;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
