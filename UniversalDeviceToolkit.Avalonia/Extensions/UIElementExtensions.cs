using System.Collections.Generic;
using Avalonia;
using Avalonia.VisualTree;

namespace UniversalDeviceToolkit.Avalonia.Extensions;

public static class UIElementExtensions
{
    public static IEnumerable<T> GetVisibleChildrenOfType<T>(this Visual depObj) where T : Visual
    {
        if (!depObj.IsVisible)
            yield break;

        foreach (var child in depObj.GetVisualChildren())
        {

            switch (child)
            {
                case T value:
                    {
                        yield return value;
                        break;
                    }
                case Visual element:
                    {
                        foreach (var sub in GetVisibleChildrenOfType<T>(element))
                            yield return sub;
                        break;
                    }
            }
        }
    }
}
