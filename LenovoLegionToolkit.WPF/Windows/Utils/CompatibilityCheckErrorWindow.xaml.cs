using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.WPF.Resources;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Theme = Wpf.Ui.Appearance.Theme;

namespace LenovoLegionToolkit.WPF.Windows.Utils;

public partial class CompatibilityCheckErrorWindow : UiWindow
{
    private readonly Exception _exception;
    private readonly string _logFilePath;

    public CompatibilityCheckErrorWindow(Exception exception)
    {
        InitializeComponent();
        _exception = exception;
        _logFilePath = Log.Instance.LogPath;
        
        InitializeErrorDisplay();
    }

    private void InitializeErrorDisplay()
    {
        // Set main error message
        _mainMessageTextBlock.Text = Resource.CompatibilityCheckError_Message;

        // Build detailed error information
        var errorDetails = new System.Text.StringBuilder();
        errorDetails.AppendLine($"Exception Type: {_exception.GetType().Name}");
        errorDetails.AppendLine($"Message: {_exception.Message}");
        
        if (_exception.InnerException != null)
        {
            errorDetails.AppendLine();
            errorDetails.AppendLine($"Inner Exception: {_exception.InnerException.GetType().Name}");
            errorDetails.AppendLine($"Inner Message: {_exception.InnerException.Message}");
        }

        if (!string.IsNullOrEmpty(_exception.StackTrace))
        {
            errorDetails.AppendLine();
            errorDetails.AppendLine("Stack Trace:");
            errorDetails.AppendLine(_exception.StackTrace);
        }

        _errorDetailsTextBlock.Text = errorDetails.ToString();
    }

    private void ApplyTheme()
    {
        try
        {
            // Try to detect system theme
            var isDarkMode = Lib.System.SystemTheme.IsDarkMode();
            var themeType = isDarkMode ? ThemeType.Dark : ThemeType.Light;
            
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Applying theme to CompatibilityCheckErrorWindow: {themeType} (System is {(isDarkMode ? "Dark" : "Light")} mode)");
            
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

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyTheme();
        
        // Check if log file exists and enable/disable button accordingly
        if (File.Exists(_logFilePath))
        {
            _openLogButton.IsEnabled = true;
            
            // Automatically open log file after a short delay
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                OpenLogFile();
            };
            timer.Start();
        }
        else
        {
            _openLogButton.IsEnabled = false;
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Log file not found: {_logFilePath}");
        }
    }
    
    private void OpenLogFile()
    {
        try
        {
            if (File.Exists(_logFilePath))
            {
                // Open log file with default text editor
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _logFilePath,
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
            }
            else
            {
                // If log file doesn't exist, try to open the log directory
                var logDirectory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory))
                {
                    Process.Start("explorer.exe", logDirectory);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to open log file automatically: {ex.Message}", ex);
            
            // Don't show error dialog for automatic opening - user can still click the button
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        // Window closed by user
    }

    private void OpenLogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            OpenLogFile();
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to open log file: {ex.Message}", ex);
            
            System.Windows.MessageBox.Show(
                $"Failed to open log file: {ex.Message}",
                Resource.AppName,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
