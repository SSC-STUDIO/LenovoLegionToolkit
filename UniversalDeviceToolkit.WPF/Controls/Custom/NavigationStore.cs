using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.WPF.Controls.Custom;

public class NavigationStore : Control
{
    private const string TogglePaneButtonPartName = "PART_TogglePaneButton";

    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items),
        typeof(ObservableCollection<NavigationItem>),
        typeof(NavigationStore),
        new PropertyMetadata(null));

    public ObservableCollection<NavigationItem> Items
    {
        get => (ObservableCollection<NavigationItem>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(
        nameof(Footer),
        typeof(ObservableCollection<NavigationItem>),
        typeof(NavigationStore),
        new PropertyMetadata(null));

    public ObservableCollection<NavigationItem> Footer
    {
        get => (ObservableCollection<NavigationItem>)GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    public static readonly DependencyProperty FrameProperty = DependencyProperty.Register(
        nameof(Frame),
        typeof(Frame),
        typeof(NavigationStore),
        new PropertyMetadata(null));

    public Frame? Frame
    {
        get => (Frame?)GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    public static readonly DependencyProperty SelectedPageIndexProperty = DependencyProperty.Register(
        nameof(SelectedPageIndex),
        typeof(int),
        typeof(NavigationStore),
        new PropertyMetadata(0));

    public int SelectedPageIndex
    {
        get => (int)GetValue(SelectedPageIndexProperty);
        set => SetValue(SelectedPageIndexProperty, value);
    }

    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded),
        typeof(bool),
        typeof(NavigationStore),
        new PropertyMetadata(true, OnIsExpandedChanged));

    public static readonly DependencyPropertyKey NavigationPaneExpandedPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "NavigationPaneExpanded",
        typeof(bool),
        typeof(NavigationStore),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty NavigationPaneExpandedProperty = NavigationPaneExpandedPropertyKey.DependencyProperty;

    public static bool GetNavigationPaneExpanded(DependencyObject element) => (bool)element.GetValue(NavigationPaneExpandedProperty);

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public NavigationItem? Current { get; private set; }

    private Wpf.Ui.Controls.Button? _togglePaneButton;
    private bool _settingsLoaded;
    private bool _isApplyingInitialState;

    static NavigationStore()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationStore), new FrameworkPropertyMetadata(typeof(NavigationStore)));
    }

    public NavigationStore()
    {
        Items = [];
        Footer = [];
        SetValue(NavigationPaneExpandedPropertyKey, true);
        Loaded += NavigationStore_Loaded;
    }

    public bool Navigate(string? pageTag)
    {
        if (string.IsNullOrWhiteSpace(pageTag))
            return false;

        var item = Items.Concat(Footer).FirstOrDefault(i => string.Equals(i.PageTag, pageTag, StringComparison.OrdinalIgnoreCase));
        return item is not null && Navigate(item);
    }

    public bool Navigate(Type? pageType)
    {
        if (pageType is null)
            return false;

        var item = Items.Concat(Footer).FirstOrDefault(i => i.PageType == pageType);
        return item is not null && Navigate(item);
    }

    public void ToggleExpanded() => IsExpanded = !IsExpanded;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_togglePaneButton is not null)
            _togglePaneButton.Click -= TogglePaneButton_Click;

        _togglePaneButton = GetTemplateChild(TogglePaneButtonPartName) as Wpf.Ui.Controls.Button;
        if (_togglePaneButton is not null)
            _togglePaneButton.Click += TogglePaneButton_Click;
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        AttachHandlers(Items);
        AttachHandlers(Footer);
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not NavigationStore store)
            return;

        store.SetValue(NavigationPaneExpandedPropertyKey, (bool)e.NewValue);
        store.ApplyNavigationPaneExpandedToItems();
        store.UpdateNavigationWidth(animate: !store._isApplyingInitialState);
        store.PersistNavigationPaneExpanded();
    }

    private void NavigationStore_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_settingsLoaded)
        {
            _isApplyingInitialState = true;
            try
            {
                var settings = IoCContainer.Resolve<ApplicationSettings>();
                IsExpanded = settings.Store.NavigationPaneExpanded;
            }
            catch
            {
                IsExpanded = true;
            }

            _isApplyingInitialState = false;
            _settingsLoaded = true;
            UpdateNavigationWidth(animate: false);
        }

        if (Current is not null || Items.Count == 0)
            return;

        var index = SelectedPageIndex >= 0 && SelectedPageIndex < Items.Count ? SelectedPageIndex : 0;
        Navigate(Items[index]);
    }

    private void TogglePaneButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleExpanded();
    }

    private void PersistNavigationPaneExpanded()
    {
        if (!_settingsLoaded || _isApplyingInitialState)
            return;

        try
        {
            var settings = IoCContainer.Resolve<ApplicationSettings>();
            if (settings.Store.NavigationPaneExpanded == IsExpanded)
                return;

            settings.Store.NavigationPaneExpanded = IsExpanded;
            settings.SynchronizeStore();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Couldn't persist navigation pane state.", ex);
        }
    }

    private void UpdateNavigationWidth(bool animate)
    {
        var targetWidth = GetNavigationWidth();
        Width = targetWidth;
        MinWidth = targetWidth;
        MaxWidth = targetWidth;

        if (!animate || !ShouldAnimate())
            return;

        var animation = new DoubleAnimation
        {
            From = ActualWidth > 0 ? ActualWidth : targetWidth,
            To = targetWidth,
            Duration = Application.Current.Resources["AnimationDurationMedium"] as Duration? ?? new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = Application.Current.Resources["AnimationEasingCubicOut"] as IEasingFunction
        };

        BeginAnimation(WidthProperty, animation);
        BeginAnimation(MinWidthProperty, animation);
        BeginAnimation(MaxWidthProperty, animation);
    }

    private double GetNavigationWidth()
    {
        var key = IsExpanded ? "NavigationWidthExpanded" : "NavigationWidthCollapsed";
        if (Application.Current.TryFindResource(key) is double width)
            return width;

        return IsExpanded ? 220 : 70;
    }

    private static bool ShouldAnimate()
    {
        try
        {
            return IoCContainer.Resolve<ApplicationSettings>().Store.AnimationsEnabled;
        }
        catch
        {
            return true;
        }
    }

    private void AttachHandlers(ObservableCollection<NavigationItem> items)
    {
        foreach (var item in items)
        {
            item.Click += NavigationItem_Click;
            ApplyNavigationPaneExpanded(item);
        }

        items.CollectionChanged += (_, e) =>
        {
            if (e.OldItems is not null)
            {
                foreach (NavigationItem item in e.OldItems)
                    item.Click -= NavigationItem_Click;
            }

            if (e.NewItems is not null)
            {
                foreach (NavigationItem item in e.NewItems)
                {
                    item.Click += NavigationItem_Click;
                    ApplyNavigationPaneExpanded(item);
                }
            }
        };
    }

    private void ApplyNavigationPaneExpandedToItems()
    {
        foreach (var item in Items.Concat(Footer))
            ApplyNavigationPaneExpanded(item);
    }

    private void ApplyNavigationPaneExpanded(NavigationItem item)
    {
        item.SetValue(NavigationPaneExpandedPropertyKey, IsExpanded);
    }

    private void NavigationItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is NavigationItem item)
            Navigate(item);
    }

    private bool Navigate(NavigationItem item)
    {
        if (Frame is null)
            return false;

        object? page = null;

        if (item.PageType is not null)
            page = Activator.CreateInstance(item.PageType);
        else if (item.PageSource is not null)
            page = item.PageSource;

        if (page is null)
            return false;

        Frame.Navigate(page);
        AnimateFrameContent();
        SetCurrent(item);
        return true;
    }

    private void AnimateFrameContent()
    {
        if (Frame?.Content is not FrameworkElement content || !ShouldAnimate())
            return;

        content.Opacity = 0;
        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = Application.Current.Resources["AnimationDurationMedium"] as Duration? ?? new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = Application.Current.Resources["AnimationEasingCubicOut"] as IEasingFunction
        };

        content.BeginAnimation(UIElement.OpacityProperty, fadeIn);
    }

    private void SetCurrent(NavigationItem item)
    {
        foreach (var navigationItem in Items.Concat(Footer))
            navigationItem.IsActive = ReferenceEquals(navigationItem, item);

        Current = item;
        var index = Items.IndexOf(item);
        if (index >= 0)
            SelectedPageIndex = index;
    }
}
