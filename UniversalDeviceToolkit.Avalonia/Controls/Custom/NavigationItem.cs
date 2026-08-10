using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using UniversalDeviceToolkit.Avalonia.Controls;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

// AVALONIA: WPF ButtonBase (ContentControl) replaced by Avalonia Button so Content,
// Click and IsEnabled come from the base; AutomationPeer removed. All public members kept.
public class NavigationItem : Avalonia.Controls.Button
{
    public static readonly StyledProperty<string?> PageTagProperty = AvaloniaProperty.Register<NavigationItem, string?>(
        nameof(PageTag),
        string.Empty);

    public static readonly StyledProperty<Type?> PageTypeProperty = AvaloniaProperty.Register<NavigationItem, Type?>(
        nameof(PageType));

    public static readonly StyledProperty<bool> IsActiveProperty = AvaloniaProperty.Register<NavigationItem, bool>(
        nameof(IsActive),
        false);

    public static readonly StyledProperty<bool> CacheProperty = AvaloniaProperty.Register<NavigationItem, bool>(
        nameof(Cache),
        true);

    public static readonly StyledProperty<Uri?> PageSourceProperty = AvaloniaProperty.Register<NavigationItem, Uri?>(
        nameof(PageSource));

    public static readonly StyledProperty<SymbolRegular> IconProperty = AvaloniaProperty.Register<NavigationItem, SymbolRegular>(
        nameof(Icon),
        SymbolRegular.Empty);

    public static readonly StyledProperty<bool> IconFilledProperty = AvaloniaProperty.Register<NavigationItem, bool>(
        nameof(IconFilled),
        false);

    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<NavigationItem, double>(
        nameof(IconSize),
        16d);

    public static readonly StyledProperty<IBrush?> IconForegroundProperty = AvaloniaProperty.Register<NavigationItem, IBrush?>(
        nameof(IconForeground));

    public static readonly StyledProperty<Bitmap?> ImageProperty = AvaloniaProperty.Register<NavigationItem, Bitmap?>(
        nameof(Image));

    public static readonly StyledProperty<bool> HasImageProperty = AvaloniaProperty.Register<NavigationItem, bool>(
        nameof(HasImage),
        false);

    public string? PageTag
    {
        get => GetValue(PageTagProperty);
        set => SetValue(PageTagProperty, value);
    }

    public Type? PageType
    {
        get => GetValue(PageTypeProperty);
        set => SetValue(PageTypeProperty, value);
    }

    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public bool Cache
    {
        get => GetValue(CacheProperty);
        set => SetValue(CacheProperty, value);
    }

    public Uri? PageSource
    {
        get => GetValue(PageSourceProperty);
        set => SetValue(PageSourceProperty, value);
    }

    public Uri? AbsolutePageSource => PageSource;

    public SymbolRegular Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IconFilled
    {
        get => GetValue(IconFilledProperty);
        set => SetValue(IconFilledProperty, value);
    }

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public IBrush? IconForeground
    {
        get => GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }

    public Bitmap? Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public bool HasImage
    {
        get => GetValue(HasImageProperty);
        private set => SetValue(HasImageProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ImageProperty)
            HasImage = Image is not null;
    }
}
