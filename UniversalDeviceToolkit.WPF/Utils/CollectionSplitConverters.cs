using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Takes the first half of an <see cref="IEnumerable"/> collection and returns it as a
/// <see cref="ReadOnlyCollection{T}"/>.
/// </summary>
public class TakeHalfConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IEnumerable items)
            return value;

        var enumerator = items.GetEnumerator();
        var allItems = new List<object>();
        while (enumerator.MoveNext())
            allItems.Add(enumerator.Current);

        if (allItems.Count == 0)
            return Array.Empty<object>();

        var half = (int)Math.Ceiling(allItems.Count / 2.0);
        return new ReadOnlyCollection<object>(allItems.GetRange(0, half));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Skips the first half of an <see cref="IEnumerable"/> collection and returns the remainder as a
/// <see cref="ReadOnlyCollection{T}"/>.
/// </summary>
public class SkipHalfConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IEnumerable items)
            return value;

        var enumerator = items.GetEnumerator();
        var allItems = new List<object>();
        while (enumerator.MoveNext())
            allItems.Add(enumerator.Current);

        if (allItems.Count == 0)
            return Array.Empty<object>();

        var half = (int)Math.Ceiling(allItems.Count / 2.0);
        return new ReadOnlyCollection<object>(allItems.GetRange(half, allItems.Count - half));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
