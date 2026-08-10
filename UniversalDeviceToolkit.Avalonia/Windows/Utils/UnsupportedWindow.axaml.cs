using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Styling;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows.Utils
{
public partial class UnsupportedWindow : BaseWindow
{
    private readonly TaskCompletionSource<bool> _taskCompletionSource = new();

    public Task<bool> ShouldContinue => _taskCompletionSource.Task;

    public static readonly StyledProperty<bool> IsCountdownCompleteProperty =
        AvaloniaProperty.Register<UnsupportedWindow, bool>(nameof(IsCountdownComplete), false);

    public bool IsCountdownComplete
    {
        get => GetValue(IsCountdownCompleteProperty);
        set => SetValue(IsCountdownCompleteProperty, value);
    }

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
            var themeVariant = isDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Applying theme to UnsupportedWindow: {themeVariant} (System is {(isDarkMode ? "Dark" : "Light")} mode)");

            // AVALONIA: Wpf.Ui ApplicationThemeManager.Apply replaced by
            // RequestedThemeVariant + per-window TransparencyLevelHint.
            Application.Current.RequestedThemeVariant = themeVariant;
            TransparencyLevelHint = new[] { RenderingCompatibilityHelper.GetPreferredBackgroundType() };
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to detect system theme, defaulting to Light mode", ex);

            // If theme detection fails, fall back to light theme
            Application.Current.RequestedThemeVariant = ThemeVariant.Light;
            TransparencyLevelHint = new[] { RenderingCompatibilityHelper.GetPreferredBackgroundType() };
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Ensure theme is applied when window is loaded
            ApplyTheme();

            var continueButton = GetContinueButton();
            if (continueButton is null)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace("UnsupportedWindow continue button was not initialized.");

                return;
            }
        
            var continueText = Resource.Continue;
            for (var i = 5; i > 0; i--)
            {
                continueButton.Content = $"{continueText} ({i})";
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            continueButton.Content = continueText;
            IsCountdownComplete = true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Exception in {nameof(Window_Loaded)}.", ex);
        }
    }

    private UniversalDeviceToolkit.Avalonia.Controls.Button? GetContinueButton()
    {
        // AVALONIA: the x:Name field is always generated from XAML after InitializeComponent;
        // the WPF FindName/LogicalTreeHelper fallbacks are no longer needed.
        return _continueButton;
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

    private void ContributionLink_Click(object sender, RoutedEventArgs e)
    {
        if (!Uri.TryCreate(Constants.ContributionUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return;

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace("Failed to open URL");
        }
    }
}
}
