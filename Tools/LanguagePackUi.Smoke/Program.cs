using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Automation;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.ResourcesCatalog;
using LenovoLegionToolkit.Lib.Utils;
using UniversalDeviceToolkit.WPF.Utils;

const string pass = "PASS";
const string fail = "FAIL";
const string StableCatalogUrl = "https://ssc-studio.github.io/UniversalDeviceToolkit/resources/stable/catalog.json";
const string DefaultLocalCatalogUrl = "http://127.0.0.1:18765/catalog.json";

var options = SmokeOptions.Parse(args);
var repositoryRoot = ResolveRepositoryRoot();
var runtimeDirectory = ResolveRuntimeDirectory(repositoryRoot);
var artifactRoot = Path.Combine(Path.GetTempPath(), $"udt-lang-ui-smoke-{DateTime.Now:yyyyMMdd-HHmmss}");
var sandboxRoot = Path.Combine(artifactRoot, "sandbox");
var appDataDirectory = Path.Combine(sandboxRoot, "appdata");
Directory.CreateDirectory(appDataDirectory);

var version = GetAppVersion(runtimeDirectory);
var targetCulture = options.Culture;
var cultureDirectory = targetCulture.Split('-')[0];
var uiMarkers = GetUiMarkersForCulture(targetCulture);

var effectiveCatalogUrl = ResolveCatalogUrl(options);
var modeLabel = effectiveCatalogUrl is null
    ? "online (default catalog)"
    : $"local catalog ({effectiveCatalogUrl})";
Console.WriteLine($"[lang-ui-smoke] Mode: {modeLabel}, culture: {targetCulture}");
if (effectiveCatalogUrl is not null)
    Console.WriteLine("[lang-ui-smoke] Start mock server first: Tools/LanguagePackMockBackend/Start-MockCatalogServer.ps1");

RemoveAppLanguageSatellite(runtimeDirectory, cultureDirectory);

await PreflightOnlineCatalogAsync(version, targetCulture, effectiveCatalogUrl, options.SkipPreflight);

if (options.BackendOnly)
{
    var backendFailures = await RunBackendOnlyInstallAsync(
        runtimeDirectory,
        appDataDirectory,
        effectiveCatalogUrl ?? DefaultLocalCatalogUrl,
        targetCulture,
        cultureDirectory);
    return backendFailures == 0 ? 0 : 1;
}

File.WriteAllText(Path.Combine(appDataDirectory, "lang"), "en");
await File.WriteAllTextAsync(
    Path.Combine(appDataDirectory, "settings.json"),
    """
    {
      "Theme": 0,
      "TemperatureUnit": 0,
      "ThemeStylePreset": 0,
      "AccentColorSource": 0,
      "MinimizeToTray": false,
      "MinimizeOnClose": false,
      "DisableUnsupportedHardwareWarning": true,
      "ForceSoftwareRendering": true,
      "ExtensionsEnabled": false,
      "CheckPluginUpdatesOnStartup": false
    }
    """);

Process? process = null;
var failures = 0;
var useOnlineLikeTimeouts = effectiveCatalogUrl is not null || options.UseOnlineCatalog;
var installActivityTimeout = useOnlineLikeTimeouts ? TimeSpan.FromMinutes(3) : TimeSpan.FromSeconds(30);
var installFilesTimeout = useOnlineLikeTimeouts ? TimeSpan.FromMinutes(5) : TimeSpan.FromSeconds(90);
var localizedUiTimeout = useOnlineLikeTimeouts ? TimeSpan.FromMinutes(2) : TimeSpan.FromSeconds(60);

