using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace LenovoLegionToolkit.WPF.Utils
{
    public class BooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return false;

            foreach (var value in values)
            {
                if (value is bool boolValue && !boolValue)
                    return false;
            }

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}