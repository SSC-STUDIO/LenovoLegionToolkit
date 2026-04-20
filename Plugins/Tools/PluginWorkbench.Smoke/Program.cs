using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Windows.Automation;

namespace PluginWorkbench.Smoke;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private const string DefaultPluginId = "custom-mouse";
    private const string StartupTheme = "Dark";

    private static int Main(string[] args)
    {
        Process? process = null;
        try
        {
            var options = ParseOptions(args);
            var repositoryRoot = options.RepositoryRoot;
            var plugin = ResolvePlugin(repositoryRoot, options.PluginId);
            var buildDirectory = ResolvePluginBuildDirectory(repositoryRoot, plugin.FolderName, plugin.Id);

            Console.WriteLine($"[workbench-smoke] Repository root: {repositoryRoot}");
            Console.WriteLine($"[workbench-smoke] Plugin under test: {plugin.Name} ({plugin.Id})");

            var (startInfo, appDirectory) = ResolveWorkbenchStartInfo(repositoryRoot);
            startInfo.Arguments = AppendArgument(startInfo.Arguments, $"--repository-root \"{repositoryRoot}\"");
            startInfo.Arguments = AppendArgument(startInfo.Arguments, $"--theme {StartupTheme.ToLowerInvariant()}");
            startInfo.Arguments = AppendArgument(startInfo.Arguments, "--auto-accept-runtime-confirmation");

            Console.WriteLine($"[workbench-smoke] Launching: {startInfo.FileName} {startInfo.Arguments}");
            Console.WriteLine($"[workbench-smoke] Workbench directory: {appDirectory}");
            Console.WriteLine($"[workbench-smoke] Expected plugin build: {buildDirectory}");

            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start PluginWorkbench process.");
            process.WaitForInputIdle(5000);

            var window = WaitForMainWindow(process.Id, TimeSpan.FromSeconds(30));
            Console.WriteLine("[workbench-smoke] Main window ready");

            var themeStatus = WaitForAutomationId(window, "ThemeStateTextBlock", TimeSpan.FromSeconds(5));
            WaitUntil(
                () => ReadElementText(themeStatus).Contains(StartupTheme, StringComparison.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(250));
            Console.WriteLine("[workbench-smoke] Startup theme verified");

            var pluginList = WaitForAutomationId(window, "PluginListBox", TimeSpan.FromSeconds(10));
            var pluginItem = WaitForPluginListItem(pluginList, plugin, TimeSpan.FromSeconds(10));
            Select(pluginItem);

            var loadSelectedButton = WaitForAutomationId(window, "LoadSelectedButton", TimeSpan.FromSeconds(5));
            Click(loadSelectedButton);
            Console.WriteLine("[workbench-smoke] Selected plugin load requested");

            var pluginTitle = WaitForAutomationId(window, "PluginTitleTextBlock", TimeSpan.FromSeconds(30));
            WaitUntil(
                () => ReadElementText(pluginTitle).Contains(plugin.Name, StringComparison.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMilliseconds(250));

            var status = WaitForAutomationId(window, "StatusTextBlock", TimeSpan.FromSeconds(5));
            var subtitle = WaitForAutomationId(window, "PluginSubtitleTextBlock", TimeSpan.FromSeconds(5));
            var optimizationTab = WaitForAutomationId(window, "OptimizationTabItem", TimeSpan.FromSeconds(10));
            Select(optimizationTab);

            var runButton = WaitForAutomationId(window, "OptimizationRunButton", TimeSpan.FromSeconds(15));

            if (!ReadElementText(status).Contains("Loaded", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Workbench did not report a loaded plugin state.");

            if (!ReadElementText(subtitle).Contains("Mode: Preview", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Workbench did not start in Preview mode for the loaded plugin.");

            if (runButton.Current.IsEnabled)
                throw new InvalidOperationException("Optimization action button should be disabled in Preview mode.");

            Console.WriteLine("[workbench-smoke] Preview mode verified");

            var settingsTab = WaitForAutomationId(window, "SettingsTabItem", TimeSpan.FromSeconds(5));
            Select(settingsTab);
            var settingsShellTitle = WaitForAutomationId(window, "SettingsShellTitleTextBlock", TimeSpan.FromSeconds(10));
            WaitUntil(
                () => ReadElementText(settingsShellTitle).Contains(plugin.Name, StringComparison.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(250));
            Console.WriteLine("[workbench-smoke] Settings host shell verified");

            var modeToggleButton = WaitForAutomationId(window, "ModeToggleButton", TimeSpan.FromSeconds(5));
            Click(modeToggleButton);
            Console.WriteLine("[workbench-smoke] Real Runtime switch requested");

            WaitUntil(
                () => ReadElementText(subtitle).Contains("Mode: RealRuntime", StringComparison.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMilliseconds(250));

            optimizationTab = WaitForAutomationId(window, "OptimizationTabItem", TimeSpan.FromSeconds(10));
            Select(optimizationTab);
            runButton = WaitForAutomationId(window, "OptimizationRunButton", TimeSpan.FromSeconds(15));
            WaitUntil(
                () => runButton.Current.IsEnabled,
                TimeSpan.FromSeconds(15),
                TimeSpan.FromMilliseconds(250));

            Console.WriteLine("[workbench-smoke] Real Runtime mode verified");

            CloseWindow(window);
            process.WaitForExit(5000);
            Console.WriteLine("[workbench-smoke] PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[workbench-smoke] FAIL");
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (process is not null && !process.HasExited)
                process.Kill(entireProcessTree: true);
        }
    }

    private static SmokeOptions ParseOptions(string[] args)
    {
        var repositoryRoot = ResolveRepositoryRoot(args);
        var pluginId = DefaultPluginId;

        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], "--plugin-id", StringComparison.OrdinalIgnoreCase))
                continue;

            if (i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]))
                pluginId = args[i + 1];
        }

        return new SmokeOptions(repositoryRoot, pluginId);
    }

    private static string ResolveRepositoryRoot(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], "--repository-root", StringComparison.OrdinalIgnoreCase))
                continue;

            if (i + 1 < args.Length)
            {
                var fromArg = Path.GetFullPath(args[i + 1]);
                EnsureRepositoryRoot(fromArg);
                return fromArg;
            }
        }

        var current = new DirectoryInfo(Environment.CurrentDirectory);
        for (var i = 0; i < 10 && current is not null; i++)
        {
            if (IsRepositoryRoot(current.FullName))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Cannot infer plugin repository root. Pass --repository-root <path>.");
    }

    private static void EnsureRepositoryRoot(string repositoryRoot)
    {
        if (!IsRepositoryRoot(repositoryRoot))
            throw new DirectoryNotFoundException($"Path is not plugin repository root: {repositoryRoot}");
    }

    private static bool IsRepositoryRoot(string repositoryRoot)
    {
        return File.Exists(Path.Combine(repositoryRoot, "LenovoLegionToolkit-Plugins.sln")) &&
               File.Exists(Path.Combine(repositoryRoot, "store.json")) &&
               Directory.Exists(Path.Combine(repositoryRoot, "Plugins")) &&
               Directory.Exists(Path.Combine(repositoryRoot, @"Tools\PluginWorkbench"));
    }

    private static PluginDescriptor ResolvePlugin(string repositoryRoot, string pluginId)
    {
        var pluginDirectories = Directory.EnumerateDirectories(Path.Combine(repositoryRoot, "Plugins"));
        foreach (var pluginDirectory in pluginDirectories)
        {
            var folderName = Path.GetFileName(pluginDirectory);
            if (folderName is "Shared" or "Template" || folderName.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase))
                continue;

            var manifestPath = Path.Combine(pluginDirectory, "plugin.json");
            if (!File.Exists(manifestPath))
                continue;

            using var stream = File.OpenRead(manifestPath);
            using var document = JsonDocument.Parse(stream);
            var root = document.RootElement;
            var id = root.GetProperty("id").GetString() ?? folderName;
            if (!string.Equals(id, pluginId, StringComparison.OrdinalIgnoreCase))
                continue;

            return new PluginDescriptor(
                folderName,
                id,
                root.GetProperty("name").GetString() ?? folderName,
                root.GetProperty("version").GetString() ?? "0.0.0");
        }

        throw new DirectoryNotFoundException($"Plugin '{pluginId}' was not found in repository manifests.");
    }

    private static string ResolvePluginBuildDirectory(string repositoryRoot, string folderName, string pluginId)
    {
        var canonical = Path.Combine(repositoryRoot, "Build", "plugins", $"LenovoLegionToolkit.Plugins.{folderName}");
        if (Directory.Exists(canonical))
            return canonical;

        var buildRoot = Path.Combine(repositoryRoot, "Build", "plugins");
        if (Directory.Exists(buildRoot))
        {
            foreach (var directory in Directory.EnumerateDirectories(buildRoot))
            {
                var manifestPath = Path.Combine(directory, "plugin.json");
                if (!File.Exists(manifestPath))
                    continue;

                using var stream = File.OpenRead(manifestPath);
                using var document = JsonDocument.Parse(stream);
                var id = document.RootElement.GetProperty("id").GetString();
                if (string.Equals(id, pluginId, StringComparison.OrdinalIgnoreCase))
                    return directory;
            }
        }

        throw new DirectoryNotFoundException($"Plugin build output for '{pluginId}' was not found. Build the plugin first.");
    }

    private static (ProcessStartInfo startInfo, string appDirectory) ResolveWorkbenchStartInfo(string repositoryRoot)
    {
        var candidateRoots = new[]
        {
            Path.Combine(repositoryRoot, @"Tools\PluginWorkbench\bin\Release"),
            Path.Combine(repositoryRoot, @"Tools\PluginWorkbench\bin\Debug")
        };

        foreach (var candidateRoot in candidateRoots)
        {
            var resolved = TryResolveWorkbenchStartInfo(candidateRoot);
            if (resolved is not null)
                return resolved.Value;
        }

        throw new FileNotFoundException(
            $"PluginWorkbench build output not found under {string.Join(", ", candidateRoots)}. Build the workbench first.");
    }

    private static (ProcessStartInfo startInfo, string appDirectory)? TryResolveWorkbenchStartInfo(string buildRoot)
    {
        if (!Directory.Exists(buildRoot))
            return null;

        var exePath = FindArtifact(buildRoot, "PluginWorkbench.exe");
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

        var dllPath = FindArtifact(buildRoot, "PluginWorkbench.dll");
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

    private static string? FindArtifact(string buildRoot, string fileName)
    {
        return Directory.EnumerateFiles(buildRoot, fileName, SearchOption.AllDirectories)
            .OrderBy(path => path.Count(ch => ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar))
            .FirstOrDefault();
    }

    private static string AppendArgument(string existingArguments, string argument)
    {
        return string.IsNullOrWhiteSpace(existingArguments)
            ? argument
            : $"{existingArguments} {argument}";
    }

    private static AutomationElement WaitForMainWindow(int processId, TimeSpan timeout)
    {
        return WaitForWindow(processId, "Plugin Workbench", timeout, automationId: "PluginWorkbenchMainWindow");
    }

    private static AutomationElement WaitForWindow(int processId, string windowName, TimeSpan timeout, string? automationId = null)
    {
        var found = WaitUntil(
            () => FindWindow(processId, windowName, automationId) is not null,
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for window '{windowName}'.");

        return FindWindow(processId, windowName, automationId)
            ?? throw new InvalidOperationException($"Window '{windowName}' was not found.");
    }

    private static AutomationElement? FindWindow(int processId, string windowName, string? automationId)
    {
        var windows = AutomationElement.RootElement.FindAll(
            TreeScope.Children,
            new AndCondition(
                new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)));

        foreach (AutomationElement window in windows)
        {
            if (!string.IsNullOrWhiteSpace(automationId) &&
                !string.Equals(window.Current.AutomationId, automationId, StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(window.Current.Name, windowName, StringComparison.OrdinalIgnoreCase))
                return window;
        }

        return null;
    }

    private static AutomationElement WaitForAutomationId(AutomationElement root, string automationId, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => FindByAutomationId(root, automationId) is not null,
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for automation element '{automationId}'.");

        return FindByAutomationId(root, automationId)
            ?? throw new InvalidOperationException($"Automation element '{automationId}' not found.");
    }

    private static AutomationElement? FindByAutomationId(AutomationElement root, string automationId)
    {
        return root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
    }

    private static AutomationElement WaitForPluginListItem(AutomationElement pluginList, PluginDescriptor plugin, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => FindPluginListItem(pluginList, plugin) is not null,
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for plugin list item '{plugin.Id}'.");

        return FindPluginListItem(pluginList, plugin)
            ?? throw new InvalidOperationException($"Plugin list item '{plugin.Id}' not found.");
    }

    private static AutomationElement? FindPluginListItem(AutomationElement pluginList, PluginDescriptor plugin)
    {
        var items = pluginList.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem));
        foreach (AutomationElement item in items)
        {
            var name = item.Current.Name;
            if (name.Contains(plugin.Id, StringComparison.OrdinalIgnoreCase) ||
                name.Contains(plugin.Name, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    private static AutomationElement WaitForDescendantByName(AutomationElement root, string name, ControlType controlType, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => root.FindFirst(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.NameProperty, name),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, controlType))) is not null,
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for descendant '{name}'.");

        return root.FindFirst(
                   TreeScope.Descendants,
                   new AndCondition(
                       new PropertyCondition(AutomationElement.NameProperty, name),
                       new PropertyCondition(AutomationElement.ControlTypeProperty, controlType)))
               ?? throw new InvalidOperationException($"Descendant '{name}' not found.");
    }

    private static void Select(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionPattern))
        {
            ((SelectionItemPattern)selectionPattern).Select();
            return;
        }

        Click(element);
    }

    private static void SelectComboBoxItem(AutomationElement rootWindow, AutomationElement comboBox, string itemName)
    {
        if (comboBox.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandPattern))
            ((ExpandCollapsePattern)expandPattern).Expand();

        var item = WaitForDescendantByName(rootWindow, itemName, ControlType.ListItem, TimeSpan.FromSeconds(5));
        Select(item);

        if (comboBox.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out expandPattern))
            ((ExpandCollapsePattern)expandPattern).Collapse();
    }

    private static void Click(AutomationElement element)
    {
        var current = element;
        while (current is not null)
        {
            if (current.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
            {
                ((InvokePattern)invokePattern).Invoke();
                return;
            }

            if (current.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionPattern))
            {
                ((SelectionItemPattern)selectionPattern).Select();
                return;
            }

            current = TreeWalker.RawViewWalker.GetParent(current);
        }

        throw new InvalidOperationException($"InvokePattern unavailable for '{element.Current.AutomationId}:{element.Current.Name}'.");
    }

    private static string ReadElementText(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
            return ((ValuePattern)valuePattern).Current.Value ?? string.Empty;

        return element.Current.Name ?? string.Empty;
    }

    private static bool WaitUntil(Func<bool> predicate, TimeSpan timeout, TimeSpan pollInterval)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            if (predicate())
                return true;

            Thread.Sleep(pollInterval);
        }

        return predicate();
    }

    private static void CloseWindow(AutomationElement window)
    {
        if (!window.TryGetCurrentPattern(WindowPattern.Pattern, out var pattern))
            throw new InvalidOperationException("WindowPattern unavailable for main window.");

        ((WindowPattern)pattern).Close();
    }

    private sealed record SmokeOptions(string RepositoryRoot, string PluginId);
    private sealed record PluginDescriptor(string FolderName, string Id, string Name, string Version);
}
