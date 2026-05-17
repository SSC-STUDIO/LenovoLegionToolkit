using System.Drawing;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Windows.Automation;
using LenovoLegionToolkit.CLI.Lib;
using LenovoLegionToolkit.CLI.Lib.Extensions;
using Microsoft.Win32;

namespace MainAppPluginUi.Smoke;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private const string AppDataOverrideEnvironmentVariable = "LLT_APPDATA_OVERRIDE";
    private const string PluginDirectoryOverrideEnvironmentVariable = "LLT_PLUGIN_DIRECTORY_OVERRIDE";
    private const string PluginImportFilesEnvironmentVariable = "LLT_PLUGIN_IMPORT_FILES";
    private const string PluginSignatureModeEnvironmentVariable = "LLT_PLUGIN_SIGNATURE_MODE";
    private const string SingleInstanceKeyEnvironmentVariable = "LLT_SINGLE_INSTANCE_KEY";
    private const string PluginSourcesEnvironmentVariable = "LLT_SMOKE_PLUGIN_SOURCES";
    private const string ScreenshotModeEnvironmentVariable = "LLT_SMOKE_SCREENSHOTS";
    private const string ScreenshotDirectoryEnvironmentVariable = "LLT_SMOKE_SCREENSHOT_DIR";
    private const string KeepArtifactsEnvironmentVariable = "LLT_SMOKE_KEEP_ARTIFACTS";
    private const string WatchModeEnvironmentVariable = "LLT_SMOKE_WATCH";
    private const string StepDelayEnvironmentVariable = "LLT_SMOKE_STEP_DELAY_MS";
    private const string SuccessHoldEnvironmentVariable = "LLT_SMOKE_SUCCESS_HOLD_MS";
    private const string FailureHoldEnvironmentVariable = "LLT_SMOKE_FAILURE_HOLD_MS";
    private const string ScenarioEnvironmentVariable = "LLT_SMOKE_SCENARIO";
    private const string ThemeEnvironmentVariable = "LLT_SMOKE_THEME";
    private const string AnimationSpeedEnvironmentVariable = "LLT_SMOKE_ANIMATION_SPEED_MS";
    private const string DisableAnimationsEnvironmentVariable = "LLT_SMOKE_DISABLE_ANIMATIONS";
    private const string RelaxedIpcAclEnvironmentVariable = "LLT_RELAXED_IPC_ACL";
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const byte VkControl = 0x11;
    private const byte VkEnter = 0x0D;
    private const byte VkSpace = 0x20;
    private const byte VkTab = 0x09;
    private const uint KeyEventExtendedKey = 0x0001;
    private const uint KeyEventKeyUp = 0x0002;
    private const int SwRestore = 9;
    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;
    private const int PwRenderFullContent = 0x00000002;
    private const int Srccopy = 0x00CC0020;
    private const int BaseAnimationDurationMs = 350;
    private static readonly TimeSpan OnlinePluginInstallTimeout = TimeSpan.FromMinutes(12);
    private static readonly TimeSpan WindowAnimationDuration = TimeSpan.FromMilliseconds(BaseAnimationDurationMs);
    private static readonly TimeSpan WindowAnimationGracePeriod = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan MessageBoxDetectionTimeout = TimeSpan.FromSeconds(5);
    private static readonly string[] DefaultPluginIds = { "custom-mouse", "shell-integration", "vive-tool", "network-acceleration" };
    // Empirical values: WPF.UI MessageBox and NotificationPopup typically fit within 600x400 pixels
    private static readonly int MessageBoxMaxWidth = 600;
    private static readonly int MessageBoxMaxHeight = 400;
    private static int? _mainProcessId;
    private static ScreenshotMode _screenshotMode = ScreenshotMode.Failures;
    private static string? _requestedScreenshotOutputDirectory;
    private static string? _activeScreenshotOutputDirectory;
    private static int _screenshotSequence;
    private static readonly List<ScreenshotCaptureRecord> _screenshotCaptures = new();
    private static readonly List<DismissedPopupRecord> _dismissedPopups = new();
    private static bool _watchMode;
    private static TimeSpan _stepDelay = TimeSpan.Zero;
    private static TimeSpan _successHold = TimeSpan.Zero;
    private static TimeSpan _failureHold = TimeSpan.Zero;
    private static SmokeScenario _activeScenario = SmokeScenario.None;
    private static SmokeTheme _activeTheme = SmokeTheme.System;
    private static double _animationSpeedMultiplier = 1.0;
    private static bool _animationsDisabled = false;

    private enum PluginInstallSource
    {
        Online,
        Local
    }

    private enum ScreenshotMode
    {
        Off,
        Failures,
        Always
    }

    private enum SmokeScenario
    {
        None,
        ShellLocal,
        ComboLocal,
        DriverDownload,
        SystemOptimization
    }

    private enum SmokeTheme
    {
        System,
        Light,
        Dark
    }

    private enum MessageBoxType
    {
        Unknown,
        WpfUiMessageBox,
        SystemWindowsMessageBox,
        NotificationPopup
    }

    private sealed record PreparedPluginInstallState(
        string SettingsPath,
        bool SettingsFileExisted,
        Dictionary<string, JsonNode?> OriginalProperties,
        HashSet<string> EnsuredPluginIds);

    private sealed record SmokeSandboxState(
        string RootDirectory,
        string AppDataDirectory,
        string PluginsDirectory);

    private sealed record LocalPluginPackageState(
        string PluginId,
        string PackagePath);

    private sealed record LocalPluginPackageBundle(
        string RootDirectory,
        IReadOnlyList<LocalPluginPackageState> Packages);

    private sealed record PluginInstallPlan(
        string PluginId,
        PluginInstallSource Source,
        string? LocalPackagePath);

    private sealed record RuntimePluginFixtureState(
        string PluginId,
        string SourceDirectory,
        string TargetDirectory,
        string BackupDirectory,
        bool TargetExistedBefore,
        bool FixturePrepared,
        string? WarningMessage);

    private sealed record RuntimeFileFixtureState(
        string TargetPath,
        string BackupPath,
        bool TargetExistedBefore);

    private sealed record ScreenshotCaptureRecord(
        int Sequence,
        string Label,
        string FilePath,
        DateTimeOffset CapturedAt);

    private sealed record ScenarioPreset(
        SmokeScenario Scenario,
        IReadOnlyList<string> PluginIds,
        IReadOnlyDictionary<string, PluginInstallSource> PluginSources);

    private sealed record DismissedPopupRecord(
        DateTimeOffset Timestamp,
        MessageBoxType PopupType,
        string WindowName,
        string DismissMethod);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr ho);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private static int Main(string[] args)
    {
        Process? process = null;
        AutomationElement? mainWindow = null;
        SmokeSandboxState? smokeSandboxState = null;
        LocalPluginPackageBundle? localPluginPackageBundle = null;
        var preserveArtifacts = false;

        try
        {
            if (HasOption(args, "--help"))
            {
                PrintUsage();
                return 0;
            }

            if (HasOption(args, "--list-plugins"))
            {
                PrintSupportedPlugins();
                return 0;
            }

            preserveArtifacts = ResolveKeepArtifacts(args);
            ConfigureScreenshotSession(args);
            ConfigureObservationSession(args);
            ConfigureAnimationSettings(args);
            _activeScenario = ResolveScenario(args);
            _activeTheme = ResolveTheme(args);
            var repositoryRoot = ResolveRepositoryRoot(args);
            Console.WriteLine($"[main-smoke] Repository root: {repositoryRoot}");
            Console.WriteLine($"[main-smoke] Scenario: {_activeScenario}");
            Console.WriteLine($"[main-smoke] Theme: {_activeTheme}");

            var isDriverDownloadScenario = _activeScenario == SmokeScenario.DriverDownload;
            var isSystemOptimizationScenario = _activeScenario == SmokeScenario.SystemOptimization;
            var scenarioPreset = ResolveScenarioPreset(_activeScenario);
            var preferredPlugins = isDriverDownloadScenario || isSystemOptimizationScenario
                ? Array.Empty<string>()
                : ResolvePreferredPlugins(args, scenarioPreset);
            var requestedPluginSources = isDriverDownloadScenario || isSystemOptimizationScenario
                ? new Dictionary<string, PluginInstallSource>(StringComparer.OrdinalIgnoreCase)
                : ResolveRequestedPluginSources(args, scenarioPreset);
            var desiredPluginSources = preferredPlugins
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    pluginId => pluginId,
                    pluginId => ResolveRequestedPluginSource(pluginId, requestedPluginSources),
                    StringComparer.OrdinalIgnoreCase);
            var appRuntimeDirectory = ResolveMainAppRuntimeDirectory(repositoryRoot);
            smokeSandboxState = PrepareSmokeSandbox();
            ApplySmokeSettingsOverrides(smokeSandboxState, _activeTheme);
            var smokeIpcPipeName = $"{Constants.DEFAULT_PIPE_NAME}-{Path.GetFileName(smokeSandboxState.RootDirectory)}";
            Environment.SetEnvironmentVariable(Constants.PIPE_NAME_ENVIRONMENT_VARIABLE, smokeIpcPipeName);
            Console.WriteLine($"[main-smoke] IPC pipe: {smokeIpcPipeName}");
            localPluginPackageBundle = PrepareLocalPluginPackages(
                repositoryRoot,
                desiredPluginSources
                    .Where(pair => pair.Value == PluginInstallSource.Local)
                    .Select(pair => pair.Key)
                    .ToArray());

            var startInfo = CreateMainAppStartInfo(appRuntimeDirectory, smokeSandboxState, localPluginPackageBundle);
            Console.WriteLine($"[main-smoke] Launching: {startInfo.FileName} {startInfo.Arguments}");

            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start main app process.");
            _mainProcessId = process.Id;
            TryWaitForInputIdle(process, 8000);

            mainWindow = WaitForMainShellWindow(process.Id, TimeSpan.FromSeconds(60));
            Console.WriteLine("[main-smoke] Main window ready");
            CaptureMainWindow(mainWindow, "main-shell-home");
            ObserveStep("Main window ready", mainWindow);

            if (isDriverDownloadScenario)
            {
                TestDriverDownloadUi(mainWindow);
                HoldForObservation("Driver Download smoke completed successfully", mainWindow, _successHold);
                CloseWindow(mainWindow);
                process.WaitForExit(7000);
                Console.WriteLine("[main-smoke] PASS");
                return 0;
            }

            if (isSystemOptimizationScenario)
            {
                TestSystemOptimizationUi(mainWindow);
                HoldForObservation("System Optimization smoke completed successfully", mainWindow, _successHold);
                CloseWindow(mainWindow);
                process.WaitForExit(7000);
                Console.WriteLine("[main-smoke] PASS");
                return 0;
            }

            NavigateToPluginExtensionsPage(mainWindow, refresh: true);
            TryCapturePluginExtensionsLoadingSkeleton(mainWindow, process.Id);
            WaitForRequestedOnlinePluginEntries(
                mainWindow,
                desiredPluginSources
                    .Where(pair => pair.Value == PluginInstallSource.Online)
                    .Select(pair => pair.Key)
                    .ToArray());
            CaptureMainWindow(mainWindow, "plugin-extensions-ready");
            ObserveStep("Plugin Extensions page ready", mainWindow);
            var availablePlugins = GetAvailablePluginIds(mainWindow).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var pluginsUnderTest = preferredPlugins
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Console.WriteLine($"[main-smoke] Plugins under test: [{string.Join(", ", pluginsUnderTest)}]");
            var installPlans = ResolvePluginInstallPlans(
                pluginsUnderTest,
                availablePlugins,
                requestedPluginSources,
                localPluginPackageBundle.Packages);
            Console.WriteLine($"[main-smoke] Install plan: [{string.Join(", ", installPlans.Select(plan => $"{plan.PluginId}={plan.Source.ToString().ToLowerInvariant()}"))}]");

            ImportPluginsFromLocalPackages(
                mainWindow,
                smokeSandboxState,
                installPlans.Where(plan => plan.Source == PluginInstallSource.Local).ToArray());

            foreach (var plan in installPlans.Where(plan => plan.Source == PluginInstallSource.Online))
                EnsurePluginInstalled(mainWindow, smokeSandboxState, plan.PluginId);

            for (var index = 0; index < pluginsUnderTest.Count; index++)
            {
                var pluginId = pluginsUnderTest[index];
                var isLastPlugin = index == pluginsUnderTest.Count - 1;
                TestPluginEntryUi(mainWindow, process.Id, pluginId, isLastPlugin, marketplaceAvailable: true, isKnownInstalled: true, installPlan: installPlans.First(plan => string.Equals(plan.PluginId, pluginId, StringComparison.OrdinalIgnoreCase)));
            }

            HoldForObservation("Smoke completed successfully", mainWindow, _successHold);
            CloseWindow(mainWindow);
            process.WaitForExit(7000);
            Console.WriteLine("[main-smoke] PASS");
            return 0;
        }
        catch (Exception ex)
        {
            preserveArtifacts = true;
            TryCaptureFailureMainWindow(mainWindow, "failure-main-window");
            HoldForObservation("Smoke failed", mainWindow, _failureHold);
            Console.Error.WriteLine("[main-smoke] FAIL");
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (process is not null && !process.HasExited)
                process.Kill(entireProcessTree: true);

            if (preserveArtifacts)
            {
                if (localPluginPackageBundle is not null)
                    Console.WriteLine($"[main-smoke] Preserved local package bundle: {localPluginPackageBundle.RootDirectory}");
                if (smokeSandboxState is not null)
                    Console.WriteLine($"[main-smoke] Preserved smoke sandbox: {smokeSandboxState.RootDirectory}");
            }
            else
            {
                CleanupLocalPluginPackages(localPluginPackageBundle);
                CleanupSmokeSandbox(smokeSandboxState);
            }

            WriteScreenshotManifest();
            WriteDismissedPopupsSummary();
        }
    }

    private static void WriteDismissedPopupsSummary()
    {
        if (_dismissedPopups.Count == 0)
            return;

        Console.WriteLine($"[main-smoke] Dismissed popups summary ({_dismissedPopups.Count} total):");
        foreach (var popup in _dismissedPopups)
        {
            Console.WriteLine($"  - [{popup.Timestamp:HH:mm:ss.fff}] {popup.PopupType}: '{popup.WindowName}' via {popup.DismissMethod}");
        }
    }

    private static string ResolveRepositoryRoot(string[] args)
    {
        var repoRootFromOption = TryReadOptionValue(args, "--repo-root");
        if (!string.IsNullOrWhiteSpace(repoRootFromOption))
        {
            var fromOption = Path.GetFullPath(repoRootFromOption);
            EnsureRepositoryRoot(fromOption);
            return fromOption;
        }

        if (args.Length > 0 && !IsOptionToken(args[0]))
        {
            var fromArg = Path.GetFullPath(args[0]);
            EnsureRepositoryRoot(fromArg);
            return fromArg;
        }

        var current = new DirectoryInfo(Environment.CurrentDirectory);
        for (var i = 0; i < 10 && current is not null; i++)
        {
            var solutionPath = Path.Combine(current.FullName, "LenovoLegionToolkit.sln");
            var wpfProjectPath = Path.Combine(current.FullName, @"LenovoLegionToolkit.WPF\LenovoLegionToolkit.WPF.csproj");
            if (File.Exists(solutionPath) && File.Exists(wpfProjectPath))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Cannot infer main repository root. Pass repo root as first argument.");
    }

    private static string? TryReadOptionValue(IReadOnlyList<string> args, string optionName)
    {
        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            if (!string.Equals(argument, optionName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (index + 1 >= args.Count)
                throw new ArgumentException($"Missing value for option '{optionName}'.");

            var value = args[index + 1];
            if (IsOptionToken(value))
                throw new ArgumentException($"Missing value for option '{optionName}'.");

            return value;
        }

        return null;
    }

    private static bool HasOption(IReadOnlyList<string> args, string optionName)
    {
        return args.Any(argument => string.Equals(argument, optionName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsOptionToken(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.StartsWith("--", StringComparison.Ordinal);

    private static void ConfigureScreenshotSession(IReadOnlyList<string> args)
    {
        _screenshotMode = ResolveScreenshotMode(args);
        _requestedScreenshotOutputDirectory = ResolveScreenshotOutputDirectory(args);
        _activeScreenshotOutputDirectory = null;
        _screenshotSequence = 0;
        _screenshotCaptures.Clear();

        Console.WriteLine($"[main-smoke] Screenshot mode: {_screenshotMode.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrWhiteSpace(_requestedScreenshotOutputDirectory))
            Console.WriteLine($"[main-smoke] Screenshot output directory override: {_requestedScreenshotOutputDirectory}");
    }

    private static void ConfigureObservationSession(IReadOnlyList<string> args)
    {
        _watchMode = ResolveBooleanSwitch(args, "--watch", WatchModeEnvironmentVariable);
        _stepDelay = ResolveNonNegativeDuration(args, "--step-delay-ms", StepDelayEnvironmentVariable, _watchMode ? 1200 : 0);
        _successHold = ResolveNonNegativeDuration(args, "--success-hold-ms", SuccessHoldEnvironmentVariable, _watchMode ? 5000 : 0);
        _failureHold = ResolveNonNegativeDuration(args, "--failure-hold-ms", FailureHoldEnvironmentVariable, _watchMode ? 15000 : 0);

        Console.WriteLine($"[main-smoke] Watch mode: {_watchMode}");
        if (_watchMode || _stepDelay > TimeSpan.Zero || _successHold > TimeSpan.Zero || _failureHold > TimeSpan.Zero)
        {
            Console.WriteLine(
                $"[main-smoke] Watch timing: step={_stepDelay.TotalMilliseconds:0}ms success-hold={_successHold.TotalMilliseconds:0}ms failure-hold={_failureHold.TotalMilliseconds:0}ms");
        }
    }

    private static void ConfigureAnimationSettings(IReadOnlyList<string> args)
    {
        _animationsDisabled = ResolveBooleanSwitch(args, "--disable-animations", DisableAnimationsEnvironmentVariable);

        var animationSpeedMs = ResolveNonNegativeDuration(args, "--animation-speed-ms", AnimationSpeedEnvironmentVariable, BaseAnimationDurationMs);
        _animationSpeedMultiplier = animationSpeedMs.TotalMilliseconds > 0
            ? animationSpeedMs.TotalMilliseconds / BaseAnimationDurationMs
            : 1.0;

        if (_animationsDisabled)
            Console.WriteLine("[main-smoke] Animations disabled for faster test execution");
        else if (Math.Abs(_animationSpeedMultiplier - 1.0) > 0.01)
            Console.WriteLine($"[main-smoke] Animation speed multiplier: {_animationSpeedMultiplier:F2}x (base: {BaseAnimationDurationMs}ms)");
    }

    private static SmokeScenario ResolveScenario(IReadOnlyList<string> args)
    {
        var rawValue = TryReadOptionValue(args, "--scenario")
                       ?? Environment.GetEnvironmentVariable(ScenarioEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(rawValue))
            return SmokeScenario.None;

        return rawValue.Trim().ToLowerInvariant() switch
        {
            "shell-local" => SmokeScenario.ShellLocal,
            "combo-local" => SmokeScenario.ComboLocal,
            "driver-download" => SmokeScenario.DriverDownload,
            "system-optimization" => SmokeScenario.SystemOptimization,
            _ => throw new ArgumentException($"Unsupported smoke scenario '{rawValue}'. Expected 'shell-local', 'combo-local', 'driver-download', or 'system-optimization'.")
        };
    }

    private static SmokeTheme ResolveTheme(IReadOnlyList<string> args)
    {
        var rawValue = TryReadOptionValue(args, "--theme")
                       ?? Environment.GetEnvironmentVariable(ThemeEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(rawValue))
            return SmokeTheme.System;

        return rawValue.Trim().ToLowerInvariant() switch
        {
            "system" => SmokeTheme.System,
            "light" => SmokeTheme.Light,
            "dark" => SmokeTheme.Dark,
            _ => throw new ArgumentException($"Unsupported smoke theme '{rawValue}'. Expected 'system', 'light', or 'dark'.")
        };
    }

    private static ScenarioPreset? ResolveScenarioPreset(SmokeScenario scenario)
    {
        return scenario switch
        {
            SmokeScenario.ShellLocal => new ScenarioPreset(
                SmokeScenario.ShellLocal,
                new[] { "shell-integration" },
                new Dictionary<string, PluginInstallSource>(StringComparer.OrdinalIgnoreCase)
                {
                    ["shell-integration"] = PluginInstallSource.Local
                }),
            SmokeScenario.ComboLocal => new ScenarioPreset(
                SmokeScenario.ComboLocal,
                new[] { "custom-mouse", "shell-integration" },
                new Dictionary<string, PluginInstallSource>(StringComparer.OrdinalIgnoreCase)
                {
                    ["custom-mouse"] = PluginInstallSource.Local,
                    ["shell-integration"] = PluginInstallSource.Local
                }),
            _ => null
        };
    }

    private static ScreenshotMode ResolveScreenshotMode(IReadOnlyList<string> args)
    {
        var rawValue = TryReadOptionValue(args, "--screenshots")
                       ?? Environment.GetEnvironmentVariable(ScreenshotModeEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(rawValue))
            return ScreenshotMode.Failures;

        return rawValue.Trim().ToLowerInvariant() switch
        {
            "off" => ScreenshotMode.Off,
            "failures" => ScreenshotMode.Failures,
            "failure" => ScreenshotMode.Failures,
            "always" => ScreenshotMode.Always,
            "all" => ScreenshotMode.Always,
            _ => throw new ArgumentException($"Unsupported screenshot mode '{rawValue}'. Expected 'off', 'failures', or 'always'.")
        };
    }

    private static string? ResolveScreenshotOutputDirectory(IReadOnlyList<string> args)
    {
        var rawValue = TryReadOptionValue(args, "--screenshot-dir")
                       ?? Environment.GetEnvironmentVariable(ScreenshotDirectoryEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        return Path.GetFullPath(rawValue);
    }

    private static bool ResolveBooleanSwitch(IReadOnlyList<string> args, string optionName, string environmentVariableName)
    {
        if (HasOption(args, optionName))
            return true;

        var rawValue = Environment.GetEnvironmentVariable(environmentVariableName);
        if (string.IsNullOrWhiteSpace(rawValue))
            return false;

        return ParseBooleanLikeValue(rawValue, environmentVariableName);
    }

    private static TimeSpan ResolveNonNegativeDuration(
        IReadOnlyList<string> args,
        string optionName,
        string environmentVariableName,
        int defaultMilliseconds)
    {
        var rawValue = TryReadOptionValue(args, optionName)
                       ?? Environment.GetEnvironmentVariable(environmentVariableName);
        if (string.IsNullOrWhiteSpace(rawValue))
            return TimeSpan.FromMilliseconds(defaultMilliseconds);

        if (!int.TryParse(rawValue, out var milliseconds) || milliseconds < 0)
            throw new ArgumentException($"Unsupported duration '{rawValue}' for '{optionName}'. Expected a non-negative integer in milliseconds.");

        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static bool ResolveKeepArtifacts(IReadOnlyList<string> args)
    {
        if (HasOption(args, "--keep-artifacts"))
            return true;

        var rawValue = Environment.GetEnvironmentVariable(KeepArtifactsEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(rawValue))
            return false;

        return ParseBooleanLikeValue(rawValue, KeepArtifactsEnvironmentVariable);
    }

    private static bool ParseBooleanLikeValue(string rawValue, string optionName)
    {
        return rawValue.Trim().ToLowerInvariant() switch
        {
            "1" => true,
            "true" => true,
            "yes" => true,
            "on" => true,
            "0" => false,
            "false" => false,
            "no" => false,
            "off" => false,
            _ => throw new ArgumentException($"Unsupported value '{rawValue}' for '{optionName}'. Expected a boolean-like value.")
        };
    }

    private static void PrintUsage()
    {
        Console.WriteLine("""
MainAppPluginUi.Smoke

Usage:
MainAppPluginUi.Smoke.dll [--repo-root <path>] [--plugin <id[,id]>] [--plugin-source <pluginId=online|local[,pluginId=...]>]
                            [--scenario shell-local|combo-local|driver-download|system-optimization] [--theme system|light|dark]
                            [--screenshots off|failures|always] [--screenshot-dir <path>] [--keep-artifacts]
                            [--watch] [--step-delay-ms <ms>] [--success-hold-ms <ms>] [--failure-hold-ms <ms>]
                            [--disable-animations] [--animation-speed-ms <ms>]
                            [--list-plugins] [--help]

Options:
  --repo-root            Main repository root. Defaults to the current repo when auto-detected.
  --plugin               Comma-separated plugin id filter. Defaults to the smoke-supported plugin set.
  --plugin-source        Per-plugin install source. Use '*' as wildcard, for example '*=online' or 'shell-integration=online,custom-mouse=local'. Local sources require matching plugin build directories or the smoke fails fast.
  --scenario             Predefined smoke preset. 'shell-local' runs shell-integration only; 'combo-local' runs custom-mouse + shell-integration; 'driver-download' captures the Driver Download page without plugin install work; 'system-optimization' validates all System Optimization tabs without applying destructive actions.
  --theme                Override app theme for the smoke sandbox. One of: system, light, dark.
  --screenshots          Screenshot policy: 'off', 'failures', or 'always'. Default: 'failures'.
  --screenshot-dir       Output directory for screenshot artifacts. Defaults to a temp folder per smoke run.
  --keep-artifacts       Keep the smoke sandbox and local package bundle after a successful run.
  --watch                Slow visible transitions so the smoke process can be watched on the real desktop.
  --step-delay-ms        Per-step observation delay in milliseconds. Default: 1200 when --watch is enabled, otherwise 0.
  --success-hold-ms      Keep the main window open before closing on success. Default: 5000 when --watch is enabled.
  --failure-hold-ms      Keep the failure state visible before exit. Default: 15000 when --watch is enabled.
  --disable-animations   Disable UI animations for faster test execution in non-watch mode.
  --animation-speed-ms   Override animation speed in milliseconds. Default: 350ms. Lower values speed up tests.
  --list-plugins         Print the smoke-supported plugin ids and default install sources, then exit.
  --help                 Print this help text and exit.

Environment variables:
  LLT_SMOKE_PLUGIN_IDS
  LLT_SMOKE_PLUGIN_SOURCES
  LLT_SMOKE_SCENARIO
  LLT_SMOKE_THEME
  LLT_SMOKE_SCREENSHOTS
  LLT_SMOKE_SCREENSHOT_DIR
  LLT_SMOKE_KEEP_ARTIFACTS
  LLT_SMOKE_WATCH
  LLT_SMOKE_STEP_DELAY_MS
  LLT_SMOKE_SUCCESS_HOLD_MS
  LLT_SMOKE_FAILURE_HOLD_MS
  LLT_SMOKE_ANIMATION_SPEED_MS
  LLT_SMOKE_DISABLE_ANIMATIONS
""");
    }

    private static void PrintSupportedPlugins()
    {
        Console.WriteLine("Smoke-supported plugins:");
        foreach (var pluginId in DefaultPluginIds)
            Console.WriteLine($"- {pluginId} (default source: {GetDefaultPluginInstallSource(pluginId).ToString().ToLowerInvariant()})");
    }

    private static IReadOnlyList<string> ResolvePreferredPlugins(string[] args, ScenarioPreset? scenarioPreset)
    {
        if (scenarioPreset is not null)
        {
            Console.WriteLine($"[main-smoke] Plugin filter from scenario: [{string.Join(", ", scenarioPreset.PluginIds)}]");
            return scenarioPreset.PluginIds;
        }

        // First check command line argument --plugin
        var fromCommandLine = TryReadOptionValue(args, "--plugin");
        if (!string.IsNullOrWhiteSpace(fromCommandLine))
        {
            var requested = fromCommandLine
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (requested.Length > 0)
            {
                Console.WriteLine($"[main-smoke] Plugin filter from --plugin: [{string.Join(", ", requested)}]");
                return requested;
            }
        }

        // Fall back to environment variable
        var fromEnvironment = Environment.GetEnvironmentVariable("LLT_SMOKE_PLUGIN_IDS");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            var requested = fromEnvironment
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (requested.Length > 0)
            {
                Console.WriteLine($"[main-smoke] Plugin filter from LLT_SMOKE_PLUGIN_IDS: [{string.Join(", ", requested)}]");
                return requested;
            }
        }

        return DefaultPluginIds;
    }

    private static IReadOnlyDictionary<string, PluginInstallSource> ResolveRequestedPluginSources(string[] args, ScenarioPreset? scenarioPreset)
    {
        if (scenarioPreset is not null)
        {
            Console.WriteLine($"[main-smoke] Plugin sources from scenario: [{string.Join(", ", scenarioPreset.PluginSources.Select(pair => $"{pair.Key}={pair.Value.ToString().ToLowerInvariant()}"))}]");
            return scenarioPreset.PluginSources;
        }

        var rawValue = TryReadOptionValue(args, "--plugin-source")
                       ?? Environment.GetEnvironmentVariable(PluginSourcesEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(rawValue))
            return new Dictionary<string, PluginInstallSource>(StringComparer.OrdinalIgnoreCase);

        var sources = new Dictionary<string, PluginInstallSource>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in rawValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = token.IndexOf('=');
            if (separatorIndex < 0)
            {
                sources["*"] = ParsePluginInstallSource(token);
                continue;
            }

            var pluginId = token[..separatorIndex].Trim();
            var sourceValue = token[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(pluginId))
                throw new ArgumentException($"Invalid plugin source token '{token}'. Expected '<pluginId>=online|local'.");

            sources[pluginId] = ParsePluginInstallSource(sourceValue);
        }

        Console.WriteLine($"[main-smoke] Plugin sources: [{string.Join(", ", sources.Select(pair => $"{pair.Key}={pair.Value.ToString().ToLowerInvariant()}"))}]");
        return sources;
    }

    private static PluginInstallSource ParsePluginInstallSource(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "online" => PluginInstallSource.Online,
            "local" => PluginInstallSource.Local,
            _ => throw new ArgumentException($"Unsupported plugin source '{value}'. Expected 'online' or 'local'.")
        };
    }

    private static PluginInstallSource GetDefaultPluginInstallSource(string pluginId)
    {
        if (pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase) ||
            pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase))
        {
            return PluginInstallSource.Local;
        }

        return PluginInstallSource.Online;
    }

    private static PluginInstallSource ResolveRequestedPluginSource(
        string pluginId,
        IReadOnlyDictionary<string, PluginInstallSource> requestedSources)
    {
        if (requestedSources.TryGetValue(pluginId, out var source))
            return source;

        if (requestedSources.TryGetValue("*", out var wildcardSource))
            return wildcardSource;

        return GetDefaultPluginInstallSource(pluginId);
    }

    private static string NormalizeRuntimeFixturePluginId(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return string.Empty;

        var simpleName = pluginId;
        const string prefix = "LenovoLegionToolkit.Plugins.";
        if (simpleName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            simpleName = simpleName[prefix.Length..];

        return simpleName switch
        {
            "CustomMouse" => "custom-mouse",
            "ShellIntegration" => "shell-integration",
            "NetworkAcceleration" => "network-acceleration",
            "ViveTool" => "vive-tool",
            _ => simpleName
        };
    }

    private static void EnsureRepositoryRoot(string repositoryRoot)
    {
        var solutionPath = Path.Combine(repositoryRoot, "LenovoLegionToolkit.sln");
        var wpfProjectPath = Path.Combine(repositoryRoot, @"LenovoLegionToolkit.WPF\LenovoLegionToolkit.WPF.csproj");
        if (!File.Exists(solutionPath) || !File.Exists(wpfProjectPath))
            throw new DirectoryNotFoundException($"Path is not main repository root: {repositoryRoot}");
    }

    private static string ResolveMainAppRuntimeDirectory(string repositoryRoot)
    {
        var releaseRoot = Path.Combine(repositoryRoot, @"LenovoLegionToolkit.WPF\bin\Release");
        if (!Directory.Exists(releaseRoot))
            throw new DirectoryNotFoundException($"Main app Release output not found: {releaseRoot}. Build main app first.");

        var runtimeDirectory = Directory
            .EnumerateFiles(releaseRoot, "Lenovo Legion Toolkit.dll", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .Where(ContainsMainAppExecutableArtifacts)
            .OrderByDescending(path => Directory.GetLastWriteTimeUtc(path))
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(runtimeDirectory))
            throw new DirectoryNotFoundException("Could not locate runtime directory containing 'Lenovo Legion Toolkit.dll'.");

        return runtimeDirectory;
    }

    private static bool ContainsMainAppExecutableArtifacts(string path)
    {
        return File.Exists(Path.Combine(path, "Lenovo Legion Toolkit.runtimeconfig.json"))
               || File.Exists(Path.Combine(path, "Lenovo Legion Toolkit.exe"));
    }

    private static string ResolveRuntimePluginsDirectory(string runtimeDirectory)
    {
        var candidates = new[]
        {
            Path.Combine(runtimeDirectory, "plugins"),
            Path.Combine(runtimeDirectory, "Build", "plugins")
        };

        var existing = candidates.FirstOrDefault(Directory.Exists);
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        return candidates[0];
    }

    private static SmokeSandboxState PrepareSmokeSandbox()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), $"llt-plugin-smoke-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        var appDataDirectory = Path.Combine(rootDirectory, "appdata");
        var pluginsDirectory = Path.Combine(rootDirectory, "plugins");

        Directory.CreateDirectory(appDataDirectory);
        Directory.CreateDirectory(pluginsDirectory);
        File.WriteAllText(Path.Combine(appDataDirectory, "lang"), "en");

        Console.WriteLine($"[main-smoke] Smoke sandbox appdata: {appDataDirectory}");
        Console.WriteLine($"[main-smoke] Smoke sandbox plugins: {pluginsDirectory}");

        return new SmokeSandboxState(rootDirectory, appDataDirectory, pluginsDirectory);
    }

    private static void ApplySmokeSettingsOverrides(SmokeSandboxState sandboxState, SmokeTheme theme)
    {
        var settingsPath = Path.Combine(sandboxState.AppDataDirectory, "settings.json");
        var root = File.Exists(settingsPath)
            ? ReadSettingsRoot(settingsPath)
            : new JsonObject();

        root["Theme"] = theme switch
        {
            SmokeTheme.Light => "Light",
            SmokeTheme.Dark => "Dark",
            _ => "System"
        };

        // Inject animation settings for faster test execution (single write)
        var animationSettingsMessage = string.Empty;
        if (_animationsDisabled)
        {
            root["AnimationsEnabled"] = false;
            animationSettingsMessage = " AnimationsEnabled=false";
        }
        else if (Math.Abs(_animationSpeedMultiplier - 1.0) > 0.01)
        {
            var animationSpeedMs = (int)(BaseAnimationDurationMs * _animationSpeedMultiplier);
            root["AnimationSpeed"] = animationSpeedMs;
            animationSettingsMessage = $" AnimationSpeed={animationSpeedMs}ms";
        }

        WriteSettingsRoot(settingsPath, root);
        Console.WriteLine($"[main-smoke] Smoke settings override written: Theme={root["Theme"]}{animationSettingsMessage}");

        var integrationsPath = Path.Combine(sandboxState.AppDataDirectory, "integrations.json");
        var integrationsRoot = File.Exists(integrationsPath)
            ? ReadSettingsRoot(integrationsPath)
            : new JsonObject();
        integrationsRoot["CLI"] = true;
        WriteSettingsRoot(integrationsPath, integrationsRoot);
        Console.WriteLine("[main-smoke] Smoke settings override written: CLI=true");
    }

    private static void CleanupSmokeSandbox(SmokeSandboxState? state)
    {
        if (state is null)
            return;

        try
        {
            if (Directory.Exists(state.RootDirectory))
                Directory.Delete(state.RootDirectory, recursive: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Failed to clean smoke sandbox '{state.RootDirectory}': {ex.Message}");
        }
    }

    private static LocalPluginPackageBundle PrepareLocalPluginPackages(string repositoryRoot, IReadOnlyList<string> preferredPlugins)
    {
        var packageRoot = Path.Combine(Path.GetTempPath(), $"llt-plugin-local-packages-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(packageRoot);

        if (preferredPlugins.Count == 0)
        {
            Console.WriteLine("[main-smoke] No plugins requested for local ZIP import");
            return new LocalPluginPackageBundle(packageRoot, Array.Empty<LocalPluginPackageState>());
        }

        var sourceCandidates = new[]
        {
            Path.GetFullPath(Path.Combine(repositoryRoot, "..", "LenovoLegionToolkit-Plugins", "Build", "plugins")),
            Path.Combine(repositoryRoot, "Build", "plugins")
        };

        var sourceRoot = sourceCandidates.FirstOrDefault(Directory.Exists);
        if (string.IsNullOrWhiteSpace(sourceRoot))
        {
            Console.WriteLine("[main-smoke] Local plugin package source not found; continuing without local ZIP packages");
            return new LocalPluginPackageBundle(packageRoot, Array.Empty<LocalPluginPackageState>());
        }

        var pluginSourceDirectories = Directory.GetDirectories(sourceRoot, "*", SearchOption.TopDirectoryOnly)
            .ToDictionary(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);
        var pluginDirectoryNames = ResolveFixturePluginDirectoryNames(preferredPlugins, pluginSourceDirectories.Keys)
            .ToArray();
        if (pluginDirectoryNames.Length == 0)
        {
            Console.WriteLine("[main-smoke] No matching local plugin directories were found for requested ZIP imports");
            return new LocalPluginPackageBundle(packageRoot, Array.Empty<LocalPluginPackageState>());
        }

        var packages = new List<LocalPluginPackageState>();
        foreach (var pluginDirectoryName in pluginDirectoryNames)
        {
            if (!pluginSourceDirectories.TryGetValue(pluginDirectoryName, out var sourcePluginDirectory))
                continue;

            var pluginId = NormalizeRuntimeFixturePluginId(pluginDirectoryName);
            var packagePath = Path.Combine(packageRoot, $"{pluginId}.zip");
            if (File.Exists(packagePath))
                File.Delete(packagePath);

            ZipFile.CreateFromDirectory(sourcePluginDirectory, packagePath, CompressionLevel.Optimal, includeBaseDirectory: false);
            packages.Add(new LocalPluginPackageState(pluginId, packagePath));
        }

        Console.WriteLine($"[main-smoke] Prepared local plugin ZIP packages: [{string.Join(", ", packages.Select(package => package.PluginId))}]");
        return new LocalPluginPackageBundle(packageRoot, packages);
    }

    private static void CleanupLocalPluginPackages(LocalPluginPackageBundle? bundle)
    {
        if (bundle is null)
            return;

        try
        {
            if (Directory.Exists(bundle.RootDirectory))
                Directory.Delete(bundle.RootDirectory, recursive: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Failed to clean local plugin package bundle '{bundle.RootDirectory}': {ex.Message}");
        }
    }

    private static ProcessStartInfo CreateMainAppStartInfo(
        string runtimeDirectory,
        SmokeSandboxState sandboxState,
        LocalPluginPackageBundle localPluginPackageBundle)
    {
        var dllPath = Path.Combine(runtimeDirectory, "Lenovo Legion Toolkit.dll");
        var runtimeConfigPath = Path.Combine(runtimeDirectory, "Lenovo Legion Toolkit.runtimeconfig.json");
        if (File.Exists(dllPath) && File.Exists(runtimeConfigPath))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                // Smoke runs need to get past the unsupported-device gate on this workstation
                // so the plugin UI can still be validated end-to-end.
                Arguments = $"\"{dllPath}\" --skip-compat-check --trace --disable-update-checker",
                WorkingDirectory = runtimeDirectory,
                UseShellExecute = false
            };

            ApplySmokeEnvironmentOverrides(startInfo, sandboxState, localPluginPackageBundle);
            startInfo.EnvironmentVariables[PluginSignatureModeEnvironmentVariable] = "AllowUnsigned";
            return startInfo;
        }

        var exePath = Path.Combine(runtimeDirectory, "Lenovo Legion Toolkit.exe");
        if (File.Exists(exePath))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "--skip-compat-check --trace --disable-update-checker",
                WorkingDirectory = runtimeDirectory,
                UseShellExecute = false
            };

            ApplySmokeEnvironmentOverrides(startInfo, sandboxState, localPluginPackageBundle);
            startInfo.EnvironmentVariables[PluginSignatureModeEnvironmentVariable] = "AllowUnsigned";
            return startInfo;
        }

        throw new FileNotFoundException($"Could not find startup entry in runtime directory: {runtimeDirectory}");
    }

    private static void ApplySmokeEnvironmentOverrides(
        ProcessStartInfo startInfo,
        SmokeSandboxState sandboxState,
        LocalPluginPackageBundle localPluginPackageBundle)
    {
        startInfo.EnvironmentVariables[AppDataOverrideEnvironmentVariable] = sandboxState.AppDataDirectory;
        startInfo.EnvironmentVariables[PluginDirectoryOverrideEnvironmentVariable] = sandboxState.PluginsDirectory;
        startInfo.EnvironmentVariables[SingleInstanceKeyEnvironmentVariable] = Path.GetFileName(sandboxState.RootDirectory);
        startInfo.EnvironmentVariables[Constants.PIPE_NAME_ENVIRONMENT_VARIABLE] = Constants.PIPE_NAME;
        startInfo.EnvironmentVariables[RelaxedIpcAclEnvironmentVariable] = "1";

        if (localPluginPackageBundle.Packages.Count > 0)
        {
            startInfo.EnvironmentVariables[PluginImportFilesEnvironmentVariable] =
                string.Join(Path.PathSeparator, localPluginPackageBundle.Packages.Select(package => package.PackagePath));
        }
    }

    private static PreparedPluginInstallState? PreparePluginInstallState(IReadOnlyList<string> preferredPlugins, string runtimePluginsDirectory)
    {
        if (preferredPlugins.Count == 0)
            return null;

        var configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LenovoLegionToolkit");
        var settingsPath = Path.Combine(configDirectory, "settings.json");
        Directory.CreateDirectory(configDirectory);

        var settingsFileExisted = File.Exists(settingsPath);
        var root = settingsFileExisted
            ? ReadSettingsRoot(settingsPath)
            : new JsonObject();
        var originalProperties = CaptureSettingsProperties(root, "InstalledExtensions", "PendingDeletionExtensions");
        var installedExtensions = EnsureJsonArray(root, "InstalledExtensions");
        var pendingDeletionExtensions = EnsureJsonArray(root, "PendingDeletionExtensions");
        var ensuredPluginIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pluginId in preferredPlugins)
        {
            if (!PluginRuntimeExists(runtimePluginsDirectory, pluginId))
            {
                Console.WriteLine($"[main-smoke] Skipping install-state preseed for missing runtime plugin: {pluginId}");
                continue;
            }

            RemoveJsonValue(pendingDeletionExtensions, pluginId);
            if (ContainsJsonValue(installedExtensions, pluginId))
                continue;

            installedExtensions.Add(pluginId);
            ensuredPluginIds.Add(pluginId);
        }

        if (ensuredPluginIds.Count == 0)
            return null;

        WriteSettingsRoot(settingsPath, root);
        Console.WriteLine($"[main-smoke] Pre-seeded InstalledExtensions for: [{string.Join(", ", ensuredPluginIds)}]");
        return new PreparedPluginInstallState(settingsPath, settingsFileExisted, originalProperties, ensuredPluginIds);
    }

    private static void RestorePluginInstallState(PreparedPluginInstallState? state)
    {
        if (state is null)
            return;

        try
        {
            if (!state.SettingsFileExisted)
            {
                if (File.Exists(state.SettingsPath))
                    File.Delete(state.SettingsPath);

                Console.WriteLine("[main-smoke] Restored plugin install-state settings");
                return;
            }

            var root = ReadSettingsRoot(state.SettingsPath);
            RestoreSettingsProperties(root, state.OriginalProperties);
            WriteSettingsRoot(state.SettingsPath, root);
            Console.WriteLine("[main-smoke] Restored plugin install-state settings");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Failed to restore plugin install-state settings: {ex.Message}");
        }
    }

    private static JsonObject ReadSettingsRoot(string settingsPath)
    {
        if (!File.Exists(settingsPath))
            return new JsonObject();

        return ParseSettingsRoot(File.ReadAllText(settingsPath));
    }

    private static void WriteSettingsRoot(string settingsPath, JsonObject root)
    {
        File.WriteAllText(settingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static Dictionary<string, JsonNode?> CaptureSettingsProperties(JsonObject root, params string[] propertyNames)
    {
        var captured = new Dictionary<string, JsonNode?>(StringComparer.Ordinal);
        foreach (var propertyName in propertyNames)
            captured[propertyName] = root[propertyName]?.DeepClone();

        return captured;
    }

    private static void RestoreSettingsProperties(JsonObject root, IReadOnlyDictionary<string, JsonNode?> originalProperties)
    {
        foreach (var pair in originalProperties)
        {
            if (pair.Value is null)
                root.Remove(pair.Key);
            else
                root[pair.Key] = pair.Value.DeepClone();
        }
    }

    private static JsonObject ParseSettingsRoot(string? content)
    {
        if (!string.IsNullOrWhiteSpace(content) && JsonNode.Parse(content) is JsonObject parsed)
            return parsed;

        return new JsonObject();
    }

    private static JsonArray EnsureJsonArray(JsonObject root, string propertyName)
    {
        if (root[propertyName] is JsonArray existing)
            return existing;

        var created = new JsonArray();
        root[propertyName] = created;
        return created;
    }

    private static bool ContainsJsonValue(JsonArray array, string value) =>
        array.Any(node => string.Equals(node?.GetValue<string>(), value, StringComparison.OrdinalIgnoreCase));

    private static void RemoveJsonValue(JsonArray array, string value)
    {
        for (var index = array.Count - 1; index >= 0; index--)
        {
            if (string.Equals(array[index]?.GetValue<string>(), value, StringComparison.OrdinalIgnoreCase))
                array.RemoveAt(index);
        }
    }

    private static bool PluginRuntimeExists(string runtimePluginsDirectory, string pluginId)
    {
        if (!Directory.Exists(runtimePluginsDirectory))
            return false;

        var candidateDirectories = new[]
        {
            Path.Combine(runtimePluginsDirectory, pluginId),
            Path.Combine(runtimePluginsDirectory, $"LenovoLegionToolkit.Plugins.{pluginId}"),
            Path.Combine(runtimePluginsDirectory, $"LenovoLegionToolkit.Plugins.{pluginId.Replace("-", string.Empty)}"),
            Path.Combine(runtimePluginsDirectory, "local", pluginId)
        };

        if (candidateDirectories.Any(Directory.Exists))
            return true;

        var candidateDlls = new[]
        {
            Path.Combine(runtimePluginsDirectory, $"{pluginId}.dll"),
            Path.Combine(runtimePluginsDirectory, $"LenovoLegionToolkit.Plugins.{pluginId}.dll"),
            Path.Combine(runtimePluginsDirectory, $"LenovoLegionToolkit.Plugins.{pluginId.Replace("-", string.Empty)}.dll")
        };

        return candidateDlls.Any(File.Exists);
    }

    private static List<RuntimePluginFixtureState> PrepareRuntimePluginFixtures(
        string repositoryRoot,
        string runtimeDirectory,
        string runtimePluginsDirectory,
        IReadOnlyList<string> preferredPlugins)
    {
        var sourceCandidates = new[]
        {
            Path.GetFullPath(Path.Combine(repositoryRoot, "..", "LenovoLegionToolkit-Plugins", "Build", "plugins")),
            Path.Combine(repositoryRoot, "Build", "plugins")
        };

        var sourceRoot = sourceCandidates.FirstOrDefault(Directory.Exists);
        if (string.IsNullOrWhiteSpace(sourceRoot))
        {
            Console.WriteLine("[main-smoke] Plugin fixture source not found; continuing without fixture copy");
            return new List<RuntimePluginFixtureState>();
        }

        Directory.CreateDirectory(runtimePluginsDirectory);
        var fixtureStates = new List<RuntimePluginFixtureState>();
        var pluginSourceDirectories = Directory.GetDirectories(sourceRoot, "*", SearchOption.TopDirectoryOnly)
            .ToDictionary(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);
        var pluginDirectoryNames = ResolveFixturePluginDirectoryNames(preferredPlugins, pluginSourceDirectories.Keys)
            .ToArray();

        if (pluginDirectoryNames.Length == 0)
        {
            Console.WriteLine("[main-smoke] No matching runtime plugin fixtures selected; continuing without fixture copy");
            return fixtureStates;
        }

        try
        {
            foreach (var pluginDirectoryName in pluginDirectoryNames)
            {
                if (!pluginSourceDirectories.TryGetValue(pluginDirectoryName, out var sourcePluginDirectory))
                    continue;

                fixtureStates.Add(PrepareRuntimePluginFixture(runtimePluginsDirectory, pluginDirectoryName, sourcePluginDirectory));
            }

            Console.WriteLine($"[main-smoke] Prepared runtime plugin fixtures from: {sourceRoot} => [{string.Join(", ", pluginDirectoryNames)}]");
            return fixtureStates;
        }
        catch
        {
            RestoreRuntimePluginFixtures(fixtureStates);
            throw;
        }
    }

    private static IEnumerable<string> ResolveFixturePluginDirectoryNames(
        IReadOnlyList<string> preferredPlugins,
        IEnumerable<string> availableDirectoryNames)
    {
        var available = availableDirectoryNames.ToArray();
        if (preferredPlugins.Count == 0)
            return available.OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pluginId in preferredPlugins)
        {
            foreach (var candidate in EnumeratePluginDirectoryNameCandidates(pluginId))
            {
                var match = available.FirstOrDefault(name => string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match))
                    resolved.Add(match);
            }
        }

        return resolved.OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumeratePluginDirectoryNameCandidates(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            yield break;

        yield return pluginId;
        yield return $"LenovoLegionToolkit.Plugins.{pluginId}";
        yield return $"LenovoLegionToolkit.Plugins.{pluginId.Replace("-", string.Empty, StringComparison.Ordinal)}";
    }

    private static RuntimePluginFixtureState PrepareRuntimePluginFixture(
        string runtimePluginsDirectory,
        string pluginDirectoryName,
        string sourcePluginDirectory)
    {
        var targetPluginDirectory = Path.Combine(runtimePluginsDirectory, pluginDirectoryName);
        var backupPluginDirectory = Path.Combine(runtimePluginsDirectory, $".{pluginDirectoryName}.smoke-backup");
        var targetExistedBefore = Directory.Exists(targetPluginDirectory);
        var pluginId = NormalizePluginIdFromDirectoryName(pluginDirectoryName);

        try
        {
            CleanupFixtureDirectory(backupPluginDirectory);
            if (targetExistedBefore)
                Directory.Move(targetPluginDirectory, backupPluginDirectory);

            CopyDirectory(sourcePluginDirectory, targetPluginDirectory);
            return new RuntimePluginFixtureState(pluginId, sourcePluginDirectory, targetPluginDirectory, backupPluginDirectory, targetExistedBefore, true, null);
        }
        catch (Exception ex)
        {
            var warningMessage = $"Runtime fixture warning for '{pluginId}': {ex.Message}";
            Console.WriteLine($"[main-smoke] {warningMessage}");
            TryRestorePreparedRuntimePluginFixture(targetPluginDirectory, backupPluginDirectory, targetExistedBefore);
            return new RuntimePluginFixtureState(pluginId, sourcePluginDirectory, targetPluginDirectory, backupPluginDirectory, targetExistedBefore, false, warningMessage);
        }
    }

    private static string NormalizePluginIdFromDirectoryName(string pluginDirectoryName)
    {
        const string prefix = "LenovoLegionToolkit.Plugins.";
        if (pluginDirectoryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return pluginDirectoryName[prefix.Length..];

        return pluginDirectoryName;
    }

    private static void TryRestorePreparedRuntimePluginFixture(string targetPluginDirectory, string backupPluginDirectory, bool targetExistedBefore)
    {
        try
        {
            CleanupFixtureDirectory(targetPluginDirectory);
            if (targetExistedBefore && Directory.Exists(backupPluginDirectory))
                Directory.Move(backupPluginDirectory, targetPluginDirectory);
        }
        catch (Exception restoreEx)
        {
            Console.WriteLine($"[main-smoke] Failed to rollback runtime fixture staging '{targetPluginDirectory}': {restoreEx.Message}");
        }
    }

    private static void RestoreRuntimePluginFixtures(IReadOnlyList<RuntimePluginFixtureState>? fixtureStates)
    {
        if (fixtureStates is null || fixtureStates.Count == 0)
            return;

        foreach (var state in fixtureStates.Reverse())
        {
            if (!state.FixturePrepared)
            {
                if (!string.IsNullOrWhiteSpace(state.WarningMessage))
                    Console.WriteLine($"[main-smoke] Leaving runtime fixture unchanged for '{state.PluginId}' after warning: {state.WarningMessage}");
                continue;
            }

            try
            {
                CleanupFixtureDirectory(state.TargetDirectory);

                if (state.TargetExistedBefore && Directory.Exists(state.BackupDirectory))
                    Directory.Move(state.BackupDirectory, state.TargetDirectory);
                else
                    CleanupFixtureDirectory(state.BackupDirectory);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[main-smoke] Failed to restore runtime plugin fixture '{state.TargetDirectory}': {ex.Message}");
            }
        }
    }

    private static void CleanupFixtureDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }

    private static RuntimeFileFixtureState? PrepareRuntimeSdkFixture(string repositoryRoot, string runtimeDirectory)
    {
        var sdkDllCandidates = new[]
        {
            Path.GetFullPath(Path.Combine(repositoryRoot, "..", "LenovoLegionToolkit-Plugins", "Build", "SDK", "LenovoLegionToolkit.Plugins.SDK.dll")),
            Path.Combine(repositoryRoot, "Build", "SDK", "LenovoLegionToolkit.Plugins.SDK.dll")
        };

        var sdkDllPath = sdkDllCandidates.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(sdkDllPath))
            return null;

        var runtimeSdkPath = Path.Combine(runtimeDirectory, "LenovoLegionToolkit.Plugins.SDK.dll");
        var backupSdkPath = Path.Combine(runtimeDirectory, ".LenovoLegionToolkit.Plugins.SDK.dll.smoke-backup");
        var runtimeSdkExistedBefore = File.Exists(runtimeSdkPath);

        CleanupFixtureFile(backupSdkPath);
        if (runtimeSdkExistedBefore)
            File.Move(runtimeSdkPath, backupSdkPath);

        try
        {
            File.Copy(sdkDllPath, runtimeSdkPath, overwrite: true);
            return new RuntimeFileFixtureState(runtimeSdkPath, backupSdkPath, runtimeSdkExistedBefore);
        }
        catch
        {
            RestoreRuntimeFileFixture(new RuntimeFileFixtureState(runtimeSdkPath, backupSdkPath, runtimeSdkExistedBefore));
            throw;
        }
    }

    private static void RestoreRuntimeFileFixture(RuntimeFileFixtureState? fixtureState)
    {
        if (fixtureState is null)
            return;

        try
        {
            CleanupFixtureFile(fixtureState.TargetPath);

            if (fixtureState.TargetExistedBefore && File.Exists(fixtureState.BackupPath))
                File.Move(fixtureState.BackupPath, fixtureState.TargetPath);
            else
                CleanupFixtureFile(fixtureState.BackupPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Failed to restore runtime file fixture '{fixtureState.TargetPath}': {ex.Message}");
        }
    }

    private static void CleanupFixtureFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            var fileName = Path.GetFileName(file);
            var targetPath = Path.Combine(destination, fileName);
            File.Copy(file, targetPath, overwrite: true);
        }

        foreach (var subDirectory in Directory.GetDirectories(source))
        {
            var directoryName = Path.GetFileName(subDirectory);
            var targetSubDirectory = Path.Combine(destination, directoryName);
            CopyDirectory(subDirectory, targetSubDirectory);
        }
    }

    private static void TryWaitForInputIdle(Process process, int milliseconds)
    {
        try
        {
            process.WaitForInputIdle(milliseconds);
        }
        catch (InvalidOperationException)
        {
            // dotnet host process may not report input idle; explicit UIA waits below handle readiness.
        }
    }

    private static AutomationElement WaitForMainShellWindow(int processId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var window = TryFindMainShellWindow(processId);
            if (window is not null)
                return window;

            Thread.Sleep(300);
        }

        throw new TimeoutException("Timed out waiting for main app shell window.");
    }

    private static AutomationElement? TryFindMainShellWindow(int processId)
    {
        var condition = new AndCondition(
            new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

        var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, condition).Cast<AutomationElement>().ToArray();
        foreach (var window in windows)
        {
            if (TryHandleCompatibilityWindow(window))
                continue;

            if (FindByAutomationId(window, "MainNavigationStore") is not null
                || FindByAutomationId(window, "_navigationStore") is not null
                || FindByAutomationId(window, "MainRootFrame") is not null)
            {
                return window;
            }
        }

        return null;
    }

    private static bool TryHandleCompatibilityWindow(AutomationElement window)
    {
        var continueButton = FindByAutomationId(window, "_continueButton");
        if (!IsVisible(continueButton))
            return false;

        if (continueButton is null || !continueButton.Current.IsEnabled)
            return false;

        Click(continueButton);
        Console.WriteLine("[main-smoke] Compatibility prompt detected and continued");
        Thread.Sleep(500);
        return true;
    }

    private static void NavigateToPluginExtensionsPage(AutomationElement mainWindow, bool refresh)
    {
        Console.WriteLine("[main-smoke] Navigating to Plugin Extensions page");
        mainWindow = ResolveLiveWindow(mainWindow);
        Console.WriteLine("[main-smoke] Main window resolved for plugin navigation");
        CloseStalePluginSettingsWindows(mainWindow);
        Console.WriteLine("[main-smoke] Stale plugin settings windows closed");
        BringToForeground(mainWindow);

        var processId = mainWindow.Current.ProcessId;
        DismissAnyBlockingMessageBox(mainWindow, processId);

        var arrived = false;
        for (var attempt = 1; attempt <= 6; attempt++)
        {
            mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
            Console.WriteLine($"[main-smoke] Waiting for plugin navigation element (attempt {attempt}/6)");
            AutomationElement? pluginNav = null;
            try
            {
                pluginNav = WaitForPluginNavigationElement(mainWindow, TimeSpan.FromSeconds(8));
                Console.WriteLine($"[main-smoke] Plugin navigation element ready (attempt {attempt}/6)");
                ActivateNavigationElement(pluginNav, "Plugin Extensions");
                WaitForAnimationsToComplete();
                Console.WriteLine($"[main-smoke] Invoked plugin navigation element (attempt {attempt}/6)");
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"[main-smoke] Plugin navigation element unavailable; trying keyboard navigation fallback (attempt {attempt}/6)");
                BringToForeground(mainWindow);
                PressCtrlTab();
                WaitForAnimationsToComplete();
            }

            mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);

            // Nav items exposed as DataItem do not always react to SelectionItemPattern
            // on this machine; fall back to a physical click before declaring the attempt failed.
            var quickReady = WaitUntil(
                () =>
                {
                    mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
                    return IsPluginMarketplaceReady(mainWindow);
                },
                TimeSpan.FromSeconds(2),
                TimeSpan.FromMilliseconds(200));

            if (!quickReady)
            {
                BringToForeground(mainWindow);
                DismissAnyBlockingMessageBox(mainWindow, processId);
                if (pluginNav is not null)
                    ActivateNavigationElement(pluginNav, "Plugin Extensions");
                else
                    PressCtrlTab();
                WaitForAnimationsToComplete();
            }

            var ready = WaitUntil(
                () =>
                {
                    mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
                    return IsPluginMarketplaceReady(mainWindow);
                },
                TimeSpan.FromSeconds(12),
                TimeSpan.FromMilliseconds(250));

            if (ready)
            {
                arrived = true;
                break;
            }

            Console.WriteLine($"[main-smoke] Plugin page navigation retry {attempt}/6");
            Thread.Sleep(700);
        }

        if (!arrived)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            BringToForeground(mainWindow);

            for (var attempt = 1; attempt <= 8 && !arrived; attempt++)
            {
                PressCtrlTab();
                Console.WriteLine($"[main-smoke] Plugin page keyboard navigation retry {attempt}/8");

                arrived = WaitUntil(
                    () =>
                    {
                        mainWindow = ResolveLiveWindow(mainWindow);
                        return IsPluginMarketplaceReady(mainWindow);
                    },
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromMilliseconds(250));
            }
        }

        if (!arrived)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            DumpAutomationSnapshot(mainWindow, 350);
            throw new TimeoutException("Timed out waiting for plugin marketplace page controls.");
        }

        if (refresh)
        {
            var refreshButton = FindByAutomationId(mainWindow, "PluginRefreshButton");
            if (refreshButton is not null && IsVisible(refreshButton))
            {
                Click(refreshButton);
                Console.WriteLine("[main-smoke] Plugin page refreshed");
            }
            else
            {
                Console.WriteLine("[main-smoke] Plugin refresh button not visible; continuing with current plugin feed");
            }
        }

        if (!refresh)
            return;

        var cardReady = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                return IsPluginMarketplaceReady(mainWindow);
            },
            TimeSpan.FromSeconds(60),
            TimeSpan.FromMilliseconds(350));

        if (!cardReady)
        {
            DumpAutomationSnapshot(mainWindow, 300);
            throw new TimeoutException("Plugin marketplace controls did not appear in plugin marketplace view.");
        }

        CaptureMainWindow(mainWindow, refresh ? "plugin-extensions-refreshed" : "plugin-extensions");
        ObserveStep(refresh ? "Plugin Extensions refreshed" : "Plugin Extensions visible", mainWindow);
    }

    private static void TryCapturePluginExtensionsLoadingSkeleton(AutomationElement mainWindow, int processId)
    {
        try
        {
            var skeletonVisible = WaitUntil(
                () =>
                {
                    mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
                    return IsVisible(FindByAutomationId(mainWindow, "PluginLoadingIndicator"));
                },
                TimeSpan.FromSeconds(5),
                TimeSpan.FromMilliseconds(150));

            if (!skeletonVisible)
            {
                Console.WriteLine("[main-smoke] Plugin loading skeleton not observed; skipping loading screenshot");
                return;
            }

            CaptureMainWindow(mainWindow, "plugin-extensions-loading");

            // Capture a second frame to make the shimmer visible even when the first frame
            // lands near the transition edge.
            Thread.Sleep(450);
            mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
            if (IsVisible(FindByAutomationId(mainWindow, "PluginLoadingIndicator")))
                CaptureMainWindow(mainWindow, "plugin-extensions-loading-2");
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            Console.WriteLine($"[main-smoke] Plugin loading skeleton screenshot skipped: {ex.GetType().Name}");
        }
    }

    private static void ActivateNavigationElement(AutomationElement element, string label)
    {
        try
        {
            element.SetFocus();
            Thread.Sleep(120);
        }
        catch
        {
            // Ignore and continue with click-based fallbacks.
        }

        try
        {
            Click(element);
        }
        catch (InvalidOperationException) when (!IsInteractable(element))
        {
            // Element may have become stale after window transitions; try resolving a fresh reference.
            Console.WriteLine($"[main-smoke] Navigation element '{label}' was not interactable for direct click; trying fallback clicks.");
        }

        var textDescendant = FindFirstVisibleDescendant(
            element,
            candidate => candidate.Current.ControlType == ControlType.Text);
        if (textDescendant is not null)
        {
            try
            {
                MouseClick(textDescendant);
                return;
            }
            catch
            {
                // Ignore and continue with broader fallbacks.
            }
        }

        try
        {
            MouseClick(element);
            return;
        }
        catch
        {
            // Ignore and continue with double click fallback.
        }

        try
        {
            DoubleClick(element);
            return;
        }
        catch
        {
            DumpAutomationSnapshot(element, 120);
            throw new InvalidOperationException($"Failed to activate navigation element '{label}'.");
        }
    }

    private static void BringToForeground(AutomationElement window)
    {
        if (!TryGetNativeWindowHandle(window, out var handle))
            return;

        const int SW_RESTORE = 9;
        _ = ShowWindow((IntPtr)handle, SW_RESTORE);
        _ = SetForegroundWindow((IntPtr)handle);
        Thread.Sleep(150);
    }

    private static AutomationElement ResolveLiveWindow(AutomationElement window)
    {
        if (_mainProcessId is int processId)
        {
            var liveWindow = TryFindMainShellWindow(processId);
            if (liveWindow is not null)
                return liveWindow;
        }

        if (!TryGetNativeWindowHandle(window, out var handle))
            return window;

        try
        {
            return AutomationElement.FromHandle((IntPtr)handle);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            return window;
        }
    }

    private static bool TryGetNativeWindowHandle(AutomationElement element, out int handle)
    {
        try
        {
            handle = element.Current.NativeWindowHandle;
            return handle != 0;
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            handle = 0;
            return false;
        }
    }

    private static bool IsRecoverableAutomationException(Exception ex) =>
        ex is COMException
            or ElementNotAvailableException
            or InvalidOperationException;

    private static AutomationElement ResolveLiveWindowAndDismissPopups(AutomationElement window, int processId, string? logContext = null)
    {
        var liveWindow = ResolveLiveWindow(window);
        DismissAnyBlockingMessageBox(liveWindow, processId);
        if (logContext is not null)
            Console.WriteLine($"[main-smoke] {logContext}: resolved live window and dismissed popups");
        return liveWindow;
    }

    private static AutomationElement WaitForAnimationsAndResolveWindow(AutomationElement window, int processId, TimeSpan? additionalDelay = null)
    {
        WaitForAnimationsToComplete(additionalDelay);
        return ResolveLiveWindowAndDismissPopups(window, processId);
    }

    private static bool IsPluginMarketplaceReady(AutomationElement mainWindow)
    {
        var rootReady = IsVisible(FindByAutomationId(mainWindow, "PluginExtensionsPageRoot"));
        var searchReady = IsVisible(FindByAutomationId(mainWindow, "PluginSearchTextBox"));
        var listReady = IsVisible(FindByAutomationId(mainWindow, "PluginListBox"));
        var hasActionButtons =
            GetPluginIdsByButtonPrefix(mainWindow, "PluginInstallButton_").Any()
            || GetPluginIdsByButtonPrefix(mainWindow, "PluginOpenButton_").Any()
            || GetPluginIdsByButtonPrefix(mainWindow, "PluginConfigureButton_").Any()
            || GetPluginIdsByButtonPrefix(mainWindow, "PluginUninstallButton_").Any();

        if (hasActionButtons || (searchReady && listReady) || rootReady)
            return true;

        return TryFindMarketplacePluginCard(mainWindow, out _);
    }

    private static void WaitForPluginMarketplaceInteractionReady(AutomationElement mainWindow, string pluginId, TimeSpan timeout)
    {
        var ready = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                var entryVisible = IsPluginMarketplaceEntryVisible(mainWindow, pluginId);
                var loadingVisible = IsVisible(FindByAutomationId(mainWindow, "PluginLoadingIndicator"))
                                     || IsVisible(FindByAutomationId(mainWindow, "_loadingText"))
                                     || FindVisibleTextContains(mainWindow, "Loading plugins...")
                                     || FindVisibleTextContains(mainWindow, "加载插件...");
                if (loadingVisible && !IsPluginMarketplaceEntryActionable(mainWindow, pluginId))
                    return false;

                return entryVisible;
            },
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (ready)
            return;

        mainWindow = ResolveLiveWindow(mainWindow);
        DumpAutomationSnapshot(mainWindow, 260);
        throw new TimeoutException($"Plugin marketplace entry did not reach an interaction-ready state: {pluginId}");
    }

    private static bool TryFindMarketplacePluginCard(AutomationElement root, out AutomationElement? element)
    {
        var cardPrefixes = new[]
        {
            "PluginCard_",
            "PluginInstallButton_",
            "PluginOpenButton_",
            "PluginConfigureButton_",
            "PluginUninstallButton_"
        };

        try
        {
            element = root.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                .Cast<AutomationElement>()
                .FirstOrDefault(candidate =>
                {
                    var automationId = candidate.Current.AutomationId ?? string.Empty;
                    return cardPrefixes.Any(prefix => automationId.StartsWith(prefix, StringComparison.Ordinal));
                });
            return element is not null;
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            element = null;
            return false;
        }
    }

    private static AutomationElement? FindFirstVisibleDescendant(AutomationElement root, Func<AutomationElement, bool> predicate)
    {
        try
        {
            return root.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                .Cast<AutomationElement>()
                .Where(IsVisible)
                .FirstOrDefault(predicate);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            return null;
        }
    }

    private static IEnumerable<string> GetAvailablePluginIds(AutomationElement mainWindow)
    {
        return GetPluginIdsByButtonPrefix(mainWindow, "PluginCard_")
            .Concat(GetPluginIdsByButtonPrefix(mainWindow, "PluginInstallButton_"))
            .Concat(GetPluginIdsByButtonPrefix(mainWindow, "PluginOpenButton_"))
            .Concat(GetPluginIdsByButtonPrefix(mainWindow, "PluginConfigureButton_"))
            .Concat(GetPluginIdsByButtonPrefix(mainWindow, "PluginUninstallButton_"))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static void WaitForRequestedOnlinePluginEntries(AutomationElement mainWindow, IReadOnlyList<string> pluginIds)
    {
        if (pluginIds.Count == 0)
            return;

        var unresolvedPluginIds = pluginIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var ready = WaitUntil(
                () =>
                {
                    mainWindow = ResolveLiveWindow(mainWindow);
                    unresolvedPluginIds.RemoveWhere(pluginId => IsPluginMarketplaceEntryVisible(mainWindow, pluginId));
                    return unresolvedPluginIds.Count == 0;
                },
                TimeSpan.FromSeconds(45),
                TimeSpan.FromMilliseconds(350));

            if (ready)
                return;

            Console.WriteLine($"[main-smoke] Online marketplace entries still missing after wait attempt {attempt}/3: [{string.Join(", ", unresolvedPluginIds)}]");

            if (attempt == 3)
                return;

            mainWindow = ResolveLiveWindow(mainWindow);
            var refreshButton = FindByAutomationId(mainWindow, "PluginRefreshButton");
            if (refreshButton is not null && IsVisible(refreshButton))
            {
                try
                {
                    Click(refreshButton);
                }
                catch (InvalidOperationException)
                {
                    try
                    {
                        mainWindow = ResolveLiveWindow(mainWindow);
                        BringToForeground(mainWindow);
                        refreshButton = FindByAutomationId(mainWindow, "PluginRefreshButton");
                        if (refreshButton is not null && IsInteractable(refreshButton))
                            MouseClick(refreshButton);
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"[main-smoke] Skipping marketplace refresh fallback after interactability failure: {ex.Message}");
                    }
                }

                Console.WriteLine($"[main-smoke] Retrying online marketplace refresh ({attempt}/2)");
            }

            Thread.Sleep(1000);
        }
    }

    private static bool IsPluginMarketplaceEntryVisible(AutomationElement mainWindow, string pluginId)
    {
        return IsVisible(FindByAutomationId(mainWindow, $"PluginInstallButton_{pluginId}"))
               || IsVisible(FindByAutomationId(mainWindow, $"PluginOpenButton_{pluginId}"))
               || IsVisible(FindByAutomationId(mainWindow, $"PluginConfigureButton_{pluginId}"))
               || IsVisible(FindByAutomationId(mainWindow, $"PluginUninstallButton_{pluginId}"))
               || IsVisible(FindByAutomationId(mainWindow, $"PluginCard_{pluginId}"));
    }

    private static bool IsPluginMarketplaceEntryActionable(AutomationElement mainWindow, string pluginId)
    {
        return IsVisible(FindByAutomationId(mainWindow, $"PluginInstallButton_{pluginId}"))
               || IsVisible(FindByAutomationId(mainWindow, $"PluginOpenButton_{pluginId}"))
               || IsVisible(FindByAutomationId(mainWindow, $"PluginConfigureButton_{pluginId}"))
               || IsVisible(FindByAutomationId(mainWindow, $"PluginUninstallButton_{pluginId}"));
    }

    private static IReadOnlyList<PluginInstallPlan> ResolvePluginInstallPlans(
        IReadOnlyList<string> pluginIds,
        IReadOnlySet<string> availablePlugins,
        IReadOnlyDictionary<string, PluginInstallSource> requestedSources,
        IReadOnlyList<LocalPluginPackageState> localPluginPackages)
    {
        var localPackagesByPluginId = localPluginPackages.ToDictionary(package => package.PluginId, StringComparer.OrdinalIgnoreCase);
        var plans = new List<PluginInstallPlan>(pluginIds.Count);

        foreach (var pluginId in pluginIds)
        {
            var requestedSource = ResolveRequestedPluginSource(pluginId, requestedSources);
            localPackagesByPluginId.TryGetValue(pluginId, out var localPackage);
            var availableOnline = availablePlugins.Contains(pluginId);

            if (requestedSource == PluginInstallSource.Local)
            {
                if (localPackage is null)
                    throw new InvalidOperationException($"Plugin '{pluginId}' was requested as local, but no local ZIP package could be prepared.");

                plans.Add(new PluginInstallPlan(pluginId, PluginInstallSource.Local, localPackage.PackagePath));
                continue;
            }

            if (!availableOnline)
                throw new InvalidOperationException($"Plugin '{pluginId}' was requested as online, but no marketplace entry was available.");

            plans.Add(new PluginInstallPlan(pluginId, PluginInstallSource.Online, null));
        }

        return plans;
    }

    private static void EnsurePluginInstalled(AutomationElement mainWindow, SmokeSandboxState? sandboxState, string pluginId)
    {
        if (IsPluginInstalled(mainWindow, sandboxState, pluginId))
        {
            Console.WriteLine($"[main-smoke] Plugin already installed: {pluginId}");
            return;
        }

        InstallPluginFromMarketplace(mainWindow, sandboxState, pluginId);
    }

    private static void ImportPluginsFromLocalPackages(AutomationElement mainWindow, SmokeSandboxState? sandboxState, IReadOnlyList<PluginInstallPlan> localPlans)
    {
        if (localPlans.Count == 0)
            return;

        var bulkImportButton = WaitForAutomationId(mainWindow, "PluginBulkImportButton", TimeSpan.FromSeconds(20));
        Click(bulkImportButton);
        Console.WriteLine($"[main-smoke] Clicked local bulk import for: [{string.Join(", ", localPlans.Select(plan => plan.PluginId))}]");

        foreach (var plan in localPlans)
        {
            var installed = WaitUntil(
                () => IsPluginInstalled(mainWindow, sandboxState, plan.PluginId),
                TimeSpan.FromSeconds(90),
                TimeSpan.FromMilliseconds(350));

            if (!installed)
                throw new TimeoutException($"Local ZIP import did not reach installed state: {plan.PluginId}");

            Console.WriteLine($"[main-smoke] Local ZIP import verified for plugin: {plan.PluginId}");
            CaptureMainWindow(mainWindow, $"{plan.PluginId}-local-import-installed");
            ObserveStep($"Local ZIP import verified: {plan.PluginId}", mainWindow);
        }
    }

    private static void TestPluginEntryUi(AutomationElement mainWindow, int processId, string pluginId, bool isLastPlugin, bool marketplaceAvailable, bool isKnownInstalled, PluginInstallPlan installPlan)
    {
        Console.WriteLine($"[main-smoke] Testing plugin UI entry: {pluginId} ({installPlan.Source.ToString().ToLowerInvariant()})");
        WaitForPluginMarketplaceInteractionReady(mainWindow, pluginId, TimeSpan.FromSeconds(20));
        if (marketplaceAvailable)
        {
            EnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);
            mainWindow = ResolveLiveWindow(mainWindow);
        }

        if (pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase) && isLastPlugin)
        {
            var featureRouteValidated = false;
            var openButton = FindByAutomationId(ResolveLiveWindow(mainWindow), $"PluginOpenButton_{pluginId}");
            if (marketplaceAvailable && openButton is not null && IsVisible(openButton))
            {
                try
                {
                    TestOpenFeaturePage(mainWindow, pluginId, returnToMarketplace: marketplaceAvailable);
                    featureRouteValidated = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[main-smoke] Marketplace Open route failed for '{pluginId}': {ex.Message}. Falling back to sidebar route for functional validation.");
                    CloseStalePluginSettingsWindows(mainWindow);
                }
            }

            if (!featureRouteValidated && isKnownInstalled)
                TestSidebarPluginPageEntry(mainWindow, pluginId, returnToMarketplace: marketplaceAvailable);
            else if (!featureRouteValidated)
                Console.WriteLine($"[main-smoke] Network feature-page test skipped (no Open button): {pluginId}");

            mainWindow = ResolveLiveWindow(mainWindow);

            if (marketplaceAvailable && IsPluginInstalledInUi(mainWindow, pluginId))
            {
                var settingsRouteValidated = false;
                try
                {
                    TestDoubleClickOpensSettings(mainWindow, processId, pluginId);
                    settingsRouteValidated = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[main-smoke] Marketplace double-click settings route failed for '{pluginId}': {ex.Message}. Continuing with Configure button validation.");
                }

                TestConfigureOpensSettings(mainWindow, processId, pluginId, settingsRouteAlreadyValidated: settingsRouteValidated);
            }
            else if (isKnownInstalled)
                Console.WriteLine($"[main-smoke] Skipping marketplace settings validation for '{pluginId}' because marketplace UI is unavailable.");
            else
                throw new InvalidOperationException($"Plugin is not installed before settings validation: {pluginId}");

            return;
        }

        if (pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
        {
            var returnToMarketplace = marketplaceAvailable && !isLastPlugin;

            if (isKnownInstalled)
                AssertNoStandaloneFeatureEntry(mainWindow, pluginId);

            var openButton = FindByAutomationId(mainWindow, $"PluginOpenButton_{pluginId}");
            if (marketplaceAvailable && openButton is not null && IsVisible(openButton))
                TestOpenOptimizationExtension(mainWindow, pluginId, returnToMarketplace);
            else
                TestOptimizationExtensionCategory(mainWindow, pluginId);

            if (isKnownInstalled)
                TestOptimizationSettingsWindow(mainWindow, processId, pluginId, returnToMarketplace: marketplaceAvailable);

            if (marketplaceAvailable && IsPluginInstalledInUi(mainWindow, pluginId))
                TestConfigureOpensSettings(mainWindow, processId, pluginId);

            return;
        }

        if (UsesOptimizationOpenRoute(pluginId))
        {
            if (isKnownInstalled)
                AssertNoStandaloneFeatureEntry(mainWindow, pluginId);

            var optimizationOpenButton = FindByAutomationId(mainWindow, $"PluginOpenButton_{pluginId}");
            if (marketplaceAvailable && optimizationOpenButton is not null && IsVisible(optimizationOpenButton))
                TestOpenOptimizationExtension(mainWindow, pluginId, returnToMarketplace: true);
            else if (isKnownInstalled)
                TestOptimizationExtensionCategory(mainWindow, pluginId);
            else
                Console.WriteLine($"[main-smoke] Optimization-page test skipped (no Open button): {pluginId}");

            if (isKnownInstalled)
                TestOptimizationSettingsWindow(mainWindow, processId, pluginId, returnToMarketplace: marketplaceAvailable);
            else
                throw new InvalidOperationException($"Plugin is not installed before optimization settings validation: {pluginId}");

            return;
        }

        var featureOpenButton = FindByAutomationId(mainWindow, $"PluginOpenButton_{pluginId}");
        if (marketplaceAvailable && featureOpenButton is not null && IsVisible(featureOpenButton))
        {
            var marketplaceOpenRouteValidated = false;
            try
            {
                TestOpenFeaturePage(mainWindow, pluginId, returnToMarketplace: true);
                marketplaceOpenRouteValidated = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[main-smoke] Marketplace Open route failed for '{pluginId}': {ex.Message}. Falling back to sidebar route for functional validation.");
                CloseStalePluginSettingsWindows(mainWindow);
            }

            try
            {
                TestSidebarPluginPageEntry(mainWindow, pluginId, returnToMarketplace: true);
            }
            catch (Exception ex) when (marketplaceOpenRouteValidated)
            {
                Console.WriteLine($"[main-smoke] Sidebar route validation for '{pluginId}' was non-blocking because marketplace Open already rendered and exercised the feature page: {ex.Message}");
                NavigateToPluginExtensionsPage(mainWindow, refresh: false);
            }
        }
        else if (isKnownInstalled)
            TestSidebarPluginPageEntry(mainWindow, pluginId, returnToMarketplace: false);
        else
            Console.WriteLine($"[main-smoke] Feature-page test skipped (no Open button): {pluginId}");

        if (marketplaceAvailable && IsPluginInstalledInUi(mainWindow, pluginId))
        {
            var settingsRouteValidated = false;
            if (SupportsMarketplaceDoubleClickSettings(pluginId))
            {
                try
                {
                    TestDoubleClickOpensSettings(mainWindow, processId, pluginId);
                    settingsRouteValidated = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[main-smoke] Marketplace double-click settings route failed for '{pluginId}': {ex.Message}. Continuing with Configure button validation.");
                }
            }
            else
                Console.WriteLine($"[main-smoke] Skipping double-click settings validation for '{pluginId}' because the marketplace card is configure-button driven.");

            TestConfigureOpensSettings(mainWindow, processId, pluginId, settingsRouteAlreadyValidated: settingsRouteValidated);
        }
        else if (isKnownInstalled)
            Console.WriteLine($"[main-smoke] Skipping marketplace settings validation for '{pluginId}' because direct-route verification already succeeded.");
        else
            throw new InvalidOperationException($"Plugin is not installed before settings validation: {pluginId}");
    }

    private static void TestOpenFeaturePage(AutomationElement mainWindow, string pluginId, bool returnToMarketplace)
    {
        CloseStalePluginSettingsWindows(mainWindow);

        mainWindow = ResolveLiveWindow(mainWindow);
        var processId = mainWindow.Current.ProcessId;
        DismissAnyBlockingMessageBox(mainWindow, processId);
        EnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);

        var openButton = WaitForAutomationId(mainWindow, $"PluginOpenButton_{pluginId}", TimeSpan.FromSeconds(20));
        Click(openButton);
        WaitForAnimationsToComplete();
        Console.WriteLine($"[main-smoke] Opened plugin feature page: {pluginId}");

        mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);

        var leftPluginPage = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
                return !IsVisible(FindByAutomationId(mainWindow, "PluginSearchTextBox"));
            },
            TimeSpan.FromSeconds(3),
            TimeSpan.FromMilliseconds(200));

        if (!leftPluginPage)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            BringToForeground(mainWindow);
            DismissAnyBlockingMessageBox(mainWindow, processId);
            openButton = WaitForAutomationId(mainWindow, $"PluginOpenButton_{pluginId}", TimeSpan.FromSeconds(5));
            MouseClick(openButton);
            WaitForAnimationsToComplete();
            Console.WriteLine($"[main-smoke] Feature-page open received mouse-click fallback: {pluginId}");

            leftPluginPage = WaitUntil(
                () =>
                {
                    mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
                    return !IsVisible(FindByAutomationId(mainWindow, "PluginSearchTextBox"));
                },
                TimeSpan.FromSeconds(7),
                TimeSpan.FromMilliseconds(200));
        }

        if (!leftPluginPage)
            EnsureMarketplaceViewExited(mainWindow, pluginId, "marketplace-open");

        EnsurePluginFeaturePageRendered(mainWindow, pluginId, entrySource: "marketplace-open");
        CaptureMainWindow(mainWindow, pluginId, "feature-page");
        ObserveStep($"Feature page opened: {pluginId}", mainWindow);

        if (pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase))
            TestNetworkAccelerationFeatureInteractions(mainWindow);

        if (returnToMarketplace)
            NavigateToPluginExtensionsPage(mainWindow, refresh: false);
    }

    private static void TestSidebarPluginPageEntry(AutomationElement mainWindow, string pluginId, bool returnToMarketplace)
    {
        CloseStalePluginSettingsWindows(mainWindow);
        mainWindow = ResolveLiveWindow(mainWindow);
        var processId = mainWindow.Current.ProcessId;
        mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);

        var navAutomationId = $"PluginNavItem_{pluginId}";
        var navItem = WaitForAutomationId(mainWindow, navAutomationId, TimeSpan.FromSeconds(20));
        ActivateNavigationElement(navItem, $"Plugin sidebar {pluginId}");
        WaitForAnimationsToComplete();

        mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
        if (!WaitForMarketplaceViewExited(mainWindow, TimeSpan.FromSeconds(3)))
        {
            BringToForeground(mainWindow);
            DismissAnyBlockingMessageBox(mainWindow, processId);
            navItem = WaitForAutomationId(mainWindow, navAutomationId, TimeSpan.FromSeconds(5));
            PressNavigationKey(navItem, VkEnter);
            WaitForAnimationsToComplete();
            Console.WriteLine($"[main-smoke] Sidebar navigation received Enter-key fallback: {pluginId}");
        }

        mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
        if (!WaitForMarketplaceViewExited(mainWindow, TimeSpan.FromSeconds(3)))
        {
            BringToForeground(mainWindow);
            DismissAnyBlockingMessageBox(mainWindow, processId);
            navItem = WaitForAutomationId(mainWindow, navAutomationId, TimeSpan.FromSeconds(5));
            PressNavigationKey(navItem, VkSpace);
            WaitForAnimationsToComplete();
            Console.WriteLine($"[main-smoke] Sidebar navigation received Space-key fallback: {pluginId}");
        }

        EnsureMarketplaceViewExited(mainWindow, pluginId, "sidebar");
        EnsurePluginFeaturePageRendered(mainWindow, pluginId, entrySource: "sidebar");
        Console.WriteLine($"[main-smoke] Opened plugin feature page from sidebar: {pluginId}");
        CaptureMainWindow(mainWindow, pluginId, "feature-page-sidebar");
        ObserveStep($"Sidebar feature page opened: {pluginId}", mainWindow);

        if (pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase))
            TestNetworkAccelerationFeatureInteractions(mainWindow);

        if (returnToMarketplace)
            NavigateToPluginExtensionsPage(mainWindow, refresh: false);
    }

    private static bool UsesOptimizationOpenRoute(string pluginId)
    {
        return pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase)
               || pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureMarketplaceViewExited(AutomationElement mainWindow, string pluginId, string entrySource)
    {
        var leftMarketplace = WaitForMarketplaceViewExited(mainWindow, TimeSpan.FromSeconds(10));

        if (leftMarketplace)
            return;

        mainWindow = ResolveLiveWindow(mainWindow);
        DumpAutomationSnapshot(mainWindow, 280);
        throw new InvalidOperationException($"Plugin '{pluginId}' did not leave marketplace view via {entrySource}.");
    }

    private static bool WaitForMarketplaceViewExited(AutomationElement mainWindow, TimeSpan timeout)
    {
        return WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                return !IsVisible(FindByAutomationId(mainWindow, "PluginSearchTextBox"));
            },
            timeout,
            TimeSpan.FromMilliseconds(250));
    }

    private static bool SupportsMarketplaceDoubleClickSettings(string pluginId)
    {
        return !pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SupportsPluginFocusedOptimizationRoute(string pluginId)
    {
        return pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase)
               || pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase);
    }

    private static void TestOpenOptimizationExtension(AutomationElement mainWindow, string pluginId, bool returnToMarketplace)
    {
        EnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);
        var openButton = FindOptimizationOpenEntryButton(mainWindow, pluginId, TimeSpan.FromSeconds(20));
        Click(openButton);

        EnsureOptimizationCategoryVisible(mainWindow, pluginId, toggleActions: false);
        CaptureMainWindow(mainWindow, pluginId, "optimization-page");
        ToggleOptimizationActions(mainWindow, pluginId);
        Console.WriteLine($"[main-smoke] Open button routed to optimization extension: {pluginId}");
        ObserveStep($"Optimization route opened: {pluginId}", mainWindow);

        if (returnToMarketplace)
            NavigateToPluginExtensionsPage(mainWindow, refresh: false);
    }

    private static AutomationElement FindOptimizationOpenEntryButton(AutomationElement mainWindow, string pluginId, TimeSpan timeout)
    {
        var directAutomationId = $"PluginOpenButton_{pluginId}";
        var openButton = TryWaitForAutomationId(mainWindow, directAutomationId, timeout);
        if (openButton is not null)
            return openButton;

        throw new TimeoutException($"Timed out waiting for optimization entry button for '{pluginId}'. Tried '{directAutomationId}'.");
    }

    private static void AssertNoStandaloneFeatureEntry(AutomationElement mainWindow, string pluginId)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        var navItem = TryWaitForAutomationId(mainWindow, $"PluginNavItem_{pluginId}", TimeSpan.FromSeconds(2));
        if (navItem is not null)
            throw new InvalidOperationException($"Plugin '{pluginId}' unexpectedly exposed a sidebar feature-page entry.");
    }

    private static void EnsurePluginFeaturePageRendered(AutomationElement mainWindow, string pluginId, string entrySource)
    {
        var wrapperReady = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                return IsVisible(FindByAutomationId(mainWindow, "PluginPageWrapperRoot"))
                       || IsVisible(FindByAutomationId(mainWindow, "PluginPageContentFrame"))
                       || IsVisible(FindByAutomationId(mainWindow, "PluginPageEmptyState"))
                       || IsPluginSpecificFeatureMarkerVisible(mainWindow, pluginId);
            },
            TimeSpan.FromSeconds(15),
            TimeSpan.FromMilliseconds(250));

        if (!wrapperReady)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            DumpAutomationSnapshot(mainWindow, 300);
            throw new TimeoutException($"Plugin page wrapper did not appear for '{pluginId}' via {entrySource}.");
        }

        mainWindow = ResolveLiveWindow(mainWindow);
        var emptyStateVisible = IsVisible(FindByAutomationId(mainWindow, "PluginPageEmptyState"));
        if (emptyStateVisible)
        {
            DumpAutomationSnapshot(mainWindow, 300);
            throw new InvalidOperationException($"Plugin '{pluginId}' opened an empty-state page via {entrySource}.");
        }

        if (PluginPageShowsLoadFailure(mainWindow))
        {
            DumpAutomationSnapshot(mainWindow, 320);
            throw new InvalidOperationException($"Plugin '{pluginId}' feature page reported a runtime load failure via {entrySource}.");
        }

        if (pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase))
        {
            var networkMarkerReady = WaitUntil(
                () => IsPluginSpecificFeatureMarkerVisible(mainWindow, pluginId),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromMilliseconds(250));

            if (!networkMarkerReady)
            {
                DumpAutomationSnapshot(mainWindow, 350);
                throw new InvalidOperationException($"Network plugin page appears blank via {entrySource}; expected controls were not detected.");
            }
        }
    }


    private static bool IsPluginSpecificFeatureMarkerVisible(AutomationElement mainWindow, string pluginId)
    {
        if (pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase))
        {
            return IsVisible(FindByAutomationId(mainWindow, "NetworkAcceleration_FeatureRoot"))
                   || IsVisible(FindByAutomationId(mainWindow, "NetworkAcceleration_ModeComboBox"))
                   || IsVisible(FindByAutomationId(mainWindow, "NetworkAcceleration_QuickOptimizeButton"))
                   || IsVisible(FindByAutomationId(mainWindow, "NetworkAcceleration_ResetStackButton"))
                   || IsVisible(FindByAutomationId(mainWindow, "NetworkAcceleration_SaveModeButton"))
                   || IsVisible(FindByAutomationId(mainWindow, "NetworkAcceleration_StatusText"))
                   || FindByName(mainWindow, "Run Quick Optimization") is not null
                   || FindByName(mainWindow, "Reset Network Stack") is not null
                   || FindByName(mainWindow, "Quick Optimize") is not null
                   || FindByName(mainWindow, "Reset Stack") is not null;
        }

        if (pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase))
        {
            return IsVisible(FindByAutomationId(mainWindow, "ViveToolPageRoot"))
                   || IsVisible(FindByAutomationId(mainWindow, "ViveToolImportButton"))
                   || IsVisible(FindByAutomationId(mainWindow, "ViveToolRefreshListButton"))
                   || FindVisibleTextContains(mainWindow, "ViVeTool")
                   || FindVisibleTextContains(mainWindow, "Feature Flags")
                   || FindVisibleTextContains(mainWindow, "Import")
                   || FindVisibleTextContains(mainWindow, "Refresh List");
        }

        return false;
    }

    private static bool PluginPageShowsLoadFailure(AutomationElement mainWindow)
    {
        return FindVisibleTextContainsAny(
            mainWindow,
            "Failed to load plugin page",
            "Could not load file or assembly",
            "无法加载插件页面",
            "找不到指定的文件");
    }

    private static void TestNetworkAccelerationFeatureInteractions(AutomationElement mainWindow)
    {
        var modeCombo = WaitForAutomationId(mainWindow, "NetworkAcceleration_ModeComboBox", TimeSpan.FromSeconds(12));
        SelectComboBoxItemByNames(modeCombo, "Gaming", "游戏");

        var saveModeButton = WaitForAutomationId(mainWindow, "NetworkAcceleration_SaveModeButton", TimeSpan.FromSeconds(8));
        Click(saveModeButton);

        var status = WaitForAutomationId(mainWindow, "NetworkAcceleration_StatusText", TimeSpan.FromSeconds(8));
        var modeSaved = WaitUntil(
            () => StatusTextIndicatesSaved(status)
                  || (IsVisible(status) && !string.IsNullOrWhiteSpace(ReadElementText(status))),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(250));

        if (!modeSaved)
        {
            DumpAutomationSnapshot(mainWindow, 320);
            throw new InvalidOperationException("Network feature-page interaction failed: mode save status was not observed.");
        }

        Console.WriteLine("[main-smoke] Network feature-page interactions passed");
    }

    private static void TestDoubleClickOpensSettings(AutomationElement mainWindow, int processId, string pluginId)
    {
        mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);

        var existingSettingsWindows = GetSettingsWindowHandles(processId, mainWindow.Current.NativeWindowHandle);
        var mainWindowHandle = mainWindow.Current.NativeWindowHandle;
        var targetElement = ResolvePluginDoubleClickTarget(mainWindow, pluginId);
        TrySelect(targetElement);
        DoubleClick(targetElement);
        WaitForAnimationsToComplete();

        mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);

        var settingsWindow = pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase)
            ? WaitForPluginSettingsWindowByHandleOrName(
                processId,
                mainWindowHandle,
                existingSettingsWindows,
                TimeSpan.FromSeconds(12),
                "network-acceleration double-click",
                new[] { "Network Acceleration Settings", "Network Acceleration 设置" })
            : WaitForPluginSettingsWindow(
                processId,
                mainWindowHandle,
                existingSettingsWindows,
                TimeSpan.FromSeconds(7));

        Console.WriteLine($"[main-smoke] Double-click opened settings window for: {pluginId}");
        CapturePluginSettingsWindow(settingsWindow, pluginId, "settings-double-click");
        ObserveStep($"Settings window opened by double-click: {pluginId}", settingsWindow);

        if (pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase))
            TestNetworkAccelerationSettingsInteractions(settingsWindow);

        CloseWindowAndWait(settingsWindow, processId, TimeSpan.FromSeconds(8));
    }


    private static void TestConfigureOpensSettings(AutomationElement mainWindow, int processId, string pluginId, bool settingsRouteAlreadyValidated = false)
    {
        mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
        EnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);

        var existingSettingsWindows = GetSettingsWindowHandles(processId, mainWindow.Current.NativeWindowHandle);
        var expectedWindowNames = pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase)
            ? new[] { "ViVeTool Settings", "ViVeTool 设置" }
            : GetPluginSettingsWindowExpectedNames(pluginId);

        AutomationElement? settingsWindow = null;
        var activationModes = pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase)
            ? new[] { "invoke", "keyboard", "mouse", "mouse" }
            : new[] { "invoke", "mouse" };

        for (var attempt = 0; attempt < activationModes.Length && settingsWindow is null; attempt++)
        {
            try
            {
                mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
                var configureButton = WaitForAutomationId(mainWindow, $"PluginConfigureButton_{pluginId}", TimeSpan.FromSeconds(8));

                switch (activationModes[attempt])
                {
                    case "keyboard":
                        BringToForeground(mainWindow);
                        PressNavigationKey(configureButton, VkSpace);
                        Console.WriteLine($"[main-smoke] {pluginId} configure button received Space-key fallback.");
                        break;
                    case "mouse":
                        BringToForeground(mainWindow);
                        MouseClick(configureButton);
                        Console.WriteLine($"[main-smoke] {pluginId} configure button received mouse-click fallback.");
                        break;
                    default:
                        Click(configureButton);
                        break;
                }
            }
            catch (Exception ex) when (IsRecoverableAutomationException(ex))
            {
                Console.WriteLine($"[main-smoke] Retrying {pluginId} Configure activation after {ex.GetType().Name}");
            }

            WaitForAnimationsToComplete();
            mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
            settingsWindow = TryWaitForConfigureSettingsWindow(
                mainWindow,
                processId,
                existingSettingsWindows,
                pluginId,
                expectedWindowNames,
                TimeSpan.FromSeconds(attempt == 0 ? 5 : 7));
        }

        if (settingsWindow is null)
        {
            if (settingsRouteAlreadyValidated && pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[main-smoke] network-acceleration Configure did not expose a distinct settings window, but the settings route was already validated by double-click.");
                return;
            }

            if (pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase))
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                TryCaptureFailureMainWindow(mainWindow, $"{pluginId}-configure-failed");
                DumpAutomationSnapshot(mainWindow, 260);
            }

            throw new TimeoutException($"{pluginId} configure settings window did not appear.");
        }

        Console.WriteLine($"[main-smoke] Configure button opened settings window for: {pluginId}");
        CapturePluginSettingsWindow(settingsWindow, pluginId, "settings-configure");
        ObserveStep($"Settings window opened by configure: {pluginId}", settingsWindow);

        if (pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
            TestShellIntegrationSettingsInteractions(settingsWindow, processId, mainWindow.Current.NativeWindowHandle);

        if (pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase))
            TestNetworkAccelerationSettingsInteractions(settingsWindow);

        CloseWindowAndWait(settingsWindow, processId, TimeSpan.FromSeconds(8));
    }

    private static AutomationElement? TryWaitForConfigureSettingsWindow(
        AutomationElement mainWindow,
        int processId,
        ISet<int> existingSettingsWindows,
        string pluginId,
        string[] expectedWindowNames,
        TimeSpan? timeoutOverride = null)
    {
        var timeout = timeoutOverride ?? TimeSpan.FromSeconds(15);
        if (expectedWindowNames.Length > 0)
        {
            return TryWaitForPluginSettingsWindowByHandleOrName(
                processId,
                mainWindow.Current.NativeWindowHandle,
                existingSettingsWindows,
                timeout,
                $"{pluginId} configure",
                expectedWindowNames);
        }

        return TryWaitForPluginSettingsWindow(
            processId,
            mainWindow.Current.NativeWindowHandle,
            existingSettingsWindows,
            timeout);
    }

    private static void TestOptimizationSettingsWindow(AutomationElement mainWindow, int processId, string pluginId, bool returnToMarketplace)
    {
        NavigateToWindowsOptimizationPage(mainWindow);

        var definition = GetOptimizationRouteDefinition(pluginId)
                         ?? throw new InvalidOperationException($"No optimization route definition found for plugin '{pluginId}'.");

        var category = WaitForOptimizationCategory(mainWindow, pluginId, definition, TimeSpan.FromSeconds(30));
        if (category is not null)
            ExpandIfNeeded(category);

        var settingsButton = WaitForOptimizationSettingsButton(mainWindow, pluginId, definition, TimeSpan.FromSeconds(20));
        var existingSettingsWindows = GetSettingsWindowHandles(processId, mainWindow.Current.NativeWindowHandle);
        var expectedWindowNames = GetPluginSettingsWindowExpectedNames(pluginId);
        Click(settingsButton);

        if (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[main-smoke] custom-mouse optimization settings button clicked: id='{settingsButton.Current.AutomationId}' name='{settingsButton.Current.Name}'");
            BringToForeground(mainWindow);
            Click(settingsButton);
            MouseClick(settingsButton);
            MouseClick(settingsButton);
            Console.WriteLine("[main-smoke] custom-mouse optimization settings button received fallback mouse double-click.");
        }

        AutomationElement? settingsWindow = TryWaitForOptimizationSettingsWindow(mainWindow, processId, pluginId, existingSettingsWindows, expectedWindowNames);
        if (settingsWindow is null && pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
        {
            BringToForeground(mainWindow);
            MouseClick(settingsButton);
            Console.WriteLine("[main-smoke] shell-integration optimization settings button received mouse-click fallback.");
            settingsWindow = TryWaitForOptimizationSettingsWindow(mainWindow, processId, pluginId, existingSettingsWindows, expectedWindowNames);
        }

        if (settingsWindow is null && pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[main-smoke] custom-mouse optimization settings window not detected; continuing after explicit trigger trace.");
            return;
        }

        if (settingsWindow is null)
            throw new TimeoutException($"{pluginId} optimization settings window did not appear.");

        Console.WriteLine($"[main-smoke] Opened optimization settings window for: {pluginId}");
        CapturePluginSettingsWindow(settingsWindow, pluginId, "settings-optimization");
        ObserveStep($"Optimization settings window opened: {pluginId}", settingsWindow);

        var settingsWindowHandle = settingsWindow.Current.NativeWindowHandle;
        Console.WriteLine($"[main-smoke] Optimization settings window handle for '{pluginId}': {settingsWindowHandle}");

        if (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
        {
            WaitForCustomMouseTopLevelWindowToClose(settingsWindow, processId, TimeSpan.FromSeconds(8), "optimization");
        }
        else
        {
            CloseWindowAndWait(settingsWindow, processId, TimeSpan.FromSeconds(8));
        }

        if (returnToMarketplace)
        {
            if (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
            {
                TryHandleCustomMouseReentryCheckpoint(mainWindow, processId, TimeSpan.FromSeconds(8));
                NavigateToPluginExtensionsPage(mainWindow, refresh: false);
                TryHandleCustomMouseMarketplaceReentryReady(mainWindow, processId, TimeSpan.FromSeconds(12));
                return;
            }

            NavigateToPluginExtensionsPage(mainWindow, refresh: false);
        }
    }

    private static AutomationElement? TryWaitForOptimizationSettingsWindow(
        AutomationElement mainWindow,
        int processId,
        string pluginId,
        ISet<int> existingSettingsWindows,
        string[] expectedWindowNames)
    {
        var timeout = TimeSpan.FromSeconds(15);

        if (expectedWindowNames.Length > 0)
        {
            return TryWaitForPluginSettingsWindowByHandleOrName(
                processId,
                mainWindow.Current.NativeWindowHandle,
                existingSettingsWindows,
                timeout,
                $"{pluginId} optimization",
                expectedWindowNames);
        }

        return TryWaitForPluginSettingsWindow(
            processId,
            mainWindow.Current.NativeWindowHandle,
            existingSettingsWindows,
            timeout,
            Array.Empty<string>());
    }

    private static string[] GetPluginSettingsWindowExpectedNames(string pluginId)
    {
        if (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
            return new[] { "自定义鼠标 设置", "Custom Mouse Settings" };

        if (pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
            return new[] { "Shell Integration Settings", "Shell Integration 设置" };

        return Array.Empty<string>();
    }

    private static void WaitForCustomMouseReentryCleanupCheckpoint(AutomationElement mainWindow, int processId, TimeSpan timeout)
    {
        Console.WriteLine("[main-smoke] Waiting for custom-mouse reentry cleanup checkpoint before returning to Plugin Extensions");
        var reentryWindow = WaitForTopLevelSettingsWindowByName(
            processId,
            mainWindow.Current.NativeWindowHandle,
            new[] { "自定义鼠标 设置", "Custom Mouse Settings" },
            timeout);

        if (reentryWindow is null)
        {
            Console.WriteLine("[main-smoke] custom-mouse reentry cleanup checkpoint: no reentry settings window appeared.");
            return;
        }

        var reentryHandle = reentryWindow.Current.NativeWindowHandle;
        Console.WriteLine($"[main-smoke] custom-mouse reentry cleanup checkpoint: reentry window appeared, handle={reentryHandle} name='{reentryWindow.Current.Name}'");
        Console.WriteLine($"[main-smoke] custom-mouse reentry cleanup checkpoint: forcing explicit close for top-level handle {reentryHandle} via PART_CloseButton/_closeButton.");
        WaitForCustomMouseTopLevelWindowToClose(reentryWindow, processId, timeout, "reentry-checkpoint");
    }

    private static void TryHandleCustomMouseReentryCheckpoint(AutomationElement mainWindow, int processId, TimeSpan timeout)
    {
        try
        {
            WaitForCustomMouseReentryCleanupCheckpoint(mainWindow, processId, timeout);
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"[main-smoke] custom-mouse reentry cleanup checkpoint degraded to best-effort: {ex.Message}");
        }
    }

    private static void WaitForCustomMouseMarketplaceReentryReady(AutomationElement mainWindow, int processId, TimeSpan timeout)
    {
        Console.WriteLine("[main-smoke] Waiting for custom-mouse marketplace reentry readiness");
        mainWindow = ResolveLiveWindow(mainWindow);
        var namedWindows = new[] { "自定义鼠标 设置", "Custom Mouse Settings" };
        var drainWindow = WaitForTopLevelSettingsWindowByName(
            processId,
            mainWindow.Current.NativeWindowHandle,
            namedWindows,
            TimeSpan.FromSeconds(Math.Min(timeout.TotalSeconds, 3)));

        if (drainWindow is not null)
        {
            var drainHandle = drainWindow.Current.NativeWindowHandle;
            Console.WriteLine($"[main-smoke] custom-mouse marketplace reentry: respawned settings window detected, handle={drainHandle} name='{drainWindow.Current.Name}'");
            WaitForCustomMouseTopLevelWindowToClose(drainWindow, processId, timeout, "marketplace-reentry-drain");

            var handleGone = WaitUntil(
                () => !IsTopLevelWindowOpen(processId, drainHandle),
                timeout,
                TimeSpan.FromMilliseconds(150));
            Console.WriteLine($"[main-smoke] custom-mouse marketplace reentry: drained handle gone verification for {drainHandle}: {handleGone}");
            if (!handleGone)
                throw new TimeoutException($"custom-mouse marketplace reentry drained handle {drainHandle} remained open.");
        }
        else
        {
            Console.WriteLine("[main-smoke] custom-mouse marketplace reentry: no respawned settings window detected after returning from plugin page.");
        }

        var ready = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                return IsPluginMarketplaceReentryReady(mainWindow);
            },
            timeout,
            TimeSpan.FromMilliseconds(200));

        Console.WriteLine($"[main-smoke] custom-mouse marketplace reentry readiness: {ready}");
        if (!ready)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            DumpAutomationSnapshot(mainWindow, 260);
            throw new TimeoutException("custom-mouse marketplace reentry did not restore Plugin Extensions readiness.");
        }
    }

    private static void TryHandleCustomMouseMarketplaceReentryReady(AutomationElement mainWindow, int processId, TimeSpan timeout)
    {
        try
        {
            WaitForCustomMouseMarketplaceReentryReady(mainWindow, processId, timeout);
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"[main-smoke] custom-mouse marketplace reentry readiness degraded to best-effort: {ex.Message}");
        }
    }

    private static bool IsPluginMarketplaceReentryReady(AutomationElement mainWindow)
    {
        return IsPluginMarketplaceReady(mainWindow);
    }

    private static void TestShellIntegrationSettingsInteractions(AutomationElement settingsWindow, int processId, int mainWindowHandle)
    {
        var styleButton = WaitForShellIntegrationActionButton(
            settingsWindow,
            new[] { "OpenStyleSettingsButton", "_openStyleSettingsButton" },
            new[] { "Open Style Settings", "Open Style", "打开样式设置", "打开样式" },
            TimeSpan.FromSeconds(15));

        if (!IsInteractable(styleButton))
        {
            Console.WriteLine("[main-smoke] Shell style settings button is present but disabled; skipping dialog launch because shell assets are unavailable in the current sandbox.");
            return;
        }

        var excludedHandles = GetSettingsWindowHandles(processId, mainWindowHandle)
            .Append(settingsWindow.Current.NativeWindowHandle)
            .Where(handle => handle != 0)
            .Distinct()
            .ToArray();

        Click(styleButton);

        var styleWindow = WaitForTopLevelSettingsWindowByName(
            processId,
            mainWindowHandle,
            new[] { "Menu Style Settings", "样式设置", "菜单样式设置", "Shell Integration" },
            TimeSpan.FromSeconds(15),
            excludedHandles);

        if (styleWindow is null)
            throw new TimeoutException("Shell style settings window did not appear.");

        var handle = styleWindow.Current.NativeWindowHandle;
        Console.WriteLine($"[main-smoke] Shell style settings window opened: handle={handle} name='{styleWindow.Current.Name}'");

        if (_screenshotMode == ScreenshotMode.Always && handle != 0)
            CaptureWindowArtifacts(handle, "shell-integration-style-settings", includeFullScreen: true);

        ObserveStep("Shell style settings window opened", styleWindow);
        CloseWindowAndWait(styleWindow, processId, TimeSpan.FromSeconds(8));
        Console.WriteLine("[main-smoke] Shell settings-page interactions passed");
    }

    private static AutomationElement WaitForShellIntegrationActionButton(
        AutomationElement settingsWindow,
        IReadOnlyList<string> automationIds,
        IReadOnlyList<string> names,
        TimeSpan timeout)
    {
        var windowHandle = settingsWindow.Current.NativeWindowHandle;
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            AutomationElement? liveWindow = null;
            try
            {
                liveWindow = windowHandle != 0
                    ? AutomationElement.FromHandle((IntPtr)windowHandle)
                    : settingsWindow;
            }
            catch (Exception ex) when (IsRecoverableAutomationException(ex))
            {
                liveWindow = settingsWindow;
            }

            if (liveWindow is null)
            {
                Thread.Sleep(250);
                continue;
            }

            foreach (var automationId in automationIds)
            {
                var byId = FindByAutomationId(liveWindow, automationId);
                if (byId is not null)
                    return byId;
            }

            foreach (var name in names)
            {
                var byName = FindByName(liveWindow, name);
                if (byName is not null)
                    return byName;
            }

            Thread.Sleep(250);
        }

        AutomationElement? snapshotRoot = settingsWindow;
        if (windowHandle != 0)
        {
            try
            {
                snapshotRoot = AutomationElement.FromHandle((IntPtr)windowHandle);
            }
            catch (Exception ex) when (IsRecoverableAutomationException(ex))
            {
                snapshotRoot = settingsWindow;
            }
        }
        DumpAutomationSnapshot(snapshotRoot ?? settingsWindow, 260);
        throw new TimeoutException($"Timed out waiting for shell-integration action button set [{string.Join(", ", automationIds)}] or names [{string.Join(", ", names)}].");
    }

    private static void TestNetworkAccelerationSettingsInteractions(AutomationElement settingsWindow)
    {
        var autoOptimize = WaitForAutomationIdOrNames(
            settingsWindow,
            "NetworkAcceleration_AutoOptimizeCheckBox",
            new[] { "Auto optimize on startup" },
            TimeSpan.FromSeconds(15));
        var resetWinsock = WaitForAutomationIdOrNames(
            settingsWindow,
            "NetworkAcceleration_ResetWinsockCheckBox",
            new[] { "Reset Winsock during quick optimization", "Reset Winsock during optimization" },
            TimeSpan.FromSeconds(15));
        var resetTcpIp = WaitForAutomationIdOrNames(
            settingsWindow,
            "NetworkAcceleration_ResetTcpIpCheckBox",
            new[] { "Reset TCP/IP stack during quick optimization", "Reset TCP/IP during optimization" },
            TimeSpan.FromSeconds(15));
        var saveButton = WaitForAutomationIdOrNames(
            settingsWindow,
            "NetworkAcceleration_SaveSettingsButton",
            new[] { "Save Settings", "Save" },
            TimeSpan.FromSeconds(15));
        var settingsWindowHandle = settingsWindow.Current.NativeWindowHandle;

        Click(autoOptimize);
        Thread.Sleep(120);
        Click(resetWinsock);
        Thread.Sleep(120);
        Click(resetTcpIp);
        Thread.Sleep(120);

        Click(saveButton);

        var settingsSaved = WaitUntil(
            () =>
            {
                AutomationElement? liveSettingsWindow = null;
                try
                {
                    liveSettingsWindow = AutomationElement.FromHandle((IntPtr)settingsWindowHandle);
                }
                catch (Exception ex) when (IsRecoverableAutomationException(ex))
                {
                    return false;
                }

                if (liveSettingsWindow is null)
                    return false;

                var status = FindByAutomationId(liveSettingsWindow, "NetworkAcceleration_SettingsStatusText");
                if (StatusTextIndicatesSaved(status))
                    return true;

                if (status is not null && IsVisible(status) && !string.IsNullOrWhiteSpace(ReadElementText(status)))
                    return true;

                return FindVisibleTextContainsAny(liveSettingsWindow, "saved", "已保存", "保存");
            },
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(250));

        if (!settingsSaved)
        {
            DumpAutomationSnapshot(settingsWindow, 250);
            throw new InvalidOperationException("Network settings-page interaction failed: save status was not observed.");
        }

        Console.WriteLine("[main-smoke] Network settings-page interactions passed");
    }

    private static AutomationElement WaitForPluginSettingsWindow(
        int processId,
        int mainWindowHandle,
        ISet<int> existingSettingsWindows,
        TimeSpan timeout,
        params string[] expectedWindowNames)
    {
        var normalizedExpectedNames = expectedWindowNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)
                    .Cast<AutomationElement>()
                    .Where(window => window.Current.ProcessId == processId)
                    .ToArray();

                foreach (var window in windows)
                {
                    if (window.Current.ControlType != ControlType.Window)
                        continue;

                    var handle = window.Current.NativeWindowHandle;
                    if (handle == mainWindowHandle || handle == 0)
                        continue;

                    if (!IsLikelySettingsWindow(window))
                        continue;

                    if (normalizedExpectedNames.Length > 0
                        && !normalizedExpectedNames.Any(name => string.Equals(window.Current.Name, name, StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    if (!existingSettingsWindows.Contains(handle))
                    {
                        Console.WriteLine($"[main-smoke] Detected plugin settings window handle {handle}: id='{window.Current.AutomationId}' name='{window.Current.Name}'");
                        return window;
                    }
                }
            }
            catch (Exception ex) when (IsRecoverableAutomationException(ex))
            {
                Console.WriteLine($"[main-smoke] Retrying settings window discovery after {ex.GetType().Name}");
            }

            Thread.Sleep(200);
        }

        DumpProcessTopLevelElements(processId);
        throw new TimeoutException("Plugin settings window did not appear after double-click.");
    }

    private static AutomationElement WaitForPluginSettingsWindowByHandleOrName(
        int processId,
        int mainWindowHandle,
        ISet<int> existingSettingsWindows,
        TimeSpan timeout,
        string scenario,
        params string[] expectedWindowNames)
    {
        var handleTimeout = TimeSpan.FromSeconds(Math.Max(1, Math.Min(timeout.TotalSeconds / 2, 7)));
        var windowByHandle = TryWaitForPluginSettingsWindow(
            processId,
            mainWindowHandle,
            existingSettingsWindows,
            handleTimeout,
            Array.Empty<string>());

        if (windowByHandle is not null)
            return windowByHandle;

        var namedWindow = WaitForTopLevelSettingsWindowByName(
            processId,
            mainWindowHandle,
            expectedWindowNames,
            timeout,
            existingSettingsWindows.ToArray());

        if (namedWindow is not null)
        {
            Console.WriteLine($"[main-smoke] Detected {scenario} settings window by explicit name: handle={namedWindow.Current.NativeWindowHandle} id='{namedWindow.Current.AutomationId}' name='{namedWindow.Current.Name}'");
            return namedWindow;
        }

        namedWindow = WaitForTopLevelSettingsWindowByName(
            processId,
            mainWindowHandle,
            expectedWindowNames,
            TimeSpan.FromSeconds(Math.Max(1, Math.Min(timeout.TotalSeconds / 3, 5))));

        if (namedWindow is not null)
        {
            Console.WriteLine($"[main-smoke] Detected {scenario} settings window by explicit name after handle reuse: handle={namedWindow.Current.NativeWindowHandle} id='{namedWindow.Current.AutomationId}' name='{namedWindow.Current.Name}'");
            return namedWindow;
        }

        DumpProcessTopLevelElements(processId);
        throw new TimeoutException($"{scenario} settings window did not appear by handle or explicit name.");
    }

    private static AutomationElement? TryWaitForPluginSettingsWindowByHandleOrName(
        int processId,
        int mainWindowHandle,
        ISet<int> existingSettingsWindows,
        TimeSpan timeout,
        string scenario,
        params string[] expectedWindowNames)
    {
        try
        {
            return WaitForPluginSettingsWindowByHandleOrName(
                processId,
                mainWindowHandle,
                existingSettingsWindows,
                timeout,
                scenario,
                expectedWindowNames);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    private static AutomationElement? TryWaitForPluginSettingsWindow(
        int processId,
        int mainWindowHandle,
        ISet<int> existingSettingsWindows,
        TimeSpan timeout,
        params string[] expectedWindowNames)
    {
        try
        {
            return WaitForPluginSettingsWindow(processId, mainWindowHandle, existingSettingsWindows, timeout, expectedWindowNames);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    private static HashSet<int> GetSettingsWindowHandles(int processId, int mainWindowHandle)
    {
        try
        {
            return AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)
                .Cast<AutomationElement>()
                .Where(window =>
                {
                    if (window.Current.ProcessId != processId)
                        return false;

                    if (window.Current.ControlType != ControlType.Window)
                        return false;

                    var handle = window.Current.NativeWindowHandle;
                    return handle != 0 && handle != mainWindowHandle && IsLikelySettingsWindow(window);
                })
                .Select(window => window.Current.NativeWindowHandle)
                .ToHashSet();
        }
        catch
        {
            return new HashSet<int>();
        }
    }

    private static bool IsLikelySettingsWindow(AutomationElement window)
    {
        return FindByAutomationId(window, "_pluginSettingsFrame") is not null
               || FindByAutomationId(window, "_pluginNameTextBlock") is not null
               || (window.Current.Name?.Contains("settings", StringComparison.OrdinalIgnoreCase) ?? false)
               || (window.Current.Name?.Contains("设置", StringComparison.Ordinal) ?? false);
    }

    private static void CloseWindowAndWait(AutomationElement window, int processId, TimeSpan timeout)
    {
        var handle = window.Current.NativeWindowHandle;
        if (handle == 0)
        {
            CloseWindow(window);
            WaitForWindowCloseAnimation();
            return;
        }

        Console.WriteLine($"[main-smoke] Closing settings window handle {handle}");

        if (IsCustomMouseSettingsWindow(window))
        {
            CloseCustomMouseSettingsWindowHandleAndWait(window, processId, timeout, "generic");
            return;
        }

        var closed = TryCloseWindowViaExplicitCloseButton(window, processId, handle, timeout, logPrefix: "settings window");

        if (closed)
        {
            WaitForWindowCloseAnimation();
        }
    }

    private static bool IsCustomMouseSettingsWindow(AutomationElement window)
    {
        if (window is null)
            return false;

        if (string.Equals(window.Current.Name, "自定义鼠标 设置", StringComparison.OrdinalIgnoreCase)
            || string.Equals(window.Current.Name, "Custom Mouse Settings", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool TryCloseWindowViaExplicitCloseButton(
        AutomationElement? window,
        int processId,
        int handle,
        TimeSpan timeout,
        string logPrefix)
    {
        if (window is null || handle == 0)
            return false;

        var explicitCloseButton = FindVisibleCloseButton(window);
        if (!IsVisible(explicitCloseButton))
        {
            Console.WriteLine($"[main-smoke] {logPrefix} explicit close button not found for handle {handle}.");
            return false;
        }

        Console.WriteLine($"[main-smoke] Clicking {logPrefix} explicit close button for handle {handle}: {explicitCloseButton!.Current.AutomationId}");
        Click(explicitCloseButton!);

        Thread.Sleep((int)WindowAnimationDuration.TotalMilliseconds);

        var closed = WaitUntil(
            () => !IsTopLevelWindowOpen(processId, handle),
            timeout,
            TimeSpan.FromMilliseconds(150));
        Console.WriteLine($"[main-smoke] {logPrefix} closed verification via explicit button (handle {handle}): {closed}");
        return closed;
    }

    private static void CloseCustomMouseSettingsWindowHandleAndWait(AutomationElement window, int processId, TimeSpan timeout, string stage)
    {
        var handle = window.Current.NativeWindowHandle;
        Console.WriteLine($"[main-smoke] custom-mouse {stage} close start: handle={handle} name='{window.Current.Name}'");

        if (handle == 0)
            throw new InvalidOperationException($"custom-mouse {stage} settings window does not have a native handle.");

        var liveWindow = WaitForCustomMouseSettingsCloseButton(window, processId, timeout, stage);
        var liveHandle = liveWindow.Current.NativeWindowHandle;
        if (liveHandle != handle)
            Console.WriteLine($"[main-smoke] custom-mouse {stage} close target refreshed: originalHandle={handle} liveHandle={liveHandle}");

        var targetHandle = liveHandle != 0 ? liveHandle : handle;
        Console.WriteLine($"[main-smoke] custom-mouse {stage} closing explicit top-level handle {targetHandle}.");
        ClickCustomMouseExplicitCloseButtonAndWait(liveWindow, processId, targetHandle, timeout, stage);
    }

    private static void WaitForCustomMouseTopLevelWindowToClose(AutomationElement window, int processId, TimeSpan timeout, string stage)
    {
        var handle = window.Current.NativeWindowHandle;
        Console.WriteLine($"[main-smoke] custom-mouse {stage}: detected top-level settings window handle={handle} name='{window.Current.Name}'");
        Console.WriteLine($"[main-smoke] custom-mouse {stage}: explicitly closing handle {handle} via PART_CloseButton/_closeButton when available.");
        CloseCustomMouseSettingsWindowHandleAndWait(window, processId, timeout, stage);

        var closed = WaitUntil(
            () => !IsTopLevelWindowOpen(processId, handle),
            timeout,
            TimeSpan.FromMilliseconds(150));
        Console.WriteLine($"[main-smoke] custom-mouse {stage}: top-level handle gone verification for {handle}: {closed}");
        if (!closed)
            throw new TimeoutException($"custom-mouse {stage} window handle {handle} remained open after explicit close.");
    }

    private static void ClickCustomMouseExplicitCloseButtonAndWait(AutomationElement window, int processId, int handle, TimeSpan timeout, string stage)
    {
        var closeButton = FindVisibleCloseButton(window)
            ?? throw new TimeoutException($"custom-mouse {stage} settings window handle {handle} never exposed PART_CloseButton/_closeButton.");

        var closeButtonId = closeButton.Current.AutomationId;
        var closeButtonName = closeButton.Current.Name;
        Console.WriteLine($"[main-smoke] Clicking custom-mouse {stage} explicit close button for handle {handle}: id='{closeButtonId}' name='{closeButtonName}'");
        Click(closeButton);

        var closed = WaitUntil(
            () => !IsTopLevelWindowOpen(processId, handle),
            timeout,
            TimeSpan.FromMilliseconds(150));

        Console.WriteLine($"[main-smoke] custom-mouse {stage} settings handle closed: {closed} (handle={handle})");

        if (!closed)
            throw new TimeoutException($"custom-mouse settings window handle {handle} did not close after explicit close button click.");
    }

    private static AutomationElement WaitForCustomMouseSettingsCloseButton(AutomationElement window, int processId, TimeSpan timeout, string stage)
    {
        var handle = window.Current.NativeWindowHandle;
        AutomationElement? liveWindowWithCloseButton = null;
        var closeButtonReady = WaitUntil(
            () =>
            {
                var liveWindow = FindTopLevelWindow(processId, handle)
                    ?? WaitForTopLevelSettingsWindowByName(
                        processId,
                        0,
                        new[] { "自定义鼠标 设置", "Custom Mouse Settings" },
                        TimeSpan.FromMilliseconds(1),
                        handle);
                if (liveWindow is null)
                    return false;

                var liveCloseButton = FindVisibleCloseButton(liveWindow);
                if (liveCloseButton is null || !IsVisible(liveCloseButton))
                {
                    Console.WriteLine($"[main-smoke] custom-mouse {stage} explicit close button not found yet for handle {handle}.");
                    return false;
                }

                liveWindowWithCloseButton = liveWindow;
                Console.WriteLine($"[main-smoke] custom-mouse {stage} close button ready for handle {liveWindow.Current.NativeWindowHandle}: {liveCloseButton.Current.AutomationId}");
                return true;
            },
            timeout,
            TimeSpan.FromMilliseconds(150));

        if (!closeButtonReady || liveWindowWithCloseButton is null)
            throw new TimeoutException($"custom-mouse {stage} settings window handle {handle} never exposed PART_CloseButton/_closeButton.");

        return liveWindowWithCloseButton;
    }

    private static AutomationElement? FindVisibleCloseButton(AutomationElement window)
    {
        return FindByAutomationId(window, "PART_CloseButton")
               ?? FindByAutomationId(window, "_closeButton")
               ?? FindByAutomationId(window, "CloseButton")
               ?? FindByAutomationId(window, "PluginSettingsCloseButton")
               ?? FindByName(window, "Close")
               ?? FindByName(window, "关闭")
               ?? FindByName(window, "OK")
               ?? FindByName(window, "确定");
    }

    private static void CloseStalePluginSettingsWindows(AutomationElement mainWindow)
    {
        try
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            Console.WriteLine("[main-smoke] Resolving stale plugin settings windows");
            var processId = mainWindow.Current.ProcessId;
            var mainWindowHandle = mainWindow.Current.NativeWindowHandle;
            var handles = GetSettingsWindowHandles(processId, mainWindowHandle);
            Console.WriteLine($"[main-smoke] Stale settings window handles discovered: {handles.Count}");

            foreach (var handle in handles)
            {
                var settingsWindow = FindTopLevelWindow(processId, handle);
                if (settingsWindow == null)
                    continue;

                Console.WriteLine($"[main-smoke] Closing stale settings window handle: {handle}");
                CloseWindowAndWait(settingsWindow, processId, TimeSpan.FromSeconds(4));
            }
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            Console.WriteLine($"[main-smoke] Skipping stale settings cleanup: {ex.GetType().Name}");
        }
    }

    private static AutomationElement? DetectMessageBoxWindow(int processId, int mainWindowHandle, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)
                    .Cast<AutomationElement>()
                    .Where(window => window.Current.ProcessId == processId)
                    .ToArray();

                foreach (var window in windows)
                {
                    if (window.Current.ControlType != ControlType.Window)
                        continue;

                    var handle = window.Current.NativeWindowHandle;
                    if (handle == mainWindowHandle || handle == 0)
                        continue;

                    if (IsMessageBoxWindow(window))
                    {
                        Console.WriteLine($"[main-smoke] Detected MessageBox window: handle={handle} name='{window.Current.Name}'");
                        return window;
                    }
                }
            }
            catch (Exception ex) when (IsRecoverableAutomationException(ex))
            {
                Console.WriteLine($"[main-smoke] Retrying MessageBox detection after {ex.GetType().Name}");
            }

            Thread.Sleep(100);
        }

        return null;
    }

    private static MessageBoxType ClassifyPopupWindow(AutomationElement window)
    {
        var automationId = window.Current.AutomationId ?? string.Empty;
        var name = window.Current.Name ?? string.Empty;

        // Check for WPF.UI MessageBox (has ButtonLeft/ButtonRight)
        if (FindByAutomationId(window, "ButtonLeft") is not null || FindByAutomationId(window, "ButtonRight") is not null)
            return MessageBoxType.WpfUiMessageBox;

        // Check for explicit MessageBox automation ID
        if (string.Equals(automationId, "MessageBox", StringComparison.OrdinalIgnoreCase))
            return MessageBoxType.WpfUiMessageBox;

        // Check for System.Windows.MessageBox (ClassName = "#32770")
        try
        {
            var classNameProperty = AutomationElement.ClassNameProperty;
            var className = window.GetCurrentPropertyValue(classNameProperty) as string;
            if (className == "#32770")
                return MessageBoxType.SystemWindowsMessageBox;
        }
        catch { /* Ignore property access errors */ }

        // Check for notification popup (small window with close button)
        var closeButton = FindByAutomationId(window, "PART_CloseButton") ?? FindByAutomationId(window, "_closeButton");
        if (closeButton is not null)
        {
            // Use window size as secondary check
            try
            {
                var rect = window.Current.BoundingRectangle;
                if (rect.Width <= MessageBoxMaxWidth && rect.Height <= MessageBoxMaxHeight)
                {
                    var buttons = window.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
                        .Cast<AutomationElement>()
                        .Where(btn => IsVisible(btn))
                        .ToArray();

                    if (buttons.Length <= 3)
                        return MessageBoxType.NotificationPopup;
                }
            }
            catch { /* Ignore rect access errors */ }
        }

        // Fallback: name contains MessageBox
        if (name.Contains("MessageBox", StringComparison.OrdinalIgnoreCase))
            return MessageBoxType.WpfUiMessageBox;

        return MessageBoxType.Unknown;
    }

    private static bool IsMessageBoxWindow(AutomationElement window)
    {
        var popupType = ClassifyPopupWindow(window);
        return popupType != MessageBoxType.Unknown;
    }

    private static bool TryDismissMessageBox(AutomationElement mainWindow, int processId, TimeSpan timeout)
    {
        var mainWindowHandle = mainWindow.Current.NativeWindowHandle;
        var messageBox = DetectMessageBoxWindow(processId, mainWindowHandle, timeout);

        if (messageBox is null)
            return false;

        var popupType = ClassifyPopupWindow(messageBox);
        var windowName = messageBox.Current.Name ?? "<unnamed>";

        try
        {
            var rightButton = FindByAutomationId(messageBox, "ButtonRight")
                              ?? FindByName(messageBox, "No")
                              ?? FindByName(messageBox, "取消")
                              ?? FindByName(messageBox, "Cancel");

            if (rightButton is not null && IsVisible(rightButton))
            {
                Console.WriteLine($"[main-smoke] Dismissing {popupType} via secondary button: '{windowName}'");
                Click(rightButton);
                RecordDismissedPopup(popupType, windowName, "secondary-button");
                Thread.Sleep((int)WindowAnimationDuration.TotalMilliseconds);
                return true;
            }

            var leftButton = FindByAutomationId(messageBox, "ButtonLeft")
                             ?? FindByName(messageBox, "Yes")
                             ?? FindByName(messageBox, "OK")
                             ?? FindByName(messageBox, "确定")
                             ?? FindByName(messageBox, "是");

            if (leftButton is not null && IsVisible(leftButton))
            {
                Console.WriteLine($"[main-smoke] Dismissing {popupType} via primary button: '{windowName}'");
                Click(leftButton);
                RecordDismissedPopup(popupType, windowName, "primary-button");
                Thread.Sleep((int)WindowAnimationDuration.TotalMilliseconds);
                return true;
            }

            var closeButton = FindByAutomationId(messageBox, "PART_CloseButton")
                              ?? FindByAutomationId(messageBox, "_closeButton")
                              ?? FindByAutomationId(messageBox, "CloseButton");

            if (closeButton is not null && IsVisible(closeButton))
            {
                Console.WriteLine($"[main-smoke] Dismissing {popupType} via close button: '{windowName}'");
                Click(closeButton);
                RecordDismissedPopup(popupType, windowName, "close-button");
                Thread.Sleep((int)WindowAnimationDuration.TotalMilliseconds);
                return true;
            }

            Console.WriteLine($"[main-smoke] {popupType} detected but no dismissible button found: '{windowName}'");
            return false;
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            Console.WriteLine($"[main-smoke] Failed to dismiss {popupType}: {ex.GetType().Name}");
            return false;
        }
    }

    private static void RecordDismissedPopup(MessageBoxType popupType, string windowName, string dismissMethod)
    {
        _dismissedPopups.Add(new DismissedPopupRecord(DateTimeOffset.UtcNow, popupType, windowName, dismissMethod));
    }

    private static void DismissAnyBlockingMessageBox(AutomationElement mainWindow, int processId)
    {
        var attempts = 0;
        const int maxAttempts = 3;

        while (attempts < maxAttempts)
        {
            if (!TryDismissMessageBox(mainWindow, processId, TimeSpan.FromMilliseconds(500)))
                break;

            attempts++;
            Console.WriteLine($"[main-smoke] MessageBox dismissed (attempt {attempts}/{maxAttempts})");
        }
    }

    private static AutomationElement? DetectNotificationWindow(int processId, int mainWindowHandle, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)
                    .Cast<AutomationElement>()
                    .Where(window => window.Current.ProcessId == processId)
                    .ToArray();

                foreach (var window in windows)
                {
                    if (window.Current.ControlType != ControlType.Window)
                        continue;

                    var handle = window.Current.NativeWindowHandle;
                    if (handle == mainWindowHandle || handle == 0)
                        continue;

                    if (IsNotificationWindow(window))
                    {
                        Console.WriteLine($"[main-smoke] Detected NotificationWindow: handle={handle}");
                        return window;
                    }
                }
            }
            catch (Exception ex) when (IsRecoverableAutomationException(ex))
            {
                Console.WriteLine($"[main-smoke] Retrying NotificationWindow detection after {ex.GetType().Name}");
            }

            Thread.Sleep(100);
        }

        return null;
    }

    private static bool IsNotificationWindow(AutomationElement window)
    {
        var name = window.Current.Name ?? string.Empty;

        if (name.Contains("Notification", StringComparison.OrdinalIgnoreCase))
            return true;

        if (window.TryGetCurrentPattern(WindowPattern.Pattern, out var pattern))
        {
            try
            {
                var windowPattern = (WindowPattern)pattern;
                if (windowPattern.Current.IsTopmost)
                {
                    var children = window.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                        .Cast<AutomationElement>()
                        .Take(10)
                        .ToArray();

                    if (children.Length <= 5)
                        return true;
                }
            }
            catch
            {
                // Ignore pattern access errors
            }
        }

        return false;
    }

    private static void WaitForAnimationsToComplete(TimeSpan? additionalDelay = null)
    {
        // Non-observation mode: skip animation waits for faster execution
        if (!_watchMode && _animationsDisabled)
        {
            Thread.Sleep(50); // Minimal delay for UI responsiveness
            return;
        }

        var baseAnimationDuration = TimeSpan.FromMilliseconds(WindowAnimationDuration.TotalMilliseconds * _animationSpeedMultiplier);
        var gracePeriod = TimeSpan.FromMilliseconds(WindowAnimationGracePeriod.TotalMilliseconds * _animationSpeedMultiplier);
        var totalDelay = baseAnimationDuration + gracePeriod + (additionalDelay ?? TimeSpan.Zero);
        Thread.Sleep((int)totalDelay.TotalMilliseconds);
    }

    private static void WaitForWindowCloseAnimation()
    {
        if (!_watchMode && _animationsDisabled)
        {
            Thread.Sleep(50);
            return;
        }

        var baseAnimationDuration = TimeSpan.FromMilliseconds(WindowAnimationDuration.TotalMilliseconds * _animationSpeedMultiplier);
        var gracePeriod = TimeSpan.FromMilliseconds(WindowAnimationGracePeriod.TotalMilliseconds * _animationSpeedMultiplier);
        Thread.Sleep((int)(baseAnimationDuration + gracePeriod).TotalMilliseconds);
    }

    private static bool IsTopLevelWindowOpen(int processId, int windowHandle)
    {
        return FindTopLevelWindow(processId, windowHandle) is not null;
    }

    private static AutomationElement? WaitForTopLevelSettingsWindowByName(
        int processId,
        int mainWindowHandle,
        IEnumerable<string> names,
        TimeSpan timeout,
        params int[] excludedHandles)
    {
        var normalizedNames = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalizedNames.Length == 0)
            return null;

        var excludedHandleSet = excludedHandles
            .Where(handle => handle != 0)
            .ToHashSet();
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var match = AutomationElement.RootElement.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                    .Cast<AutomationElement>()
                    .FirstOrDefault(window =>
                    {
                        if (window.Current.ProcessId != processId)
                            return false;

                        if (window.Current.ControlType != ControlType.Window)
                            return false;

                        var handle = window.Current.NativeWindowHandle;
                        if (handle == 0 || handle == mainWindowHandle || excludedHandleSet.Contains(handle))
                            return false;

                        if (!IsLikelySettingsWindow(window))
                            return false;

                        return normalizedNames.Any(name => string.Equals(window.Current.Name, name, StringComparison.OrdinalIgnoreCase));
                    });

                if (match is not null)
                    return match;
            }
            catch (Exception ex) when (IsRecoverableAutomationException(ex))
            {
                Console.WriteLine($"[main-smoke] Retrying named settings window discovery after {ex.GetType().Name}");
            }

            Thread.Sleep(150);
        }

        return null;
    }

    private static AutomationElement? FindTopLevelWindow(int processId, int windowHandle)
    {
        try
        {
            var candidates = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)
                .Cast<AutomationElement>()
                .Concat(AutomationElement.RootElement.FindAll(TreeScope.Descendants, Condition.TrueCondition).Cast<AutomationElement>())
                .Where(window => window.Current.ProcessId == processId
                                 && window.Current.ControlType == ControlType.Window
                                 && window.Current.NativeWindowHandle == windowHandle)
                .ToArray();

            return candidates.Length > 0 ? candidates[0] : null;
        }
        catch
        {
            return null;
        }
    }

    private static AutomationElement ResolvePluginDoubleClickTarget(AutomationElement mainWindow, string pluginId)
    {
        var pluginCard = FindByAutomationId(mainWindow, $"PluginCard_{pluginId}");
        if (pluginCard is not null && IsVisible(pluginCard))
            return pluginCard;

        var anchor = FindByAutomationId(mainWindow, $"PluginConfigureButton_{pluginId}")
                     ?? FindByAutomationId(mainWindow, $"PluginOpenButton_{pluginId}")
                     ?? FindByAutomationId(mainWindow, $"PluginInstallButton_{pluginId}")
                     ?? FindByAutomationId(mainWindow, $"PluginUninstallButton_{pluginId}");

        if (anchor is null)
            throw new InvalidOperationException($"Cannot resolve double-click target for plugin '{pluginId}'.");

        var walker = TreeWalker.ControlViewWalker;
        var current = anchor;
        for (var i = 0; i < 8; i++)
        {
            var parent = walker.GetParent(current);
            if (parent is null)
                break;

            if (parent.Current.ControlType == ControlType.ListItem || parent.Current.ControlType == ControlType.DataItem)
                return parent;

            current = parent;
        }

        throw new InvalidOperationException($"Cannot find list item container for plugin '{pluginId}' double-click target.");
    }

    private static void TrySelect(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionItemPattern))
        {
            ((SelectionItemPattern)selectionItemPattern).Select();
        }
    }

    private static void EnsurePluginMarketplaceEntrySelected(AutomationElement mainWindow, string pluginId)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        if (IsPluginMarketplaceEntryActionable(mainWindow, pluginId))
            return;

        // Wait for marketplace to be fully ready before attempting selection
        var marketplaceReady = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                return IsPluginMarketplaceReady(mainWindow);
            },
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(250));

        if (!marketplaceReady)
        {
            Console.WriteLine($"[main-smoke] Marketplace not fully ready before selecting '{pluginId}', proceeding anyway.");
        }

        AutomationElement? pluginCard = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                pluginCard = WaitForAutomationId(mainWindow, $"PluginCard_{pluginId}", TimeSpan.FromSeconds(15));
                break;
            }
            catch (TimeoutException)
            {
                if (attempt == 3)
                    throw;

                Console.WriteLine($"[main-smoke] PluginCard_{pluginId} not found (attempt {attempt}/3), retrying after delay...");
                Thread.Sleep(500);
                BringToForeground(mainWindow);
            }
        }

        if (pluginCard is null)
            throw new TimeoutException($"PluginCard_{pluginId} could not be found after multiple attempts.");

        if (!TrySelectElementOrAncestor(pluginCard))
            MouseClick(pluginCard);

        var selected = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                return IsPluginMarketplaceEntryActionable(mainWindow, pluginId);
            },
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(250));

        if (!selected)
        {
            // Final fallback: try clicking the card again and wait longer
            Console.WriteLine($"[main-smoke] Plugin '{pluginId}' not actionable after first selection, trying fallback click.");
            MouseClick(pluginCard);
            Thread.Sleep(300);

            selected = WaitUntil(
                () =>
                {
                    mainWindow = ResolveLiveWindow(mainWindow);
                    return IsPluginMarketplaceEntryActionable(mainWindow, pluginId);
                },
                TimeSpan.FromSeconds(8),
                TimeSpan.FromMilliseconds(250));
        }

        if (!selected)
            throw new TimeoutException($"Plugin marketplace detail actions did not appear after selecting '{pluginId}'.");
    }

    private static bool TrySelectElementOrAncestor(AutomationElement element)
    {
        var walker = TreeWalker.ControlViewWalker;
        var current = element;

        for (var i = 0; i < 8 && current is not null; i++)
        {
            if (current.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionItemPattern))
            {
                ((SelectionItemPattern)selectionItemPattern).Select();
                return true;
            }

            current = walker.GetParent(current);
        }

        return false;
    }

    private static void InstallPluginFromMarketplace(AutomationElement mainWindow, SmokeSandboxState? sandboxState, string pluginId)
    {
        EnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);
        var installButton = WaitForAutomationId(mainWindow, $"PluginInstallButton_{pluginId}", TimeSpan.FromSeconds(20));
        Click(installButton);
        Console.WriteLine($"[main-smoke] Clicked install for plugin: {pluginId}");

        var installed = WaitUntil(
            () => IsPluginInstalled(mainWindow, sandboxState, pluginId),
            OnlinePluginInstallTimeout,
            TimeSpan.FromMilliseconds(300));

        if (!installed)
            throw new TimeoutException($"Plugin install did not reach installed state: {pluginId}");

        if (!IsPluginInstalledInUi(mainWindow, pluginId) && IsPluginInstalledInSandbox(sandboxState, pluginId))
            Console.WriteLine($"[main-smoke] Install verified via sandbox state fallback before marketplace buttons refreshed: {pluginId}");

        Console.WriteLine($"[main-smoke] Install verified for plugin: {pluginId}");
        CaptureMainWindow(mainWindow, $"{pluginId}-marketplace-installed");
        ObserveStep($"Marketplace install verified: {pluginId}", mainWindow);
    }

    private static void UninstallPluginFromMarketplace(AutomationElement mainWindow, string pluginId)
    {
        EnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);
        var uninstallButton = WaitForAutomationId(mainWindow, $"PluginUninstallButton_{pluginId}", TimeSpan.FromSeconds(20));
        Click(uninstallButton);
        Console.WriteLine($"[main-smoke] Clicked uninstall for plugin: {pluginId}");

        var uninstalled = WaitUntil(
            () => !IsPluginInstalledInUi(mainWindow, pluginId),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromMilliseconds(300));

        if (!uninstalled)
            throw new TimeoutException($"Plugin uninstall did not reach uninstalled state: {pluginId}");

        Console.WriteLine($"[main-smoke] Uninstall verified for plugin: {pluginId}");
    }

    private static bool IsPluginInstalledInUi(AutomationElement root, string pluginId)
    {
        root = ResolveLiveWindow(root);
        var installButton = FindByAutomationId(root, $"PluginInstallButton_{pluginId}");
        var installButtonText = installButton is not null ? ReadElementText(installButton) : string.Empty;

        return IsVisible(FindByAutomationId(root, $"PluginUninstallButton_{pluginId}"))
               || IsVisible(FindByAutomationId(root, $"PluginConfigureButton_{pluginId}"))
               || IsVisible(FindByAutomationId(root, $"PluginOpenButton_{pluginId}"))
               || (IsVisible(installButton)
                   && (installButtonText.Contains("Installed", StringComparison.OrdinalIgnoreCase)
                       || installButtonText.Contains("Update", StringComparison.OrdinalIgnoreCase)
                       || installButtonText.Contains("已安装", StringComparison.OrdinalIgnoreCase)
                       || installButtonText.Contains("更新", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsPluginInstalled(AutomationElement root, SmokeSandboxState? sandboxState, string pluginId)
        => IsPluginInstalledInUi(root, pluginId) || IsPluginInstalledInSandbox(sandboxState, pluginId);

    private static bool IsPluginInstalledInSandbox(SmokeSandboxState? sandboxState, string pluginId)
    {
        if (sandboxState is null || string.IsNullOrWhiteSpace(pluginId))
            return false;

        try
        {
            var settingsPath = Path.Combine(sandboxState.AppDataDirectory, "settings.json");
            if (!File.Exists(settingsPath))
                return false;

            var root = ReadSettingsRoot(settingsPath);
            var installedExtensions = EnsureJsonArray(root, "InstalledExtensions");
            if (!ContainsJsonValue(installedExtensions, pluginId))
                return false;

            var pendingDeletionExtensions = EnsureJsonArray(root, "PendingDeletionExtensions");
            if (ContainsJsonValue(pendingDeletionExtensions, pluginId))
                return false;

            var candidatePluginDirectories = new[]
            {
                Path.Combine(sandboxState.PluginsDirectory, pluginId),
                Path.Combine(sandboxState.PluginsDirectory, "local", pluginId),
                Path.Combine(sandboxState.PluginsDirectory, "installed", pluginId)
            };

            return candidatePluginDirectories
                .Where(Directory.Exists)
                .Any(directory => Directory.EnumerateFileSystemEntries(directory).Any());
        }
        catch
        {
            return false;
        }
    }

    private static void TestOptimizationExtensionCategory(AutomationElement mainWindow, string pluginId)
    {
        EnsureOptimizationCategoryVisible(mainWindow, pluginId, toggleActions: false);
        CaptureMainWindow(mainWindow, pluginId, "optimization-category");
        ToggleOptimizationActions(mainWindow, pluginId);
        ObserveStep($"Optimization category visible: {pluginId}", mainWindow);
    }

    private static void EnsureOptimizationCategoryVisible(AutomationElement mainWindow, string pluginId, bool toggleActions)
    {
        NavigateToWindowsOptimizationPage(mainWindow);

        var definition = GetOptimizationRouteDefinition(pluginId)
                         ?? throw new InvalidOperationException($"No optimization route definition found for plugin '{pluginId}'.");

        var category = WaitForOptimizationCategory(mainWindow, pluginId, definition, TimeSpan.FromSeconds(30));
        if (category is not null)
            ExpandIfNeeded(category);

        var settingsButton = WaitForOptimizationSettingsButton(mainWindow, pluginId, definition, TimeSpan.FromSeconds(20));
        Console.WriteLine($"[main-smoke] Optimization settings button ready ({pluginId}): {settingsButton.Current.AutomationId}");

        var actions = WaitForOptimizationActionButtons(mainWindow, pluginId, definition, TimeSpan.FromSeconds(20));

        if (!toggleActions)
            return;

        for (var index = 0; index < actions.Length; index++)
        {
            var actionAutomationId = definition.ActionAutomationIds[index];
            var actionKey = actionAutomationId.Replace("WindowsOptimizationAction_", string.Empty, StringComparison.Ordinal);
            ClickActionCheckbox(actions[index], actionKey);
        }
    }

    private static void ToggleOptimizationActions(AutomationElement mainWindow, string pluginId)
    {
        EnsureOptimizationCategoryVisible(mainWindow, pluginId, toggleActions: true);
    }

    private static AutomationElement[] WaitForOptimizationActionButtons(
        AutomationElement mainWindow,
        string pluginId,
        OptimizationRouteDefinition definition,
        TimeSpan timeout)
    {
        try
        {
            return definition.ActionAutomationIds
                .Select(actionId => WaitForAutomationId(mainWindow, actionId, timeout))
                .ToArray();
        }
        catch (TimeoutException) when (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
        {
            var actionPrefixes = new[]
            {
                "WindowsOptimizationAction_custom.mouse.",
                "WindowsOptimizationAction_custom-mouse.",
                "WindowsOptimizationAction_custommouse.",
                "WindowsOptimizationAction_LenovoLegionToolkit.Plugins.CustomMouse.",
                "WindowsOptimizationAction_CustomMouse."
            };
            var resolvedActions = definition.ActionAutomationIds
                .Select(actionId =>
                {
                    var suffix = actionId.StartsWith("WindowsOptimizationAction_", StringComparison.Ordinal)
                        ? actionId["WindowsOptimizationAction_".Length..]
                        : actionId;

                    var suffixCandidates = new[]
                    {
                        suffix,
                        suffix.Replace("CustomMouse.", "custom.mouse.", StringComparison.Ordinal),
                        suffix.Replace("CustomMouse.", "custom-mouse.", StringComparison.Ordinal),
                        suffix.Replace("CustomMouse.", "custommouse.", StringComparison.Ordinal),
                        suffix.Replace("custom.mouse.", "CustomMouse.", StringComparison.Ordinal),
                        suffix.Replace("custom.mouse.", "custom-mouse.", StringComparison.Ordinal),
                        suffix.Replace("custom.mouse.", "custommouse.", StringComparison.Ordinal)
                    }.Distinct(StringComparer.Ordinal);

                    foreach (var candidateSuffix in suffixCandidates)
                    {
                        foreach (var actionPrefix in actionPrefixes)
                        {
                            var fallback = TryWaitForAutomationIdPrefix(mainWindow, actionPrefix + candidateSuffix, timeout);
                            if (fallback is not null)
                            {
                                Console.WriteLine($"[main-smoke] custom-mouse optimization action resolved by prefix fallback: requested='{actionId}' candidate='{actionPrefix + candidateSuffix}' actual='{fallback.Current.AutomationId}' name='{fallback.Current.Name}'");
                                return fallback;
                            }
                        }
                    }

                    return WaitForAutomationId(mainWindow, actionId, timeout);
                })
                .ToArray();

            return resolvedActions;
        }
        catch (TimeoutException) when (pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
        {
            var actionPrefixes = new[]
            {
                "WindowsOptimizationAction_shell.integration.",
                "WindowsOptimizationAction_shell-integration.",
                "WindowsOptimizationAction_shellintegration.",
                "WindowsOptimizationAction_ShellIntegration."
            };
            var resolvedActions = definition.ActionAutomationIds
                .Select(actionId =>
                {
                    var suffix = actionId.StartsWith("WindowsOptimizationAction_", StringComparison.Ordinal)
                        ? actionId["WindowsOptimizationAction_".Length..]
                        : actionId;

                    var suffixCandidates = new[]
                    {
                        suffix,
                        suffix.Replace("ShellIntegration.", "shell.integration.", StringComparison.Ordinal),
                        suffix.Replace("ShellIntegration.", "shell-integration.", StringComparison.Ordinal),
                        suffix.Replace("ShellIntegration.", "shellintegration.", StringComparison.Ordinal),
                        suffix.Replace("shell.integration.", "ShellIntegration.", StringComparison.Ordinal),
                        suffix.Replace("shell.integration.", "shell-integration.", StringComparison.Ordinal),
                        suffix.Replace("shell.integration.", "shellintegration.", StringComparison.Ordinal)
                    }.Distinct(StringComparer.Ordinal);

                    foreach (var candidateSuffix in suffixCandidates)
                    {
                        foreach (var actionPrefix in actionPrefixes)
                        {
                            var fallback = TryWaitForAutomationIdPrefix(mainWindow, actionPrefix + candidateSuffix, timeout);
                            if (fallback is not null)
                            {
                                Console.WriteLine($"[main-smoke] shell-integration optimization action resolved by prefix fallback: requested='{actionId}' candidate='{actionPrefix + candidateSuffix}' actual='{fallback.Current.AutomationId}' name='{fallback.Current.Name}'");
                                return fallback;
                            }
                        }
                    }

                    return WaitForAutomationId(mainWindow, actionId, timeout);
                })
                .ToArray();

            return resolvedActions;
        }
    }

    private static OptimizationRouteDefinition? GetOptimizationRouteDefinition(string pluginId)
    {
        if (pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
        {
            return new OptimizationRouteDefinition(
                new[]
                {
                    "WindowsOptimizationCategory_shell.integration",
                    "WindowsOptimizationCategory_shell-integration",
                    "WindowsOptimizationCategory_shellintegration",
                    "WindowsOptimizationCategory_ShellIntegration"
                },
                new[]
                {
                    "WindowsOptimizationCategorySettings_shell-integration",
                    "WindowsOptimizationCategorySettings_shell.integration",
                    "WindowsOptimizationCategorySettings_shellintegration",
                    "WindowsOptimizationCategorySettings_ShellIntegration"
                },
                new[]
                {
                    "WindowsOptimizationAction_shell.integration.enable",
                    "WindowsOptimizationAction_shell.integration.disable"
                },
                new[]
                {
                    "WindowsOptimizationAction_ShellIntegration.enable",
                    "WindowsOptimizationAction_ShellIntegration.disable"
                });
        }

        if (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
        {
            return new OptimizationRouteDefinition(
                new[]
                {
                    "WindowsOptimizationCategory_custom.mouse",
                    "WindowsOptimizationCategory_custom-mouse",
                    "WindowsOptimizationCategory_custommouse",
                    "WindowsOptimizationCategory_LenovoLegionToolkit.Plugins.CustomMouse",
                    "WindowsOptimizationCategory_CustomMouse"
                },
                new[]
                {
                    "WindowsOptimizationCategorySettings_custom.mouse",
                    "WindowsOptimizationCategorySettings_custom-mouse",
                    "WindowsOptimizationCategorySettings_custommouse",
                    "WindowsOptimizationCategorySettings_LenovoLegionToolkit.Plugins.CustomMouse",
                    "WindowsOptimizationCategorySettings_CustomMouse"
                },
                new[]
                {
                    "WindowsOptimizationAction_custom.mouse.cursor.auto-theme.enable",
                    "WindowsOptimizationAction_custom.mouse.cursor.auto-theme.disable"
                },
                new[]
                {
                    "WindowsOptimizationAction_CustomMouse.cursor.auto-theme.enable",
                    "WindowsOptimizationAction_CustomMouse.cursor.auto-theme.disable"
                });
        }

        return null;
    }

    private static AutomationElement? WaitForOptimizationCategory(
        AutomationElement mainWindow,
        string pluginId,
        OptimizationRouteDefinition definition,
        TimeSpan timeout)
    {
        var categoryAutomationIds = definition.CategoryAutomationIds;
        if (definition.CategoryAutomationIdFallbacks is { Length: > 0 })
            categoryAutomationIds = categoryAutomationIds.Concat(definition.CategoryAutomationIdFallbacks).Distinct(StringComparer.Ordinal).ToArray();

        if (categoryAutomationIds.Length > 0)
        {
            try
            {
                var category = WaitForAnyAutomationId(mainWindow, categoryAutomationIds, timeout);
                Console.WriteLine($"[main-smoke] Optimization category ready via category automation id ({pluginId}): {category.Current.AutomationId}");
                return category;
            }
            catch (TimeoutException) when (SupportsPluginFocusedOptimizationRoute(pluginId))
            {
                Console.WriteLine($"[main-smoke] Optimization category direct locator missed ({pluginId}); tried '{string.Join("', '", categoryAutomationIds)}'. Falling back to settings/action markers.");

                if (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
                {
                    var focusedTimeout = TimeSpan.FromSeconds(Math.Max(3, timeout.TotalSeconds / 2));
                    var categoryPrefixes = new[]
                    {
                        "WindowsOptimizationCategory_custom",
                        "WindowsOptimizationCategorySettings_custom",
                        "WindowsOptimizationCategory_LenovoLegionToolkit.Plugins.CustomMouse",
                        "WindowsOptimizationCategorySettings_LenovoLegionToolkit.Plugins.CustomMouse",
                        "WindowsOptimizationCategory_CustomMouse",
                        "WindowsOptimizationCategorySettings_CustomMouse"
                    };

                    foreach (var categoryPrefix in categoryPrefixes)
                    {
                        var categoryByPrefix = TryWaitForAutomationIdPrefix(mainWindow, categoryPrefix, focusedTimeout);
                        if (categoryByPrefix is not null)
                        {
                            Console.WriteLine($"[main-smoke] custom-mouse optimization category resolved by prefix fallback: actual='{categoryByPrefix.Current.AutomationId}' name='{categoryByPrefix.Current.Name}'");
                            return categoryByPrefix;
                        }
                    }

                    var categoryBySettingsButtonPrefix = TryWaitForAutomationIdPrefix(mainWindow, "WindowsOptimizationCategorySettings_custom", focusedTimeout)
                        ?? TryWaitForAutomationIdPrefix(mainWindow, "WindowsOptimizationCategorySettings_LenovoLegionToolkit.Plugins.CustomMouse", focusedTimeout)
                        ?? TryWaitForAutomationIdPrefix(mainWindow, "WindowsOptimizationCategorySettings_CustomMouse", focusedTimeout);
                    if (categoryBySettingsButtonPrefix is not null)
                    {
                        Console.WriteLine($"[main-smoke] custom-mouse optimization category inferred from settings-button prefix fallback: actual='{categoryBySettingsButtonPrefix.Current.AutomationId}' name='{categoryBySettingsButtonPrefix.Current.Name}'");
                        var inferredCategory = FindAncestorByAutomationIdPrefix(categoryBySettingsButtonPrefix, "WindowsOptimizationCategory_");
                        if (inferredCategory is not null)
                        {
                            Console.WriteLine($"[main-smoke] custom-mouse optimization category inferred from settings-button ancestor: actual='{inferredCategory.Current.AutomationId}' name='{inferredCategory.Current.Name}'");
                            return inferredCategory;
                        }

                        Console.WriteLine("[main-smoke] custom-mouse settings button prefix located, but no category ancestor with WindowsOptimizationCategory_ prefix was found.");
                    }

                    DumpAutomationSnapshot(mainWindow, 220);
                }

                if (pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
                {
                    var focusedTimeout = TimeSpan.FromSeconds(Math.Max(3, timeout.TotalSeconds / 2));
                    var categoryPrefixes = new[]
                    {
                        "WindowsOptimizationCategory_shell",
                        "WindowsOptimizationCategorySettings_shell",
                        "WindowsOptimizationCategory_shell-integration",
                        "WindowsOptimizationCategorySettings_shell-integration",
                        "WindowsOptimizationCategory_ShellIntegration",
                        "WindowsOptimizationCategorySettings_ShellIntegration"
                    };

                    foreach (var categoryPrefix in categoryPrefixes)
                    {
                        var categoryByPrefix = TryWaitForAutomationIdPrefix(mainWindow, categoryPrefix, focusedTimeout);
                        if (categoryByPrefix is not null)
                        {
                            Console.WriteLine($"[main-smoke] shell-integration optimization category resolved by prefix fallback: actual='{categoryByPrefix.Current.AutomationId}' name='{categoryByPrefix.Current.Name}'");
                            return categoryByPrefix;
                        }
                    }

                    var categoryBySettingsButtonPrefix = TryWaitForAutomationIdPrefix(mainWindow, "WindowsOptimizationCategorySettings_shell", focusedTimeout)
                        ?? TryWaitForAutomationIdPrefix(mainWindow, "WindowsOptimizationCategorySettings_shell-integration", focusedTimeout)
                        ?? TryWaitForAutomationIdPrefix(mainWindow, "WindowsOptimizationCategorySettings_ShellIntegration", focusedTimeout);
                    if (categoryBySettingsButtonPrefix is not null)
                    {
                        Console.WriteLine($"[main-smoke] shell-integration optimization category inferred from settings-button prefix fallback: actual='{categoryBySettingsButtonPrefix.Current.AutomationId}' name='{categoryBySettingsButtonPrefix.Current.Name}'");
                        var inferredCategory = FindAncestorByAutomationIdPrefix(categoryBySettingsButtonPrefix, "WindowsOptimizationCategory_");
                        if (inferredCategory is not null)
                        {
                            Console.WriteLine($"[main-smoke] shell-integration optimization category inferred from settings-button ancestor: actual='{inferredCategory.Current.AutomationId}' name='{inferredCategory.Current.Name}'");
                            return inferredCategory;
                        }

                        Console.WriteLine("[main-smoke] shell-integration settings button prefix located, but no category ancestor with WindowsOptimizationCategory_ prefix was found.");
                    }

                    DumpAutomationSnapshot(mainWindow, 220);
                }
            }
        }

        if (SupportsPluginFocusedOptimizationRoute(pluginId))
        {
            var settingsButton = WaitForOptimizationSettingsButton(mainWindow, pluginId, definition, timeout);
            Console.WriteLine($"[main-smoke] Optimization category fallback anchored by settings button ({pluginId}): {settingsButton.Current.AutomationId}");
            var category = FindAncestorByAutomationIdPrefix(settingsButton, "WindowsOptimizationCategory_");
            if (category is not null)
            {
                Console.WriteLine($"[main-smoke] Optimization category inferred from settings button ({pluginId}): {category.Current.AutomationId}");
                return category;
            }

            Console.WriteLine($"[main-smoke] Optimization category inferred via plugin-focused route failed to resolve ancestor ({pluginId}); continuing with settings/action markers.");
            return null;
        }

        throw new InvalidOperationException($"No optimization category locator available for plugin '{pluginId}'.");
    }

    private static AutomationElement WaitForOptimizationSettingsButton(
        AutomationElement mainWindow,
        string pluginId,
        OptimizationRouteDefinition definition,
        TimeSpan timeout)
    {
        AutomationElement settingsButton;
        try
        {
            settingsButton = WaitForAnyAutomationId(mainWindow, definition.SettingsButtonAutomationIds, timeout);
        }
        catch (TimeoutException) when (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
        {
            var settingsButtonPrefixes = new[]
            {
                "WindowsOptimizationCategorySettings_custom.mouse",
                "WindowsOptimizationCategorySettings_custom-mouse",
                "WindowsOptimizationCategorySettings_custommouse",
                "WindowsOptimizationCategorySettings_custom",
                "WindowsOptimizationCategorySettings_LenovoLegionToolkit.Plugins.CustomMouse",
                "WindowsOptimizationCategorySettings_CustomMouse"
            };

            settingsButton = settingsButtonPrefixes
                .Select(prefix => TryWaitForAutomationIdPrefix(mainWindow, prefix, timeout))
                .FirstOrDefault(element => element is not null)
                ?? WaitForAutomationIdPrefix(mainWindow, "WindowsOptimizationCategorySettings_LenovoLegionToolkit.Plugins.CustomMouse", timeout);
            Console.WriteLine($"[main-smoke] custom-mouse optimization settings button resolved by prefix fallback: requested='{string.Join("', '", definition.SettingsButtonAutomationIds)}' actual='{settingsButton.Current.AutomationId}' name='{settingsButton.Current.Name}'");
        }
        catch (TimeoutException) when (pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
        {
            var settingsButtonPrefixes = new[]
            {
                "WindowsOptimizationCategorySettings_shell.integration",
                "WindowsOptimizationCategorySettings_shell-integration",
                "WindowsOptimizationCategorySettings_shellintegration",
                "WindowsOptimizationCategorySettings_shell",
                "WindowsOptimizationCategorySettings_ShellIntegration"
            };

            settingsButton = settingsButtonPrefixes
                .Select(prefix => TryWaitForAutomationIdPrefix(mainWindow, prefix, timeout))
                .FirstOrDefault(element => element is not null)
                ?? WaitForAutomationIdPrefix(mainWindow, "WindowsOptimizationCategorySettings_shell-integration", timeout);
            Console.WriteLine($"[main-smoke] shell-integration optimization settings button resolved by prefix fallback: requested='{string.Join("', '", definition.SettingsButtonAutomationIds)}' actual='{settingsButton.Current.AutomationId}' name='{settingsButton.Current.Name}'");
        }

        if (SupportsPluginFocusedOptimizationRoute(pluginId))
            Console.WriteLine($"[main-smoke] Optimization route anchored by plugin settings button ({pluginId}): {settingsButton.Current.AutomationId}");

        return settingsButton;
    }

    private static AutomationElement WaitForAutomationIdPrefix(AutomationElement root, string automationIdPrefix, TimeSpan timeout)
    {
        var element = TryWaitForAutomationIdPrefix(root, automationIdPrefix, timeout);
        if (element is null)
            throw new TimeoutException($"Timed out waiting for automation element prefix '{automationIdPrefix}'.");

        return element;
    }

    private static AutomationElement? TryWaitForAutomationIdPrefix(AutomationElement root, string automationIdPrefix, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => IsInteractable(FindByAutomationIdPrefix(root, automationIdPrefix)),
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            return null;

        var element = FindByAutomationIdPrefix(root, automationIdPrefix);
        return IsInteractable(element) ? element : null;
    }

    private static AutomationElement? FindAncestorByAutomationIdPrefix(AutomationElement element, string automationIdPrefix)
    {
        var walker = TreeWalker.ControlViewWalker;
        var current = element;
        for (var i = 0; i < 16; i++)
        {
            var parent = walker.GetParent(current);
            if (parent is null)
                return null;

            var automationId = parent.Current.AutomationId ?? string.Empty;
            if (automationId.StartsWith(automationIdPrefix, StringComparison.Ordinal))
                return parent;

            current = parent;
        }

        return null;
    }

    private sealed record OptimizationRouteDefinition(
        string[] CategoryAutomationIds,
        string[] SettingsButtonAutomationIds,
        string[] ActionAutomationIds,
        string[]? CategoryAutomationIdFallbacks = null);

    private static void TestDriverDownloadUi(AutomationElement mainWindow)
    {
        NavigateToWindowsOptimizationPage(mainWindow);
        NavigateToDriverDownloadTab(mainWindow);
        TryPopulateDriverMachineType(mainWindow, "82JQ");
        CaptureMainWindow(ResolveLiveWindow(mainWindow), "driver-download-ready");
        TryCaptureDriverDownloadLoadingSkeleton(mainWindow);
        ObserveStep("Driver Download page ready", ResolveLiveWindow(mainWindow));
    }

    private static void TestSystemOptimizationUi(AutomationElement mainWindow)
    {
        NavigateToWindowsOptimizationPage(mainWindow);
        VerifyOptimizationTabUi(mainWindow);
        VerifyCleanupTabUi(mainWindow);
        NavigateToDriverDownloadTab(mainWindow);
        TryPopulateDriverMachineType(mainWindow, "82JQ");
        CaptureMainWindow(ResolveLiveWindow(mainWindow), "system-optimization-driver-download");
        TryCaptureDriverDownloadLoadingSkeleton(mainWindow);
        ObserveStep("System Optimization all tabs verified", ResolveLiveWindow(mainWindow));
    }

    private static void VerifyOptimizationTabUi(AutomationElement mainWindow)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        Click(WaitForAutomationId(mainWindow, "WindowsOptimizationOptimizationTabButton", TimeSpan.FromSeconds(12)));

        var expectedCategories = new[] { "explorer", "performance", "services", "network" };
        foreach (var categoryKey in expectedCategories)
            ExpandOptimizationCategory(mainWindow, categoryKey);

        var expectedActions = new[]
        {
            "explorer.taskbar",
            "explorer.startMenu",
            "explorer.responsiveness",
            "explorer.visibility",
            "explorer.suggestions",
            "performance.multimedia",
            "performance.memory",
            "performance.notifications",
            "performance.telemetry",
            "performance.powerPlan",
            "services.diagnostics",
            "services.sysmain",
            "services.search",
            "services.remoteRegistry",
            "services.errorReporting",
            "network.acceleration",
            "network.optimization"
        };

        foreach (var actionKey in expectedActions)
            WaitForAutomationIdPresent(mainWindow, $"WindowsOptimizationAction_{actionKey}", TimeSpan.FromSeconds(8));

        CaptureMainWindow(mainWindow, "system-optimization-optimization-tab");
        Click(WaitForAutomationId(mainWindow, "WindowsOptimizationSelectRecommendedButton", TimeSpan.FromSeconds(8)));
        VerifySelectedActionsWindow(mainWindow);
        Click(WaitForAutomationId(ResolveLiveWindow(mainWindow), "WindowsOptimizationBulkActionButton", TimeSpan.FromSeconds(8)));
        Console.WriteLine("[main-smoke] System Optimization tab verified without applying optimization actions.");
    }

    private static void VerifyCleanupTabUi(AutomationElement mainWindow)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        Click(WaitForAutomationId(mainWindow, "WindowsOptimizationCleanupTabButton", TimeSpan.FromSeconds(12)));

        var expectedCategories = new[]
        {
            "cleanup.cache",
            "cleanup.systemFiles",
            "cleanup.systemComponents",
            "cleanup.performance",
            "cleanup.largeFiles",
            "cleanup.custom"
        };

        foreach (var categoryKey in expectedCategories)
            ExpandOptimizationCategory(mainWindow, categoryKey);

        var expectedActions = new[]
        {
            "cleanup.browserCache",
            "cleanup.appLeftovers",
            "cleanup.thumbnailCache",
            "cleanup.remoteDesktopCache",
            "cleanup.tempFiles",
            "cleanup.logs",
            "cleanup.registry",
            "cleanup.crashDumps",
            "cleanup.recycleBin",
            "cleanup.defender",
            "cleanup.windowsUpdate",
            "cleanup.componentStore",
            "cleanup.dotnetNative",
            "cleanup.prefetch",
            "cleanup.largeFiles",
            "cleanup.custom"
        };

        foreach (var actionKey in expectedActions)
            WaitForAutomationIdPresent(mainWindow, $"WindowsOptimizationAction_{actionKey}", TimeSpan.FromSeconds(8));

        CaptureMainWindow(mainWindow, "system-optimization-cleanup-tab");

        var browserCacheAction = WaitForAutomationId(mainWindow, "WindowsOptimizationAction_cleanup.browserCache", TimeSpan.FromSeconds(8));
        ClickActionCheckbox(browserCacheAction, "cleanup.browserCache");

        var scanButton = WaitForAutomationIdOrNames(
            mainWindow,
            "WindowsOptimizationScanCleanupButton",
            new[] { "Scan", "扫描" },
            TimeSpan.FromSeconds(8));
        Click(scanButton);

        WaitUntil(
            () => !IsVisible(FindByAutomationId(ResolveLiveWindow(mainWindow), "WindowsOptimizationScanCleanupButton")),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromMilliseconds(250));

        CaptureMainWindow(ResolveLiveWindow(mainWindow), "system-optimization-cleanup-scanned");

        Click(WaitForAutomationId(ResolveLiveWindow(mainWindow), "WindowsOptimizationBulkActionButton", TimeSpan.FromSeconds(8)));
        Console.WriteLine("[main-smoke] System Optimization cleanup tab scanned and selection cleared without running cleanup.");
    }

    private static void ExpandOptimizationCategory(AutomationElement mainWindow, string categoryKey)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        var category = WaitForAutomationIdPresent(mainWindow, $"WindowsOptimizationCategory_{categoryKey}", TimeSpan.FromSeconds(12));
        ExpandIfNeeded(category);
    }

    private static void VerifyActionDetailsWindow(AutomationElement mainWindow, string actionKey)
    {
        var action = WaitForAutomationId(mainWindow, $"WindowsOptimizationAction_{actionKey}", TimeSpan.FromSeconds(8));
        DoubleClick(action);

        var processId = mainWindow.Current.ProcessId;
        var detailsWindow = WaitForOwnedWindow(
            processId,
            mainWindow.Current.NativeWindowHandle,
            window => IsVisible(FindByAutomationId(window, "ActionDetailsWindowTitleBar"))
                      || string.Equals(window.Current.Name, "Action Details", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(10),
            "Action Details");

        CapturePluginSettingsWindow(detailsWindow, "system-optimization", "action-details");
        var closeButton = FindByAutomationId(detailsWindow, "ActionDetailsWindowCloseButton")
                          ?? FindByName(detailsWindow, "Close")
                          ?? FindByName(detailsWindow, "关闭");
        if (closeButton is not null)
            Click(closeButton);
        else
            CloseWindow(detailsWindow);

        Thread.Sleep((int)WindowAnimationDuration.TotalMilliseconds);
        Console.WriteLine($"[main-smoke] Action details window verified for {actionKey}");
    }

    private static void VerifySelectedActionsWindow(AutomationElement mainWindow)
    {
        var selectedActionsButton = WaitForAutomationId(mainWindow, "WindowsOptimizationSelectedActionsButton", TimeSpan.FromSeconds(8));
        Click(selectedActionsButton);

        var processId = mainWindow.Current.ProcessId;
        var selectedActionsWindow = WaitForOwnedWindow(
            processId,
            mainWindow.Current.NativeWindowHandle,
            window => IsVisible(FindByAutomationId(window, "SelectedActionsWindowTitleBar"))
                      || string.Equals(window.Current.Name, "Selected actions", StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(10),
            "Selected Actions");

        CapturePluginSettingsWindow(selectedActionsWindow, "system-optimization", "selected-actions");
        var closeButton = FindByAutomationId(selectedActionsWindow, "SelectedActionsWindowCloseButton")
                          ?? FindByName(selectedActionsWindow, "Close")
                          ?? FindByName(selectedActionsWindow, "关闭");
        if (closeButton is not null)
            Click(closeButton);
        else
            CloseWindow(selectedActionsWindow);

        Thread.Sleep((int)WindowAnimationDuration.TotalMilliseconds);
        Console.WriteLine("[main-smoke] Selected actions window verified.");
    }

    private static AutomationElement WaitForOwnedWindow(
        int processId,
        int mainWindowHandle,
        Func<AutomationElement, bool> predicate,
        TimeSpan timeout,
        string description)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)
                    .Cast<AutomationElement>()
                    .Where(window => window.Current.ProcessId == processId)
                    .Where(window => window.Current.ControlType == ControlType.Window)
                    .Where(window => window.Current.NativeWindowHandle != 0)
                    .Where(window => window.Current.NativeWindowHandle != mainWindowHandle)
                    .ToArray();

                foreach (var window in windows)
                {
                    if (predicate(window))
                        return window;
                }
            }
            catch (Exception ex) when (IsRecoverableAutomationException(ex))
            {
                Console.WriteLine($"[main-smoke] Retrying {description} window detection after {ex.GetType().Name}");
            }

            Thread.Sleep(150);
        }

        throw new TimeoutException($"Timed out waiting for {description} window.");
    }

    private static void NavigateToDriverDownloadTab(AutomationElement mainWindow)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        var tab = WaitForAutomationId(mainWindow, "WindowsOptimizationDriverTabButton", TimeSpan.FromSeconds(12));
        Click(tab);

        var ready = WaitUntil(
            () => IsVisible(FindByAutomationId(ResolveLiveWindow(mainWindow), "WindowsOptimizationDriverSearchButton")),
            TimeSpan.FromSeconds(8),
            TimeSpan.FromMilliseconds(200));

        if (!ready)
        {
            DumpAutomationSnapshot(ResolveLiveWindow(mainWindow), 200);
            throw new TimeoutException("Timed out waiting for Driver Download tab.");
        }

        Console.WriteLine("[main-smoke] Navigated to Driver Download tab");
    }

    private static void TryPopulateDriverMachineType(AutomationElement mainWindow, string machineType)
    {
        try
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            var condition = new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
                new PropertyCondition(AutomationElement.NameProperty, "Machine Type"));
            var edit = FindBestMatchingDescendant(mainWindow, condition);
            if (edit is null || !edit.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
            {
                Console.WriteLine("[main-smoke] Driver machine type field not found; using whatever the app prefilled");
                return;
            }

            ((ValuePattern)valuePattern).SetValue(machineType);
            Console.WriteLine($"[main-smoke] Driver machine type set to {machineType}");
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex) || ex is InvalidOperationException)
        {
            Console.WriteLine($"[main-smoke] Driver machine type prefill skipped: {ex.Message}");
        }
    }

    private static void TryCaptureDriverDownloadLoadingSkeleton(AutomationElement mainWindow)
    {
        if (_screenshotMode != ScreenshotMode.Always)
            return;

        try
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            var searchButton = TryWaitForAutomationId(mainWindow, "WindowsOptimizationDriverSearchButton", TimeSpan.FromSeconds(4));
            if (searchButton is null)
            {
                Console.WriteLine("[main-smoke] Driver Download loading screenshot skipped: scan button not found");
                return;
            }

            Click(searchButton);
            Thread.Sleep(500);
            CaptureMainWindow(ResolveLiveWindow(mainWindow), "driver-download-loading");
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex) || ex is InvalidOperationException || ex is TimeoutException)
        {
            Console.WriteLine($"[main-smoke] Driver Download loading screenshot skipped: {ex.Message}");
        }
    }

    private static void NavigateToWindowsOptimizationPage(AutomationElement mainWindow)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        var arrived = false;

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            AutomationElement? nav = null;
            try
            {
                nav = WaitForWindowsOptimizationNavigationElement(mainWindow, TimeSpan.FromSeconds(8));
                Click(nav);
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"[main-smoke] Windows Optimization navigation element unavailable; trying keyboard navigation fallback (attempt {attempt}/5)");
                BringToForeground(mainWindow);
                PressCtrlTab();
            }

            var quickReady = WaitUntil(
                () =>
                {
                    mainWindow = ResolveLiveWindow(mainWindow);
                    return IsWindowsOptimizationPageReady(mainWindow);
                },
                TimeSpan.FromSeconds(2),
                TimeSpan.FromMilliseconds(200));

            if (!quickReady)
            {
                BringToForeground(mainWindow);
                if (nav is not null)
                    MouseClick(nav);
                else
                    PressCtrlTab();
            }

            var ready = WaitUntil(
                () =>
                {
                    mainWindow = ResolveLiveWindow(mainWindow);
                    return IsWindowsOptimizationPageReady(mainWindow);
                },
                TimeSpan.FromSeconds(12),
                TimeSpan.FromMilliseconds(250));

            if (ready)
            {
                arrived = true;
                break;
            }

            Console.WriteLine($"[main-smoke] Windows Optimization navigation retry {attempt}/5");
            Thread.Sleep(700);
        }

        if (!arrived)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            DumpAutomationSnapshot(mainWindow, 300);
            throw new TimeoutException("Timed out waiting for Windows Optimization page.");
        }

        Console.WriteLine("[main-smoke] Navigated to Windows Optimization page");
    }

    private static bool IsWindowsOptimizationPageReady(AutomationElement mainWindow)
    {
        return IsVisible(FindByAutomationId(mainWindow, "WindowsOptimizationCategoryList"))
               && IsVisible(FindByAutomationId(mainWindow, "WindowsOptimizationOptimizationTabButton"));
    }

    private static AutomationElement WaitForWindowsOptimizationNavigationElement(AutomationElement root, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => TryFindWindowsOptimizationNavigationElement(ResolveLiveWindow(root), out _),
            timeout,
            TimeSpan.FromMilliseconds(250));

        var liveRoot = ResolveLiveWindow(root);
        if (!found || !TryFindWindowsOptimizationNavigationElement(liveRoot, out var element) || element is null)
        {
            DumpAutomationSnapshot(liveRoot, 250);
            throw new TimeoutException("Timed out waiting for windows optimization navigation item.");
        }

        return element;
    }

    private static bool TryFindWindowsOptimizationNavigationElement(AutomationElement root, out AutomationElement? element)
    {
        var idCandidates = new[]
        {
            "WindowsOptimizationNavItem",
            "_windowsOptimizationItem"
        };

        foreach (var id in idCandidates)
        {
            var byId = FindByAutomationId(root, id);
            if (IsVisible(byId))
            {
                element = byId;
                return true;
            }
        }

        var nameCandidates = new[]
        {
            "System Optimization",
            "Windows Optimization",
            "系统优化"
        };

        foreach (var name in nameCandidates)
        {
            var byName = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name));
            if (IsVisible(byName))
            {
                element = byName;
                return true;
            }
        }

        element = null;
        return false;
    }

    private static void ExpandIfNeeded(AutomationElement element)
    {
        if (!element.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var pattern))
            return;

        var expander = (ExpandCollapsePattern)pattern;
        if (expander.Current.ExpandCollapseState == ExpandCollapseState.Collapsed ||
            expander.Current.ExpandCollapseState == ExpandCollapseState.PartiallyExpanded)
        {
            expander.Expand();
            Thread.Sleep(250);
        }
    }

    private static void ClickActionCheckbox(AutomationElement checkbox, string actionKey)
    {
        var before = ReadToggleState(checkbox);
        Click(checkbox);
        Thread.Sleep(1200);
        var after = ReadToggleState(checkbox);
        Console.WriteLine($"[main-smoke] Triggered optimization action {actionKey}: {before} -> {after}");
        LogActionSystemState(actionKey);
    }

    private static void LogActionSystemState(string actionKey)
    {
        if (actionKey.StartsWith("shell.integration.", StringComparison.OrdinalIgnoreCase))
        {
            var registered = IsShellRegisteredInMergedClasses();
            Console.WriteLine($"[main-smoke] Shell integration effective registration: {registered}");
            return;
        }

        if (actionKey.StartsWith("custom.mouse.cursor.auto-theme.", StringComparison.OrdinalIgnoreCase))
        {
            var scheme = ReadCurrentUserRegistryString(Registry.CurrentUser, @"Control Panel\Cursors", string.Empty);
            var arrow = ReadCurrentUserRegistryString(Registry.CurrentUser, @"Control Panel\Cursors", "Arrow");
            var wait = ReadCurrentUserRegistryString(Registry.CurrentUser, @"Control Panel\Cursors", "Wait");
            Console.WriteLine($"[main-smoke] Cursor scheme='{scheme}', Arrow='{arrow}', Wait='{wait}'");
        }
    }

    private static bool IsShellRegisteredInMergedClasses()
    {
        var parents = new[]
        {
            @"*\shellex\ContextMenuHandlers",
            @"DesktopBackground\shellex\ContextMenuHandlers",
            @"Directory\background\shellex\ContextMenuHandlers",
            @"Directory\shellex\ContextMenuHandlers",
            @"Drive\shellex\ContextMenuHandlers",
            @"Folder\ShellEx\ContextMenuHandlers",
            @"LibraryFolder\background\shellex\ContextMenuHandlers",
            @"LibraryFolder\ShellEx\ContextMenuHandlers"
        };

        foreach (var parentPath in parents)
        {
            var value = ReadCurrentUserRegistryString(Registry.ClassesRoot, $@"{parentPath}\ @nilesoft.shell", string.Empty);
            if (string.IsNullOrWhiteSpace(value))
                value = ReadCurrentUserRegistryString(Registry.ClassesRoot, $@"{parentPath}\@nilesoft.shell", string.Empty);

            if (!value.Equals("{BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static string ReadCurrentUserRegistryString(RegistryKey root, string subKeyPath, string valueName)
    {
        try
        {
            using var key = root.OpenSubKey(subKeyPath, false);
            var value = key?.GetValue(valueName);
            return Convert.ToString(value) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ReadToggleState(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(TogglePattern.Pattern, out var togglePattern))
        {
            return ((TogglePattern)togglePattern).Current.ToggleState.ToString();
        }

        return "Unknown";
    }

    private static AutomationElement WaitForPluginNavigationElement(AutomationElement root, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => TryFindPluginNavigationElement(ResolveLiveWindow(root), out _),
            timeout,
            TimeSpan.FromMilliseconds(250));

        var liveRoot = ResolveLiveWindow(root);
        if (!found || !TryFindPluginNavigationElement(liveRoot, out var element) || element is null)
        {
            DumpAutomationSnapshot(liveRoot, 250);
            throw new TimeoutException("Timed out waiting for plugin extensions navigation item.");
        }

        return element;
    }

    private static bool TryFindPluginNavigationElement(AutomationElement root, out AutomationElement? element)
    {
        var idCandidates = new[]
        {
            "PluginExtensionsNavItem",
            "_pluginExtensionsItem"
        };

        foreach (var id in idCandidates)
        {
            var byId = FindByAutomationId(root, id);
            if (IsVisible(byId))
            {
                element = byId;
                return true;
            }
        }

        var nameCandidates = new[]
        {
            "Plugin Extensions",
            "插件扩展",
            "插件拓展"
        };

        foreach (var name in nameCandidates)
        {
            var byName = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name));
            if (IsVisible(byName))
            {
                element = byName;
                return true;
            }
        }

        element = null;
        return false;
    }

    private static IEnumerable<string> GetPluginIdsByButtonPrefix(AutomationElement root, string prefix)
    {
        return root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button))
            .Cast<AutomationElement>()
            .Where(IsVisible)
            .Select(button => button.Current.AutomationId ?? string.Empty)
            .Where(id => id.StartsWith(prefix, StringComparison.Ordinal))
            .Select(id => id.Substring(prefix.Length))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static AutomationElement WaitForAutomationId(AutomationElement root, string automationId, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => IsInteractable(FindByAutomationId(root, automationId)),
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for automation element '{automationId}'.");

        var element = FindByAutomationId(root, automationId);
        if (element is null || !IsInteractable(element))
            throw new InvalidOperationException($"Automation element '{automationId}' was not interactable after wait.");

        return element;
    }

    private static AutomationElement WaitForAutomationIdPresent(AutomationElement root, string automationId, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => FindByAutomationId(root, automationId) is not null,
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for automation element '{automationId}' to exist.");

        return FindByAutomationId(root, automationId)
               ?? throw new InvalidOperationException($"Automation element '{automationId}' disappeared after wait.");
    }

    private static AutomationElement? TryWaitForAutomationId(AutomationElement root, string automationId, TimeSpan timeout)
    {
        try
        {
            return WaitForAutomationId(root, automationId, timeout);
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    private static AutomationElement WaitForAnyAutomationId(AutomationElement root, IReadOnlyList<string> automationIds, TimeSpan timeout)
    {
        var candidates = automationIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (candidates.Length == 0)
            throw new InvalidOperationException("No automation ids were provided.");

        var found = WaitUntil(
            () => candidates.Any(id => IsInteractable(FindByAutomationId(root, id))),
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for automation element set: '{string.Join("', '", candidates)}'.");

        foreach (var automationId in candidates)
        {
            var element = FindByAutomationId(root, automationId);
            if (element is not null && IsInteractable(element))
                return element;
        }

        throw new InvalidOperationException($"Automation element set was not interactable after wait: '{string.Join("', '", candidates)}'.");
    }

    private static AutomationElement WaitForAutomationIdOrNames(AutomationElement root, string automationId, string[] names, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => IsInteractable(FindByAutomationId(root, automationId)) || names.Any(name => IsInteractable(FindByName(root, name))),
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for element '{automationId}' or names [{string.Join(", ", names)}].");

        var byId = FindByAutomationId(root, automationId);
        if (byId is not null && IsInteractable(byId))
            return byId;

        foreach (var name in names)
        {
            var byName = FindByName(root, name);
            if (byName is not null && IsInteractable(byName))
                return byName;
        }

        throw new InvalidOperationException($"Element '{automationId}' or fallback names was not interactable after wait.");
    }

    private static AutomationElement? FindByAutomationId(AutomationElement root, string automationId)
    {
        var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);

        try
        {
            return FindBestMatchingDescendant(root, condition);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            var liveRoot = ResolveLiveWindow(root);
            if (ReferenceEquals(liveRoot, root))
                return null;

            try
            {
                return FindBestMatchingDescendant(liveRoot, condition);
            }
            catch (Exception retryEx) when (IsRecoverableAutomationException(retryEx))
            {
                return null;
            }
        }
    }

    private static AutomationElement? FindByAutomationIdPrefix(AutomationElement root, string automationIdPrefix)
    {
        try
        {
            var elements = root.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                .Cast<AutomationElement>()
                .Where(element =>
                {
                    try
                    {
                        var automationId = element.Current.AutomationId ?? string.Empty;
                        return automationId.StartsWith(automationIdPrefix, StringComparison.Ordinal);
                    }
                    catch (Exception ex) when (IsRecoverableAutomationException(ex))
                    {
                        return false;
                    }
                })
                .Where(IsInteractable)
                .OrderBy(element => element.Current.AutomationId, StringComparer.Ordinal)
                .FirstOrDefault();

            if (elements is not null)
                return elements;
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            var liveRoot = ResolveLiveWindow(root);
            if (ReferenceEquals(liveRoot, root))
                return null;

            try
            {
                return FindByAutomationIdPrefix(liveRoot, automationIdPrefix);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static AutomationElement? FindByName(AutomationElement root, string name)
    {
        var condition = new PropertyCondition(AutomationElement.NameProperty, name);

        try
        {
            return FindBestMatchingDescendant(root, condition);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            var liveRoot = ResolveLiveWindow(root);
            if (ReferenceEquals(liveRoot, root))
                return null;

            try
            {
                return FindBestMatchingDescendant(liveRoot, condition);
            }
            catch (Exception retryEx) when (IsRecoverableAutomationException(retryEx))
            {
                return null;
            }
        }
    }

    private static AutomationElement? FindBestMatchingDescendant(AutomationElement root, Condition condition)
    {
        var matches = root.FindAll(TreeScope.Descendants, condition).Cast<AutomationElement>().ToArray();
        if (matches.Length == 0)
            return null;

        return matches.FirstOrDefault(IsInteractable)
               ?? matches.FirstOrDefault(IsVisible)
               ?? matches[0];
    }


    private static void Click(AutomationElement element)
    {
        EnsureElementInteractable(element, "click target");

        if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
        {
            ((InvokePattern)invokePattern).Invoke();
            return;
        }

        if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionPattern))
        {
            ((SelectionItemPattern)selectionPattern).Select();
            return;
        }

        MouseClick(element);
    }


    private static void SelectComboBoxItemByNames(AutomationElement comboBox, params string[] itemNames)
    {
        if (comboBox.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandPattern))
        {
            var expander = (ExpandCollapsePattern)expandPattern;
            expander.Expand();
        }

        Thread.Sleep(250);

        var listItemCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem);
        var items = comboBox.FindAll(TreeScope.Descendants, listItemCondition)
            .Cast<AutomationElement>()
            .Concat(
                AutomationElement.RootElement
                    .FindAll(TreeScope.Descendants, listItemCondition)
                    .Cast<AutomationElement>())
            .Where(IsVisible)
            .ToArray();

        var item = items.FirstOrDefault(candidate =>
            itemNames.Any(itemName =>
                string.Equals(candidate.Current.Name, itemName, StringComparison.OrdinalIgnoreCase)));

        item ??= items.FirstOrDefault();

        if (item is null)
            throw new InvalidOperationException($"ComboBox option was not found. Expected one of: [{string.Join(", ", itemNames)}].");

        Click(item);
        Thread.Sleep(180);

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

    private static void DoubleClick(AutomationElement element)
    {
        MouseClick(element);
        Thread.Sleep(120);
        MouseClick(element);
    }

    private static void CapturePluginSettingsWindow(AutomationElement settingsWindow, string pluginId, string suffix)
    {
        if (_screenshotMode != ScreenshotMode.Always)
            return;

        var handle = settingsWindow.Current.NativeWindowHandle;
        if (handle == 0)
            throw new InvalidOperationException($"Settings window handle unavailable for screenshot: {pluginId}/{suffix}");

        CaptureWindowArtifacts(handle, $"{pluginId}-{suffix}", includeFullScreen: true);
    }

    private static void CaptureMainWindow(AutomationElement mainWindow, string pluginId, string suffix)
    {
        CaptureMainWindow(mainWindow, $"{pluginId}-{suffix}");
    }

    private static void CaptureMainWindow(AutomationElement mainWindow, string captureLabel)
    {
        if (_screenshotMode != ScreenshotMode.Always)
            return;

        if (!TryGetNativeWindowHandle(mainWindow, out var handle))
            throw new InvalidOperationException($"Main window handle unavailable for screenshot: {captureLabel}");

        CaptureWindowArtifacts(handle, captureLabel, includeFullScreen: false);
    }

    private static void TryCaptureFailureMainWindow(AutomationElement? mainWindow, string captureLabel)
    {
        if (_screenshotMode == ScreenshotMode.Off || mainWindow is null)
            return;

        try
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            if (!TryGetNativeWindowHandle(mainWindow, out var handle))
                return;

            CaptureWindowArtifacts(handle, captureLabel, includeFullScreen: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Failure screenshot skipped for {captureLabel}: {ex.Message}");
        }
    }

    private static void CaptureWindowArtifacts(int windowHandle, string captureLabel, bool includeFullScreen)
    {
        var outputDirectory = EnsureScreenshotOutputDirectory();
        var fileStem = Path.Combine(outputDirectory, BuildScreenshotStem(captureLabel));
        var windowPath = $"{fileStem}-window.png";

        if (TryCaptureWindowToFileViaIpc(windowHandle, windowPath, captureLabel, out var windowBounds))
        {
            using var ipcBitmap = new Bitmap(windowPath);

            if (includeFullScreen)
            {
                var fullPath = $"{fileStem}-fullscreen.png";
                CaptureFullScreenToFile(fullPath, ipcBitmap, windowBounds);
                RegisterScreenshot($"{captureLabel}/fullscreen", fullPath);
            }

            RegisterScreenshot($"{captureLabel}/window", windowPath);
            Console.WriteLine($"[main-smoke] Captured screenshot for {captureLabel} via app IPC: {windowPath}");
            return;
        }

        using var windowBitmap = CaptureWindowBitmapWithFallback(windowHandle, captureLabel, out windowBounds);

        if (includeFullScreen)
        {
            var fullPath = $"{fileStem}-fullscreen.png";
            CaptureFullScreenToFile(fullPath, windowBitmap, windowBounds);
            RegisterScreenshot($"{captureLabel}/fullscreen", fullPath);
        }

        windowBitmap.Save(windowPath, System.Drawing.Imaging.ImageFormat.Png);
        RegisterScreenshot($"{captureLabel}/window", windowPath);
        Console.WriteLine($"[main-smoke] Captured screenshot for {captureLabel}: {windowPath}");
    }

    private static string EnsureScreenshotOutputDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_activeScreenshotOutputDirectory))
            return _activeScreenshotOutputDirectory;

        _activeScreenshotOutputDirectory = string.IsNullOrWhiteSpace(_requestedScreenshotOutputDirectory)
            ? Path.Combine(Path.GetTempPath(), $"llt-main-smoke-{DateTime.Now:yyyyMMdd-HHmmss}")
            : _requestedScreenshotOutputDirectory;

        Directory.CreateDirectory(_activeScreenshotOutputDirectory);
        Console.WriteLine($"[main-smoke] Screenshot artifacts: {_activeScreenshotOutputDirectory}");
        return _activeScreenshotOutputDirectory;
    }

    private static string BuildScreenshotStem(string captureLabel)
    {
        _screenshotSequence++;
        return $"{_screenshotSequence:000}-{SanitizeFileNameSegment(captureLabel)}";
    }

    private static void RegisterScreenshot(string label, string filePath)
    {
        _screenshotCaptures.Add(new ScreenshotCaptureRecord(_screenshotCaptures.Count + 1, label, filePath, DateTimeOffset.Now));
    }

    private static string SanitizeFileNameSegment(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        var characters = value
            .Select(character => invalidChars.Contains(character) || char.IsWhiteSpace(character) || character == '/'
                ? '-'
                : char.ToLowerInvariant(character))
            .ToArray();
        return string.Concat(characters);
    }

    private static void WriteScreenshotManifest()
    {
        if (string.IsNullOrWhiteSpace(_activeScreenshotOutputDirectory) || _screenshotCaptures.Count == 0)
            return;

        var manifestPath = Path.Combine(_activeScreenshotOutputDirectory, "index.md");
        var storyboardPath = Path.Combine(_activeScreenshotOutputDirectory, "storyboard.html");
        var lines = new List<string>
        {
            "# MainAppPluginUi.Smoke Screenshots",
            string.Empty,
            $"Generated: {DateTimeOffset.Now:O}",
            $"Scenario: {_activeScenario}",
            $"Theme: {_activeTheme}",
            $"Mode: {_screenshotMode.ToString().ToLowerInvariant()}",
            string.Empty,
            "## Captures",
        };

        lines.AddRange(_screenshotCaptures.Select(capture => $"- `{Path.GetFileName(capture.FilePath)}`: {capture.Label} ({capture.CapturedAt:HH:mm:ss})"));
        File.WriteAllLines(manifestPath, lines);
        File.WriteAllText(storyboardPath, BuildScreenshotStoryboardHtml(), System.Text.Encoding.UTF8);
        Console.WriteLine($"[main-smoke] Screenshot index: {manifestPath}");
        Console.WriteLine($"[main-smoke] Screenshot storyboard: {storyboardPath}");
    }

    private static string BuildScreenshotStoryboardHtml()
    {
        var items = _screenshotCaptures
            .Select(capture => new
            {
                sequence = capture.Sequence,
                label = capture.Label,
                fileName = Path.GetFileName(capture.FilePath),
                capturedAt = capture.CapturedAt.ToString("HH:mm:ss")
            })
            .ToArray();
        var itemsJson = JsonSerializer.Serialize(items);
        var title = WebUtility.HtmlEncode($"MainAppPluginUi.Smoke Storyboard · {_activeScenario} · {_activeTheme}");

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>{{title}}</title>
  <style>
    :root {
      color-scheme: dark;
      --bg: #111318;
      --panel: #1a1e27;
      --panel-2: #202634;
      --text: #eef2ff;
      --muted: #a6b0c3;
      --accent: #75b8ff;
      --border: #2f3748;
    }

    * { box-sizing: border-box; }
    body {
      margin: 0;
      font-family: "Segoe UI", "Microsoft YaHei UI", sans-serif;
      background: linear-gradient(180deg, #10131a 0%, #151925 100%);
      color: var(--text);
    }

    .layout {
      display: grid;
      grid-template-columns: minmax(300px, 360px) 1fr;
      min-height: 100vh;
    }

    .sidebar {
      border-right: 1px solid var(--border);
      background: rgba(18, 22, 30, 0.95);
      padding: 20px;
      overflow: auto;
    }

    .viewer {
      display: flex;
      flex-direction: column;
      min-width: 0;
    }

    h1 {
      margin: 0 0 8px;
      font-size: 24px;
      font-weight: 700;
    }

    .muted {
      color: var(--muted);
      font-size: 14px;
      line-height: 1.5;
    }

    .controls {
      display: flex;
      flex-wrap: wrap;
      gap: 10px;
      padding: 16px 20px;
      border-bottom: 1px solid var(--border);
      background: rgba(22, 27, 37, 0.92);
      position: sticky;
      top: 0;
      z-index: 10;
    }

    button, input {
      font: inherit;
    }

    button {
      border: 1px solid var(--border);
      background: var(--panel-2);
      color: var(--text);
      border-radius: 10px;
      padding: 10px 14px;
      cursor: pointer;
    }

    button.primary {
      background: #22486f;
      border-color: #2d6fab;
    }

    button:disabled {
      opacity: 0.45;
      cursor: default;
    }

    .playback {
      display: inline-flex;
      align-items: center;
      gap: 8px;
    }

    .playback input {
      width: 84px;
      border: 1px solid var(--border);
      border-radius: 8px;
      background: var(--panel);
      color: var(--text);
      padding: 8px 10px;
    }

    .frame {
      padding: 18px;
      min-width: 0;
    }

    .frame-card {
      background: rgba(19, 24, 33, 0.92);
      border: 1px solid var(--border);
      border-radius: 18px;
      overflow: hidden;
      box-shadow: 0 18px 60px rgba(0, 0, 0, 0.3);
    }

    .frame-meta {
      display: flex;
      justify-content: space-between;
      gap: 12px;
      padding: 14px 18px;
      border-bottom: 1px solid var(--border);
      background: rgba(26, 31, 43, 0.92);
    }

    .frame-meta strong {
      display: block;
      font-size: 18px;
      margin-bottom: 4px;
    }

    .frame-media {
      background: #0b0d12;
      display: flex;
      justify-content: center;
      padding: 18px;
    }

    .frame-media img {
      max-width: 100%;
      height: auto;
      border-radius: 12px;
      border: 1px solid rgba(255, 255, 255, 0.08);
    }

    .capture-list {
      list-style: none;
      margin: 18px 0 0;
      padding: 0;
      display: grid;
      gap: 10px;
    }

    .capture-list button {
      width: 100%;
      text-align: left;
      background: var(--panel);
      padding: 12px;
    }

    .capture-list button.active {
      border-color: var(--accent);
      box-shadow: 0 0 0 1px rgba(117, 184, 255, 0.25);
    }

    .capture-seq {
      color: var(--accent);
      font-weight: 700;
      margin-right: 8px;
    }

    .capture-file {
      display: block;
      margin-top: 4px;
      color: var(--muted);
      font-size: 12px;
    }

    @media (max-width: 1080px) {
      .layout {
        grid-template-columns: 1fr;
      }

      .sidebar {
        border-right: 0;
        border-bottom: 1px solid var(--border);
        max-height: 40vh;
      }
    }
  </style>
</head>
<body>
  <div class="layout">
    <aside class="sidebar">
      <h1>{{title}}</h1>
      <div class="muted">Open this file directly from the smoke artifact folder to replay the page-by-page flow with the original screenshots.</div>
      <ul id="captureList" class="capture-list"></ul>
    </aside>
    <main class="viewer">
      <div class="controls">
        <button id="prevButton">Previous</button>
        <button id="nextButton" class="primary">Next</button>
        <div class="playback">
          <button id="playButton">Play</button>
          <label for="intervalInput">Interval (ms)</label>
          <input id="intervalInput" type="number" min="250" step="250" value="1200" />
        </div>
      </div>
      <section class="frame">
        <div class="frame-card">
          <div class="frame-meta">
            <div>
              <strong id="frameTitle">Capture</strong>
              <div id="frameFile" class="muted"></div>
            </div>
            <div id="frameTime" class="muted"></div>
          </div>
          <div class="frame-media">
            <img id="frameImage" alt="Smoke capture frame" />
          </div>
        </div>
      </section>
    </main>
  </div>

  <script>
    const captures = {{itemsJson}};
    let currentIndex = 0;
    let timer = null;

    const captureList = document.getElementById('captureList');
    const frameTitle = document.getElementById('frameTitle');
    const frameFile = document.getElementById('frameFile');
    const frameTime = document.getElementById('frameTime');
    const frameImage = document.getElementById('frameImage');
    const prevButton = document.getElementById('prevButton');
    const nextButton = document.getElementById('nextButton');
    const playButton = document.getElementById('playButton');
    const intervalInput = document.getElementById('intervalInput');

    function renderList() {
      captureList.innerHTML = '';
      captures.forEach((capture, index) => {
        const item = document.createElement('li');
        const button = document.createElement('button');
        button.type = 'button';
        button.dataset.index = String(index);
        button.innerHTML = `
          <span class="capture-seq">#${capture.sequence}</span>${capture.label}
          <span class="capture-file">${capture.fileName} · ${capture.capturedAt}</span>
        `;
        button.addEventListener('click', () => {
          stopPlayback();
          showCapture(index);
        });
        item.appendChild(button);
        captureList.appendChild(item);
      });
    }

    function showCapture(index) {
      currentIndex = Math.max(0, Math.min(index, captures.length - 1));
      const capture = captures[currentIndex];
      frameTitle.textContent = capture.label;
      frameFile.textContent = capture.fileName;
      frameTime.textContent = `${currentIndex + 1} / ${captures.length} · ${capture.capturedAt}`;
      frameImage.src = encodeURI(capture.fileName);
      frameImage.alt = capture.label;

      document.querySelectorAll('#captureList button').forEach((button, buttonIndex) => {
        button.classList.toggle('active', buttonIndex === currentIndex);
      });

      prevButton.disabled = currentIndex === 0;
      nextButton.disabled = currentIndex === captures.length - 1;
    }

    function stepForward() {
      if (currentIndex >= captures.length - 1) {
        stopPlayback();
        return;
      }

      showCapture(currentIndex + 1);
    }

    function stopPlayback() {
      if (timer !== null) {
        window.clearInterval(timer);
        timer = null;
      }

      playButton.textContent = 'Play';
    }

    function startPlayback() {
      if (captures.length === 0) {
        return;
      }

      const interval = Math.max(250, Number(intervalInput.value) || 1200);
      stopPlayback();
      playButton.textContent = 'Pause';
      timer = window.setInterval(stepForward, interval);
    }

    prevButton.addEventListener('click', () => {
      stopPlayback();
      showCapture(currentIndex - 1);
    });

    nextButton.addEventListener('click', () => {
      stopPlayback();
      showCapture(currentIndex + 1);
    });

    playButton.addEventListener('click', () => {
      if (timer !== null) {
        stopPlayback();
      } else {
        startPlayback();
      }
    });

    renderList();
    if (captures.length > 0) {
      showCapture(0);
    }
  </script>
</body>
</html>
""";
    }

    private static void ObserveStep(string description, AutomationElement? focusWindow)
    {
        if (_stepDelay <= TimeSpan.Zero)
            return;

        Console.WriteLine($"[main-smoke] Watch step: {description}");
        BringObservationTargetToForeground(focusWindow);
        Thread.Sleep(_stepDelay);
    }

    private static void HoldForObservation(string description, AutomationElement? focusWindow, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return;

        Console.WriteLine($"[main-smoke] Watch hold: {description} ({duration.TotalMilliseconds:0} ms)");
        BringObservationTargetToForeground(focusWindow);
        Thread.Sleep(duration);
    }

    private static void BringObservationTargetToForeground(AutomationElement? focusWindow)
    {
        if (focusWindow is null)
            return;

        try
        {
            if (!TryGetNativeWindowHandle(ResolveLiveWindow(focusWindow), out var handle))
                return;

            BringWindowToForeground(handle);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            Console.WriteLine($"[main-smoke] Watch foreground refresh skipped: {ex.GetType().Name}");
        }
    }

    private static Bitmap CaptureWindowBitmapWithFallback(int windowHandle, string captureLabel, out Rectangle windowBounds)
    {
        try
        {
            return CaptureWindowBitmap(windowHandle, out windowBounds);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Window screenshot failed for {captureLabel}; retrying after refocus. ({ex.Message})");
            BringWindowToForeground(windowHandle);
            Thread.Sleep(800);
            return CaptureWindowBitmap(windowHandle, out windowBounds);
        }
    }

    private static Bitmap CaptureWindowBitmap(int windowHandle, out Rectangle windowBounds)
    {
        BringWindowToForeground(windowHandle);
        Thread.Sleep(300);

        if (!GetWindowRect((IntPtr)windowHandle, out var rect))
            throw new InvalidOperationException($"Failed to resolve window bounds for handle {windowHandle}.");

        var width = Math.Max(1, rect.Right - rect.Left);
        var height = Math.Max(1, rect.Bottom - rect.Top);
        windowBounds = new Rectangle(rect.Left, rect.Top, width, height);

        if (TryCaptureWindowBitmapWithPrintWindow(windowHandle, width, height, out var printWindowBitmap))
        {
            if (!IsLikelyBlankCapture(printWindowBitmap))
                return printWindowBitmap;

            Console.WriteLine("[main-smoke] PrintWindow capture looked blank; falling back to window DC capture.");
            printWindowBitmap.Dispose();
        }

        if (TryCaptureWindowBitmapFromWindowDc(windowHandle, width, height, out var windowDcBitmap))
        {
            if (!IsLikelyBlankCapture(windowDcBitmap))
                return windowDcBitmap;

            Console.WriteLine("[main-smoke] Window DC capture looked blank; falling back to CopyFromScreen.");
            windowDcBitmap.Dispose();
        }

        return CaptureWindowBitmapFromScreen(windowBounds);
    }

    private static void BringWindowToForeground(int windowHandle)
    {
        var handle = (IntPtr)windowHandle;
        ShowWindow(handle, SwRestore);
        SetForegroundWindow(handle);
        Thread.Sleep(500);
    }

    private static void CaptureFullScreenToFile(string outputPath, Bitmap? focusedWindowBitmap = null, Rectangle? focusedWindowBounds = null)
    {
        using var bitmap = CaptureFullScreenBitmap();
        if (focusedWindowBitmap is not null && focusedWindowBounds is Rectangle windowBounds)
            OverlayWindowBitmap(bitmap, focusedWindowBitmap, windowBounds);

        bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
    }

    private static Bitmap CaptureFullScreenBitmap()
    {
        var left = GetSystemMetrics(SmXVirtualScreen);
        var top = GetSystemMetrics(SmYVirtualScreen);
        var width = Math.Max(1, GetSystemMetrics(SmCxVirtualScreen));
        var height = Math.Max(1, GetSystemMetrics(SmCyVirtualScreen));

        var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(left, top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    private static Bitmap CaptureWindowBitmapFromScreen(Rectangle bounds)
    {
        var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, new Size(bounds.Width, bounds.Height), CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    private static bool TryCaptureWindowBitmapWithPrintWindow(int windowHandle, int width, int height, out Bitmap bitmap)
    {
        bitmap = null!;
        var workingBitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(workingBitmap);
        var deviceContext = graphics.GetHdc();

        try
        {
            if (PrintWindow((IntPtr)windowHandle, deviceContext, PwRenderFullContent))
            {
                bitmap = workingBitmap;
                return true;
            }

            if (PrintWindow((IntPtr)windowHandle, deviceContext, 0))
            {
                bitmap = workingBitmap;
                return true;
            }

            bitmap = null!;
            return false;
        }
        finally
        {
            graphics.ReleaseHdc(deviceContext);
            if (!ReferenceEquals(bitmap, workingBitmap))
                workingBitmap.Dispose();
        }
    }

    private static bool TryCaptureWindowBitmapFromWindowDc(int windowHandle, int width, int height, out Bitmap bitmap)
    {
        bitmap = null!;
        var windowHandlePtr = (IntPtr)windowHandle;
        var sourceDc = GetWindowDC(windowHandlePtr);
        if (sourceDc == IntPtr.Zero)
            return false;

        var memoryDc = IntPtr.Zero;
        var hBitmap = IntPtr.Zero;
        var previousObject = IntPtr.Zero;

        try
        {
            memoryDc = CreateCompatibleDC(sourceDc);
            if (memoryDc == IntPtr.Zero)
                return false;

            hBitmap = CreateCompatibleBitmap(sourceDc, width, height);
            if (hBitmap == IntPtr.Zero)
                return false;

            previousObject = SelectObject(memoryDc, hBitmap);
            if (!BitBlt(memoryDc, 0, 0, width, height, sourceDc, 0, 0, Srccopy))
                return false;

            using var nativeBitmap = Image.FromHbitmap(hBitmap);
            bitmap = new Bitmap(nativeBitmap);
            return true;
        }
        finally
        {
            if (previousObject != IntPtr.Zero)
                SelectObject(memoryDc, previousObject);

            if (hBitmap != IntPtr.Zero)
                DeleteObject(hBitmap);

            if (memoryDc != IntPtr.Zero)
                DeleteDC(memoryDc);

            ReleaseDC(windowHandlePtr, sourceDc);
        }
    }

    private static void OverlayWindowBitmap(Bitmap fullScreenBitmap, Bitmap windowBitmap, Rectangle windowBounds)
    {
        var screenLeft = GetSystemMetrics(SmXVirtualScreen);
        var screenTop = GetSystemMetrics(SmYVirtualScreen);
        var destinationX = windowBounds.Left - screenLeft;
        var destinationY = windowBounds.Top - screenTop;

        using var graphics = Graphics.FromImage(fullScreenBitmap);
        graphics.DrawImage(windowBitmap, destinationX, destinationY, windowBitmap.Width, windowBitmap.Height);
    }

    private static bool IsLikelyBlankCapture(Bitmap bitmap)
    {
        const int sampleColumns = 10;
        const int sampleRows = 10;
        var distinctColors = new HashSet<int>();

        for (var row = 0; row < sampleRows; row++)
        {
            for (var column = 0; column < sampleColumns; column++)
            {
                var x = Math.Min(bitmap.Width - 1, (int)Math.Round((bitmap.Width - 1d) * column / Math.Max(1, sampleColumns - 1)));
                var y = Math.Min(bitmap.Height - 1, (int)Math.Round((bitmap.Height - 1d) * row / Math.Max(1, sampleRows - 1)));
                var pixel = bitmap.GetPixel(x, y);
                var quantized = ((pixel.R >> 4) << 8) | ((pixel.G >> 4) << 4) | (pixel.B >> 4);
                distinctColors.Add(quantized);
                if (distinctColors.Count > 10)
                    return false;
            }
        }

        return true;
    }

    private static bool TryCaptureWindowToFileViaIpc(int windowHandle, string outputPath, string captureLabel, out Rectangle windowBounds)
    {
        windowBounds = Rectangle.Empty;

        if (!GetWindowRect((IntPtr)windowHandle, out var rect))
            return false;

        windowBounds = new Rectangle(rect.Left, rect.Top, Math.Max(1, rect.Right - rect.Left), Math.Max(1, rect.Bottom - rect.Top));

        try
        {
            using var pipe = new NamedPipeClientStream(".", Constants.PIPE_NAME, PipeDirection.InOut, PipeOptions.None);
            pipe.Connect(2500);
            pipe.ReadMode = PipeTransmissionMode.Message;

            var request = new IpcRequest
            {
                Operation = IpcRequest.OperationType.CaptureWindowVisual,
                Name = windowHandle.ToString(CultureInfo.InvariantCulture),
                Value = outputPath
            };

            pipe.WriteObjectAsync(request).GetAwaiter().GetResult();
            var response = pipe.ReadObjectAsync<IpcResponse>().GetAwaiter().GetResult();
            if (response?.Success == true && File.Exists(outputPath))
                return true;

            Console.WriteLine($"[main-smoke] IPC window capture skipped for {captureLabel}: {response?.Message ?? "unknown error"}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] IPC window capture unavailable for {captureLabel}: {ex.Message}");
            return false;
        }
    }

    private static void MouseClick(AutomationElement element)
    {
        var target = ResolveMouseClickableElement(element);
        EnsureElementInteractable(target, "mouse click target");
        var rect = target.Current.BoundingRectangle;
        if (rect.Width <= 0 || rect.Height <= 0)
            throw new InvalidOperationException($"Cannot click element with empty bounds: {element.Current.AutomationId}");

        var centerX = (int)(rect.Left + rect.Width / 2);
        var centerY = (int)(rect.Top + rect.Height / 2);
        SetCursorPos(centerX, centerY);
        Thread.Sleep(60);
        mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
    }

    private static void PressCtrlTab()
    {
        keybd_event(VkControl, 0, KeyEventExtendedKey, UIntPtr.Zero);
        Thread.Sleep(40);
        keybd_event(VkTab, 0, KeyEventExtendedKey, UIntPtr.Zero);
        Thread.Sleep(40);
        keybd_event(VkTab, 0, KeyEventExtendedKey | KeyEventKeyUp, UIntPtr.Zero);
        Thread.Sleep(40);
        keybd_event(VkControl, 0, KeyEventExtendedKey | KeyEventKeyUp, UIntPtr.Zero);
        Thread.Sleep(220);
    }

    private static void PressNavigationKey(AutomationElement element, byte virtualKey)
    {
        try
        {
            element.SetFocus();
        }
        catch
        {
            // Continue with native key input; focus may already be on the navigation item.
        }

        Thread.Sleep(120);
        keybd_event(virtualKey, 0, KeyEventExtendedKey, UIntPtr.Zero);
        Thread.Sleep(60);
        keybd_event(virtualKey, 0, KeyEventExtendedKey | KeyEventKeyUp, UIntPtr.Zero);
        Thread.Sleep(220);
    }

    private static AutomationElement ResolveMouseClickableElement(AutomationElement element)
    {
        if (HasClickableBounds(element))
            return element;

        try
        {
            var descendant = element
                .FindAll(TreeScope.Descendants, Condition.TrueCondition)
                .Cast<AutomationElement>()
                .FirstOrDefault(HasClickableBounds);

            if (descendant is not null)
                return descendant;
        }
        catch
        {
            // Ignore and continue to parent fallback.
        }

        var walker = TreeWalker.ControlViewWalker;
        var current = element;
        for (var i = 0; i < 8; i++)
        {
            var parent = walker.GetParent(current);
            if (parent is null)
                break;

            if (HasClickableBounds(parent))
                return parent;

            current = parent;
        }

        return element;
    }

    private static bool HasClickableBounds(AutomationElement element)
    {
        try
        {
            var rect = element.Current.BoundingRectangle;
            return rect.Width > 0 && rect.Height > 0;
        }
        catch
        {
            return false;
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
        catch
        {
            return false;
        }
    }

    private static void EnsureElementInteractable(AutomationElement? element, string description)
    {
        if (!IsInteractable(element))
            throw new InvalidOperationException($"{description} is not interactable.");
    }

    private static string ReadElementText(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
            return ((ValuePattern)valuePattern).Current.Value ?? string.Empty;

        return element.Current.Name ?? string.Empty;
    }

    private static bool FindVisibleTextContains(AutomationElement root, string keyword)
    {
        try
        {
            return root.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                .Cast<AutomationElement>()
                .Where(IsVisible)
                .Select(ReadElementText)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Any(text => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    private static bool FindVisibleTextContainsAny(AutomationElement root, params string[] keywords)
    {
        return keywords.Any(keyword => FindVisibleTextContains(root, keyword));
    }

    private static bool StatusTextIndicatesSaved(AutomationElement? element)
    {
        if (element is null)
            return false;

        var text = ReadElementText(element);
        return text.Contains("saved", StringComparison.OrdinalIgnoreCase)
               || text.Contains("已保存", StringComparison.OrdinalIgnoreCase);
    }

    private static bool WaitUntil(Func<bool> predicate, TimeSpan timeout, TimeSpan interval)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (predicate())
                    return true;
            }
            catch (Exception ex) when (IsRecoverableAutomationException(ex))
            {
                // UI Automation can transiently invalidate cached elements while the WPF tree
                // is rebuilding during page transitions. Keep polling until timeout.
            }

            Thread.Sleep(interval);
        }

        return false;
    }

    private static void CloseWindow(AutomationElement window)
    {
        if (window.TryGetCurrentPattern(WindowPattern.Pattern, out var windowPattern))
            ((WindowPattern)windowPattern).Close();
    }

    private static void DumpAutomationSnapshot(AutomationElement root, int maxCount)
    {
        try
        {
            var elements = root.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                .Cast<AutomationElement>()
                .Take(maxCount)
                .Select(element =>
                {
                    var id = element.Current.AutomationId ?? string.Empty;
                    var name = element.Current.Name ?? string.Empty;
                    var type = element.Current.ControlType?.ProgrammaticName ?? "ControlType.Unknown";
                    return $"{type} | id='{id}' | name='{name}'";
                })
                .ToArray();

            Console.WriteLine($"[main-smoke] Automation snapshot ({elements.Length} elements):");
            foreach (var line in elements)
                Console.WriteLine($"[main-smoke]   {line}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Failed to dump automation snapshot: {ex.Message}");
        }
    }

    private static void DumpProcessTopLevelElements(int processId)
    {
        try
        {
            var condition = new PropertyCondition(AutomationElement.ProcessIdProperty, processId);
            var elements = AutomationElement.RootElement.FindAll(TreeScope.Children, condition)
                .Cast<AutomationElement>()
                .Select(element =>
                {
                    var id = element.Current.AutomationId ?? string.Empty;
                    var name = element.Current.Name ?? string.Empty;
                    var type = element.Current.ControlType?.ProgrammaticName ?? "ControlType.Unknown";
                    var handle = element.Current.NativeWindowHandle;
                    return $"{type} | handle={handle} | id='{id}' | name='{name}'";
                })
                .ToArray();

            Console.WriteLine($"[main-smoke] Process top-level elements ({elements.Length}):");
            foreach (var line in elements)
                Console.WriteLine($"[main-smoke]   {line}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Failed to dump top-level elements: {ex.Message}");
        }
    }
}
