using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows.Automation;
using UniversalDeviceToolkit.Abstractions.Localization;
using UniversalDeviceToolkit.CLI.Lib;

namespace VisualRegression.Smoke;

internal static partial class Program
{
    private const string _appDataOverrideEnvironmentVariable = "UDT_APPDATA_OVERRIDE";

    private static void SetEnvVar(System.Collections.Specialized.StringDictionary environmentVariables, string environmentVariableName, string value) =>
        environmentVariables[environmentVariableName] = value;

    private const int _windowX = 80;
    private const int _windowY = 80;
    private const int _windowWidth = 1300;
    private const int _windowHeight = 850;
    private const int _minWindowWidth = 1000;
    private const int _minWindowHeight = 650;
    private static int _activeWindowWidth = _windowWidth;
    private static int _activeWindowHeight = _windowHeight;
    private static int _activeMinimumWidth = _minWindowWidth;
    private static int _activeMinimumHeight = _minWindowHeight;
    private static Viewport _activeViewport = Viewport.Default;
    private static readonly string[] _wpfAppBaseNames = ["Universal Device Toolkit", "Lenovo Legion Toolkit"];
    private static readonly string[] _avaloniaAppBaseNames = ["udt-gui"];

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private static readonly List<CaptureRecord> _captures = new();
    private static int _captureSequence;
    private static string _pipeName = string.Empty;
    private static int _processId;
    private static bool _assertDarkThemeSurface;
    private static string _appDataDirectory = string.Empty;
    private static bool _videoEnabled;
    private static string _host = "wpf";

    public static int Main(string[] args)
    {
        try
        {
            var options = SmokeOptions.Parse(args);
            return options.Viewports.Length > 1
                ? RunViewportBatch(args, options)
                : options.AllCultures
                ? RunCultureBatch(args, options)
                : RunSingle(args, options);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[visual-smoke] Failed before launch:");
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static int RunSingle(string[] args, SmokeOptions options)
    {
        Process? process = null;

        try
        {
            _captures.Clear();
            _captureSequence = 0;
            _videoEnabled = options.Video;
            _host = options.Host;
            _activeViewport = options.Viewports[0];
            _activeWindowWidth = _activeViewport.Width;
            _activeWindowHeight = _activeViewport.Height;
            _activeMinimumWidth = Math.Max(640, _activeWindowWidth - 16);
            _activeMinimumHeight = Math.Max(480, _activeWindowHeight - 16);
            _assertDarkThemeSurface = options.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
            var repoRoot = Path.GetFullPath(options.RepoRoot);
            var outputRoot = Path.GetFullPath(options.OutputDirectory);
            var currentDirectory = Path.Combine(outputRoot, "current");
            var sandboxRoot = Path.Combine(outputRoot, "sandbox");
            var appDataDirectory = Path.Combine(sandboxRoot, "appdata");
            var pluginsDirectory = Path.Combine(appDataDirectory, "plugins");
            _appDataDirectory = appDataDirectory;

            ResetDirectory(currentDirectory);
            ResetDirectory(sandboxRoot);
            Directory.CreateDirectory(currentDirectory);
            Directory.CreateDirectory(appDataDirectory);
            Directory.CreateDirectory(pluginsDirectory);

            PrepareSandboxSettings(repoRoot, appDataDirectory, options.Theme, options.ThemeStyle, options.Language, options.EnableAnimations);
            if (options.OsdOnly)
                PrepareOsdSandboxSettings(appDataDirectory);
            SeedPluginStoreCache(repoRoot, appDataDirectory);

            _pipeName = Constants.GetPipeName(appDataDirectory);

            var runtimeDirectory = ResolveRuntimeDirectory(repoRoot, options.Configuration, options.Host);
            process = StartApp(runtimeDirectory, appDataDirectory, options.Host);
            _processId = process.Id;

            Console.WriteLine($"[visual-smoke] Process: {_processId}");
            Console.WriteLine($"[visual-smoke] Runtime: {runtimeDirectory}");
            Console.WriteLine($"[visual-smoke] Output: {currentDirectory}");
            Console.WriteLine($"[visual-smoke] Sandbox appdata: {appDataDirectory}");
            Console.WriteLine($"[visual-smoke] IPC pipe: {_pipeName}");

            TryWaitForInputIdle(process, 10_000);
            var mainWindow = WaitForMainShellWindow(process.Id, TimeSpan.FromSeconds(90));
            NormalizeWindow(mainWindow);
            if (options.Host.Equals("wpf", StringComparison.OrdinalIgnoreCase)
                && !WaitForIpcReady(TimeSpan.FromSeconds(30)))
                Console.WriteLine("[visual-smoke] IPC did not become ready; continuing with UI Automation-only capture.");

            if (options.Host.Equals("avalonia", StringComparison.OrdinalIgnoreCase))
                return RunAvaloniaFlow(args, options, currentDirectory, outputRoot, appDataDirectory, ref process, mainWindow);

            if (options.OsdOnly)
            {
                CaptureOsdVisualAcceptance(currentDirectory, mainWindow);

                WriteManifest(currentDirectory, outputRoot, appDataDirectory);
                WriteResult(outputRoot, appDataDirectory, process, exitCode: null, error: null);

                if (options.KeepApp)
                {
                    Console.WriteLine("[visual-smoke] Leaving app running for inspection.");
                    process = null;
                    return 0;
                }

                TryCloseProcess(process);
                process = null;
                return 0;
            }

            if (options.ReadmeScreenshots)
            {
                WaitForAnimationsToComplete();
                CapturePage(currentDirectory, mainWindow, "dashboard");
                WriteManifest(currentDirectory, outputRoot, appDataDirectory);
                WriteResult(outputRoot, appDataDirectory, process, exitCode: null, error: null);
                TryCloseProcess(process);
                process = null;
                return 0;
            }

            CapturePage(currentDirectory, mainWindow, "main-window-ready");
            CaptureNavigationSidebarStates(currentDirectory, mainWindow);

            if (options.NavigationSidebarOnly)
            {
                WriteManifest(currentDirectory, outputRoot, appDataDirectory);
                WriteResult(outputRoot, appDataDirectory, process, exitCode: null, error: null);
                TryCloseProcess(process);
                process = null;
                return 0;
            }

            CapturePage(currentDirectory, mainWindow, "dashboard");
            CaptureInteractiveStates(currentDirectory, ResolveLiveWindow(mainWindow), "dashboard");
            CaptureWindowLifecycleStates(currentDirectory, mainWindow);
            CaptureResizeSequence(currentDirectory, mainWindow);

            if (options.SwitchTheme is { } switchTheme)
            {
                UpdateSandboxTheme(switchTheme);
                _assertDarkThemeSurface = switchTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
                NavigateAndCapture(currentDirectory, mainWindow, new PageTarget(
                    "settings",
                    ["SettingsNavItem"],
                    ["Settings"],
                    root => IsVisible(FindByAutomationId(root, "SettingsNavigationList"))
                            || FindVisibleClassContains(root, "SettingsPage")
                            || FindVisibleTextContains(root, "Settings")));
                SelectComboBoxItemByNames(WaitForNamedComboBox(ResolveLiveWindow(mainWindow), "Theme", TimeSpan.FromSeconds(10)), switchTheme);
                WaitForAnimationsToComplete();
                NavigateAndCapture(currentDirectory, mainWindow, new PageTarget(
                    $"dashboard-after-{switchTheme.ToLowerInvariant()}-switch",
                    ["_dashboardItem"],
                    ["Dashboard"],
                    root => root.Current.Name.Contains("Home", StringComparison.OrdinalIgnoreCase) || FindVisibleTextContains(root, "Power Mode")));

                WriteManifest(currentDirectory, outputRoot, appDataDirectory);
                WriteResult(outputRoot, appDataDirectory, process, exitCode: null, error: null);

                if (options.KeepApp)
                {
                    Console.WriteLine("[visual-smoke] Leaving app running for inspection.");
                    process = null;
                    return 0;
                }

                TryCloseProcess(process);
                process = null;
                return 0;
            }

            if (options.PluginOnly)
            {
                NavigateAndCapture(currentDirectory, mainWindow, new PageTarget(
                    "pluginExtensions",
                    ["PluginExtensionsNavItem"],
                    ["Plugin Extensions"],
                    IsPluginExtensionsPageReady));

                WriteManifest(currentDirectory, outputRoot, appDataDirectory);
                WriteResult(outputRoot, appDataDirectory, process, exitCode: null, error: null);

                if (options.KeepApp)
                {
                    Console.WriteLine("[visual-smoke] Leaving app running for inspection.");
                    process = null;
                    return 0;
                }

                TryCloseProcess(process);
                process = null;
                return 0;
            }

            if (options.SettingsOnly)
            {
                NavigateAndCapture(currentDirectory, mainWindow, new PageTarget(
                    "settings",
                    ["SettingsNavItem"],
                    ["Settings"],
                    root => IsVisible(FindByAutomationId(root, "SettingsNavigationList"))
                            || FindVisibleClassContains(root, "SettingsPage")
                            || FindVisibleTextContains(root, "Settings")));
                CaptureWpfSettingsStates(currentDirectory, mainWindow);

                WriteManifest(currentDirectory, outputRoot, appDataDirectory);
                WriteResult(outputRoot, appDataDirectory, process, exitCode: null, error: null);

                if (options.KeepApp)
                {
                    Console.WriteLine("[visual-smoke] Leaving app running for inspection.");
                    process = null;
                    return 0;
                }

                TryCloseProcess(process);
                process = null;
                return 0;
            }

            if (options.ExpectKeyboardNavigation)
            {
                NavigateAndCapture(currentDirectory, mainWindow, new PageTarget(
                    "keyboard",
                    ["_keyboardItem"],
                    ["Keyboard", "Keyboard Backlight"],
                    root => (root.Current.Name.Contains("Keyboard", StringComparison.OrdinalIgnoreCase)
                             && !root.Current.Name.Contains("Home", StringComparison.OrdinalIgnoreCase))
                            || FindVisibleTextContains(root, "No compatible keyboards")
                            || IsVisible(FindByAutomationId(root, "KeyboardBacklightPageRoot"))));
            }

            NavigateAndCapture(currentDirectory, mainWindow, new PageTarget(
                "automation",
                ["_automationItem"],
                ["Actions", "Automation"],
                root => root.Current.Name.Contains("Automation", StringComparison.OrdinalIgnoreCase)
                        || FindVisibleTextContains(root, "Quick Actions")
                        || FindVisibleTextContains(root, "automatic actions")));

            NavigateAndCapture(currentDirectory, mainWindow, new PageTarget(
                "macro",
                ["_macroItem"],
                ["Macro"],
                root => root.Current.Name.Contains("Macro", StringComparison.OrdinalIgnoreCase)
                        || FindVisibleTextContains(root, "M1")
                        || FindVisibleTextContains(root, "Record")));

            NavigateAndCapture(currentDirectory, mainWindow, new PageTarget(
                "windowsOptimization",
                ["WindowsOptimizationNavItem", "_windowsOptimizationItem"],
                ["System optimization", "Windows Optimization", "Windows optimization"],
                IsWindowsOptimizationPageReady));

            NavigateAndCapture(currentDirectory, mainWindow, new PageTarget(
                "pluginExtensions",
                ["PluginExtensionsNavItem"],
                ["Plugin Extensions"],
                IsPluginExtensionsPageReady));

            NavigateAndCapture(currentDirectory, mainWindow, new PageTarget(
                "settings",
                ["SettingsNavItem"],
                ["Settings"],
                root => IsVisible(FindByAutomationId(root, "SettingsNavigationList"))
                        || FindVisibleClassContains(root, "SettingsPage")
                        || FindVisibleTextContains(root, "Settings")));
            CaptureWpfSettingsStates(currentDirectory, mainWindow);

            NavigateAndCapture(currentDirectory, mainWindow, new PageTarget(
                "about",
                ["_aboutItem"],
                ["About"],
                root => FindVisibleClassContains(root, "AboutPage")
                        || FindVisibleTextContains(root, "Third-party libraries")
                        || FindVisibleTextContains(root, "Application Folders")));
            CapturePage(currentDirectory, ResolveLiveWindow(mainWindow), "about-min-window", _minWindowWidth, _minWindowHeight);

            NavigateAndWait(mainWindow, new PageTarget(
                "windowsOptimization",
                ["WindowsOptimizationNavItem", "_windowsOptimizationItem"],
                ["System optimization", "Windows Optimization", "Windows optimization"],
                IsWindowsOptimizationPageReady));
            ClickTabAndCapture(currentDirectory, mainWindow, "WindowsOptimizationOptimizationTabButton", "winopt-optimization-tab", IsWindowsOptimizationPageReady);
            ClickTabAndCapture(currentDirectory, mainWindow, "WindowsOptimizationCleanupTabButton", "winopt-cleanup-tab",
                root => IsVisible(FindByAutomationId(root, "WindowsOptimizationCategoryList")) && FindVisibleTextContains(root, "Cleanup"));
            ClickTabAndCapture(currentDirectory, mainWindow, "WindowsOptimizationDriverTabButton", "winopt-driver-tab",
                root => IsVisible(FindByAutomationId(root, "WindowsOptimizationDriverSearchButton")) || FindVisibleTextContains(root, "Driver Download"));

            WriteManifest(currentDirectory, outputRoot, appDataDirectory);
            WriteResult(outputRoot, appDataDirectory, process, exitCode: null, error: null);

            if (options.KeepApp)
            {
                Console.WriteLine("[visual-smoke] Leaving app running for inspection.");
                process = null;
                return 0;
            }

            TryCloseProcess(process);
            process = null;
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[visual-smoke] Failed:");
            Console.Error.WriteLine(ex);
            TryWriteFailureResult(args, process, ex);
            return 1;
        }
        finally
        {
            if (process is not null && !process.HasExited)
                TryCloseProcess(process);
        }
    }

    private static int RunCultureBatch(string[] args, SmokeOptions options)
    {
        var failures = new List<string>();
        foreach (var culture in LocalizationCatalog.SupportedCultures)
        {
            var viewportLabel = options.Viewports[0].Label;
            var cultureOutput = Path.Combine(options.OutputDirectory, options.Host, culture.Name, viewportLabel);
            Console.WriteLine($"[visual-smoke] Starting {options.Host} culture {culture.Name} -> {cultureOutput}");

            var result = RunSingle(
                args,
                options with
                {
                    AllCultures = false,
                    Language = culture.Name,
                    OutputDirectory = cultureOutput
                });

            if (result != 0)
                failures.Add(culture.Name);
        }

        var batchResult = new
        {
            host = options.Host,
            cultures = LocalizationCatalog.SupportedCultures.Select(culture => culture.Name).ToArray(),
            failures,
            succeeded = failures.Count == 0,
            completedAt = DateTimeOffset.Now
        };
        Directory.CreateDirectory(options.OutputDirectory);
        File.WriteAllText(
            Path.Combine(options.OutputDirectory, $"{options.Host}-{options.Viewports[0].Label}-batch-result.json"),
            JsonSerializer.Serialize(batchResult, _jsonOptions));

        return failures.Count == 0 ? 0 : 1;
    }

    private static int RunViewportBatch(string[] args, SmokeOptions options)
    {
        var failures = new List<string>();
        foreach (var viewport in options.Viewports)
        {
            var viewportOptions = options with { Viewports = [viewport] };
            var result = options.AllCultures
                ? RunCultureBatch(args, viewportOptions)
                : RunSingle(
                    args,
                    viewportOptions with
                    {
                        OutputDirectory = Path.Combine(
                            options.OutputDirectory,
                            options.Host,
                            LocalizationCatalog.NormalizeCulture(options.Language).Name,
                            viewport.Label)
                    });

            if (result != 0)
                failures.Add(viewport.Label);
        }

        Directory.CreateDirectory(options.OutputDirectory);
        var batchResult = new
        {
            host = options.Host,
            viewports = options.Viewports.Select(viewport => viewport.Label).ToArray(),
            failures,
            succeeded = failures.Count == 0,
            completedAt = DateTimeOffset.Now
        };
        File.WriteAllText(
            Path.Combine(options.OutputDirectory, $"{options.Host}-viewport-batch-result.json"),
            JsonSerializer.Serialize(batchResult, _jsonOptions));

        return failures.Count == 0 ? 0 : 1;
    }

    private static int RunAvaloniaFlow(
        string[] args,
        SmokeOptions options,
        string currentDirectory,
        string outputRoot,
        string appDataDirectory,
        ref Process? process,
        AutomationElement mainWindow)
    {
        CapturePage(currentDirectory, mainWindow, "dashboard");
        CaptureInteractiveStates(currentDirectory, ResolveLiveWindow(mainWindow), "dashboard");
        CaptureWindowLifecycleStates(currentDirectory, mainWindow);
        CaptureResizeSequence(currentDirectory, mainWindow);

        // The Avalonia shell exposes the WPF navigation routes even when their
        // platform adapter is unavailable. Visit each one to prove it renders a
        // capability result rather than leaving the content surface blank.
        foreach (var capabilityTarget in new[]
                 {
                     new PageTarget(
                         "keyboard",
                         ["AvaloniaKeyboardButton"],
                         ["Keyboard"],
                         root => FindVisibleClassContains(root, "HostCapabilityView")
                                 || FindVisibleTextContains(root, "Unavailable in this Avalonia host")),
                     new PageTarget(
                         "actions",
                         ["AvaloniaActionsButton"],
                         ["Actions"],
                         root => FindVisibleClassContains(root, "HostCapabilityView")
                                 || FindVisibleTextContains(root, "Unavailable in this Avalonia host")),
                     new PageTarget(
                         "macro",
                         ["AvaloniaMacroButton"],
                         ["Macro"],
                         root => FindVisibleClassContains(root, "HostCapabilityView")
                                 || FindVisibleTextContains(root, "Unavailable in this Avalonia host")),
                     new PageTarget(
                         "windows-optimization",
                         ["AvaloniaWindowsOptimizationButton"],
                         ["System optimization"],
                         root => FindVisibleClassContains(root, "HostCapabilityView")
                                 || FindVisibleTextContains(root, "Unavailable in this Avalonia host")),
                     new PageTarget(
                         "plugin-extensions",
                         ["AvaloniaPluginExtensionsButton"],
                         ["Plugin Extensions"],
                         root => FindVisibleClassContains(root, "HostCapabilityView")
                                 || FindVisibleTextContains(root, "Unavailable in this Avalonia host")),
                 })
        {
            NavigateAndCapture(currentDirectory, mainWindow, capabilityTarget);
        }

        NavigateAndCapture(currentDirectory, mainWindow, new PageTarget(
            "about",
            ["AvaloniaAboutButton"],
            ["About"],
            root => FindVisibleClassContains(root, "AboutPage")
                    || FindVisibleTextContains(root, "Third-party Libraries")
                    || FindVisibleTextContains(root, "Application Folders")));

        NavigateAndWait(mainWindow, new PageTarget(
            "settings",
            ["AvaloniaSettingsButton"],
            ["Settings"],
            root => FindVisibleClassContains(root, "SettingsPage")
                    || FindVisibleTextContains(root, "Appearance")
                    || FindVisibleTextContains(root, "Application Behavior")
                    || FindVisibleTextContains(root, "Settings for")));
        CapturePage(currentDirectory, ResolveLiveWindow(mainWindow), "settings");
        CaptureAvaloniaSettingsItems(currentDirectory, mainWindow);

        WriteManifest(currentDirectory, outputRoot, appDataDirectory);
        WriteResult(outputRoot, appDataDirectory, process, exitCode: null, error: null);

        if (options.KeepApp)
        {
            Console.WriteLine("[visual-smoke] Leaving Avalonia app running for inspection.");
            process = null;
            return 0;
        }

        if (process is not null)
            TryCloseProcess(process);
        process = null;
        return 0;
    }

    private static void CaptureAvaloniaSettingsItems(string currentDirectory, AutomationElement mainWindow)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var pass = 0; pass < 32; pass++)
        {
            var live = ResolveLiveWindow(mainWindow);
            var navigation = FindByAutomationId(live, "AvaloniaSettingsNavigationList") ?? live;
            var items = navigation.FindAll(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem))
                .Cast<AutomationElement>()
                .ToArray();

            var descriptor = items
                .Select(item => new NavigationItemDescriptor(
                    GetStableNavigationItemKey(item),
                    GetAutomationLabel(item)))
                .FirstOrDefault(item => !visited.Contains(item.Key));
            if (descriptor is null)
                break;

            var item = FindNavigationItem(navigation, descriptor);
            if (item is null)
                break;

            TryScrollIntoView(item);
            item = FindNavigationItem(navigation, descriptor);
            if (item is null || !IsVisible(item))
            {
                visited.Add(descriptor.Key);
                continue;
            }

            visited.Add(descriptor.Key);
            ActivateElement(item);
            WaitForAnimationsToComplete();
            var itemLabel = SanitizeFileNameSegment(descriptor.Label);
            var label = string.IsNullOrWhiteSpace(itemLabel)
                ? $"settings-item-{visited.Count}"
                : $"settings-{visited.Count}-{itemLabel}";
            CapturePage(currentDirectory, ResolveLiveWindow(mainWindow), label);
            CaptureInteractiveStates(currentDirectory, ResolveLiveWindow(mainWindow), label);
            CaptureFirstComboState(currentDirectory, mainWindow, label);
            CaptureFirstSafeToggleState(currentDirectory, mainWindow, label);
        }

