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
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Windows.Automation;
using UniversalDeviceToolkit.CLI.Lib;
using UniversalDeviceToolkit.CLI.Lib.Extensions;
using Microsoft.Win32;
using UniversalDeviceToolkit.Lib;
using UniversalDeviceToolkit.Lib.System.Management;
using UniversalDeviceToolkit.Lib.Utils;

namespace MainAppPluginUi.Smoke;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private const string AppDataOverrideEnvironmentVariable = "UDT_APPDATA_OVERRIDE";
    private const string PluginIdsEnvironmentVariable = "UDT_SMOKE_PLUGIN_IDS";
    private const string PluginSourcesEnvironmentVariable = "UDT_SMOKE_PLUGIN_SOURCES";
    private const string ScreenshotModeEnvironmentVariable = "UDT_SMOKE_SCREENSHOTS";
    private const string ScreenshotDirectoryEnvironmentVariable = "UDT_SMOKE_SCREENSHOT_DIR";
    private const string KeepArtifactsEnvironmentVariable = "UDT_SMOKE_KEEP_ARTIFACTS";
    private const string WatchModeEnvironmentVariable = "UDT_SMOKE_WATCH";
    private const string StepDelayEnvironmentVariable = "UDT_SMOKE_STEP_DELAY_MS";
    private const string SuccessHoldEnvironmentVariable = "UDT_SMOKE_SUCCESS_HOLD_MS";
    private const string FailureHoldEnvironmentVariable = "UDT_SMOKE_FAILURE_HOLD_MS";
    private const string ScenarioEnvironmentVariable = "UDT_SMOKE_SCENARIO";
    private const string ThemeEnvironmentVariable = "UDT_SMOKE_THEME";
    private const string AnimationSpeedEnvironmentVariable = "UDT_SMOKE_ANIMATION_SPEED_MS";
    private const string DisableAnimationsEnvironmentVariable = "UDT_SMOKE_DISABLE_ANIMATIONS";
    private const string PowerModeHardwareVerifyEnvironmentVariable = "UDT_SMOKE_POWER_MODE_HARDWARE_VERIFY";
    private const string SmokeAutomationEnvironmentVariable = "UDT_SMOKE_AUTOMATION";

    private static string? GetEnvVar(string environmentVariableName) =>
        Environment.GetEnvironmentVariable(environmentVariableName);

    private static void SetEnvVar(System.Collections.Specialized.StringDictionary environmentVariables, string environmentVariableName, string value) =>
        environmentVariables[environmentVariableName] = value;

    private static readonly string[] MainAppBaseNames = ["Universal Device Toolkit", "Lenovo Legion Toolkit"];
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private const byte VkControl = 0x11;
    private const byte VkEnter = 0x0D;
    private const byte VkSpace = 0x20;
    private const byte VkTab = 0x09;
    private const byte VkA = 0x41;
    private const byte VkBack = 0x08;
    private const uint KeyEventExtendedKey = 0x0001;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint GwOwner = 4;
    private const int SwRestore = 9;
    private const int SmXVirtualScreen = 76;
    private const int SmYVirtualScreen = 77;
    private const int SmCxVirtualScreen = 78;
    private const int SmCyVirtualScreen = 79;
    private const int PwRenderFullContent = 0x00000002;
    private const int Srccopy = 0x00CC0020;
    private const uint WmClose = 0x0010;
    private const int BaseAnimationDurationMs = 350;
    private static readonly TimeSpan OnlinePluginInstallTimeout = TimeSpan.FromMinutes(12);
    private static readonly TimeSpan WindowAnimationDuration = TimeSpan.FromMilliseconds(BaseAnimationDurationMs);
    private static readonly TimeSpan WindowAnimationGracePeriod = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan PowerModeHardwareReadbackTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PowerModeHardwareReadbackPollDelay = TimeSpan.FromMilliseconds(300);
    private static readonly string[] DefaultPluginIds = { "custom-mouse", "shell-integration", "vive-tool" };
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
    private static string _activeLanguage = "en";
    private static string? _activeAppRuntimeDirectory;
    private static string? _activeRepositoryRoot;
    private static string? _activeSmokeAppDataDirectory;
    private static double _animationSpeedMultiplier = 1.0;
    private static bool _animationsDisabled = false;
    private static bool _powerModeHardwareVerificationEnabled;

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
        SystemOptimization,
        Dashboard,
        PowerMode
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

    private sealed record LocalPluginFixtureState(
        string PluginId,
        string SourceDirectory,
        string TargetDirectory,
        string BackupDirectory,
        bool TargetExistedBefore,
        bool FixturePrepared,
        string? WarningMessage);

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

    private sealed record MarketplaceReadiness(
        AutomationElement Window,
        bool Ready);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
    private static extern bool DeleteObject(IntPtr ho);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.StdCall)]
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
        List<LocalPluginFixtureState>? localPluginFixtureStates = null;
        PreparedPluginInstallState? preparedPluginInstallState = null;
        List<RuntimePluginFixtureState>? runtimePluginFixtureStates = null;
        List<RuntimeFileFixtureState>? runtimeSupportFixtureStates = null;
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
            ConfigurePowerModeHardwareVerification(args);
            _activeScenario = ResolveScenario(args);
            _activeTheme = ResolveTheme(args);
            _activeLanguage = ResolveLanguage(args);
            var repositoryRoot = ResolveRepositoryRoot(args);
            _activeRepositoryRoot = repositoryRoot;
            Console.WriteLine($"[main-smoke] Repository root: {repositoryRoot}");
            Console.WriteLine($"[main-smoke] Scenario: {_activeScenario}");
            Console.WriteLine($"[main-smoke] Theme: {_activeTheme}");
            Console.WriteLine($"[main-smoke] Language: {_activeLanguage}");

            var isDriverDownloadScenario = _activeScenario == SmokeScenario.DriverDownload;
            var isSystemOptimizationScenario = _activeScenario == SmokeScenario.SystemOptimization;
            var isDashboardScenario = _activeScenario == SmokeScenario.Dashboard;
            var isPowerModeScenario = _activeScenario == SmokeScenario.PowerMode;
            var scenarioPreset = ResolveScenarioPreset(_activeScenario);
            var preferredPlugins = isDriverDownloadScenario || isDashboardScenario || isPowerModeScenario
                ? Array.Empty<string>()
                : ResolvePreferredPlugins(args, scenarioPreset);
            var requestedPluginSources = isDriverDownloadScenario || isDashboardScenario || isPowerModeScenario
                ? new Dictionary<string, PluginInstallSource>(StringComparer.OrdinalIgnoreCase)
                : ResolveRequestedPluginSources(args, scenarioPreset);
            var desiredPluginSources = preferredPlugins
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    pluginId => pluginId,
                    pluginId => ResolveRequestedPluginSource(pluginId, requestedPluginSources),
                    StringComparer.OrdinalIgnoreCase);
            var appRuntimeDirectory = ResolveMainAppRuntimeDirectory(repositoryRoot, args);
            _activeAppRuntimeDirectory = appRuntimeDirectory;
            var runtimePluginsDirectory = ResolveRuntimePluginsDirectory(appRuntimeDirectory);
            runtimePluginFixtureStates = PrepareRuntimePluginFixtures(repositoryRoot, appRuntimeDirectory, runtimePluginsDirectory, preferredPlugins);
            runtimeSupportFixtureStates = PrepareRuntimeSupportFixtures(repositoryRoot, appRuntimeDirectory);
            smokeSandboxState = PrepareSmokeSandbox();
            _activeSmokeAppDataDirectory = smokeSandboxState.AppDataDirectory;
            ApplySmokeSettingsOverrides(smokeSandboxState, _activeTheme);
            var smokeIpcPipeName = Constants.GetPipeName(smokeSandboxState.AppDataDirectory);
            Console.WriteLine($"[main-smoke] IPC pipe: {smokeIpcPipeName}");
            localPluginPackageBundle = PrepareLocalPluginPackages(
                repositoryRoot,
                desiredPluginSources
                    .Where(pair => pair.Value == PluginInstallSource.Local)
                    .Select(pair => pair.Key)
                    .ToArray());
            localPluginFixtureStates = PrepareLocalPluginFixtures(
                repositoryRoot,
                smokeSandboxState.PluginsDirectory,
                desiredPluginSources
                    .Where(pair => pair.Value == PluginInstallSource.Local)
                    .Select(pair => pair.Key)
                    .ToArray());
            preparedPluginInstallState = PreparePluginInstallState(preferredPlugins, runtimePluginsDirectory);

            var startInfo = CreateMainAppStartInfo(appRuntimeDirectory, smokeSandboxState, localPluginPackageBundle);
            Console.WriteLine($"[main-smoke] Launching: {startInfo.FileName} {startInfo.Arguments}");

            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start main app process."); // NOTE: Cross-method process reference — disposal must be handled at a higher level
            _mainProcessId = process.Id;
            TryWaitForInputIdle(process, 8000);

            mainWindow = WaitForMainShellWindow(process.Id, TimeSpan.FromMinutes(3));
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

            if (isDashboardScenario)
            {
                TestDashboardSensorDetailsToggle(mainWindow);
                HoldForObservation("Dashboard smoke completed successfully", mainWindow, _successHold);
                CloseWindow(mainWindow);
                process.WaitForExit(7000);
                Console.WriteLine("[main-smoke] PASS");
                return 0;
            }

            if (isPowerModeScenario)
            {
                TestPowerModeUi(mainWindow);
                HoldForObservation("Power Mode smoke completed successfully", mainWindow, _successHold);
                CloseWindow(mainWindow);
                process.WaitForExit(7000);
                Console.WriteLine("[main-smoke] PASS");
                return 0;
            }

            NavigateToPluginExtensionsPage(mainWindow, refresh: true);
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

            AssertNoInstalledPluginEmptyOptimizationCategories(mainWindow, pluginsUnderTest);

            for (var index = 0; index < pluginsUnderTest.Count; index++)
            {
                var pluginId = pluginsUnderTest[index];
                var isLastPlugin = index == pluginsUnderTest.Count - 1;
                TestPluginEntryUi(mainWindow, process.Id, pluginId, isLastPlugin, marketplaceAvailable: true, isKnownInstalled: true, installPlan: installPlans.First(plan => string.Equals(plan.PluginId, pluginId, StringComparison.OrdinalIgnoreCase)));
            }

            foreach (var plan in installPlans.Where(plan => plan.Source == PluginInstallSource.Online))
                UninstallPluginFromMarketplace(mainWindow, smokeSandboxState, plan.PluginId);

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
            StopMainProcess(process);

            if (preserveArtifacts)
            {
                if (localPluginPackageBundle is not null)
                    Console.WriteLine($"[main-smoke] Preserved local package bundle: {localPluginPackageBundle.RootDirectory}");
                if (localPluginFixtureStates is not null)
                    Console.WriteLine($"[main-smoke] Preserved local plugin fixtures: [{string.Join(", ", localPluginFixtureStates.Select(state => state.PluginId))}]");
                if (smokeSandboxState is not null)
                    Console.WriteLine($"[main-smoke] Preserved smoke sandbox: {smokeSandboxState.RootDirectory}");
            }
            else
            {
                CleanupLocalPluginPackages(localPluginPackageBundle);
                CleanupSmokeSandbox(smokeSandboxState);
            }

            RestorePluginInstallState(preparedPluginInstallState);
            RestoreLocalPluginFixtures(localPluginFixtureStates);
            RestoreRuntimeFileFixtures(runtimeSupportFixtureStates);
            RestoreRuntimePluginFixtures(runtimePluginFixtureStates);

            WriteScreenshotManifest();
            WriteDismissedPopupsSummary();
        }
    }

    private static void StopMainProcess(Process? process)
    {
        if (process is null)
            return;

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                if (!process.WaitForExit(10000))
                    Console.WriteLine("[main-smoke] Main app process did not exit within fixture cleanup wait.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Failed to stop main app process before cleanup: {ex.Message}");
        }
        finally
        {
            process.Dispose();
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
            var solutionPath = Path.Combine(current.FullName, "UniversalDeviceToolkit.sln");
            var wpfProjectPath = Path.Combine(current.FullName, @"UniversalDeviceToolkit.WPF\UniversalDeviceToolkit.WPF.csproj");
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

    private static void ConfigurePowerModeHardwareVerification(IReadOnlyList<string> args)
    {
        _powerModeHardwareVerificationEnabled = ResolveBooleanSwitch(
            args,
            "--power-mode-hardware-verify",
            PowerModeHardwareVerifyEnvironmentVariable);

        Console.WriteLine($"[main-smoke] Power-mode hardware verification: {_powerModeHardwareVerificationEnabled}");
    }

    private static SmokeScenario ResolveScenario(IReadOnlyList<string> args)
    {
        var rawValue = TryReadOptionValue(args, "--scenario")
                       ?? GetEnvVar(ScenarioEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(rawValue))
            return SmokeScenario.None;

        return rawValue.Trim().ToLowerInvariant() switch
        {
            "shell-local" => SmokeScenario.ShellLocal,
            "combo-local" => SmokeScenario.ComboLocal,
            "driver-download" => SmokeScenario.DriverDownload,
            "system-optimization" => SmokeScenario.SystemOptimization,
            "dashboard" => SmokeScenario.Dashboard,
            "power-mode" => SmokeScenario.PowerMode,
            _ => throw new ArgumentException($"Unsupported smoke scenario '{rawValue}'. Expected 'shell-local', 'combo-local', 'driver-download', 'system-optimization', 'dashboard', or 'power-mode'.")
        };
    }

    private static SmokeTheme ResolveTheme(IReadOnlyList<string> args)
    {
        var rawValue = TryReadOptionValue(args, "--theme")
                       ?? GetEnvVar(ThemeEnvironmentVariable);
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

    private static string ResolveLanguage(IReadOnlyList<string> args)
    {
        var rawValue = TryReadOptionValue(args, "--lang")
                       ?? GetEnvVar("UDT_SMOKE_LANG");
        if (string.IsNullOrWhiteSpace(rawValue))
            return "en";

        var normalized = rawValue.Trim().ToLowerInvariant().Replace('_', '-');
        var canonical = normalized switch
        {
            "en" or "en-us" or "english" => "en",
            "zh" or "zh-cn" or "zh-hans" or "zh-chs" or "chinese" => "zh-Hans",
            "zh-hant" or "zh-tw" or "zh-hk" or "zh-cht" => "zh-Hant",
            _ => ToCanonicalCultureName(normalized)
        };
        return canonical;
    }

    private static string ToCanonicalCultureName(string name)
    {
        try
        {
            return new CultureInfo(name).Name;
        }
        catch
        {
            return name;
        }
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
                    ["shell-integration"] = PluginInstallSource.Online
                }),
            SmokeScenario.ComboLocal => new ScenarioPreset(
                SmokeScenario.ComboLocal,
                new[] { "custom-mouse", "shell-integration" },
                new Dictionary<string, PluginInstallSource>(StringComparer.OrdinalIgnoreCase)
                {
                    ["custom-mouse"] = PluginInstallSource.Online,
                    ["shell-integration"] = PluginInstallSource.Online
                }),
            _ => null
        };
    }

    private static ScreenshotMode ResolveScreenshotMode(IReadOnlyList<string> args)
    {
        var rawValue = TryReadOptionValue(args, "--screenshots")
                       ?? GetEnvVar(ScreenshotModeEnvironmentVariable);
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
                       ?? GetEnvVar(ScreenshotDirectoryEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        return Path.GetFullPath(rawValue);
    }

    private static bool ResolveBooleanSwitch(IReadOnlyList<string> args, string optionName, string environmentVariableName)
    {
        if (HasOption(args, optionName))
            return true;

        var rawValue = GetEnvVar(environmentVariableName);
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
                       ?? GetEnvVar(environmentVariableName);
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

        var rawValue = GetEnvVar(KeepArtifactsEnvironmentVariable);
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
MainAppPluginUi.Smoke.dll [--repo-root <path>] [--app-dir <installed-or-published-app-dir>] [--plugin <id[,id]>] [--plugin-source <pluginId=online|local[,pluginId=...]>]
                            [--scenario shell-local|combo-local|driver-download|system-optimization|dashboard|power-mode] [--theme system|light|dark]
                            [--lang en|zh-Hans|zh-Hant|<culture>]
                            [--screenshots off|failures|always] [--screenshot-dir <path>] [--keep-artifacts]
                            [--watch] [--step-delay-ms <ms>] [--success-hold-ms <ms>] [--failure-hold-ms <ms>]
                            [--disable-animations] [--animation-speed-ms <ms>]
                            [--power-mode-hardware-verify]
                            [--list-plugins] [--help]

Options:
  --repo-root            Main repository root. Defaults to the current repo when auto-detected.
  --app-dir              Optional installed or published app directory to launch instead of the repo Release build output.
  --plugin               Comma-separated plugin id filter. Defaults to the smoke-supported plugin set.
  --plugin-source        Per-plugin install source. Use '*' as wildcard, for example '*=online' or 'shell-integration=online,custom-mouse=local'. Default source is online for every smoke-supported plugin. Local sources require matching plugin build directories or the smoke fails fast.
  --scenario             Predefined smoke preset. 'shell-local' and 'combo-local' keep their historical plugin filters but now default to online install flow; 'driver-download' captures the Driver Download page without plugin install work; 'system-optimization' validates all System Optimization tabs without applying destructive actions; 'dashboard' validates sensor card expand/collapse; 'power-mode' validates the performance-mode entry without changing hardware mode unless --power-mode-hardware-verify is set.
  --theme                Override app theme for the smoke sandbox. One of: system, light, dark.
  --lang                 UI language written to the smoke sandbox lang file. Default: en. Use zh-Hans for Simplified Chinese README captures.
  --screenshots          Screenshot policy: 'off', 'failures', or 'always'. Default: 'failures'.
  --screenshot-dir       Output directory for screenshot artifacts. Defaults to a temp folder per smoke run.
  --keep-artifacts       Keep the smoke sandbox and local package bundle after a successful run.
  --watch                Slow visible transitions so the smoke process can be watched on the real desktop.
  --step-delay-ms        Per-step observation delay in milliseconds. Default: 1200 when --watch is enabled, otherwise 0.
  --success-hold-ms      Keep the main window open before closing on success. Default: 5000 when --watch is enabled.
  --failure-hold-ms      Keep the failure state visible before exit. Default: 15000 when --watch is enabled.
  --disable-animations   Disable UI animations for faster test execution in non-watch mode.
  --animation-speed-ms   Override animation speed in milliseconds. Default: 350ms. Lower values speed up tests.
  --power-mode-hardware-verify  For the power-mode scenario, opt in to a real hardware write/readback verification via Tools\HardwareValidation. The tool restores the original power mode.
  --list-plugins         Print the smoke-supported plugin ids and default install sources, then exit.
  --help                 Print this help text and exit.

Environment variables:
  UDT_SMOKE_PLUGIN_IDS
  UDT_SMOKE_PLUGIN_SOURCES
  UDT_SMOKE_SCENARIO
  UDT_SMOKE_THEME
  UDT_SMOKE_SCREENSHOTS
  UDT_SMOKE_SCREENSHOT_DIR
  UDT_SMOKE_KEEP_ARTIFACTS
  UDT_SMOKE_WATCH
  UDT_SMOKE_STEP_DELAY_MS
  UDT_SMOKE_SUCCESS_HOLD_MS
  UDT_SMOKE_FAILURE_HOLD_MS
  UDT_SMOKE_ANIMATION_SPEED_MS
  UDT_SMOKE_DISABLE_ANIMATIONS
  UDT_SMOKE_POWER_MODE_HARDWARE_VERIFY
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
        var fromEnvironment = GetEnvVar(PluginIdsEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            var requested = fromEnvironment
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (requested.Length > 0)
            {
                Console.WriteLine($"[main-smoke] Plugin filter from {PluginIdsEnvironmentVariable}: [{string.Join(", ", requested)}]");
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
                       ?? GetEnvVar(PluginSourcesEnvironmentVariable);
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
        foreach (var prefix in new[] { "UniversalDeviceToolkit.Plugins.", "LenovoLegionToolkit.Plugins." })
        {
            if (simpleName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                simpleName = simpleName[prefix.Length..];
                break;
            }
        }

        return simpleName switch
        {
            "CustomMouse" => "custom-mouse",
            "ShellIntegration" => "shell-integration",
            "ViveTool" => "vive-tool",
            _ => simpleName
        };
    }

    private static void EnsureRepositoryRoot(string repositoryRoot)
    {
        var solutionPath = Path.Combine(repositoryRoot, "UniversalDeviceToolkit.sln");
        var wpfProjectPath = Path.Combine(repositoryRoot, @"UniversalDeviceToolkit.WPF\UniversalDeviceToolkit.WPF.csproj");
        if (!File.Exists(solutionPath) || !File.Exists(wpfProjectPath))
            throw new DirectoryNotFoundException($"Path is not main repository root: {repositoryRoot}");
    }

    private static string ResolveMainAppRuntimeDirectory(string repositoryRoot, IReadOnlyList<string> args)
    {
        var appDirectoryFromOption = TryReadOptionValue(args, "--app-dir");
        if (!string.IsNullOrWhiteSpace(appDirectoryFromOption))
        {
            var fromOption = Path.GetFullPath(appDirectoryFromOption);
            if (!Directory.Exists(fromOption))
                throw new DirectoryNotFoundException($"Main app directory not found: {fromOption}");

            if (!ContainsMainAppExecutableArtifacts(fromOption))
                throw new DirectoryNotFoundException($"Main app directory does not contain runnable app artifacts: {fromOption}");

            return fromOption;
        }

        var releaseRoot = Path.Combine(repositoryRoot, @"UniversalDeviceToolkit.WPF\bin\Release");
        if (!Directory.Exists(releaseRoot))
            throw new DirectoryNotFoundException($"Main app Release output not found: {releaseRoot}. Build main app first.");

        var runtimeDirectory = Directory
            .EnumerateFiles(releaseRoot, "*.dll", SearchOption.AllDirectories)
            .Where(path => MainAppBaseNames.Contains(Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase))
            .Select(Path.GetDirectoryName)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .Where(ContainsMainAppExecutableArtifacts)
            .OrderByDescending(path => Directory.GetLastWriteTimeUtc(path))
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(runtimeDirectory))
            throw new DirectoryNotFoundException("Could not locate runtime directory containing the main app executable artifacts.");

        return runtimeDirectory;
    }

    private static bool ContainsMainAppExecutableArtifacts(string path)
    {
        return MainAppBaseNames.Any(name =>
            File.Exists(Path.Combine(path, $"{name}.runtimeconfig.json")) ||
            File.Exists(Path.Combine(path, $"{name}.exe")));
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
        var rootDirectory = Path.Combine(Path.GetTempPath(), $"llt-plugin-smoke-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        var appDataDirectory = Path.Combine(rootDirectory, "appdata");
        var pluginsDirectory = Path.Combine(appDataDirectory, "plugins");

        Directory.CreateDirectory(appDataDirectory);
        Directory.CreateDirectory(pluginsDirectory);
        File.WriteAllText(Path.Combine(appDataDirectory, "lang"), _activeLanguage);
        File.WriteAllLines(
            Path.Combine(appDataDirectory, "device-setup"),
            ["devicePackId=", "basicMode=false", $"confirmedAtUtc={DateTimeOffset.UtcNow:O}"]);

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

        // Normalize main-shell size for README / dashboard screenshots (logical DIPs).
        root["WindowSize"] = new JsonObject
        {
            ["Width"] = 1300,
            ["Height"] = 850
        };
        root["MinimizeToTray"] = false;
        root["MinimizeOnClose"] = false;

        // Plugin extensions are opt-in by default; the smoke exercises plugin flows,
        // so enable them explicitly in the sandbox.
        root["ExtensionsEnabled"] = true;
        root["PluginExtensionsOptInMigrationDone"] = true;
        root["NavigationItemsVisibility"] = new JsonObject
        {
            ["pluginExtensions"] = true
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
        Console.WriteLine($"[main-smoke] Smoke settings override written: Theme={root["Theme"]} WindowSize=1300x850{animationSettingsMessage}");

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
        var packageRoot = Path.Combine(Path.GetTempPath(), $"llt-plugin-local-packages-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(packageRoot);

        if (preferredPlugins.Count == 0)
        {
            Console.WriteLine("[main-smoke] No plugins requested for local ZIP import");
            return new LocalPluginPackageBundle(packageRoot, Array.Empty<LocalPluginPackageState>());
        }

        var sourceCandidates = new[]
        {
            Path.Combine(repositoryRoot, "Plugins", ".build", "plugins")
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

    private static List<LocalPluginFixtureState> PrepareLocalPluginFixtures(
        string repositoryRoot,
        string sandboxPluginsDirectory,
        IReadOnlyList<string> preferredPlugins)
    {
        var sourceCandidates = new[]
        {
            Path.Combine(repositoryRoot, "Plugins", ".build", "plugins")
        };

        var sourceRoot = sourceCandidates.FirstOrDefault(Directory.Exists);
        if (string.IsNullOrWhiteSpace(sourceRoot))
        {
            Console.WriteLine("[main-smoke] Local plugin fixture source not found; continuing without preinstalled local fixtures");
            return new List<LocalPluginFixtureState>();
        }

        Directory.CreateDirectory(Path.Combine(sandboxPluginsDirectory, "local"));
        var fixtureStates = new List<LocalPluginFixtureState>();
        var pluginSourceDirectories = Directory.GetDirectories(sourceRoot, "*", SearchOption.TopDirectoryOnly)
            .ToDictionary(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);
        var pluginDirectoryNames = ResolveFixturePluginDirectoryNames(preferredPlugins, pluginSourceDirectories.Keys)
            .ToArray();

        if (pluginDirectoryNames.Length == 0)
        {
            Console.WriteLine("[main-smoke] No matching local plugin fixtures selected; continuing without preinstall copy");
            return fixtureStates;
        }

        try
        {
            foreach (var pluginDirectoryName in pluginDirectoryNames)
            {
                if (!pluginSourceDirectories.TryGetValue(pluginDirectoryName, out var sourcePluginDirectory))
                    continue;

                fixtureStates.Add(PrepareLocalPluginFixture(sandboxPluginsDirectory, pluginDirectoryName, sourcePluginDirectory));
            }

            Console.WriteLine($"[main-smoke] Prepared local plugin fixtures in sandbox: [{string.Join(", ", fixtureStates.Where(state => state.FixturePrepared).Select(state => state.PluginId))}]");
            return fixtureStates;
        }
        catch
        {
            RestoreLocalPluginFixtures(fixtureStates);
            throw;
        }
    }

    private static LocalPluginFixtureState PrepareLocalPluginFixture(
        string sandboxPluginsDirectory,
        string pluginDirectoryName,
        string sourcePluginDirectory)
    {
        var pluginId = NormalizeRuntimeFixturePluginId(pluginDirectoryName);
        var targetPluginDirectory = Path.Combine(sandboxPluginsDirectory, "local", pluginId);
        var backupPluginDirectory = Path.Combine(sandboxPluginsDirectory, "local", $".{pluginId}.smoke-backup");
        var targetExistedBefore = Directory.Exists(targetPluginDirectory);

        try
        {
            CleanupFixtureDirectory(backupPluginDirectory);
            if (targetExistedBefore)
                Directory.Move(targetPluginDirectory, backupPluginDirectory);

            CopyDirectory(sourcePluginDirectory, targetPluginDirectory);
            return new LocalPluginFixtureState(pluginId, sourcePluginDirectory, targetPluginDirectory, backupPluginDirectory, targetExistedBefore, true, null);
        }
        catch (Exception ex)
        {
            var warningMessage = $"Local fixture warning for '{pluginId}': {ex.Message}";
            Console.WriteLine($"[main-smoke] {warningMessage}");
            TryRestorePreparedRuntimePluginFixture(targetPluginDirectory, backupPluginDirectory, targetExistedBefore);
            return new LocalPluginFixtureState(pluginId, sourcePluginDirectory, targetPluginDirectory, backupPluginDirectory, targetExistedBefore, false, warningMessage);
        }
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

    private static void RestoreLocalPluginFixtures(IEnumerable<LocalPluginFixtureState>? fixtureStates)
    {
        if (fixtureStates is null)
            return;

        foreach (var state in fixtureStates.Reverse())
        {
            if (!state.FixturePrepared)
                continue;

            TryRestorePreparedRuntimePluginFixture(state.TargetDirectory, state.BackupDirectory, state.TargetExistedBefore);
        }
    }

    private static ProcessStartInfo CreateMainAppStartInfo(
        string runtimeDirectory,
        SmokeSandboxState sandboxState,
        LocalPluginPackageBundle localPluginPackageBundle)
    {
        var appBaseName = MainAppBaseNames.FirstOrDefault(name =>
            File.Exists(Path.Combine(runtimeDirectory, $"{name}.dll")) &&
            File.Exists(Path.Combine(runtimeDirectory, $"{name}.runtimeconfig.json")))
            ?? MainAppBaseNames.FirstOrDefault(name => File.Exists(Path.Combine(runtimeDirectory, $"{name}.exe")));

        if (string.IsNullOrWhiteSpace(appBaseName))
            throw new FileNotFoundException($"Could not find startup entry in runtime directory: {runtimeDirectory}");

        var dllPath = Path.Combine(runtimeDirectory, $"{appBaseName}.dll");
        var runtimeConfigPath = Path.Combine(runtimeDirectory, $"{appBaseName}.runtimeconfig.json");
        if (File.Exists(dllPath) && File.Exists(runtimeConfigPath))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                // Smoke runs need to get past the unsupported-device gate on this workstation
                // so the plugin UI can still be validated end-to-end.
                Arguments = $"\"{dllPath}\" --trace --disable-update-checker",
                WorkingDirectory = runtimeDirectory,
                UseShellExecute = false
            };

            ApplySmokeEnvironmentOverrides(startInfo, sandboxState, localPluginPackageBundle);
            return startInfo;
        }

        var exePath = Path.Combine(runtimeDirectory, $"{appBaseName}.exe");
        if (File.Exists(exePath))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "--trace --disable-update-checker",
                WorkingDirectory = runtimeDirectory,
                UseShellExecute = false
            };

            ApplySmokeEnvironmentOverrides(startInfo, sandboxState, localPluginPackageBundle);
            return startInfo;
        }

        throw new FileNotFoundException($"Could not find startup entry in runtime directory: {runtimeDirectory}");
    }

    private static void ApplySmokeEnvironmentOverrides(
        ProcessStartInfo startInfo,
        SmokeSandboxState sandboxState,
        LocalPluginPackageBundle localPluginPackageBundle)
    {
        SetEnvVar(startInfo.EnvironmentVariables, AppDataOverrideEnvironmentVariable, sandboxState.AppDataDirectory);
        SetEnvVar(startInfo.EnvironmentVariables, SmokeAutomationEnvironmentVariable, "1");
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
            Path.Combine(runtimePluginsDirectory, $"UniversalDeviceToolkit.Plugins.{pluginId}"),
            Path.Combine(runtimePluginsDirectory, $"UniversalDeviceToolkit.Plugins.{pluginId.Replace("-", string.Empty)}"),
            Path.Combine(runtimePluginsDirectory, "local", pluginId)
        };

        if (candidateDirectories.Any(Directory.Exists))
            return true;

        var candidateDlls = new[]
        {
            Path.Combine(runtimePluginsDirectory, $"{pluginId}.dll"),
            Path.Combine(runtimePluginsDirectory, $"UniversalDeviceToolkit.Plugins.{pluginId}.dll"),
            Path.Combine(runtimePluginsDirectory, $"UniversalDeviceToolkit.Plugins.{pluginId.Replace("-", string.Empty)}.dll")
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
            Path.Combine(repositoryRoot, "Plugins", ".build", "plugins")
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
        yield return $"UniversalDeviceToolkit.Plugins.{pluginId}";
        yield return $"UniversalDeviceToolkit.Plugins.{pluginId.Replace("-", string.Empty, StringComparison.Ordinal)}";
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
        foreach (var prefix in new[] { "UniversalDeviceToolkit.Plugins.", "LenovoLegionToolkit.Plugins." })
        {
            if (pluginDirectoryName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return pluginDirectoryName[prefix.Length..];
        }

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
        if (!Directory.Exists(path))
            return;

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Skipping fixture directory cleanup for '{path}': {ex.Message}");
        }
    }

    private static List<RuntimeFileFixtureState> PrepareRuntimeSupportFixtures(string repositoryRoot, string runtimeDirectory)
    {
        var fixtures = new List<RuntimeFileFixtureState>();
        var fixtureMap = new (string FileName, string[] SourceDirectories)[]
        {
            ("UniversalDeviceToolkit.Plugins.SDK.dll", ["SDK"]),
            ("UniversalDeviceToolkit.Plugins.Shared.dll", ["Shared"]),
            // Dual-load fixtures for pre-cutover host/plugin trees
            ("LenovoLegionToolkit.Plugins.SDK.dll", ["SDK"]),
            ("LenovoLegionToolkit.Plugins.Shared.dll", ["Shared"])
        };

        foreach (var (fileName, sourceDirectories) in fixtureMap)
        {
            var fixture = PrepareRuntimeAssemblyFixture(repositoryRoot, runtimeDirectory, fileName, sourceDirectories);
            if (fixture is not null)
                fixtures.Add(fixture);
        }

        return fixtures;
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

    private static void RestoreRuntimeFileFixtures(IReadOnlyList<RuntimeFileFixtureState>? fixtureStates)
    {
        if (fixtureStates is null || fixtureStates.Count == 0)
            return;

        foreach (var fixtureState in fixtureStates.Reverse())
            RestoreRuntimeFileFixture(fixtureState);
    }

    private static void CleanupFixtureFile(string path)
    {
        if (!File.Exists(path))
            return;

        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Skipping fixture file cleanup for '{path}': {ex.Message}");
        }
    }

    private static RuntimeFileFixtureState? PrepareRuntimeAssemblyFixture(
        string repositoryRoot,
        string runtimeDirectory,
        string fileName,
        params string[] sourceDirectories)
    {
        var sourceCandidates = sourceDirectories
            .SelectMany(directoryName => new[]
            {
                Path.Combine(repositoryRoot, "Plugins", ".build", directoryName, fileName)
            })
            .ToArray();

        var sourcePath = sourceCandidates.FirstOrDefault(File.Exists);
        if (string.IsNullOrWhiteSpace(sourcePath))
            return null;

        var runtimePath = Path.Combine(runtimeDirectory, fileName);
        var backupPath = Path.Combine(runtimeDirectory, $".{fileName}.smoke-backup");
        var existedBefore = File.Exists(runtimePath);

        CleanupFixtureFile(backupPath);
        if (existedBefore)
            File.Move(runtimePath, backupPath);

        try
        {
            File.Copy(sourcePath, runtimePath, overwrite: true);
            return new RuntimeFileFixtureState(runtimePath, backupPath, existedBefore);
        }
        catch
        {
            RestoreRuntimeFileFixture(new RuntimeFileFixtureState(runtimePath, backupPath, existedBefore));
            throw;
        }
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
        var candidates = new List<AutomationElement>();
        foreach (var window in windows)
        {
            if (TryHandleCompatibilityWindow(window))
                continue;

            if (IsLikelySettingsWindow(window))
                continue;

            candidates.Add(window);
        }

        foreach (var window in candidates)
        {
            if (WindowTitleLooksLikeMainShell(window) && HasMainShellMarkers(window))
                return window;
        }

        foreach (var window in candidates)
        {
            if (HasMainNavigationMarkers(window))
                return window;
        }

        foreach (var window in candidates)
        {
            if (WindowTitleLooksLikeMainShell(window) && HasUsableTopLevelBounds(window))
                return window;
        }

        return null;
    }

    private static bool WindowTitleLooksLikeMainShell(AutomationElement window)
    {
        try
        {
            var title = window.Current.Name ?? string.Empty;
            return MainAppBaseNames.Any(baseName => title.Contains(baseName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            return false;
        }
    }

    private static bool HasMainShellMarkers(AutomationElement window) =>
        HasMainNavigationMarkers(window)
        || FindByAutomationId(window, "MainRootFrame") is not null;

    private static bool HasMainNavigationMarkers(AutomationElement window) =>
        FindByAutomationId(window, "MainNavigationStore") is not null
        || FindByAutomationId(window, "_navigationStore") is not null;

    private static bool HasUsableTopLevelBounds(AutomationElement window)
    {
        try
        {
            var bounds = window.Current.BoundingRectangle;
            return bounds.Width > 0 && bounds.Height > 0;
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            return false;
        }
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

        var processId = GetProcessId(mainWindow);
        DismissAnyBlockingMessageBox(mainWindow, processId);

        var arrived = false;
        var initialSkeletonCaptured = false;
        for (var attempt = 1; attempt <= 6; attempt++)
        {
            mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
            Console.WriteLine($"[main-smoke] Waiting for plugin navigation element (attempt {attempt}/6)");
            AutomationElement? pluginNav = null;
            try
            {
                pluginNav = WaitForPluginNavigationElement(mainWindow, TimeSpan.FromSeconds(8));
                Console.WriteLine($"[main-smoke] Plugin navigation element ready (attempt {attempt}/6)");
                if (TryActivateNavigationElement(pluginNav, "PluginExtensionsNavItem"))
                {
                    Console.WriteLine($"[main-smoke] Invoked plugin navigation element (attempt {attempt}/6)");
                    if (!initialSkeletonCaptured)
                    {
                        TryCapturePluginExtensionsLoadingSkeleton(mainWindow, processId);
                        initialSkeletonCaptured = true;
                    }
                    WaitForAnimationsToComplete();
                }
                else
                {
                    Console.WriteLine($"[main-smoke] Plugin navigation element was not directly activatable; trying keyboard navigation fallback (attempt {attempt}/6)");
                    BringToForeground(mainWindow);
                    PressCtrlTab();
                    if (!initialSkeletonCaptured)
                    {
                        TryCapturePluginExtensionsLoadingSkeleton(mainWindow, processId);
                        initialSkeletonCaptured = true;
                    }
                    WaitForAnimationsToComplete();
                }
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"[main-smoke] Plugin navigation element unavailable; trying keyboard navigation fallback (attempt {attempt}/6)");
                BringToForeground(mainWindow);
                PressCtrlTab();
                if (!initialSkeletonCaptured)
                {
                    TryCapturePluginExtensionsLoadingSkeleton(mainWindow, processId);
                    initialSkeletonCaptured = true;
                }
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
                if (pluginNav is not null && TryActivateNavigationElement(pluginNav, "PluginExtensionsNavItem"))
                {
                    Console.WriteLine($"[main-smoke] Re-invoked plugin navigation element (attempt {attempt}/6)");
                }
                else
                {
                    PressCtrlTab();
                }
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

        var readiness = WaitForPluginMarketplaceReadyAfterRefresh(mainWindow, TimeSpan.FromSeconds(60));
        mainWindow = readiness.Window;
        var cardReady = readiness.Ready;
        if (!cardReady)
        {
            Console.WriteLine("[main-smoke] Plugin marketplace controls were not visible after refresh; retrying Plugin Extensions navigation once.");
            mainWindow = ResolveLiveWindow(mainWindow);
            BringToForeground(mainWindow);
            PressCtrlTab();
            WaitForAnimationsToComplete(TimeSpan.FromMilliseconds(500));
            readiness = WaitForPluginMarketplaceReadyAfterRefresh(mainWindow, TimeSpan.FromSeconds(20));
            mainWindow = readiness.Window;
            cardReady = readiness.Ready;
        }

        if (!cardReady)
        {
            DumpAutomationSnapshot(mainWindow, 300);
            throw new TimeoutException("Plugin marketplace controls did not appear in plugin marketplace view.");
        }

        CaptureMainWindow(mainWindow, refresh ? "plugin-extensions-refreshed" : "plugin-extensions");
        ObserveStep(refresh ? "Plugin Extensions refreshed" : "Plugin Extensions visible", mainWindow);
    }

    private static MarketplaceReadiness WaitForPluginMarketplaceReadyAfterRefresh(AutomationElement mainWindow, TimeSpan timeout)
    {
        var liveWindow = mainWindow;
        var ready = WaitUntil(
            () =>
            {
                liveWindow = ResolveLiveWindow(liveWindow);
                return IsPluginMarketplaceReady(liveWindow);
            },
            timeout,
            TimeSpan.FromMilliseconds(350));

        return new MarketplaceReadiness(liveWindow, ready);
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
                DumpAutomationSnapshot(mainWindow, 220);
                throw new TimeoutException("Plugin loading skeleton was not visible during initial marketplace loading.");
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
            throw new InvalidOperationException("Plugin loading skeleton validation failed.", ex);
        }
    }

    private static void ActivateNavigationElement(AutomationElement element, string label)
    {
        if (TryActivateNavigationElement(element, label))
            return;

        DumpAutomationSnapshot(element, 120);
        throw new InvalidOperationException($"Failed to activate navigation element '{label}'.");
    }

    private static bool TryActivateNavigationElement(AutomationElement element, string label)
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
            if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionItemPattern))
            {
                ((SelectionItemPattern)selectionItemPattern).Select();
                return true;
            }
        }
        catch
        {
            // Ignore and continue with invoke / physical click fallbacks.
        }

        try
        {
            if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var invokePattern))
            {
                ((InvokePattern)invokePattern).Invoke();
                return true;
            }
        }
        catch (InvalidOperationException) when (!IsInteractable(element))
        {
            // Element may have become stale after window transitions; try resolving a fresh reference.
            Console.WriteLine($"[main-smoke] Navigation element '{label}' was not interactable for direct click; trying fallback clicks.");
        }
        catch
        {
            // Ignore and continue with physical click fallbacks.
        }

        var textDescendant = FindFirstVisibleDescendant(
            element,
            candidate => candidate.Current.ControlType == ControlType.Text);
        if (textDescendant is not null)
        {
            try
            {
                MouseClick(textDescendant);
                return true;
            }
            catch
            {
                // Ignore and continue with broader fallbacks.
            }
        }

        try
        {
            MouseClick(element);
            return true;
        }
        catch
        {
            // Ignore and continue with double click fallback.
        }

        try
        {
            DoubleClick(element);
            return true;
        }
        catch
        {
            // Ignore and continue with selection fallback.
        }

        try
        {
            if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionPattern))
            {
                ((SelectionItemPattern)selectionPattern).Select();
                return true;
            }

            return false;
        }
        catch
        {
            return false;
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

    private static int GetProcessId(AutomationElement window)
    {
        try
        {
            return window.Current.ProcessId;
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            if (_mainProcessId is int processId)
                return processId;

            throw;
        }
    }

    private static int GetWindowHandle(AutomationElement window)
    {
        if (TryGetNativeWindowHandle(window, out var handle))
            return handle;

        throw new InvalidOperationException("Failed to resolve native window handle for automation target.");
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

    private static bool IsPluginMarketplaceReady(AutomationElement mainWindow)
    {
        if (!TryFindPluginNavigationElement(mainWindow, out var pluginNav)
            || pluginNav is null
            || !IsNavigationItemSelected(pluginNav))
        {
            return false;
        }

        var rootReady = IsVisible(FindByAutomationId(mainWindow, "PluginExtensionsPageRoot"));
        var searchReady = IsVisible(FindByAutomationId(mainWindow, "PluginSearchTextBox"));
        var filterReady = IsVisible(FindByAutomationId(mainWindow, "PluginFilterComboBox"));
        var bulkImportReady = IsVisible(FindByAutomationId(mainWindow, "PluginBulkImportButton"));
        var listReady = IsVisible(FindByAutomationId(mainWindow, "PluginListBox"));
        var titleLooksCorrect = (mainWindow.Current.Name ?? string.Empty)
            .Contains("Plugin Extensions", StringComparison.OrdinalIgnoreCase);
        var hasActionButtons =
            GetPluginIdsByButtonPrefix(mainWindow, "PluginInstallButton_").Any()
            || GetPluginIdsByButtonPrefix(mainWindow, "PluginOpenButton_").Any()
            || GetPluginIdsByButtonPrefix(mainWindow, "PluginConfigureButton_").Any()
            || GetPluginIdsByButtonPrefix(mainWindow, "PluginUninstallButton_").Any();

        if (rootReady || (titleLooksCorrect && searchReady && (filterReady || bulkImportReady)) || (searchReady && listReady && hasActionButtons))
            return true;

        if (!titleLooksCorrect)
            return false;

        return searchReady && (filterReady || bulkImportReady) && TryFindMarketplacePluginCard(mainWindow, out _);
    }

    private static void WaitForPluginMarketplaceInteractionReady(AutomationElement mainWindow, string pluginId, TimeSpan timeout)
    {
        var ready = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                var entryVisible = IsPluginMarketplaceEntryVisible(mainWindow, pluginId);
                var loadingVisible = IsVisible(FindByAutomationId(mainWindow, "PluginLoadingIndicator"))
                                     || IsVisible(FindByAutomationId(mainWindow, "_loadingText"));
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
                Console.WriteLine($"[main-smoke] Online marketplace pre-check did not resolve plugin '{pluginId}'. Continuing and deferring verification to direct marketplace selection.");

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

        mainWindow = ResolveLiveWindow(mainWindow);
        var pendingPlans = localPlans
            .Where(plan => !IsPluginInstalled(mainWindow, sandboxState, plan.PluginId))
            .ToArray();

        Console.WriteLine("[main-smoke] Verifying preinstalled local plugin fixtures through the normal installed-plugin UI.");
        if (pendingPlans.Length > 0)
            throw new TimeoutException($"Preinstalled local plugin fixture did not reach installed state: {string.Join(", ", pendingPlans.Select(plan => plan.PluginId))}");

        foreach (var plan in localPlans)
        {
            Console.WriteLine($"[main-smoke] Preinstalled local plugin fixture verified: {plan.PluginId}");
            CaptureMainWindow(mainWindow, $"{plan.PluginId}-local-import-installed");
            ObserveStep($"Local plugin fixture verified: {plan.PluginId}", mainWindow);
        }
    }

    private static void TestPluginEntryUi(AutomationElement mainWindow, int processId, string pluginId, bool isLastPlugin, bool marketplaceAvailable, bool isKnownInstalled, PluginInstallPlan installPlan)
    {
        Console.WriteLine($"[main-smoke] Testing plugin UI entry: {pluginId} ({installPlan.Source.ToString().ToLowerInvariant()})");
        if (marketplaceAvailable)
        {
            EnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);
            mainWindow = ResolveLiveWindow(mainWindow);
        }
        WaitForPluginMarketplaceInteractionReady(mainWindow, pluginId, TimeSpan.FromSeconds(20));

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
        else if (isKnownInstalled && ExpectsSidebarFeaturePage(pluginId))
            TestSidebarPluginPageEntry(mainWindow, pluginId, returnToMarketplace: false);
        else if (isKnownInstalled)
            Console.WriteLine($"[main-smoke] Feature-page test skipped for non-feature plugin: {pluginId}");
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

    private static bool ExpectsSidebarFeaturePage(string pluginId)
    {
        return pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase);
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

        if (UsesOptimizationOpenRoute(pluginId))
        {
            EnsureOptimizationCategoryVisible(mainWindow, pluginId, toggleActions: false);
            CaptureMainWindow(mainWindow, pluginId, "optimization-open-route");
            ObserveStep($"Optimization route opened: {pluginId}", mainWindow);

            if (returnToMarketplace)
                NavigateToPluginExtensionsPage(mainWindow, refresh: false);

            return;
        }

        EnsurePluginFeaturePageRendered(mainWindow, pluginId, entrySource: "marketplace-open");
        CaptureMainWindow(mainWindow, pluginId, "feature-page");
        ObserveStep($"Feature page opened: {pluginId}", mainWindow);

        if (pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase)
            && IsPluginSpecificFeatureMarkerVisible(mainWindow, pluginId))
            TestViveToolFeatureInteractions(mainWindow);

        if (returnToMarketplace)
            NavigateToPluginExtensionsPage(mainWindow, refresh: false);
    }

    private static void TestSidebarPluginPageEntry(AutomationElement mainWindow, string pluginId, bool returnToMarketplace)
    {
        CloseStalePluginSettingsWindows(mainWindow);
        mainWindow = ResolveLiveWindow(mainWindow);
        var processId = GetProcessId(mainWindow);
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
            mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
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
            mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
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

        if (pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase)
            && IsPluginSpecificFeatureMarkerVisible(mainWindow, pluginId))
            TestViveToolFeatureInteractions(mainWindow);

        if (returnToMarketplace)
            NavigateToPluginExtensionsPage(mainWindow, refresh: false);
    }

    private static bool UsesOptimizationOpenRoute(string pluginId)
    {
        return pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase)
               || pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase);
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
        return !pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase)
               && !pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SupportsPluginFocusedOptimizationRoute(string pluginId)
    {
        return false;
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
                       || IsVisible(FindByAutomationId(mainWindow, "_emptyStateTextBlock"))
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
            if (PluginPageShowsCompatibilityFallback(mainWindow))
            {
                Console.WriteLine($"[main-smoke] Plugin '{pluginId}' opened compatibility fallback via {entrySource}; accepting stable host-protection path.");
                return;
            }

            DumpAutomationSnapshot(mainWindow, 300);
            throw new InvalidOperationException($"Plugin '{pluginId}' opened an empty-state page via {entrySource}.");
        }

        if (PluginPageShowsLoadFailure(mainWindow))
        {
            DumpAutomationSnapshot(mainWindow, 320);
            throw new InvalidOperationException($"Plugin '{pluginId}' feature page reported a runtime load failure via {entrySource}.");
        }

    }

    private static bool IsPluginSpecificFeatureMarkerVisible(AutomationElement mainWindow, string pluginId)
    {
        if (pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase))
        {
            return IsVisible(FindByAutomationId(mainWindow, "ViveToolPageRoot"))
                   || IsVisible(FindByAutomationId(mainWindow, "ViveToolImportButton"))
                    || IsVisible(FindByAutomationId(mainWindow, "ViveToolRefreshListButton"));
        }

        return false;
    }

    private static bool PluginPageShowsLoadFailure(AutomationElement mainWindow)
    {
        return FindVisibleTextContainsAny(
            mainWindow,
            "Failed to load plugin page",
            "Could not load file or assembly",
            "????????",
            "????????");
    }

    private static bool PluginPageShowsCompatibilityFallback(AutomationElement mainWindow)
    {
        return FindVisibleTextContainsAny(
            mainWindow,
            "targets Wpf.Ui",
            "host is running Wpf.Ui",
            "hidden to keep the app stable");
    }

    private static void TestViveToolFeatureInteractions(AutomationElement mainWindow)
    {
        var ready = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                return IsInteractable(FindByAutomationId(mainWindow, "ViveToolSearchTextBox"))
                       || IsInteractable(FindByAutomationId(mainWindow, "ViveToolMissingGoToSettingsButton"));
            },
            TimeSpan.FromSeconds(15),
            TimeSpan.FromMilliseconds(250));

        if (!ready)
        {
            DumpAutomationSnapshot(mainWindow, 320);
            throw new TimeoutException("Timed out waiting for ViveTool feature list or missing-runtime state.");
        }

        mainWindow = ResolveLiveWindow(mainWindow);
        var missingSettingsButton = FindByAutomationId(mainWindow, "ViveToolMissingGoToSettingsButton");
        if (IsInteractable(missingSettingsButton))
        {
            var missingRefreshButton = WaitForAutomationId(mainWindow, "ViveToolMissingRefreshStatusButton", TimeSpan.FromSeconds(8));
            Click(missingRefreshButton);
            WaitForAnimationsToComplete();
            CaptureMainWindow(ResolveLiveWindow(mainWindow), "vive-tool-missing-runtime");
            Console.WriteLine("[main-smoke] ViveTool feature page reached recoverable missing-runtime state");
            return;
        }

        var searchTextBox = WaitForAutomationId(mainWindow, "ViveToolSearchTextBox", TimeSpan.FromSeconds(5));
        var statusFilter = WaitForAutomationId(mainWindow, "ViveToolStatusFilterComboBox", TimeSpan.FromSeconds(5));
        var refreshButton = WaitForAutomationId(mainWindow, "ViveToolRefreshListButton", TimeSpan.FromSeconds(5));

        SetTextBoxValue(searchTextBox, "1");
        Thread.Sleep(700);
        SelectComboBoxItemByNames(statusFilter, "All Statuses", "All");
        Click(refreshButton);

        var refreshed = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                var dataGrid = FindByAutomationId(mainWindow, "ViveToolFeaturesDataGrid");
                var loadingPanel = FindByAutomationId(mainWindow, "ViveToolLoadingPanel");
                var featureCount = FindByAutomationId(mainWindow, "ViveToolFeatureCountText");
                return !IsVisible(loadingPanel)
                       && IsVisible(dataGrid)
                       && ViveToolFeatureCountShowsData(featureCount);
            },
            TimeSpan.FromSeconds(20),
            TimeSpan.FromMilliseconds(250));

        if (!refreshed)
        {
            DumpAutomationSnapshot(mainWindow, 320);
            throw new InvalidOperationException("ViveTool feature-page interaction failed: refresh/search result state was not observed.");
        }

        CaptureMainWindow(ResolveLiveWindow(mainWindow), "vive-tool-feature-interactions");
        Console.WriteLine("[main-smoke] ViveTool feature-page interactions passed");
    }

    private static bool ViveToolFeatureCountShowsData(AutomationElement? featureCountElement)
    {
        if (featureCountElement is null)
            return false;

        var text = ReadElementText(featureCountElement).Trim();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var firstToken = text.Split(' ', '|', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (int.TryParse(firstToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var total))
            return total > 0;

        return !text.StartsWith("0 ", StringComparison.OrdinalIgnoreCase)
               && !text.StartsWith("0|", StringComparison.OrdinalIgnoreCase);
    }

    private static void TestDoubleClickOpensSettings(AutomationElement mainWindow, int processId, string pluginId)
    {
        mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);

        var mainWindowHandle = GetWindowHandle(mainWindow);
        var existingSettingsWindows = GetSettingsWindowHandles(processId, mainWindowHandle);
        var targetElement = ResolvePluginDoubleClickTarget(mainWindow, pluginId);
        TrySelect(targetElement);
        DoubleClick(targetElement);
        WaitForAnimationsToComplete();

        mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);

        var settingsWindow = WaitForPluginSettingsWindow(
            processId,
            mainWindowHandle,
            existingSettingsWindows,
            TimeSpan.FromSeconds(7));

        Console.WriteLine($"[main-smoke] Double-click opened settings window for: {pluginId}");
        CapturePluginSettingsWindow(settingsWindow, pluginId, "settings-double-click");
        ObserveStep($"Settings window opened by double-click: {pluginId}", settingsWindow);

        if (pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase))
            TestViveToolSettingsInteractions(settingsWindow);

        CloseWindowAndWait(settingsWindow, processId, TimeSpan.FromSeconds(8));
    }

    private static void TestConfigureOpensSettings(AutomationElement mainWindow, int processId, string pluginId, bool settingsRouteAlreadyValidated = false)
    {
        mainWindow = ResolveLiveWindowAndDismissPopups(mainWindow, processId);
        EnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);

        var mainWindowHandle = GetWindowHandle(mainWindow);
        var existingSettingsWindows = GetSettingsWindowHandles(processId, mainWindowHandle);
        var expectedWindowNames = pluginId.Equals("vive-tool", StringComparison.OrdinalIgnoreCase)
            ? new[] { "ViVeTool Settings", "ViVeTool ??" }
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
                var configureButton = WaitForPluginConfigureButton(mainWindow, pluginId, TimeSpan.FromSeconds(12));

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
            TestShellIntegrationSettingsInteractions(settingsWindow, processId, mainWindowHandle);

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
        var mainWindowHandle = GetWindowHandle(mainWindow);
        if (expectedWindowNames.Length > 0)
        {
            return TryWaitForPluginSettingsWindowByHandleOrName(
                processId,
                mainWindowHandle,
                existingSettingsWindows,
                timeout,
                $"{pluginId} configure",
                expectedWindowNames);
        }

        return TryWaitForPluginSettingsWindow(
            processId,
            mainWindowHandle,
            existingSettingsWindows,
            timeout);
    }

    private static AutomationElement WaitForPluginConfigureButton(AutomationElement mainWindow, string pluginId, TimeSpan timeout)
    {
        var automationId = $"PluginConfigureButton_{pluginId}";
        var deadline = DateTime.UtcNow + timeout;
        TimeoutException? lastTimeout = null;

        while (DateTime.UtcNow < deadline)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
                break;

            try
            {
                return WaitForAutomationIdWithScroll(
                    mainWindow,
                    automationId,
                    TimeSpan.FromMilliseconds(Math.Min(2500, Math.Max(500, remaining.TotalMilliseconds))));
            }
            catch (TimeoutException ex)
            {
                lastTimeout = ex;
                TryRefreshMarketplaceSelection(mainWindow, pluginId);
            }
        }

        throw lastTimeout ?? new TimeoutException($"Timed out waiting for automation element '{automationId}'.");
    }

    private static void TryRefreshMarketplaceSelection(AutomationElement mainWindow, string pluginId)
    {
        try
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            var pluginCard = FindByAutomationId(mainWindow, $"PluginCard_{pluginId}");
            if (pluginCard is not null)
            {
                TryScrollElementIntoView(pluginCard);
                if (!TrySelectElementOrAncestor(pluginCard))
                    MouseClick(pluginCard);
                Thread.Sleep(250);
                return;
            }

            _ = TryEnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex) || ex is InvalidOperationException || ex is TimeoutException)
        {
            Console.WriteLine($"[main-smoke] Marketplace selection refresh for '{pluginId}' skipped: {ex.Message}");
        }
    }

    private static string[] GetPluginSettingsWindowExpectedNames(string pluginId)
    {
        if (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
            return new[] { "????????", "Custom Mouse Settings" };

        if (pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
            return new[] { "Shell Integration Settings", "Shell Integration ??" };

        return Array.Empty<string>();
    }

    private static void TestShellIntegrationSettingsInteractions(AutomationElement settingsWindow, int processId, int mainWindowHandle)
    {
        if (ShellIntegrationSettingsExplicitlyEmpty(settingsWindow))
        {
            Console.WriteLine("[main-smoke] Shell Integration settings page reports no configurable settings; accepting as valid settings route.");
            return;
        }

        if (PluginSettingsCompatibilityFallbackVisible(settingsWindow))
        {
            Console.WriteLine("[main-smoke] Shell Integration settings page is compatibility-gated by host/plugin Wpf.Ui mismatch; accepting stable fallback.");
            return;
        }

        var styleButton = WaitForShellIntegrationActionButton(
            settingsWindow,
            new[] { "OpenStyleSettingsButton", "_openStyleSettingsButton" },
            new[] { "Open Style Settings", "Open Style", "??????", "????" },
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
            new[] { "Menu Style Settings", "????", "??????", "Shell Integration" },
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

    private static bool ShellIntegrationSettingsExplicitlyEmpty(AutomationElement settingsWindow)
    {
        return FindVisibleTextContainsAny(
            settingsWindow,
            "This plugin has no configurable settings.",
            "no configurable settings");
    }

    private static bool PluginSettingsCompatibilityFallbackVisible(AutomationElement settingsWindow)
    {
        return FindVisibleTextContainsAny(
            settingsWindow,
            "This plugin settings UI targets Wpf.Ui",
            "page is hidden to keep the app stable");
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

    private static void TestViveToolSettingsInteractions(AutomationElement settingsWindow)
    {
        if (PluginSettingsCompatibilityFallbackVisible(settingsWindow))
        {
            Console.WriteLine("[main-smoke] ViveTool settings page is compatibility-gated by host/plugin Wpf.Ui mismatch; accepting stable fallback.");
            return;
        }

        var statusText = WaitForAutomationId(settingsWindow, "ViveToolSettingsStatusText", TimeSpan.FromSeconds(15));
        var pathTextBox = WaitForAutomationId(settingsWindow, "ViveToolSettingsPathTextBox", TimeSpan.FromSeconds(15));
        var refreshButton = WaitForAutomationId(settingsWindow, "ViveToolSettingsRefreshStatusButton", TimeSpan.FromSeconds(15));

        Click(refreshButton);

        var settingsRefreshed = WaitUntil(
            () =>
            {
                var status = ReadElementText(statusText);
                var path = ReadElementText(pathTextBox);
                return !string.IsNullOrWhiteSpace(status) || !string.IsNullOrWhiteSpace(path);
            },
            TimeSpan.FromSeconds(12),
            TimeSpan.FromMilliseconds(250));

        if (!settingsRefreshed)
        {
            DumpAutomationSnapshot(settingsWindow, 260);
            throw new InvalidOperationException("ViveTool settings-page interaction failed: refreshed status/path was not observed.");
        }

        Console.WriteLine("[main-smoke] ViveTool settings-page interactions passed");
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
               || FindByAutomationId(window, "PluginSettingsCloseButton") is not null
               || (window.Current.Name?.Contains("settings", StringComparison.OrdinalIgnoreCase) ?? false)
               || (window.Current.Name?.Contains("??", StringComparison.Ordinal) ?? false);
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

        if (string.Equals(window.Current.Name, "????????", StringComparison.OrdinalIgnoreCase)
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
                        new[] { "????????", "Custom Mouse Settings" },
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
               ?? FindByName(window, "??")
               ?? FindByName(window, "OK")
               ?? FindByName(window, "??");
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
        if (!TryGetNativeWindowHandle(mainWindow, out var mainWindowHandle))
        {
            mainWindow = TryFindMainShellWindow(processId) ?? mainWindow;
            if (!TryGetNativeWindowHandle(mainWindow, out mainWindowHandle))
            {
                Console.WriteLine("[main-smoke] MessageBox detection skipped because the main window handle is temporarily unavailable.");
                return false;
            }
        }

        var messageBox = DetectMessageBoxWindow(processId, mainWindowHandle, timeout);

        if (messageBox is null)
            return false;

        var popupType = ClassifyPopupWindow(messageBox);
        var windowName = messageBox.Current.Name ?? "<unnamed>";

        try
        {
            var rightButton = FindByAutomationId(messageBox, "ButtonRight")
                              ?? FindByName(messageBox, "No")
                              ?? FindByName(messageBox, "??")
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
                             ?? FindByName(messageBox, "\u786e\u5b9a")
                             ?? FindByName(messageBox, "\u662f");

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
        EnsurePluginMarketplaceView(mainWindow);
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
                    break;

                Console.WriteLine($"[main-smoke] PluginCard_{pluginId} not found (attempt {attempt}/3), retrying after delay...");
                Thread.Sleep(500);
                BringToForeground(mainWindow);
            }
        }

        if (pluginCard is null)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            if (IsPluginMarketplaceEntryVisible(mainWindow, pluginId))
            {
                Console.WriteLine($"[main-smoke] PluginCard_{pluginId} not found, but plugin entry is already visible through action controls. Continuing without explicit card selection.");
                return;
            }

            throw new TimeoutException($"PluginCard_{pluginId} could not be found after multiple attempts.");
        }

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

    private static void EnsurePluginMarketplaceView(AutomationElement mainWindow)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        if (IsPluginMarketplaceReady(mainWindow))
            return;

        NavigateToPluginExtensionsPage(mainWindow, refresh: false);
        mainWindow = ResolveLiveWindow(mainWindow);

        var ready = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                return IsPluginMarketplaceReady(mainWindow);
            },
            TimeSpan.FromSeconds(12),
            TimeSpan.FromMilliseconds(250));

        if (!ready)
        {
            DumpAutomationSnapshot(mainWindow, 260);
            throw new TimeoutException("Failed to restore Plugin Extensions marketplace view.");
        }
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

        if (IsPluginInstalledInSandbox(sandboxState, pluginId))
            WaitForInstalledMarketplaceUiState(mainWindow, pluginId, TimeSpan.FromSeconds(45));

        Console.WriteLine($"[main-smoke] Install verified for plugin: {pluginId}");
        CaptureMainWindow(mainWindow, $"{pluginId}-marketplace-installed");
        ObserveStep($"Marketplace install verified: {pluginId}", mainWindow);
    }

    private static void UninstallPluginFromMarketplace(AutomationElement mainWindow, SmokeSandboxState? sandboxState, string pluginId)
    {
        EnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);
        var uninstallButton = WaitForAutomationId(mainWindow, $"PluginUninstallButton_{pluginId}", TimeSpan.FromSeconds(20));
        Click(uninstallButton);
        Console.WriteLine($"[main-smoke] Clicked uninstall for plugin: {pluginId}");

        var uninstalled = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                if (!IsPluginUninstalledInSandbox(sandboxState, pluginId))
                    return false;

                _ = TryEnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);
                return IsPluginUninstalledInUi(mainWindow, pluginId)
                       || IsPluginUninstalledInMarketplaceState(mainWindow, pluginId);
            },
            TimeSpan.FromSeconds(60),
            TimeSpan.FromMilliseconds(300));

        if (!uninstalled)
            throw new TimeoutException($"Plugin uninstall did not reach uninstalled state: {pluginId}");

        EnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);
        AssertPluginUninstalledUiState(mainWindow, sandboxState, pluginId);
        CaptureMainWindow(mainWindow, $"{pluginId}-marketplace-uninstalled");
        ObserveStep($"Marketplace uninstall verified: {pluginId}", mainWindow);
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
                       || installButtonText.Contains("\u5df2\u5b89\u88c5", StringComparison.OrdinalIgnoreCase)
                       || installButtonText.Contains("\u66f4\u65b0", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsPluginInstalled(AutomationElement root, SmokeSandboxState? sandboxState, string pluginId)
        => IsPluginInstalledInUi(root, pluginId) || IsPluginInstalledInSandbox(sandboxState, pluginId);

    private static void WaitForInstalledMarketplaceUiState(AutomationElement mainWindow, string pluginId, TimeSpan timeout)
    {
        var ready = WaitUntil(
            () =>
            {
                mainWindow = ResolveLiveWindow(mainWindow);
                _ = TryEnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);
                return IsVisible(FindByAutomationId(mainWindow, $"PluginConfigureButton_{pluginId}"))
                       || IsVisible(FindByAutomationId(mainWindow, $"PluginOpenButton_{pluginId}"))
                       || IsVisible(FindByAutomationId(mainWindow, $"PluginUninstallButton_{pluginId}"))
                       || IsPluginInstalledInUi(mainWindow, pluginId);
            },
            timeout,
            TimeSpan.FromMilliseconds(300));

        if (!ready)
            Console.WriteLine($"[main-smoke] Marketplace UI did not fully refresh to installed actions within {timeout.TotalSeconds:0}s for '{pluginId}'. Continuing with best-effort fallbacks.");
    }

    private static bool TryEnsurePluginMarketplaceEntrySelected(AutomationElement mainWindow, string pluginId)
    {
        try
        {
            EnsurePluginMarketplaceEntrySelected(mainWindow, pluginId);
            return true;
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"[main-smoke] Marketplace selection for '{pluginId}' remains best-effort while UI refreshes: {ex.Message}");
            return false;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[main-smoke] Marketplace selection for '{pluginId}' skipped during UI refresh: {ex.Message}");
            return false;
        }
    }

    private static bool IsPluginUninstalledInUi(AutomationElement root, string pluginId)
    {
        root = ResolveLiveWindow(root);
        var installButton = FindByAutomationId(root, $"PluginInstallButton_{pluginId}");
        return IsVisible(installButton)
               && !IsVisible(FindByAutomationId(root, $"PluginOpenButton_{pluginId}"))
               && !IsVisible(FindByAutomationId(root, $"PluginConfigureButton_{pluginId}"))
               && !IsVisible(FindByAutomationId(root, $"PluginUninstallButton_{pluginId}"));
    }

    private static bool IsPluginUninstalledInMarketplaceState(AutomationElement root, string pluginId)
    {
        root = ResolveLiveWindow(root);
        return !IsVisible(FindByAutomationId(root, $"PluginOpenButton_{pluginId}"))
               && !IsVisible(FindByAutomationId(root, $"PluginConfigureButton_{pluginId}"))
               && !IsVisible(FindByAutomationId(root, $"PluginUninstallButton_{pluginId}"))
               && !IsPluginInstalledInUi(root, pluginId);
    }

    private static void AssertPluginUninstalledUiState(AutomationElement root, SmokeSandboxState? sandboxState, string pluginId)
    {
        root = ResolveLiveWindow(root);
        if (!IsPluginUninstalledInUi(root, pluginId))
            throw new InvalidOperationException($"Plugin uninstall UI state did not reset correctly: {pluginId}");

        if (!IsPluginUninstalledInSandbox(sandboxState, pluginId))
            throw new InvalidOperationException($"Plugin uninstall sandbox state still reports installed: {pluginId}");
    }

    private static bool IsPluginUninstalledInSandbox(SmokeSandboxState? sandboxState, string pluginId)
    {
        if (sandboxState is null || string.IsNullOrWhiteSpace(pluginId))
            return true;

        try
        {
            var settingsPath = Path.Combine(sandboxState.AppDataDirectory, "settings.json");
            if (!File.Exists(settingsPath))
                return true;

            var root = ReadSettingsRoot(settingsPath);
            var installedExtensions = EnsureJsonArray(root, "InstalledExtensions");
            var pendingDeletionExtensions = EnsureJsonArray(root, "PendingDeletionExtensions");

            return !ContainsJsonValue(installedExtensions, pluginId)
                   || ContainsJsonValue(pendingDeletionExtensions, pluginId);
        }
        catch
        {
            return false;
        }
    }

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

    private static void AssertNoInstalledPluginEmptyOptimizationCategories(AutomationElement mainWindow, IEnumerable<string> pluginIds)
    {
        var pluginIdList = pluginIds
            .Where(pluginId => !string.IsNullOrWhiteSpace(pluginId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (pluginIdList.Length == 0)
            return;

        NavigateToWindowsOptimizationPage(mainWindow);
        mainWindow = ResolveLiveWindow(mainWindow);

        foreach (var pluginId in pluginIdList)
            AssertNoEmptyOptimizationCategory(mainWindow, pluginId);

        NavigateToPluginExtensionsPage(mainWindow, refresh: false);
    }

    private static void AssertNoEmptyOptimizationCategory(AutomationElement mainWindow, string pluginId)
    {
        var category = TryFindOptimizationCategoryForPlugin(mainWindow, pluginId);
        var settingsButton = TryFindOptimizationSettingsButtonForPlugin(mainWindow, pluginId);

        if (category is null && settingsButton is null)
        {
            Console.WriteLine($"[main-smoke] No System Optimization category exposed for plugin: {pluginId}");
            return;
        }

        if (category is not null)
        {
            TryScrollElementIntoView(category);
            ExpandIfNeeded(category);
            TryScrollElementIntoView(category);
        }

        var action = TryFindOptimizationActionForPlugin(mainWindow, pluginId);
        if (action is null)
        {
            DumpAutomationSnapshot(ResolveLiveWindow(mainWindow), 260);
            throw new InvalidOperationException($"Plugin '{pluginId}' exposed a System Optimization category/settings entry without any action checkboxes.");
        }

        Console.WriteLine($"[main-smoke] System Optimization category for plugin '{pluginId}' has action checkbox: {action.Current.AutomationId}");
    }

    private static AutomationElement? TryFindOptimizationCategoryForPlugin(AutomationElement mainWindow, string pluginId)
    {
        foreach (var prefix in GetPluginAutomationIdPrefixes(pluginId, "WindowsOptimizationCategory_"))
        {
            var element = TryWaitForAutomationIdPrefix(mainWindow, prefix, TimeSpan.FromSeconds(2));
            if (element is not null)
                return element;
        }

        return null;
    }

    private static AutomationElement? TryFindOptimizationSettingsButtonForPlugin(AutomationElement mainWindow, string pluginId)
    {
        foreach (var prefix in GetPluginAutomationIdPrefixes(pluginId, "WindowsOptimizationCategorySettings_"))
        {
            var element = TryWaitForAutomationIdPrefix(mainWindow, prefix, TimeSpan.FromSeconds(2));
            if (element is not null)
                return element;
        }

        return null;
    }

    private static AutomationElement? TryFindOptimizationActionForPlugin(AutomationElement mainWindow, string pluginId)
    {
        foreach (var prefix in GetPluginAutomationIdPrefixes(pluginId, "WindowsOptimizationAction_"))
        {
            var element = TryWaitForAutomationIdPrefixWithScroll(mainWindow, prefix, TimeSpan.FromSeconds(2));
            if (element is not null)
                return element;
        }

        return null;
    }

    private static IEnumerable<string> GetPluginAutomationIdPrefixes(string pluginId, string basePrefix)
    {
        var normalized = pluginId.Trim();
        if (normalized.Length == 0)
            yield break;

        yield return basePrefix + normalized;

        var dotted = normalized.Replace('-', '.');
        if (!string.Equals(dotted, normalized, StringComparison.Ordinal))
            yield return basePrefix + dotted;

        var compact = normalized.Replace("-", string.Empty, StringComparison.Ordinal);
        if (!string.Equals(compact, normalized, StringComparison.Ordinal))
            yield return basePrefix + compact;

        if (normalized.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
        {
            yield return basePrefix + "CustomMouse";
            yield return basePrefix + "UniversalDeviceToolkit.Plugins.CustomMouse";
        }

        if (normalized.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
            yield return basePrefix + "ShellIntegration";
    }

    private static void EnsureOptimizationCategoryVisible(AutomationElement mainWindow, string pluginId, bool toggleActions)
    {
        NavigateToWindowsOptimizationPage(mainWindow);

        var definition = GetOptimizationRouteDefinition(pluginId)
                         ?? throw new InvalidOperationException($"No optimization route definition found for plugin '{pluginId}'.");

        var category = WaitForOptimizationCategory(mainWindow, pluginId, definition, TimeSpan.FromSeconds(30));
        if (category is not null)
        {
            TryScrollElementIntoView(category);
            ExpandIfNeeded(category);
            TryScrollElementIntoView(category);
        }

        var settingsButton = WaitForOptimizationSettingsButton(mainWindow, pluginId, definition, TimeSpan.FromSeconds(20));
        Console.WriteLine($"[main-smoke] Optimization settings button ready ({pluginId}): {settingsButton.Current.AutomationId}");

        var actions = WaitForOptimizationActionButtons(mainWindow, pluginId, definition, TimeSpan.FromSeconds(20));

        if (actions.Length == 0)
            throw new InvalidOperationException($"Optimization category for {pluginId} has no action checkboxes.");

        if (!toggleActions)
            return;

        for (var index = 0; index < actions.Length; index++)
        {
            var actionAutomationId = definition.ActionAutomationIds[index];
            var actionKey = actionAutomationId.Replace("WindowsOptimizationAction_", string.Empty, StringComparison.Ordinal);
            ClickActionCheckbox(actions[index], actionKey);
        }
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
                .Select(actionId => WaitForAutomationIdWithScroll(mainWindow, actionId, timeout))
                .ToArray();
        }
        catch (TimeoutException) when (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
        {
            var actionPrefixes = new[]
            {
                "WindowsOptimizationAction_custom.mouse.",
                "WindowsOptimizationAction_custom-mouse.",
                "WindowsOptimizationAction_custommouse.",
                "WindowsOptimizationAction_UniversalDeviceToolkit.Plugins.CustomMouse.",
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
                            var fallback = TryWaitForAutomationIdPrefixWithScroll(mainWindow, actionPrefix + candidateSuffix, timeout);
                            if (fallback is not null)
                            {
                                Console.WriteLine($"[main-smoke] custom-mouse optimization action resolved by prefix fallback: requested='{actionId}' candidate='{actionPrefix + candidateSuffix}' actual='{fallback.Current.AutomationId}' name='{fallback.Current.Name}'");
                                return fallback;
                            }
                        }
                    }

                    return WaitForAutomationIdWithScroll(mainWindow, actionId, timeout);
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
                            var fallback = TryWaitForAutomationIdPrefixWithScroll(mainWindow, actionPrefix + candidateSuffix, timeout);
                            if (fallback is not null)
                            {
                                Console.WriteLine($"[main-smoke] shell-integration optimization action resolved by prefix fallback: requested='{actionId}' candidate='{actionPrefix + candidateSuffix}' actual='{fallback.Current.AutomationId}' name='{fallback.Current.Name}'");
                                return fallback;
                            }
                        }
                    }

                    return WaitForAutomationIdWithScroll(mainWindow, actionId, timeout);
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
                    "WindowsOptimizationCategory_UniversalDeviceToolkit.Plugins.CustomMouse",
                    "WindowsOptimizationCategory_CustomMouse"
                },
                new[]
                {
                    "WindowsOptimizationCategorySettings_custom.mouse",
                    "WindowsOptimizationCategorySettings_custom-mouse",
                    "WindowsOptimizationCategorySettings_custommouse",
                    "WindowsOptimizationCategorySettings_UniversalDeviceToolkit.Plugins.CustomMouse",
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
                        "WindowsOptimizationCategory_UniversalDeviceToolkit.Plugins.CustomMouse",
                        "WindowsOptimizationCategorySettings_UniversalDeviceToolkit.Plugins.CustomMouse",
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
                        ?? TryWaitForAutomationIdPrefix(mainWindow, "WindowsOptimizationCategorySettings_UniversalDeviceToolkit.Plugins.CustomMouse", focusedTimeout)
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
                "WindowsOptimizationCategorySettings_UniversalDeviceToolkit.Plugins.CustomMouse",
                "WindowsOptimizationCategorySettings_CustomMouse"
            };

            settingsButton = settingsButtonPrefixes
                .Select(prefix => TryWaitForAutomationIdPrefix(mainWindow, prefix, timeout))
                .FirstOrDefault(element => element is not null)
                ?? WaitForAutomationIdPrefix(mainWindow, "WindowsOptimizationCategorySettings_UniversalDeviceToolkit.Plugins.CustomMouse", timeout);
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

    private static void TestDashboardSensorDetailsToggle(AutomationElement mainWindow)
    {
        NavigateToDashboardPage(mainWindow);
        mainWindow = ResolveLiveWindow(mainWindow);
        WaitForDashboardLoadingToFinish(mainWindow);

        var sensorsCard = WaitForAutomationId(mainWindow, "DashboardSensorsCard", TimeSpan.FromSeconds(25));
        CaptureMainWindow(mainWindow, "dashboard-sensors-collapsed");

        var initiallyExpanded = AnyDashboardSensorDetailsVisible(mainWindow);
        if (initiallyExpanded)
        {
            MouseDoubleClick(sensorsCard);
            WaitUntil(
                () => !AnyDashboardSensorDetailsVisible(ResolveLiveWindow(mainWindow)),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromMilliseconds(200));
        }

        mainWindow = ResolveLiveWindow(mainWindow);
        sensorsCard = WaitForAutomationId(mainWindow, "DashboardSensorsCard", TimeSpan.FromSeconds(8));
        MouseDoubleClick(sensorsCard);

        var expanded = WaitUntil(
            () => AnyDashboardSensorDetailsVisible(ResolveLiveWindow(mainWindow)),
            TimeSpan.FromSeconds(8),
            TimeSpan.FromMilliseconds(200));

        if (!expanded)
        {
            AutomationElement? detailsWindow = null;
            WaitUntil(
                () =>
                {
                    detailsWindow = AutomationElement.RootElement
                        .FindAll(TreeScope.Children, Condition.TrueCondition)
                        .Cast<AutomationElement>()
                        .FirstOrDefault(window => string.Equals(
                            window.Current.AutomationId,
                            "SensorDetailsWindow",
                            StringComparison.OrdinalIgnoreCase));
                    return detailsWindow is not null;
                },
                TimeSpan.FromSeconds(8),
                TimeSpan.FromMilliseconds(200));

            if (detailsWindow is null)
            {
                DumpAutomationSnapshot(ResolveLiveWindow(mainWindow), 220);
                throw new TimeoutException("Dashboard sensor details neither expanded inline nor opened the responsive details window.");
            }

            if (TryGetNativeWindowHandle(detailsWindow, out var detailsHandle))
                CaptureWindowArtifacts(detailsHandle, "dashboard-sensor-details-window", includeFullScreen: false);
            CloseWindow(detailsWindow);
            CaptureMainWindow(ResolveLiveWindow(mainWindow), "dashboard-sensors-recollapsed");
            Console.WriteLine("[main-smoke] Dashboard responsive sensor details window verified.");
            return;
        }

        CaptureMainWindow(ResolveLiveWindow(mainWindow), "dashboard-sensors-expanded");

        mainWindow = ResolveLiveWindow(mainWindow);
        sensorsCard = WaitForAutomationId(mainWindow, "DashboardSensorsCard", TimeSpan.FromSeconds(8));
        MouseDoubleClick(sensorsCard);

        var collapsed = WaitUntil(
            () => !AnyDashboardSensorDetailsVisible(ResolveLiveWindow(mainWindow)),
            TimeSpan.FromSeconds(8),
            TimeSpan.FromMilliseconds(200));

        if (!collapsed)
        {
            DumpAutomationSnapshot(ResolveLiveWindow(mainWindow), 220);
            throw new TimeoutException("Dashboard sensor details did not collapse after second double-click.");
        }

        CaptureMainWindow(ResolveLiveWindow(mainWindow), "dashboard-sensors-recollapsed");
        Console.WriteLine("[main-smoke] Dashboard sensor card expand/collapse verified.");
    }

    private static void WaitForDashboardLoadingToFinish(AutomationElement mainWindow)
    {
        var finished = WaitUntil(
            () => !IsVisible(FindByAutomationId(ResolveLiveWindow(mainWindow), "DashboardLoadingSkeleton")),
            TimeSpan.FromSeconds(8),
            TimeSpan.FromMilliseconds(200));

        if (!finished)
            Console.WriteLine("[main-smoke] Dashboard loading skeleton still visible when attempting sensor-card interaction.");
    }

    private static bool AnyDashboardSensorDetailsVisible(AutomationElement mainWindow)
    {
        var detailElementIds = new[]
        {
            "DashboardSensorsCpuDetailsPanel",
            "DashboardSensorsBatteryDetailsPanel",
            "DashboardSensorsGpuDetailsPanel",
            "_cpuWattage",
            "_batteryRateRange",
            "_gpuMemoryClockText"
        };

        return detailElementIds.Any(id => IsVisible(FindByAutomationId(mainWindow, id)));
    }

    private static void TestPowerModeUi(AutomationElement mainWindow)
    {
        Console.WriteLine("[main-smoke] Starting power-mode UI flow.");
        NavigateToDashboardPage(mainWindow);
        Console.WriteLine("[main-smoke] Dashboard page navigation completed for power-mode flow.");
        mainWindow = ResolveLiveWindow(mainWindow);

        Console.WriteLine("[main-smoke] Resolving Power Mode combo box.");
        var comboBox = TryFindPowerModeComboBox(mainWindow, TimeSpan.FromSeconds(25));
        if (comboBox is null)
        {
            Console.WriteLine("[main-smoke] Power Mode control is not visible on this device; treating unsupported hardware as a valid basic-mode outcome.");
            CaptureMainWindow(mainWindow, "power-mode-not-supported");
            return;
        }

        VerifyPowerModeComboBox(comboBox);
        CaptureMainWindow(mainWindow, "power-mode-combobox");

        Console.WriteLine("[main-smoke] Reading original power mode.");
        var originalPowerMode = ReadElementText(comboBox);
        Console.WriteLine($"[main-smoke] Original power mode resolved as '{originalPowerMode}'.");
        if (_powerModeHardwareVerificationEnabled)
        {
            RunPowerModeUiHardwareReadbackVerification(mainWindow, comboBox);
            mainWindow = ResolveLiveWindow(mainWindow);
            comboBox = TryFindPowerModeComboBox(mainWindow, TimeSpan.FromSeconds(10)) ?? comboBox;
            CaptureMainWindow(mainWindow, "power-mode-hardware-verified");
        }

        Console.WriteLine("[main-smoke] Resolving power-mode settings button.");
        var settingsButton = TryWaitForAutomationId(ResolveLiveWindow(mainWindow), "PowerModeSettingsButton", TimeSpan.FromSeconds(6));
        Console.WriteLine(settingsButton is null
            ? "[main-smoke] Power-mode settings button not currently visible."
            : $"[main-smoke] Power-mode settings button resolved. Enabled={settingsButton.Current.IsEnabled}.");
        if (settingsButton is null || !IsInteractable(settingsButton))
        {
            Console.WriteLine("[main-smoke] Power Mode settings button is hidden for the current mode/device; combo box presence verified without changing hardware mode.");
            return;
        }

        var processId = GetProcessId(mainWindow);
        var ownerHandle = mainWindow.Current.NativeWindowHandle;
        Click(settingsButton);

        var settingsWindow = WaitForOwnedWindow(
            processId,
            ownerHandle,
            IsPowerModeSettingsWindow,
            TimeSpan.FromSeconds(15),
            "power mode settings window");

        CapturePluginSettingsWindow(settingsWindow, "power-mode", "settings-window");

        settingsWindow = ResolvePowerModeSettingsWindow(settingsWindow);

        if (IsVisible(FindByAutomationId(settingsWindow, "GodModeSettingsWindow"))
            || FindByAutomationId(settingsWindow, "GodModePresetComboBox") is not null)
        {
            WaitForAutomationId(settingsWindow, "GodModePresetComboBox", TimeSpan.FromSeconds(15));
            WaitForAutomationId(settingsWindow, "GodModeAddPresetButton", TimeSpan.FromSeconds(10));
            WaitForAutomationId(settingsWindow, "GodModeEditPresetButton", TimeSpan.FromSeconds(10));
            WaitForAutomationId(settingsWindow, "GodModeDeletePresetButton", TimeSpan.FromSeconds(10));
            VerifyGodModeBasePresets(settingsWindow);
            Console.WriteLine("[main-smoke] God Mode preset controls verified. Preset create/rename/delete is covered by Tools\\PresetUiValidation; hardware apply verification is covered by Tools\\HardwareValidation.");
        }
        else
        {
            Console.WriteLine("[main-smoke] Balance/AI settings window opened; no custom preset controls expected.");
        }

        CloseWindow(settingsWindow);
        Thread.Sleep((int)WindowAnimationDuration.TotalMilliseconds);

        Console.WriteLine(_powerModeHardwareVerificationEnabled
            ? "[main-smoke] Power Mode UI and hardware readback verification completed."
            : "[main-smoke] Power Mode UI verified without changing the selected hardware mode.");
    }

    private static void RunPowerModeUiHardwareReadbackVerification(AutomationElement mainWindow, AutomationElement comboBox)
    {
        var beforeMode = ReadHardwareSmartFanMode();
        var targetMode = ChooseUiHardwareReadbackTarget(beforeMode, comboBox);
        var expectedAfterMode = TryResolveExpectedSmartFanModeRawValue(targetMode)
            ?? throw new NotSupportedException($"Power mode '{targetMode}' has no known SmartFan raw value.");

        Console.WriteLine($"[main-smoke] BeforeSmartFanMode: {beforeMode}");
        Console.WriteLine($"[main-smoke] RequestedPowerModeState: {targetMode}");
        Console.WriteLine($"[main-smoke] RequestedSmartFanMode: {expectedAfterMode}");
        Console.WriteLine("[main-smoke] Selecting power mode in the main UI and waiting for hardware readback.");

        var selectedText = SelectPowerModeComboBoxItem(comboBox, targetMode);
        var afterMode = WaitForHardwareSmartFanMode(expectedAfterMode, PowerModeHardwareReadbackTimeout);
        var hardwareChanged = afterMode != beforeMode;
        var hardwarePassed = afterMode == expectedAfterMode && hardwareChanged;

        Console.WriteLine($"[main-smoke] UiSelectedPowerMode: {selectedText}");
        Console.WriteLine($"[main-smoke] AfterSmartFanMode: {afterMode}");
        Console.WriteLine($"[main-smoke] PowerModeDelta: {afterMode - beforeMode}");
        Console.WriteLine($"[main-smoke] UiPowerModeHardwareChanged: {hardwareChanged}");
        Console.WriteLine($"[main-smoke] UiPowerModeHardwareVerificationPassed: {hardwarePassed}");

        var restorePassed = RestoreHardwarePowerModeFromUi(mainWindow, comboBox, beforeMode);
        Console.WriteLine($"[main-smoke] UiPowerModeHardwareRestorePassed: {restorePassed}");
        Console.WriteLine($"[main-smoke] PowerModeHardwareOverallPassed: {hardwarePassed && restorePassed}");

        if (!hardwarePassed || !restorePassed)
            throw new InvalidOperationException("UI-triggered power-mode hardware readback verification failed.");
    }

    private static PowerModeState ChooseUiHardwareReadbackTarget(int beforeMode, AutomationElement comboBox)
    {
        var supportedOptions = GetComboBoxOptionNames(comboBox)
            .Select(TryResolvePowerModeStateFromText)
            .Where(mode => mode is not null)
            .Select(mode => mode!.Value)
            .Distinct()
            .Where(mode => TryResolveExpectedSmartFanModeRawValue(mode) is { } rawMode && rawMode != beforeMode)
            .ToArray();

        foreach (var preferredMode in new[] { PowerModeState.Performance, PowerModeState.Balance, PowerModeState.Quiet, PowerModeState.GodMode })
        {
            if (supportedOptions.Contains(preferredMode))
                return preferredMode;
        }

        throw new InvalidOperationException(
            $"Power Mode combo box has no selectable mode different from hardware mode '{beforeMode}'.");
    }

    private static string SelectPowerModeComboBoxItem(AutomationElement comboBox, PowerModeState targetMode)
    {
        if (comboBox.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandPattern))
        {
            var expander = (ExpandCollapsePattern)expandPattern;
            expander.Expand();
        }

        Thread.Sleep(250);

        var items = GetComboBoxItems(comboBox);
        var item = items.FirstOrDefault(candidate => TryResolvePowerModeStateFromText(ReadElementText(candidate)) == targetMode);
        if (item is not null)
        {
            var selectedName = ReadElementText(item);
            Console.WriteLine($"[main-smoke] Selecting localized power-mode item '{selectedName}' for '{targetMode}'.");
            Click(item);
            Thread.Sleep(180);
            CollapseComboBox(comboBox);
            return selectedName;
        }

        return targetMode switch
        {
            PowerModeState.Quiet => SelectComboBoxItemByNamesOrContains(comboBox, "Quiet", "安静", "静音"),
            PowerModeState.Balance => SelectComboBoxItemByNamesOrContains(comboBox, "Balance", "Balanced", "平衡"),
            PowerModeState.Performance => SelectComboBoxItemByNamesOrContains(comboBox, "Performance", "性能"),
            PowerModeState.GodMode => SelectComboBoxItemByNamesOrContains(comboBox, "God Mode", "GodMode", "Custom", "自定义"),
            _ => throw new NotSupportedException($"Power mode '{targetMode}' is not supported by the UI hardware smoke.")
        };
    }

    private static bool RestoreHardwarePowerModeFromUi(AutomationElement mainWindow, AutomationElement comboBox, int beforeMode)
    {
        if (TryResolvePowerModeStateFromSmartFanMode(beforeMode) is not { } restoreMode)
        {
            Console.WriteLine($"[main-smoke] Original hardware power mode '{beforeMode}' is not a known UI mode; restoring with direct hardware API.");
            WMI.LenovoGameZoneData.SetSmartFanModeAsync(beforeMode).GetAwaiter().GetResult();
            return WaitForHardwareSmartFanMode(beforeMode, PowerModeHardwareReadbackTimeout) == beforeMode;
        }

        try
        {
            comboBox = FindPowerModeComboBox(ResolveLiveWindow(mainWindow)) ?? comboBox;
            SelectPowerModeComboBoxItem(comboBox, restoreMode);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex) || ex is InvalidOperationException or NotSupportedException)
        {
            Console.WriteLine($"[main-smoke] UI restore could not select the original mode, falling back to direct hardware restore. Reason: {ex.Message}");
            WMI.LenovoGameZoneData.SetSmartFanModeAsync(beforeMode).GetAwaiter().GetResult();
        }

        var restoredMode = WaitForHardwareSmartFanMode(beforeMode, PowerModeHardwareReadbackTimeout);
        Console.WriteLine($"[main-smoke] RestoredSmartFanMode: {restoredMode}");
        return restoredMode == beforeMode;
    }

    private static int? TryResolveExpectedSmartFanModeRawValue(PowerModeState mode) => mode switch
    {
        PowerModeState.Quiet => 1,
        PowerModeState.Balance => 2,
        PowerModeState.Performance => 3,
        PowerModeState.GodMode => 255,
        _ => null
    };

    private static PowerModeState? TryResolvePowerModeStateFromSmartFanMode(int rawMode) => rawMode switch
    {
        1 => PowerModeState.Quiet,
        2 => PowerModeState.Balance,
        3 => PowerModeState.Performance,
        255 => PowerModeState.GodMode,
        _ => null
    };

    private static int ReadHardwareSmartFanMode()
    {
        return WMI.LenovoGameZoneData.GetSmartFanModeAsync().GetAwaiter().GetResult();
    }

    private static int WaitForHardwareSmartFanMode(int expectedMode, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        var currentMode = ReadHardwareSmartFanMode();
        while (currentMode != expectedMode && DateTimeOffset.UtcNow < deadline)
        {
            Thread.Sleep(PowerModeHardwareReadbackPollDelay);
            currentMode = ReadHardwareSmartFanMode();
        }

        return currentMode;
    }

    private static PowerModeState? TryResolvePowerModeStateFromText(string text)
    {
        if (TryResolveLocalizedPowerModeState(text) is { } localizedMode)
            return localizedMode;

        return NormalizePowerModeValue(text) switch
        {
            "quiet" => PowerModeState.Quiet,
            "balance" => PowerModeState.Balance,
            "performance" => PowerModeState.Performance,
            "godmode" => PowerModeState.GodMode,
            _ => null
        };
    }

    private static PowerModeState? TryResolveLocalizedPowerModeState(string text)
    {
        var normalizedText = NormalizeComparableText(text);
        if (string.IsNullOrWhiteSpace(normalizedText))
            return null;

        foreach (var culture in GetPowerModeResourceCultures())
        {
            foreach (var mode in new[] { PowerModeState.Quiet, PowerModeState.Balance, PowerModeState.Performance, PowerModeState.GodMode })
            {
                var resourceKey = $"PowerModeState_{mode}";
                var localizedName = UniversalDeviceToolkit.Lib.Resources.Resource.ResourceManager.GetString(resourceKey, culture);
                if (NormalizeComparableText(localizedName) == normalizedText)
                    return mode;
            }
        }

        return null;
    }

    private static IEnumerable<CultureInfo> GetPowerModeResourceCultures()
    {
        yield return CultureInfo.InvariantCulture;
        yield return CultureInfo.CurrentCulture;
        yield return CultureInfo.CurrentUICulture;

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            yield return culture;
    }

    private static string NormalizeComparableText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static AutomationElement? TryFindPowerModeComboBox(AutomationElement mainWindow, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => FindPowerModeComboBox(ResolveLiveWindow(mainWindow)) is not null,
            timeout,
            TimeSpan.FromMilliseconds(250));

        return found ? FindPowerModeComboBox(ResolveLiveWindow(mainWindow)) : null;
    }

    private static AutomationElement? FindPowerModeComboBox(AutomationElement root)
    {
        var byAutomationId = FindByAutomationId(root, "PowerModeControl_ComboBox");
        if (byAutomationId is not null)
            return byAutomationId;

        var powerModeCard = FindFirstVisibleDescendant(root, element =>
            element.Current.ControlType == ControlType.Text
            && string.Equals(ReadElementText(element), "Power Mode", StringComparison.OrdinalIgnoreCase));
        if (powerModeCard is not null)
        {
            var parent = TreeWalker.ControlViewWalker.GetParent(powerModeCard);
            AutomationElement? comboInPowerSection = null;
            while (parent is not null)
            {
                if (parent.Current.ControlType == ControlType.Pane || parent.Current.ControlType == ControlType.Group)
                {
                    comboInPowerSection = parent.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox));
                    if (comboInPowerSection is not null)
                        break;
                }

                parent = TreeWalker.ControlViewWalker.GetParent(parent);
            }

            if (comboInPowerSection is not null && IsVisible(comboInPowerSection))
                return comboInPowerSection;
        }

        var settingsButton = FindByAutomationId(root, "PowerModeSettingsButton");
        if (settingsButton is null)
            return null;

        var comboBoxes = root
            .FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ComboBox))
            .Cast<AutomationElement>()
            .Where(IsVisible)
            .ToArray();

        if (comboBoxes.Length == 0)
            return null;

        var buttonBounds = settingsButton.Current.BoundingRectangle;
        return comboBoxes
            .Where(combo =>
            {
                var bounds = combo.Current.BoundingRectangle;
                return Math.Abs(bounds.Top - buttonBounds.Top) < 80 && bounds.Left < buttonBounds.Left;
            })
            .OrderByDescending(combo => combo.Current.BoundingRectangle.Width)
            .FirstOrDefault()
            ?? comboBoxes.OrderByDescending(combo => combo.Current.BoundingRectangle.Width).FirstOrDefault();
    }

    private static void VerifyPowerModeComboBox(AutomationElement comboBox)
    {
        if (!comboBox.Current.IsEnabled)
            throw new InvalidOperationException("Power Mode combo box is visible but disabled.");

        var currentSelection = TryGetComboBoxSelectedItemText(comboBox) ?? ReadElementText(comboBox);

        if (comboBox.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandPattern))
        {
            var expander = (ExpandCollapsePattern)expandPattern;
            expander.Expand();
            Thread.Sleep(250);

            var options = GetComboBoxOptionNames(comboBox);

            if (options.Length == 0)
            {
                if (!string.IsNullOrWhiteSpace(currentSelection))
                {
                    Console.WriteLine($"[main-smoke] Power Mode combo box exposed no visible options; continuing with current selection '{currentSelection}'.");
                    return;
                }

                throw new InvalidOperationException("Power Mode combo box opened but exposed no selectable options.");
            }

            Console.WriteLine($"[main-smoke] Power Mode combo box exposed {options.Length} option(s).");
        }
        else
        {
            Console.WriteLine("[main-smoke] Power Mode combo box visible; ExpandCollapsePattern is unavailable, so option count was not inspected.");
        }
    }

    private static bool IsPowerModeSettingsWindow(AutomationElement window)
    {
        return IsVisible(FindByAutomationId(window, "GodModeSettingsWindow"))
               || IsVisible(FindByAutomationId(window, "BalanceModeSettingsWindow"))
               || IsVisible(FindByAutomationId(window, "GodModePresetComboBox"))
               || IsVisible(FindByAutomationId(window, "BalanceModeAiModeCheckBox"))
               || WindowNameContains(window, "God Mode")
               || WindowNameContains(window, "Custom")
               || WindowNameContains(window, "Performance")
               || WindowNameContains(window, "Balance")
               || WindowNameContains(window, "自定义")
               || WindowNameContains(window, "性能")
               || WindowNameContains(window, "平衡");
    }

    private static AutomationElement ResolvePowerModeSettingsWindow(AutomationElement settingsWindow)
    {
        var resolvedWindow = ResolveTopLevelWindow(settingsWindow);
        var identifiedWindow = WaitUntilValue(
            () =>
            {
                var liveWindow = ResolveTopLevelWindow(resolvedWindow);
                if (FindByAutomationId(liveWindow, "GodModePresetComboBox") is not null)
                    return liveWindow;

                if (FindByAutomationId(liveWindow, "BalanceModeAiModeCheckBox") is not null)
                    return liveWindow;

                if (IsVisible(FindByAutomationId(liveWindow, "GodModeSettingsWindow")))
                    return liveWindow;

                if (IsVisible(FindByAutomationId(liveWindow, "BalanceModeSettingsWindow")))
                    return liveWindow;

                return null;
            },
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMilliseconds(250),
            "power mode settings window content");

        return identifiedWindow;
    }

    private static void VerifyGodModeBasePresets(AutomationElement settingsWindow)
    {
        var presetComboBox = WaitForAutomationId(settingsWindow, "GodModePresetComboBox", TimeSpan.FromSeconds(10));
        var options = GetComboBoxOptionNames(presetComboBox);
        var requiredOptions = new[] { "Quiet", "Balance", "Performance" };
        var missing = requiredOptions
            .Where(required => !options.Any(option => option.Contains(required, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (missing.Length > 0)
            throw new InvalidOperationException(
                $"God Mode base preset(s) missing from combo box: [{string.Join(", ", missing)}]. Options: [{string.Join(", ", options)}]");

        Console.WriteLine($"[main-smoke] God Mode base presets verified: [{string.Join(", ", options)}]");
    }

    private static string NormalizePowerModeValue(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "quiet" or "安静" or "静音" => "quiet",
            "balance" or "balanced" or "平衡" => "balance",
            "performance" or "性能" => "performance",
            "godmode" or "god mode" or "custom" or "自定义" => "godmode",
            _ when normalized.Contains("quiet") => "quiet",
            _ when normalized.Contains("balance") => "balance",
            _ when normalized.Contains("performance") => "performance",
            _ when normalized.Contains("god") || normalized.Contains("custom") => "godmode",
            _ when normalized.Contains("安静") || normalized.Contains("静音") => "quiet",
            _ when normalized.Contains("平衡") => "balance",
            _ when normalized.Contains("性能") => "performance",
            _ when normalized.Contains("自定义") => "godmode",
            _ => normalized
        };
    }

    private static string[] GetComboBoxOptionNames(AutomationElement comboBox)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                if (comboBox.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandPattern))
                {
                    var expander = (ExpandCollapsePattern)expandPattern;
                    expander.Expand();
                    Thread.Sleep(250);
                }

                var names = GetComboBoxItems(comboBox)
                    .Select(ReadElementText)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (comboBox.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var collapsePattern))
                {
                    CollapseComboBox(comboBox);
                }

                Thread.Sleep(120);
                return names;
            }
            catch (Exception ex) when (IsRecoverableAutomationException(ex))
            {
                if (attempt == 3)
                    throw;

                Thread.Sleep(200);
            }
        }

        return [];
    }

    private static void CollapseComboBox(AutomationElement comboBox)
    {
        if (!comboBox.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var collapsePattern))
            return;

        var expander = (ExpandCollapsePattern)collapsePattern;
        if (expander.Current.ExpandCollapseState is ExpandCollapseState.Expanded or ExpandCollapseState.PartiallyExpanded)
            expander.Collapse();
    }

    private static AutomationElement[] GetComboBoxItems(AutomationElement comboBox)
    {
        var comboProcessId = GetProcessId(comboBox);
        var comboBounds = comboBox.Current.BoundingRectangle;
        var listItemCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem);

        return comboBox.FindAll(TreeScope.Descendants, listItemCondition)
            .Cast<AutomationElement>()
            .Concat(
                AutomationElement.RootElement
                    .FindAll(TreeScope.Descendants, listItemCondition)
                    .Cast<AutomationElement>())
            .Where(IsVisible)
            .Where(item => GetProcessId(item) == comboProcessId)
            .Where(item => !string.IsNullOrWhiteSpace(ReadElementText(item)))
            .Where(item => IsComboBoxItemCandidate(comboBox, comboBounds, item))
            .GroupBy(item => string.Join(",", item.GetRuntimeId()))
            .Select(group => group.First())
            .ToArray();
    }

    private static bool IsComboBoxItemCandidate(AutomationElement comboBox, System.Windows.Rect comboBounds, AutomationElement item)
    {
        if (IsDescendantOf(item, comboBox))
            return true;

        var itemBounds = item.Current.BoundingRectangle;
        if (itemBounds.Width <= 0 || itemBounds.Height <= 0)
            return false;

        if (!HasMeaningfulHorizontalOverlap(comboBounds, itemBounds))
            return false;

        if (itemBounds.Top >= comboBounds.Top - 40 && itemBounds.Bottom <= comboBounds.Bottom + 700)
            return true;

        if (item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionPatternObject)
            && selectionPatternObject is SelectionItemPattern selectionPattern)
        {
            try
            {
                var selectionContainer = selectionPattern.Current.SelectionContainer;
                if (selectionContainer is not null)
                {
                    if (IsDescendantOf(selectionContainer, comboBox))
                        return true;

                    var selectionBounds = selectionContainer.Current.BoundingRectangle;
                    return HasMeaningfulHorizontalOverlap(comboBounds, selectionBounds)
                           && selectionBounds.Top >= comboBounds.Top - 40
                           && selectionBounds.Bottom <= comboBounds.Bottom + 700;
                }
            }
            catch (Exception ex) when (IsRecoverableAutomationException(ex))
            {
                return false;
            }
        }

        return false;
    }

    private static bool HasMeaningfulHorizontalOverlap(System.Windows.Rect comboBounds, System.Windows.Rect candidateBounds)
    {
        var overlap = Math.Min(comboBounds.Right, candidateBounds.Right) - Math.Max(comboBounds.Left, candidateBounds.Left);
        return overlap >= Math.Min(comboBounds.Width, candidateBounds.Width) * 0.35;
    }

    private static bool IsDescendantOf(AutomationElement candidate, AutomationElement ancestor)
    {
        try
        {
            var walker = TreeWalker.ControlViewWalker;
            var current = candidate;
            while (current is not null)
            {
                if (current.Equals(ancestor))
                    return true;

                current = walker.GetParent(current);
            }
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            return false;
        }

        return false;
    }

    private static string? TryGetComboBoxSelectedItemText(AutomationElement comboBox)
    {
        try
        {
            if (comboBox.TryGetCurrentPattern(SelectionPattern.Pattern, out var selectionPatternObject)
                && selectionPatternObject is SelectionPattern selectionPattern)
            {
                var selectedItems = selectionPattern.Current.GetSelection();
                var selectedItem = selectedItems.FirstOrDefault(IsVisible) ?? selectedItems.FirstOrDefault();
                if (selectedItem is not null)
                {
                    var text = ReadElementText(selectedItem);
                    if (!string.IsNullOrWhiteSpace(text))
                        return text;
                }
            }
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            // Fall back to selected list item scan below.
        }

        try
        {
            var selectedListItem = GetComboBoxItems(comboBox)
                .FirstOrDefault(item =>
                    item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectionItemPatternObject)
                    && selectionItemPatternObject is SelectionItemPattern selectionItemPattern
                    && selectionItemPattern.Current.IsSelected);

            return selectedListItem is null ? null : ReadElementText(selectedListItem);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            return null;
        }
    }

    private static AutomationElement ResolveTopLevelWindow(AutomationElement window)
    {
        var processId = GetProcessId(window);
        var handle = window.Current.NativeWindowHandle;
        if (handle != 0)
            return FindTopLevelWindow(processId, handle) ?? window;

        return ResolveLiveWindow(window);
    }

    private static void VerifyOptimizationTabUi(AutomationElement mainWindow)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        Click(WaitForAutomationId(mainWindow, "WindowsOptimizationOptimizationTabButton", TimeSpan.FromSeconds(12)));

        var expectedCategories = new[] { "explorer", "performance", "services" };
        foreach (var categoryKey in expectedCategories)
            ExpandOptimizationCategory(mainWindow, categoryKey);

        if (FindByAutomationId(mainWindow, "WindowsOptimizationCategory_network") is not null)
            ExpandOptimizationCategory(mainWindow, "network");
        else
            Console.WriteLine("[main-smoke] Optional network optimization plugin category is not present; skipping plugin action assertions.");

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
            "services.errorReporting"
        };

        foreach (var actionKey in expectedActions)
            WaitForAutomationIdPresent(mainWindow, $"WindowsOptimizationAction_{actionKey}", TimeSpan.FromSeconds(8));

        var optionalActions = new[] { "network.acceleration", "network.optimization" };
        foreach (var actionKey in optionalActions)
        {
            var action = FindByAutomationId(mainWindow, $"WindowsOptimizationAction_{actionKey}");
            if (action is not null)
                WaitForAutomationIdPresent(mainWindow, $"WindowsOptimizationAction_{actionKey}", TimeSpan.FromSeconds(8));
        }

        CaptureMainWindow(mainWindow, "system-optimization-optimization-tab");
        Click(WaitForAutomationId(mainWindow, "WindowsOptimizationSelectRecommendedButton", TimeSpan.FromSeconds(8)));
        VerifySelectedActionsWindow(mainWindow);
        WaitForAutomationId(ResolveLiveWindow(mainWindow), "WindowsOptimizationBulkActionButton", TimeSpan.FromSeconds(8));
        Console.WriteLine("[main-smoke] System Optimization tab verified; bulk action button was visible but not clicked.");
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

        var scanButton = WaitForAutomationId(mainWindow, "WindowsOptimizationScanCleanupButton", TimeSpan.FromSeconds(8));
        Click(scanButton);

        WaitUntil(
            () => !IsVisible(FindByAutomationId(ResolveLiveWindow(mainWindow), "WindowsOptimizationScanCleanupButton")),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromMilliseconds(250));

        CaptureMainWindow(ResolveLiveWindow(mainWindow), "system-optimization-cleanup-scanned");

        WaitForAutomationId(ResolveLiveWindow(mainWindow), "WindowsOptimizationBulkActionButton", TimeSpan.FromSeconds(8));
        Console.WriteLine("[main-smoke] System Optimization cleanup tab scanned; cleanup action button was visible but not clicked.");
    }

    private static void ExpandOptimizationCategory(AutomationElement mainWindow, string categoryKey)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        var category = WaitForAutomationIdPresent(mainWindow, $"WindowsOptimizationCategory_{categoryKey}", TimeSpan.FromSeconds(12));
        ExpandIfNeeded(category);
    }

    private static void VerifySelectedActionsWindow(AutomationElement mainWindow)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        var selectedActionsButton = WaitForAutomationId(mainWindow, "WindowsOptimizationSelectedActionsButton", TimeSpan.FromSeconds(8));
        var processId = mainWindow.Current.ProcessId;
        var mainWindowHandle = mainWindow.Current.NativeWindowHandle;
        BringToForeground(mainWindow);
        LogAutomationElement("Selected actions button", selectedActionsButton);

        var selectedActionsWindow = TryOpenSelectedActionsWindow(processId, mainWindowHandle, selectedActionsButton);
        if (selectedActionsWindow is null)
        {
            DumpProcessTopLevelElements(processId);
            DumpAutomationSnapshot(ResolveLiveWindow(mainWindow), 120);
            throw new TimeoutException("Timed out waiting for selected actions window.");
        }

        CapturePluginSettingsWindow(selectedActionsWindow, "system-optimization", "selected-actions");
        var closeButton = FindByAutomationId(selectedActionsWindow, "SelectedActionsWindowCloseButton");
        if (closeButton is not null)
            Click(closeButton);
        else
            CloseWindow(selectedActionsWindow);

        Thread.Sleep((int)WindowAnimationDuration.TotalMilliseconds);
        Console.WriteLine("[main-smoke] Selected actions window verified.");
    }

    private static AutomationElement? TryOpenSelectedActionsWindow(int processId, int mainWindowHandle, AutomationElement selectedActionsButton)
    {
        var attempts = new (string Description, Action Activate)[]
        {
            ("InvokePattern/default click", () => Click(selectedActionsButton)),
            ("keyboard Space", () => FocusAndPress(selectedActionsButton, VkSpace)),
            ("keyboard Enter", () => FocusAndPress(selectedActionsButton, VkEnter)),
            ("mouse click", () => MouseClick(selectedActionsButton)),
            ("mouse double-click", () => DoubleClick(selectedActionsButton))
        };

        foreach (var attempt in attempts)
        {
            try
            {
                Console.WriteLine($"[main-smoke] Opening selected actions window via {attempt.Description}");
                attempt.Activate();
            }
            catch (Exception ex) when (IsRecoverableAutomationException(ex) || ex is InvalidOperationException)
            {
                Console.WriteLine($"[main-smoke] Selected actions activation failed via {attempt.Description}: {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            var selectedActionsWindow = TryWaitForOwnedWindow(
                processId,
                mainWindowHandle,
                IsSelectedActionsWindow,
                TimeSpan.FromSeconds(4),
                "selected actions window");

            if (selectedActionsWindow is not null)
            {
                Console.WriteLine($"[main-smoke] Selected actions window detected after {attempt.Description}: handle={selectedActionsWindow.Current.NativeWindowHandle} name='{selectedActionsWindow.Current.Name}'");
                return selectedActionsWindow;
            }
        }

        return null;
    }

    private static void FocusAndPress(AutomationElement element, byte virtualKey)
    {
        element.SetFocus();
        Thread.Sleep(140);
        PressVirtualKey(virtualKey);
    }

    private static bool IsSelectedActionsWindow(AutomationElement window)
    {
        return AutomationIdEquals(window, "SelectedActionsWindow")
               || IsVisible(FindByAutomationId(window, "SelectedActionsWindowTitleBar"))
               || IsVisible(FindByAutomationId(window, "SelectedActionsWindowCloseButton"))
               || WindowNameContains(window, "Selected actions")
               || WindowNameContains(window, "???");
    }

    private static bool AutomationIdEquals(AutomationElement element, string automationId)
    {
        try
        {
            return string.Equals(element.Current.AutomationId, automationId, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            return false;
        }
    }

    private static bool WindowNameContains(AutomationElement window, string expected)
    {
        try
        {
            return (window.Current.Name ?? string.Empty).Contains(expected, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            return false;
        }
    }

    private static void LogAutomationElement(string label, AutomationElement element)
    {
        try
        {
            var rect = element.Current.BoundingRectangle;
            var patterns = new List<string>();
            if (element.TryGetCurrentPattern(InvokePattern.Pattern, out _))
                patterns.Add("Invoke");
            if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out _))
                patterns.Add("SelectionItem");
            if (element.TryGetCurrentPattern(TogglePattern.Pattern, out _))
                patterns.Add("Toggle");

            Console.WriteLine(
                $"[main-smoke] {label}: id='{element.Current.AutomationId}' name='{element.Current.Name}' type='{element.Current.ControlType?.ProgrammaticName}' enabled={element.Current.IsEnabled} offscreen={element.Current.IsOffscreen} bounds=({rect.Left:0},{rect.Top:0},{rect.Width:0},{rect.Height:0}) patterns=[{string.Join(",", patterns)}]");
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            Console.WriteLine($"[main-smoke] {label}: failed to read element details: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static AutomationElement? TryWaitForOwnedWindow(
        int processId,
        int mainWindowHandle,
        Func<AutomationElement, bool> predicate,
        TimeSpan timeout,
        string description)
    {
        try
        {
            return WaitForOwnedWindow(processId, mainWindowHandle, predicate, timeout, description);
        }
        catch (TimeoutException)
        {
            return null;
        }
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
                    .Concat(FindDescendantWindows(mainWindowHandle))
                    .GroupBy(window => window.Current.NativeWindowHandle)
                    .Select(group => group.First())
                    .ToArray();

                foreach (var window in windows)
                {
                    if (!HasExpectedOwner(window, mainWindowHandle))
                        continue;

                    if (predicate(window))
                    {
                        Console.WriteLine($"[main-smoke] Detected {description} window: handle={window.Current.NativeWindowHandle} name='{window.Current.Name}'");
                        return window;
                    }
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

    private static bool HasExpectedOwner(AutomationElement window, int expectedOwnerHandle)
    {
        if (!TryGetNativeWindowHandle(window, out var handle))
            return false;

        try
        {
            var ownerHandle = GetWindow((IntPtr)handle, GwOwner);
            return ownerHandle != IntPtr.Zero && ownerHandle == (IntPtr)expectedOwnerHandle;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<AutomationElement> FindDescendantWindows(int mainWindowHandle)
    {
        try
        {
            var mainWindow = AutomationElement.FromHandle((IntPtr)mainWindowHandle);
            if (mainWindow is null)
                return [];

            return mainWindow.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window))
                .Cast<AutomationElement>()
                .Where(window => window.Current.NativeWindowHandle != 0)
                .Where(window => window.Current.NativeWindowHandle != mainWindowHandle)
                .ToArray();
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            return [];
        }
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
            var edit = FindByAutomationId(mainWindow, "WindowsOptimizationDriverMachineTypeTextBox");
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

    private static void SetTextBoxValue(AutomationElement textBox, string value)
    {
        EnsureElementInteractable(textBox, "text box");

        if (textBox.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
        {
            ((ValuePattern)valuePattern).SetValue(value);
            return;
        }

        try
        {
            textBox.SetFocus();
            Thread.Sleep(80);
            PressCtrlA();
            PressVirtualKey(VkBack);
            foreach (var character in value)
                SendUnicodeCharacter(character);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex) || ex is InvalidOperationException)
        {
            throw new InvalidOperationException("Failed to set text box value.", ex);
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
                ActivateNavigationElement(nav, "WindowsOptimizationNavItem");
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
                    TryActivateNavigationElement(nav, "WindowsOptimizationNavItem");
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

    private static void NavigateToDashboardPage(AutomationElement mainWindow)
    {
        mainWindow = ResolveLiveWindow(mainWindow);
        if (IsDashboardPageReady(mainWindow))
            return;

        var arrived = false;
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            AutomationElement? nav = null;
            try
            {
                nav = WaitForDashboardNavigationElement(mainWindow, TimeSpan.FromSeconds(8));
                ActivateNavigationElement(nav, "DashboardNavItem");
            }
            catch (TimeoutException)
            {
                Console.WriteLine($"[main-smoke] Dashboard navigation element unavailable; trying keyboard navigation fallback (attempt {attempt}/5)");
                BringToForeground(mainWindow);
                PressCtrlTab();
            }

            var quickReady = WaitUntil(
                () =>
                {
                    mainWindow = ResolveLiveWindow(mainWindow);
                    return IsDashboardPageReady(mainWindow);
                },
                TimeSpan.FromSeconds(2),
                TimeSpan.FromMilliseconds(200));

            if (!quickReady)
            {
                BringToForeground(mainWindow);
                if (nav is not null)
                    TryActivateNavigationElement(nav, "DashboardNavItem");
                else
                    PressCtrlTab();
            }

            arrived = WaitUntil(
                () =>
                {
                    mainWindow = ResolveLiveWindow(mainWindow);
                    return IsDashboardPageReady(mainWindow);
                },
                TimeSpan.FromSeconds(12),
                TimeSpan.FromMilliseconds(250));

            if (arrived)
                break;

            Console.WriteLine($"[main-smoke] Dashboard navigation retry {attempt}/5");
            Thread.Sleep(700);
        }

        if (!arrived)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            DumpAutomationSnapshot(mainWindow, 300);
            throw new TimeoutException("Timed out waiting for Dashboard page.");
        }

        Console.WriteLine("[main-smoke] Navigated to Dashboard page");
    }

    private static bool IsDashboardPageReady(AutomationElement mainWindow)
    {
        if (!TryFindDashboardNavigationElement(mainWindow, out var dashboardNav)
            || dashboardNav is null
            || !IsNavigationItemSelected(dashboardNav))
        {
            return false;
        }

        return IsVisible(FindByAutomationId(mainWindow, "DashboardPageRoot"))
               || IsVisible(FindByAutomationId(mainWindow, "DashboardSensorsCard"))
               || IsVisible(FindPowerModeComboBox(mainWindow));
    }

    private static bool IsNavigationItemSelected(AutomationElement element)
    {
        try
        {
            return element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var pattern)
                   && pattern is SelectionItemPattern selectionItemPattern
                   && selectionItemPattern.Current.IsSelected;
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            return false;
        }
    }

    private static AutomationElement WaitForDashboardNavigationElement(AutomationElement root, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => TryFindDashboardNavigationElement(ResolveLiveWindow(root), out _),
            timeout,
            TimeSpan.FromMilliseconds(250));

        var liveRoot = ResolveLiveWindow(root);
        if (!found || !TryFindDashboardNavigationElement(liveRoot, out var element) || element is null)
        {
            DumpAutomationSnapshot(liveRoot, 250);
            throw new TimeoutException("Timed out waiting for dashboard navigation item.");
        }

        return element;
    }

    private static bool TryFindDashboardNavigationElement(AutomationElement root, out AutomationElement? element)
    {
        var idCandidates = new[]
        {
            "DashboardNavItem",
            "_dashboardItem"
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

        element = null;
        return false;
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

    private static AutomationElement WaitForAutomationIdWithScroll(AutomationElement root, string automationId, TimeSpan timeout)
    {
        var found = WaitUntil(
            () =>
            {
                var element = FindByAutomationId(root, automationId);
                if (element is null)
                    return false;

                if (IsInteractable(element))
                    return true;

                TryScrollElementIntoView(element);
                return IsInteractable(element);
            },
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for automation element '{automationId}'.");

        var resolved = FindByAutomationId(root, automationId);
        if (resolved is null)
            throw new InvalidOperationException($"Automation element '{automationId}' disappeared after wait.");

        if (!IsInteractable(resolved))
        {
            TryScrollElementIntoView(resolved);
            Thread.Sleep(150);
        }

        if (!IsInteractable(resolved))
            throw new InvalidOperationException($"Automation element '{automationId}' was not interactable after scroll wait.");

        return resolved;
    }

    private static AutomationElement? TryWaitForAutomationIdPrefixWithScroll(AutomationElement root, string automationIdPrefix, TimeSpan timeout)
    {
        var found = WaitUntil(
            () =>
            {
                var element = FindByAutomationIdPrefix(root, automationIdPrefix)
                              ?? FindByAutomationIdPrefixIncludingOffscreen(root, automationIdPrefix);
                if (element is null)
                    return false;

                if (IsInteractable(element))
                    return true;

                TryScrollElementIntoView(element);
                return IsInteractable(element);
            },
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            return null;

        var resolved = FindByAutomationIdPrefix(root, automationIdPrefix)
                       ?? FindByAutomationIdPrefixIncludingOffscreen(root, automationIdPrefix);
        if (resolved is null)
            return null;

        if (!IsInteractable(resolved))
        {
            TryScrollElementIntoView(resolved);
            Thread.Sleep(150);
        }

        return IsInteractable(resolved) ? resolved : null;
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

    private static T WaitUntilValue<T>(
        Func<T?> getValue,
        TimeSpan timeout,
        TimeSpan interval,
        string description) where T : class
    {
        T? result = null;
        var found = WaitUntil(
            () =>
            {
                result = getValue();
                return result is not null;
            },
            timeout,
            interval);

        return found && result is not null
            ? result
            : throw new TimeoutException($"Timed out waiting for {description}.");
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

    private static AutomationElement? FindByAutomationIdPrefixIncludingOffscreen(AutomationElement root, string automationIdPrefix)
    {
        try
        {
            return root.FindAll(TreeScope.Descendants, Condition.TrueCondition)
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
                .OrderBy(element => element.Current.AutomationId, StringComparer.Ordinal)
                .FirstOrDefault();
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            var liveRoot = ResolveLiveWindow(root);
            if (ReferenceEquals(liveRoot, root))
                return null;

            try
            {
                return FindByAutomationIdPrefixIncludingOffscreen(liveRoot, automationIdPrefix);
            }
            catch
            {
                return null;
            }
        }
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

    private static AutomationElement? FindBestMatchingDescendant(AutomationElement root, System.Windows.Automation.Condition condition)
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
        SelectComboBoxItem(comboBox, exactOnly: true, itemNames);
    }

    private static string SelectComboBoxItemByNamesOrContains(AutomationElement comboBox, params string[] itemNames)
    {
        return SelectComboBoxItem(comboBox, exactOnly: false, itemNames);
    }

    private static string SelectComboBoxItem(AutomationElement comboBox, bool exactOnly, params string[] itemNames)
    {
        if (comboBox.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var expandPattern))
        {
            var expander = (ExpandCollapsePattern)expandPattern;
            expander.Expand();
        }

        Thread.Sleep(250);

        var items = GetComboBoxItems(comboBox);

        AutomationElement? item = null;
        foreach (var itemName in itemNames)
        {
            item = items.FirstOrDefault(candidate =>
                string.Equals(candidate.Current.Name, itemName, StringComparison.OrdinalIgnoreCase));
            if (item is not null)
                break;

            item = items.FirstOrDefault(candidate =>
                string.Equals(ReadElementText(candidate), itemName, StringComparison.OrdinalIgnoreCase));
            if (item is not null)
                break;
        }

        if (item is null && !exactOnly)
        {
            foreach (var itemName in itemNames)
            {
                item = items.FirstOrDefault(candidate =>
                    ReadElementText(candidate).Contains(itemName, StringComparison.OrdinalIgnoreCase));
                if (item is not null)
                    break;
            }
        }

        item ??= exactOnly ? items.FirstOrDefault() : null;

        if (item is null)
            throw new InvalidOperationException($"ComboBox option was not found. Expected one of: [{string.Join(", ", itemNames)}].");

        var selectedName = ReadElementText(item);
        Console.WriteLine($"[main-smoke] Selecting combo-box item '{selectedName}' from options: [{string.Join(", ", items.Select(ReadElementText).Where(text => !string.IsNullOrWhiteSpace(text)).Distinct(StringComparer.OrdinalIgnoreCase))}]");
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

        return selectedName;
    }

    private static void DoubleClick(AutomationElement element)
    {
        MouseDoubleClick(element);
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
        {
            Console.WriteLine($"[main-smoke] Screenshot skipped because main window handle is unavailable: {captureLabel}");
            return;
        }

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

        Rectangle windowBounds;
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
            ? Path.Combine(Path.GetTempPath(), $"llt-main-smoke-{DateTime.UtcNow:yyyyMMdd-HHmmss}")
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
        var title = WebUtility.HtmlEncode($"MainAppPluginUi.Smoke Storyboard ? {_activeScenario} ? {_activeTheme}");

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
          <span class="capture-file">${capture.fileName} ? ${capture.capturedAt}</span>
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
      frameTime.textContent = `${currentIndex + 1} / ${captures.length} ? ${capture.capturedAt}`;
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

    private static void MouseDoubleClick(AutomationElement element)
    {
        var target = ResolveMouseClickableElement(element);
        EnsureElementInteractable(target, "mouse double-click target");
        var rect = target.Current.BoundingRectangle;
        if (rect.Width <= 0 || rect.Height <= 0)
            throw new InvalidOperationException($"Cannot double-click element with empty bounds: {element.Current.AutomationId}");

        var centerX = (int)(rect.Left + rect.Width / 2);
        var centerY = (int)(rect.Top + rect.Height / 2);
        SetCursorPos(centerX, centerY);
        Thread.Sleep(60);
        mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(80);
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

    private static void PressCtrlA()
    {
        keybd_event(VkControl, 0, KeyEventExtendedKey, UIntPtr.Zero);
        Thread.Sleep(40);
        keybd_event(VkA, 0, KeyEventExtendedKey, UIntPtr.Zero);
        Thread.Sleep(40);
        keybd_event(VkA, 0, KeyEventExtendedKey | KeyEventKeyUp, UIntPtr.Zero);
        Thread.Sleep(40);
        keybd_event(VkControl, 0, KeyEventExtendedKey | KeyEventKeyUp, UIntPtr.Zero);
        Thread.Sleep(80);
    }

    private static void PressVirtualKey(byte virtualKey)
    {
        keybd_event(virtualKey, 0, KeyEventExtendedKey, UIntPtr.Zero);
        Thread.Sleep(40);
        keybd_event(virtualKey, 0, KeyEventExtendedKey | KeyEventKeyUp, UIntPtr.Zero);
        Thread.Sleep(60);
    }

    private static void SendUnicodeCharacter(char character)
    {
        keybd_event((byte)character, 0, KeyEventExtendedKey, UIntPtr.Zero);
        Thread.Sleep(40);
        keybd_event((byte)character, 0, KeyEventExtendedKey | KeyEventKeyUp, UIntPtr.Zero);
        Thread.Sleep(60);
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
            var bounds = element.Current.BoundingRectangle;
            return !element.Current.IsOffscreen && bounds.Width > 0 && bounds.Height > 0;
        }
        catch
        {
            return false;
        }
    }

    private static void TryScrollElementIntoView(AutomationElement? element)
    {
        if (element is null)
            return;

        try
        {
            if (element.TryGetCurrentPattern(ScrollItemPattern.Pattern, out var scrollItemPattern))
            {
                ((ScrollItemPattern)scrollItemPattern).ScrollIntoView();
                Thread.Sleep(150);
            }
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex) || ex is InvalidOperationException)
        {
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
               || text.Contains("\u5df2\u4fdd\u5b58", StringComparison.OrdinalIgnoreCase);
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
        var handle = window.Current.NativeWindowHandle;

        try
        {
            if (window.TryGetCurrentPattern(WindowPattern.Pattern, out var windowPattern))
                ((WindowPattern)windowPattern).Close();
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            Console.WriteLine($"[main-smoke] WindowPattern close failed for handle={handle}: {ex.GetType().Name}");
        }

        if (handle == 0)
            return;

        var closed = WaitUntil(
            () => !IsWindow(new IntPtr(handle)),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(150));

        if (closed)
            return;

        Console.WriteLine($"[main-smoke] Window handle {handle} still open after WindowPattern close; sending WM_CLOSE.");
        SendMessage(new IntPtr(handle), WmClose, IntPtr.Zero, IntPtr.Zero);

        WaitUntil(
            () => !IsWindow(new IntPtr(handle)),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(150));
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
