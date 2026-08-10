using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using UniversalDeviceToolkit.Lib.Utils;
using UniversalDeviceToolkit.Avalonia.Resources;
using UniversalDeviceToolkit.Avalonia.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows.Utils
{
public partial class CompatibilityCheckErrorWindow : BaseWindow
{
    private readonly Exception _exception;
    private readonly string _logFilePath;
    private static string T(string key, string fallback) => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

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
            var isDarkMode = UniversalDeviceToolkit.Lib.System.SystemTheme.IsDarkMode();
            var themeVariant = isDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Applying theme to CompatibilityCheckErrorWindow: {themeVariant} (System is {(isDarkMode ? "Dark" : "Light")} mode)");

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

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyTheme();
        
        // Check if log file exists and enable/disable button accordingly
        if (File.Exists(_logFilePath))
        {
            _openLogButton.IsEnabled = true;
            
            // Automatically open log file after a short delay
            var timer = new DispatcherTimer
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
    
    private void OpenLogFile(bool showErrors = false)
    {
        try
        {
            if (File.Exists(_logFilePath) && IsSafeFilePath(_logFilePath))
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = _logFilePath,
                    UseShellExecute = true
                });
            }
            else
            {
                // If log file doesn't exist, try to open the log directory
                var logDirectory = Path.GetDirectoryName(_logFilePath);
                if (!string.IsNullOrEmpty(logDirectory) && Directory.Exists(logDirectory))
                {
                    using var process = Process.Start("explorer.exe", logDirectory);
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to open log file automatically: {ex.Message}", ex);

            if (!showErrors)
                return;

            MessageBox.Show(
                string.Format(T("CompatibilityCheckErrorWindow_OpenLogFailed", "Failed to open log file: {0}"), ex.Message),
                AppIdentity.DisplayName,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OpenLogButton_Click(object sender, RoutedEventArgs e)
    {
        OpenLogFile(showErrors: true);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static bool IsSafeFilePath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            return fullPath.IndexOfAny(Path.GetInvalidPathChars()) == -1;
        }
        catch
        {
            return false;
        }
    }
}
}
