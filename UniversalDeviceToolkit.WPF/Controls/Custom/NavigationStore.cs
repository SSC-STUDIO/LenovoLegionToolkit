using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Settings;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Controls.Loading;
using UniversalDeviceToolkit.WPF.Utils;

namespace UniversalDeviceToolkit.WPF.Controls.Custom;

public class NavigationStore : Control
{
    private const string TogglePaneButtonPartName = "PART_TogglePaneButton";
    private readonly Dictionary<string, object> _pageCache = new(StringComparer.OrdinalIgnoreCase);
    private int _navigateGeneration;

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
        var currentWidth = ResolveCurrentNavigationWidth(targetWidth);

        BeginAnimation(WidthProperty, null);
        BeginAnimation(MinWidthProperty, null);
        BeginAnimation(MaxWidthProperty, null);

        if (!animate || !ShouldAnimate())
        {
            SetNavigationWidth(targetWidth);
            return;
        }

        if (Math.Abs(currentWidth - targetWidth) < 0.5)
        {
            SetNavigationWidth(targetWidth);
            return;
        }

        var animation = CreateNavigationWidthAnimation(currentWidth, targetWidth);
        animation.Completed += (_, _) => SetNavigationWidth(targetWidth);

        BeginAnimation(WidthProperty, animation);
        BeginAnimation(MinWidthProperty, animation.Clone());
        BeginAnimation(MaxWidthProperty, animation.Clone());
    }

    private double ResolveCurrentNavigationWidth(double targetWidth)
    {
        if (ActualWidth > 0)
            return ActualWidth;

        if (!double.IsNaN(Width) && Width > 0)
            return Width;

        return IsExpanded ? GetNavigationWidth(false) : GetNavigationWidth(true);
    }

    private static DoubleAnimation CreateNavigationWidthAnimation(double from, double to)
    {
        return new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = Application.Current.Resources["AnimationDurationMedium"] as Duration? ?? new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = Application.Current.Resources["AnimationEasingCubicOut"] as IEasingFunction,
            FillBehavior = FillBehavior.Stop
        };
    }

    private void SetNavigationWidth(double width)
    {
        Width = width;
        MinWidth = width;
        MaxWidth = width;
    }

    private double GetNavigationWidth()
    {
        return GetNavigationWidth(IsExpanded);
    }

    private double GetNavigationWidth(bool isExpanded)
    {
        if (!isExpanded)
            return NavigationPaneMetrics.GetCollapsedWidth();

        // Expand target scales with the host window so large monitors get a wider rail.
        var host = Window.GetWindow(this);
        var windowWidth = host?.ActualWidth > 0
            ? host.ActualWidth
            : host is { Width: > 0 } w && !double.IsNaN(w.Width)
                ? w.Width
                : 1300;
        return NavigationPaneMetrics.GetExpandedWidth(windowWidth);
    }

    /// <summary>
    /// Re-apply the expanded width after the host window size changes (max stretch scales with window).
    /// </summary>
    public void RefreshWidthForHostWindow()
    {
        if (!IsExpanded)
            return;

        UpdateNavigationWidth(animate: false);
    }

    private static bool ShouldAnimate()
    {
        try
        {
            return IoCContainer.Resolve<ApplicationSettings>().Store.AnimationsEnabled;
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "nav-should-animate",
                "Failed to read AnimationsEnabled; defaulting to animated navigation.",
                ex);
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

        // Highlight the nav item immediately so the click feels instant.
        SetCurrent(item);
        var generation = ++_navigateGeneration;

        // Explicit page instance (plugin host pages, etc.) — not cached.
        if (item.PageSource is not null)
        {
            PresentPage(item.PageSource, animate: true);
            return true;
        }

        if (item.PageType is null)
            return false;

        var cacheKey = GetPageCacheKey(item);

        if (_pageCache.TryGetValue(cacheKey, out var cached) && cached is not null)
        {
            if (ReferenceEquals(Frame.Content, cached))
            {
                // Re-selecting the same page must not leave a stuck Opacity=0 from a prior fade-out.
                EnsurePageOpaque(cached);
                return true;
            }

            // Pages that own loading chrome (plugin store, etc.) must not SoftFadeIn from 0 —
            // that hides their skeleton 流光 for the whole crossfade and feels like "no second animation".
            var animateReturn = !OwnsLoadingChrome(item.PageType);
            PresentPage(cached, animate: animateReturn);
            // Immediate commit path only — do not cancel SoftFadeIn for normal pages.
            if (!animateReturn)
                EnsurePageOpaque(cached);
            return true;
        }

        // Pages that own their loading chrome are constructed directly so the
        // generic navigation skeleton never flashes before dedicated loading UI.
        if (OwnsLoadingChrome(item.PageType))
        {
            try
            {
                var pluginPage = Activator.CreateInstance(item.PageType);
                if (pluginPage is null)
                    return false;

                _pageCache[cacheKey] = pluginPage;
                PresentPage(pluginPage, animate: false);
                EnsurePageOpaque(pluginPage);
                return true;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to create navigation page {item.PageType.FullName}", ex);
                return false;
            }
        }

        // First visit: show a lightweight skeleton shell NOW, then build the real page.
        // Skeleton was previously inside the real page — useless while CreateInstance blocks.
        PresentPage(CreateNavigationSkeletonShell(), animate: false);

        var pageType = item.PageType;
        // Create page on next Loaded tick so the skeleton can paint once, then hand off.
        // No artificial multi-hundred-ms hold — that padded every cold navigation for aesthetics.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (generation != _navigateGeneration || !ReferenceEquals(Current, item))
                return;

            try
            {
                var page = Activator.CreateInstance(pageType);
                if (page is null)
                    return;

                _pageCache[cacheKey] = page;

                if (generation != _navigateGeneration || !ReferenceEquals(Current, item))
                    return;

                // Soft handoff shell → real page (real page keeps its own loading chrome).
                PresentPage(page, animate: true);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to create navigation page {pageType.FullName}", ex);
            }
        }), DispatcherPriority.Loaded);

        return true;
    }

    private static bool OwnsLoadingChrome(Type pageType)
    {
        var metadata = pageType
            .GetCustomAttributes(typeof(LoadingChromeOwnerAttribute), inherit: true)
            .OfType<LoadingChromeOwnerAttribute>()
            .FirstOrDefault();

        return metadata?.Ownership == LoadingChromeOwnership.Page;
    }

    private void PresentPage(object page, bool animate)
    {
        if (Frame is null)
            return;

        if (ReferenceEquals(Frame.Content, page))
            return;

        // Crossfade: ease current content out, then swap + ease new content in.
        if (animate && ShouldAnimate() && Frame.Content is FrameworkElement outgoing)
        {
            outgoing.BeginAnimation(UIElement.OpacityProperty, null);
            var duration = ResolveCrossfadeDuration();
            var target = page;
            var generation = _navigateGeneration;
            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = duration,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (_, _) =>
            {
                if (Frame is null || generation != _navigateGeneration)
                    return;
                CommitPage(target);
                if (Frame.Content is FrameworkElement incoming)
                    SoftFadeIn(incoming, duration);
            };
            outgoing.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            return;
        }

        CommitPage(page);
        if (Frame.Content is FrameworkElement content)
        {
            if (animate && ShouldAnimate())
                SoftFadeIn(content, ResolveCrossfadeDuration());
            else
            {
                content.BeginAnimation(UIElement.OpacityProperty, null);
                content.Opacity = 1;
            }
        }
    }

    private void CommitPage(object page)
    {
        if (Frame is null)
            return;

        Frame.Navigate(page);
        TrimFrameJournal();
    }

    private static Duration ResolveCrossfadeDuration() =>
        Application.Current?.TryFindResource("AnimationDurationSkeletonCrossfade") as Duration?
        ?? new Duration(TimeSpan.FromMilliseconds(280));

    private static void SoftFadeIn(FrameworkElement content, Duration duration)
    {
        content.BeginAnimation(UIElement.OpacityProperty, null);
        content.Opacity = 0;
        var fade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = duration,
            FillBehavior = FillBehavior.Stop,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        fade.Completed += (_, _) =>
        {
            content.BeginAnimation(UIElement.OpacityProperty, null);
            content.Opacity = 1;
        };
        content.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    /// <summary>
    /// Clears any leftover Opacity animation (e.g. navigate-away fade-out left the page at 0).
    /// </summary>
    private static void EnsurePageOpaque(object page)
    {
        if (page is not FrameworkElement element)
            return;

        element.BeginAnimation(UIElement.OpacityProperty, null);
        element.Opacity = 1;
    }

    private void TrimFrameJournal()
    {
        if (Frame is null)
            return;

        try
        {
            while (Frame.CanGoBack)
                Frame.RemoveBackEntry();
        }
        catch (Exception ex)
        {
            // Journal may be unavailable for some content types.
            Log.Instance.TraceOnce(
                "nav-trim-journal",
                "Failed to trim navigation frame journal (content may not support it).",
                ex);
        }
    }

    private static string GetPageCacheKey(NavigationItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.PageTag))
            return item.PageTag!;
        if (item.PageType is not null)
            return item.PageType.FullName ?? item.PageType.Name;
        return item.GetHashCode().ToString();
    }

    /// <summary>
    /// Instant shell shown while a heavy Page is constructed on the UI thread.
    /// </summary>
    private static Page CreateNavigationSkeletonShell()
    {
        static Border Block(double width, double height, Thickness margin)
        {
            var border = new Border
            {
                Width = width,
                Height = height,
                Margin = margin,
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(6),
                Background = TryBrush("ControlFillColorTertiaryBrush", new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)))
            };

            // Prefer shared shimmer style when available.
            if (Application.Current?.TryFindResource("AppSkeletonShimmerBlockStyle") is Style shimmer)
                border.Style = shimmer;

            return border;
        }

        static Border Card()
        {
            var card = new Border
            {
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(16, 14, 16, 14),
                MinHeight = 88,
                CornerRadius = new CornerRadius(10),
                Background = TryBrush("ControlFillColorDefaultBrush", new SolidColorBrush(Color.FromRgb(0x30, 0x30, 0x30))),
                BorderBrush = TryBrush("ControlStrokeColorDefaultBrush", new SolidColorBrush(Color.FromRgb(0x50, 0x50, 0x50))),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var icon = Block(42, 42, new Thickness(0, 0, 12, 0));
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            var lines = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            lines.Children.Add(Block(210, 14, new Thickness(0)));
            lines.Children.Add(Block(140, 10, new Thickness(0, 10, 0, 0)));
            var stretch = Block(180, 10, new Thickness(0, 12, 0, 0));
            stretch.HorizontalAlignment = HorizontalAlignment.Stretch;
            stretch.Width = double.NaN;
            lines.Children.Add(stretch);
            Grid.SetColumn(lines, 1);
            grid.Children.Add(lines);

            card.Child = grid;
            return card;
        }

        var root = new StackPanel { Margin = new Thickness(20, 16, 20, 16) };
        root.Children.Add(Block(160, 16, new Thickness(0, 0, 0, 16)));
        root.Children.Add(Card());
        root.Children.Add(Card());
        root.Children.Add(Card());
        root.Children.Add(Card());

        return new Page
        {
            Background = Brushes.Transparent,
            Content = root,
            Focusable = false
        };
    }

    private static Brush TryBrush(string key, Brush fallback)
    {
        try
        {
            if (Application.Current?.TryFindResource(key) is Brush brush)
                return brush;
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                $"nav-brush-{key}",
                $"Failed to resolve brush resource '{key}'.",
                ex);
        }

        return fallback;
    }

    private void AnimateFrameContent()
    {
        if (Frame?.Content is not FrameworkElement content || !ShouldAnimate())
            return;

        SoftFadeIn(content, ResolveCrossfadeDuration());
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
