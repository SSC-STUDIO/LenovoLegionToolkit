using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace LenovoLegionToolkit.WPF.Utils;

public class TakeHalfConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IEnumerable items)
            return value;

        var list = items.Cast<object>().ToList();
        var half = (int)Math.Ceiling(list.Count / 2.0);
        return list.Take(half).ToList();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SkipHalfConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IEnumerable items)
            return value;

        var list = items.Cast<object>().ToList();
        var half = (int)Math.Ceiling(list.Count / 2.0);
        return list.Skip(half).ToList();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
