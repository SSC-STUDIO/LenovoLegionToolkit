using System;
using Avalonia.Controls.Primitives;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using UniversalDeviceToolkit.Avalonia.Controls.Loading;
using UniversalDeviceToolkit.Avalonia.Utils;
using UniversalDeviceToolkit.Lib.Settings;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Controls.Custom;

// AVALONIA: WPF-UI NavigationStore contract preserved (Items/Footer/Frame/Navigate/
// IsExpanded/NavigationPaneExpanded attached property). Base is ContentControl, the
// host frame container is now a ContentControl (MainWindow converts <Frame> to one),
// and the WPF animation APIs were replaced by Avalonia Transitions.
public class NavigationStore : ContentControl
{
    // The navigation shell has its own template in Styles/NavigationStore.axaml.
    // Without this key Avalonia uses ContentControl's default theme and ignores Items/Footer.
    protected override Type StyleKeyOverride => typeof(NavigationStore);

    private const string TogglePaneButtonPartName = "PART_TogglePaneButton";
    private readonly Dictionary<string, object> _pageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<object> _attachedCollections = [];
    private int _navigateGeneration;

    public static readonly StyledProperty<ObservableCollection<NavigationItem>?> ItemsProperty = AvaloniaProperty.Register<NavigationStore, ObservableCollection<NavigationItem>?>(
        nameof(Items));

    public static readonly StyledProperty<ObservableCollection<NavigationItem>?> FooterProperty = AvaloniaProperty.Register<NavigationStore, ObservableCollection<NavigationItem>?>(
        nameof(Footer));

    public static readonly StyledProperty<ContentControl?> FrameProperty = AvaloniaProperty.Register<NavigationStore, ContentControl?>(
        nameof(Frame));

    public static readonly StyledProperty<int> SelectedPageIndexProperty = AvaloniaProperty.Register<NavigationStore, int>(
        nameof(SelectedPageIndex),
        0);

    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<NavigationStore, bool>(
        nameof(IsExpanded),
        true);

    /// <summary>
    /// Inherited flag telling nav items whether the rail is expanded.
    /// AVALONIA: WPF DependencyPropertyKey (read-only attached) replaced by a plain
    /// registered attached StyledProperty with the same public getter.
    /// </summary>
    public static readonly AttachedProperty<bool> NavigationPaneExpandedProperty = AvaloniaProperty.RegisterAttached<NavigationItem, bool>(
        "NavigationPaneExpanded",
        typeof(NavigationStore),
        true,
        inherits: true);

    static NavigationStore()
    {
        IsExpandedProperty.Changed.AddClassHandler<NavigationStore>(OnIsExpandedChanged);
    }