try
{
    var startInfo = CreateAppStartInfo(runtimeDirectory, appDataDirectory, sandboxRoot);
    if (effectiveCatalogUrl is not null)
    {
        startInfo.EnvironmentVariables["UDT_RESOURCE_CATALOG_URL"] = effectiveCatalogUrl;
        startInfo.EnvironmentVariables["LLT_RESOURCE_CATALOG_URL"] = effectiveCatalogUrl;
        Console.WriteLine($"[lang-ui-smoke] Catalog override: {effectiveCatalogUrl}");
    }
    else
    {
        Console.WriteLine($"[lang-ui-smoke] Using default online catalog: {StableCatalogUrl}");
    }

    startInfo.EnvironmentVariables["UDT_APPDATA_OVERRIDE"] = appDataDirectory;
    startInfo.EnvironmentVariables["LLT_APPDATA_OVERRIDE"] = appDataDirectory;
    startInfo.EnvironmentVariables["UDT_SINGLE_INSTANCE_KEY"] = Path.GetFileName(sandboxRoot);
    startInfo.EnvironmentVariables["LLT_SINGLE_INSTANCE_KEY"] = Path.GetFileName(sandboxRoot);
    startInfo.EnvironmentVariables["UDT_SMOKE_AUTOMATION"] = "1";
    startInfo.EnvironmentVariables["LLT_SMOKE_AUTOMATION"] = "1";

    Console.WriteLine($"[lang-ui-smoke] Launching: {startInfo.FileName} {startInfo.Arguments}");
    process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start app.");
    process = WaitForAppProcess(process, TimeSpan.FromSeconds(30));
    try
    {
        process.WaitForInputIdle(20000);
    }
    catch (InvalidOperationException)
    {
        Thread.Sleep(3000);
    }

    if (process.HasExited)
        throw new InvalidOperationException($"App exited early with code {process.ExitCode}.");

    var mainWindow = WaitForMainWindow(process, appDataDirectory, TimeSpan.FromSeconds(180));
    Console.WriteLine("[lang-ui-smoke] Main window ready");

    DismissCompatibilityDialog(mainWindow);
    ClickSettingsNavigation(mainWindow);

    mainWindow = RefreshWindow(mainWindow);
    if (!WaitUntil(
            () => SettingsAppearancePageIsReady(RefreshWindow(mainWindow)),
            TimeSpan.FromSeconds(40),
            "settings appearance page"))
        throw new TimeoutException("Settings appearance page did not load.");

    mainWindow = RefreshWindow(mainWindow);
    var installButton = SelectTargetLanguageAndGetInstallButton(mainWindow, targetCulture, uiMarkers.ComboMarkers, TimeSpan.FromSeconds(40));
    ActivateElement(installButton);
    Console.WriteLine("[lang-ui-smoke] Clicked install language");

    var cultureInstallDirectory = Path.Combine(runtimeDirectory, cultureDirectory);
    var sawInstallUi = WaitForLanguageInstallActivity(mainWindow, cultureInstallDirectory, installActivityTimeout);
    if (!sawInstallUi)
    {
        Console.WriteLine($"{fail}: Install UI (panel/progress) or early file copy was not observed.");
        failures++;
    }
    else
    {
        Console.WriteLine($"{pass}: Language install progress UI or install activity was observed.");
    }

    if (!WaitUntil(
            () => Directory.Exists(cultureInstallDirectory) &&
                  Directory.EnumerateFiles(cultureInstallDirectory, "Universal Device Toolkit.resources.dll").Any(),
            installFilesTimeout,
            $"{targetCulture} language pack files"))
    {
        Console.WriteLine($"{fail}: Language files were not installed under '{cultureInstallDirectory}'.");
        failures++;
    }
    else
    {
        Console.WriteLine($"{pass}: App satellite resources.dll present for '{targetCulture}'.");
    }

    var langPath = Path.Combine(appDataDirectory, "lang");
    if (!WaitUntil(
            () => File.Exists(langPath) &&
                  string.Equals(File.ReadAllText(langPath).Trim(), targetCulture, StringComparison.OrdinalIgnoreCase),
            TimeSpan.FromSeconds(60),
            $"lang file updated to {targetCulture}"))
    {
        var actual = File.Exists(langPath) ? File.ReadAllText(langPath).Trim() : "<missing>";
        Console.WriteLine($"{fail}: Expected lang file '{targetCulture}', got '{actual}'.");
        failures++;
    }
    else
    {
        Console.WriteLine($"{pass}: Sandbox lang file set to {targetCulture}.");
    }

    if (!WaitForLocalizedUi(process, uiMarkers.UiStrings, localizedUiTimeout))
    {
        Console.WriteLine($"{fail}: Main window did not show expected localized UI strings after install.");
        failures++;
    }
    else
    {
        Console.WriteLine($"{pass}: Main window shows localized strings ({string.Join(", ", uiMarkers.UiStrings)}).");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"{fail}: {ex.Message}");
    failures++;
}
finally
{
    try
    {
        if (process is { HasExited: false })
            process.Kill(entireProcessTree: true);
    }
    catch
    {
        // best-effort
    }

}

Console.WriteLine();
Console.WriteLine(failures == 0
    ? $"{pass}: Language pack UI smoke test passed. Artifacts: {artifactRoot}"
    : $"{fail}: Language pack UI smoke test failed ({failures} checks). Artifacts: {artifactRoot}");

return failures == 0 ? 0 : 1;

static string ResolveRepositoryRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "UniversalDeviceToolkit.sln")))
            return dir.FullName;
        dir = dir.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate repository root.");
}

