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
        // Show DashboardPage by default on startup
        ShowDashboardPage();
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

    private void ShowDashboardPage()
    {
        _activePage = MainNavigation.Dashboard;
        MainContent.Content = new DashboardPage(_platformServices);
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

    private void ShowHostCapabilityPage(string route)
    {
        var descriptor = route switch
        {
            MainNavigation.Keyboard => new HostCapabilityDescriptor(
                "MainWindow_NavigationItem_Keyboard",
                "Keyboard",
                "HostCapability_KeyboardDescription",
                "Configure keyboard backlight and keyboard-specific controls.",
                "Keyboard24",
                "HostCapability_KeyboardReason",
                "Keyboard hardware controls require the Windows device adapter."),
            MainNavigation.Actions => new HostCapabilityDescriptor(
                "MainWindow_NavigationItem_Actions",
                "Actions",
                "HostCapability_ActionsDescription",
                "Run supported device actions and hardware workflows.",
                "Rocket24",
                "HostCapability_ActionsReason",
                "Hardware action execution is not exposed by this host."),
            MainNavigation.Macro => new HostCapabilityDescriptor(
                "MainWindow_NavigationItem_Macro",
                "Macro",
                "HostCapability_MacroDescription",
                "Create and manage device macros.",
                "ReceiptPlay24",
                "HostCapability_MacroReason",
                "Macro execution requires the Windows input and device services."),
            MainNavigation.WindowsOptimization => new HostCapabilityDescriptor(
                "MainWindow_NavigationItem_WindowsOptimization",
                "System optimization",
                "HostCapability_WindowsOptimizationDescription",
                "Review Windows optimization actions and their current state.",
                "Gauge24",
                "HostCapability_WindowsOptimizationReason",
                "Windows optimization actions are only available through the Windows host."),
            MainNavigation.PluginExtensions => new HostCapabilityDescriptor(
                "MainWindow_NavigationItem_PluginExtensions",
                "Plugin Extensions",
                "HostCapability_PluginExtensionsDescription",
                "Discover and manage optional plugin extensions.",
                "Apps24",
                "HostCapability_PluginExtensionsReason",
                "Plugin discovery and installation services are not available in this host."),
            _ => throw new ArgumentOutOfRangeException(nameof(route), route, "Unknown host capability route."),
        };

        MainContent.Content = new HostCapabilityView(
            descriptor.TitleKey,
            descriptor.TitleFallback,
            Localization.AvaloniaLocalization.GetString(descriptor.DescriptionKey, descriptor.DescriptionFallback),
            descriptor.IconIdentifier,
            Localization.AvaloniaLocalization.GetString(descriptor.ReasonKey, descriptor.ReasonFallback));
        _activePage = route;
        SetActiveButton(GetNavigationButton(route));
    }

    /// <summary>
    /// Navigates to a route supported by this host. Unknown routes are ignored so
    /// plugin or WPF-only links cannot leave the content surface in a blank state.
    /// </summary>
    public void Navigate(string? route)
    {
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
                ShowHostCapabilityPage(route!.Trim().ToLowerInvariant());
                break;
        }
    }

    public void RefreshForCulture()
    {
        ApplyTextDirection(LocalizationRuntime.CurrentCulture);
        Title = Localization.AvaloniaLocalization.GetString("Window_Title", "Universal Device Toolkit");

        Navigate(_activePage);
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
        _ => DashboardButton,
    };

    private sealed record HostCapabilityDescriptor(
        string TitleKey,
        string TitleFallback,
        string DescriptionKey,
        string DescriptionFallback,
        string IconIdentifier,
        string ReasonKey,
        string ReasonFallback);
}
