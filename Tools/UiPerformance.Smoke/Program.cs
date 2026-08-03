using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Windows.Automation;
using UniversalDeviceToolkit.CLI.Lib;

namespace UiPerformance.Smoke;

/// <summary>
/// Measures navigation latency and process resource cost for every main shell surface.
/// Uses UI Automation (same approach as VisualRegression / MainAppPluginUi smoke).
/// </summary>
internal static class Program
{
    private const string AppDataOverrideEnvironmentVariable = "UDT_APPDATA_OVERRIDE";
    private static readonly string[] MainAppBaseNames = ["Universal Device Toolkit", "Lenovo Legion Toolkit"];
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private const int WindowX = 80;
    private const int WindowY = 80;
    private const int WindowWidth = 1300;
    private const int WindowHeight = 850;

    // Navigation latency budgets (ms). Exceeding "fair" flags the surface.
    private const double ExcellentNavMs = 400;
    private const double GoodNavMs = 900;
    private const double FairNavMs = 1800;

    public static int Main(string[] args)
    {
        Process? process = null;
        try
        {
            var options = Options.Parse(args);
            var repoRoot = Path.GetFullPath(options.RepoRoot);
            var outputRoot = Path.GetFullPath(options.OutputDirectory);
            var sandboxRoot = Path.Combine(outputRoot, "sandbox");
            var appDataDirectory = Path.Combine(sandboxRoot, "appdata");

            ResetDirectory(outputRoot);
            Directory.CreateDirectory(appDataDirectory);
            Directory.CreateDirectory(Path.Combine(appDataDirectory, "plugins"));
            PrepareSandboxSettings(appDataDirectory);

            if (options.KillExisting)
                TryKillExistingAppInstances();

            var runtimeDirectory = ResolveRuntimeDirectory(repoRoot, options.Configuration);
            Console.WriteLine($"[ui-perf] Runtime: {runtimeDirectory}");
            Console.WriteLine($"[ui-perf] Output:  {outputRoot}");
            Console.WriteLine($"[ui-perf] Iterations per surface: {options.Iterations}");

            var coldStart = Stopwatch.StartNew();
            process = StartApp(runtimeDirectory, appDataDirectory);
            TryWaitForInputIdle(process, 15_000);
            // Prefer the real shell (nav markers). PID may change if the app re-elevates.
            var mainWindow = WaitForMainShellWindow(process.Id, TimeSpan.FromSeconds(120));
            try
            {
                var livePid = mainWindow.Current.ProcessId;
                if (livePid != process.Id)
                {
                    Console.WriteLine($"[ui-perf] Shell PID {livePid} differs from launch PID {process.Id} (elevation/relaunch).");
                    process = Process.GetProcessById(livePid);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ui-perf] Could not rebind process from shell window: {ex.Message}");
            }

            NormalizeWindow(mainWindow);
            WaitUntilNavigationReady(mainWindow, TimeSpan.FromSeconds(60));
            WaitForIpcReady(appDataDirectory, TimeSpan.FromSeconds(15));
            WaitForAnimationsToComplete();
            coldStart.Stop();

            var suite = new SuiteResult
            {
                StartedUtc = DateTimeOffset.UtcNow,
                MachineName = Environment.MachineName,
                Configuration = options.Configuration,
                RuntimeDirectory = runtimeDirectory,
                ProcessId = process.Id,
                ColdStartMs = coldStart.ElapsedMilliseconds,
                Baseline = CaptureProcessSnapshot(process)
            };

            Console.WriteLine($"[ui-perf] Cold start (to shell ready): {suite.ColdStartMs} ms");
            Console.WriteLine($"[ui-perf] Baseline WS={suite.Baseline.WorkingSetMb:F1} MB private={suite.Baseline.PrivateMb:F1} MB handles={suite.Baseline.HandleCount}");

            var surfaces = BuildSurfaces();
            foreach (var surface in surfaces)
            {
                Console.WriteLine($"[ui-perf] === {surface.Id} ===");
                var result = MeasureSurface(process, mainWindow, surface, options.Iterations);
                suite.Surfaces.Add(result);
                Console.WriteLine(
                    $"[ui-perf] {surface.Id}: ready={result.ReadyMsMedian:F0} ms settle={result.SettleMsMedian:F0} ms " +
                    $"ΔWS={result.WorkingSetDeltaMbMedian:+0.0;-0.0;0} MB uia≈{result.UiaElementCountMedian} [{result.Rating}]");
            }

            // Return to dashboard and capture final process cost.
            try
            {
                if (!process.HasExited)
                {
                    TryNavigate(mainWindow, surfaces[0]);
                    WaitForAnimationsToComplete();
                    suite.Final = CaptureProcessSnapshot(process);
                }
                else
                {
                    suite.Final = suite.Baseline;
                    Console.WriteLine("[ui-perf] App exited before final snapshot; using baseline.");
                }
            }
            catch (Exception ex)
            {
                suite.Final = suite.Baseline;
                Console.WriteLine($"[ui-perf] Final snapshot skipped: {ex.Message}");
            }

            suite.FinishedUtc = DateTimeOffset.UtcNow;
            suite.TotalWallMs = (suite.FinishedUtc - suite.StartedUtc).TotalMilliseconds;

            WriteReports(outputRoot, suite);
            PrintSummary(suite);

            if (options.KeepApp)
            {
                Console.WriteLine("[ui-perf] Leaving app running (--keep-app).");
                process = null;
                return suite.FailedCount > 0 ? 2 : 0;
            }

            TryCloseProcess(process);
            process = null;
            return suite.FailedCount > 0 ? 2 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[ui-perf] FAILED:");
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (process is not null && !process.HasExited)
                TryCloseProcess(process);
        }
    }

