using System;
using System.Globalization;
using System.Windows.Data;

namespace LenovoLegionToolkit.Plugins.AiAssistant;

/// <summary>
/// Converter to calculate percentage of a value
/// </summary>
public class PercentageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double doubleValue && parameter is string stringParam)
        {
            if (double.TryParse(stringParam, NumberStyles.Float, culture, out var percentage))
            {
                return doubleValue * percentage;
            }
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

