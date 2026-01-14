using System;
using System.Globalization;
using System.Windows.Data;

namespace LenovoLegionToolkit.WPF.Utils;

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
