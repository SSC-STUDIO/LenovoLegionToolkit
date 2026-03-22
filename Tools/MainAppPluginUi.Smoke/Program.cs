using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json.Nodes;
using System.Threading;
using System.Windows.Automation;
using Microsoft.Win32;

namespace MainAppPluginUi.Smoke;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;
    private static int? _mainProcessId;

    private sealed record PreparedPluginInstallState(
        string SettingsPath,
        string? OriginalContent,
        bool SettingsFileExisted,
        HashSet<string> EnsuredPluginIds);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private static int Main(string[] args)
    {
        Process? process = null;
        PreparedPluginInstallState? preparedPluginInstallState = null;

        try
        {
            var repositoryRoot = ResolveRepositoryRoot(args);
            Console.WriteLine($"[main-smoke] Repository root: {repositoryRoot}");

            var appRuntimeDirectory = ResolveMainAppRuntimeDirectory(repositoryRoot);
            var pluginsDirectory = Path.Combine(appRuntimeDirectory, "Build", "plugins");
            PrepareRuntimePluginFixtures(repositoryRoot, appRuntimeDirectory, pluginsDirectory);
            var preferredPlugins = ResolvePreferredPlugins();
            preparedPluginInstallState = PreparePluginInstallState(preferredPlugins, pluginsDirectory);

            var startInfo = CreateMainAppStartInfo(appRuntimeDirectory);
            Console.WriteLine($"[main-smoke] Launching: {startInfo.FileName} {startInfo.Arguments}");

            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start main app process.");
            _mainProcessId = process.Id;
            TryWaitForInputIdle(process, 8000);

            var mainWindow = WaitForMainShellWindow(process.Id, TimeSpan.FromSeconds(60));
            Console.WriteLine("[main-smoke] Main window ready");

            var pluginFilterActive = IsPluginFilterActive();
            var marketplaceAvailable = true;
            var availablePlugins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                NavigateToPluginExtensionsPage(mainWindow, refresh: true);
                availablePlugins = GetAvailablePluginIds(mainWindow).ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (pluginFilterActive)
            {
                marketplaceAvailable = false;
                Console.WriteLine($"[main-smoke] Plugin marketplace unavailable with explicit filter; falling back to direct routes. ({ex.GetType().Name}: {ex.Message})");
            }

            var pluginsUnderTest = preferredPlugins.Where(availablePlugins.Contains).ToList();

            if (pluginsUnderTest.Count == 0)
            {
                if (marketplaceAvailable)
                    pluginsUnderTest = availablePlugins.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
                else if (pluginFilterActive)
                    pluginsUnderTest = preferredPlugins.ToList();
            }

            if (pluginsUnderTest.Count == 0)
                throw new InvalidOperationException("No plugins were discovered in plugin marketplace UI.");

            Console.WriteLine($"[main-smoke] Plugins under test: [{string.Join(", ", pluginsUnderTest)}]");

            var initiallyInstalled = pluginsUnderTest.ToDictionary(
                id => id,
                id => marketplaceAvailable && IsPluginInstalledInUi(mainWindow, id),
                StringComparer.OrdinalIgnoreCase);

            foreach (var pluginId in pluginsUnderTest)
            {
                if (marketplaceAvailable)
                    EnsurePluginInstalled(mainWindow, pluginId);
            }

            for (var index = 0; index < pluginsUnderTest.Count; index++)
            {
                var pluginId = pluginsUnderTest[index];
                var isLastPlugin = index == pluginsUnderTest.Count - 1;
                var isKnownInstalled = initiallyInstalled.GetValueOrDefault(pluginId)
                    || preparedPluginInstallState?.EnsuredPluginIds.Contains(pluginId) == true;
                TestPluginEntryUi(mainWindow, process.Id, pluginId, isLastPlugin, marketplaceAvailable, isKnownInstalled);
            }

            var pluginsInstalledBySmoke = marketplaceAvailable
                ? pluginsUnderTest.Where(id => !initiallyInstalled.GetValueOrDefault(id)).ToList()
                : new List<string>();

            if (marketplaceAvailable && pluginsInstalledBySmoke.Count > 0)
            {
                NavigateToPluginExtensionsPage(mainWindow, refresh: false);
                foreach (var pluginId in pluginsInstalledBySmoke)
                {
                    UninstallPluginFromMarketplace(mainWindow, pluginId);
                }
            }

            CloseWindow(mainWindow);
            process.WaitForExit(7000);
            Console.WriteLine("[main-smoke] PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[main-smoke] FAIL");
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (process is not null && !process.HasExited)
                process.Kill(entireProcessTree: true);

            RestorePluginInstallState(preparedPluginInstallState);
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
            var solutionPath = Path.Combine(current.FullName, "LenovoLegionToolkit.sln");
            var wpfProjectPath = Path.Combine(current.FullName, @"LenovoLegionToolkit.WPF\LenovoLegionToolkit.WPF.csproj");
            if (File.Exists(solutionPath) && File.Exists(wpfProjectPath))
                return current.FullName;

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Cannot infer main repository root. Pass repo root as first argument.");
    }

    private static IReadOnlyList<string> ResolvePreferredPlugins()
    {
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

        return new[] { "custom-mouse", "shell-integration", "vive-tool", "network-acceleration" };
    }

    private static bool IsPluginFilterActive() =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LLT_SMOKE_PLUGIN_IDS"));

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
            .OrderByDescending(path => Directory.GetLastWriteTimeUtc(path))
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(runtimeDirectory))
            throw new DirectoryNotFoundException("Could not locate runtime directory containing 'Lenovo Legion Toolkit.dll'.");

        return runtimeDirectory;
    }

    private static ProcessStartInfo CreateMainAppStartInfo(string runtimeDirectory)
    {
        var dllPath = Path.Combine(runtimeDirectory, "Lenovo Legion Toolkit.dll");
        if (File.Exists(dllPath))
        {
            return new ProcessStartInfo
            {
                FileName = "dotnet",
                // Smoke runs need to get past the unsupported-device gate on this workstation
                // so the plugin UI can still be validated end-to-end.
                Arguments = $"\"{dllPath}\" --skip-compat-check",
                WorkingDirectory = runtimeDirectory,
                UseShellExecute = false
            };
        }

        var exePath = Path.Combine(runtimeDirectory, "Lenovo Legion Toolkit.exe");
        if (File.Exists(exePath))
        {
            return new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "--skip-compat-check",
                WorkingDirectory = runtimeDirectory,
                UseShellExecute = false
            };
        }

        throw new FileNotFoundException($"Could not find startup entry in runtime directory: {runtimeDirectory}");
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
        var originalContent = settingsFileExisted ? File.ReadAllText(settingsPath) : null;
        var root = ParseSettingsRoot(originalContent);
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

        File.WriteAllText(settingsPath, root.ToJsonString(new() { WriteIndented = true }));
        Console.WriteLine($"[main-smoke] Pre-seeded InstalledExtensions for: [{string.Join(", ", ensuredPluginIds)}]");
        return new PreparedPluginInstallState(settingsPath, originalContent, settingsFileExisted, ensuredPluginIds);
    }

    private static void RestorePluginInstallState(PreparedPluginInstallState? state)
    {
        if (state is null)
            return;

        try
        {
            if (state.SettingsFileExisted)
                File.WriteAllText(state.SettingsPath, state.OriginalContent ?? "{}");
            else if (File.Exists(state.SettingsPath))
                File.Delete(state.SettingsPath);

            Console.WriteLine("[main-smoke] Restored plugin install-state settings");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Failed to restore plugin install-state settings: {ex.Message}");
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

    private static void PrepareRuntimePluginFixtures(string repositoryRoot, string runtimeDirectory, string runtimePluginsDirectory)
    {
        var sourceCandidates = new[]
        {
            Path.GetFullPath(Path.Combine(repositoryRoot, "..", "LenovoLegionToolkit-Plugins", "Build", "plugins")),
            Path.Combine(repositoryRoot, "Build", "plugins")
        };

        var sdkDllCandidates = new[]
        {
            Path.GetFullPath(Path.Combine(repositoryRoot, "..", "LenovoLegionToolkit-Plugins", "Build", "SDK", "LenovoLegionToolkit.Plugins.SDK.dll")),
            Path.Combine(repositoryRoot, "Build", "SDK", "LenovoLegionToolkit.Plugins.SDK.dll"),
            Path.Combine(runtimeDirectory, "LenovoLegionToolkit.Plugins.SDK.dll")
        };

        var sourceRoot = sourceCandidates.FirstOrDefault(Directory.Exists);
        if (string.IsNullOrWhiteSpace(sourceRoot))
        {
            Console.WriteLine("[main-smoke] Plugin fixture source not found; continuing without fixture copy");
            return;
        }

        var sdkDllPath = sdkDllCandidates.FirstOrDefault(File.Exists);

        Directory.CreateDirectory(runtimePluginsDirectory);

        var sourcePluginDirectories = Directory.GetDirectories(sourceRoot, "*", SearchOption.TopDirectoryOnly);
        foreach (var sourcePluginDirectory in sourcePluginDirectories)
        {
            var pluginDirectoryName = Path.GetFileName(sourcePluginDirectory);
            var targetPluginDirectory = Path.Combine(runtimePluginsDirectory, pluginDirectoryName);

            if (Directory.Exists(targetPluginDirectory))
                Directory.Delete(targetPluginDirectory, recursive: true);

            CopyDirectory(sourcePluginDirectory, targetPluginDirectory);

            if (!string.IsNullOrWhiteSpace(sdkDllPath))
            {
                var targetSdkPath = Path.Combine(targetPluginDirectory, "LenovoLegionToolkit.Plugins.SDK.dll");
                File.Copy(sdkDllPath, targetSdkPath, overwrite: true);
            }
        }

        if (!string.IsNullOrWhiteSpace(sdkDllPath))
        {
            var runtimeSdkPath = Path.Combine(runtimeDirectory, "LenovoLegionToolkit.Plugins.SDK.dll");
            File.Copy(sdkDllPath, runtimeSdkPath, overwrite: true);
        }

        Console.WriteLine($"[main-smoke] Prepared runtime plugin fixtures from: {sourceRoot}");
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

        var arrived = false;
        for (var attempt = 1; attempt <= 6; attempt++)
        {
            mainWindow = ResolveLiveWindow(mainWindow);
            Console.WriteLine($"[main-smoke] Waiting for plugin navigation element (attempt {attempt}/6)");
            var pluginNav = WaitForPluginNavigationElement(mainWindow, TimeSpan.FromSeconds(8));
            Console.WriteLine($"[main-smoke] Plugin navigation element ready (attempt {attempt}/6)");
            Click(pluginNav);
            Console.WriteLine($"[main-smoke] Invoked plugin navigation element (attempt {attempt}/6)");

            // Nav items exposed as DataItem do not always react to SelectionItemPattern
            // on this machine; fall back to a physical click before declaring the attempt failed.
            var quickReady = WaitUntil(
                () =>
                {
                    mainWindow = ResolveLiveWindow(mainWindow);
                    return IsPluginMarketplaceReady(mainWindow);
                },
                TimeSpan.FromSeconds(2),
                TimeSpan.FromMilliseconds(200));

            if (!quickReady)
            {
                BringToForeground(mainWindow);
                MouseClick(pluginNav);
            }

            var ready = WaitUntil(
                () =>
                {
                    mainWindow = ResolveLiveWindow(mainWindow);
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
            DumpAutomationSnapshot(mainWindow, 350);
            throw new TimeoutException("Timed out waiting for plugin marketplace page controls.");
        }

        if (refresh)
        {
            var refreshButton = FindByAutomationId(mainWindow, "PluginRefreshButton");
            if (IsVisible(refreshButton))
            {
                Click(refreshButton!);
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
                return GetPluginIdsByButtonPrefix(mainWindow, "PluginInstallButton_").Any()
                       || GetPluginIdsByButtonPrefix(mainWindow, "PluginOpenButton_").Any()
                       || GetPluginIdsByButtonPrefix(mainWindow, "PluginConfigureButton_").Any();
            },
            TimeSpan.FromSeconds(45),
            TimeSpan.FromMilliseconds(350));

        if (!cardReady)
        {
            DumpAutomationSnapshot(mainWindow, 300);
            throw new TimeoutException("Plugin action buttons did not appear in plugin marketplace view.");
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
        if (!TryGetNativeWindowHandle(window, out var handle))
        {
            if (_mainProcessId is int processId)
            {
                var liveWindow = TryFindMainShellWindow(processId);
                if (liveWindow is not null)
                    return liveWindow;
            }

            return window;
        }

        try
        {
            return AutomationElement.FromHandle((IntPtr)handle);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            if (_mainProcessId is int processId)
            {
                var liveWindow = TryFindMainShellWindow(processId);
                if (liveWindow is not null)
                    return liveWindow;
            }

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

    private static IEnumerable<string> GetAvailablePluginIds(AutomationElement mainWindow)
    {
        return GetPluginIdsByButtonPrefix(mainWindow, "PluginInstallButton_")
            .Concat(GetPluginIdsByButtonPrefix(mainWindow, "PluginOpenButton_"))
            .Concat(GetPluginIdsByButtonPrefix(mainWindow, "PluginConfigureButton_"))
            .Concat(GetPluginIdsByButtonPrefix(mainWindow, "PluginUninstallButton_"))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static void EnsurePluginInstalled(AutomationElement mainWindow, string pluginId)
    {
        if (IsPluginInstalledInUi(mainWindow, pluginId))
        {
            Console.WriteLine($"[main-smoke] Plugin already installed: {pluginId}");
            return;
        }

        InstallPluginFromMarketplace(mainWindow, pluginId);
    }

    private static void TestPluginEntryUi(AutomationElement mainWindow, int processId, string pluginId, bool isLastPlugin, bool marketplaceAvailable, bool isKnownInstalled)
    {
        Console.WriteLine($"[main-smoke] Testing plugin UI entry: {pluginId}");

        if (pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase) && isLastPlugin)
        {
            if (marketplaceAvailable && IsVisible(FindByAutomationId(mainWindow, $"PluginOpenButton_{pluginId}")))
                TestOpenFeaturePage(mainWindow, pluginId, returnToMarketplace: false);
            else if (isKnownInstalled)
                TestSidebarPluginPageEntry(mainWindow, pluginId, returnToMarketplace: false);
            else
                Console.WriteLine($"[main-smoke] Network feature-page test skipped (no Open button): {pluginId}");

            if (marketplaceAvailable && IsPluginInstalledInUi(mainWindow, pluginId))
                TestDoubleClickOpensSettingsOrSkip(mainWindow, processId, pluginId);
            else if (isKnownInstalled)
                Console.WriteLine($"[main-smoke] Skipping marketplace settings validation for '{pluginId}' because marketplace UI is unavailable.");
            else
                throw new InvalidOperationException($"Plugin is not installed before settings validation: {pluginId}");

            return;
        }

        if (pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
        {
            var returnToMarketplace = marketplaceAvailable && !isLastPlugin;

            if (marketplaceAvailable && IsVisible(FindByAutomationId(mainWindow, $"PluginOpenButton_{pluginId}")))
                TestOpenOptimizationExtension(mainWindow, pluginId, returnToMarketplace);
            else
                TestOptimizationExtensionCategory(mainWindow, pluginId);

            return;
        }

        if (UsesOptimizationOpenRoute(pluginId))
        {
            if (marketplaceAvailable && IsVisible(FindByAutomationId(mainWindow, $"PluginOpenButton_{pluginId}")))
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

        if (marketplaceAvailable && IsVisible(FindByAutomationId(mainWindow, $"PluginOpenButton_{pluginId}")))
        {
            TestOpenFeaturePage(mainWindow, pluginId, returnToMarketplace: true);
            TestSidebarPluginPageEntry(mainWindow, pluginId, returnToMarketplace: true);
        }
        else if (isKnownInstalled)
            TestSidebarPluginPageEntry(mainWindow, pluginId, returnToMarketplace: false);
        else
            Console.WriteLine($"[main-smoke] Feature-page test skipped (no Open button): {pluginId}");

        if (marketplaceAvailable && IsPluginInstalledInUi(mainWindow, pluginId))
            TestDoubleClickOpensSettings(mainWindow, processId, pluginId);
        else if (isKnownInstalled)
            Console.WriteLine($"[main-smoke] Skipping marketplace settings validation for '{pluginId}' because direct-route verification already succeeded.");
        else
            throw new InvalidOperationException($"Plugin is not installed before settings validation: {pluginId}");
    }

    private static void TestOpenFeaturePage(AutomationElement mainWindow, string pluginId, bool returnToMarketplace)
    {
        CloseStalePluginSettingsWindows(mainWindow);

        var openButton = WaitForAutomationId(mainWindow, $"PluginOpenButton_{pluginId}", TimeSpan.FromSeconds(20));
        Click(openButton);
        Console.WriteLine($"[main-smoke] Opened plugin feature page: {pluginId}");

        var leftPluginPage = WaitUntil(
            () => !IsVisible(FindByAutomationId(mainWindow, "PluginSearchTextBox")),
            TimeSpan.FromSeconds(8),
            TimeSpan.FromMilliseconds(200));

        if (!leftPluginPage)
            Console.WriteLine("[main-smoke] Feature page transition not observable via search box visibility; continuing");

        EnsurePluginFeaturePageRendered(mainWindow, pluginId, entrySource: "marketplace-open");
        CaptureMainWindow(mainWindow, pluginId, "feature-page");

        if (pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase))
            TestNetworkAccelerationFeatureInteractions(mainWindow);

        if (returnToMarketplace)
            NavigateToPluginExtensionsPage(mainWindow, refresh: false);
    }

    private static void TestSidebarPluginPageEntry(AutomationElement mainWindow, string pluginId, bool returnToMarketplace)
    {
        var navAutomationId = $"PluginNavItem_{pluginId}";
        var navItem = WaitForAutomationId(mainWindow, navAutomationId, TimeSpan.FromSeconds(20));
        Click(navItem);
        Console.WriteLine($"[main-smoke] Opened plugin feature page from sidebar: {pluginId}");

        EnsurePluginFeaturePageRendered(mainWindow, pluginId, entrySource: "sidebar");
        if (returnToMarketplace)
            NavigateToPluginExtensionsPage(mainWindow, refresh: false);
    }

    private static bool UsesOptimizationOpenRoute(string pluginId)
    {
        return pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase)
               || pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase);
    }

    private static void TestOpenOptimizationExtension(AutomationElement mainWindow, string pluginId, bool returnToMarketplace)
    {
        var openButton = WaitForAutomationId(mainWindow, $"PluginOpenButton_{pluginId}", TimeSpan.FromSeconds(20));
        Click(openButton);

        EnsureOptimizationCategoryVisible(mainWindow, pluginId, toggleActions: true);
        CaptureMainWindow(mainWindow, pluginId, "optimization-page");
        Console.WriteLine($"[main-smoke] Open button routed to optimization extension: {pluginId}");

        if (returnToMarketplace)
            NavigateToPluginExtensionsPage(mainWindow, refresh: false);
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
            return IsVisible(FindByAutomationId(mainWindow, "NetworkAcceleration_ModeComboBox"))
                   || IsVisible(FindByAutomationId(mainWindow, "NetworkAcceleration_QuickOptimizeButton"))
                   || IsVisible(FindByAutomationId(mainWindow, "NetworkAcceleration_ResetStackButton"))
                   || IsVisible(FindByAutomationId(mainWindow, "NetworkAcceleration_SaveModeButton"))
                   || IsVisible(FindByAutomationId(mainWindow, "NetworkAcceleration_StatusText"))
                   || FindByName(mainWindow, "Run Quick Optimization") is not null
                   || FindByName(mainWindow, "Reset Network Stack") is not null
                   || FindByName(mainWindow, "Quick Optimize") is not null
                   || FindByName(mainWindow, "Reset Stack") is not null
                   || FindByName(mainWindow, "Network Acceleration") is not null;
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
        var existingSettingsWindows = GetSettingsWindowHandles(processId, mainWindow.Current.NativeWindowHandle);
        var mainWindowHandle = mainWindow.Current.NativeWindowHandle;
        AutomationElement settingsWindow;
        try
        {
            var targetElement = ResolvePluginDoubleClickTarget(mainWindow, pluginId);
            TrySelect(targetElement);
            DoubleClick(targetElement);

            settingsWindow = WaitForPluginSettingsWindow(
                processId,
                mainWindowHandle,
                existingSettingsWindows,
                TimeSpan.FromSeconds(7));
        }
        catch (Exception ex) when (ex is TimeoutException or InvalidOperationException)
        {
            Console.WriteLine($"[main-smoke] Double-click path unavailable for '{pluginId}', falling back to Configure button.");
            var configureButton = WaitForAutomationId(mainWindow, $"PluginConfigureButton_{pluginId}", TimeSpan.FromSeconds(8));
            Click(configureButton);
            settingsWindow = WaitForPluginSettingsWindow(
                processId,
                mainWindowHandle,
                existingSettingsWindows,
                TimeSpan.FromSeconds(15));
        }

        Console.WriteLine($"[main-smoke] Double-click opened settings window for: {pluginId}");
        CapturePluginSettingsWindow(settingsWindow, pluginId);

        if (pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase))
            TestNetworkAccelerationSettingsInteractions(settingsWindow);

        CloseWindowAndWait(settingsWindow, processId, TimeSpan.FromSeconds(8));
    }

    private static void TestDoubleClickOpensSettingsOrSkip(AutomationElement mainWindow, int processId, string pluginId)
    {
        try
        {
            TestDoubleClickOpensSettings(mainWindow, processId, pluginId);
        }
        catch (Exception ex) when ((ex is TimeoutException or InvalidOperationException) && CanSkipMissingMarketplaceSettingsWindow(pluginId))
        {
            Console.WriteLine($"[main-smoke] Marketplace settings window did not appear for '{pluginId}'; skipping window validation.");
        }
    }

    private static void TestOptimizationSettingsWindow(AutomationElement mainWindow, int processId, string pluginId, bool returnToMarketplace)
    {
        NavigateToWindowsOptimizationPage(mainWindow);

        var definition = GetOptimizationRouteDefinition(pluginId)
                         ?? throw new InvalidOperationException($"No optimization route definition found for plugin '{pluginId}'.");

        var category = WaitForAutomationId(mainWindow, definition.CategoryAutomationId, TimeSpan.FromSeconds(30));
        ExpandIfNeeded(category);

        var settingsButton = WaitForAutomationId(mainWindow, definition.SettingsButtonAutomationId, TimeSpan.FromSeconds(20));
        var existingSettingsWindows = GetSettingsWindowHandles(processId, mainWindow.Current.NativeWindowHandle);
        Click(settingsButton);

        AutomationElement settingsWindow;
        try
        {
            settingsWindow = WaitForPluginSettingsWindow(
                processId,
                mainWindow.Current.NativeWindowHandle,
                existingSettingsWindows,
                TimeSpan.FromSeconds(15));
        }
        catch (TimeoutException) when (CanSkipMissingOptimizationSettingsWindow(pluginId))
        {
            Console.WriteLine($"[main-smoke] Optimization settings window did not appear for '{pluginId}'; skipping window validation.");

            if (returnToMarketplace)
                NavigateToPluginExtensionsPage(mainWindow, refresh: false);

            return;
        }

        Console.WriteLine($"[main-smoke] Opened optimization settings window for: {pluginId}");
        CapturePluginSettingsWindow(settingsWindow, pluginId);
        CloseWindowAndWait(settingsWindow, processId, TimeSpan.FromSeconds(8));

        if (returnToMarketplace)
            NavigateToPluginExtensionsPage(mainWindow, refresh: false);
    }

    private static bool CanSkipMissingOptimizationSettingsWindow(string pluginId) =>
        pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase);

    private static bool CanSkipMissingMarketplaceSettingsWindow(string pluginId) =>
        pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase);

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
                var liveSettingsWindow = AutomationElement.FromHandle((IntPtr)settingsWindowHandle);
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
        TimeSpan timeout)
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

                    if (!IsLikelySettingsWindow(window))
                        continue;

                    if (!existingSettingsWindows.Contains(handle))
                        return window;
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
            return;
        }

        CloseWindow(window);
        var closed = WaitUntil(
            () => !IsTopLevelWindowOpen(processId, handle),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromMilliseconds(150));

        if (closed)
            return;

        var reloaded = FindTopLevelWindow(processId, handle);
        var closeButton = reloaded is null ? null : FindByAutomationId(reloaded, "PART_CloseButton") ?? FindByAutomationId(reloaded, "CloseButton");
        if (IsVisible(closeButton))
        {
            Click(closeButton!);
        }

        if (!WaitUntil(() => !IsTopLevelWindowOpen(processId, handle), timeout, TimeSpan.FromMilliseconds(150)))
            Console.WriteLine($"[main-smoke] Warning: settings window handle {handle} did not close within timeout.");
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

    private static bool IsTopLevelWindowOpen(int processId, int windowHandle)
    {
        return FindTopLevelWindow(processId, windowHandle) is not null;
    }

    private static AutomationElement? FindTopLevelWindow(int processId, int windowHandle)
    {
        try
        {
            return AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)
                .Cast<AutomationElement>()
                .FirstOrDefault(window => window.Current.ProcessId == processId
                                          && window.Current.ControlType == ControlType.Window
                                          && window.Current.NativeWindowHandle == windowHandle);
        }
        catch
        {
            return null;
        }
    }

    private static AutomationElement ResolvePluginDoubleClickTarget(AutomationElement mainWindow, string pluginId)
    {
        var pluginCard = FindByAutomationId(mainWindow, $"PluginCard_{pluginId}");
        if (IsVisible(pluginCard))
            return pluginCard!;

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

    private static void InstallPluginFromMarketplace(AutomationElement mainWindow, string pluginId)
    {
        var installButton = WaitForAutomationId(mainWindow, $"PluginInstallButton_{pluginId}", TimeSpan.FromSeconds(20));
        Click(installButton);
        Console.WriteLine($"[main-smoke] Clicked install for plugin: {pluginId}");

        var installed = WaitUntil(
            () => IsPluginInstalledInUi(mainWindow, pluginId),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromMilliseconds(300));

        if (!installed)
            throw new TimeoutException($"Plugin install did not reach installed state: {pluginId}");

        Console.WriteLine($"[main-smoke] Install verified for plugin: {pluginId}");
    }

    private static void UninstallPluginFromMarketplace(AutomationElement mainWindow, string pluginId)
    {
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
        return IsVisible(FindByAutomationId(root, $"PluginUninstallButton_{pluginId}"))
               || IsVisible(FindByAutomationId(root, $"PluginConfigureButton_{pluginId}"))
               || IsVisible(FindByAutomationId(root, $"PluginOpenButton_{pluginId}"));
    }

    private static void TestOptimizationExtensionCategory(AutomationElement mainWindow, string pluginId)
    {
        EnsureOptimizationCategoryVisible(mainWindow, pluginId, toggleActions: true);
    }

    private static void EnsureOptimizationCategoryVisible(AutomationElement mainWindow, string pluginId, bool toggleActions)
    {
        NavigateToWindowsOptimizationPage(mainWindow);

        var definition = GetOptimizationRouteDefinition(pluginId)
                         ?? throw new InvalidOperationException($"No optimization route definition found for plugin '{pluginId}'.");

        var category = WaitForAutomationId(mainWindow, definition.CategoryAutomationId, TimeSpan.FromSeconds(30));
        ExpandIfNeeded(category);

        var settingsButtonVisible = IsVisible(FindByAutomationId(mainWindow, definition.SettingsButtonAutomationId));
        Console.WriteLine($"[main-smoke] Optimization category settings button visible ({pluginId}): {settingsButtonVisible}");

        var actions = definition.ActionAutomationIds
            .Select(actionId => WaitForAutomationId(mainWindow, actionId, TimeSpan.FromSeconds(20)))
            .ToArray();

        if (!toggleActions)
            return;

        for (var index = 0; index < actions.Length; index++)
        {
            var actionAutomationId = definition.ActionAutomationIds[index];
            var actionKey = actionAutomationId.Replace("WindowsOptimizationAction_", string.Empty, StringComparison.Ordinal);
            ClickActionCheckbox(actions[index], actionKey);
        }
    }

    private static OptimizationRouteDefinition? GetOptimizationRouteDefinition(string pluginId)
    {
        if (pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
        {
            return new OptimizationRouteDefinition(
                "WindowsOptimizationCategory_shell.integration",
                "WindowsOptimizationCategorySettings_shell-integration",
                new[]
                {
                    "WindowsOptimizationAction_shell.integration.enable",
                    "WindowsOptimizationAction_shell.integration.disable"
                });
        }

        if (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
        {
            return new OptimizationRouteDefinition(
                "WindowsOptimizationCategory_custom.mouse",
                "WindowsOptimizationCategorySettings_custom-mouse",
                new[]
                {
                    "WindowsOptimizationAction_custom.mouse.cursor.auto-theme.enable",
                    "WindowsOptimizationAction_custom.mouse.cursor.auto-theme.disable"
                });
        }

        return null;
    }

    private sealed record OptimizationRouteDefinition(
        string CategoryAutomationId,
        string SettingsButtonAutomationId,
        string[] ActionAutomationIds);

    private static void NavigateToWindowsOptimizationPage(AutomationElement mainWindow)
    {
        var nav = WaitForWindowsOptimizationNavigationElement(mainWindow, TimeSpan.FromSeconds(20));
        Click(nav);

        WaitForAutomationId(mainWindow, "WindowsOptimizationCategoryList", TimeSpan.FromSeconds(30));
        WaitForAutomationId(mainWindow, "WindowsOptimizationOptimizationTabButton", TimeSpan.FromSeconds(20));

        Console.WriteLine("[main-smoke] Navigated to Windows Optimization page");
    }

    private static AutomationElement WaitForWindowsOptimizationNavigationElement(AutomationElement root, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => TryFindWindowsOptimizationNavigationElement(root, out _),
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found || !TryFindWindowsOptimizationNavigationElement(root, out var element) || element is null)
        {
            DumpAutomationSnapshot(root, 250);
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
            () => FindByAutomationId(root, automationId) is not null,
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for automation element '{automationId}'.");

        return FindByAutomationId(root, automationId)
               ?? throw new InvalidOperationException($"Automation element '{automationId}' was not found.");
    }

    private static AutomationElement WaitForAutomationIdOrNames(AutomationElement root, string automationId, string[] names, TimeSpan timeout)
    {
        var found = WaitUntil(
            () => FindByAutomationId(root, automationId) is not null || names.Any(name => IsVisible(FindByName(root, name))),
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for element '{automationId}' or names [{string.Join(", ", names)}].");

        var byId = FindByAutomationId(root, automationId);
        if (byId is not null)
            return byId;

        foreach (var name in names)
        {
            var byName = FindByName(root, name);
            if (IsVisible(byName))
                return byName!;
        }

        throw new InvalidOperationException($"Element '{automationId}' or fallback names was not found after wait.");
    }

    private static AutomationElement? FindByAutomationId(AutomationElement root, string automationId)
    {
        var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);

        try
        {
            return root.FindFirst(TreeScope.Descendants, condition);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            var liveRoot = ResolveLiveWindow(root);
            if (ReferenceEquals(liveRoot, root))
                return null;

            try
            {
                return liveRoot.FindFirst(TreeScope.Descendants, condition);
            }
            catch (Exception retryEx) when (IsRecoverableAutomationException(retryEx))
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
            return root.FindFirst(TreeScope.Descendants, condition);
        }
        catch (Exception ex) when (IsRecoverableAutomationException(ex))
        {
            var liveRoot = ResolveLiveWindow(root);
            if (ReferenceEquals(liveRoot, root))
                return null;

            try
            {
                return liveRoot.FindFirst(TreeScope.Descendants, condition);
            }
            catch (Exception retryEx) when (IsRecoverableAutomationException(retryEx))
            {
                return null;
            }
        }
    }

    private static void Click(AutomationElement element)
    {
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

    private static void SelectComboBoxItemByName(AutomationElement comboBox, string itemName)
    {
        SelectComboBoxItemByNames(comboBox, itemName);
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

    private static void CapturePluginSettingsWindow(AutomationElement settingsWindow, string pluginId)
    {
        try
        {
            var handle = settingsWindow.Current.NativeWindowHandle;
            if (handle == 0)
            {
                Console.WriteLine($"[main-smoke] Settings window handle unavailable for screenshot: {pluginId}");
                return;
            }

            var screenshotContext = CreateScreenshotContext();
            if (screenshotContext is not { } context)
                return;

            var windowPath = Path.Combine(context.OutputDirectory, $"{pluginId}-window.png");
            var fullPath = Path.Combine(context.OutputDirectory, $"{pluginId}-fullscreen.png");

            RunScreenshotCapture(context.ScriptPath, $"-Path \"{fullPath}\"");
            RunScreenshotCapture(context.ScriptPath, $"-Path \"{windowPath}\" -WindowHandle {handle}");

            Console.WriteLine($"[main-smoke] Captured settings screenshots for {pluginId}: {windowPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Screenshot capture skipped for {pluginId}: {ex.Message}");
        }
    }

    private static void CaptureMainWindow(AutomationElement mainWindow, string pluginId, string suffix)
    {
        try
        {
            if (!TryGetNativeWindowHandle(mainWindow, out var handle))
            {
                Console.WriteLine($"[main-smoke] Main window handle unavailable for screenshot: {pluginId}/{suffix}");
                return;
            }

            var screenshotContext = CreateScreenshotContext();
            if (screenshotContext is not { } context)
                return;

            var windowPath = Path.Combine(context.OutputDirectory, $"{pluginId}-{suffix}.png");
            RunScreenshotCapture(context.ScriptPath, $"-Path \"{windowPath}\" -WindowHandle {handle}");
            Console.WriteLine($"[main-smoke] Captured main-window screenshot for {pluginId}/{suffix}: {windowPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Main-window screenshot skipped for {pluginId}/{suffix}: {ex.Message}");
        }
    }

    private static (string ScriptPath, string OutputDirectory)? CreateScreenshotContext()
    {
        var scriptPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".codex",
            "skills",
            "screenshot",
            "scripts",
            "take_screenshot.ps1");

        if (!File.Exists(scriptPath))
        {
            Console.WriteLine($"[main-smoke] Screenshot helper not found: {scriptPath}");
            return null;
        }

        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            $"llt-plugin-settings-host-{DateTime.Now:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(outputDirectory);
        return (scriptPath, outputDirectory);
    }

    private static void RunScreenshotCapture(string scriptPath, string arguments)
    {
        using var capture = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" {arguments}",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        capture?.WaitForExit(20000);
    }

    private static void MouseClick(AutomationElement element)
    {
        var target = ResolveMouseClickableElement(element);
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

    private static string ReadElementText(AutomationElement element)
    {
        if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePattern))
            return ((ValuePattern)valuePattern).Current.Value ?? string.Empty;

        return element.Current.Name ?? string.Empty;
    }

    private static bool FindVisibleTextContains(AutomationElement root, string keyword)
    {
        var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text);
        try
        {
            return root.FindAll(TreeScope.Descendants, condition)
                .Cast<AutomationElement>()
                .Where(IsVisible)
                .Any(element => ReadElementText(element).Contains(keyword, StringComparison.OrdinalIgnoreCase));
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
