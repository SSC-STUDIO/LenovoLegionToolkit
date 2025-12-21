using System;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Theme = Wpf.Ui.Appearance.Theme;

namespace LenovoLegionToolkit.WPF.Windows.Utils;

public partial class UnsupportedWindow : UiWindow
{
    private readonly TaskCompletionSource<bool> _taskCompletionSource = new();

    public Task<bool> ShouldContinue => _taskCompletionSource.Task;

    public UnsupportedWindow(MachineInformation mi)
    {
        InitializeComponent();

        _vendorText.Text = mi.Vendor;
        _modelText.Text = mi.Model;
        _machineTypeText.Text = mi.MachineType;
    }

    private void ApplyTheme()
    {
        try
        {
            // Try to detect system theme
            var isDarkMode = SystemTheme.IsDarkMode();
            var themeType = isDarkMode ? ThemeType.Dark : ThemeType.Light;
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Applying theme to UnsupportedWindow: {themeType} (System is {(isDarkMode ? "Dark" : "Light")} mode)");
            
            // Apply theme with Mica background
            Theme.Apply(themeType, BackgroundType.Mica, false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to detect system theme, defaulting to Light mode", ex);
            
            // If theme detection fails, fall back to light theme
            Theme.Apply(ThemeType.Light, BackgroundType.Mica, false);
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Ensure theme is applied when window is loaded
        ApplyTheme();
        
        var continueText = Resource.Continue;
        for (var i = 5; i > 0; i--)
        {
            _continueButton.Content = $"{continueText} ({i})";
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        _continueButton.Content = continueText;
        _continueButton.IsEnabled = true;
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _taskCompletionSource.TrySetResult(false);
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        _taskCompletionSource.TrySetResult(true);
        Close();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _taskCompletionSource.TrySetResult(false);
        Close();
    }
}
