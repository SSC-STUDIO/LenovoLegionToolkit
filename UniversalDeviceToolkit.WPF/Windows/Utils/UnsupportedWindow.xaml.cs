using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Windows.Utils
{
public partial class UnsupportedWindow : FluentWindow, INotifyPropertyChanged
{
    private readonly TaskCompletionSource<bool> _taskCompletionSource = new();
    private bool _isContinueEnabled;

    public Task<bool> ShouldContinue => _taskCompletionSource.Task;
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsContinueEnabled
    {
        get => _isContinueEnabled;
        private set
        {
            if (_isContinueEnabled == value)
                return;

            _isContinueEnabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsContinueEnabled)));
        }
    }

    public UnsupportedWindow(MachineInformation mi)
    {
        InitializeComponent();
        DataContext = this;

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
            var themeType = isDarkMode ? ApplicationTheme.Dark : ApplicationTheme.Light;
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Applying theme to UnsupportedWindow: {themeType} (System is {(isDarkMode ? "Dark" : "Light")} mode)");
            
            var backgroundType = RenderingCompatibilityHelper.GetPreferredBackgroundType();
            ApplicationThemeManager.Apply(themeType, backgroundType, false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to detect system theme, defaulting to Light mode", ex);
            
            // If theme detection fails, fall back to light theme
            ApplicationThemeManager.Apply(ApplicationTheme.Light, RenderingCompatibilityHelper.GetPreferredBackgroundType(), false);
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
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
        IsContinueEnabled = true;
    }

    private Wpf.Ui.Controls.Button? GetContinueButton()
    {
        if (_continueButton is not null)
            return _continueButton;

        if (FindName("_continueButton") is Wpf.Ui.Controls.Button namedButton)
            return namedButton;

        return LogicalTreeHelper.FindLogicalNode(this, "_continueButton") as Wpf.Ui.Controls.Button;
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
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Constants.ContributionUrl,
                UseShellExecute = true
            });
        }
        catch { }
    }
}
}
