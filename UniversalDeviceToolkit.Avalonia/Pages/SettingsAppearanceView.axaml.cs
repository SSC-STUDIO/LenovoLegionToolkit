using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using RoutedEventArgs = global::Avalonia.Interactivity.RoutedEventArgs;

namespace UniversalDeviceToolkit.Avalonia.Pages;

public partial class SettingsAppearanceView : UserControl
{
    public SettingsAppearanceView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var currentTheme = Application.Current?.RequestedThemeVariant;
        if (ThemeComboBox is { } comboBox)
        {
            var targetTag = currentTheme switch
            {
                var v when v == ThemeVariant.Light => "Light",
                var v when v == ThemeVariant.Dark => "Dark",
                _ => "Default"
            };

            foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
            {
                if (item.Tag?.ToString() == targetTag)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }
    }

    private void ThemeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item)
        {
            var theme = item.Tag?.ToString() switch
            {
                "Light" => ThemeVariant.Light,
                "Dark" => ThemeVariant.Dark,
                _ => ThemeVariant.Default
            };

            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = theme;
            }
        }
    }
}
