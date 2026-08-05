using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia;

public partial class MainWindow : Window
{
    private readonly IPlatformServices _platformServices;
    private string _activePage = MainNavigation.Dashboard;

    /// <summary>
    /// Gets the route currently rendered by the shell.
    /// </summary>
    public string ActiveRoute => _activePage;

    public MainWindow(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        InitializeComponent();
        ApplyNavigationVisibility();
        ApplyTextDirection(LocalizationRuntime.CurrentCulture);
        Loaded += OnLoaded;
        
        // Handle window state changes (minimize/restore)
        PropertyChanged += OnWindowPropertyChanged;
        SizeChanged += OnWindowSizeChanged;
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // Force UI refresh when window state changes
        if (e.Property == WindowStateProperty)
        {
            ApplyWindowBackdrop();

            if (WindowState == WindowState.Minimized
                && Application.Current is App app
                && app.MinimizeToTrayEnabled)
            {
                Hide();
                return;
            }

            if (WindowState == WindowState.Normal || WindowState == WindowState.Maximized)
            {
                // Invalidate visual to force redraw
                InvalidateVisual();
            }
        }
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // Trigger layout update when window is resized (including from minimized state)
        InvalidateArrange();
        InvalidateMeasure();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        ApplyWindowBackdrop();
        ApplyNavigationVisibility();
        // Show DashboardPage by default on startup
        ShowDashboardPage();
    }

    /// <summary>
    /// Applies the persisted backdrop preference to the current top-level window.
    /// Avalonia accepts the levels in priority order and uses the first level the
    /// platform supports, so unsupported Mica/Acrylic implementations degrade to
    /// blur and finally an opaque window instead of rendering an empty black layer.
    /// </summary>
    public void ApplyWindowBackdrop()
    {
        if (WindowState == WindowState.Minimized)
        {
            TransparencyLevelHint = [WindowTransparencyLevel.None];
            return;
        }

#if WINDOWS
        var style = Services.WindowsAvaloniaSettingsService.SharedApplicationSettings
            .Store.WindowBackdropStyle
            .ToString();

        TransparencyLevelHint = style switch
        {
            "Windows" =>
            [
                WindowTransparencyLevel.Mica,
                WindowTransparencyLevel.AcrylicBlur,
                WindowTransparencyLevel.Blur,
                WindowTransparencyLevel.None,
            ],
            "macOS" =>
            [
                WindowTransparencyLevel.AcrylicBlur,
                WindowTransparencyLevel.Blur,
                WindowTransparencyLevel.None,
            ],
            _ => [WindowTransparencyLevel.None],
        };
#else
        TransparencyLevelHint = [WindowTransparencyLevel.None];
#endif

        if (this.TryFindResource("AppBackgroundBrush", out var fallback)
            && fallback is IBrush brush)
        {
            TransparencyBackgroundFallback = brush;
        }
    }

