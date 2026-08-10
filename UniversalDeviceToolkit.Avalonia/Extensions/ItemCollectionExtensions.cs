using System.Collections;
using Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Extensions;

public static class ItemCollectionExtensions
{
    public static void AddRange(this ItemCollection itemCollection, IEnumerable enumerable)
    {
        foreach (var item in enumerable)
            itemCollection.Add(item);
    }
}
