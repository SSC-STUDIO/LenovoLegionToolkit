#if WINDOWS

using System.Diagnostics;
using System.Text;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.Avalonia.Windows;

/// <summary>
/// Startup failure dialog shown when the hardware compatibility check throws:
/// the user can open the log folder, restart the application or exit before the
/// host shuts down with exit code 202.
/// </summary>
internal sealed class AvaloniaCompatibilityCheckErrorWindow : Window
{
    private readonly string _logDirectory;
    private readonly TextBlock _statusText = new();

    public AvaloniaCompatibilityCheckErrorWindow(Exception exception)
    {
        _logDirectory = Log.Instance.LogPath;

        Title = Get("CompatibilityCheckErrorWindow_Title", "Compatibility Check Error");
        Width = 700;
        Height = 560;
        MinWidth = 520;
        MinHeight = 420;
        MaxWidth = 900;
        MaxHeight = 720;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        AutomationProperties.SetAutomationId(this, "AvaloniaCompatibilityCheckErrorWindow");
        AutomationProperties.SetName(this, Title);
        Content = BuildContent(exception);
    }

    private Control BuildContent(Exception exception)
    {
        var title = new LocalizedTextBlock
        {
            Text = Get("CompatibilityCheckError_Message", "Error occurred when reading device information."),
            FontSize = 18,
            FontWeight = FontWeight.SemiBold,
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var description = new LocalizedTextBlock
        {
            Text = Get(
                "CompatibilityCheckErrorWindow_Description",
                "The application failed to read device information during startup. " +
                "Please check the error details below and try the troubleshooting steps if needed."),
            Foreground = GetBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var headerCopy = new StackPanel { Spacing = 4, MinWidth = 0 };
        headerCopy.Children.Add(title);
        headerCopy.Children.Add(description);

        var errorIcon = new NavigationIcon
        {
            IconIdentifier = "ErrorCircle24",
            FontSize = 24,
            Foreground = GetBrush("StatusCriticalBrush"),
            VerticalAlignment = VerticalAlignment.Top,
        };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 12 };
        header.Children.Add(errorIcon);
        Grid.SetColumn(headerCopy, 1);
        header.Children.Add(headerCopy);

        var detailsHeading = new TextBlock
        {
            Text = Get("CompatibilityCheckErrorWindow_DetailsHeading", "Error details"),
            FontWeight = FontWeight.SemiBold,
        };
        var details = new TextBox
        {
            Text = BuildDetails(exception),
            FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New"),
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 140,
            MaxHeight = 240,
        };
        var detailsPanel = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        detailsPanel.Children.Add(detailsHeading);
        Grid.SetRow(details, 1);
        detailsPanel.Children.Add(details);

        var logPath = new LocalizedTextBlock
        {
            Text = string.Format(Get("CompatibilityCheckErrorWindow_LogPath", "Log folder: {0}"), _logDirectory),
            Foreground = GetBrush("TextFillColorSecondaryBrush"),
            OverflowMode = LocalizedOverflowMode.Ellipsis,
        };
        ToolTip.SetTip(logPath, logPath.Text);

        _statusText.Foreground = GetBrush("StatusCriticalTextBrush");
        _statusText.TextWrapping = TextWrapping.Wrap;
        _statusText.IsVisible = false;

        var openLog = CreateButton("CompatibilityCheckErrorWindow_OpenLog", "Open Log Folder", OpenLogFolder);
        var restart = CreateButton("CompatibilityCheckErrorWindow_Restart", "Restart", RestartApplication);
        var exit = CreateButton("Exit", "Exit", Close);
        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { openLog, exit, restart },
        };

        var content = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto,Auto"), RowSpacing = 14 };
        content.Children.Add(header);
        Grid.SetRow(detailsPanel, 1);
        content.Children.Add(detailsPanel);
        Grid.SetRow(logPath, 2);
        content.Children.Add(logPath);
        Grid.SetRow(_statusText, 3);
        content.Children.Add(_statusText);
        Grid.SetRow(actions, 4);
        content.Children.Add(actions);

        return new Border { Padding = new Thickness(24, 20), Child = content };
    }

    private Button CreateButton(string key, string fallback, Action action)
    {
        var label = Get(key, fallback);
        var button = new Button { Content = label, MinWidth = 110 };
        button.Click += (_, _) => action();
        AutomationProperties.SetName(button, label);
        return button;
    }

    private string BuildDetails(Exception exception)
    {
        var details = new StringBuilder();
        details.AppendLine($"{Get("CompatibilityCheckErrorWindow_ExceptionType", "Exception type")}: {exception.GetType().Name}");
        details.AppendLine($"{Get("CompatibilityCheckErrorWindow_ExceptionMessage", "Message")}: {exception.Message}");

        if (exception.InnerException is { } inner)
        {
            details.AppendLine();
            details.AppendLine($"{Get("CompatibilityCheckErrorWindow_InnerException", "Inner exception")}: {inner.GetType().Name}");
            details.AppendLine($"{Get("CompatibilityCheckErrorWindow_InnerMessage", "Inner message")}: {inner.Message}");
        }

        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            details.AppendLine();
            details.AppendLine($"{Get("CompatibilityCheckErrorWindow_StackTrace", "Stack trace")}:");
            details.AppendLine(exception.StackTrace);
        }

        return details.ToString().TrimEnd();
    }

    private void OpenLogFolder()
    {
        try
        {
            var target = IsSafePath(_logDirectory) && Directory.Exists(_logDirectory)
                ? _logDirectory
                : Path.GetDirectoryName(_logDirectory);
            if (string.IsNullOrWhiteSpace(target) || !Directory.Exists(target))
                throw new DirectoryNotFoundException(_logDirectory);

            Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
        }
        catch (Exception exception)
        {
            Log.Instance.Trace("Failed to open the log folder.", exception);
            _statusText.Text = string.Format(
                Get("CompatibilityCheckErrorWindow_OpenLogFailed", "Failed to open the log folder: {0}"),
                exception.Message);
            _statusText.IsVisible = true;
        }
    }

    private void RestartApplication()
    {
        try
        {
            var processPath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(processPath))
                throw new InvalidOperationException(
                    Get("CompatibilityCheckErrorWindow_ProcessPathMissing", "The application executable path could not be determined."));

            Process.Start(new ProcessStartInfo(processPath) { UseShellExecute = true });
            Environment.Exit(201);
        }
        catch (Exception exception)
        {
            Log.Instance.Trace("Failed to restart after the compatibility check error.", exception);
            _statusText.Text = string.Format(
                Get("CompatibilityCheckErrorWindow_RestartFailed", "Failed to restart the application: {0}"),
                exception.Message);
            _statusText.IsVisible = true;
        }
    }

    private static bool IsSafePath(string path)
    {
        try
        {
            _ = Path.GetFullPath(path);
            return path.IndexOfAny(Path.GetInvalidPathChars()) < 0;
        }
        catch
        {
            return false;
        }
    }

    private static string Get(string key, string fallback) => AvaloniaLocalization.GetString(key, fallback);

    private static IBrush GetBrush(string key) =>
        Application.Current?.TryFindResource(key, out var resource) == true
        && resource is IBrush brush
            ? brush
            : Brushes.Transparent;
}

#endif
