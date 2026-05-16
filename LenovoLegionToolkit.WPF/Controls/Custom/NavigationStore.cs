using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LenovoLegionToolkit.WPF.Controls.Custom;

public class NavigationStore : Control
{
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

    public NavigationItem? Current { get; private set; }

    public NavigationStore()
    {
        Items = [];
        Footer = [];
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

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        AttachHandlers(Items);
        AttachHandlers(Footer);
    }

    private void NavigationStore_Loaded(object sender, RoutedEventArgs e)
    {
        if (Current is not null || Items.Count == 0)
            return;

        var index = SelectedPageIndex >= 0 && SelectedPageIndex < Items.Count ? SelectedPageIndex : 0;
        Navigate(Items[index]);
    }

    private void AttachHandlers(ObservableCollection<NavigationItem> items)
    {
        foreach (var item in items)
            item.Click += NavigationItem_Click;

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
                    item.Click += NavigationItem_Click;
            }
        };
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
        SetCurrent(item);
        return true;
    }

    private void SetCurrent(NavigationItem item)
    {
        foreach (var navigationItem in Items.Concat(Footer))
            navigationItem.IsActive = ReferenceEquals(navigationItem, item);

        Current = item;
        SelectedPageIndex = Items.IndexOf(item);
    }
}
