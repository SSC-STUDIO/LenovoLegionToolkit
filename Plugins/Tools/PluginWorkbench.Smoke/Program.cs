using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Windows.Automation;

namespace PluginWorkbench.Smoke;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private const string DefaultPluginId = "custom-mouse";
    private const int SwRestore = 9;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private static int Main(string[] args)
    {
        Process? process = null;
        AutomationElement? mainWindow = null;
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
            startInfo.Arguments = AppendArgument(startInfo.Arguments, $"--theme {options.Theme.ToLowerInvariant()}");
            startInfo.Arguments = AppendArgument(startInfo.Arguments, "--view feature");
            startInfo.Arguments = AppendArgument(startInfo.Arguments, "--auto-accept-runtime-confirmation");

            Console.WriteLine($"[workbench-smoke] Launching: {startInfo.FileName} {startInfo.Arguments}");
            Console.WriteLine($"[workbench-smoke] Workbench directory: {appDirectory}");
            Console.WriteLine($"[workbench-smoke] Expected plugin build: {buildDirectory}");

            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start PluginWorkbench process.");
            process.WaitForInputIdle(5000);

            var window = WaitForMainWindow(process.Id, TimeSpan.FromSeconds(30));
            mainWindow = window;
            Console.WriteLine("[workbench-smoke] Main window ready");

            var themeStatus = WaitForAutomationId(window, "ThemeStateTextBlock", TimeSpan.FromSeconds(5));
            WaitUntil(
                () => ReadElementText(themeStatus).Contains(options.Theme, StringComparison.OrdinalIgnoreCase),
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
            var optimizationTab = TryWaitForAutomationId(window, "OptimizationTabItem", TimeSpan.FromSeconds(10));
            AutomationElement? runButton = null;
            var requiresOptimizationAction = RequiresOptimizationAction(plugin);
            if (IsInteractable(optimizationTab))
            {
                Select(optimizationTab!);
                runButton = requiresOptimizationAction
                    ? WaitForAutomationId(window, "OptimizationRunButton", TimeSpan.FromSeconds(15))
                    : TryWaitForAutomationId(window, "OptimizationRunButton", TimeSpan.FromSeconds(5));

                if (runButton is null)
                    Console.WriteLine("[workbench-smoke] Optimization preview tab has no runnable action rows");
            }
            else
            {
                if (requiresOptimizationAction)
                    throw new InvalidOperationException($"{plugin.Id} should expose an optimization preview tab.");

                Console.WriteLine("[workbench-smoke] Plugin does not expose an optimization preview tab");
            }

            var statusText = ReadElementText(status);
            if (string.IsNullOrWhiteSpace(statusText)
                || statusText.Contains("failed", StringComparison.OrdinalIgnoreCase)
                || statusText.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                PrintWorkbenchLog(window);

                throw new InvalidOperationException($"Workbench reported an invalid plugin state: '{statusText}'.");
            }

            if (!ReadElementText(subtitle).Contains("Mode: Preview", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Workbench did not start in Preview mode for the loaded plugin.");

            if (plugin.Id.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
            {
                var featureTabHidden = WaitUntil(
                    () => !IsVisible(FindByAutomationId(window, "FeatureTabItem")),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromMilliseconds(250));
                if (!featureTabHidden)
                    throw new InvalidOperationException("custom-mouse should not expose a standalone feature preview tab.");
            }

            if (runButton is not null && runButton.Current.IsEnabled)
                throw new InvalidOperationException("Optimization action button should be disabled in Preview mode.");

            var featureTab = TryWaitForAutomationId(window, "FeatureTabItem", TimeSpan.FromSeconds(2));
            if (IsInteractable(featureTab))
                Select(featureTab!);

            Console.WriteLine("[workbench-smoke] Preview mode verified");
            CaptureAndValidateVisual(window, options, plugin, "preview");

            var settingsTab = WaitForAutomationId(window, "SettingsTabItem", TimeSpan.FromSeconds(5));
            Select(settingsTab);
            var settingsShellTitle = WaitForAutomationId(window, "SettingsShellTitleTextBlock", TimeSpan.FromSeconds(10));
            WaitUntil(
                () => ReadElementText(settingsShellTitle).Contains(plugin.Name, StringComparison.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(250));
            Console.WriteLine("[workbench-smoke] Settings host shell verified");
            CaptureAndValidateVisual(window, options, plugin, "settings");

            if (plugin.Id.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
            {
                var settingsWindowHandle = window.Current.NativeWindowHandle;
                var openStyleSettingsButton = TryWaitForAutomationId(window, "OpenStyleSettingsButton", TimeSpan.FromSeconds(5))
                    ?? TryWaitForAutomationId(window, "_openStyleSettingsButton", TimeSpan.FromSeconds(2))
                    ?? TryWaitForDescendantByName(window, "Open Style Settings", ControlType.Button, TimeSpan.FromSeconds(6))
                    ?? WaitForDescendantByName(window, "Open Style", ControlType.Button, TimeSpan.FromSeconds(6));

                if (openStyleSettingsButton.Current.IsEnabled)
                {
                    Click(openStyleSettingsButton);
                    var styleWindow = WaitForAnyWindow(
                        process.Id,
                        new[] { "Menu Style Settings", "Shell Integration" },
                        TimeSpan.FromSeconds(15),
                        settingsWindowHandle);
                    Console.WriteLine($"[workbench-smoke] Shell style window opened: {styleWindow.Current.Name}");
                    CaptureAndValidateVisual(styleWindow, options, plugin, "shell-style-settings");
                    CloseWindow(styleWindow);
                }
                else
                {
                    Console.WriteLine("[workbench-smoke] Shell style settings button is present but disabled in the current host state.");
                }
            }

            var modeToggleButton = WaitForAutomationId(window, "ModeToggleButton", TimeSpan.FromSeconds(5));
            Click(modeToggleButton);
            Console.WriteLine("[workbench-smoke] Real Runtime switch requested");

            WaitUntil(
                () => ReadElementText(subtitle).Contains("Mode: RealRuntime", StringComparison.OrdinalIgnoreCase),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMilliseconds(250));

            if (runButton is not null)
            {
                optimizationTab = WaitForAutomationId(window, "OptimizationTabItem", TimeSpan.FromSeconds(10));
                Select(optimizationTab);
                runButton = WaitForAutomationId(window, "OptimizationRunButton", TimeSpan.FromSeconds(15));
                WaitUntil(
                    () => runButton.Current.IsEnabled,
                    TimeSpan.FromSeconds(15),
                    TimeSpan.FromMilliseconds(250));
            }

            Console.WriteLine("[workbench-smoke] Real Runtime mode verified");
            CaptureAndValidateVisual(window, options, plugin, "real-runtime");

            CloseWindow(window);
            process.WaitForExit(5000);
            Console.WriteLine("[workbench-smoke] PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[workbench-smoke] FAIL");
            if (mainWindow is not null)
            {
                try
                {
                    PrintWorkbenchLog(mainWindow);
                }
                catch (Exception logException)
                {
                    Console.Error.WriteLine($"[workbench-smoke] Unable to read Workbench log: {logException.Message}");
                }
            }

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
        var theme = "Dark";

        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--plugin-id", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]))
                    pluginId = args[i + 1];

                continue;
            }

            if (string.Equals(args[i], "--theme", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length && !string.IsNullOrWhiteSpace(args[i + 1]))
                    theme = args[i + 1];
            }
        }

        return new SmokeOptions(repositoryRoot, pluginId, theme);
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

            var manifestPath = ResolveManifestPath(pluginDirectory);
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
                var manifestPath = ResolveManifestPath(directory);
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

    private static string ResolveManifestPath(string directory)
    {
        var unified = Path.Combine(directory, "plugin.manifest.json");
        if (File.Exists(unified))
            return unified;

        return Path.Combine(directory, "plugin.json");
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

    private static bool RequiresOptimizationAction(PluginDescriptor plugin)
    {
        return plugin.Id.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase) ||
               plugin.Id.Equals("shell-integration", StringComparison.OrdinalIgnoreCase);
    }

    private static void PrintWorkbenchLog(AutomationElement window)
    {
        var logExpander = TryWaitForAutomationId(window, "LogExpander", TimeSpan.FromSeconds(2));
        if (logExpander is not null &&
            logExpander.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var pattern))
        {
            ((ExpandCollapsePattern)pattern).Expand();
        }

        var logTextBox = TryWaitForAutomationId(window, "LogTextBox", TimeSpan.FromSeconds(10));
        if (logTextBox is not null)
            Console.WriteLine($"[workbench-smoke] Workbench log:\n{ReadElementText(logTextBox)}");
    }

    private static void CaptureAndValidateVisual(AutomationElement window, SmokeOptions options, PluginDescriptor plugin, string stage)
    {
        BringWindowToForeground(window);

        var bounds = window.Current.BoundingRectangle;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            throw new InvalidOperationException($"Visual capture target has invalid bounds for {plugin.Id}/{options.Theme}/{stage}: {bounds}.");

        var left = (int)Math.Floor(bounds.Left);
        var top = (int)Math.Floor(bounds.Top);
        var width = Math.Max(1, (int)Math.Ceiling(bounds.Width));
        var height = Math.Max(1, (int)Math.Ceiling(bounds.Height));

        using var bitmap = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(left, top, 0, 0, new Size(width, height));
        }

        var stats = AnalyzeBitmap(bitmap);
        if (stats.SampleCount == 0 || stats.LuminanceRange < 20 || stats.StandardDeviation < 6)
        {
            throw new InvalidOperationException(
                $"Visual capture appears blank or unreadable for {plugin.Id}/{options.Theme}/{stage}. " +
                $"Samples={stats.SampleCount}, range={stats.LuminanceRange:0.0}, stddev={stats.StandardDeviation:0.0}.");
        }

        var outputDirectory = Path.Combine(
            options.RepositoryRoot,
            "artifacts",
            "workbench-visual",
            $"{plugin.Id}-{options.Theme.ToLowerInvariant()}");
        Directory.CreateDirectory(outputDirectory);

        var screenshotPath = Path.Combine(outputDirectory, $"{stage}.png");
        bitmap.Save(screenshotPath, ImageFormat.Png);

        var statsPath = Path.Combine(outputDirectory, $"{stage}.json");
        var report = new
        {
            plugin = plugin.Id,
            pluginVersion = plugin.Version,
            options.Theme,
            stage,
            screenshotPath,
            bounds = new { left, top, width, height },
            stats.SampleCount,
            stats.MinLuminance,
            stats.MaxLuminance,
            stats.LuminanceRange,
            stats.StandardDeviation
        };
        File.WriteAllText(statsPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"[workbench-smoke] Visual capture saved: {screenshotPath} (range={stats.LuminanceRange:0.0}, stddev={stats.StandardDeviation:0.0})");
    }

    private static void BringWindowToForeground(AutomationElement window)
    {
        var handle = window.Current.NativeWindowHandle;
        if (handle == 0)
            return;

        ShowWindow((IntPtr)handle, SwRestore);
        SetForegroundWindow((IntPtr)handle);
        Thread.Sleep(350);
    }

    private static VisualStats AnalyzeBitmap(Bitmap bitmap)
    {
        var stride = Math.Max(1, Math.Min(bitmap.Width, bitmap.Height) / 180);
        var min = double.MaxValue;
        var max = double.MinValue;
        var sum = 0d;
        var sumSquared = 0d;
        var count = 0;

        for (var y = 0; y < bitmap.Height; y += stride)
        {
            for (var x = 0; x < bitmap.Width; x += stride)
            {
                var pixel = bitmap.GetPixel(x, y);
                var luminance = 0.2126 * pixel.R + 0.7152 * pixel.G + 0.0722 * pixel.B;
                min = Math.Min(min, luminance);
                max = Math.Max(max, luminance);
                sum += luminance;
                sumSquared += luminance * luminance;
                count++;
            }
        }

        if (count == 0)
            return new VisualStats(0, 0, 0, 0, 0);

        var mean = sum / count;
        var variance = Math.Max(0, (sumSquared / count) - (mean * mean));
        return new VisualStats(count, min, max, max - min, Math.Sqrt(variance));
    }

    private static AutomationElement WaitForMainWindow(int processId, TimeSpan timeout)
    {
        return WaitForWindow(processId, "Universal Device Toolkit Plugin Workbench - TEST", timeout, automationId: "PluginWorkbenchMainWindow");
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

    private static AutomationElement WaitForAnyWindow(int processId, IReadOnlyList<string> windowNames, TimeSpan timeout, params int[] excludedHandles)
    {
        var found = WaitUntil(
            () => FindAnyWindow(processId, windowNames, excludedHandles) is not null,
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for one of: {string.Join(", ", windowNames)}");

        return FindAnyWindow(processId, windowNames, excludedHandles)
            ?? throw new InvalidOperationException($"Window not found: {string.Join(", ", windowNames)}");
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

    private static AutomationElement? FindAnyWindow(int processId, IReadOnlyList<string> windowNames, params int[] excludedHandles)
    {
        var excluded = excludedHandles.Where(handle => handle != 0).ToHashSet();
        var windows = AutomationElement.RootElement.FindAll(
            TreeScope.Children,
            new AndCondition(
                new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)));

        foreach (AutomationElement window in windows)
        {
            if (excluded.Contains(window.Current.NativeWindowHandle))
                continue;

            if (windowNames.Any(name => string.Equals(window.Current.Name, name, StringComparison.OrdinalIgnoreCase)))
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

    private static AutomationElement? TryWaitForAutomationId(AutomationElement root, string automationId, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => FindByAutomationId(root, automationId) is not null,
            timeout,
            TimeSpan.FromMilliseconds(250));

        return found ? FindByAutomationId(root, automationId) : null;
    }

    private static AutomationElement? FindByAutomationId(AutomationElement root, string automationId)
    {
        var matches = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId))
            .Cast<AutomationElement>()
            .ToArray();

        if (matches.Length == 0)
            return null;

        return matches.FirstOrDefault(IsInteractable)
               ?? matches.FirstOrDefault(IsVisible)
               ?? matches[0];
    }

    private static bool IsVisible(AutomationElement? element)
    {
        if (element is null)
            return false;

        try
        {
            return !element.Current.IsOffscreen;
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
    }

    private static bool IsInteractable(AutomationElement? element)
    {
        if (!IsVisible(element))
            return false;

        try
        {
            return element is not null
                   && element.Current.IsEnabled
                   && element.Current.BoundingRectangle.Width > 0
                   && element.Current.BoundingRectangle.Height > 0;
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
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
            () => FindDescendantByName(root, name, controlType) is not null,
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for descendant '{name}'.");

        return FindDescendantByName(root, name, controlType)
               ?? throw new InvalidOperationException($"Descendant '{name}' not found.");
    }

    private static AutomationElement? TryWaitForDescendantByName(AutomationElement root, string name, ControlType controlType, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => FindDescendantByName(root, name, controlType) is not null,
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            return null;

        return FindDescendantByName(root, name, controlType);
    }

    private static AutomationElement? FindDescendantByName(AutomationElement root, string name, ControlType controlType)
    {
        var matches = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(AutomationElement.NameProperty, name),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, controlType)))
            .Cast<AutomationElement>()
            .ToArray();

        if (matches.Length == 0)
            return null;

        return matches.FirstOrDefault(IsInteractable)
               ?? matches.FirstOrDefault(IsVisible)
               ?? matches[0];
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

    private sealed record SmokeOptions(string RepositoryRoot, string PluginId, string Theme);
    private sealed record PluginDescriptor(string FolderName, string Id, string Name, string Version);
    private sealed record VisualStats(
        int SampleCount,
        double MinLuminance,
        double MaxLuminance,
        double LuminanceRange,
        double StandardDeviation);
}