    private static List<SurfaceTarget> BuildSurfaces() =>
    [
        new("dashboard",
            ["DashboardNavItem", "_dashboardItem"],
            ["Dashboard", "控制台", "Home"],
            root => TitleOrText(root, "Home", "Dashboard", "控制台", "Power Mode", "性能模式", "Sensors", "传感器")
                    || IsVisible(FindByAutomationId(root, "MainRootFrame"))),

        new("keyboard",
            ["_keyboardItem"],
            ["Keyboard", "键盘", "Keyboard Backlight"],
            root => TitleOrText(root, "Keyboard", "键盘", "No compatible keyboards", "无兼容")
                    || IsVisible(FindByAutomationId(root, "KeyboardBacklightPageRoot")),
            optional: true),

        new("automation",
            ["_automationItem"],
            ["Actions", "Automation", "自动化", "动作"],
            root => TitleOrText(root, "Automation", "自动化", "Actions", "动作", "Quick Actions", "快速操作")
                    || WindowTitleContains(root, "自动化", "Automation", "Actions")),

        new("macro",
            ["_macroItem"],
            ["Macro", "宏", "自定义宏"],
            root => TitleOrText(root, "Macro", "宏", "M1", "Record", "录制")
                    || WindowTitleContains(root, "宏", "Macro")),

        new("winopt-home",
            ["WindowsOptimizationNavItem", "_windowsOptimizationItem"],
            ["System optimization", "Windows Optimization", "系统优化"],
            root => IsVisible(FindByAutomationId(root, "WindowsOptimizationOptimizationTabButton"))
                    || TitleOrText(root, "System optimization", "系统优化", "Optimization", "优化")),

        new("winopt-optimization",
            ["WindowsOptimizationOptimizationTabButton"],
            ["Optimization", "优化"],
            root => IsVisible(FindByAutomationId(root, "WindowsOptimizationOptimizationTabButton"))
                    || TitleOrText(root, "Optimization", "优化", "Recommended", "推荐"),
            isTab: true),

        new("winopt-cleanup",
            ["WindowsOptimizationCleanupTabButton"],
            ["Cleanup", "清理"],
            root => IsVisible(FindByAutomationId(root, "WindowsOptimizationCategoryList"))
                    || TitleOrText(root, "Cleanup", "清理"),
            isTab: true,
            parentNavIds: ["WindowsOptimizationNavItem", "_windowsOptimizationItem"]),

        new("winopt-driver",
            ["WindowsOptimizationDriverTabButton"],
            ["Driver", "驱动"],
            root => IsVisible(FindByAutomationId(root, "WindowsOptimizationDriverSearchButton"))
                    || TitleOrText(root, "Driver", "驱动", "Driver Download", "驱动下载"),
            isTab: true,
            parentNavIds: ["WindowsOptimizationNavItem", "_windowsOptimizationItem"]),

        new("winopt-network",
            ["WindowsOptimizationNetworkAccelerationTabButton"],
            ["Network", "网络", "加速"],
            root => IsVisible(FindByAutomationId(root, "NetworkAccelerationPageScrollViewer"))
                    || IsVisible(FindByAutomationId(root, "NetworkAccelerationControlCard"))
                    || TitleOrText(root, "Network", "网络", "加速"),
            isTab: true,
            parentNavIds: ["WindowsOptimizationNavItem", "_windowsOptimizationItem"]),

        new("plugins",
            ["PluginExtensionsNavItem"],
            ["Plugin", "插件", "Extensions"],
            root => WindowTitleContains(root, "插件", "Plugin", "Extensions")
                    || TitleOrText(root, "Plugin", "插件", "Extensions", "扩展", "Install", "安装", "已安装", "商店")),

        new("settings",
            ["SettingsNavItem", "_settingsItem"],
            ["Settings", "设置"],
            root => WindowTitleContains(root, "设置", "Settings")
                    || TitleOrText(root, "Settings", "设置", "Theme", "主题")),

        new("about",
            ["_aboutItem"],
            ["About", "关于"],
            root => WindowTitleContains(root, "关于", "About")
                    || TitleOrText(root, "About", "关于", "Third-party", "第三方", "Application Folders", "应用文件夹", "版本")),

        new("device-info-dialog",
            ["DeviceInfoIndicator"],
            ["Device", "设备"],
            root => FindVisibleWindowTitleContains("Device", "设备", "Device Information", "设备信息")
                    || TitleOrText(root, "Device", "设备", "Machine", "机型", "BIOS", "SN", "机型", "序列"),
            isDialog: true),
    ];

