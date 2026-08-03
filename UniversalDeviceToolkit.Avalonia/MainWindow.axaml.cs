using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using UniversalDeviceToolkit.Avalonia.Pages;
using UniversalDeviceToolkit.Avalonia.Services;

namespace UniversalDeviceToolkit.Avalonia;

public partial class MainWindow : Window
{
    private readonly IPlatformServices _platformServices;
    private string _activePage = "Dashboard";

    public MainWindow(IPlatformServices platformServices)
    {
        _platformServices = platformServices;
        InitializeComponent();
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
        ShowDashboardPage();
    }

    private void AboutButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowAboutPage();
    }

    private void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowSettingsPage();
    }

    private void ShowDashboardPage()
    {
        _activePage = "Dashboard";
        MainContent.Content = new DashboardPage(_platformServices);
        SetActiveButton(DashboardButton);
    }

    private void ShowAboutPage()
    {
        _activePage = "About";
        MainContent.Content = new AboutPage();
        SetActiveButton(AboutButton);
    }

    public void ShowSettingsPage()
    {
        _activePage = "Settings";
        MainContent.Content = new SettingsPage(_platformServices);
        SetActiveButton(SettingsButton);
    }

    public void RefreshForCulture()
    {
        Title = Localization.AvaloniaLocalization.GetString("Window_Title", "Universal Device Toolkit");

        switch (_activePage)
        {
            case "About":
                ShowAboutPage();
                break;
            case "Settings":
                ShowSettingsPage();
                break;
            default:
                ShowDashboardPage();
                break;
        }
    }

    private void SetActiveButton(Button activeButton)
    {
        foreach (var btn in new[] { DashboardButton, AboutButton, SettingsButton })
        {
            btn.Classes.Set("active", btn == activeButton);
        }
    }
}