        if (visited.Count == 0)
            throw new TimeoutException("Avalonia settings navigation did not expose any items.");
    }

    private static void CaptureWpfSettingsStates(string currentDirectory, AutomationElement mainWindow)
    {
        var live = ResolveLiveWindow(mainWindow);
        var navigation = WaitForAutomationId(live, "SettingsNavigationList", TimeSpan.FromSeconds(15));
        var hasItems = WaitUntil(
            () => navigation.FindAll(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem))
                .Cast<AutomationElement>()
                .Any(IsVisible),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromMilliseconds(250));

        if (!hasItems)
            throw new TimeoutException("Settings navigation did not expose any visible items.");

        var visited = new HashSet<string>(StringComparer.Ordinal);
        for (var pass = 0; pass < 32; pass++)
        {
            live = ResolveLiveWindow(mainWindow);
            navigation = FindByAutomationId(live, "SettingsNavigationList") ?? navigation;
            var navItems = navigation.FindAll(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem))
                .Cast<AutomationElement>()
                .ToArray();

            var descriptor = navItems
                .Select(item => new NavigationItemDescriptor(
                    GetStableNavigationItemKey(item),
                    GetAutomationLabel(item)))
                .FirstOrDefault(item => !visited.Contains(item.Key));
            if (descriptor is null)
                break;

            var item = FindNavigationItem(navigation, descriptor);
            if (item is null)
                break;

            TryScrollIntoView(item);
            item = FindNavigationItem(navigation, descriptor);
            if (item is null || !IsVisible(item))
            {
                visited.Add(descriptor.Key);
                continue;
            }

            visited.Add(descriptor.Key);
            ActivateElement(item);
            WaitUntil(
                () => IsVisible(FindByAutomationId(ResolveLiveWindow(mainWindow), "SettingsContentControl")),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(200));
            WaitForAnimationsToComplete();

            var label = $"settings-{visited.Count}-{SanitizeFileNameSegment(descriptor.Label)}";
            CapturePage(currentDirectory, ResolveLiveWindow(mainWindow), label);
            CaptureInteractiveStates(currentDirectory, ResolveLiveWindow(mainWindow), label);
            CaptureFirstComboState(currentDirectory, mainWindow, label);
            CaptureFirstSafeToggleState(currentDirectory, mainWindow, label);
        }

        if (visited.Count == 0)
            throw new TimeoutException("WPF settings navigation did not expose any items.");
    }

    private static string GetStableNavigationItemKey(AutomationElement item)
    {
        var automationId = item.Current.AutomationId;
        return string.IsNullOrWhiteSpace(automationId)
            ? $"name:{GetAutomationLabel(item)}"
            : $"id:{automationId}";
    }

    private static AutomationElement? FindNavigationItem(
        AutomationElement navigation,
        NavigationItemDescriptor descriptor)
    {
        var items = navigation.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem))
            .Cast<AutomationElement>();

        return items.FirstOrDefault(item => string.Equals(
            GetStableNavigationItemKey(item), descriptor.Key, StringComparison.Ordinal));
    }

    private static void TryScrollIntoView(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(ScrollItemPattern.Pattern, out var pattern))
                ((ScrollItemPattern)pattern).ScrollIntoView();
        }
        catch (ElementNotAvailableException)
        {
        }
        catch (InvalidOperationException)
        {
        }

        Thread.Sleep(150);
    }

    private static void CaptureFirstComboState(string currentDirectory, AutomationElement mainWindow, string label)
    {
        var live = ResolveLiveWindow(mainWindow);
        var combo = live.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox))
            .Cast<AutomationElement>()
            .FirstOrDefault(IsVisible);
        if (combo is null || !combo.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var pattern))
            return;

        var expander = (ExpandCollapsePattern)pattern;
        if (expander.Current.ExpandCollapseState == ExpandCollapseState.Expanded)
            return;

        expander.Expand();
        Thread.Sleep(250);
        CapturePage(currentDirectory, ResolveLiveWindow(mainWindow), $"{label}-combo-expanded");

        if (combo.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var collapsePattern))
            ((ExpandCollapsePattern)collapsePattern).Collapse();
        Thread.Sleep(150);
    }

    private static void CaptureFirstSafeToggleState(string currentDirectory, AutomationElement mainWindow, string label)
    {
        var live = ResolveLiveWindow(mainWindow);
        var toggle = live.FindAll(
                TreeScope.Descendants,
                new OrCondition(
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.CheckBox),
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)))
            .Cast<AutomationElement>()
            .Where(IsVisible)
            .FirstOrDefault(element => IsSafeToggle(element));
        if (toggle is null || !toggle.TryGetCurrentPattern(TogglePattern.Pattern, out var pattern))
            return;

        var togglePattern = (TogglePattern)pattern;
        var initialState = togglePattern.Current.ToggleState;
        if (initialState == ToggleState.Indeterminate)
            return;

        togglePattern.Toggle();
        Thread.Sleep(300);
        CapturePage(currentDirectory, ResolveLiveWindow(mainWindow), $"{label}-toggle-changed");

        if (toggle.TryGetCurrentPattern(TogglePattern.Pattern, out var restorePattern)
            && ((TogglePattern)restorePattern).Current.ToggleState != initialState)
        {
            ((TogglePattern)restorePattern).Toggle();
            Thread.Sleep(250);
        }
    }

    private static bool IsSafeToggle(AutomationElement element)
    {
        var id = element.Current.AutomationId ?? string.Empty;
        return !id.Contains("GodMode", StringComparison.OrdinalIgnoreCase)
               && !id.Contains("ForceSoftware", StringComparison.OrdinalIgnoreCase)
               && !id.Contains("OnBattery", StringComparison.OrdinalIgnoreCase)
               && !id.Contains("Disable", StringComparison.OrdinalIgnoreCase);
    }

    private static void CaptureNavigationSidebarStates(string currentDirectory, AutomationElement mainWindow)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        BringToForeground(mainWindow);
        WaitForAnimationsToComplete();

        var initialWidth = MeasureNavigationPaneWidth(mainWindow);
        Console.WriteLine($"[visual-smoke] Navigation pane width (initial): {initialWidth:F1}px");

        if (initialWidth >= 150)
            CapturePage(currentDirectory, mainWindow, "nav-sidebar-expanded");
        else
            CapturePage(currentDirectory, mainWindow, "nav-sidebar-compact");

        var toggle = WaitForAutomationId(mainWindow, "NavigationPaneToggle", TimeSpan.FromSeconds(10));
        ActivateElement(toggle);
        CapturePage(currentDirectory, ResolveLiveWindow(mainWindow), "nav-sidebar-transition-start", _activeWindowWidth, _activeWindowHeight, waitForAnimations: false);
        Thread.Sleep(450);
        CapturePage(currentDirectory, ResolveLiveWindow(mainWindow), "nav-sidebar-transition-mid", _activeWindowWidth, _activeWindowHeight, waitForAnimations: false);
        WaitForAnimationsToComplete();

        mainWindow = ResolveLiveWindow(mainWindow);
        var afterToggleWidth = WaitForNavigationPaneWidthChange(mainWindow, initialWidth, TimeSpan.FromSeconds(5));
        Console.WriteLine($"[visual-smoke] Navigation pane width (after toggle): {afterToggleWidth:F1}px");

        if (afterToggleWidth >= 150)
            CapturePage(currentDirectory, mainWindow, "nav-sidebar-expanded-after-toggle");
        else
            CapturePage(currentDirectory, mainWindow, "nav-sidebar-compact-after-toggle");

        if (Math.Abs(afterToggleWidth - initialWidth) < 80)
            throw new InvalidOperationException(
                $"Navigation pane toggle did not change width. Before={initialWidth:F1}px, after={afterToggleWidth:F1}px.");

        toggle = WaitForAutomationId(mainWindow, "NavigationPaneToggle", TimeSpan.FromSeconds(10));
        ActivateElement(toggle);
        CapturePage(currentDirectory, ResolveLiveWindow(mainWindow), "nav-sidebar-restore-transition-start", _activeWindowWidth, _activeWindowHeight, waitForAnimations: false);
        WaitForAnimationsToComplete();

        mainWindow = ResolveLiveWindow(mainWindow);
        var restoredWidth = WaitForNavigationPaneWidthChange(mainWindow, afterToggleWidth, TimeSpan.FromSeconds(5));
        Console.WriteLine($"[visual-smoke] Navigation pane width (restored): {restoredWidth:F1}px");
        CapturePage(currentDirectory, mainWindow, "nav-sidebar-toggle-restored");

        if (Math.Abs(restoredWidth - initialWidth) > 12)
            throw new InvalidOperationException(
                $"Navigation pane toggle did not restore width. Initial={initialWidth:F1}px, restored={restoredWidth:F1}px.");
    }

    private static void CaptureWindowLifecycleStates(string currentDirectory, AutomationElement mainWindow)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        if (!TryGetNativeWindowHandle(mainWindow, out var handle))
            throw new InvalidOperationException("Window handle unavailable for lifecycle audit.");

        var hwnd = (IntPtr)handle;
        var states = new List<object>();

        ShowWindow(hwnd, 6); // SW_MINIMIZE
        Thread.Sleep(450);
        states.Add(new { state = "minimized", iconic = IsIconic(hwnd), zoomed = IsZoomed(hwnd) });
        CaptureDesktopLifecycleState(currentDirectory, "window-minimized-desktop");

        ShowWindow(hwnd, 9); // SW_RESTORE
        Thread.Sleep(350);
        CapturePage(
            currentDirectory,
            ResolveLiveWindow(mainWindow),
            "window-restored-from-minimized",
            _activeWindowWidth,
            _activeWindowHeight,
            waitForAnimations: true,
            normalizeWindow: false);
        states.Add(new { state = "restored", iconic = IsIconic(hwnd), zoomed = IsZoomed(hwnd) });

        ShowWindow(hwnd, 3); // SW_MAXIMIZE
        Thread.Sleep(450);
        CapturePage(
            currentDirectory,
            ResolveLiveWindow(mainWindow),
            "window-maximized",
            _activeWindowWidth,
            _activeWindowHeight,
            waitForAnimations: true,
            normalizeWindow: false);
        states.Add(new { state = "maximized", iconic = IsIconic(hwnd), zoomed = IsZoomed(hwnd) });

        ShowWindow(hwnd, 9); // SW_RESTORE
        NormalizeWindow(ResolveLiveWindow(mainWindow));
        states.Add(new { state = "restored-after-maximize", iconic = IsIconic(hwnd), zoomed = IsZoomed(hwnd) });

        File.WriteAllText(
            Path.Combine(currentDirectory, "window-lifecycle.json"),
            JsonSerializer.Serialize(new
            {
                generatedAt = DateTimeOffset.Now,
                viewport = _activeViewport.Label,
                states
            }, _jsonOptions));
    }

    private static void CaptureDesktopLifecycleState(string currentDirectory, string label)
    {
        var screens = System.Windows.Forms.Screen.AllScreens;
        if (screens.Length == 0)
            throw new InvalidOperationException("Interactive desktop has no screens for lifecycle capture.");

        var left = screens.Min(screen => screen.Bounds.Left);
        var top = screens.Min(screen => screen.Bounds.Top);
        var right = screens.Max(screen => screen.Bounds.Right);
        var bottom = screens.Max(screen => screen.Bounds.Bottom);
        var bounds = new Rectangle(left, top, right - left, bottom - top);

        var sequence = ++_captureSequence;
        var fileStem = $"{sequence:000}-{SanitizeFileNameSegment(label)}";
        var fileName = ToArtifactPath(Path.Combine("window", SanitizeFileNameSegment(label), $"{fileStem}.png"));
        var outputPath = Path.Combine(currentDirectory, fileName);
        var videoFileName = ToArtifactPath(Path.ChangeExtension(fileName, ".mp4"));

        using var recorder = _videoEnabled
            ? WindowVideoRecorder.Start(bounds, Path.Combine(currentDirectory, videoFileName))
            : null;
        CaptureDesktopFromScreen(bounds, outputPath);
        Thread.Sleep(350);

        AssertCaptureDimensions(outputPath, label);
        AssertNotBlankCapture(outputPath, label);

        var snapshotFileName = ToArtifactPath(Path.ChangeExtension(fileName, ".json"));
        var ocrFileName = ToArtifactPath(Path.ChangeExtension(fileName, ".ocr.json"));
        File.WriteAllText(
            Path.Combine(currentDirectory, snapshotFileName),
            JsonSerializer.Serialize(new
            {
                label,
                capturedAt = DateTimeOffset.Now,
                desktop = true,
                bounds = new { bounds.Left, bounds.Top, bounds.Width, bounds.Height }
            }, _jsonOptions));
        File.WriteAllText(
            Path.Combine(currentDirectory, ocrFileName),
            JsonSerializer.Serialize(new
            {
                label,
                source = "desktop",
                items = Array.Empty<object>()
            }, _jsonOptions));

        _captures.Add(new CaptureRecord(
            _captures.Count + 1,
            label,
            fileName,
            snapshotFileName,
            _videoEnabled ? videoFileName : null,
            ocrFileName,
            DateTimeOffset.Now));
        WriteCaptureStateResult(currentDirectory, _captures[^1]);
        Console.WriteLine($"[visual-smoke] Captured desktop lifecycle state {label}: {outputPath}");
    }

    private static void CaptureDesktopFromScreen(Rectangle bounds, string outputPath)
    {
        using var bitmap = new Bitmap(Math.Max(1, bounds.Width), Math.Max(1, bounds.Height));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bitmap.Size);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
    }

    private static void CaptureResizeSequence(string currentDirectory, AutomationElement mainWindow)
    {
        var sizes = new[]
        {
            (_activeWindowWidth, _activeWindowHeight),
            (Math.Max(640, Math.Min(_activeWindowWidth, 900)), Math.Max(480, Math.Min(_activeWindowHeight, 700))),
            (_activeWindowWidth, _activeWindowHeight)
        }.Distinct().ToArray();

        for (var index = 0; index < sizes.Length; index++)
        {
            var (width, height) = sizes[index];
            CapturePage(
                currentDirectory,
                ResolveLiveWindow(mainWindow),
                $"dashboard-resize-{width}x{height}",
                width,
                height);
        }
    }

    private static double MeasureNavigationPaneWidth(AutomationElement mainWindow)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        var windowRect = mainWindow.Current.BoundingRectangle;
        var rootFrame = FindByAutomationId(mainWindow, "MainRootFrame");
        if (rootFrame is not null && IsVisible(rootFrame))
        {
            var frameRect = rootFrame.Current.BoundingRectangle;
            var leftNavigationGap = frameRect.Left - windowRect.Left - 30;
            var rightNavigationGap = windowRect.Right - frameRect.Right - 30;
            return Math.Max(0, Math.Max(leftNavigationGap, rightNavigationGap));
        }

        var navStore = FindByAutomationId(mainWindow, "MainNavigationStore");
        if (navStore is not null && IsVisible(navStore))
            return navStore.Current.BoundingRectangle.Width;

        throw new InvalidOperationException("Could not measure navigation pane width from automation tree.");
    }

    private static double WaitForNavigationPaneWidthChange(AutomationElement mainWindow, double previousWidth, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var width = MeasureNavigationPaneWidth(mainWindow);
        while (DateTime.UtcNow < deadline)
        {
            Thread.Sleep(100);
            width = MeasureNavigationPaneWidth(ResolveLiveWindow(mainWindow));
            if (Math.Abs(width - previousWidth) >= 80)
                return width;
        }
        return width;
    }

    private static void NavigateAndCapture(string currentDirectory, AutomationElement mainWindow, PageTarget target)
    {
        NavigateAndWait(mainWindow, target);
        var liveWindow = ResolveLiveWindow(mainWindow);
        CapturePage(currentDirectory, liveWindow, target.Label);
        CaptureInteractiveStates(currentDirectory, liveWindow, target.Label);
    }

    private static void CaptureInteractiveStates(
        string currentDirectory,
        AutomationElement mainWindow,
        string pageLabel)
    {
        var interactive = EnumerateDescendants(mainWindow, 800)
            .Where(IsVisible)
            .Where(element =>
            {
                try
                {
                    var type = element.Current.ControlType;
                    if (IsFrameworkTemplatePart(element))
                        return false;
                    return type == ControlType.Button
                           || type == ControlType.CheckBox
                           || type == ControlType.ComboBox
                           || type == ControlType.RadioButton
                           || type == ControlType.TabItem;
                }
                catch (ElementNotAvailableException)
                {
                    return false;
                }
            })
            .FirstOrDefault(element => IsSafeToggle(element) || !IsDangerousAction(element));

        if (interactive is null)
            return;

        try
        {
            var rectangle = interactive.Current.BoundingRectangle;
            if (rectangle.Width > 1 && rectangle.Height > 1)
            {
                SetCursorPos(
                    (int)Math.Round(rectangle.Left + rectangle.Width / 2),
                    (int)Math.Round(rectangle.Top + rectangle.Height / 2));
                Thread.Sleep(180);
                CapturePage(
                    currentDirectory,
                    ResolveLiveWindow(mainWindow),
                    $"{pageLabel}-hover",
                    _activeWindowWidth,
                    _activeWindowHeight,
                    waitForAnimations: false);
            }

            try
            {
                interactive.SetFocus();
                Thread.Sleep(180);
                CapturePage(
                    currentDirectory,
                    ResolveLiveWindow(mainWindow),
                    $"{pageLabel}-focus",
                    _activeWindowWidth,
                    _activeWindowHeight,
                    waitForAnimations: false);
            }
            catch (ElementNotAvailableException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
        finally
        {
            SetCursorPos(_windowX, _windowY);
        }
    }

    private static bool IsDangerousAction(AutomationElement element)
    {
        try
        {
            var id = element.Current.AutomationId ?? string.Empty;
            var name = element.Current.Name ?? string.Empty;
            return id.Contains("Apply", StringComparison.OrdinalIgnoreCase)
                   || id.Contains("Delete", StringComparison.OrdinalIgnoreCase)
                   || id.Contains("Remove", StringComparison.OrdinalIgnoreCase)
                   || id.Contains("Force", StringComparison.OrdinalIgnoreCase)
                   || name.Contains("Apply", StringComparison.OrdinalIgnoreCase)
                   || name.Contains("Delete", StringComparison.OrdinalIgnoreCase)
                   || name.Contains("Remove", StringComparison.OrdinalIgnoreCase);
        }
        catch (ElementNotAvailableException)
        {
            return true;
        }
    }

    private static void NavigateAndWait(AutomationElement mainWindow, PageTarget target)
    {
        Console.WriteLine($"[visual-smoke] Navigating to {target.Label}");
        mainWindow = ResolveLiveWindow(mainWindow);
        BringToForeground(mainWindow);

        var arrived = false;
        for (var attempt = 1; attempt <= 5 && !arrived; attempt++)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            var nav = FindNavigationElement(mainWindow, target);
            if (nav is not null)
            {
                Console.WriteLine($"[visual-smoke] Activating {DescribeElement(nav)}");
                ActivateNavigationElement(nav);
            }
            else
            {
                Console.WriteLine($"[visual-smoke] Navigation target {target.Label} not found, pressing Ctrl+Tab.");
                PressCtrlTab();
            }

            arrived = WaitUntil(
                () =>
                {
                    var live = ResolveLiveWindow(mainWindow);
                    return target.Ready(live);
                },
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMilliseconds(250));

            if (!arrived)
                Thread.Sleep(500);
        }

        if (!arrived)
        {
            DumpAutomationSnapshot(ResolveLiveWindow(mainWindow), 220);
            throw new TimeoutException($"Timed out waiting for page '{target.Label}'.");
        }

        WaitForAnimationsToComplete();
        NormalizeWindow(mainWindow);
    }

    private static void ClickTabAndCapture(
        string currentDirectory,
        AutomationElement mainWindow,
        string automationId,
        string label,
        Func<AutomationElement, bool> ready)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        var tab = WaitForAutomationId(mainWindow, automationId, TimeSpan.FromSeconds(10));
        ActivateElement(tab);

        var arrived = WaitUntil(
            () => ready(ResolveLiveWindow(mainWindow)),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(250));

        if (!arrived)
            throw new TimeoutException($"Timed out waiting for tab '{automationId}'.");

        WaitForAnimationsToComplete();
        CapturePage(currentDirectory, ResolveLiveWindow(mainWindow), label);
    }

    private static void CaptureOsdVisualAcceptance(string currentDirectory, AutomationElement mainWindow)
    {
        Console.WriteLine("[visual-smoke] Waiting for OSD overlay and sensor refresh...");
        Thread.Sleep(TimeSpan.FromSeconds(4));

        var osdWindow = WaitForOsdWindow(_processId, "OsdPanelWindow", TimeSpan.FromSeconds(45));
        CaptureOverlayWindow(currentDirectory, osdWindow, "osd-panel-overlay");
        AssertOsdOverlayCapture(Path.Combine(currentDirectory, _captures[^1].FileName));

        CapturePage(currentDirectory, mainWindow, "dashboard-with-osd");

        NavigateAndWait(mainWindow, new PageTarget(
            "settings",
            [],
            ["Settings"],
            root => FindVisibleTextContains(root, "Settings") && FindVisibleTextContains(root, "Theme style")));

        mainWindow = ResolveLiveWindow(mainWindow);
        var applicationTab = FindByName(mainWindow, "Application")
                               ?? throw new InvalidOperationException("Settings Application tab not found.");
        ActivateElement(applicationTab);

        var applicationReady = WaitUntil(
            () =>
            {
                var live = ResolveLiveWindow(mainWindow);
                return FindVisibleTextContains(live, "Hardware Sensors")
                       && FindVisibleTextContains(live, "Enable OSD");
            },
            TimeSpan.FromSeconds(15),
            TimeSpan.FromMilliseconds(250));

        if (!applicationReady)
            throw new TimeoutException("Timed out waiting for Application settings with OSD controls.");

        WaitForAnimationsToComplete();
        CapturePage(currentDirectory, ResolveLiveWindow(mainWindow), "settings-application-osd");
    }

    private static AutomationElement WaitForOsdWindow(int processId, string title, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var window = TryFindOsdWindow(processId, title);
            if (window is not null)
                return window;

            Thread.Sleep(250);
        }

        throw new TimeoutException($"Timed out waiting for OSD window '{title}'.");
    }

    private static AutomationElement? TryFindOsdWindow(int processId, string title)
    {
        var condition = new AndCondition(
            new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window),
            new PropertyCondition(AutomationElement.NameProperty, title));

        try
        {
            var window = AutomationElement.RootElement.FindFirst(TreeScope.Children, condition);
            return window is not null && IsVisible(window) ? window : null;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    private static void CaptureOverlayWindow(string currentDirectory, AutomationElement window, string label)
    {
        if (!TryGetNativeWindowHandle(window, out var windowHandle))
            throw new InvalidOperationException($"Window handle unavailable for overlay '{label}'.");

        var fileName = $"{++_captureSequence:000}-{SanitizeFileNameSegment(label)}.png";
        var outputPath = Path.Combine(currentDirectory, fileName);

        using var recorder = _videoEnabled
            ? WindowVideoRecorder.Start(windowHandle, Path.Combine(currentDirectory, Path.ChangeExtension(fileName, ".mp4")))
            : null;

        CaptureWindowFromScreen(windowHandle, outputPath);
        if (recorder is not null)
            Thread.Sleep(350);

        using (var bitmap = new Bitmap(outputPath))
        {
            if (bitmap.Width < 80 || bitmap.Height < 80)
            {
                throw new InvalidOperationException(
                    $"OSD overlay screenshot '{label}' is too small ({bitmap.Width}x{bitmap.Height}).");
            }

            Console.WriteLine($"[visual-smoke] OSD overlay size for {label}: {bitmap.Width}x{bitmap.Height}");
        }

        var snapshotPath = Path.Combine(currentDirectory, Path.ChangeExtension(fileName, ".json"));
        var snapshot = BuildSnapshot(label, window);
        File.WriteAllText(snapshotPath, JsonSerializer.Serialize(snapshot, _jsonOptions));
        var ocrFileName = Path.ChangeExtension(fileName, ".ocr.json");
        File.WriteAllText(
            Path.Combine(currentDirectory, ocrFileName),
            JsonSerializer.Serialize(BuildTextAudit(label, window), _jsonOptions));

        _captures.Add(new CaptureRecord(
            _captures.Count + 1,
            label,
            fileName,
            Path.GetFileName(snapshotPath),
            recorder?.FileName is { } videoPath ? Path.GetFileName(videoPath) : null,
            Path.GetFileName(ocrFileName),
            DateTimeOffset.Now));
        Console.WriteLine($"[visual-smoke] Captured {label}: {outputPath}");
    }

    private static void AssertOsdOverlayCapture(string outputPath)
    {
        using var bitmap = new Bitmap(outputPath);
        var sample = SampleRegion(bitmap, 0, 0, bitmap.Width, bitmap.Height);
        if (sample.AverageLuminance < 8)
        {
            throw new InvalidOperationException(
                $"OSD overlay appears blank or fully transparent. Average luminance {sample.AverageLuminance:F1}. Screenshot: {outputPath}");
        }

        Console.WriteLine($"[visual-smoke] OSD overlay luminance check passed ({sample.AverageLuminance:F1}).");
    }

    private static void CapturePage(string currentDirectory, AutomationElement mainWindow, string label)
        => CapturePage(currentDirectory, mainWindow, label, _activeWindowWidth, _activeWindowHeight);

    private static void CapturePage(string currentDirectory, AutomationElement mainWindow, string label, int width, int height)
        => CapturePage(currentDirectory, mainWindow, label, width, height, waitForAnimations: true, normalizeWindow: true);

    private static void CapturePage(
        string currentDirectory,
        AutomationElement mainWindow,
        string label,
        int width,
        int height,
        bool waitForAnimations,
        bool normalizeWindow = true)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        if (normalizeWindow)
            NormalizeWindow(mainWindow, width, height);
        else
            BringToForeground(mainWindow);

        if (!TryGetNativeWindowHandle(mainWindow, out var windowHandle))
            throw new InvalidOperationException($"Window handle unavailable for '{label}'.");

        var sequence = ++_captureSequence;
        var fileStem = $"{sequence:000}-{SanitizeFileNameSegment(label)}";
        var capturePath = ClassifyCaptureLabel(label);
        var relativeDirectory = Path.Combine(capturePath.Page, capturePath.State);
        var fileName = ToArtifactPath(Path.Combine(relativeDirectory, $"{fileStem}.png"));
        var outputPath = Path.Combine(currentDirectory, fileName);
        var videoFileName = ToArtifactPath(Path.ChangeExtension(fileName, ".mp4"));
        using var recorder = _videoEnabled
            ? WindowVideoRecorder.Start(windowHandle, Path.Combine(currentDirectory, videoFileName))
            : null;

        if (waitForAnimations)
            WaitForAnimationsToComplete();

        CaptureWindowFromScreen(windowHandle, outputPath);

        AssertCaptureDimensions(outputPath, label);
        AssertNotBlankCapture(outputPath, label);
        AssertThemeSurface(outputPath, label);

        ValidateAutomationLayout(label, mainWindow);

        var snapshotFileName = ToArtifactPath(Path.ChangeExtension(fileName, ".json"));
        var snapshotPath = Path.Combine(currentDirectory, snapshotFileName);
        var snapshot = BuildSnapshot(label, mainWindow);
        File.WriteAllText(snapshotPath, JsonSerializer.Serialize(snapshot, _jsonOptions));

        var ocrFileName = ToArtifactPath(Path.ChangeExtension(fileName, ".ocr.json"));
        File.WriteAllText(
            Path.Combine(currentDirectory, ocrFileName),
            JsonSerializer.Serialize(BuildTextAudit(label, mainWindow), _jsonOptions));

        _captures.Add(new CaptureRecord(
            _captures.Count + 1,
            label,
            fileName,
            snapshotFileName,
            recorder is null ? null : videoFileName,
            ocrFileName,
            DateTimeOffset.Now));
        WriteCaptureStateResult(currentDirectory, _captures[^1]);
        Console.WriteLine($"[visual-smoke] Captured {label}: {outputPath}");
    }

    private static CapturePath ClassifyCaptureLabel(string label)
    {
        var normalized = SanitizeFileNameSegment(label);
        if (normalized.StartsWith("settings-", StringComparison.Ordinal))
            return new CapturePath("settings", normalized);
        if (normalized.StartsWith("nav-sidebar", StringComparison.Ordinal))
            return new CapturePath("navigation", normalized);
        if (normalized.StartsWith("window-", StringComparison.Ordinal))
            return new CapturePath("window", normalized);
        if (normalized.StartsWith("dashboard", StringComparison.Ordinal))
            return new CapturePath("dashboard", normalized);

        var separator = normalized.IndexOf('-');
        return separator > 0
            ? new CapturePath(normalized[..separator], normalized)
            : new CapturePath(normalized, "ready");
    }

    private static string ToArtifactPath(string path) => path.Replace('\\', '/');

    private static object BuildTextAudit(string label, AutomationElement root)
    {
        var textItems = EnumerateDescendants(root, 1000)
            .Select(SafeDescribeElement)
            .Where(element => element is { IsOffscreen: false } && !string.IsNullOrWhiteSpace(element.Name))
            .Select(element => new
            {
                text = element!.Name!,
                fullText = string.IsNullOrWhiteSpace(element.HelpText) ? element.Name! : element.HelpText,
                tooltip = element.HelpText,
                type = element.Type,
                automationId = element.AutomationId,
                x = element.X,
                y = element.Y,
                width = element.Width,
                height = element.Height
            })
            .DistinctBy(item => $"{item.type}|{item.automationId}|{item.text}|{item.x}|{item.y}")
            .ToArray();

        return new
        {
            label,
            source = "uia",
            generatedAt = DateTimeOffset.Now,
            items = textItems,
            truncatedCandidates = textItems
                .Where(item => item.text.EndsWith("...", StringComparison.Ordinal)
                               || item.text.EndsWith("…", StringComparison.Ordinal))
                .ToArray()
        };
    }

    private static void AssertCaptureDimensions(string outputPath, string label)
    {
        using var bitmap = new Bitmap(outputPath);
        if (bitmap.Width < _activeMinimumWidth || bitmap.Height < _activeMinimumHeight)
        {
            throw new InvalidOperationException(
                $"Screenshot '{label}' is too small ({bitmap.Width}x{bitmap.Height}). Expected at least {_activeMinimumWidth}x{_activeMinimumHeight}. " +
                $"Ensure IPC capture succeeded and the main window was normalized to {_activeWindowWidth}x{_activeWindowHeight}.");
        }

        Console.WriteLine($"[visual-smoke] Capture size for {label}: {bitmap.Width}x{bitmap.Height} (window target {_activeWindowWidth}x{_activeWindowHeight})");
    }

    private static void AssertNotBlankCapture(string outputPath, string label)
    {
        using var bitmap = new Bitmap(outputPath);
        var sample = SampleRegion(bitmap, 0, 0, bitmap.Width, bitmap.Height);
        if (sample.AverageLuminance < 2.0 || sample.AverageLuminance > 253.5)
        {
            throw new InvalidOperationException(
                $"Screenshot '{label}' appears blank or fully solid. Average luminance {sample.AverageLuminance:F1}: {outputPath}");
        }
    }

    private static void ValidateAutomationLayout(string label, AutomationElement root)
    {
        var rootRect = root.Current.BoundingRectangle;
        if (rootRect.Width <= 1 || rootRect.Height <= 1)
            throw new InvalidOperationException($"UI Automation root has invalid bounds for '{label}'.");

        var interactiveTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Button",
            "CheckBox",
            "ComboBox",
            "ListItem",
            "RadioButton",
            "TabItem",
        };

        var visibleInteractive = new List<(AutomationElement Element, ElementSnapshot Snapshot)>();
        var visibleText = new List<ElementSnapshot>();
        foreach (var element in EnumerateDescendants(root, 600))
        {
            ElementSnapshot? snapshot;
            try
            {
                snapshot = SafeDescribeElement(element);
            }
            catch (ElementNotAvailableException)
            {
                continue;
            }

            if (snapshot is null || snapshot.IsOffscreen || IsFrameworkTemplatePart(snapshot)
                || !interactiveTypes.Contains(snapshot.Type)
                || string.Equals(snapshot.Name, "Close notification", StringComparison.Ordinal)
                || snapshot.X is null || snapshot.Y is null || snapshot.Width is null || snapshot.Height is null)
                continue;

            if (string.IsNullOrWhiteSpace(snapshot.Name))
                throw new InvalidOperationException(
                    $"Interactive UIA element '{snapshot.AutomationId ?? snapshot.Type}' has no Automation name on '{label}'.");

            if (IsEllipsized(snapshot.Name) && string.IsNullOrWhiteSpace(snapshot.HelpText))
                throw new InvalidOperationException(
                    $"Ellipsized UIA element '{snapshot.AutomationId ?? snapshot.Name}' has no full-text Tooltip on '{label}'.");

            if (snapshot.Width <= 1 || snapshot.Height <= 1)
                throw new InvalidOperationException(
                    $"Interactive UIA element '{snapshot.AutomationId ?? snapshot.Name}' has empty bounds on '{label}'.");

            // UI Automation reports WPF bounds in device-independent units while
            // PrintWindow/screen capture uses physical pixels. Allow the larger
            // DPI conversion delta, but still fail on grossly detached controls.
            var tolerance = Math.Max(32, Math.Max(rootRect.Width, rootRect.Height) * 0.20);
            if (snapshot.X < rootRect.Left - tolerance
                || snapshot.Y < rootRect.Top - tolerance
                || snapshot.X + snapshot.Width > rootRect.Right + tolerance
                || snapshot.Y + snapshot.Height > rootRect.Bottom + tolerance)
            {
                Console.WriteLine(
                    $"[visual-smoke] Layout warning on '{label}': interactive UIA element " +
                    $"'{snapshot.AutomationId ?? snapshot.Name}' reports bounds outside the root window " +
                    "(DPI/provider coordinate mismatch is possible)." );
            }

            visibleInteractive.Add((element, snapshot));
        }

        foreach (var element in EnumerateDescendants(root, 1000))
        {
            var snapshot = SafeDescribeElement(element);
            if (snapshot is null
                || snapshot.IsOffscreen
                || string.IsNullOrWhiteSpace(snapshot.Name)
                || snapshot.X is null
                || snapshot.Y is null
                || snapshot.Width is null
                || snapshot.Height is null
                || snapshot.Width <= 1
                || snapshot.Height <= 1
                || IsIconGlyph(snapshot.Name)
                || string.Equals(snapshot.AutomationId, "MessageText", StringComparison.Ordinal)
                || (!snapshot.Type.Equals("Text", StringComparison.Ordinal)
                    && !snapshot.Type.Equals("Label", StringComparison.Ordinal)))
            {
                continue;
            }

            if (IsEllipsized(snapshot.Name) && string.IsNullOrWhiteSpace(snapshot.HelpText))
                throw new InvalidOperationException(
                    $"Ellipsized text '{snapshot.Name}' has no full-text Tooltip on '{label}'.");
            visibleText.Add(snapshot);
        }

        for (var leftIndex = 0; leftIndex < visibleText.Count; leftIndex++)
        {
            var left = visibleText[leftIndex];
            for (var rightIndex = leftIndex + 1; rightIndex < visibleText.Count; rightIndex++)
            {
                var right = visibleText[rightIndex];
                if (Contains(left, right) || Contains(right, left))
                    continue;

                var overlap = IntersectionArea(left, right);
                var smallerArea = Math.Min(left.Width!.Value * left.Height!.Value, right.Width!.Value * right.Height!.Value);
                if (smallerArea > 0 && overlap / smallerArea >= 0.20)
                    throw new InvalidOperationException(
                        $"Visible text nodes overlap on '{label}': '{left.Name}' and '{right.Name}'.");
            }
        }

        for (var leftIndex = 0; leftIndex < visibleInteractive.Count; leftIndex++)
        {
            var left = visibleInteractive[leftIndex].Snapshot;
            for (var rightIndex = leftIndex + 1; rightIndex < visibleInteractive.Count; rightIndex++)
            {
                var right = visibleInteractive[rightIndex].Snapshot;
                if (Contains(left, right) || Contains(right, left))
                    continue;

                var overlap = IntersectionArea(left, right);
                var smallerArea = Math.Min(left.Width!.Value * left.Height!.Value, right.Width!.Value * right.Height!.Value);
                if (smallerArea <= 0 || overlap / smallerArea < 0.35)
                    continue;

                throw new InvalidOperationException(
                    $"Interactive UIA elements overlap on '{label}': " +
                    $"'{left.AutomationId ?? left.Name}' and '{right.AutomationId ?? right.Name}'.");
            }
        }
    }

    private static bool Contains(ElementSnapshot outer, ElementSnapshot inner) =>
        outer.X <= inner.X
        && outer.Y <= inner.Y
        && outer.X + outer.Width >= inner.X + inner.Width
        && outer.Y + outer.Height >= inner.Y + inner.Height;

    private static double IntersectionArea(ElementSnapshot left, ElementSnapshot right)
    {
        var leftX = left.X!.Value;
        var leftY = left.Y!.Value;
        var rightX = right.X!.Value;
        var rightY = right.Y!.Value;
        var leftRight = leftX + left.Width!.Value;
        var leftBottom = leftY + left.Height!.Value;
        var rightRight = rightX + right.Width!.Value;
        var rightBottom = rightY + right.Height!.Value;
        var x = Math.Max(0d, Math.Min(leftRight, rightRight) - Math.Max(leftX, rightX));
        var y = Math.Max(0d, Math.Min(leftBottom, rightBottom) - Math.Max(leftY, rightY));
        return x * y;
    }

    private static bool IsEllipsized(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && (value.EndsWith("...", StringComparison.Ordinal)
            || value.EndsWith("…", StringComparison.Ordinal));

    private static bool IsFrameworkTemplatePart(AutomationElement element)
    {
        try
        {
            return IsFrameworkTemplatePart(element.Current.AutomationId, element.Current.ClassName);
        }
        catch (ElementNotAvailableException)
        {
            return true;
        }
    }

    private static bool IsFrameworkTemplatePart(ElementSnapshot snapshot) =>
        IsFrameworkTemplatePart(snapshot.AutomationId, snapshot.ClassName);

    private static bool IsIconGlyph(string? value) =>
        !string.IsNullOrEmpty(value)
        && value.Length <= 2
        && value.All(character => char.GetUnicodeCategory(character) == UnicodeCategory.PrivateUse);

    private static bool IsFrameworkTemplatePart(string? automationId, string? className) =>
        (!string.IsNullOrWhiteSpace(automationId)
         && automationId.StartsWith("PART_", StringComparison.OrdinalIgnoreCase))
        || string.Equals(className, "ScrollBar", StringComparison.OrdinalIgnoreCase)
        || string.Equals(className, "RepeatButton", StringComparison.OrdinalIgnoreCase);

    private static void AssertThemeSurface(string outputPath, string label)
    {
        if (!_assertDarkThemeSurface)
            return;

        if (!label.StartsWith("dashboard", StringComparison.OrdinalIgnoreCase) &&
            !label.Equals("main-window-ready", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        using var bitmap = new Bitmap(outputPath);
        var sample = SampleRegion(bitmap,
            x: (int)Math.Round(bitmap.Width * 0.24),
            y: (int)Math.Round(bitmap.Height * 0.10),
            width: (int)Math.Round(bitmap.Width * 0.70),
            height: (int)Math.Round(bitmap.Height * 0.42));

        if (sample.AverageLuminance > 120)
        {
            var message = $"Dark theme surface regression detected in '{label}'. Average luminance {sample.AverageLuminance:F1} is too bright. Screenshot: {outputPath}";
            if (_host.Equals("avalonia", StringComparison.OrdinalIgnoreCase))
                Console.WriteLine($"[visual-smoke] Theme warning: {message} Avalonia currently uses its system theme variant.");
            else
                throw new InvalidOperationException(message);
        }
    }

    private static RegionSample SampleRegion(Bitmap bitmap, int x, int y, int width, int height)
    {
        var left = Math.Clamp(x, 0, bitmap.Width - 1);
        var top = Math.Clamp(y, 0, bitmap.Height - 1);
        var right = Math.Clamp(x + width, left + 1, bitmap.Width);
        var bottom = Math.Clamp(y + height, top + 1, bitmap.Height);
        double luminance = 0;
        var count = 0;

        for (var sampleY = top; sampleY < bottom; sampleY += 12)
        {
            for (var sampleX = left; sampleX < right; sampleX += 12)
            {
                var color = bitmap.GetPixel(sampleX, sampleY);
                luminance += 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;
                count++;
            }
        }

        return new RegionSample(count == 0 ? 0 : luminance / count);
    }

    private static object BuildSnapshot(string label, AutomationElement root)
    {
        var elements = EnumerateDescendants(root, 260)
            .Select(element => SafeDescribeElement(element))
            .Where(element => element is not null)
            .ToArray();

        var visibleText = elements
            .Where(element => element is { IsOffscreen: false } && !string.IsNullOrWhiteSpace(element.Name))
            .Select(element => element!.Name!)
            .Distinct()
            .Take(120)
            .ToArray();

        return new
        {
            label,
            capturedAt = DateTimeOffset.Now,
            root = SafeDescribeElement(root),
            visibleText,
            elements,
        };
    }

    private static ElementSnapshot? SafeDescribeElement(AutomationElement element)
    {
        try
        {
            var rect = element.Current.BoundingRectangle;
            return new ElementSnapshot(
                element.Current.ControlType.ProgrammaticName.Replace("ControlType.", string.Empty, StringComparison.Ordinal),
                element.Current.Name,
                element.Current.HelpText,
                element.Current.AutomationId,
                element.Current.ClassName,
                element.Current.IsEnabled,
                element.Current.IsOffscreen,
                NormalizeJsonNumber(rect.X),
                NormalizeJsonNumber(rect.Y),
                NormalizeJsonNumber(rect.Width),
                NormalizeJsonNumber(rect.Height));
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    private static IEnumerable<AutomationElement> EnumerateDescendants(AutomationElement root, int maxCount)
    {
        AutomationElementCollection collection;
        try
        {
            collection = root.FindAll(TreeScope.Descendants, Condition.TrueCondition);
        }
        catch (ElementNotAvailableException)
        {
            yield break;
        }
        catch (COMException)
        {
            yield break;
        }

        var count = Math.Min(maxCount, collection.Count);
        for (var i = 0; i < count; i++)
            yield return collection[i];
    }

    private static AutomationElement? FindNavigationElement(AutomationElement root, PageTarget target)
    {
        foreach (var id in target.AutomationIds)
        {
            var byId = FindByAutomationId(root, id);
            if (IsVisible(byId))
                return byId;
        }

        foreach (var name in target.Names)
        {
            var byName = FindByName(root, name);
            if (IsVisible(byName))
                return byName is null ? null : FindClickableAncestor(byName) ?? byName;
        }

        return null;
    }

    private static AutomationElement? FindClickableAncestor(AutomationElement element)
    {
        var walker = TreeWalker.ControlViewWalker;
        var current = element;
        for (var i = 0; i < 5; i++)
        {
            var parent = walker.GetParent(current);
            if (parent is null)
                return null;

            try
            {
                var controlType = parent.Current.ControlType;
                if (controlType == ControlType.DataItem || controlType == ControlType.Button || controlType == ControlType.ListItem)
                    return parent;

                current = parent;
            }
            catch (ElementNotAvailableException)
            {
                return null;
            }
        }

        return null;
    }

    private static void ActivateElement(AutomationElement element)
    {
        BringToForeground(ResolveLiveWindowByProcessId(_processId));

        if (TryInvokePattern(element))
            return;

        if (TryExpandCollapsePattern(element))
            return;

        MouseClick(element);
    }

    private static void ActivateNavigationElement(AutomationElement element)
    {
        BringToForeground(ResolveLiveWindowByProcessId(_processId));
        MouseClick(element);
    }

    private static bool TryInvokePattern(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var pattern))
            {
                ((InvokePattern)pattern).Invoke();
                return true;
            }
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"[visual-smoke] InvokePattern failed: {ex.Message}");
        }
        catch (ElementNotAvailableException ex)
        {
            Debug.WriteLine($"[visual-smoke] InvokePattern element not available: {ex.Message}");
        }

        return false;
    }

    private static bool TryExpandCollapsePattern(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var pattern))
            {
                var expander = (ExpandCollapsePattern)pattern;
                if (expander.Current.ExpandCollapseState != ExpandCollapseState.Expanded)
                    expander.Expand();
                return true;
            }
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"[visual-smoke] ExpandCollapsePattern failed: {ex.Message}");
        }
        catch (ElementNotAvailableException ex)
        {
            Debug.WriteLine($"[visual-smoke] ExpandCollapsePattern element not available: {ex.Message}");
        }

        return false;
    }

    private static AutomationElement WaitForMainShellWindow(int processId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var window = TryFindMainShellWindow(processId);
            if (window is not null)
                return window;

            Thread.Sleep(250);
        }

        throw new TimeoutException("Timed out waiting for main shell window.");
    }

    private static AutomationElement? TryFindMainShellWindow(int processId)
    {
        var condition = new AndCondition(
            new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

        var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, condition).Cast<AutomationElement>();
        foreach (var window in windows)
        {
            if (TryHandleCompatibilityWindow(window))
                continue;

            var name = GetElementName(window);
            if (FindByAutomationId(window, "MainNavigationStore") is not null
                || FindByAutomationId(window, "MainRootFrame") is not null
                || name.Contains("Universal Device Toolkit", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Lenovo Legion Toolkit", StringComparison.OrdinalIgnoreCase))
            {
                return window;
            }
        }

        return null;
    }

    private static string GetElementName(AutomationElement element)
    {
        try
        {
            return element.Current.Name ?? string.Empty;
        }
        catch (ElementNotAvailableException)
        {
            return string.Empty;
        }
    }

    private static string GetAutomationLabel(AutomationElement element)
    {
        var name = GetElementName(element);
        if (!name.Contains("NavigationItemViewModel", StringComparison.OrdinalIgnoreCase))
            return name;

        try
        {
            var textNames = element.FindAll(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text))
                .Cast<AutomationElement>()
                .Select(GetElementName)
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
                .Where(candidate => !candidate.Equals(name, StringComparison.Ordinal))
                .Where(candidate => !candidate.EndsWith("24", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return textNames.FirstOrDefault() ?? name;
        }
        catch (ElementNotAvailableException)
        {
            return name;
        }
    }

    private static bool TryHandleCompatibilityWindow(AutomationElement window)
    {
        var continueButton = FindByAutomationId(window, "_continueButton");
        if (!IsVisible(continueButton) || continueButton is null || !continueButton.Current.IsEnabled)
            return false;

        ActivateElement(continueButton);
        Thread.Sleep(400);
        return true;
    }

    private static AutomationElement ResolveLiveWindow(AutomationElement window)
    {
        var processId = window.Current.ProcessId;
        return ResolveLiveWindowByProcessId(processId, TimeSpan.FromSeconds(5));
    }

    private static AutomationElement ResolveLiveWindowByProcessId(int processId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (DateTime.UtcNow < deadline)
        {
            var window = TryFindMainShellWindow(processId);
            if (window is not null)
                return window;

            Thread.Sleep(100);
        }

        throw new InvalidOperationException(
            $"Main shell window is not available for process {processId} after {(timeout ?? TimeSpan.FromSeconds(5)).TotalSeconds:F0}s.");
    }

    private static AutomationElement WaitForAutomationId(AutomationElement root, string automationId, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => TryResolveLiveWindow(root, out var live)
                  && IsVisible(FindByAutomationId(live, automationId)),
            timeout,
            TimeSpan.FromMilliseconds(200));

        var element = TryResolveLiveWindow(root, out var resolved)
            ? FindByAutomationId(resolved, automationId)
            : null;
        if (!found || element is null)
            throw new TimeoutException($"Timed out waiting for automation id '{automationId}'.");

        return element;
    }

    private static bool TryResolveLiveWindow(AutomationElement window, out AutomationElement live)
    {
        live = null!;
        try
        {
            var processId = window.Current.ProcessId;
            var candidate = TryFindMainShellWindow(processId);
            if (candidate is null)
                return false;

            live = candidate;
            return true;
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
    }

    private static AutomationElement? FindByAutomationId(AutomationElement root, string automationId)
    {
        try
        {
            return root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    private static AutomationElement? FindByName(AutomationElement root, string name)
    {
        try
        {
            var matches = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name))
                .Cast<AutomationElement>()
                .Where(IsVisible)
                .OrderBy(element =>
                {
                    try
                    {
                        return element.Current.BoundingRectangle.X;
                    }
                    catch
                    {
                        return double.MaxValue;
                    }
                })
                .ToArray();

            return matches.FirstOrDefault();
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
    }

    private static bool IsWindowsOptimizationPageReady(AutomationElement root)
    {
        var isSystemOptimizationWindow = root.Current.Name.Contains("System optimization", StringComparison.OrdinalIgnoreCase)
                                         || FindVisibleTextContains(root, "These actions modify system services and files");

        return isSystemOptimizationWindow
               && IsVisible(FindByAutomationId(root, "WindowsOptimizationCategoryList"))
               && IsVisible(FindByAutomationId(root, "WindowsOptimizationOptimizationTabButton"));
    }

    private static bool IsPluginExtensionsPageReady(AutomationElement root)
    {
        if (!FindVisibleTextContains(root, "Plugin Extensions") && !IsVisible(FindByAutomationId(root, "PluginListBox")))
            return false;

        if (FindVisibleTextContains(root, "Loading metadata"))
            return false;

        return IsVisible(FindByAutomationId(root, "PluginCard_custom-mouse"))
               || IsVisible(FindByAutomationId(root, "PluginCard_vive-tool"))
               || IsVisible(FindByAutomationId(root, "PluginNoPluginsMessage"))
               || IsVisible(FindByAutomationId(root, "PluginNoResultsMessage"))
               || FindVisibleTextContains(root, "Found 4 plugins")
               || FindVisibleTextContains(root, "Available to install");
    }

    private static AutomationElement WaitForNamedComboBox(AutomationElement root, string name, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => FindNamedComboBox(ResolveLiveWindow(root), name) is not null,
            timeout,
            TimeSpan.FromMilliseconds(200));

        var comboBox = FindNamedComboBox(ResolveLiveWindow(root), name);
        if (!found || comboBox is null)
            throw new TimeoutException($"Timed out waiting for combo box '{name}'.");

        return comboBox;
    }

    private static AutomationElement? FindNamedComboBox(AutomationElement root, string name)
    {
        try
        {
            return root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox))
                .Cast<AutomationElement>()
                .Where(IsVisible)
                .FirstOrDefault(element => string.Equals(element.Current.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        catch (ElementNotAvailableException)
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
            var bounds = element.Current.BoundingRectangle;
            return element.Current.IsEnabled
                   && !element.Current.IsOffscreen
                   && bounds.Width > 1
                   && bounds.Height > 1
                   && !double.IsInfinity(bounds.X)
                   && !double.IsInfinity(bounds.Y);
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
    }

    private static bool FindVisibleTextContains(AutomationElement root, string keyword)
    {
        try
        {
            return root.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                .Cast<AutomationElement>()
                .Any(element =>
                {
                    try
                    {
                        return !element.Current.IsOffscreen
                               && !string.IsNullOrWhiteSpace(element.Current.Name)
                               && element.Current.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase);
                    }
                    catch (ElementNotAvailableException)
                    {
                        return false;
                    }
                });
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
    }

    private static bool FindVisibleClassContains(AutomationElement root, string keyword)
    {
        try
        {
            return root.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                .Cast<AutomationElement>()
                .Any(element =>
                {
                    try
                    {
                        return !element.Current.IsOffscreen
                               && element.Current.ClassName.Contains(keyword, StringComparison.OrdinalIgnoreCase);
                    }
                    catch (ElementNotAvailableException)
                    {
                        return false;
                    }
                });
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
    }

    private static bool WaitUntil(Func<bool> predicate, TimeSpan timeout, TimeSpan interval)
    {
        var deadline = DateTime.UtcNow + timeout;
        Exception? lastException = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (predicate())
                    return true;
            }
            catch (ElementNotAvailableException ex)
            {
                lastException = ex;
            }
            catch (InvalidOperationException ex)
            {
                lastException = ex;
            }

            Thread.Sleep(interval);
        }

        if (lastException is not null)
            Console.WriteLine($"[visual-smoke] Wait ignored transient error: {lastException.Message}");

        return false;
    }

    private static void NormalizeWindow(AutomationElement window)
        => NormalizeWindow(window, _activeWindowWidth, _activeWindowHeight);

    private static void NormalizeWindow(AutomationElement window, int width, int height)
    {
        if (!TryGetNativeWindowHandle(window, out var handle))
            return;

        var hwnd = (IntPtr)handle;
        ShowWindow(hwnd, 9);
        SetWindowPos(hwnd, IntPtr.Zero, _windowX, _windowY, width, height, 0x0040);
        SetForegroundWindow(hwnd);
        Thread.Sleep(250);
    }

    private static void BringToForeground(AutomationElement window)
    {
        if (!TryGetNativeWindowHandle(window, out var handle))
            return;

        var hwnd = (IntPtr)handle;
        ShowWindow(hwnd, 9);
        SetForegroundWindow(hwnd);
        Thread.Sleep(100);
    }

    private static bool TryGetNativeWindowHandle(AutomationElement window, out int handle)
    {
        try
        {
            handle = window.Current.NativeWindowHandle;
            return handle != 0;
        }
        catch (ElementNotAvailableException)
        {
            handle = 0;
            return false;
        }
    }

    private static bool WaitForIpcReady(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (TrySendIpcRequest(IpcRequest.OperationType.GetAppStatus, out var response) && response?.Success == true)
            {
                Console.WriteLine("[visual-smoke] IPC ready.");
                return true;
            }

            Thread.Sleep(250);
        }

        return false;
    }

    private static bool TrySendIpcRequest(IpcRequest.OperationType operation, out IpcResponse? response, string? name = null, string? value = null)
    {
        response = null;

        try
        {
            using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.None);
            pipe.Connect(5000);
            pipe.ReadMode = PipeTransmissionMode.Message;
            pipe.ReadTimeout = 5000;
            pipe.WriteTimeout = 5000;

            var request = new IpcRequest
            {
                Operation = operation,
                Name = name,
                Value = value
            };

            WritePipeObject(pipe, request);
            response = ReadPipeObject<IpcResponse>(pipe);
            return response?.Success == true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[visual-smoke] IPC request '{operation}' unavailable: {ex.Message}");
            return false;
        }
    }

    private static void CaptureWindowFromScreen(int windowHandle, string outputPath)
    {
        if (!GetWindowRect((IntPtr)windowHandle, out var rect))
            throw new InvalidOperationException($"Could not read window bounds for {windowHandle}.");

        var width = Math.Max(1, rect.Right - rect.Left);
        var height = Math.Max(1, rect.Bottom - rect.Top);
        using var bitmap = new Bitmap(width, height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            var hdc = graphics.GetHdc();
            bool captured;
            try
            {
                captured = PrintWindow((IntPtr)windowHandle, hdc, 2);
            }
            finally
            {
                graphics.ReleaseHdc(hdc);
            }

            if (!captured)
                graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
    }

    private static void WritePipeObject<T>(PipeStream stream, T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private static T? ReadPipeObject<T>(PipeStream stream)
    {
        var buffer = new byte[4096];
        var builder = new StringBuilder();

        do
        {
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read <= 0)
                break;

            builder.Append(Encoding.UTF8.GetString(buffer, 0, read));
        } while (!stream.IsMessageComplete);

        return JsonSerializer.Deserialize<T>(builder.ToString());
    }

    private static double? NormalizeJsonNumber(double value)
    {
        return double.IsFinite(value) ? value : null;
    }

    private static void MouseClick(AutomationElement element)
    {
        var bounds = element.Current.BoundingRectangle;
        if (bounds.Width <= 1 || bounds.Height <= 1)
            throw new InvalidOperationException($"Cannot click element with empty bounds: {element.Current.AutomationId}");

        var x = (int)Math.Round(bounds.X + bounds.Width / 2);
        var y = (int)Math.Round(bounds.Y + bounds.Height / 2);
        SetCursorPos(x, y);
        mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(50);
        mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
    }

    private static void SelectComboBoxItemByNames(AutomationElement comboBox, params string[] itemNames)
    {
        if (comboBox.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandPattern))
            ((ExpandCollapsePattern)expandPattern).Expand();

        Thread.Sleep(250);

        var listItemCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem);
        var items = comboBox.FindAll(TreeScope.Descendants, listItemCondition)
            .Cast<AutomationElement>()
            .Concat(AutomationElement.RootElement.FindAll(TreeScope.Descendants, listItemCondition).Cast<AutomationElement>())
            .Where(IsVisible)
            .ToArray();

        var item = items.FirstOrDefault(candidate =>
            itemNames.Any(itemName =>
                string.Equals(candidate.Current.Name, itemName, StringComparison.OrdinalIgnoreCase)));

        if (item is null)
            throw new InvalidOperationException($"ComboBox option was not found. Expected one of: [{string.Join(", ", itemNames)}].");

        MouseClick(item);
        Thread.Sleep(500);

        if (comboBox.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var collapsePattern))
        {
            var expander = (ExpandCollapsePattern)collapsePattern;
            if (expander.Current.ExpandCollapseState == ExpandCollapseState.Expanded ||
                expander.Current.ExpandCollapseState == ExpandCollapseState.PartiallyExpanded)
            {
                expander.Collapse();
            }
        }
    }

    private static void PressCtrlTab()
    {
        keybd_event(0x11, 0, 0, UIntPtr.Zero);
        keybd_event(0x09, 0, 0, UIntPtr.Zero);
        Thread.Sleep(40);
        keybd_event(0x09, 0, 0x0002, UIntPtr.Zero);
        keybd_event(0x11, 0, 0x0002, UIntPtr.Zero);
        Thread.Sleep(150);
    }

    private static void WaitForAnimationsToComplete()
    {
        Thread.Sleep(900);
    }

    private static Process StartApp(string runtimeDirectory, string appDataDirectory, string host)
    {
        var appBaseNames = host.Equals("avalonia", StringComparison.OrdinalIgnoreCase)
            ? _avaloniaAppBaseNames
            : _wpfAppBaseNames;
        var appBaseName = appBaseNames.FirstOrDefault(name =>
            File.Exists(Path.Combine(runtimeDirectory, $"{name}.dll")) &&
            File.Exists(Path.Combine(runtimeDirectory, $"{name}.runtimeconfig.json")))
            ?? appBaseNames.FirstOrDefault(name => File.Exists(Path.Combine(runtimeDirectory, $"{name}.exe")));

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
                Arguments = $"--trace --disable-update-checker --disable-tray-tooltip",
                WorkingDirectory = runtimeDirectory,
                UseShellExecute = false
            };
        }
        else
            throw new FileNotFoundException($"Could not find startup entry in runtime directory: {runtimeDirectory}");

        SetEnvVar(startInfo.EnvironmentVariables, _appDataOverrideEnvironmentVariable, appDataDirectory);

        SetEnvVar(startInfo.EnvironmentVariables, "UDT_SMOKE_AUTOMATION", "1");

        return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start app process."); // NOTE: Returned process — caller is responsible for disposal
    }

    private static void PrepareSandboxSettings(string repoRoot, string appDataDirectory, string theme, string themeStyle, string language, bool enableAnimations)
    {
        var baselineAppData = Path.Combine(repoRoot, "Build", "wpf-navigation-smoke-2026-05-08", "sandbox", "appdata");
        CopyIfExists(Path.Combine(baselineAppData, "settings.json"), Path.Combine(appDataDirectory, "settings.json"));
        CopyIfExists(Path.Combine(baselineAppData, "package_downloader.json"), Path.Combine(appDataDirectory, "package_downloader.json"));

        var settingsPath = Path.Combine(appDataDirectory, "settings.json");
        var root = File.Exists(settingsPath)
            ? JsonNode.Parse(File.ReadAllText(settingsPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();

        root["Theme"] = theme;
        root["ThemeStylePreset"] = themeStyle;
        root["WindowSize"] = new JsonObject
        {
            ["Width"] = _activeWindowWidth,
            ["Height"] = _activeWindowHeight
        };
        root["MinimizeToTray"] = false;
        root["MinimizeOnClose"] = false;
        root["DisableUnsupportedHardwareWarning"] = true;
        root["ForceSoftwareRendering"] = true;
        root["ExtensionsEnabled"] = false;
        root["AnimationsEnabled"] = enableAnimations;
        root["NavigationPaneExpanded"] = true;

        Directory.CreateDirectory(appDataDirectory);
        File.WriteAllText(settingsPath, root.ToJsonString(_jsonOptions));

        // Avalonia persists its portable theme preference separately from the WPF settings file.
        // Seed the same requested variant so cross-host visual checks exercise the intended theme.
        var avaloniaThemePath = Path.Combine(appDataDirectory, "avalonia-theme.json");
        File.WriteAllText(
            avaloniaThemePath,
            new JsonObject
            {
                ["Theme"] = theme.Equals("Light", StringComparison.OrdinalIgnoreCase)
                    ? "Light"
                    : theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                        ? "Dark"
                        : "System",
                ["ApplyAccentColorToTheme"] = true,
                ["UseSystemAccent"] = true
            }.ToJsonString(_jsonOptions));

        var integrationsPath = Path.Combine(appDataDirectory, "integrations.json");
        File.WriteAllText(integrationsPath, new JsonObject { ["CLI"] = true }.ToJsonString(_jsonOptions));

        var langPath = Path.Combine(appDataDirectory, "lang");
        File.WriteAllText(langPath, string.IsNullOrWhiteSpace(language) ? "en" : language);

        var deviceSetupPath = Path.Combine(appDataDirectory, "device-setup");
        if (!File.Exists(deviceSetupPath))
        {
            File.WriteAllLines(deviceSetupPath,
            [
                "devicePackId=",
                "basicMode=false",
                $"confirmedAtUtc={DateTimeOffset.UtcNow:O}"
            ]);
        }
    }

    private static void PrepareOsdSandboxSettings(string appDataDirectory)
    {
        var settingsPath = Path.Combine(appDataDirectory, "settings.json");
        var root = File.Exists(settingsPath)
            ? JsonNode.Parse(File.ReadAllText(settingsPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();

        root["EnableHardwareSensors"] = true;
        File.WriteAllText(settingsPath, root.ToJsonString(_jsonOptions));

        var osdPath = Path.Combine(appDataDirectory, "osd.json");
        File.WriteAllText(osdPath, new JsonObject
        {
            ["ShowOsd"] = true,
            ["SelectedStyleIndex"] = 0,
            ["OsdRefreshInterval"] = 1,
            ["BackgroundOpacity"] = 0.85,
            ["BackgroundColor"] = "#1E1E1E",
            ["PanelPositionX"] = 60,
            ["PanelPositionY"] = 60,
            ["IsLocked"] = true
        }.ToJsonString(_jsonOptions));

        Console.WriteLine("[visual-smoke] Prepared sandbox OSD settings (panel style, hardware sensors enabled).");
    }

    private static void UpdateSandboxTheme(string theme)
    {
        if (string.IsNullOrWhiteSpace(_appDataDirectory))
            return;

        var settingsPath = Path.Combine(_appDataDirectory, "settings.json");
        var root = File.Exists(settingsPath)
            ? JsonNode.Parse(File.ReadAllText(settingsPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();

        root["Theme"] = theme;
        File.WriteAllText(settingsPath, root.ToJsonString(_jsonOptions));
        Console.WriteLine($"[visual-smoke] Updated sandbox theme to {theme}");
    }

    private static void ResetDirectory(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);

        Directory.CreateDirectory(directory);
    }

    private static void CopyIfExists(string sourcePath, string destinationPath)
    {
        if (!File.Exists(sourcePath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourcePath, destinationPath, overwrite: true);
    }

    private static void SeedPluginStoreCache(string repoRoot, string appDataDirectory)
    {
        var destinationPath = Path.Combine(appDataDirectory, "plugin-store-cache.json");
        var sourcePath = Path.Combine(repoRoot, "Build", "manual-simulated-legion-run", "appdata", "plugin-store-cache.json");

        var storeJson = File.Exists(sourcePath)
            ? File.ReadAllText(sourcePath, Encoding.UTF8)
            : """
              {
                "lastUpdated": "2026-04-29T12:18:46Z",
                "plugins": [
                  {
                    "id": "custom-mouse",
                    "name": "Custom Mouse",
                    "description": "Customize mouse cursor style behavior and mouse settings",
                    "author": "SSC-STUDIO",
                    "version": "1.0.15",
                    "minLLTVersion": "3.6.1",
                    "downloadUrl": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/download/custom-mouse-v1.0.15/custom-mouse-v1.0.15.zip",
                    "changelog": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/tag/custom-mouse-v1.0.15",
                    "releaseDate": "2026-04-29T12:18:46Z",
                    "icon": "Pen24",
                    "iconBackground": "#2563EB",
                    "dependencies": [],
                    "tags": [ "mouse", "customization", "gaming" ]
                  },
                  {
                    "id": "shell-integration",
                    "name": "Shell Integration",
                    "description": "Integrate Universal Device Toolkit with Windows shell context menu",
                    "author": "SSC-STUDIO",
                    "version": "1.0.11",
                    "minLLTVersion": "3.6.1",
                    "isSystemPlugin": true,
                    "downloadUrl": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/download/shell-integration-v1.0.11/shell-integration-v1.0.11.zip",
                    "changelog": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/tag/shell-integration-v1.0.11",
                    "releaseDate": "2026-04-29T12:18:46Z",
                    "icon": "Folder24",
                    "iconBackground": "#0F766E",
                    "dependencies": [],
                    "tags": [ "system", "shell", "integration" ]
                  },
                  {
                    "id": "vive-tool",
                    "name": "ViVeTool",
                    "description": "Manage Windows feature flags using ViVeTool",
                    "author": "SSC-STUDIO",
                    "version": "1.2.1",
                    "minLLTVersion": "3.6.1",
                    "downloadUrl": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/download/vive-tool-v1.2.1/vive-tool-v1.2.1.zip",
                    "changelog": "https://github.com/SSC-STUDIO/UniversalDeviceToolkit-Plugins/releases/tag/vive-tool-v1.2.1",
                    "releaseDate": "2026-04-29T12:18:46Z",
                    "icon": "Code24",
                    "iconBackground": "#7C3AED",
                    "dependencies": [],
                    "tags": [ "windows", "feature-flags", "vivetool" ]
                  }
                ]
              }
              """;

        Directory.CreateDirectory(appDataDirectory);
        File.WriteAllText(destinationPath, storeJson, Encoding.UTF8);
        File.SetLastWriteTimeUtc(destinationPath, DateTime.UtcNow);
    }

    private static string ResolveRuntimeDirectory(string repoRoot, string configuration, string host)
    {
        var projectDirectory = host.Equals("avalonia", StringComparison.OrdinalIgnoreCase)
            ? "UniversalDeviceToolkit.Avalonia"
            : "UniversalDeviceToolkit.WPF";
        var binRoot = Path.Combine(repoRoot, projectDirectory, "bin");
        var runtimeRoots = new[]
        {
            Path.Combine(binRoot, "x64", configuration),
            Path.Combine(binRoot, configuration)
        };

        var directCandidates = runtimeRoots.SelectMany(runtimeRoot => new[]
        {
            Path.Combine(runtimeRoot, "net10.0-windows10.0.26100.0"),
            Path.Combine(runtimeRoot, "net10.0"),
            Path.Combine(runtimeRoot, "net10.0-windows10.0.26100.0", "win-x64"),
            Path.Combine(runtimeRoot, "net10.0", "win-x64"),
            Path.Combine(runtimeRoot, "net10.0-windows", "win-x64")
        });

        foreach (var candidate in directCandidates)
        {
            if (Directory.Exists(candidate) && ContainsMainAppStartupEntry(candidate, host))
                return candidate;
        }

        var discovered = runtimeRoots
            .Where(Directory.Exists)
            .SelectMany(runtimeRoot => Directory.EnumerateDirectories(runtimeRoot, "net10.0*", SearchOption.TopDirectoryOnly))
            .SelectMany(path => new[] { path, Path.Combine(path, "win-x64") })
            .Where(Directory.Exists)
            .Where(path => ContainsMainAppStartupEntry(path, host))
            .OrderByDescending(Directory.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (discovered is not null)
            return discovered;

        throw new DirectoryNotFoundException($"Runtime directory not found under: {string.Join(", ", runtimeRoots)}");
    }

    private static bool ContainsMainAppStartupEntry(string runtimeDirectory, string host)
    {
        var appBaseNames = host.Equals("avalonia", StringComparison.OrdinalIgnoreCase)
            ? _avaloniaAppBaseNames
            : _wpfAppBaseNames;
        return appBaseNames.Any(name =>
            (File.Exists(Path.Combine(runtimeDirectory, $"{name}.dll")) &&
             File.Exists(Path.Combine(runtimeDirectory, $"{name}.runtimeconfig.json"))) ||
            File.Exists(Path.Combine(runtimeDirectory, $"{name}.exe")));
    }

    private static void TryWaitForInputIdle(Process process, int milliseconds)
    {
        try
        {
            process.WaitForInputIdle(milliseconds);
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"[visual-smoke] WaitForInputIdle failed: {ex.Message}");
        }
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
        catch (Exception ex)
        {
            Debug.WriteLine($"[visual-smoke] CloseMainWindow failed: {ex.Message}");
        }

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[visual-smoke] Kill process failed: {ex.Message}");
        }
    }

    private static void WriteManifest(string currentDirectory, string outputRoot, string appDataDirectory)
    {
        ValidateCaptureArtifacts(currentDirectory);
        MaterializeCanonicalArtifacts(currentDirectory, outputRoot);

        var indexPath = Path.Combine(currentDirectory, "index.md");
        var lines = new List<string>
        {
            "# Visual Regression Smoke",
            string.Empty,
            $"Generated: {DateTimeOffset.Now:O}",
            $"AppData: `{appDataDirectory}`",
            string.Empty,
            "## Captures",
        };

        lines.AddRange(_captures.Select(capture =>
            $"- `{capture.FileName}`: {capture.Label} ({capture.CapturedAt:HH:mm:ss})" +
            (capture.VideoFileName is null ? string.Empty : $"; video `{capture.VideoFileName}`") +
            $"; UIA `{capture.SnapshotFileName}`"));
        File.WriteAllLines(indexPath, lines);

        var htmlPath = Path.Combine(currentDirectory, "storyboard.html");
        File.WriteAllText(htmlPath, BuildStoryboardHtml());
        var canonicalIndexPath = Path.Combine(outputRoot, "index.md");
        var canonicalStoryboardPath = Path.Combine(outputRoot, "storyboard.html");
        if (!string.Equals(Path.GetFullPath(indexPath), Path.GetFullPath(canonicalIndexPath), StringComparison.OrdinalIgnoreCase))
            File.Copy(indexPath, canonicalIndexPath, overwrite: true);
        if (!string.Equals(Path.GetFullPath(htmlPath), Path.GetFullPath(canonicalStoryboardPath), StringComparison.OrdinalIgnoreCase))
            File.Copy(htmlPath, canonicalStoryboardPath, overwrite: true);
        Console.WriteLine($"[visual-smoke] Index: {indexPath}");
        Console.WriteLine($"[visual-smoke] Storyboard: {htmlPath}");
    }

    private static void WriteCaptureStateResult(string currentDirectory, CaptureRecord capture)
    {
        var stateDirectory = Path.Combine(
            currentDirectory,
            Path.GetDirectoryName(capture.FileName.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty);
        Directory.CreateDirectory(stateDirectory);

        var result = new
        {
            status = "passed",
            capture.Label,
            capture.Sequence,
            screenshot = Path.GetFileName(capture.FileName),
            video = capture.VideoFileName is null ? null : Path.GetFileName(capture.VideoFileName),
            automation = Path.GetFileName(capture.SnapshotFileName),
            ocr = Path.GetFileName(capture.OcrFileName),
            capturedAt = capture.CapturedAt
        };
        File.WriteAllText(
            Path.Combine(stateDirectory, "result.json"),
            JsonSerializer.Serialize(result, _jsonOptions));
    }

    private static void MaterializeCanonicalArtifacts(string currentDirectory, string outputRoot)
    {
        var sourceRoot = Path.GetFullPath(currentDirectory);
        var destinationRoot = Path.GetFullPath(outputRoot);
        Directory.CreateDirectory(destinationRoot);

        foreach (var capture in _captures)
        {
            var sourceDirectory = Path.Combine(
                sourceRoot,
                Path.GetDirectoryName(capture.FileName.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty);
            var destinationDirectory = Path.Combine(
                destinationRoot,
                Path.GetDirectoryName(capture.FileName.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty);
            Directory.CreateDirectory(destinationDirectory);

            CopyArtifact(sourceRoot, destinationRoot, capture.FileName);
            CopyArtifact(sourceRoot, destinationRoot, capture.SnapshotFileName);
            CopyArtifact(sourceRoot, destinationRoot, capture.OcrFileName);

            var stateResult = Path.Combine(sourceDirectory, "result.json");
            if (File.Exists(stateResult))
                File.Copy(stateResult, Path.Combine(destinationDirectory, "result.json"), overwrite: true);

            var screenshotPath = Path.Combine(sourceRoot, capture.FileName.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(screenshotPath))
                File.Copy(screenshotPath, Path.Combine(destinationDirectory, "keyframe.png"), overwrite: true);

            var automationPath = Path.Combine(sourceRoot, capture.SnapshotFileName.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(automationPath))
                File.Copy(automationPath, Path.Combine(destinationDirectory, "automation.json"), overwrite: true);

            var ocrPath = Path.Combine(sourceRoot, capture.OcrFileName.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(ocrPath))
                File.Copy(ocrPath, Path.Combine(destinationDirectory, "ocr.json"), overwrite: true);

            if (capture.VideoFileName is { } videoFileName)
            {
                var videoPath = Path.Combine(sourceRoot, videoFileName.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(videoPath))
                    File.Copy(videoPath, Path.Combine(destinationDirectory, "page.mp4"), overwrite: true);
            }
        }

        var canonicalManifest = new
        {
            schema = "udt.ui-audit.v1",
            generatedAt = DateTimeOffset.Now,
            captures = _captures.Select(capture => new
            {
                capture.Label,
                capture.Sequence,
                page = Path.GetDirectoryName(capture.FileName.Replace('/', Path.DirectorySeparatorChar))?.Split(Path.DirectorySeparatorChar)[0],
                state = Path.GetDirectoryName(capture.FileName.Replace('/', Path.DirectorySeparatorChar))?.Split(Path.DirectorySeparatorChar).Skip(1).FirstOrDefault(),
                screenshot = capture.FileName,
                video = capture.VideoFileName,
                automation = capture.SnapshotFileName,
                ocr = capture.OcrFileName
            }).ToArray()
        };
        File.WriteAllText(
            Path.Combine(destinationRoot, "manifest.json"),
            JsonSerializer.Serialize(canonicalManifest, _jsonOptions));
    }

    private static void CopyArtifact(string sourceRoot, string destinationRoot, string relativePath)
    {
        var sourcePath = Path.Combine(sourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var destinationPath = Path.Combine(destinationRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourcePath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourcePath, destinationPath, overwrite: true);
    }

    private static string BuildStoryboardHtml()
    {
        var json = JsonSerializer.Serialize(_captures.Select(capture => new
        {
            capture.Sequence,
            capture.Label,
            capture.FileName,
            capture.SnapshotFileName,
            capture.VideoFileName,
            capturedAt = capture.CapturedAt.ToString("HH:mm:ss")
        }));

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>Visual Regression Smoke</title>
  <style>
    body { margin: 0; font-family: "Segoe UI", sans-serif; background: #111318; color: #eef2ff; }
    .layout { display: grid; grid-template-columns: 320px 1fr; min-height: 100vh; }
    aside { padding: 18px; background: #171b24; border-right: 1px solid #303746; overflow: auto; }
    main { padding: 18px; overflow: auto; }
    button { display: block; width: 100%; margin: 0 0 8px; padding: 10px; border: 1px solid #303746; border-radius: 8px; background: #202634; color: #eef2ff; text-align: left; cursor: pointer; }
    button.active { border-color: #75b8ff; }
    img, video { max-width: 100%; height: auto; border: 1px solid #303746; border-radius: 10px; background: #05070a; }
    video { display: none; margin-top: 12px; }
    .muted { color: #a6b0c3; font-size: 13px; }
  </style>
</head>
<body>
  <div class="layout">
    <aside>
      <h1>Visual Regression Smoke</h1>
      <p class="muted">Page-by-page WPF/Avalonia screenshots, UIA snapshots and animation recordings.</p>
      <div id="list"></div>
    </aside>
    <main>
      <h2 id="title"></h2>
      <p id="meta" class="muted"></p>
      <img id="image" alt="" />
      <video id="video" controls preload="metadata"></video>
    </main>
  </div>
  <script>
    const captures = {{json}};
    const list = document.getElementById('list');
    const title = document.getElementById('title');
    const meta = document.getElementById('meta');
    const image = document.getElementById('image');
    const video = document.getElementById('video');
    function select(index) {
      const item = captures[index];
      title.textContent = `${item.Sequence}. ${item.Label}`;
      meta.textContent = `${item.FileName} | ${item.SnapshotFileName} | ${item.capturedAt}`;
      image.src = item.FileName;
      if (item.VideoFileName) {
        video.src = item.VideoFileName;
        video.style.display = 'block';
      } else {
        video.removeAttribute('src');
        video.style.display = 'none';
      }
      [...list.querySelectorAll('button')].forEach((button, i) => button.classList.toggle('active', i === index));
    }
    captures.forEach((item, index) => {
      const button = document.createElement('button');
      button.textContent = `${item.Sequence}. ${item.Label}`;
      button.addEventListener('click', () => select(index));
      list.appendChild(button);
    });
    if (captures.length) select(0);
  </script>
</body>
</html>
""";
    }

    private static void ValidateCaptureArtifacts(string currentDirectory)
    {
        if (_captures.Count == 0)
            throw new InvalidOperationException("Visual smoke produced no captures.");

        foreach (var capture in _captures)
        {
            var imagePath = Path.Combine(currentDirectory, capture.FileName);
            var snapshotPath = Path.Combine(currentDirectory, capture.SnapshotFileName);
            var ocrPath = Path.Combine(currentDirectory, capture.OcrFileName);
            if (!File.Exists(imagePath) || new FileInfo(imagePath).Length == 0)
                throw new InvalidOperationException($"Missing screenshot artifact for '{capture.Label}': {imagePath}");
            if (!File.Exists(snapshotPath) || new FileInfo(snapshotPath).Length == 0)
                throw new InvalidOperationException($"Missing UI Automation snapshot for '{capture.Label}': {snapshotPath}");
            if (!File.Exists(ocrPath) || new FileInfo(ocrPath).Length == 0)
                throw new InvalidOperationException($"Missing text audit artifact for '{capture.Label}': {ocrPath}");

            if (_videoEnabled)
            {
                if (string.IsNullOrWhiteSpace(capture.VideoFileName))
                    throw new InvalidOperationException($"Video artifact was not registered for '{capture.Label}'.");

                var videoPath = Path.Combine(currentDirectory, capture.VideoFileName);
                if (!File.Exists(videoPath) || new FileInfo(videoPath).Length == 0)
                    throw new InvalidOperationException($"Missing video artifact for '{capture.Label}': {videoPath}");
            }

            var stateDirectory = Path.Combine(
                currentDirectory,
                Path.GetDirectoryName(capture.FileName.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty);
            if (!File.Exists(Path.Combine(stateDirectory, "result.json")))
                throw new InvalidOperationException($"Missing state result artifact for '{capture.Label}'.");
        }
    }

    private static void WriteResult(string outputRoot, string appDataDirectory, Process? process, int? exitCode, string? error)
    {
        var resultPath = Path.Combine(outputRoot, "result.json");
        var result = new
        {
            finishedAt = DateTimeOffset.Now,
            appDataDirectory,
            processId = process?.Id,
            exitCode,
            error,
            captures = _captures,
            appLog = Directory.Exists(Path.Combine(appDataDirectory, "logs"))
                ? Directory.GetFiles(Path.Combine(appDataDirectory, "logs"), "*.json").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
                : null,
            errorLogs = Directory.Exists(Path.Combine(appDataDirectory, "logs"))
                ? Directory.GetFiles(Path.Combine(appDataDirectory, "logs"), "error_*.txt").OrderByDescending(File.GetLastWriteTimeUtc).ToArray()
                : []
        };

        File.WriteAllText(resultPath, JsonSerializer.Serialize(result, _jsonOptions));
    }

    private static void TryWriteFailureResult(string[] args, Process? process, Exception ex)
    {
        try
        {
            var options = SmokeOptions.Parse(args);
            var outputRoot = Path.GetFullPath(options.OutputDirectory);
            var appDataDirectory = Path.Combine(outputRoot, "sandbox", "appdata");
            Directory.CreateDirectory(outputRoot);
            WriteResult(outputRoot, appDataDirectory, process, process?.HasExited == true ? process.ExitCode : null, ex.ToString());
        }
        catch (Exception innerEx)
        {
            Debug.WriteLine($"[visual-smoke] WriteResult failed during crash handling: {innerEx.Message}");
        }
    }

    private static void DumpAutomationSnapshot(AutomationElement root, int maxCount)
    {
        Console.WriteLine("[visual-smoke] Automation snapshot:");
        foreach (var element in EnumerateDescendants(root, maxCount))
        {
            var snapshot = SafeDescribeElement(element);
            if (snapshot is null)
                continue;

            Console.WriteLine($"  {snapshot.Type} id='{snapshot.AutomationId}' name='{snapshot.Name}' class='{snapshot.ClassName}' offscreen={snapshot.IsOffscreen}");
        }
    }

    private static string DescribeElement(AutomationElement element)
    {
        try
        {
            return $"id='{element.Current.AutomationId}' name='{element.Current.Name}' class='{element.Current.ClassName}'";
        }
        catch (ElementNotAvailableException)
        {
            return "<unavailable element>";
        }
    }

    private static string SanitizeFileNameSegment(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        return string.Concat(value.Select(character => invalidChars.Contains(character) || char.IsWhiteSpace(character) || character == '/'
            ? '-'
            : char.ToLowerInvariant(character)));
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    private sealed record PageTarget(string Label, string[] AutomationIds, string[] Names, Func<AutomationElement, bool> Ready);

    private sealed record NavigationItemDescriptor(string Key, string Label);

    private sealed record CapturePath(string Page, string State);

    private sealed record CaptureRecord(
        int Sequence,
        string Label,
        string FileName,
        string SnapshotFileName,
        string? VideoFileName,
        string OcrFileName,
        DateTimeOffset CapturedAt);

    private sealed record RegionSample(double AverageLuminance);

    private sealed record ElementSnapshot(
        string Type,
        string? Name,
        string? HelpText,
        string? AutomationId,
        string? ClassName,
        bool IsEnabled,
        bool IsOffscreen,
        double? X,
        double? Y,
        double? Width,
        double? Height);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private sealed record Viewport(int Width, int Height)
    {
        public static Viewport Default { get; } = new(1300, 850);

        public string Label => $"{Width}x{Height}";

        public static Viewport Parse(string value)
        {
            var parts = value.Trim().Split(['x', 'X', '*'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2
                || !int.TryParse(parts[0], out var width)
                || !int.TryParse(parts[1], out var height)
                || width < 640
                || height < 480
                || width > 8192
                || height > 8192)
            {
                throw new ArgumentException(
                    $"Invalid viewport '{value}'. Expected WIDTHxHEIGHT between 640x480 and 8192x8192.");
            }

            return new Viewport(width, height);
        }
    }

    private sealed record SmokeOptions(
        string RepoRoot,
        string OutputDirectory,
        string Configuration,
        string Host,
        string Theme,
        string ThemeStyle,
        string Language,
        bool AllCultures,
        bool PluginOnly,
        bool OsdOnly,
        bool SettingsOnly,
        string? SwitchTheme,
        bool KeepApp,
        bool ExpectKeyboardNavigation,
        bool NavigationSidebarOnly,
        bool ReadmeScreenshots,
        bool Video,
        bool EnableAnimations,
        Viewport[] Viewports)
    {
        public static SmokeOptions Parse(IReadOnlyList<string> args)
        {
            var repoRoot = ReadOption(args, "--repo-root") ?? Directory.GetCurrentDirectory();
            var configuration = ReadOption(args, "--configuration") ?? "Release";
            var outputDirectory = ReadOption(args, "--output-dir", "--output-directory")
                                  ?? Path.Combine(repoRoot, "Build", "visual-regression-after-wpfui4");
            var host = ReadOption(args, "--host") ?? "wpf";
            if (!host.Equals("wpf", StringComparison.OrdinalIgnoreCase)
                && !host.Equals("avalonia", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Unsupported host '{host}'. Expected 'wpf' or 'avalonia'.");
            }

            var theme = ReadOption(args, "--theme") ?? "Dark";
            var themeStyle = ReadOption(args, "--theme-style") ?? "Default";
            var language = ReadOption(args, "--lang") ?? "en";
            LocalizationCatalog.NormalizeCulture(language);
            var switchTheme = ReadOption(args, "--switch-theme");
            var allCultures = args.Contains("--all-cultures", StringComparer.OrdinalIgnoreCase);
            var pluginOnly = args.Contains("--plugin-only", StringComparer.OrdinalIgnoreCase);
            var osdOnly = args.Contains("--osd-only", StringComparer.OrdinalIgnoreCase);
            var settingsOnly = args.Contains("--settings-only", StringComparer.OrdinalIgnoreCase);
            var navigationSidebarOnly = args.Contains("--navigation-sidebar-only", StringComparer.OrdinalIgnoreCase);
            var readmeScreenshots = args.Contains("--readme-screenshots", StringComparer.OrdinalIgnoreCase);
            var keepApp = args.Contains("--keep-app", StringComparer.OrdinalIgnoreCase);
            var expectKeyboardNavigation = !args.Contains("--expect-no-keyboard-navigation", StringComparer.OrdinalIgnoreCase);
            var video = args.Contains("--video", StringComparer.OrdinalIgnoreCase);
            var enableAnimations = video || args.Contains("--animations", StringComparer.OrdinalIgnoreCase);
            var viewportArgument = ReadOption(args, "--viewports", "--viewport");
            var viewports = string.IsNullOrWhiteSpace(viewportArgument)
                ? [Viewport.Default]
                : viewportArgument
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(Viewport.Parse)
                    .DistinctBy(viewport => viewport.Label, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            return new SmokeOptions(repoRoot, outputDirectory, configuration, host, theme, themeStyle, language, allCultures, pluginOnly, osdOnly, settingsOnly, switchTheme, keepApp, expectKeyboardNavigation, navigationSidebarOnly, readmeScreenshots, video, enableAnimations, viewports);
        }

        private static string? ReadOption(IReadOnlyList<string> args, params string[] names)
        {
            for (var i = 0; i < args.Count; i++)
            {
                foreach (var name in names)
                {
                    if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Count)
                        return args[i + 1];

                    var prefix = $"{name}=";
                    if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        return args[i][prefix.Length..];
                }
            }

            return null;
        }
    }
}