    private void DashboardButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.Dashboard);
    }

    private void KeyboardButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.Keyboard);
    }

    private void ActionsButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.Actions);
    }

    private void MacroButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.Macro);
    }

    private void WindowsOptimizationButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.WindowsOptimization);
    }

    private void PluginExtensionsButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.PluginExtensions);
    }

    private void AboutButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.About);
    }

    private void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        Navigate(MainNavigation.Settings);
    }

    private void OnPluginActionRequested(string actionKey)
    {
        const string openPrefix = "plugin-open:";
        const string settingsPrefix = "plugin-settings:";
        if (actionKey.StartsWith(openPrefix, StringComparison.OrdinalIgnoreCase))
            Navigate(MainNavigation.CreatePluginRoute(actionKey[openPrefix.Length..]));
        else if (actionKey.StartsWith(settingsPrefix, StringComparison.OrdinalIgnoreCase))
            Navigate(MainNavigation.CreatePluginSettingsRoute(actionKey[settingsPrefix.Length..]));
    }

    private void ShowDashboardPage()
    {
        _activePage = MainNavigation.Dashboard;
        MainContent.Content = new DashboardPage(_platformServices, Navigate);
        SetActiveButton(DashboardButton);
    }

    private void ShowAboutPage()
    {
        _activePage = MainNavigation.About;
        MainContent.Content = new AboutPage();
        SetActiveButton(AboutButton);
    }

    public void ShowSettingsPage()
    {
        _activePage = MainNavigation.Settings;
        MainContent.Content = new SettingsPage(_platformServices);
        SetActiveButton(SettingsButton);
    }

    private void ShowFeaturePage(string route)
    {
        MainContent.Content = route switch
        {
            MainNavigation.Keyboard => new KeyboardBacklightPage(_platformServices),
            MainNavigation.Actions => new ActionsPage(_platformServices),
            MainNavigation.Macro => new MacroPage(_platformServices),
            MainNavigation.WindowsOptimization => new WindowsOptimizationPage(_platformServices),
            MainNavigation.PluginExtensions => new PluginExtensionsPage(_platformServices, OnPluginActionRequested),
            _ => throw new ArgumentOutOfRangeException(nameof(route), route, "Unknown feature route."),
        };
        _activePage = route;
        SetActiveButton(GetNavigationButton(route));
    }

    private void ShowPluginPage(string route)
    {
        var isSettings = MainNavigation.TryGetPluginSettingsId(route, out var settingsPluginId);
        if (!isSettings && !MainNavigation.TryGetPluginId(route, out settingsPluginId))
            return;

        var pluginId = settingsPluginId;
        _activePage = isSettings
            ? MainNavigation.CreatePluginSettingsRoute(pluginId)
            : MainNavigation.CreatePluginRoute(pluginId);
        MainContent.Content = new PluginHostedPage(
            _platformServices,
            pluginId,
            () => Navigate(MainNavigation.PluginExtensions),
            isSettings);
        SetActiveButton(PluginExtensionsButton);
    }

    /// <summary>
    /// Navigates to a route supported by this host. Unknown routes are ignored so
    /// plugin or WPF-only links cannot leave the content surface in a blank state.
    /// </summary>
    public void Navigate(string? route)
    {
        if (MainNavigation.TryGetPluginId(route, out _)
            || MainNavigation.TryGetPluginSettingsId(route, out _))
        {
            ShowPluginPage(route!.Trim());
            return;
        }

        switch (route?.Trim().ToLowerInvariant())
        {
            case MainNavigation.Dashboard:
                ShowDashboardPage();
                break;
            case MainNavigation.About:
                ShowAboutPage();
                break;
            case MainNavigation.Settings:
                ShowSettingsPage();
                break;
            case MainNavigation.Keyboard:
            case MainNavigation.Actions:
            case MainNavigation.Macro:
            case MainNavigation.WindowsOptimization:
            case MainNavigation.PluginExtensions:
                ShowFeaturePage(route!.Trim().ToLowerInvariant());
                break;
        }
    }

    public void RefreshForCulture()
    {
        ApplyTextDirection(LocalizationRuntime.CurrentCulture);
        Title = Localization.AvaloniaLocalization.GetString("Window_Title", "Universal Device Toolkit");

        ApplyNavigationVisibility();
        Navigate(_activePage);
    }

    /// <summary>
    /// Applies the persisted optional-navigation settings to the shell. This is
    /// kept on the window so settings changes take effect without restarting.
    /// </summary>
    public void ApplyNavigationVisibility()
    {
#if WINDOWS
        var settings = Services.WindowsAvaloniaSettingsService.SharedApplicationSettings
            .Store.NavigationItemsVisibility;
#else
        IReadOnlyDictionary<string, bool>? settings = null;
#endif

        SetNavigationVisibility(KeyboardButton, MainNavigation.Keyboard, settings);
        SetNavigationVisibility(ActionsButton, MainNavigation.Actions, settings);
        SetNavigationVisibility(MacroButton, MainNavigation.Macro, settings);
        SetNavigationVisibility(WindowsOptimizationButton, MainNavigation.WindowsOptimization, settings);
        SetNavigationVisibility(PluginExtensionsButton, MainNavigation.PluginExtensions, settings);
        SetNavigationVisibility(AboutButton, MainNavigation.About, settings);

        var activeBaseRoute = MainNavigation.TryGetPluginId(_activePage, out _)
            || MainNavigation.TryGetPluginSettingsId(_activePage, out _)
            ? MainNavigation.PluginExtensions
            : _activePage;
        if (!NavigationVisibilityPolicy.IsVisible(activeBaseRoute, settings)
            && !string.Equals(_activePage, MainNavigation.Dashboard, StringComparison.OrdinalIgnoreCase))
        {
            ShowDashboardPage();
        }
    }

    private static void SetNavigationVisibility(
        Button button,
        string route,
        IReadOnlyDictionary<string, bool>? settings)
    {
        button.IsVisible = NavigationVisibilityPolicy.IsVisible(route, settings);
    }

    private void ApplyTextDirection(System.Globalization.CultureInfo culture)
    {
        FlowDirection = LocalizationCatalog.IsRightToLeft(culture)
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
    }

    private void SetActiveButton(Button activeButton)
    {
        foreach (var btn in new[]
                 {
                     DashboardButton,
                     KeyboardButton,
                     ActionsButton,
                     MacroButton,
                     WindowsOptimizationButton,
                     PluginExtensionsButton,
                     SettingsButton,
                     AboutButton,
                 })
        {
            btn.Classes.Set("active", btn == activeButton);
        }
    }

    private Button GetNavigationButton(string route) => route switch
    {
        MainNavigation.Dashboard => DashboardButton,
        MainNavigation.Keyboard => KeyboardButton,
        MainNavigation.Actions => ActionsButton,
        MainNavigation.Macro => MacroButton,
        MainNavigation.WindowsOptimization => WindowsOptimizationButton,
        MainNavigation.PluginExtensions => PluginExtensionsButton,
        MainNavigation.Settings => SettingsButton,
        MainNavigation.About => AboutButton,
        _ when MainNavigation.TryGetPluginId(route, out _) => PluginExtensionsButton,
        _ => DashboardButton,
    };

}
