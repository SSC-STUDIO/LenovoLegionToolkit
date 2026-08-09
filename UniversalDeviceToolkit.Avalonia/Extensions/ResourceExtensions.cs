// WPF-compatible resource helpers for Avalonia StyledElement.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace UniversalDeviceToolkit.Avalonia.Extensions;

public static class ResourceExtensions
{
    /// <summary>One-argument TryFindResource (Avalonia requires a theme variant + out param).</summary>
    public static object? TryFindResource(this StyledElement element, object key) =>
        element.TryFindResource(key, Application.Current?.RequestedThemeVariant, out var value) ? value : null;

    /// <summary>One-argument FindResource that throws when the key is missing.</summary>
    public static object? FindResource(this StyledElement element, object key)
    {
        if (element.TryFindResource(key, out var value))
            return value;
        throw new KeyNotFoundException($"Resource '{key}' was not found.");
    }

    /// <summary>Applies a resource value to a property once (WPF SetResourceReference parity).</summary>
    public static void SetResourceReference(this StyledElement element, AvaloniaProperty property, object key)
    {
        if (element.TryFindResource(key, out var value))
            element.SetValue(property, value);
    }
}