static string ResolveRuntimeDirectory(string repositoryRoot)
{
    foreach (var configuration in new[] { "Debug", "Release" })
    {
        var root = Path.Combine(repositoryRoot, "UniversalDeviceToolkit.WPF", "bin", configuration);
        if (!Directory.Exists(root))
            continue;

        var runtimeDirectory = Directory
            .EnumerateFiles(root, "Universal Device Toolkit.exe", SearchOption.AllDirectories)
            .Select(Path.GetDirectoryName)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .OrderByDescending(Directory.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(runtimeDirectory))
            return runtimeDirectory;
    }

    throw new DirectoryNotFoundException("Build the WPF app first (Debug or Release).");
}

static void RemoveAppLanguageSatellite(string runtimeDirectory, string cultureDirectoryName)
{
    var satellitePath = Path.Combine(runtimeDirectory, cultureDirectoryName, "Universal Device Toolkit.resources.dll");
    if (!File.Exists(satellitePath))
        return;

    try
    {
        File.Delete(satellitePath);
        Console.WriteLine($"[lang-ui-smoke] Removed existing app satellite '{satellitePath}' for a clean install test.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[lang-ui-smoke] Warning: could not remove '{satellitePath}': {ex.Message}");
    }
}

static string GetAppVersion(string runtimeDirectory)
{
    var exePath = Path.Combine(runtimeDirectory, "Universal Device Toolkit.exe");
    if (File.Exists(exePath))
    {
        var info = FileVersionInfo.GetVersionInfo(exePath);
        if (info.FileMajorPart >= 0 && info.FileMinorPart >= 0 && info.FileBuildPart >= 0)
            return $"{info.FileMajorPart}.{info.FileMinorPart}.{info.FileBuildPart}";
    }

    return "3.8.1";
}

static ProcessStartInfo CreateAppStartInfo(string runtimeDirectory, string appDataDirectory, string sandboxRoot)
{
    var arguments =
        $"--skip-compat-check --disable-update-checker --disable-conflicting-software-warning --trace --single-instance-key={Path.GetFileName(sandboxRoot)}";

    var dllPath = Path.Combine(runtimeDirectory, "Universal Device Toolkit.dll");
    var runtimeConfigPath = Path.Combine(runtimeDirectory, "Universal Device Toolkit.runtimeconfig.json");
    if (File.Exists(dllPath) && File.Exists(runtimeConfigPath))
    {
        return new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{dllPath}\" {arguments}",
            WorkingDirectory = runtimeDirectory,
            UseShellExecute = false
        };
    }

    var exePath = Path.Combine(runtimeDirectory, "Universal Device Toolkit.exe");
    if (File.Exists(exePath))
    {
        return new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            WorkingDirectory = runtimeDirectory,
            UseShellExecute = false
        };
    }

    throw new FileNotFoundException($"Main app startup files not found in: {runtimeDirectory}");
}

static string? ResolveCatalogUrl(SmokeOptions options)
{
    if (!string.IsNullOrWhiteSpace(options.CatalogUrl))
        return options.CatalogUrl;

    if (!options.UseOnlineCatalog || options.BackendOnly)
        return DefaultLocalCatalogUrl;

    return null;
}

static async Task PreflightOnlineCatalogAsync(string appVersion, string culture, string? catalogUrlOverride, bool skipOnFailure)
{
    var catalogUrl = string.IsNullOrWhiteSpace(catalogUrlOverride) ? StableCatalogUrl : catalogUrlOverride;
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };

    string json;
    try
    {
        Console.WriteLine($"[lang-ui-smoke] Preflight GET {catalogUrl}");
        json = await GetStringWithRetryAsync(http, catalogUrl, maxAttempts: 3);
    }
    catch (Exception ex) when (skipOnFailure)
    {
        Console.WriteLine($"[lang-ui-smoke] Warning: preflight skipped after failure: {ex.Message}");
        return;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            $"Catalog preflight failed. Ensure the catalog is reachable at {catalogUrl}. Inner: {ex.Message}",
            ex);
    }
    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;

    var catalogVersion = root.GetProperty("appVersion").GetString();
    if (!string.Equals(catalogVersion, appVersion, StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"[lang-ui-smoke] Warning: build version is {appVersion}, catalog appVersion is {catalogVersion}.");
    }

    var language = root.GetProperty("languages").EnumerateArray()
        .FirstOrDefault(entry =>
        {
            var cultureName = entry.GetProperty("culture").GetString();
            return cultureName is not null &&
                   (cultureName.Equals(culture, StringComparison.OrdinalIgnoreCase) ||
                    cultureName.Equals(culture.Split('-')[0], StringComparison.OrdinalIgnoreCase));
        });

    if (language.ValueKind == JsonValueKind.Undefined)
        throw new InvalidOperationException($"Culture '{culture}' was not found in the online catalog.");

    var downloadUrl = language.GetProperty("url").GetString();
    var size = language.TryGetProperty("size", out var sizeProperty) ? sizeProperty.GetInt64() : -1;
    Console.WriteLine($"[lang-ui-smoke] Online language pack: {downloadUrl} ({size:N0} bytes)");
}

