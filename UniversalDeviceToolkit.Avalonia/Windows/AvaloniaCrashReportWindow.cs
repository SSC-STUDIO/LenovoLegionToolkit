using System.Diagnostics;
using System.Text;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using UniversalDeviceToolkit.Avalonia.Controls;
using UniversalDeviceToolkit.Avalonia.Localization;
using UniversalDeviceToolkit.Shared.Diagnostics;
using UniversalDeviceToolkit.Shared.Logging;

namespace UniversalDeviceToolkit.Avalonia.Windows;

/// <summary>
/// Shows the latest persisted report after a previous launch failed, mirroring
/// the WPF crash-report recovery flow without taking a dependency on WPF UI.
/// </summary>
internal sealed class AvaloniaCrashReportWindow : Window
{
    private const int MaximumStackTraceCharacters = 1200;

    private readonly string _reportPath;
    private readonly TextBlock _statusText = new();

    public AvaloniaCrashReportWindow(string reportPath)
    {
        _reportPath = reportPath;
        Title = Get("CrashReportNotification_Title", "Previous application error");
        Width = 640;
        Height = 520;
        MinWidth = 520;
        MinHeight = 420;
        MaxWidth = 840;
        MaxHeight = 720;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        AutomationProperties.SetAutomationId(this, "AvaloniaCrashReportNotification");
        AutomationProperties.SetName(this, Title);
        Content = BuildContent(CrashReportStore.Load(reportPath));
    }

    private Control BuildContent(CrashReport? report)
    {
        var title = new LocalizedTextBlock
        {
            Text = Get("CrashReportNotification_Message", "The previous run ended unexpectedly."),
            FontWeight = FontWeight.SemiBold,
            FontSize = 16,
            OverflowMode = UniversalDeviceToolkit.Abstractions.Localization.LocalizedOverflowMode.Wrap,
            MaxLines = 2,
        };
        var description = new LocalizedTextBlock
        {
            Text = Get("CrashReportNotification_Description", "A crash report was saved locally for review."),
            Foreground = Brushes.Gray,
            OverflowMode = UniversalDeviceToolkit.Abstractions.Localization.LocalizedOverflowMode.Wrap,
            MaxLines = 3,
        };
        var headerCopy = new StackPanel { Spacing = 4, MinWidth = 0 };
        headerCopy.Children.Add(title);
        headerCopy.Children.Add(description);

        var warningIcon = new NavigationIcon
        {
            IconIdentifier = "Warning24",
            FontSize = 24,
            Foreground = Brushes.DarkOrange,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 12 };
        header.Children.Add(warningIcon);
        Grid.SetColumn(headerCopy, 1);
        header.Children.Add(headerCopy);

        var detailHeader = new TextBlock
        {
            Text = Get("CrashReportNotification_DetailsHeading", "Details"),
            FontWeight = FontWeight.SemiBold,
        };
        var details = new TextBlock
        {
            Text = BuildDetails(report),
            FontFamily = new FontFamily("Cascadia Mono, Consolas, Courier New"),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 18,
        };
        var detailPanel = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        detailPanel.Children.Add(detailHeader);
        var scrollViewer = new ScrollViewer
        {
            Margin = new Thickness(0, 8, 0, 0),
            Content = details,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        Grid.SetRow(scrollViewer, 1);
        detailPanel.Children.Add(scrollViewer);

        var pathText = new LocalizedTextBlock
        {
            Text = string.Format(Get("CrashReportNotification_ReportPath", "Crash report saved to: {0}"), _reportPath),
            Foreground = Brushes.Gray,
            TextTrimming = TextTrimming.CharacterEllipsis,
            OverflowMode = UniversalDeviceToolkit.Abstractions.Localization.LocalizedOverflowMode.Ellipsis,
        };
        ToolTip.SetTip(pathText, pathText.Text);

        _statusText.Foreground = Brushes.OrangeRed;
        _statusText.TextWrapping = TextWrapping.Wrap;
        _statusText.IsVisible = false;

        var delete = CreateButton("CrashReportNotification_DeleteReport", "Delete report", DeleteReport);
        var open = CreateButton("CrashReportNotification_OpenReport", "Open report", OpenReport);
        var close = CreateButton("CrashReportNotification_Close", "Close", Close);
        var actions = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 10 };
        actions.Children.Add(delete);
        Grid.SetColumn(open, 1);
        actions.Children.Add(open);
        Grid.SetColumn(close, 2);
        actions.Children.Add(close);

        var content = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto,Auto"), RowSpacing = 14 };
        content.Children.Add(header);
        Grid.SetRow(detailPanel, 1);
        content.Children.Add(detailPanel);
        Grid.SetRow(pathText, 2);
        content.Children.Add(pathText);
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

    private string BuildDetails(CrashReport? report)
    {
        if (report is null)
            return Get("CrashReportNotification_UnableToLoad", "Unable to load crash report details.");

        var details = new StringBuilder();
        details.AppendLine($"{Get("CrashReportNotification_Field_Time", "Time")}: {report.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        details.AppendLine($"{Get("CrashReportNotification_Field_Version", "Version")}: {report.AppVersion}");
        details.AppendLine($"{Get("CrashReportNotification_Field_Uptime", "Uptime")}: {report.Uptime:hh\\:mm\\:ss}");
        details.AppendLine();
        details.AppendLine($"{Get("CrashReportNotification_Field_Exception", "Exception")}: {report.ExceptionType}");
        details.AppendLine($"{Get("CrashReportNotification_Field_Message", "Message")}: {report.ExceptionMessage}");

        if (!string.IsNullOrWhiteSpace(report.InnerExceptionType))
        {
            details.AppendLine();
            details.AppendLine($"{Get("CrashReportNotification_Field_Inner", "Inner")}: {report.InnerExceptionType}");
            details.AppendLine($"{Get("CrashReportNotification_Field_InnerMessage", "Inner message")}: {report.InnerExceptionMessage}");
        }

        if (!string.IsNullOrWhiteSpace(report.StackTrace))
        {
            details.AppendLine();
            details.AppendLine($"{Get("CrashReportNotification_Field_Stack", "Stack trace")}:");
            details.Append(report.StackTrace.Length <= MaximumStackTraceCharacters
                ? report.StackTrace
                : report.StackTrace[..MaximumStackTraceCharacters] + Environment.NewLine + "...");
        }

        return details.ToString().TrimEnd();
    }

    private void OpenReport()
    {
        try
        {
            var target = File.Exists(_reportPath) && IsSafePath(_reportPath)
                ? _reportPath
                : Path.GetDirectoryName(_reportPath) ?? CrashReportStore.CrashReportDirectory;
            Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
        }
        catch (Exception exception)
        {
            SharedLog.Warning("Failed to open crash report.", exception);
            _statusText.Text = string.Format(
                Get("CrashReportNotification_OpenFailed", "Failed to open crash report: {0}"),
                exception.Message);
            _statusText.IsVisible = true;
        }
    }

    private void DeleteReport()
    {
        CrashReportStore.Delete(_reportPath);
        Close();
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
}
