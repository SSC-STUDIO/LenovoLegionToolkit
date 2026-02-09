using System;
using System.Globalization;
using System.Windows.Data;

namespace LenovoLegionToolkit.WPF.Utils
{
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            var enumValue = value;
            var enumParameter = System.Enum.Parse(value.GetType(), parameter.ToString() ?? string.Empty, ignoreCase: true);

            return enumValue.Equals(enumParameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is true && parameter != null)
            {
                return System.Enum.Parse(targetType, parameter.ToString() ?? string.Empty, ignoreCase: true);
            }

            return Binding.DoNothing;
        }
    }
}