static async Task<int> RunBackendOnlyInstallAsync(
    string runtimeDirectory,
    string appDataDirectory,
    string catalogUrl,
    string targetCulture,
    string cultureDirectory)
{
    var failures = 0;
    Environment.SetEnvironmentVariable(OnlineResourceCatalogClient.CatalogUrlEnvironmentVariable, catalogUrl);
    Environment.SetEnvironmentVariable("LLT_RESOURCE_CATALOG_URL", catalogUrl);
    Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, appDataDirectory);
    Environment.SetEnvironmentVariable(Folders.LegacyAppDataOverrideEnvironmentVariable, appDataDirectory);

    var previousDirectory = Directory.GetCurrentDirectory();
    try
    {
        Directory.SetCurrentDirectory(runtimeDirectory);
        var manager = new LanguagePackManager(new OnlineResourceCatalogClient(new HttpClientFactory()));
        var culture = new CultureInfo(targetCulture);
        var progressValues = new List<float>();
        var progress = new Progress<float>(value =>
        {
            progressValues.Add(value);
            Console.WriteLine($"[lang-ui-smoke] Backend progress: {value:P0}");
        });

        await manager.InstallAsync(culture, progress);

        if (progressValues.Count == 0 || Math.Abs(progressValues[^1] - 1f) > 0.0001f)
        {
            Console.WriteLine($"{fail}: Backend install did not report progress ending at 100%.");
            failures++;
        }
        else
        {
            Console.WriteLine($"{pass}: Backend install progress reached 100%.");
        }

        // LanguagePackManager is compiled into this smoke exe; installs target AppContext.BaseDirectory, not the WPF output folder.
        var satellitePath = Path.Combine(AppContext.BaseDirectory, cultureDirectory, "Universal Device Toolkit.resources.dll");
        if (!File.Exists(satellitePath))
        {
            Console.WriteLine($"{fail}: App satellite not found at '{satellitePath}'.");
            failures++;
        }
        else
        {
            Console.WriteLine($"{pass}: App satellite installed ({new FileInfo(satellitePath).Length:N0} bytes).");
        }

        Console.WriteLine($"{pass}: Backend install completed for culture '{targetCulture}'.");
    }
    finally
    {
        Directory.SetCurrentDirectory(previousDirectory);
        Environment.SetEnvironmentVariable(OnlineResourceCatalogClient.CatalogUrlEnvironmentVariable, null);
        Environment.SetEnvironmentVariable("LLT_RESOURCE_CATALOG_URL", null);
        Environment.SetEnvironmentVariable(Folders.AppDataOverrideEnvironmentVariable, null);
        Environment.SetEnvironmentVariable(Folders.LegacyAppDataOverrideEnvironmentVariable, null);
    }

    Console.WriteLine(failures == 0
        ? $"{pass}: Backend-only local-catalog install test passed."
        : $"{fail}: Backend-only local-catalog install test failed ({failures} checks).");
    return failures;
}

static async Task<string> GetStringWithRetryAsync(HttpClient http, string url, int maxAttempts)
{
    Exception? lastError = null;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            return await http.GetStringAsync(url);
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            lastError = ex;
            Console.WriteLine($"[lang-ui-smoke] Preflight attempt {attempt} failed: {ex.Message}. Retrying...");
            await Task.Delay(TimeSpan.FromSeconds(2 * attempt));
        }
    }

    throw lastError ?? new InvalidOperationException("Preflight request failed.");
}

static bool WaitForLocalizedUi(Process process, string[] expectedStrings, TimeSpan timeout)
{
    return WaitUntil(
        () =>
        {
            if (process.HasExited)
                return false;

            var window = TryFindMainWindow(process) ?? TryFindMainShellWindowAnyProcess();
            if (window is null)
                return false;

            var liveWindow = RefreshWindow(window);
            return expectedStrings.Any(marker => FindVisibleTextContains(liveWindow, marker));
        },
        timeout,
        "localized UI strings");
}

static UiMarkers GetUiMarkersForCulture(string culture) =>
    culture.StartsWith("de", StringComparison.OrdinalIgnoreCase)
        ? new UiMarkers(["Deutsch", "German", "de"], ["Einstellungen"])
        : culture.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? new UiMarkers(["中文", "简体", "zh"], ["设置", "外观", "主题"])
            : new UiMarkers([culture], [culture]);