    private static SurfaceResult MeasureSurface(Process process, AutomationElement mainWindow, SurfaceTarget surface, int iterations)
    {
        var readySamples = new List<double>();
        var settleSamples = new List<double>();
        var deltaWs = new List<double>();
        var deltaPrivate = new List<double>();
        var handleSamples = new List<int>();
        var uiaSamples = new List<int>();
        string? error = null;

        for (var i = 0; i < iterations; i++)
        {
            try
            {
                // Navigate away first so re-entry cost is real (skip for first surface / dialogs).
                if (!surface.IsDialog && i > 0)
                {
                    TryNavigate(mainWindow, BuildSurfaces()[0]);
                    Thread.Sleep(250);
                }

                if (surface.ParentNavIds is { Length: > 0 })
                {
                    EnsureParentNav(mainWindow, surface.ParentNavIds);
                }

                var before = CaptureProcessSnapshot(process);
                var sw = Stopwatch.StartNew();

                mainWindow = ResolveLiveWindow(mainWindow, process.Id);
                BringToForeground(mainWindow);

                if (!TryActivateTarget(mainWindow, surface))
                {
                    if (surface.Optional)
                    {
                        Console.WriteLine($"[ui-perf] {surface.Id}: optional surface not present — skipped.");
                        return new SurfaceResult
                        {
                            Id = surface.Id,
                            Iterations = 0,
                            Rating = "skipped",
                            Error = null
                        };
                    }

                    throw new InvalidOperationException($"Could not activate surface '{surface.Id}'.");
                }

                var ready = WaitUntil(
                    () =>
                    {
                        try
                        {
                            return surface.Ready(ResolveLiveWindow(mainWindow, process.Id));
                        }
                        catch
                        {
                            return false;
                        }
                    },
                    TimeSpan.FromSeconds(surface.IsDialog ? 25 : 20),
                    TimeSpan.FromMilliseconds(100));

                sw.Stop();
                var readyMs = sw.Elapsed.TotalMilliseconds;

                if (!ready)
                    throw new TimeoutException($"Timed out waiting for ready: {surface.Id}");

                var settleSw = Stopwatch.StartNew();
                WaitForAnimationsToComplete();
                settleSw.Stop();

                var after = CaptureProcessSnapshot(process);
                var uiaCount = CountAutomationElements(ResolveLiveWindow(mainWindow, process.Id), max: 800);

                readySamples.Add(readyMs);
                settleSamples.Add(readyMs + settleSw.Elapsed.TotalMilliseconds);
                deltaWs.Add(after.WorkingSetMb - before.WorkingSetMb);
                deltaPrivate.Add(after.PrivateMb - before.PrivateMb);
                handleSamples.Add(after.HandleCount);
                uiaSamples.Add(uiaCount);

                if (surface.IsDialog)
                    TryDismissDialog(process.Id);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Console.WriteLine($"[ui-perf] {surface.Id} iteration {i + 1} failed: {ex.Message}");
            }
        }

        var result = new SurfaceResult
        {
            Id = surface.Id,
            Iterations = readySamples.Count,
            ReadyMsMedian = Median(readySamples),
            ReadyMsP95 = Percentile(readySamples, 0.95),
            SettleMsMedian = Median(settleSamples),
            WorkingSetDeltaMbMedian = Median(deltaWs),
            PrivateDeltaMbMedian = Median(deltaPrivate),
            HandleCountMedian = handleSamples.Count > 0 ? (int)Median(handleSamples.Select(h => (double)h).ToList()) : 0,
            UiaElementCountMedian = uiaSamples.Count > 0 ? (int)Median(uiaSamples.Select(u => (double)u).ToList()) : 0,
            Error = error
        };

        if (result.Error is not null)
            result.Rating = "failed";
        else if (result.Iterations == 0)
            result.Rating = "skipped";
        else
            result.Rating = RateNav(result.ReadyMsMedian);

        return result;
    }

    private static void EnsureParentNav(AutomationElement mainWindow, string[] parentIds)
    {
        foreach (var id in parentIds)
        {
            var el = FindByAutomationId(mainWindow, id) ?? FindByName(mainWindow, id);
            if (el is null)
                continue;
            ActivateElement(el, preferMouseClick: true);
            Thread.Sleep(400);
            return;
        }
    }

    private static bool TryActivateTarget(AutomationElement mainWindow, SurfaceTarget surface)
    {
        foreach (var id in surface.AutomationIds)
        {
            var el = FindByAutomationId(mainWindow, id);
            if (el is not null && IsVisible(el))
            {
                ActivateElement(el, preferMouseClick: !surface.IsTab);
                return true;
            }
        }

        foreach (var name in surface.Names)
        {
            var el = FindByName(mainWindow, name);
            if (el is not null && IsVisible(el))
            {
                ActivateElement(el, preferMouseClick: !surface.IsTab);
                return true;
            }
        }

        // Partial name match on list items / buttons (localized nav).
        try
        {
            var candidates = mainWindow.FindAll(TreeScope.Descendants, Condition.TrueCondition);
            foreach (AutomationElement candidate in candidates)
            {
                try
                {
                    if (candidate.Current.IsOffscreen)
                        continue;
                    var name = candidate.Current.Name ?? "";
                    var autoId = candidate.Current.AutomationId ?? "";
                    if (surface.AutomationIds.Any(id => autoId.Equals(id, StringComparison.OrdinalIgnoreCase))
                        || surface.Names.Any(n => name.Contains(n, StringComparison.OrdinalIgnoreCase)))
                    {
                        ActivateElement(candidate, preferMouseClick: true);
                        return true;
                    }
                }
                catch
                {
                    // ignore element churn
                }
            }
        }
        catch
        {
            // ignore
        }

        DumpNavigationSnapshot(mainWindow);
        return false;
    }

    private static void TryNavigate(AutomationElement mainWindow, SurfaceTarget surface)
    {
        try
        {
            TryActivateTarget(mainWindow, surface);
            WaitUntil(() => surface.Ready(mainWindow), TimeSpan.FromSeconds(8), TimeSpan.FromMilliseconds(150));
        }
        catch
        {
            // best-effort reset
        }
    }

