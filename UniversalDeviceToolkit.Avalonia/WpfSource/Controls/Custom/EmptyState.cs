using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Controls.Custom;

/// <summary>
/// Unified empty-state presenter: centered hero icon, title, description and an optional
/// action slot. Replaces the ad-hoc per-page empty markup. The visual tree lives in
/// Styles/EmptyState.xaml (merged in App.xaml) and follows the InfoBar conventions.
/// </summary>
public class EmptyState : Control
{
    static EmptyState()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(EmptyState),
            new FrameworkPropertyMetadata(typeof(EmptyState)));
    }

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(SymbolRegular),
        typeof(EmptyState),
        new FrameworkPropertyMetadata(SymbolRegular.Search24));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(EmptyState),
        new FrameworkPropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description),
        typeof(string),
        typeof(EmptyState),
        new FrameworkPropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionContentProperty = DependencyProperty.Register(
        nameof(ActionContent),
        typeof(object),
        typeof(EmptyState),
        new FrameworkPropertyMetadata(null));

    /// <summary>Hero glyph shown above the title. Defaults to <see cref="SymbolRegular.Search24" />.</summary>
    public SymbolRegular Icon
    {
        get => (SymbolRegular)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Primary message. Collapses when null or empty.</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Secondary wrapped message under the title. Collapses when null or empty.</summary>
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Optional action slot (e.g. a Button) rendered under the description.</summary>
    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }
}