static AutomationElement SelectTargetLanguageAndGetInstallButton(
    AutomationElement mainWindow,
    string culture,
    string[] comboMarkers,
    TimeSpan timeout)
{
    var deadline = DateTime.UtcNow + timeout;
    Exception? lastError = null;

    while (DateTime.UtcNow < deadline)
    {
        try
        {
            mainWindow = RefreshWindow(mainWindow);
            var combo = FindDescendant(mainWindow, "LanguageComboBox")
                        ?? FindByName(mainWindow, "Language", ControlType.ComboBox)
                        ?? throw new InvalidOperationException("Language combo box was not found.");

            ExpandCombo(combo);
            Thread.Sleep(500);

            var item = FindLanguageListItem(combo, comboMarkers);
            if (item is null)
                throw new InvalidOperationException($"Language item for '{culture}' was not found in combo box.");

            ActivateElement(item);
            Thread.Sleep(800);

            var installButton = FindInstallLanguageButton(RefreshWindow(mainWindow));
            if (installButton is not null)
                return installButton;
        }
        catch (Exception ex)
        {
            lastError = ex;
        }

        Thread.Sleep(500);
    }

    throw new TimeoutException($"Install language button did not become available for '{culture}'.", lastError);
}

static AutomationElement? FindInstallLanguageButton(AutomationElement root) =>
    FindDescendant(root, "InstallLanguageButton")
    ?? FindByName(root, "Install language", ControlType.Button)
    ?? FindByName(root, "Sprache installieren", ControlType.Button);

static Process WaitForAppProcess(Process initialProcess, TimeSpan timeout)
{
    var deadline = DateTime.UtcNow + timeout;
    while (DateTime.UtcNow < deadline)
    {
        if (initialProcess.HasExited)
            break;

        var child = Process.GetProcessesByName("Universal Device Toolkit")
            .OrderByDescending(p => p.StartTime)
            .FirstOrDefault();
        if (child is not null && child.Id != initialProcess.Id)
        {
            Console.WriteLine($"[lang-ui-smoke] Resolved app process PID={child.Id} (launcher PID={initialProcess.Id}).");
            return child;
        }

        if (initialProcess.ProcessName.Contains("Universal", StringComparison.OrdinalIgnoreCase))
            return initialProcess;

        Thread.Sleep(500);
    }

    return initialProcess;
}

static AutomationElement WaitForMainWindow(Process process, string appDataDirectory, TimeSpan timeout)
{
    var deadline = DateTime.UtcNow + timeout;
    while (DateTime.UtcNow < deadline)
    {
        var window = TryFindMainWindow(process);
        if (window is not null)
        {
            Console.WriteLine("[lang-ui-smoke] Ready: main window");
            return window;
        }

        if (process.HasExited)
            break;

        Thread.Sleep(1000);
    }

    DumpProcessWindows(process.Id);
    DumpTopLevelShellWindows();
    DumpAllTopLevelWindows();
    DumpRecentLogs(appDataDirectory);
    throw new TimeoutException($"Main window did not appear. Process exited={process.HasExited}, exitCode={(process.HasExited ? process.ExitCode.ToString() : "n/a")}.");
}

static AutomationElement? TryFindMainWindow(Process process)
{
    if (!process.HasExited)
    {
        var window = TryFindMainWindowForProcessId(process.Id);
        if (window is not null)
            return window;
    }

    return TryFindMainShellWindowAnyProcess();
}

static AutomationElement? TryFindMainShellWindowAnyProcess()
{
    foreach (AutomationElement window in AutomationElement.RootElement.FindAll(
                 TreeScope.Children,
                 new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)))
    {
        if (TryDismissCompatibilityWindow(window))
            continue;

        if (TryDismissLanguageSelector(window))
            continue;

        if (HasMainShellMarkers(window) || WindowTitleLooksLikeMainShell(window))
            return window;
    }

    return null;
}

static bool TryDismissLanguageSelector(AutomationElement window)
{
    if (HasMainShellMarkers(window))
        return false;

    var title = window.Current.Name ?? string.Empty;
    if (!title.Contains("Universal Device Toolkit", StringComparison.OrdinalIgnoreCase) &&
        !title.Contains("Lenovo Legion Toolkit", StringComparison.OrdinalIgnoreCase))
        return false;

    var combo = FindByControlType(window, ControlType.ComboBox);
    var okButton = FindByName(window, "OK", ControlType.Button)
                   ?? FindByName(window, "确定", ControlType.Button);
    if (combo is null || okButton is null)
        return false;

    Console.WriteLine("[lang-ui-smoke] Dismissing startup language selector.");
    InvokeElement(okButton);
    Thread.Sleep(1500);
    return true;
}