    public ObservableCollection<NavigationItem>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public ObservableCollection<NavigationItem>? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    public ContentControl? Frame
    {
        get => GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    public int SelectedPageIndex
    {
        get => GetValue(SelectedPageIndexProperty);
        set => SetValue(SelectedPageIndexProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public static bool GetNavigationPaneExpanded(AvaloniaObject element) => (bool)element.GetValue(NavigationPaneExpandedProperty);

    public NavigationItem? Current { get; private set; }

    private UniversalDeviceToolkit.Avalonia.Controls.Button? _togglePaneButton;
    private bool _settingsLoaded;
    private bool _isApplyingInitialState;

    public NavigationStore()
    {
        Items = [];
        Footer = [];
        SetValue(NavigationPaneExpandedProperty, true);
        Loaded += NavigationStore_Loaded;
    }

    public bool Navigate(string? pageTag)
    {
        if (string.IsNullOrWhiteSpace(pageTag))
            return false;

        var item = Items!.Concat(Footer!).FirstOrDefault(i => string.Equals(i.PageTag, pageTag, StringComparison.OrdinalIgnoreCase));
        return item is not null && Navigate(item);
    }

    public bool Navigate(Type? pageType)
    {
        if (pageType is null)
            return false;

        var item = Items!.Concat(Footer!).FirstOrDefault(i => i.PageType == pageType);
        return item is not null && Navigate(item);
    }

    public void ToggleExpanded() => IsExpanded = !IsExpanded;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (_togglePaneButton is not null)
            _togglePaneButton.Click -= TogglePaneButton_Click;

        // AVALONIA: GetTemplateChild was removed; resolve the part via the template's name scope.
        _togglePaneButton = e.NameScope.Find<UniversalDeviceToolkit.Avalonia.Controls.Button>(TogglePaneButtonPartName);
        if (_togglePaneButton is not null)
            _togglePaneButton.Click += TogglePaneButton_Click;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsProperty)
            AttachHandlers(change.GetNewValue<ObservableCollection<NavigationItem>?>());
        else if (change.Property == FooterProperty)
            AttachHandlers(change.GetNewValue<ObservableCollection<NavigationItem>?>());
    }

    private static void OnIsExpandedChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
    {
        if (d is not NavigationStore store)
            return;

        store.SetValue(NavigationPaneExpandedProperty, (bool)e.NewValue!);
        store.ApplyNavigationPaneExpandedToItems();
        store.UpdateNavigationWidth(animate: !store._isApplyingInitialState);
        store.PersistNavigationPaneExpanded();
    }

    private void NavigationStore_Loaded(object? sender, RoutedEventArgs e)
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

        if (Current is not null || Items!.Count == 0)
            return;

        var index = SelectedPageIndex >= 0 && SelectedPageIndex < Items.Count ? SelectedPageIndex : 0;
        Navigate(Items[index]);
    }

    private void TogglePaneButton_Click(object? sender, RoutedEventArgs e)
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

        if (!animate || !ShouldAnimate() || Math.Abs(currentWidth - targetWidth) < 0.5)
        {
            SetNavigationWidth(targetWidth);
            return;
        }

