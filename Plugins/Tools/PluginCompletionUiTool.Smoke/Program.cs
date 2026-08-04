using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using System.Windows.Automation;

namespace PluginCompletionUiTool.Smoke;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private const string UiReportRelativePath = @"artifacts\plugin-completion-ui-report.json";

    private static int Main(string[] args)
    {
        Process? process = null;
        try
        {
            var repositoryRoot = ResolveRepositoryRoot(args);
            Console.WriteLine($"[smoke] Repository root: {repositoryRoot}");

            var (startInfo, appDirectory) = ResolveUiAppStartInfo(repositoryRoot);
            Console.WriteLine($"[smoke] Launching: {startInfo.FileName} {startInfo.Arguments}");

            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start PluginCompletionUiTool process.");
            process.WaitForInputIdle(5000);

            var window = WaitForMainWindow(process.Id, TimeSpan.FromSeconds(30));
            Console.WriteLine("[smoke] Main window ready");

            var repositoryTextBox = WaitForAutomationId(window, "RepositoryPathTextBox", TimeSpan.FromSeconds(10));
            SetText(repositoryTextBox, repositoryRoot);

            var skipBuildCheckBox = WaitForAutomationId(window, "SkipBuildCheckBox", TimeSpan.FromSeconds(10));
            SetCheckBox(skipBuildCheckBox, true);

            var skipTestsCheckBox = WaitForAutomationId(window, "SkipTestsCheckBox", TimeSpan.FromSeconds(10));
            SetCheckBox(skipTestsCheckBox, true);

            var runButton = WaitForAutomationId(window, "RunButton", TimeSpan.FromSeconds(10));
            Click(runButton);
            Console.WriteLine("[smoke] Run button clicked");

            var completed = WaitUntil(
                () => TryReadStatus(window, out var statusText) && statusText.Contains("Completed", StringComparison.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(120),
                TimeSpan.FromMilliseconds(500));

            if (!completed)
            {
                throw new TimeoutException("UI did not reach completed status within timeout.");
            }

            var summaryElement = WaitForAutomationId(window, "SummaryTextBlock", TimeSpan.FromSeconds(5));
            var summary = ReadElementText(summaryElement);
            Console.WriteLine($"[smoke] Summary: {summary}");

            if (!summary.Contains("Plugins:", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Summary text does not contain plugin totals.");
            }

            var reportPath = Path.Combine(repositoryRoot, UiReportRelativePath);
            WaitForFile(reportPath, TimeSpan.FromSeconds(15));
            ValidateReport(reportPath);
            Console.WriteLine("[smoke] JSON report validated");

            TryDeleteReport(reportPath);

            CloseWindow(window);
            process.WaitForExit(5000);
            Console.WriteLine("[smoke] PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[smoke] FAIL");
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (process is not null && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    private static string ResolveRepositoryRoot(string[] args)
    {
        if (args.Length > 0)
        {
            var fromArg = Path.GetFullPath(args[0]);
            EnsureRepositoryRoot(fromArg);
            return fromArg;
        }

        var current = new DirectoryInfo(Environment.CurrentDirectory);
        for (var i = 0; i < 10 && current is not null; i++)
        {
            if (IsRepositoryRoot(current.FullName))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Cannot infer plugin repository root. Pass repo root as first argument.");
    }

    private static void EnsureRepositoryRoot(string repositoryRoot)
    {
        if (!IsRepositoryRoot(repositoryRoot))
        {
            throw new DirectoryNotFoundException($"Path is not plugin repository root: {repositoryRoot}");
        }
    }

    private static (ProcessStartInfo startInfo, string appDirectory) ResolveUiAppStartInfo(string repositoryRoot)
    {
        var candidateRoots = new[]
        {
            Path.Combine(repositoryRoot, @"Tooling\PluginCompletionUiTool\bin\Release"),
            Path.Combine(repositoryRoot, @"Tooling\PluginCompletionUiTool\bin\Debug")
        };

        foreach (var candidateRoot in candidateRoots)
        {
            var resolved = TryResolveUiAppStartInfo(candidateRoot);
            if (resolved is not null)
            {
                return resolved.Value;
            }
        }

        throw new FileNotFoundException(
            $"PluginCompletionUiTool build output not found under {string.Join(", ", candidateRoots)}. Build the UI tool first.");
    }

    private static bool IsRepositoryRoot(string repositoryRoot)
    {
        return File.Exists(Path.Combine(repositoryRoot, "UniversalDeviceToolkit.Plugins.sln")) &&
               Directory.Exists(Path.Combine(repositoryRoot, "Official")) &&
               Directory.Exists(Path.Combine(repositoryRoot, @"Tooling\PluginCompletionUiTool"));
    }

    private static (ProcessStartInfo startInfo, string appDirectory)? TryResolveUiAppStartInfo(string buildRoot)
    {
        if (!Directory.Exists(buildRoot))
        {
            return null;
        }

        var exePath = FindUiArtifact(buildRoot, "PluginCompletionUiTool.exe");
        if (!string.IsNullOrWhiteSpace(exePath))
        {
            var appDirectory = Path.GetDirectoryName(exePath)!;
            return (new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = appDirectory,
                UseShellExecute = false
            }, appDirectory);
        }

        var dllPath = FindUiArtifact(buildRoot, "PluginCompletionUiTool.dll");
        if (!string.IsNullOrWhiteSpace(dllPath))
        {
            var appDirectory = Path.GetDirectoryName(dllPath)!;
            return (new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{dllPath}\"",
                WorkingDirectory = appDirectory,
                UseShellExecute = false
            }, appDirectory);
        }

        return null;
    }

    private static string? FindUiArtifact(string buildRoot, string fileName)
    {
        return Directory.EnumerateFiles(buildRoot, fileName, SearchOption.AllDirectories)
            .OrderBy(path => path.Count(c => c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar))
            .FirstOrDefault();
    }

    private static AutomationElement WaitForMainWindow(int processId, TimeSpan timeout)
    {
        var condition = new AndCondition(
            new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

        var found = WaitUntil(
            () => AutomationElement.RootElement.FindFirst(TreeScope.Children, condition) is not null,
            timeout,
            TimeSpan.FromMilliseconds(300));

        if (!found)
        {
            throw new TimeoutException("Timed out waiting for main window.");
        }

        var window = AutomationElement.RootElement.FindFirst(TreeScope.Children, condition);
        return window ?? throw new InvalidOperationException("Main window was not found.");
    }

    private static AutomationElement WaitForAutomationId(AutomationElement root, string automationId, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => FindByAutomationId(root, automationId) is not null,
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
        {
            throw new TimeoutException($"Timed out waiting for automation element '{automationId}'.");
        }

        return FindByAutomationId(root, automationId) ?? throw new InvalidOperationException($"Automation element '{automationId}' not found.");
    }

    private static AutomationElement? FindByAutomationId(AutomationElement root, string automationId)
    {
        return root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
    }

    private static void SetText(AutomationElement textBox, string value)
    {
        if (!textBox.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
        {
            throw new InvalidOperationException($"ValuePattern unavailable for '{textBox.Current.AutomationId}'.");
        }

        ((ValuePattern)pattern).SetValue(value);
    }

    private static void SetCheckBox(AutomationElement checkBox, bool desiredState)
    {
        if (!checkBox.TryGetCurrentPattern(TogglePattern.Pattern, out var pattern))
        {
            throw new InvalidOperationException($"TogglePattern unavailable for '{checkBox.Current.AutomationId}'.");
        }

        var togglePattern = (TogglePattern)pattern;
        for (var i = 0; i < 3; i++)
        {
            var isOn = togglePattern.Current.ToggleState == ToggleState.On;
            if (isOn == desiredState)
            {
                return;
            }

            togglePattern.Toggle();
            Thread.Sleep(100);
        }

        throw new InvalidOperationException($"Unable to set checkbox '{checkBox.Current.AutomationId}' to desired state.");
    }

    private static void Click(AutomationElement element)
    {
        if (!element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
        {
            throw new InvalidOperationException($"InvokePattern unavailable for '{element.Current.AutomationId}'.");
        }

        ((InvokePattern)pattern).Invoke();
    }

    private static string ReadElementText(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
        {
            return ((ValuePattern)pattern).Current.Value ?? string.Empty;
        }

        return element.Current.Name ?? string.Empty;
    }

    private static bool WaitUntil(Func<bool> predicate, TimeSpan timeout, TimeSpan interval)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (predicate())
                {
                    return true;
                }
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // UI Automation may briefly return E_UNEXPECTED while the visual tree is updating.
            }
            catch (InvalidOperationException)
            {
                // Elements can disappear transiently during UI refresh; treat as retryable.
            }

            Thread.Sleep(interval);
        }

        return false;
    }

    private static bool TryReadStatus(AutomationElement window, out string statusText)
    {
        statusText = string.Empty;

        var statusElement = FindByAutomationId(window, "StatusTextBlock");
        if (statusElement is null)
        {
            return false;
        }

        statusText = ReadElementText(statusElement);
        return !string.IsNullOrWhiteSpace(statusText);
    }

    private static void WaitForFile(string path, TimeSpan timeout)
    {
        var found = WaitUntil(() => File.Exists(path), timeout, TimeSpan.FromMilliseconds(250));
        if (!found)
        {
            throw new TimeoutException($"Report file was not created: {path}");
        }
    }

    private static void ValidateReport(string reportPath)
    {
        using var stream = File.OpenRead(reportPath);
        using var document = JsonDocument.Parse(stream);

        var root = document.RootElement;
        var totals = root.GetProperty("totals");

        var pluginCount = totals.GetProperty("pluginCount").GetInt32();
        var failures = totals.GetProperty("failures").GetInt32();

        if (pluginCount <= 0)
        {
            throw new InvalidOperationException("Report pluginCount is zero.");
        }

        if (failures != 0)
        {
            throw new InvalidOperationException($"Report failures expected 0, actual {failures}.");
        }
    }

    private static void TryDeleteReport(string reportPath)
    {
        try
        {
            if (File.Exists(reportPath))
            {
                File.Delete(reportPath);
            }

            var parent = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent) && !Directory.EnumerateFileSystemEntries(parent).Any())
            {
                Directory.Delete(parent);
            }
        }
        catch
        {
            // Report cleanup failure should not fail smoke result.
        }
    }

    private static void CloseWindow(AutomationElement window)
    {
        if (window.TryGetCurrentPattern(WindowPattern.Pattern, out var pattern))
        {
            ((WindowPattern)pattern).Close();
        }
    }
}