static void DumpAllTopLevelWindows()
{
    Console.WriteLine("[lang-ui-smoke] All top-level windows:");
    foreach (AutomationElement window in AutomationElement.RootElement.FindAll(
                 TreeScope.Children,
                 new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)))
    {
        try
        {
            Console.WriteLine($"  - '{window.Current.Name}' pid={window.Current.ProcessId} class={window.Current.ClassName}");
        }
        catch
        {
            Console.WriteLine("  - <unavailable>");
        }
    }
}

static AutomationElement? TryFindMainWindowForProcessId(int processId)
{
    var condition = new AndCondition(
        new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

    var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, condition)
        .Cast<AutomationElement>()
        .ToArray();

    var candidates = new List<AutomationElement>();
    foreach (var window in windows)
    {
        if (TryDismissCompatibilityWindow(window))
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
        if (HasMainShellMarkers(window))
            return window;
    }

    return candidates.FirstOrDefault(window => WindowTitleLooksLikeMainShell(window));
}

static bool WindowTitleLooksLikeMainShell(AutomationElement window)
{
    var title = window.Current.Name ?? string.Empty;
    return title.Contains("Universal Device Toolkit", StringComparison.OrdinalIgnoreCase)
           || title.Contains("Lenovo Legion Toolkit", StringComparison.OrdinalIgnoreCase);
}

static bool HasMainShellMarkers(AutomationElement window) =>
    FindDescendant(window, "MainNavigationStore") is not null
    || FindDescendant(window, "MainRootFrame") is not null
    || FindDescendant(window, "_navigationStore") is not null;

static bool TryDismissCompatibilityWindow(AutomationElement window)
{
    var continueButton = FindDescendant(window, "_continueButton");
    if (continueButton is null || !IsVisible(continueButton) || !continueButton.Current.IsEnabled)
        return false;

    InvokeElement(continueButton);
    Thread.Sleep(800);
    return true;
}

static void DumpTopLevelShellWindows()
{
    Console.WriteLine("[lang-ui-smoke] Top-level windows with shell markers:");
    foreach (AutomationElement window in AutomationElement.RootElement.FindAll(
                 TreeScope.Children,
                 new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)))
    {
        try
        {
            if (!HasMainShellMarkers(window) && !WindowTitleLooksLikeMainShell(window))
                continue;

            Console.WriteLine($"  - '{window.Current.Name}' pid={window.Current.ProcessId} class={window.Current.ClassName}");
        }
        catch
        {
            Console.WriteLine("  - <unavailable>");
        }
    }
}

static void DumpRecentLogs(string appDataDirectory)
{
    var logsDirectory = Path.Combine(appDataDirectory, "logs");
    if (!Directory.Exists(logsDirectory))
    {
        Console.WriteLine($"[lang-ui-smoke] No logs directory at {logsDirectory}");
        return;
    }

    var latest = Directory.EnumerateFiles(logsDirectory, "*.txt")
        .OrderByDescending(File.GetLastWriteTimeUtc)
        .FirstOrDefault();

    if (latest is null)
    {
        Console.WriteLine("[lang-ui-smoke] No log files found.");
        return;
    }

    Console.WriteLine($"[lang-ui-smoke] Latest log: {latest}");
    var lines = File.ReadAllLines(latest);
    foreach (var line in lines.TakeLast(20))
        Console.WriteLine($"  {line}");
}

static void DumpProcessWindows(int processId)
{
    var condition = new AndCondition(
        new PropertyCondition(AutomationElement.ProcessIdProperty, processId),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));

    Console.WriteLine($"[lang-ui-smoke] Windows for PID {processId}:");
    foreach (AutomationElement window in AutomationElement.RootElement.FindAll(TreeScope.Children, condition))
    {
        try
        {
            Console.WriteLine($"  - '{window.Current.Name}' ({window.Current.ClassName})");
        }
        catch
        {
            Console.WriteLine("  - <unavailable>");
        }
    }
}

static void DismissCompatibilityDialog(AutomationElement mainWindow) => TryDismissCompatibilityWindow(mainWindow);

static void ClickSettingsNavigation(AutomationElement mainWindow)
{
    for (var attempt = 1; attempt <= 4; attempt++)
    {
        mainWindow = RefreshWindow(mainWindow);
        BringToForeground(mainWindow);

        if (SettingsAppearancePageIsReady(mainWindow))
            return;

        var settingsNav = FindDescendant(mainWindow, "SettingsNavItem")
                          ?? FindByName(mainWindow, "Settings", ControlType.ListItem)
                          ?? FindByName(mainWindow, "Einstellungen", ControlType.ListItem)
                          ?? FindByName(mainWindow, "设置", ControlType.ListItem)
                          ?? FindByName(mainWindow, "Settings", ControlType.Button);

        if (settingsNav is not null)
            ActivateElement(settingsNav);

        Thread.Sleep(attempt == 4 ? 0 : 800);
    }

    if (!SettingsAppearancePageIsReady(RefreshWindow(mainWindow)))
        throw new InvalidOperationException("Settings navigation item was not found or did not open the appearance page.");
}

