using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Controls.Custom;

public class NavigationItem : Wpf.Ui.Controls.NavigationViewItem
{
    static NavigationItem()
    {
        ContentProperty.OverrideMetadata(typeof(NavigationItem), new FrameworkPropertyMetadata(null, OnContentChanged));
    }

    public string? DisplayContent
    {
        get => (string?)GetValue(DisplayContentProperty);
        set => SetValue(DisplayContentProperty, value);
    }

    public static readonly DependencyProperty DisplayContentProperty =
        DependencyProperty.Register(
            nameof(DisplayContent),
            typeof(string),
            typeof(NavigationItem),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public IconElement? DisplayIcon
    {
        get => (IconElement?)GetValue(DisplayIconProperty);
        set => SetValue(DisplayIconProperty, value);
    }

    public static readonly DependencyProperty DisplayIconProperty =
        DependencyProperty.Register(
            nameof(DisplayIcon),
            typeof(IconElement),
            typeof(NavigationItem),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public SymbolRegular Symbol
    {
        get => (SymbolRegular)GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    public static readonly DependencyProperty SymbolProperty =
        DependencyProperty.Register(
            nameof(Symbol),
            typeof(SymbolRegular),
            typeof(NavigationItem),
            new FrameworkPropertyMetadata(
                SymbolRegular.Empty,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
                OnSymbolChanged));

    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(
            nameof(IconSize),
            typeof(double),
            typeof(NavigationItem),
            new FrameworkPropertyMetadata(24.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush? IconForeground
    {
        get => (Brush?)GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }

    public static readonly DependencyProperty IconForegroundProperty =
        DependencyProperty.Register(nameof(IconForeground), typeof(Brush), typeof(NavigationItem), new PropertyMetadata(null));

    public ImageSource? Image
    {
        get => (ImageSource?)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public static readonly DependencyProperty ImageProperty =
        DependencyProperty.Register(nameof(Image), typeof(ImageSource), typeof(NavigationItem), new PropertyMetadata(null));

    public new SymbolRegular Icon
    {
        get => Symbol;
        set => Symbol = value;
    }

    private EventHandler? _invoked;

    public event EventHandler Invoked
    {
        add => _invoked += value;
        remove => _invoked -= value;
    }

    internal bool HasInvokeHandler => _invoked is not null;

    internal void InvokeFromAutomation()
    {
        if (!IsEnabled)
            throw new ElementNotEnabledException();

        Invoke();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NavigationItemAutomationPeer(this);

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (Invoke())
            e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key is not Key.Enter and not Key.Space)
            return;

        if (Invoke())
            e.Handled = true;
    }

    private bool Invoke()
    {
        if (!IsEnabled || _invoked is null)
            return false;

        _invoked.Invoke(this, EventArgs.Empty);
        return true;
    }

    private static void OnSymbolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = (NavigationItem)d;
        var symbol = (SymbolRegular)e.NewValue;
        var icon = symbol == SymbolRegular.Empty ? null : new SymbolIcon { Symbol = symbol, FontSize = item.IconSize };
        item.SetCurrentValue(DisplayIconProperty, icon);
        item.SetCurrentValue(Wpf.Ui.Controls.NavigationViewItem.IconProperty, icon);
    }

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = (NavigationItem)d;
        if (item.DisplayContent is not null)
            return;

        if (e.NewValue is string text)
            item.SetCurrentValue(DisplayContentProperty, text);
    }

    private class NavigationItemAutomationPeer(NavigationItem owner) : FrameworkElementAutomationPeer(owner), IInvokeProvider
    {
        protected override string GetClassNameCore() => nameof(NavigationItem);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.ItemContainer)
                return this;

            if (patternInterface == PatternInterface.Invoke && owner.HasInvokeHandler)
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
