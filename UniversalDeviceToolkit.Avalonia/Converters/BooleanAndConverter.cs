using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace UniversalDeviceToolkit.Avalonia.Converters;

/// <summary>
/// Returns <c>true</c> when ALL bound values are <c>true</c>; any null or
/// non-boolean value fails the conjunction.
/// </summary>
public class BooleanAndConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null || values.Count == 0)
            return false;

        var andResult = true;
        foreach (var value in values)
        {
            // Handle null values
            if (value is null)
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

        return andResult;
    }

    public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("ConvertBack is not supported by this one-way converter.");
}