static bool WaitForLanguageInstallActivity(AutomationElement mainWindow, string cultureInstallDirectory, TimeSpan timeout)
{
    return WaitUntil(
        () =>
        {
            var liveWindow = RefreshWindow(mainWindow);
            var panel = FindDescendant(liveWindow, "LanguageOperationPanel");
            if (panel is not null && IsVisible(panel))
                return true;

            var progressBar = FindDescendant(liveWindow, "LanguageOperationProgressBar");
            if (progressBar is not null && IsVisible(progressBar) && ProgressBarShowsActivity(progressBar))
                return true;

            return Directory.Exists(cultureInstallDirectory) &&
                   Directory.EnumerateFiles(cultureInstallDirectory, "Universal Device Toolkit.resources.dll").Any();
        },
        timeout,
        "language install activity");
}

static bool ProgressBarShowsActivity(AutomationElement progressBar)
{
    if (progressBar.TryGetCurrentPattern(RangeValuePattern.Pattern, out var patternObj) &&
        patternObj is RangeValuePattern range &&
        range.Current.Value > 0)
        return true;

    return progressBar.Current.IsEnabled && progressBar.Current.BoundingRectangle.Width > 0;
}

static AutomationElement WaitForVisibleDescendant(AutomationElement root, string automationId, TimeSpan timeout)
{
    if (!WaitUntil(
            () =>
            {
                var element = FindDescendant(RefreshWindow(root), automationId);
                return element is not null && IsVisible(element);
            },
            timeout,
            $"visible '{automationId}'"))
        throw new TimeoutException($"Timed out waiting for visible element '{automationId}'.");

    return FindDescendant(RefreshWindow(root), automationId)!;
}

static AutomationElement WaitForDescendant(AutomationElement root, string automationId, TimeSpan timeout)
{
    if (!WaitUntil(
            () => FindDescendant(RefreshWindow(root), automationId) is not null,
            timeout,
            $"element '{automationId}'"))
        throw new TimeoutException($"Timed out waiting for element '{automationId}'.");

    return FindDescendant(RefreshWindow(root), automationId)!;
}

static AutomationElement RefreshWindow(AutomationElement window)
{
    try
    {
        var handle = window.Current.NativeWindowHandle;
        if (handle != 0)
            return AutomationElement.FromHandle((IntPtr)handle);
    }
    catch
    {
        // keep original element when handle is unavailable
    }

    return window;
}

static AutomationElement? FindDescendant(AutomationElement root, string automationId)
{
    var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
    return root.FindFirst(TreeScope.Descendants, condition);
}

static AutomationElement? FindByName(AutomationElement root, string name, ControlType controlType)
{
    var condition = new AndCondition(
        new PropertyCondition(AutomationElement.NameProperty, name),
        new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));
    return root.FindFirst(TreeScope.Descendants, condition);
}

static AutomationElement? FindByControlType(AutomationElement root, ControlType controlType)
{
    var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, controlType);
    return root.FindFirst(TreeScope.Descendants, condition);
}

static bool IsVisible(AutomationElement element) =>
    !element.Current.IsOffscreen && element.Current.BoundingRectangle.Width > 0;

static void InvokeElement(AutomationElement element) => ActivateElement(element);

static void ActivateElement(AutomationElement element)
{
    if (element.TryGetCurrentPattern(InvokePattern.Pattern, out var patternObj) && patternObj is InvokePattern invoke)
    {
        invoke.Invoke();
        return;
    }

    if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var selectObj) && selectObj is SelectionItemPattern select)
    {
        select.Select();
        return;
    }

    MouseClick(element);
}

static AutomationElement? FindLanguageListItem(AutomationElement combo, params string[] markers)
{
    var listItemCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem);
    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var candidates = combo.FindAll(TreeScope.Descendants, listItemCondition)
        .Cast<AutomationElement>()
        .Concat(AutomationElement.RootElement.FindAll(TreeScope.Descendants, listItemCondition).Cast<AutomationElement>())
        .Where(IsVisible)
        .Where(item =>
        {
            var name = item.Current.Name ?? string.Empty;
            return seen.Add(name);
        });

    foreach (var item in candidates)
    {
        var name = item.Current.Name ?? string.Empty;
        if (markers.Any(marker => name.Contains(marker, StringComparison.OrdinalIgnoreCase)))
            return item;
    }

    foreach (var exactName in markers)
    {
        var byName = FindByName(combo, exactName, ControlType.ListItem)
                     ?? FindByName(AutomationElement.RootElement, exactName, ControlType.ListItem);
        if (byName is not null)
            return byName;
    }

    return null;
}

