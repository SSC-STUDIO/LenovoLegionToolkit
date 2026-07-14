using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Resources;
using UniversalDeviceToolkit.WPF.Utils;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace UniversalDeviceToolkit.WPF.Windows.Utils
{
    /// <summary>
    /// Window to notify users about a previous crash and allow them to view or delete the crash report.
    /// </summary>
    public partial class CrashReportNotificationWindow : FluentWindow
    {
        private readonly string _crashReportPath;
        private readonly CrashReport? _crashReport;
        private static string T(string key, string fallback) => LocalizationHelper.GetStringOrEnglish(Resource.ResourceManager, key, fallback, Resource.Culture);

        /// <summary>
        /// Creates a new crash report notification window.
        /// </summary>
        /// <param name="crashReportPath">The path to the crash report file.</param>
        public CrashReportNotificationWindow(string crashReportPath)
        {
            InitializeComponent();
            _crashReportPath = crashReportPath;
            _crashReport = CrashReportHelper.LoadCrashReport(crashReportPath);
            _closeButton.Content = T("CrashReportNotification_Close", "Close");

            InitializeCrashDisplay();
        }

        private void InitializeCrashDisplay()
        {
            _reportPathTextBlock.Text = string.Format(
                T("CrashReportNotification_ReportPath", "Crash report saved to: {0}"),
                _crashReportPath);

            if (_crashReport is null)
            {
                _crashDetailsTextBlock.Text = T(
                    "CrashReportNotification_UnableToLoad",
                    "Unable to load crash report details.");
                return;
            }

            // Compact, readable summary first; stack trace last and bounded.
            var details = new StringBuilder();
            details.AppendLine($"{T("CrashReportNotification_Field_Time", "Time")}: {_crashReport.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            details.AppendLine($"{T("CrashReportNotification_Field_Version", "Version")}: {_crashReport.AppVersion}");
            details.AppendLine($"{T("CrashReportNotification_Field_Uptime", "Uptime")}: {_crashReport.Uptime:hh\\:mm\\:ss}");
            details.AppendLine();
            details.AppendLine($"{T("CrashReportNotification_Field_Exception", "Exception")}: {_crashReport.ExceptionType}");
            details.AppendLine($"{T("CrashReportNotification_Field_Message", "Message")}: {_crashReport.ExceptionMessage}");

            if (!string.IsNullOrEmpty(_crashReport.InnerExceptionType))
            {
                details.AppendLine();
                details.AppendLine($"{T("CrashReportNotification_Field_Inner", "Inner")}: {_crashReport.InnerExceptionType}");
                details.AppendLine($"{T("CrashReportNotification_Field_InnerMessage", "Inner message")}: {_crashReport.InnerExceptionMessage}");
            }

            if (!string.IsNullOrEmpty(_crashReport.StackTrace))
            {
                details.AppendLine();
                details.AppendLine($"{T("CrashReportNotification_Field_Stack", "Stack trace")}:");
                var stackTrace = _crashReport.StackTrace;
                const int maxStackChars = 1200;
                if (stackTrace.Length > maxStackChars)
                    stackTrace = stackTrace[..maxStackChars] + Environment.NewLine + "…";
                details.Append(stackTrace);
            }

            _crashDetailsTextBlock.Text = details.ToString().TrimEnd();
        }

        private void ApplyTheme()
        {
            try
            {
                // Try to detect system theme
                var isDarkMode = LenovoLegionToolkit.Lib.System.SystemTheme.IsDarkMode();
                var themeType = isDarkMode ? ApplicationTheme.Dark : ApplicationTheme.Light;

                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Applying theme to CrashReportNotificationWindow: {themeType}");

                var backgroundType = RenderingCompatibilityHelper.GetPreferredBackgroundType();
                ApplicationThemeManager.Apply(themeType, backgroundType, false);
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to detect system theme for crash notification", ex);

                // Fallback to light theme
                ApplicationThemeManager.Apply(ApplicationTheme.Light, RenderingCompatibilityHelper.GetPreferredBackgroundType(), false);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
        }

        private void OpenReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(_crashReportPath) && IsSafeFilePath(_crashReportPath))
                {
                    using var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = _crashReportPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    // If file doesn't exist, open the directory
                    var directory = Path.GetDirectoryName(_crashReportPath);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                    {
                        using var process = Process.Start("explorer.exe", directory);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to open crash report: {ex.Message}", ex);

                System.Windows.MessageBox.Show(
                    string.Format(T("CrashReportNotification_OpenFailed", "Failed to open crash report: {0}"), ex.Message),
                    AppIdentity.DisplayName,
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }

        private void DeleteReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CrashReportHelper.DeleteCrashReport(_crashReportPath);
                Close();
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to delete crash report: {ex.Message}", ex);
            }
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
