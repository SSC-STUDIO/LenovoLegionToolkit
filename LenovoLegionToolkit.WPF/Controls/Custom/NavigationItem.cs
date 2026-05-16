using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Controls.Custom;

public class NavigationItem : ButtonBase
{
    public static readonly DependencyProperty PageTagProperty = DependencyProperty.Register(
        nameof(PageTag),
        typeof(string),
        typeof(NavigationItem),
        new PropertyMetadata(string.Empty));

    public string PageTag
    {
        get => (string)GetValue(PageTagProperty);
        set => SetValue(PageTagProperty, value);
    }

    public static readonly DependencyProperty PageTypeProperty = DependencyProperty.Register(
        nameof(PageType),
        typeof(Type),
        typeof(NavigationItem),
        new PropertyMetadata(null));

    public Type? PageType
    {
        get => (Type?)GetValue(PageTypeProperty);
        set => SetValue(PageTypeProperty, value);
    }

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive),
        typeof(bool),
        typeof(NavigationItem),
        new PropertyMetadata(false));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public static readonly DependencyProperty CacheProperty = DependencyProperty.Register(
        nameof(Cache),
        typeof(bool),
        typeof(NavigationItem),
        new PropertyMetadata(true));

    public bool Cache
    {
        get => (bool)GetValue(CacheProperty);
        set => SetValue(CacheProperty, value);
    }

    public static readonly DependencyProperty PageSourceProperty = DependencyProperty.Register(
        nameof(PageSource),
        typeof(Uri),
        typeof(NavigationItem),
        new PropertyMetadata(null));

    public Uri? PageSource
    {
        get => (Uri?)GetValue(PageSourceProperty);
        set => SetValue(PageSourceProperty, value);
    }

    public Uri? AbsolutePageSource => PageSource;

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(SymbolRegular),
        typeof(NavigationItem),
        new PropertyMetadata(SymbolRegular.Empty));

    public SymbolRegular Icon
    {
        get => (SymbolRegular)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty IconFilledProperty = DependencyProperty.Register(
        nameof(IconFilled),
        typeof(bool),
        typeof(NavigationItem),
        new PropertyMetadata(false));

    public bool IconFilled
    {
        get => (bool)GetValue(IconFilledProperty);
        set => SetValue(IconFilledProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
        nameof(IconSize),
        typeof(double),
        typeof(NavigationItem),
        new PropertyMetadata(16d));

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty IconForegroundProperty = DependencyProperty.Register(
        nameof(IconForeground),
        typeof(Brush),
        typeof(NavigationItem),
        new PropertyMetadata(null));

    public Brush? IconForeground
    {
        get => (Brush?)GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }

    public static readonly DependencyProperty ImageProperty = DependencyProperty.Register(
        nameof(Image),
        typeof(BitmapSource),
        typeof(NavigationItem),
        new PropertyMetadata(null));

    public BitmapSource? Image
    {
        get => (BitmapSource?)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NavigationItemAutomationPeer(this);

    internal void InvokeFromAutomation()
    {
        if (!IsEnabled)
            throw new ElementNotEnabledException();

        OnClick();
    }

    private class NavigationItemAutomationPeer(NavigationItem owner) : FrameworkElementAutomationPeer(owner), IInvokeProvider
    {
        protected override string GetClassNameCore() => nameof(NavigationItem);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ItemContainer)
                return this;

            if (patternInterface == PatternInterface.Invoke)
                return this;

            return base.GetPattern(patternInterface);
        }

        public void Invoke()
        {
            if (owner.Dispatcher.CheckAccess())
            {
                owner.InvokeFromAutomation();
                return;
            }

            owner.Dispatcher.Invoke(owner.InvokeFromAutomation);
        }

        protected override string GetNameCore()
        {
            var result = base.GetNameCore() ?? string.Empty;

            if (result == string.Empty)
                result = AutomationProperties.GetName(owner);

            return result;
        }
    }
}
