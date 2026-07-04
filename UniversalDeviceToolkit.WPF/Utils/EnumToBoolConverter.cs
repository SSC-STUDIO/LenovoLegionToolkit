using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Data;

namespace UniversalDeviceToolkit.WPF.Utils
{
    public class EnumToBoolConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, object>> _enumCache = new();

        private static object GetParsedEnum(Type enumType, string name)
        {
            var typeCache = _enumCache.GetOrAdd(enumType, _ => new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase));
            return typeCache.GetOrAdd(name, n => System.Enum.Parse(enumType, n, ignoreCase: true));
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            var enumParameter = GetParsedEnum(value.GetType(), parameter.ToString() ?? string.Empty);

            return value.Equals(enumParameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is true && parameter != null)
            {
                return GetParsedEnum(targetType, parameter.ToString() ?? string.Empty);
            }

            return Binding.DoNothing;
        }
    }
}