static bool SettingsAppearancePageIsReady(AutomationElement root) =>
    FindDescendant(root, "InstallLanguageButton") is not null
    || FindDescendant(root, "LanguageComboBox") is not null
    || FindVisibleTextContains(root, "Theme style")
    || FindVisibleTextContains(root, "Designstil")
    || FindVisibleTextContains(root, "主题样式");

static bool FindVisibleTextContains(AutomationElement root, string text)
{
    var condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text);
    foreach (AutomationElement element in root.FindAll(TreeScope.Descendants, condition))
    {
        try
        {
            var name = element.Current.Name ?? string.Empty;
            if (name.Contains(text, StringComparison.OrdinalIgnoreCase) && IsVisible(element))
                return true;
        }
        catch
        {
            // skip stale elements
        }
    }

    return false;
}

static void BringToForeground(AutomationElement window)
{
    try
    {
        var handle = window.Current.NativeWindowHandle;
        if (handle == 0)
            return;

        var hwnd = (IntPtr)handle;
        ShowWindow(hwnd, 9);
        SetForegroundWindow(hwnd);
        Thread.Sleep(100);
    }
    catch
    {
        // best-effort
    }
}

static void MouseClick(AutomationElement element)
{
    var bounds = element.Current.BoundingRectangle;
    if (bounds.Width <= 1 || bounds.Height <= 1)
        throw new InvalidOperationException($"Cannot click element with empty bounds: '{element.Current.AutomationId}'.");

    var x = (int)Math.Round(bounds.X + bounds.Width / 2);
    var y = (int)Math.Round(bounds.Y + bounds.Height / 2);
    SetCursorPos(x, y);
    mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero);
    Thread.Sleep(50);
    mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
}

[DllImport("user32.dll")]
static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

[DllImport("user32.dll")]
static extern bool SetForegroundWindow(IntPtr hWnd);

[DllImport("user32.dll")]
static extern bool SetCursorPos(int x, int y);

[DllImport("user32.dll")]
static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

static void ExpandCombo(AutomationElement combo)
{
    if (combo.TryGetCurrentPattern(ExpandCollapsePattern.Pattern, out var patternObj) &&
        patternObj is ExpandCollapsePattern expand &&
        expand.Current.ExpandCollapseState != ExpandCollapseState.Expanded)
    {
        expand.Expand();
        return;
    }

    InvokeElement(combo);
}

static void SelectItem(AutomationElement item)
{
    if (item.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var patternObj) && patternObj is SelectionItemPattern select)
    {
        select.Select();
        return;
    }

    InvokeElement(item);
}

static bool WaitUntil(Func<bool> predicate, TimeSpan timeout, string description)
{
    var stopAt = DateTime.UtcNow + timeout;
    while (DateTime.UtcNow < stopAt)
    {
        if (predicate())
        {
            Console.WriteLine($"[lang-ui-smoke] Ready: {description}");
            return true;
        }

        Thread.Sleep(250);
    }

    Console.WriteLine($"[lang-ui-smoke] Timeout: {description}");
    return false;
}

sealed record SmokeOptions(bool UseOnlineCatalog, bool BackendOnly, string Culture, string? CatalogUrl, bool SkipPreflight)
{
    public static SmokeOptions Parse(string[] args)
    {
        var backendOnly = args.Any(arg => arg.Equals("--backend-only", StringComparison.OrdinalIgnoreCase));
        var online = !backendOnly &&
                       !args.Any(arg => arg.Equals("--local", StringComparison.OrdinalIgnoreCase));
        var culture = "de";
        string? catalogUrl = null;
        var skipPreflight = args.Any(arg => arg.Equals("--skip-preflight", StringComparison.OrdinalIgnoreCase));

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--online", StringComparison.OrdinalIgnoreCase))
                online = true;
            else if (args[i].Equals("--simulate-online", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[lang-ui-smoke] --simulate-online is deprecated; use Start-MockCatalogServer.ps1 + --local or --catalog-url.");
                online = false;
            }
            else if (args[i].Equals("--backend-only", StringComparison.OrdinalIgnoreCase))
            {
                backendOnly = true;
                online = false;
            }
            else if (args[i].Equals("--local", StringComparison.OrdinalIgnoreCase))
                online = false;
            else if (args[i].Equals("--culture", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                culture = args[++i];
            else if (args[i].Equals("--catalog-url", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                catalogUrl = args[++i];
        }

        return new SmokeOptions(online, backendOnly, culture, catalogUrl, skipPreflight);
    }
}

sealed record UiMarkers(string[] ComboMarkers, string[] UiStrings);