        // AVALONIA: WPF BeginAnimation(DoubleAnimation) replaced by control Transitions.
        var duration = ResolveNavigationWidthAnimationDuration();
        Transitions = new Transitions
        {
            new DoubleTransition { Property = WidthProperty, Duration = duration },
            new DoubleTransition { Property = MinWidthProperty, Duration = duration },
            new DoubleTransition { Property = MaxWidthProperty, Duration = duration }
        };
        SetNavigationWidth(targetWidth);
    }

    private double ResolveCurrentNavigationWidth(double targetWidth)
    {
        if (Bounds.Width > 0)
            return Bounds.Width;

        if (!double.IsNaN(Width) && Width > 0)
            return Width;

        return IsExpanded ? GetNavigationWidth(false) : GetNavigationWidth(true);
    }

    private static TimeSpan ResolveNavigationWidthAnimationDuration() =>
        TryGetApplicationResource("AnimationDurationMedium") is TimeSpan ts
            ? ts
            : TimeSpan.FromMilliseconds(200);

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
        var host = TopLevel.GetTopLevel(this) as Window;
        var windowWidth = host?.Bounds.Width > 0
            ? host.Bounds.Width
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

    private void AttachHandlers(ObservableCollection<NavigationItem>? items)
    {
        if (items is null || !_attachedCollections.Add(items))
            return;

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
        foreach (var item in Items!.Concat(Footer!))
            ApplyNavigationPaneExpanded(item);
    }

    private void ApplyNavigationPaneExpanded(NavigationItem item)
    {
        item.SetValue(NavigationPaneExpandedProperty, IsExpanded);
    }

    private void NavigationItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not NavigationItem item)
            return;

        // Plugin host pages are owned by MainWindow.NavigateToPluginPage (needs plugin id).
        // Activator.CreateInstance(PluginPageWrapper) would open an empty/wrong host page.
        if (item.PageTag is string tag &&
            tag.StartsWith("plugin:", StringComparison.OrdinalIgnoreCase))
            return;

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
            // that hides their skeleton shimmer for the whole crossfade and feels like "no second animation".
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
        Dispatcher.UIThread.InvokeAsync(new Action(() =>
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
        // AVALONIA: WPF BeginAnimation fade-out replaced by DoubleTransition + delayed commit.
        if (animate && ShouldAnimate() && Frame.Content is Control outgoing)
        {
            var duration = ResolveCrossfadeDuration();
            var target = page;
            var generation = _navigateGeneration;
            outgoing.Transitions = new Transitions
            {
                new DoubleTransition { Property = OpacityProperty, Duration = duration }
            };
            outgoing.Opacity = 0;

            _ = Task.Delay(duration).ContinueWith(_ => Dispatcher.UIThread.Post(() =>
            {
                if (Frame is null || generation != _navigateGeneration)
                    return;

                CommitPage(target);
                if (Frame.Content is Control incoming)
                    SoftFadeIn(incoming, duration);
            }));
            return;
        }

        CommitPage(page);
        if (Frame.Content is Control content)
        {
            if (animate && ShouldAnimate())
                SoftFadeIn(content, ResolveCrossfadeDuration());
            else
                EnsurePageOpaque(content);
        }
    }

    private void CommitPage(object page)
    {
        if (Frame is null)
            return;

        Frame.Content = page;
        // AVALONIA: removed WPF Frame journal trimming (Avalonia ContentControl has no journal).
    }

    private static TimeSpan ResolveCrossfadeDuration() =>
        TryGetApplicationResource("AnimationDurationSkeletonCrossfade") is TimeSpan ts
            ? ts
            : TimeSpan.FromMilliseconds(280);

    private static void SoftFadeIn(Control content, TimeSpan duration)
    {
        content.Transitions = new Transitions
        {
            new DoubleTransition { Property = OpacityProperty, Duration = duration }
        };
        content.Opacity = 0;
        Dispatcher.UIThread.Post(() => content.Opacity = 1);
    }

    /// <summary>
    /// Clears any leftover Opacity animation (e.g. navigate-away fade-out left the page at 0).
    /// </summary>
    private static void EnsurePageOpaque(object page)
    {
        if (page is not Control element)
            return;

        element.Transitions = null;
        element.Opacity = 1;
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
    /// AVALONIA: WPF Page replaced by UserControl.
    /// </summary>
    private static Control CreateNavigationSkeletonShell()
    {
        static Border Block(double width, double height, Thickness margin)
        {
            var border = new Border
            {
                Width = width,
                Height = height,
                Margin = margin,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(6),
                Background = TryBrush("ControlFillColorTertiaryBrush", new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)))
            };

            // Prefer shared shimmer style when available.
            if (TryGetApplicationResource("AppSkeletonShimmerBlockStyle") is Style shimmer)
                border.Styles.Add(shimmer);

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

            var lines = new StackPanel { VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center };
            lines.Children.Add(Block(210, 14, new Thickness(0)));
            lines.Children.Add(Block(140, 10, new Thickness(0, 10, 0, 0)));
            var stretch = Block(180, 10, new Thickness(0, 12, 0, 0));
            stretch.HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch;
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

        return new UserControl
        {
            Background = Brushes.Transparent,
            Content = root,
            Focusable = false
        };
    }

    private static object? TryGetApplicationResource(string key)
    {
        var application = Application.Current;
        if (application is not null && application.TryFindResource(key, out var value))
            return value;
        return null;
    }

    private static Brush TryBrush(string key, Brush fallback)
    {
        try
        {
            if (TryGetApplicationResource(key) is Brush brush)
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

    private void SetCurrent(NavigationItem item)
    {
        foreach (var navigationItem in Items!.Concat(Footer!))
            navigationItem.IsActive = ReferenceEquals(navigationItem, item);

        Current = item;
        var index = Items!.IndexOf(item);
        if (index >= 0)
            SelectedPageIndex = index;
    }
}
