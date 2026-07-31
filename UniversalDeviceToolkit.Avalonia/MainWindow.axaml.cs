using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using UniversalDeviceToolkit.Avalonia.Pages;

namespace UniversalDeviceToolkit.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
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
        MainContent.Content = new DashboardPage();
        SetActiveButton(DashboardButton);
    }

    private void ShowAboutPage()
    {
        MainContent.Content = new AboutPage();
        SetActiveButton(AboutButton);
    }

    public void ShowSettingsPage()
    {
        MainContent.Content = new SettingsPage();
        SetActiveButton(SettingsButton);
    }

    private void SetActiveButton(Button activeButton)
    {
        foreach (var btn in new[] { DashboardButton, AboutButton, SettingsButton })
        {
            btn.Classes.Set("active", btn == activeButton);
        }
    }
}
