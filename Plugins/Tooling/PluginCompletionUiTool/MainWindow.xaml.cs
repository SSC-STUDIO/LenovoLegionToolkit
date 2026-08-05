using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;

namespace PluginCompletionUiTool;

[SupportedOSPlatform("windows")]
public partial class MainWindow : Window
{
    private const string ReportRelativePath = @"artifacts\plugin-completion-ui-report.json";

    private readonly ObservableCollection<PluginResultRow> _pluginResults = new();
    private readonly ObservableCollection<StepLogRow> _stepLogs = new();
    private bool _isRunning;
    private string? _lastReportPath;

    public MainWindow()
    {
        InitializeComponent();

        PluginResultsDataGrid.ItemsSource = _pluginResults;
        StepLogsDataGrid.ItemsSource = _stepLogs;
        RepositoryPathTextBox.Text = DetectRepositoryRoot();
    }

    private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning)
        {
            return;
        }

        var repositoryRoot = RepositoryPathTextBox.Text.Trim();
        if (!Directory.Exists(repositoryRoot))
        {
            System.Windows.MessageBox.Show(this, "Repository root path does not exist.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var reportPath = Path.Combine(repositoryRoot, ReportRelativePath);
        _lastReportPath = reportPath;

        _pluginResults.Clear();
        _stepLogs.Clear();
        LogTextBox.Clear();
        OpenReportButton.IsEnabled = false;
        SummaryTextBlock.Text = "Running...";
        StatusTextBlock.Text = "Running native completion checks...";

        SetRunningState(true);
        try
        {
            var request = new CompletionCheckRequest
            {
                RepositoryRoot = repositoryRoot,
                Configuration = (ConfigurationComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Release",
                SkipBuild = SkipBuildCheckBox.IsChecked == true,
                SkipTests = SkipTestsCheckBox.IsChecked == true,
                PluginIds = ParsePluginIds(PluginIdsTextBox.Text)
            };

            var checker = new CompletionChecker(
                AppendLog,
                step =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var prefix = string.IsNullOrWhiteSpace(step.PluginId) ? string.Empty : $"[{step.PluginId}] ";
                        StatusTextBlock.Text = prefix + step.Message;
                    });
                });

            var report = await checker.RunAsync(request);
            await CompletionReportFile.WriteAsync(reportPath, report);

            if (File.Exists(reportPath))
            {
                LoadReport(reportPath);
                OpenReportButton.IsEnabled = true;
            }
            else
            {
                AppendLog($"Report file was not generated: {reportPath}");
            }

            StatusTextBlock.Text = report.Totals.Failures == 0
                ? "Completed successfully."
                : $"Completed with failures ({report.Totals.Failures} total).";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Execution failed.";
            AppendLog(ex.ToString());
            System.Windows.MessageBox.Show(this, ex.Message, "Execution Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetRunningState(false);
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the Plugins workspace under the Universal Device Toolkit repository"
        };

        var currentPath = RepositoryPathTextBox.Text.Trim();
        if (Directory.Exists(currentPath))
        {
            dialog.SelectedPath = currentPath;
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            RepositoryPathTextBox.Text = dialog.SelectedPath;
        }
    }

    private void OpenRepositoryButton_Click(object sender, RoutedEventArgs e)
    {
        var repositoryRoot = RepositoryPathTextBox.Text.Trim();
        if (!Directory.Exists(repositoryRoot))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = repositoryRoot,
            UseShellExecute = true
        });
    }

    private void OpenReportButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_lastReportPath) || !File.Exists(_lastReportPath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = _lastReportPath,
            UseShellExecute = true
        });
    }

    private static string[] ParsePluginIds(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return [];
        }

        return rawText
            .Split([',', ';', '\r', '\n', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void LoadReport(string reportPath)
    {
        var json = File.ReadAllText(reportPath);
        var report = JsonSerializer.Deserialize<CompletionReport>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        _pluginResults.Clear();
        _stepLogs.Clear();

        if (report?.Plugins is not null)
        {
            foreach (var plugin in report.Plugins.OrderBy(item => item.PluginId, StringComparer.OrdinalIgnoreCase))
            {
                _pluginResults.Add(new PluginResultRow
                {
                    PluginId = plugin.PluginId,
                    Status = plugin.Status,
                    Failures = plugin.Failures,
                    Warnings = plugin.Warnings
                });
            }
        }

        if (report?.Steps is not null)
        {
            foreach (var step in report.Steps)
            {
                _stepLogs.Add(new StepLogRow
                {
                    Timestamp = step.Timestamp,
                    PluginId = step.PluginId,
                    Status = step.Status,
                    Message = step.Message
                });
            }
        }

        if (report?.Totals is not null)
        {
            SummaryTextBlock.Text = $"Plugins: {report.Totals.PluginCount}, Failures: {report.Totals.Failures}, Warnings: {report.Totals.Warnings}";
        }
        else
        {
            SummaryTextBlock.Text = "Report loaded, totals unavailable.";
        }
    }

    private void AppendLog(string line)
    {
        Dispatcher.Invoke(() =>
        {
            LogTextBox.AppendText(line + Environment.NewLine);
            LogTextBox.ScrollToEnd();
        });
    }

    private void SetRunningState(bool isRunning)
    {
        _isRunning = isRunning;
        RunButton.IsEnabled = !isRunning;
        BrowseButton.IsEnabled = !isRunning;
        OpenRepositoryButton.IsEnabled = !isRunning;
        RepositoryPathTextBox.IsEnabled = !isRunning;
        PluginIdsTextBox.IsEnabled = !isRunning;
        SkipBuildCheckBox.IsEnabled = !isRunning;
        SkipTestsCheckBox.IsEnabled = !isRunning;
        ConfigurationComboBox.IsEnabled = !isRunning;
    }

    private string DetectRepositoryRoot()
    {
        var candidates = new[]
        {
            Environment.CurrentDirectory,
            AppContext.BaseDirectory
        };

        foreach (var candidate in candidates)
        {
            var resolved = FindRepoRootByWalkingUp(candidate);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        return Environment.CurrentDirectory;
    }

    private static string? FindRepoRootByWalkingUp(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        for (var depth = 0; depth < 10 && directory is not null; depth++)
        {
            var solutionPath = Path.Combine(directory.FullName, "UniversalDeviceToolkit.Plugins.sln");
            var pluginsDirectory = Path.Combine(directory.FullName, "Official");
            var toolDirectory = Path.Combine(directory.FullName, @"Tooling\PluginCompletionUiTool");
            if (File.Exists(solutionPath) &&
                Directory.Exists(pluginsDirectory) &&
                Directory.Exists(toolDirectory))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
    private sealed class PluginResultRow
    {
        public string PluginId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public int Failures { get; init; }
        public int Warnings { get; init; }
    }

    private sealed class StepLogRow
    {
        public string Timestamp { get; init; } = string.Empty;
        public string PluginId { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}
