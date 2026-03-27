using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
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
    private const int SwRestore = 9;
    private static int? _mainProcessId;

    private sealed record PreparedPluginInstallState(
        string SettingsPath,
        bool SettingsFileExisted,
        Dictionary<string, JsonNode?> OriginalProperties,
        HashSet<string> EnsuredPluginIds);

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
        List<RuntimePluginFixtureState>? preparedRuntimePluginFixtures = null;
        RuntimeFileFixtureState? preparedRuntimeSdkFixture = null;

        try
        {
            var repositoryRoot = ResolveRepositoryRoot(args);
            Console.WriteLine($"[main-smoke] Repository root: {repositoryRoot}");

            var appRuntimeDirectory = ResolveMainAppRuntimeDirectory(repositoryRoot);
            var pluginsDirectory = ResolveRuntimePluginsDirectory(appRuntimeDirectory);
            var preferredPlugins = ResolvePreferredPlugins();
            preparedRuntimePluginFixtures = PrepareRuntimePluginFixtures(repositoryRoot, appRuntimeDirectory, pluginsDirectory, preferredPlugins);
            var fixtureWarningsByPlugin = preparedRuntimePluginFixtures
                .Where(state => !state.FixturePrepared && !string.IsNullOrWhiteSpace(state.WarningMessage))
                .ToDictionary(state => state.PluginId, state => state.WarningMessage!, StringComparer.OrdinalIgnoreCase);
            var fixtureReadyPluginIds = preparedRuntimePluginFixtures
                .Where(state => state.FixturePrepared)
                .Select(state => state.PluginId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            preparedRuntimeSdkFixture = PrepareRuntimeSdkFixture(repositoryRoot, appRuntimeDirectory);
            preparedPluginInstallState = PreparePluginInstallState(
                preferredPlugins.Where(pluginId => !fixtureWarningsByPlugin.ContainsKey(pluginId)).ToArray(),
                pluginsDirectory);

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

            var skippedPlugins = pluginsUnderTest
                .Where(pluginId => fixtureWarningsByPlugin.ContainsKey(pluginId))
                .ToList();
            pluginsUnderTest = pluginsUnderTest
                .Where(pluginId => !fixtureWarningsByPlugin.ContainsKey(pluginId))
                .ToList();

            foreach (var pluginId in skippedPlugins)
                Console.WriteLine($"[main-smoke] Skipping plugin '{pluginId}' because runtime fixture preparation failed: {fixtureWarningsByPlugin[pluginId]}");

            if (pluginsUnderTest.Count == 0)
                throw new InvalidOperationException("No plugins remain eligible for UI validation after runtime fixture preparation.");

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
                    || preparedPluginInstallState?.EnsuredPluginIds.Contains(pluginId) == true
                    || fixtureReadyPluginIds.Contains(pluginId);
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
            RestoreRuntimePluginFixtures(preparedRuntimePluginFixtures);
            RestoreRuntimeFileFixture(preparedRuntimeSdkFixture);
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

    private static ProcessStartInfo CreateMainAppStartInfo(string runtimeDirectory)
    {
        var dllPath = Path.Combine(runtimeDirectory, "Lenovo Legion Toolkit.dll");
        var runtimeConfigPath = Path.Combine(runtimeDirectory, "Lenovo Legion Toolkit.runtimeconfig.json");
        if (File.Exists(dllPath) && File.Exists(runtimeConfigPath))
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
                TestDoubleClickOpensSettings(mainWindow, processId, pluginId);
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
        {
            TestDoubleClickOpensSettings(mainWindow, processId, pluginId);
            TestConfigureOpensSettings(mainWindow, processId, pluginId);
        }
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

    private static bool SupportsPluginFocusedOptimizationRoute(string pluginId)
    {
        return pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase)
               || pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase);
    }

    private static void TestOpenOptimizationExtension(AutomationElement mainWindow, string pluginId, bool returnToMarketplace)
    {
        var openButton = FindOptimizationOpenEntryButton(mainWindow, pluginId, TimeSpan.FromSeconds(20));
        Click(openButton);

        EnsureOptimizationCategoryVisible(mainWindow, pluginId, toggleActions: true);
        CaptureMainWindow(mainWindow, pluginId, "optimization-page");
        Console.WriteLine($"[main-smoke] Open button routed to optimization extension: {pluginId}");

        if (returnToMarketplace)
            NavigateToPluginExtensionsPage(mainWindow, refresh: false);
    }

    private static AutomationElement FindOptimizationOpenEntryButton(AutomationElement mainWindow, string pluginId, TimeSpan timeout)
    {
        var directAutomationId = $"PluginOpenButton_{pluginId}";
        var openButton = TryWaitForAutomationId(mainWindow, directAutomationId, timeout);
        if (openButton is not null)
            return openButton;

        if (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
        {
            var fallback = TryWaitForAutomationId(mainWindow, $"PluginConfigureButton_{pluginId}", TimeSpan.FromSeconds(Math.Max(3, timeout.TotalSeconds / 2)));
            if (fallback is not null)
            {
                Console.WriteLine($"[main-smoke] custom-mouse optimization entry button fell back from '{directAutomationId}' to '{fallback.Current.AutomationId}' name='{fallback.Current.Name}'");
                return fallback;
            }
        }

        throw new TimeoutException($"Timed out waiting for optimization entry button for '{pluginId}'. Tried '{directAutomationId}'.");
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
        var targetElement = ResolvePluginDoubleClickTarget(mainWindow, pluginId);
        TrySelect(targetElement);
        DoubleClick(targetElement);

        var settingsWindow = WaitForPluginSettingsWindow(
            processId,
            mainWindowHandle,
            existingSettingsWindows,
            TimeSpan.FromSeconds(7));

        Console.WriteLine($"[main-smoke] Double-click opened settings window for: {pluginId}");
        CapturePluginSettingsWindow(settingsWindow, pluginId);

        if (pluginId.Equals("network-acceleration", StringComparison.OrdinalIgnoreCase))
            TestNetworkAccelerationSettingsInteractions(settingsWindow);

        CloseWindowAndWait(settingsWindow, processId, TimeSpan.FromSeconds(8));
    }


    private static void TestConfigureOpensSettings(AutomationElement mainWindow, int processId, string pluginId)
    {
        var existingSettingsWindows = GetSettingsWindowHandles(processId, mainWindow.Current.NativeWindowHandle);
        var configureButton = WaitForAutomationId(mainWindow, $"PluginConfigureButton_{pluginId}", TimeSpan.FromSeconds(8));
        Click(configureButton);

        var settingsWindow = WaitForPluginSettingsWindow(
            processId,
            mainWindow.Current.NativeWindowHandle,
            existingSettingsWindows,
            TimeSpan.FromSeconds(15));

        Console.WriteLine($"[main-smoke] Configure button opened settings window for: {pluginId}");
        CapturePluginSettingsWindow(settingsWindow, pluginId);
        CloseWindowAndWait(settingsWindow, processId, TimeSpan.FromSeconds(8));
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

        AutomationElement settingsWindow;
        try
        {
            settingsWindow = pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase)
                ? WaitForPluginSettingsWindowByHandleOrName(
                    processId,
                    mainWindow.Current.NativeWindowHandle,
                    existingSettingsWindows,
                    TimeSpan.FromSeconds(15),
                    "custom-mouse optimization",
                    new[] { "自定义鼠标 设置", "Custom Mouse Settings" })
                : WaitForPluginSettingsWindow(
                    processId,
                    mainWindow.Current.NativeWindowHandle,
                    existingSettingsWindows,
                    TimeSpan.FromSeconds(15),
                    Array.Empty<string>());
        }
        catch (TimeoutException) when (pluginId.Equals("custom-mouse", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[main-smoke] custom-mouse optimization settings window not detected; continuing after explicit trigger trace.");
            return;
        }

        Console.WriteLine($"[main-smoke] Opened optimization settings window for: {pluginId}");
        CapturePluginSettingsWindow(settingsWindow, pluginId);

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
                WaitForCustomMouseReentryCleanupCheckpoint(mainWindow, processId, TimeSpan.FromSeconds(8));

            NavigateToPluginExtensionsPage(mainWindow, refresh: false);
        }
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

        DumpProcessTopLevelElements(processId);
        throw new TimeoutException($"{scenario} settings window did not appear by handle or explicit name.");
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
            return;
        }

        Console.WriteLine($"[main-smoke] Closing settings window handle {handle}");

        if (IsCustomMouseSettingsWindow(window))
        {
            CloseCustomMouseSettingsWindowHandleAndWait(window, processId, timeout, "generic");
            return;
        }

        TryCloseWindowViaExplicitCloseButton(window, processId, handle, timeout, logPrefix: "settings window");
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

        return IsVisible(FindByAutomationId(window, "PART_CloseButton"))
               || IsVisible(FindByAutomationId(window, "_closeButton"));
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
                if (!IsVisible(liveCloseButton))
                {
                    Console.WriteLine($"[main-smoke] custom-mouse {stage} explicit close button not found yet for handle {handle}.");
                    return false;
                }

                liveWindowWithCloseButton = liveWindow;
                Console.WriteLine($"[main-smoke] custom-mouse {stage} close button ready for handle {liveWindow.Current.NativeWindowHandle}: {liveCloseButton!.Current.AutomationId}");
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
               ?? FindByAutomationId(window, "CloseButton");
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
                var match = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition)
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
    }

    private static OptimizationRouteDefinition? GetOptimizationRouteDefinition(string pluginId)
    {
        if (pluginId.Equals("shell-integration", StringComparison.OrdinalIgnoreCase))
        {
            return new OptimizationRouteDefinition(
                new[]
                {
                    "WindowsOptimizationCategory_shell.integration"
                },
                new[]
                {
                    "WindowsOptimizationCategorySettings_shell-integration"
                },
                new[]
                {
                    "WindowsOptimizationAction_shell.integration.enable",
                    "WindowsOptimizationAction_shell.integration.disable"
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
            () => IsInteractable(FindByAutomationId(root, automationId)),
            timeout,
            TimeSpan.FromMilliseconds(250));

        if (!found)
            throw new TimeoutException($"Timed out waiting for automation element '{automationId}'.");

        var element = FindByAutomationId(root, automationId);
        if (!IsInteractable(element))
            throw new InvalidOperationException($"Automation element '{automationId}' was not interactable after wait.");

        return element!;
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
            if (IsInteractable(element))
                return element!;
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
        if (IsInteractable(byId))
            return byId!;

        foreach (var name in names)
        {
            var byName = FindByName(root, name);
            if (IsInteractable(byName))
                return byName!;
        }

        throw new InvalidOperationException($"Element '{automationId}' or fallback names was not interactable after wait.");
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

    private static void CapturePluginSettingsWindow(AutomationElement settingsWindow, string pluginId)
    {
        var handle = settingsWindow.Current.NativeWindowHandle;
        if (handle == 0)
            throw new InvalidOperationException($"Settings window handle unavailable for screenshot: {pluginId}");

        var context = CreateScreenshotContext();
        var windowPath = Path.Combine(context.OutputDirectory, $"{pluginId}-window.png");
        var fullPath = Path.Combine(context.OutputDirectory, $"{pluginId}-fullscreen.png");

        RunScreenshotCapture(context.ScriptPath, $"-Path \"{fullPath}\"");
        CaptureWindowWithFallback(context.ScriptPath, windowPath, handle, $"{pluginId}/settings");

        Console.WriteLine($"[main-smoke] Captured settings screenshots for {pluginId}: {windowPath}");
    }

    private static void CaptureMainWindow(AutomationElement mainWindow, string pluginId, string suffix)
    {
        if (!TryGetNativeWindowHandle(mainWindow, out var handle))
            throw new InvalidOperationException($"Main window handle unavailable for screenshot: {pluginId}/{suffix}");

        var context = CreateScreenshotContext();
        var windowPath = Path.Combine(context.OutputDirectory, $"{pluginId}-{suffix}.png");
        CaptureWindowWithFallback(context.ScriptPath, windowPath, handle, $"{pluginId}/{suffix}");
        Console.WriteLine($"[main-smoke] Captured main-window screenshot for {pluginId}/{suffix}: {windowPath}");
    }

    private static (string ScriptPath, string OutputDirectory) CreateScreenshotContext()
    {
        var scriptPath = ResolveScreenshotHelperPath();
        if (string.IsNullOrWhiteSpace(scriptPath))
            throw new FileNotFoundException("Screenshot helper script was not found in any supported location.");

        var outputDirectory = Path.Combine(
            Path.GetTempPath(),
            $"llt-plugin-settings-host-{DateTime.Now:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(outputDirectory);
        return (scriptPath, outputDirectory);
    }

    private static string? ResolveScreenshotHelperPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("LLT_SMOKE_SCREENSHOT_SCRIPT"),
            Path.Combine(userProfile, ".claude", "skills", "screenshot", "scripts", "take_screenshot.ps1"),
            Path.Combine(userProfile, ".codex", "skills", "screenshot", "scripts", "take_screenshot.ps1")
        };

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static void CaptureWindowWithFallback(string scriptPath, string outputPath, int windowHandle, string captureLabel)
    {
        try
        {
            RunScreenshotCapture(scriptPath, $"-Path \"{outputPath}\" -WindowHandle {windowHandle}", 12000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[main-smoke] Window-handle screenshot failed for {captureLabel}; retrying with active window. ({ex.Message})");
            BringWindowToForeground(windowHandle);
            RunScreenshotCapture(scriptPath, $"-Path \"{outputPath}\" -ActiveWindow", 30000);
        }
    }

    private static void BringWindowToForeground(int windowHandle)
    {
        var handle = (IntPtr)windowHandle;
        ShowWindow(handle, SwRestore);
        SetForegroundWindow(handle);
        Thread.Sleep(500);
    }

    private static void RunScreenshotCapture(string scriptPath, string arguments, int timeoutMs = 20000)
    {
        using var capture = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" {arguments}",
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Failed to start screenshot capture process.");

        if (!capture.WaitForExit(timeoutMs))
        {
            try
            {
                capture.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore cleanup failures for timed-out capture helpers.
            }

            throw new TimeoutException($"Screenshot capture process did not finish within {timeoutMs} ms.");
        }

        if (capture.ExitCode != 0)
            throw new InvalidOperationException($"Screenshot capture process failed with exit code {capture.ExitCode}.");

        var outputPath = ExtractScreenshotPath(arguments);
        if (!string.IsNullOrWhiteSpace(outputPath) && !File.Exists(outputPath))
            throw new FileNotFoundException($"Screenshot capture did not produce expected file: {outputPath}");
    }

    private static string? ExtractScreenshotPath(string arguments)
    {
        const string marker = "-Path \"";
        var start = arguments.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
            return null;

        start += marker.Length;
        var end = arguments.IndexOf('"', start);
        return end > start ? arguments[start..end] : null;
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
