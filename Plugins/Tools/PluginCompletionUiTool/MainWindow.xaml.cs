using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Forms;

namespace PluginCompletionUiTool;

[SupportedOSPlatform("windows")]
public partial class MainWindow : Window
{
    private const string ScriptRelativePath = @"scripts\plugin-completion-check.ps1";
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

        var scriptPath = Path.Combine(repositoryRoot, ScriptRelativePath);
        if (!File.Exists(scriptPath))
        {
            System.Windows.MessageBox.Show(this, $"Checker script not found:\n{scriptPath}", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var reportPath = Path.Combine(repositoryRoot, ReportRelativePath);
        _lastReportPath = reportPath;

        _pluginResults.Clear();
        _stepLogs.Clear();
        LogTextBox.Clear();
        SummaryTextBlock.Text = "Running...";
        StatusTextBlock.Text = "Executing checker script...";

        SetRunningState(true);
        try
        {
            var arguments = BuildPowerShellArguments(scriptPath, reportPath);
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                WorkingDirectory = repositoryRoot,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processStartInfo };
            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    AppendLog(args.Data);
                }
            };
            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    AppendLog("[stderr] " + args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            if (File.Exists(reportPath))
            {
                LoadReport(reportPath);
                OpenReportButton.IsEnabled = true;
            }
            else
            {
                AppendLog($"Report file was not generated: {reportPath}");
            }

            StatusTextBlock.Text = process.ExitCode == 0 ? "Completed successfully." : $"Completed with failures (exit code {process.ExitCode}).";
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
            Description = "Select LenovoLegionToolkit-Plugins repository root"
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

    private string BuildPowerShellArguments(string scriptPath, string reportPath)
    {
        var arguments = new StringBuilder();
        arguments.Append("-NoProfile -ExecutionPolicy Bypass ");
        arguments.Append("-File ");
        arguments.Append(Quote(scriptPath));
        arguments.Append(" -Configuration ");

        var selectedConfig = (ConfigurationComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
        arguments.Append(Quote(string.IsNullOrWhiteSpace(selectedConfig) ? "Release" : selectedConfig));

        if (SkipBuildCheckBox.IsChecked == true)
        {
            arguments.Append(" -SkipBuild");
        }

        if (SkipTestsCheckBox.IsChecked == true)
        {
            arguments.Append(" -SkipTests");
        }

        var pluginIds = ParsePluginIds(PluginIdsTextBox.Text);
        if (pluginIds.Length > 0)
        {
            arguments.Append(" -PluginIds");
            foreach (var pluginId in pluginIds)
            {
                arguments.Append(' ');
                arguments.Append(Quote(pluginId));
            }
        }

        arguments.Append(" -JsonReportPath ");
        arguments.Append(Quote(reportPath));

        return arguments.ToString();
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

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
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
            var storePath = Path.Combine(directory.FullName, "store.json");
            var scriptPath = Path.Combine(directory.FullName, ScriptRelativePath);
            if (File.Exists(storePath) && File.Exists(scriptPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private sealed class CompletionReport
    {
        [JsonPropertyName("totals")]
        public CompletionTotals? Totals { get; set; }

        [JsonPropertyName("plugins")]
        public List<PluginReportItem>? Plugins { get; set; }

        [JsonPropertyName("steps")]
        public List<StepReportItem>? Steps { get; set; }
    }

    private sealed class CompletionTotals
    {
        [JsonPropertyName("pluginCount")]
        public int PluginCount { get; set; }

        [JsonPropertyName("failures")]
        public int Failures { get; set; }

        [JsonPropertyName("warnings")]
        public int Warnings { get; set; }
    }

    private sealed class PluginReportItem
    {
        [JsonPropertyName("pluginId")]
        public string PluginId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("failures")]
        public int Failures { get; set; }

        [JsonPropertyName("warnings")]
        public int Warnings { get; set; }
    }

    private sealed class StepReportItem
    {
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("pluginId")]
        public string PluginId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
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
