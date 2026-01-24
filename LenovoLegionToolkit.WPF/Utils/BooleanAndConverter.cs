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
                return targetType == typeof(Visibility) ? Visibility.Collapsed : false;

            var andResult = true;
            foreach (var value in values)
            {
                // Handle null values
                if (value == null)
                {
                    andResult = false;
                    break;
                }
                    
                // Handle bool values
                if (value is bool boolValue)
                {
                    if (!boolValue)
                    {
                        andResult = false;
                        break;
                    }
                }
                else
                {
                    // If value is not a bool, treat as false for safety
                    andResult = false;
                    break;
                }
            }

            if (targetType == typeof(Visibility))
                return andResult ? Visibility.Visible : Visibility.Collapsed;

            return andResult;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