    private static void TryDismissDialog(int processId)
    {
        try
        {
            // Esc closes most device-info style dialogs.
            keybd_event(0x1B, 0, 0, UIntPtr.Zero);
            Thread.Sleep(40);
            keybd_event(0x1B, 0, 0x0002, UIntPtr.Zero);
            Thread.Sleep(200);

            // Fallback: try close button on secondary windows.
            var desktop = AutomationElement.RootElement;
            var condition = new PropertyCondition(AutomationElement.ProcessIdProperty, processId);
            var windows = desktop.FindAll(TreeScope.Children, condition);
            foreach (AutomationElement window in windows)
            {
                if (window.Current.ControlType != ControlType.Window)
                    continue;
                if (window.Current.Name.Contains("Universal Device Toolkit", StringComparison.OrdinalIgnoreCase)
                    && !window.Current.Name.Contains("Device", StringComparison.OrdinalIgnoreCase)
                    && !window.Current.Name.Contains("设备", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    if (window.TryGetCurrentPattern(WindowPattern.Pattern, out var pattern))
                        ((WindowPattern)pattern).Close();
                }
                catch
                {
                    // ignore
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private static string RateNav(double readyMs) =>
        readyMs <= ExcellentNavMs ? "excellent" :
        readyMs <= GoodNavMs ? "good" :
        readyMs <= FairNavMs ? "fair" :
        "needs work";

    private static void PrintSummary(SuiteResult suite)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("UI performance summary");
        Console.WriteLine("========================================");
        Console.WriteLine($"Cold start: {suite.ColdStartMs} ms");
        Console.WriteLine($"Working set: {suite.Baseline.WorkingSetMb:F1} → {suite.Final.WorkingSetMb:F1} MB");
        Console.WriteLine();
        Console.WriteLine($"{"Surface",-22} {"Ready ms",10} {"Settle ms",10} {"ΔWS MB",8} {"UIA",6} Rating");
        foreach (var s in suite.Surfaces.OrderByDescending(x => x.ReadyMsMedian))
        {
            Console.WriteLine(
                $"{s.Id,-22} {s.ReadyMsMedian,10:F0} {s.SettleMsMedian,10:F0} {s.WorkingSetDeltaMbMedian,8:F1} {s.UiaElementCountMedian,6} {s.Rating}");
            if (s.Error is not null)
                Console.WriteLine($"  ! {s.Error}");
        }

        Console.WriteLine();
        Console.WriteLine($"Failed surfaces: {suite.FailedCount}");
        Console.WriteLine($"Slow (needs work): {suite.Surfaces.Count(s => s.Rating == "needs work")}");
    }

    private static void WriteReports(string outputRoot, SuiteResult suite)
    {
        var jsonPath = Path.Combine(outputRoot, "ui-perf-report.json");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(suite, JsonOptions), Encoding.UTF8);

        var md = new StringBuilder();
        md.AppendLine("# UI Performance Report");
        md.AppendLine();
        md.AppendLine($"- Started: `{suite.StartedUtc:O}`");
        md.AppendLine($"- Machine: `{suite.MachineName}`");
        md.AppendLine($"- Configuration: `{suite.Configuration}`");
        md.AppendLine($"- Cold start: **{suite.ColdStartMs} ms**");
        md.AppendLine($"- Working set: **{suite.Baseline.WorkingSetMb:F1} → {suite.Final.WorkingSetMb:F1} MB**");
        md.AppendLine($"- Private bytes: **{suite.Baseline.PrivateMb:F1} → {suite.Final.PrivateMb:F1} MB**");
        md.AppendLine($"- Handles: **{suite.Baseline.HandleCount} → {suite.Final.HandleCount}**");
        md.AppendLine();
        md.AppendLine("## Surfaces (slowest first)");
        md.AppendLine();
        md.AppendLine("| Surface | Ready ms (median) | P95 | Settle ms | ΔWS MB | UIA nodes | Rating | Error |");
        md.AppendLine("|---|---:|---:|---:|---:|---:|---|---|");
        foreach (var s in suite.Surfaces.OrderByDescending(x => x.ReadyMsMedian))
        {
            md.AppendLine(
                $"| `{s.Id}` | {s.ReadyMsMedian:F0} | {s.ReadyMsP95:F0} | {s.SettleMsMedian:F0} | {s.WorkingSetDeltaMbMedian:F1} | {s.UiaElementCountMedian} | {s.Rating} | {EscapeMd(s.Error)} |");
        }

        md.AppendLine();
        md.AppendLine("## Rating thresholds (ready latency)");
        md.AppendLine();
        md.AppendLine($"- excellent ≤ {ExcellentNavMs} ms");
        md.AppendLine($"- good ≤ {GoodNavMs} ms");
        md.AppendLine($"- fair ≤ {FairNavMs} ms");
        md.AppendLine($"- needs work > {FairNavMs} ms");
        md.AppendLine();
        md.AppendLine("## How to dig deeper");
        md.AppendLine();
        md.AppendLine("1. **Visual Studio** → Debug → Performance Profiler → CPU Usage / .NET Object Allocation / UI Analysis");
        md.AppendLine("2. **dotnet-counters** while app is open: `dotnet-counters monitor --process-id <pid> System.Runtime Microsoft.Windows.Desktop.App.WPF`");
        md.AppendLine("3. **PerfView** collect: `PerfView /nogui collect /MaxCollectSec:60` then open app pages");
        md.AppendLine("4. **WPF Performance Suite** / Visual Studio Live Visual Tree for layout thrash");
        md.AppendLine("5. Backend microbenches: `dotnet run -c Release --project UniversalDeviceToolkit.PerformanceTest`");

        File.WriteAllText(Path.Combine(outputRoot, "ui-perf-report.md"), md.ToString(), Encoding.UTF8);
        Console.WriteLine($"[ui-perf] Wrote {jsonPath}");
        Console.WriteLine($"[ui-perf] Wrote {Path.Combine(outputRoot, "ui-perf-report.md")}");
    }

    private static string EscapeMd(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", " ").Replace("\n", " ");

    private static ProcessSnapshot CaptureProcessSnapshot(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return new ProcessSnapshot { TimestampUtc = DateTimeOffset.UtcNow };
            }

            process.Refresh();
            return new ProcessSnapshot
            {
                WorkingSetMb = process.WorkingSet64 / (1024.0 * 1024.0),
                PrivateMb = process.PrivateMemorySize64 / (1024.0 * 1024.0),
                VirtualMb = process.VirtualMemorySize64 / (1024.0 * 1024.0),
                HandleCount = process.HandleCount,
                ThreadCount = process.Threads.Count,
                TimestampUtc = DateTimeOffset.UtcNow
            };
        }
        catch (InvalidOperationException)
        {
            return new ProcessSnapshot { TimestampUtc = DateTimeOffset.UtcNow };
        }
    }

    private static bool WindowTitleContains(AutomationElement root, params string[] needles)
    {
        try
        {
            var title = root.Current.Name ?? "";
            return needles.Any(n => title.Contains(n, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private static int CountAutomationElements(AutomationElement root, int max)
    {
        var count = 0;
        try
        {
            var walker = TreeWalker.ControlViewWalker;
            void Walk(AutomationElement node, int depth)
            {
                if (count >= max || depth > 12)
                    return;
                count++;
                AutomationElement? child;
                try { child = walker.GetFirstChild(node); }
                catch { return; }
                while (child is not null && count < max)
                {
                    Walk(child, depth + 1);
                    try { child = walker.GetNextSibling(child); }
                    catch { break; }
                }
            }
            Walk(root, 0);
        }
        catch
        {
            // UIA can throw if tree mutates
        }

        return count;
    }

    private static double Median(List<double> values)
    {
        if (values.Count == 0)
            return 0;
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    private static double Percentile(List<double> values, double p)
    {
        if (values.Count == 0)
            return 0;
        var sorted = values.OrderBy(v => v).ToList();
        var idx = (int)Math.Clamp(Math.Ceiling(p * sorted.Count) - 1, 0, sorted.Count - 1);
        return sorted[idx];
    }

    private static bool TitleOrText(AutomationElement root, params string[] needles)
    {
        try
        {
            var title = root.Current.Name ?? "";
            foreach (var n in needles)
            {
                if (title.Contains(n, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch
        {
            // ignore
        }

        return needles.Any(n => FindVisibleTextContains(root, n));
    }

    private static bool FindVisibleWindowTitleContains(params string[] needles)
    {
        try
        {
            var desktop = AutomationElement.RootElement;
            var windows = desktop.FindAll(TreeScope.Children, Condition.TrueCondition);
            foreach (AutomationElement window in windows)
            {
                var name = window.Current.Name ?? "";
                if (needles.Any(n => name.Contains(n, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }

    private static bool FindVisibleTextContains(AutomationElement root, string text)
    {
        try
        {
            var condition = new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text),
                new PropertyCondition(AutomationElement.IsOffscreenProperty, false));
            var nodes = root.FindAll(TreeScope.Descendants, condition);
            foreach (AutomationElement node in nodes)
            {
                var name = node.Current.Name ?? "";
                if (name.Contains(text, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }

    private static AutomationElement? FindByAutomationId(AutomationElement root, string automationId)
    {
        try
        {
            return root.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
        }
        catch
        {
            return null;
        }
    }

    private static AutomationElement? FindByName(AutomationElement root, string name)
    {
        try
        {
            return root.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.NameProperty, name));
        }
        catch
        {
            return null;
        }
    }

    private static bool IsVisible(AutomationElement? element)
    {
        if (element is null)
            return false;
        try
        {
            return !element.Current.IsOffscreen;
        }
        catch
        {
            return false;
        }
    }

    private static void ActivateElement(AutomationElement element, bool preferMouseClick = true)
    {
        // Custom NavigationItem often has no InvokePattern — mouse click is reliable (same as VisualRegression smoke).
        if (preferMouseClick)
        {
            try
            {
                MouseClick(element);
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ui-perf] MouseClick failed: {ex.Message}");
            }
        }

        try
        {
            if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var invoke))
            {
                ((InvokePattern)invoke).Invoke();
                return;
            }
        }
        catch
        {
            // fall through
        }

        try
        {
            if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var sel))
            {
                ((SelectionItemPattern)sel).Select();
                return;
            }
        }
        catch
        {
            // fall through
        }

        try
        {
            MouseClick(element);
        }
        catch
        {
            // last resort failed
        }
    }

    private static void MouseClick(AutomationElement element)
    {
        var bounds = element.Current.BoundingRectangle;
        if (bounds.IsEmpty || bounds.Width <= 1 || bounds.Height <= 1)
            throw new InvalidOperationException(
                $"Cannot click element with empty bounds: id={element.Current.AutomationId} name={element.Current.Name}");

        var x = (int)Math.Round(bounds.X + bounds.Width / 2);
        var y = (int)Math.Round(bounds.Y + bounds.Height / 2);
        SetCursorPos(x, y);
        Thread.Sleep(30);
        mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero); // left down
        Thread.Sleep(40);
        mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero); // left up
        Thread.Sleep(80);
    }

    private static void DumpNavigationSnapshot(AutomationElement root)
    {
        Console.WriteLine("[ui-perf] --- UIA snapshot (nav candidates) ---");
        try
        {
            Console.WriteLine($"[ui-perf] Window: '{root.Current.Name}' pid={root.Current.ProcessId}");
            var count = 0;
            foreach (AutomationElement el in root.FindAll(TreeScope.Descendants, Condition.TrueCondition))
            {
                if (count >= 80)
                    break;
                try
                {
                    var id = el.Current.AutomationId ?? "";
                    var name = el.Current.Name ?? "";
                    if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(name))
                        continue;
                    if (el.Current.IsOffscreen)
                        continue;
                    if (id.Contains("Nav", StringComparison.OrdinalIgnoreCase)
                        || id.Contains("Item", StringComparison.OrdinalIgnoreCase)
                        || id.Contains("Tab", StringComparison.OrdinalIgnoreCase)
                        || name.Length is > 0 and < 40)
                    {
                        Console.WriteLine($"[ui-perf]   type={el.Current.ControlType.ProgrammaticName} id='{id}' name='{name}'");
                        count++;
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ui-perf] snapshot failed: {ex.Message}");
        }

        Console.WriteLine("[ui-perf] --- end snapshot ---");
    }

    private static void WaitUntilNavigationReady(AutomationElement mainWindow, TimeSpan timeout)
    {
        var ready = WaitUntil(
            () =>
            {
                try
                {
                    var live = mainWindow;
                    TryHandleCompatibilityWindow(live);
                    return IsVisible(FindByAutomationId(live, "MainNavigationStore"))
                           || IsVisible(FindByAutomationId(live, "DashboardNavItem"))
                           || IsVisible(FindByAutomationId(live, "MainRootFrame"));
                }
                catch
                {
                    return false;
                }
            },
            timeout,
            TimeSpan.FromMilliseconds(300));

        if (!ready)
        {
            DumpNavigationSnapshot(mainWindow);
            throw new TimeoutException("Main navigation chrome did not become ready.");
        }

        Console.WriteLine("[ui-perf] Navigation chrome ready.");
    }

    private static bool TryHandleCompatibilityWindow(AutomationElement window)
    {
        var continueButton = FindByAutomationId(window, "_continueButton");
        if (!IsVisible(continueButton) || continueButton is null || !continueButton.Current.IsEnabled)
            return false;

        Console.WriteLine("[ui-perf] Dismissing compatibility/continue dialog.");
        ActivateElement(continueButton, preferMouseClick: true);
        Thread.Sleep(400);
        return true;
    }

    private static bool WaitUntil(Func<bool> predicate, TimeSpan timeout, TimeSpan poll)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (predicate())
                    return true;
            }
            catch
            {
                // transient UIA errors
            }

            Thread.Sleep(poll);
        }

        try
        {
            return predicate();
        }
        catch
        {
            return false;
        }
    }

    private static AutomationElement WaitForMainShellWindow(int processId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            // Prefer windows that already expose navigation chrome.
            var withNav = TryFindMainWindow(processId, requireNavigation: true)
                          ?? TryFindMainWindowByName(requireNavigation: true);
            if (withNav is not null)
                return withNav;

            // Fall back to any UDT-titled window (setup dialogs, etc.) and try to advance them.
            var any = TryFindMainWindow(processId, requireNavigation: false)
                      ?? TryFindMainWindowByName(requireNavigation: false);
            if (any is not null)
            {
                TryHandleCompatibilityWindow(any);
            }

            Thread.Sleep(300);
        }

        throw new TimeoutException($"Main shell window not found within {timeout.TotalSeconds:F0}s.");
    }

    private static AutomationElement? TryFindMainWindow(int processId, bool requireNavigation)
    {
        try
        {
            var condition = new AndCondition(
                new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));
            var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, condition);
            foreach (AutomationElement window in windows)
            {
                if (IsMainShellCandidate(window, requireNavigation))
                    return window;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static AutomationElement? TryFindMainWindowByName(bool requireNavigation)
    {
        try
        {
            var windows = AutomationElement.RootElement.FindAll(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));
            foreach (AutomationElement window in windows)
            {
                try
                {
                    var name = window.Current.Name ?? "";
                    if (!MainAppBaseNames.Any(b => name.Contains(b, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    if (IsMainShellCandidate(window, requireNavigation))
                        return window;
                }
                catch
                {
                    // ignore
                }
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static bool IsMainShellCandidate(AutomationElement window, bool requireNavigation)
    {
        try
        {
            if (window.Current.ControlType != ControlType.Window)
                return false;

            var hasNav = IsVisible(FindByAutomationId(window, "MainNavigationStore"))
                         || IsVisible(FindByAutomationId(window, "DashboardNavItem"))
                         || IsVisible(FindByAutomationId(window, "MainRootFrame"));
            if (requireNavigation)
                return hasNav;

            var name = window.Current.Name ?? "";
            return hasNav
                   || MainAppBaseNames.Any(b => name.Contains(b, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private static AutomationElement ResolveLiveWindow(AutomationElement window, int processId)
    {
        try
        {
            _ = window.Current.Name;
            if (IsVisible(FindByAutomationId(window, "MainNavigationStore"))
                || IsVisible(FindByAutomationId(window, "DashboardNavItem")))
                return window;
        }
        catch
        {
            // stale
        }

        return TryFindMainWindow(processId, requireNavigation: true)
               ?? TryFindMainWindowByName(requireNavigation: true)
               ?? TryFindMainWindow(processId, requireNavigation: false)
               ?? throw new InvalidOperationException("Lost main window handle.");
    }

    private static void NormalizeWindow(AutomationElement window)
    {
        try
        {
            if (window.TryGetCurrentPattern(WindowPattern.Pattern, out var patternObj)
                && patternObj is WindowPattern windowPattern)
            {
                if (windowPattern.Current.WindowVisualState == WindowVisualState.Minimized)
                    windowPattern.SetWindowVisualState(WindowVisualState.Normal);
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            if (window.TryGetCurrentPattern(TransformPattern.Pattern, out var transformObj)
                && transformObj is TransformPattern transform)
            {
                if (transform.Current.CanMove)
                    transform.Move(WindowX, WindowY);
                if (transform.Current.CanResize)
                    transform.Resize(WindowWidth, WindowHeight);
            }
        }
        catch
        {
            // ignore
        }

        BringToForeground(window);
    }

    private static void BringToForeground(AutomationElement window)
    {
        try
        {
            var hwnd = new IntPtr(window.Current.NativeWindowHandle);
            if (hwnd != IntPtr.Zero)
            {
                ShowWindow(hwnd, 9); // SW_RESTORE
                SetForegroundWindow(hwnd);
            }
        }
        catch
        {
            // ignore
        }
    }

    private static void WaitForIpcReady(string appDataDirectory, TimeSpan timeout)
    {
        var pipeName = Constants.GetPipeName(appDataDirectory);
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                client.Connect(250);
                if (client.IsConnected)
                {
                    Console.WriteLine($"[ui-perf] IPC ready: {pipeName}");
                    return;
                }
            }
            catch
            {
                Thread.Sleep(200);
            }
        }

        Console.WriteLine($"[ui-perf] IPC not ready within {timeout.TotalSeconds:F0}s (continuing).");
    }

    private static void WaitForAnimationsToComplete() => Thread.Sleep(700);

    private static Process StartApp(string runtimeDirectory, string appDataDirectory)
    {
        var appBaseName = MainAppBaseNames.FirstOrDefault(name =>
            File.Exists(Path.Combine(runtimeDirectory, $"{name}.dll")) &&
            File.Exists(Path.Combine(runtimeDirectory, $"{name}.runtimeconfig.json")))
            ?? MainAppBaseNames.FirstOrDefault(name => File.Exists(Path.Combine(runtimeDirectory, $"{name}.exe")));

        if (string.IsNullOrWhiteSpace(appBaseName))
            throw new FileNotFoundException($"Could not find startup entry in runtime directory: {runtimeDirectory}");

        var dllPath = Path.Combine(runtimeDirectory, $"{appBaseName}.dll");
        var runtimeConfigPath = Path.Combine(runtimeDirectory, $"{appBaseName}.runtimeconfig.json");
        var exePath = Path.Combine(runtimeDirectory, $"{appBaseName}.exe");

        ProcessStartInfo startInfo;
        if (File.Exists(dllPath) && File.Exists(runtimeConfigPath))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{dllPath}\" --trace --disable-update-checker --disable-tray-tooltip",
                WorkingDirectory = runtimeDirectory,
                UseShellExecute = false
            };
        }
        else if (File.Exists(exePath))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "--trace --disable-update-checker --disable-tray-tooltip",
                WorkingDirectory = runtimeDirectory,
                UseShellExecute = false
            };
        }
        else
            throw new FileNotFoundException($"Could not find startup entry in: {runtimeDirectory}");

        startInfo.EnvironmentVariables[AppDataOverrideEnvironmentVariable] = appDataDirectory;
        startInfo.EnvironmentVariables["UDT_SMOKE_AUTOMATION"] = "1";
        startInfo.EnvironmentVariables["UDT_SMOKE_DISABLE_ANIMATIONS"] = "1";
        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start app process.");
    }

    private static void PrepareSandboxSettings(string appDataDirectory)
    {
        var settingsPath = Path.Combine(appDataDirectory, "settings.json");
        var root = new JsonObject
        {
            ["Theme"] = "Dark",
            ["ThemeStylePreset"] = "Default",
            ["WindowSize"] = new JsonObject { ["Width"] = WindowWidth, ["Height"] = WindowHeight },
            ["MinimizeToTray"] = false,
            ["MinimizeOnClose"] = false,
            ["DisableUnsupportedHardwareWarning"] = true,
            ["ForceSoftwareRendering"] = true,
            ["ExtensionsEnabled"] = false,
            ["AnimationsEnabled"] = false,
            ["NavigationPaneExpanded"] = true
        };
        Directory.CreateDirectory(appDataDirectory);
        File.WriteAllText(settingsPath, root.ToJsonString(JsonOptions));
        File.WriteAllText(Path.Combine(appDataDirectory, "integrations.json"),
            new JsonObject { ["CLI"] = true }.ToJsonString(JsonOptions));
        File.WriteAllText(Path.Combine(appDataDirectory, "lang"), "zh-Hans");
        File.WriteAllLines(Path.Combine(appDataDirectory, "device-setup"),
        [
            "devicePackId=",
            "basicMode=false",
            $"confirmedAtUtc={DateTimeOffset.UtcNow:O}"
        ]);
    }

    private static string ResolveRuntimeDirectory(string repoRoot, string configuration)
    {
        var candidates = new[]
        {
            Path.Combine(repoRoot, "UniversalDeviceToolkit.WPF", "bin", "x64", configuration, "net10.0-windows10.0.26100.0", "win-x64"),
            Path.Combine(repoRoot, "UniversalDeviceToolkit.WPF", "bin", configuration, "net10.0-windows10.0.26100.0", "win-x64"),
            Path.Combine(repoRoot, "UniversalDeviceToolkit.WPF", "bin", "x64", configuration, "net10.0-windows", "win-x64"),
            Path.Combine(repoRoot, "UniversalDeviceToolkit.WPF", "bin", configuration, "net10.0-windows", "win-x64"),
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate) && ContainsMainAppStartupEntry(candidate))
                return candidate;
        }

        var runtimeRoot = Path.Combine(repoRoot, "UniversalDeviceToolkit.WPF", "bin");
        if (Directory.Exists(runtimeRoot))
        {
            var discovered = Directory
                .EnumerateDirectories(runtimeRoot, "win-x64", SearchOption.AllDirectories)
                .Where(ContainsMainAppStartupEntry)
                .OrderByDescending(Directory.GetLastWriteTimeUtc)
                .FirstOrDefault();
            if (discovered is not null)
                return discovered;
        }

        throw new DirectoryNotFoundException($"Runtime directory not found under WPF bin ({configuration}). Build the app first.");
    }

    private static bool ContainsMainAppStartupEntry(string runtimeDirectory) =>
        MainAppBaseNames.Any(name =>
            (File.Exists(Path.Combine(runtimeDirectory, $"{name}.dll")) &&
             File.Exists(Path.Combine(runtimeDirectory, $"{name}.runtimeconfig.json"))) ||
            File.Exists(Path.Combine(runtimeDirectory, $"{name}.exe")));

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
        Directory.CreateDirectory(directory);
    }

    private static void TryWaitForInputIdle(Process process, int milliseconds)
    {
        try { process.WaitForInputIdle(milliseconds); }
        catch { /* non-GUI host */ }
    }

    private static void TryCloseProcess(Process process)
    {
        if (process.HasExited)
            return;
        try
        {
            process.CloseMainWindow();
            if (process.WaitForExit(5000))
                return;
        }
        catch { /* ignore */ }

        try { process.Kill(entireProcessTree: true); }
        catch { /* ignore */ }
    }

    private static void TryKillExistingAppInstances()
    {
        foreach (var name in new[] { "Universal Device Toolkit", "Lenovo Legion Toolkit", "UniversalDeviceToolkit.NetworkProxy" })
        {
            foreach (var p in Process.GetProcessesByName(name))
            {
                try
                {
                    Console.WriteLine($"[ui-perf] Killing existing process {p.ProcessName} PID={p.Id}");
                    p.Kill(entireProcessTree: true);
                    p.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ui-perf] Could not kill {name}: {ex.Message}");
                }
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private sealed record SurfaceTarget(
        string Id,
        string[] AutomationIds,
        string[] Names,
        Func<AutomationElement, bool> Ready,
        bool isTab = false,
        bool isDialog = false,
        bool optional = false,
        string[]? parentNavIds = null)
    {
        public bool IsTab { get; } = isTab;
        public bool IsDialog { get; } = isDialog;
        public bool Optional { get; } = optional;
        public string[]? ParentNavIds { get; } = parentNavIds;
    }

    private sealed class SuiteResult
    {
        public DateTimeOffset StartedUtc { get; set; }
        public DateTimeOffset FinishedUtc { get; set; }
        public string MachineName { get; set; } = "";
        public string Configuration { get; set; } = "";
        public string RuntimeDirectory { get; set; } = "";
        public int ProcessId { get; set; }
        public long ColdStartMs { get; set; }
        public double TotalWallMs { get; set; }
        public ProcessSnapshot Baseline { get; set; } = new();
        public ProcessSnapshot Final { get; set; } = new();
        public List<SurfaceResult> Surfaces { get; set; } = [];
        public int FailedCount => Surfaces.Count(s => s.Error is not null || s.Rating == "failed");
    }

    private sealed class SurfaceResult
    {
        public string Id { get; set; } = "";
        public int Iterations { get; set; }
        public double ReadyMsMedian { get; set; }
        public double ReadyMsP95 { get; set; }
        public double SettleMsMedian { get; set; }
        public double WorkingSetDeltaMbMedian { get; set; }
        public double PrivateDeltaMbMedian { get; set; }
        public int HandleCountMedian { get; set; }
        public int UiaElementCountMedian { get; set; }
        public string Rating { get; set; } = "";
        public string? Error { get; set; }
    }

    private sealed class ProcessSnapshot
    {
        public double WorkingSetMb { get; set; }
        public double PrivateMb { get; set; }
        public double VirtualMb { get; set; }
        public int HandleCount { get; set; }
        public int ThreadCount { get; set; }
        public DateTimeOffset TimestampUtc { get; set; }
    }

    private sealed record Options(
        string RepoRoot,
        string OutputDirectory,
        string Configuration,
        int Iterations,
        bool KeepApp,
        bool KillExisting)
    {
        public static Options Parse(IReadOnlyList<string> args)
        {
            string? repoRoot = null;
            string? output = null;
            var configuration = "Release";
            var iterations = 2;
            var keepApp = false;
            var killExisting = true;

            for (var i = 0; i < args.Count; i++)
            {
                switch (args[i])
                {
                    case "--repo-root" when i + 1 < args.Count:
                        repoRoot = args[++i];
                        break;
                    case "--out" when i + 1 < args.Count:
                        output = args[++i];
                        break;
                    case "--configuration" or "-c" when i + 1 < args.Count:
                        configuration = args[++i];
                        break;
                    case "--iterations" when i + 1 < args.Count:
                        iterations = Math.Clamp(int.Parse(args[++i]), 1, 10);
                        break;
                    case "--keep-app":
                        keepApp = true;
                        break;
                    case "--no-kill":
                        killExisting = false;
                        break;
                }
            }

            repoRoot ??= FindRepoRoot();
            output ??= Path.Combine(repoRoot, "_ui_perf_out");
            return new Options(repoRoot, output, configuration, iterations, keepApp, killExisting);
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "UniversalDeviceToolkit.sln")) ||
                    Directory.Exists(Path.Combine(dir.FullName, "UniversalDeviceToolkit.WPF")))
                    return dir.FullName;
                dir = dir.Parent;
            }

            return Directory.GetCurrentDirectory();
        }
    }
}
