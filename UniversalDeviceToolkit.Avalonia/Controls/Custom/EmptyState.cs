using Avalonia.Controls;
using Avalonia;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

/// <summary>
/// Unified empty-state presenter: centered hero icon, title, description and an optional
/// action slot. Replaces the ad-hoc per-page empty markup. The visual tree lives in
/// Styles/EmptyState.axaml and follows the InfoBar conventions.
/// </summary>
public class EmptyState : ContentControl
{
    public static readonly StyledProperty<SymbolRegular> IconProperty = AvaloniaProperty.Register<EmptyState, SymbolRegular>(
        nameof(Icon),
        SymbolRegular.Search24);

    public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<EmptyState, string?>(
        nameof(Title),
        string.Empty);

    public static readonly StyledProperty<string?> DescriptionProperty = AvaloniaProperty.Register<EmptyState, string?>(
        nameof(Description),
        string.Empty);

    public static readonly StyledProperty<object?> ActionContentProperty = AvaloniaProperty.Register<EmptyState, object?>(
        nameof(ActionContent));

    /// <summary>Hero glyph shown above the title. Defaults to <see cref="SymbolRegular.Search24" />.</summary>
    public SymbolRegular Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Primary message. Collapses when null or empty.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Secondary wrapped message under the title. Collapses when null or empty.</summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Optional action slot (e.g. a Button) rendered under the description.</summary>
    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }
}
