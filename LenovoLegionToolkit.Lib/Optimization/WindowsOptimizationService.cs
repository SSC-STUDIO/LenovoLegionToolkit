using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Lib.Settings;
using Microsoft.Win32;
using Windows.Win32;
using Windows.Win32.Foundation;

using ToolkitRegistry = LenovoLegionToolkit.Lib.System.Registry;

namespace LenovoLegionToolkit.Lib.Optimization;

public record WindowsOptimizationActionDefinition(
    string Key,
    string TitleResourceKey,
    string DescriptionResourceKey,
    Func<CancellationToken, Task> ExecuteAsync,
    bool Recommended = true,
    Func<CancellationToken, Task<bool>>? IsAppliedAsync = null);

public record WindowsOptimizationCategoryDefinition(
    string Key,
    string TitleResourceKey,
    string DescriptionResourceKey,
    IReadOnlyList<WindowsOptimizationActionDefinition> Actions);

public class WindowsOptimizationService
{
    public const string CleanupCategoryKey = "cleanup";
    public const string CustomCleanupActionKey = "cleanup.custom";
    public const string AppxCleanupActionKey = "cleanup.appxBloatware";

    private readonly record struct RegistryValueDefinition(string Hive, string SubKey, string ValueName, object Value, RegistryValueKind Kind);

    private static readonly string[] DefaultAppxPackages =
    {
        "Microsoft.BingNews",
        "Microsoft.BingWeather",
        "Microsoft.BingFinance",
        "Microsoft.BingSports",
        "Microsoft.GetHelp",
        "Microsoft.Getstarted",
        "Microsoft.MixedReality.Portal",
        "Microsoft.Microsoft3DViewer",
        "Microsoft.MicrosoftOfficeHub",
        "Microsoft.MicrosoftSolitaireCollection",
        "Microsoft.MicrosoftStickyNotes",
        "Microsoft.OneConnect",
        "Microsoft.Paint3D",
        "Microsoft.People",
        "Microsoft.PowerAutomateDesktop",
        "Microsoft.RemoteDesktop",
        "Microsoft.SkypeApp",
        "Microsoft.Whiteboard",
        "Microsoft.WindowsFeedbackHub",
        "Microsoft.Xbox.TCUI",
        "Microsoft.XboxApp",
        "Microsoft.XboxGameOverlay",
        "Microsoft.XboxGamingOverlay",
        "Microsoft.XboxIdentityProvider",
        "Microsoft.XboxSpeechToTextOverlay",
        "Microsoft.ZuneMusic",
        "Microsoft.ZuneVideo",
        "Microsoft.YourPhone",
        "Clipchamp.Clipchamp",
        "TikTok.TikTok",
        "SpotifyAB.SpotifyMusic",
        "Disney.37853FC22B2CE",
        "Microsoft.549981C3F5F10"
    };

    public static IReadOnlyList<string> DefaultAppxPackageIds => DefaultAppxPackages;

    private static readonly IReadOnlyList<RegistryValueDefinition> ExplorerTaskbarTweaks =
    [
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer", "EnableAutoTray", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarGlomLevel", 2, RegistryValueKind.DWord)
    ];

    private static readonly IReadOnlyList<RegistryValueDefinition> StartMenuDisableTweaks =
    [
        // Enable search box in taskbar (1 = show search box)
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 1, RegistryValueKind.DWord),
        // Disable start menu pinned list
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoStartMenuPinnedList", 1, RegistryValueKind.DWord),
        // Disable start menu more programs
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoStartMenuMorePrograms", 1, RegistryValueKind.DWord),
        // Disable start menu most frequently used programs list
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoStartMenuMFUprogramsList", 1, RegistryValueKind.DWord),
        // Disable search box suggestions
        Reg("HKEY_CURRENT_USER", @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord)
    ];

    private static readonly IReadOnlyList<RegistryValueDefinition> ExplorerResponsivenessTweaks =
    [
        Reg("HKEY_CURRENT_USER", @"Control Panel\Desktop", "MenuShowDelay", "0", RegistryValueKind.String),
        Reg("HKEY_CURRENT_USER", @"Control Panel\Desktop", "AutoEndTasks", "1", RegistryValueKind.String),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1, RegistryValueKind.DWord)
    ];

    private static readonly IReadOnlyList<RegistryValueDefinition> ExplorerVisibilityTweaks =
    [
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", 1, RegistryValueKind.DWord)
    ];

    private static readonly IReadOnlyList<RegistryValueDefinition> ExplorerSuggestionsTweaks =
    [
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSyncProviderNotifications", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338387Enabled", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-310093Enabled", 0, RegistryValueKind.DWord)
    ];

    private static readonly IReadOnlyList<RegistryValueDefinition> TelemetryTweaks =
    [
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightFeatures", 1, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Policies\Microsoft\Windows\CloudContent", "DisableSuggestionsWindowsTips", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord)
    ];

    private static readonly IReadOnlyList<RegistryValueDefinition> MultimediaTweaks =
    [
        Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0, RegistryValueKind.DWord)
    ];

    private static readonly IReadOnlyList<RegistryValueDefinition> MemoryTweaks =
    [
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1, RegistryValueKind.DWord)
    ];

    private static readonly IReadOnlyList<RegistryValueDefinition> NotificationTweaks =
    [
        Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter", 1, RegistryValueKind.DWord)
    ];

    private static readonly IReadOnlyList<string> DiagnosticsServices = ["DiagTrack", "diagnosticshub.standardcollector.service", "DoSvc"];
    private static readonly IReadOnlyList<string> SysMainService = ["SysMain"];
    private static readonly IReadOnlyList<string> SearchService = ["WSearch"];
    private static readonly IReadOnlyList<string> RemoteRegistryService = ["RemoteRegistry"];
    private static readonly IReadOnlyList<string> ErrorReportingService = ["WerSvc"];

    private static readonly IReadOnlyList<string> RemoteDesktopCacheCommands =
    [
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Terminal Server Client\\Cache\\*\" >nul 2>&1"
    ];

    private static readonly IReadOnlyList<string> WindowsUpdateCacheCommands =
    [
        "del /f /s /q \"%SystemRoot%\\SoftwareDistribution\\Download\\*\" >nul 2>&1",
        "del /f /s /q \"%SystemRoot%\\SoftwareDistribution\\DeliveryOptimization\\*\" >nul 2>&1"
    ];

    private static readonly IReadOnlyList<string> BrowserCacheCommands =
    [
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\INetCache\\*\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\INetCookies\\*\" >nul 2>&1"
    ];

    private static readonly IReadOnlyList<string> ThumbnailCacheCommands =
    [
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Local\\D3DSCache\\*\" >nul 2>&1"
    ];

    private static readonly IReadOnlyList<string> DotnetNativeImageCommands =
    [
        "rd /s /q \"%WinDir%\\assembly\\NativeImages_v4.0.30319_32\" >nul 2>&1 & rd /s /q \"%WinDir%\\assembly\\NativeImages_v4.0.30319_64\" >nul 2>&1"
    ];

    private static readonly IReadOnlyList<string> SystemLogCommands =
    [
        "del /f /s /q \"%SystemRoot%\\Logs\\*\" >nul 2>&1",
        "del /f /s /q \"%ProgramData%\\Microsoft\\Windows\\WER\\ReportQueue\\*\" >nul 2>&1",
        "del /f /s /q \"%ProgramData%\\Microsoft\\Diagnosis\\*\" >nul 2>&1"
    ];

    private static readonly IReadOnlyList<string> CrashDumpCommands =
    [
        "del /f /s /q \"%SystemRoot%\\Minidump\\*.dmp\" >nul 2>&1",
        "del /f /q \"%SystemRoot%\\memory.dmp\" >nul 2>&1",
        "del /f /s /q \"%SystemDrive%\\*.dmp\" >nul 2>&1"
    ];

    private static readonly IReadOnlyList<string> DefenderCommands =
    [
        "del /f /s /q \"%ProgramData%\\Microsoft\\Windows Defender\\Scans\\*\" >nul 2>&1"
    ];

    private static readonly IReadOnlyList<string> TempCommands =
    [
        "del /f /s /q \"%SystemRoot%\\Temp\\*\" >nul 2>&1",
        "del /f /s /q \"%SystemDrive%\\Windows\\Temp\\*\" >nul 2>&1",
        "del /f /s /q \"%TEMP%\\*\" >nul 2>&1"
    ];

    private static readonly IReadOnlyList<string> RecycleBinCommands =
    [
        "rd /s /q \"%SystemDrive%\\$Recycle.bin\" >nul 2>&1"
    ];

    private static readonly IReadOnlyList<string> PrefetchCommands =
    [
        "del /f /s /q \"%SystemRoot%\\Prefetch\\*\" >nul 2>&1"
    ];

    private static readonly IReadOnlyList<string> PowerPlanCommands =
    [
        "powercfg -setactive SCHEME_MIN",
        "powercfg -h off"
    ];

    private static readonly IReadOnlyList<string> ComponentStoreCommands =
    [
        "dism /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
        "del /f /s /q \"%SystemRoot%\\WinSxS\\Temp\\*\" >nul 2>&1"
    ];

    private static readonly IReadOnlyList<WindowsOptimizationCategoryDefinition> Categories = BuildCategories();

    private static readonly IReadOnlyDictionary<string, WindowsOptimizationActionDefinition> ActionsByKey =
        Categories.SelectMany(category => category.Actions)
            .GroupBy(action => action.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<WindowsOptimizationCategoryDefinition> GetCategories() => Categories;

    public async Task ExecuteActionsAsync(IEnumerable<string> actionKeys, CancellationToken cancellationToken)
    {
        if (actionKeys is null)
            return;

        foreach (var key in actionKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ActionsByKey.TryGetValue(key, out var action))
                continue;

            await action.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task ApplyPerformanceOptimizationsAsync(CancellationToken cancellationToken)
    {
        var keys = Categories
            .Where(category => !string.Equals(category.Key, CleanupCategoryKey, StringComparison.OrdinalIgnoreCase))
            .SelectMany(category => category.Actions.Where(action => action.Recommended).Select(action => action.Key));

        return ExecuteActionsAsync(keys, cancellationToken);
    }

    public Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        var keys = Categories
            .Where(category => category.Key.StartsWith("cleanup.", StringComparison.OrdinalIgnoreCase))
            .SelectMany(category => category.Actions.Where(action => action.Recommended).Select(action => action.Key));

        return ExecuteActionsAsync(keys, cancellationToken);
    }

    public async Task<long> EstimateCleanupSizeAsync(IEnumerable<string> actionKeys, CancellationToken cancellationToken)
    {
        if (actionKeys is null)
            return 0;

        long totalSize = 0;

        foreach (var key in actionKeys)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!ActionsByKey.TryGetValue(key, out var action))
                continue;

            try
            {
                var size = await EstimateActionSizeAsync(key, cancellationToken).ConfigureAwait(false);
                totalSize += size;
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to estimate cleanup size for action. [action={key}]", ex);
            }
        }

        return totalSize;
    }

    private async Task<long> EstimateActionSizeAsync(string actionKey, CancellationToken cancellationToken)
    {
        return actionKey switch
        {
            "cleanup.browserCache" => await EstimateDirectorySizeAsync(
                Environment.ExpandEnvironmentVariables("%LocalAppData%\\Microsoft\\Windows\\INetCache"),
                filePattern: null,
                cancellationToken: cancellationToken).ConfigureAwait(false) +
                await EstimateDirectorySizeAsync(
                    Environment.ExpandEnvironmentVariables("%LocalAppData%\\Microsoft\\Windows\\INetCookies"),
                    filePattern: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.thumbnailCache" => await EstimateDirectorySizeAsync(
                Environment.ExpandEnvironmentVariables("%LocalAppData%\\Microsoft\\Windows\\Explorer"),
                filePattern: "thumbcache_*.db",
                cancellationToken: cancellationToken).ConfigureAwait(false) +
                await EstimateDirectorySizeAsync(
                    Environment.ExpandEnvironmentVariables("%LocalAppData%\\Local\\D3DSCache"),
                    filePattern: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.windowsUpdate" => await EstimateDirectorySizeAsync(
                Environment.ExpandEnvironmentVariables("%SystemRoot%\\SoftwareDistribution\\Download"),
                filePattern: null,
                cancellationToken: cancellationToken).ConfigureAwait(false) +
                await EstimateDirectorySizeAsync(
                    Environment.ExpandEnvironmentVariables("%SystemRoot%\\SoftwareDistribution\\DeliveryOptimization"),
                    filePattern: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.tempFiles" => await EstimateDirectorySizeAsync(
                Environment.ExpandEnvironmentVariables("%SystemRoot%\\Temp"),
                filePattern: null,
                cancellationToken: cancellationToken).ConfigureAwait(false) +
                await EstimateDirectorySizeAsync(
                    Environment.ExpandEnvironmentVariables("%SystemDrive%\\Windows\\Temp"),
                    filePattern: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false) +
                await EstimateDirectorySizeAsync(
                    Environment.ExpandEnvironmentVariables("%TEMP%"),
                    filePattern: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.logs" => await EstimateDirectorySizeAsync(
                Environment.ExpandEnvironmentVariables("%SystemRoot%\\Logs"),
                filePattern: null,
                cancellationToken: cancellationToken).ConfigureAwait(false) +
                await EstimateDirectorySizeAsync(
                    Environment.ExpandEnvironmentVariables("%ProgramData%\\Microsoft\\Windows\\WER\\ReportQueue"),
                    filePattern: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false) +
                await EstimateDirectorySizeAsync(
                    Environment.ExpandEnvironmentVariables("%ProgramData%\\Microsoft\\Diagnosis"),
                    filePattern: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.crashDumps" => await EstimateDirectorySizeAsync(
                Environment.ExpandEnvironmentVariables("%SystemRoot%\\Minidump"),
                filePattern: "*.dmp",
                cancellationToken: cancellationToken).ConfigureAwait(false) +
                await EstimateFileSizeAsync(
                    Environment.ExpandEnvironmentVariables("%SystemRoot%\\memory.dmp"),
                    cancellationToken).ConfigureAwait(false) +
                await EstimateDirectorySizeAsync(
                Environment.GetEnvironmentVariable("SystemDrive") + "\\",
                filePattern: "*.dmp",
                cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.recycleBin" => await EstimateDirectorySizeAsync(
                Environment.GetEnvironmentVariable("SystemDrive") + "\\$Recycle.bin",
                filePattern: null,
                cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.defender" => await EstimateDirectorySizeAsync(
                Environment.ExpandEnvironmentVariables("%ProgramData%\\Microsoft\\Windows Defender\\Scans"),
                filePattern: null,
                cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.componentStore" => await EstimateDirectorySizeAsync(
                Environment.ExpandEnvironmentVariables("%SystemRoot%\\WinSxS\\Temp"),
                filePattern: null,
                cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.prefetch" => await EstimateDirectorySizeAsync(
                Environment.ExpandEnvironmentVariables("%SystemRoot%\\Prefetch"),
                filePattern: null,
                cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.dotnetNative" => await EstimateDirectorySizeAsync(
                Environment.ExpandEnvironmentVariables("%WinDir%\\assembly\\NativeImages_v4.0.30319_32"),
                filePattern: null,
                cancellationToken: cancellationToken).ConfigureAwait(false) +
                await EstimateDirectorySizeAsync(
                    Environment.ExpandEnvironmentVariables("%WinDir%\\assembly\\NativeImages_v4.0.30319_64"),
                    filePattern: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.remoteDesktopCache" => await EstimateDirectorySizeAsync(
                Environment.ExpandEnvironmentVariables("%LocalAppData%\\Microsoft\\Terminal Server Client\\Cache"),
                filePattern: null,
                cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.custom" => await EstimateCustomCleanupSizeAsync(cancellationToken).ConfigureAwait(false),
            _ => 0
        };
    }

    private async Task<long> EstimateDirectorySizeAsync(string directoryPath, string? filePattern = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    return 0;

                long size = 0;
                var searchOption = SearchOption.AllDirectories;

                if (string.IsNullOrEmpty(filePattern))
                {
                    foreach (var file in Directory.EnumerateFiles(directoryPath, "*", searchOption))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Exists)
                                size += fileInfo.Length;
                        }
                        catch
                        {
                            // Ignore inaccessible files
                        }
                    }
                }
                else
                {
                    foreach (var file in Directory.EnumerateFiles(directoryPath, filePattern, searchOption))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Exists)
                                size += fileInfo.Length;
                        }
                        catch
                        {
                            // Ignore inaccessible files
                        }
                    }
                }

                return size;
            }
            catch
            {
                return 0;
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<long> EstimateFileSizeAsync(string filePath, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                    return 0;

                var fileInfo = new FileInfo(filePath);
                return fileInfo.Exists ? fileInfo.Length : 0;
            }
            catch
            {
                return 0;
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    private Task<long> EstimateCustomCleanupSizeAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var settings = IoCContainer.Resolve<ApplicationSettings>();
            var rules = settings.Store.CustomCleanupRules ?? new List<CustomCleanupRule>();

            long totalSize = 0;

            foreach (var rule in rules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(rule.DirectoryPath))
                    continue;

                var directoryPath = Environment.ExpandEnvironmentVariables(rule.DirectoryPath.Trim());

                if (!Directory.Exists(directoryPath))
                    continue;

                var normalizedExtensions = (rule.Extensions ?? [])
                    .Select(NormalizeExtension)
                    .Where(extension => !string.IsNullOrEmpty(extension))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (normalizedExtensions.Length == 0)
                    continue;

                var extensionsSet = new HashSet<string>(normalizedExtensions, StringComparer.OrdinalIgnoreCase);
                var searchOption = rule.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

                try
                {
                    foreach (var file in Directory.EnumerateFiles(directoryPath, "*", searchOption))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        string extension;
                        try
                        {
                            extension = Path.GetExtension(file);
                        }
                        catch
                        {
                            continue;
                        }

                        if (!extensionsSet.Contains(extension))
                            continue;

                        try
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.Exists)
                                totalSize += fileInfo.Length;
                        }
                        catch
                        {
                            // Ignore inaccessible files
                        }
                    }
                }
                catch
                {
                    // Ignore directory enumeration errors
                }
            }

            return totalSize;
        }, cancellationToken);
    }

    public async Task<bool?> TryGetActionAppliedAsync(string actionKey, CancellationToken cancellationToken)
    {
        if (!ActionsByKey.TryGetValue(actionKey, out var definition))
            return null;

        if (definition.IsAppliedAsync is null)
            return null;

        try
        {
            return await definition.IsAppliedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to evaluate optimization action state. [action={actionKey}]", ex);
            return null;
        }
    }

    private static IReadOnlyList<WindowsOptimizationCategoryDefinition> BuildCategories() =>
    [
        CreateExplorerCategory(),
        CreatePerformanceCategory(),
        CreateServicesCategory(),
        CreateCleanupCacheCategory(),
        CreateCleanupSystemFilesCategory(),
        CreateCleanupSystemComponentsCategory(),
        CreateCleanupPerformanceCategory(),
        CreateCleanupCustomCategory()
    ];

    private static RegistryValueDefinition Reg(string hive, string subKey, string valueName, object value, RegistryValueKind kind) =>
            new(hive, subKey, valueName, value, kind);

    private static WindowsOptimizationActionDefinition CreateRegistryAction(
        string key,
        string titleResourceKey,
        string descriptionResourceKey,
        IReadOnlyList<RegistryValueDefinition> tweaks,
        bool recommended = true) =>
        new(
            key,
            titleResourceKey,
            descriptionResourceKey,
            ct => ApplyRegistryTweaksAsync(ct, tweaks),
            recommended,
            ct => Task.FromResult(AreRegistryTweaksApplied(tweaks)));

    private static WindowsOptimizationActionDefinition CreateServiceAction(
        string key,
        string titleResourceKey,
        string descriptionResourceKey,
        IReadOnlyList<string> services,
        bool recommended = true) =>
        new(
            key,
            titleResourceKey,
            descriptionResourceKey,
            ct => DisableServicesAsync(ct, services),
            recommended,
            ct => Task.FromResult(AreServicesDisabled(services)));

    private static WindowsOptimizationActionDefinition CreateCommandAction(
        string key,
        string titleResourceKey,
        string descriptionResourceKey,
        IReadOnlyList<string> commands,
        bool recommended = true) =>
        new(
            key,
            titleResourceKey,
            descriptionResourceKey,
            ct => ExecuteCommandsSequentiallyAsync(ct, commands.ToArray()),
            recommended);

    private static WindowsOptimizationCategoryDefinition CreateExplorerCategory() =>
            new(
                "explorer",
                "WindowsOptimization_Category_Explorer_Title",
                "WindowsOptimization_Category_Explorer_Description",
                new[]
                {
                CreateRegistryAction(
                        "explorer.taskbar",
                        "WindowsOptimization_Action_ExplorerTaskbar_Title",
                        "WindowsOptimization_Action_ExplorerTaskbar_Description",
                    ExplorerTaskbarTweaks),
                new WindowsOptimizationActionDefinition(
                        "explorer.startMenu",
                        "WindowsOptimization_Action_ExplorerStartMenu_Title",
                        "WindowsOptimization_Action_ExplorerStartMenu_Description",
                        ExecuteStartMenuDisableAsync,
                        Recommended: false,
                        IsAppliedAsync: ct => Task.FromResult(AreStartMenuTweaksApplied())),
                CreateRegistryAction(
                        "explorer.responsiveness",
                        "WindowsOptimization_Action_ExplorerResponsiveness_Title",
                        "WindowsOptimization_Action_ExplorerResponsiveness_Description",
                    ExplorerResponsivenessTweaks),
                CreateRegistryAction(
                        "explorer.visibility",
                        "WindowsOptimization_Action_ExplorerVisibility_Title",
                        "WindowsOptimization_Action_ExplorerVisibility_Description",
                    ExplorerVisibilityTweaks),
                CreateRegistryAction(
                        "explorer.suggestions",
                        "WindowsOptimization_Action_ExplorerSuggestions_Title",
                        "WindowsOptimization_Action_ExplorerSuggestions_Description",
                    ExplorerSuggestionsTweaks)
            });

    private static WindowsOptimizationCategoryDefinition CreatePerformanceCategory() =>
            new(
                "performance",
                "WindowsOptimization_Category_Performance_Title",
                "WindowsOptimization_Category_Performance_Description",
                new[]
                {
                CreateRegistryAction(
                        "performance.multimedia",
                        "WindowsOptimization_Action_PerformanceMultimedia_Title",
                        "WindowsOptimization_Action_PerformanceMultimedia_Description",
                    MultimediaTweaks),
                CreateRegistryAction(
                        "performance.memory",
                        "WindowsOptimization_Action_PerformanceMemory_Title",
                        "WindowsOptimization_Action_PerformanceMemory_Description",
                    MemoryTweaks),
                CreateRegistryAction(
                        "performance.notifications",
                        "WindowsOptimization_Action_PerformanceNotifications_Title",
                        "WindowsOptimization_Action_PerformanceNotifications_Description",
                    NotificationTweaks,
                    recommended: false),
                CreateRegistryAction(
                        "performance.telemetry",
                        "WindowsOptimization_Action_PerformanceTelemetry_Title",
                        "WindowsOptimization_Action_PerformanceTelemetry_Description",
                    TelemetryTweaks),
                CreateCommandAction(
                        "performance.powerPlan",
                        "WindowsOptimization_Action_PerformancePowerPlan_Title",
                        "WindowsOptimization_Action_PerformancePowerPlan_Description",
                    PowerPlanCommands)
            });

    private static WindowsOptimizationCategoryDefinition CreateServicesCategory() =>
            new(
                "services",
                "WindowsOptimization_Category_Services_Title",
                "WindowsOptimization_Category_Services_Description",
                new[]
                {
                CreateServiceAction(
                        "services.diagnostics",
                        "WindowsOptimization_Action_ServicesDiagnostics_Title",
                        "WindowsOptimization_Action_ServicesDiagnostics_Description",
                    DiagnosticsServices),
                CreateServiceAction(
                        "services.sysmain",
                        "WindowsOptimization_Action_ServicesSysMain_Title",
                        "WindowsOptimization_Action_ServicesSysMain_Description",
                    SysMainService),
                CreateServiceAction(
                        "services.search",
                        "WindowsOptimization_Action_ServicesSearch_Title",
                        "WindowsOptimization_Action_ServicesSearch_Description",
                    SearchService,
                    recommended: false),
                CreateServiceAction(
                        "services.remoteRegistry",
                        "WindowsOptimization_Action_ServicesRemoteRegistry_Title",
                        "WindowsOptimization_Action_ServicesRemoteRegistry_Description",
                    RemoteRegistryService),
                CreateServiceAction(
                        "services.errorReporting",
                        "WindowsOptimization_Action_ServicesErrorReporting_Title",
                        "WindowsOptimization_Action_ServicesErrorReporting_Description",
                    ErrorReportingService)
            });

    // Cache Cleanup Category
    private static WindowsOptimizationCategoryDefinition CreateCleanupCacheCategory() =>
            new(
                "cleanup.cache",
                "WindowsOptimization_Category_CleanupCache_Title",
                "WindowsOptimization_Category_CleanupCache_Description",
                new[]
                {
                    CreateCommandAction(
                        "cleanup.browserCache",
                        "WindowsOptimization_Action_CleanupBrowserCache_Title",
                        "WindowsOptimization_Action_CleanupBrowserCache_Description",
                        BrowserCacheCommands),
                    CreateCommandAction(
                        "cleanup.thumbnailCache",
                        "WindowsOptimization_Action_CleanupThumbnailCache_Title",
                        "WindowsOptimization_Action_CleanupThumbnailCache_Description",
                        ThumbnailCacheCommands),
                    CreateCommandAction(
                        "cleanup.remoteDesktopCache",
                        "WindowsOptimization_Action_CleanupRemoteDesktop_Title",
                        "WindowsOptimization_Action_CleanupRemoteDesktop_Description",
                        RemoteDesktopCacheCommands)
                });

    // System Files Cleanup Category
    private static WindowsOptimizationCategoryDefinition CreateCleanupSystemFilesCategory() =>
            new(
                "cleanup.systemFiles",
                "WindowsOptimization_Category_CleanupSystemFiles_Title",
                "WindowsOptimization_Category_CleanupSystemFiles_Description",
                new[]
                {
                    CreateCommandAction(
                        "cleanup.tempFiles",
                        "WindowsOptimization_Action_CleanupTempFiles_Title",
                        "WindowsOptimization_Action_CleanupTempFiles_Description",
                        TempCommands),
                    CreateCommandAction(
                        "cleanup.logs",
                        "WindowsOptimization_Action_CleanupLogs_Title",
                        "WindowsOptimization_Action_CleanupLogs_Description",
                        SystemLogCommands),
                    CreateCommandAction(
                        "cleanup.crashDumps",
                        "WindowsOptimization_Action_CleanupCrashDumps_Title",
                        "WindowsOptimization_Action_CleanupCrashDumps_Description",
                        CrashDumpCommands),
                    CreateCommandAction(
                        "cleanup.recycleBin",
                        "WindowsOptimization_Action_CleanupRecycleBin_Title",
                        "WindowsOptimization_Action_CleanupRecycleBin_Description",
                        RecycleBinCommands),
                    CreateCommandAction(
                        "cleanup.defender",
                        "WindowsOptimization_Action_CleanupDefender_Title",
                        "WindowsOptimization_Action_CleanupDefender_Description",
                        DefenderCommands,
                        recommended: false)
                });

    // System Components Cleanup Category
    private static WindowsOptimizationCategoryDefinition CreateCleanupSystemComponentsCategory() =>
            new(
                "cleanup.systemComponents",
                "WindowsOptimization_Category_CleanupSystemComponents_Title",
                "WindowsOptimization_Category_CleanupSystemComponents_Description",
                new[]
                {
                    CreateCommandAction(
                        "cleanup.windowsUpdate",
                        "WindowsOptimization_Action_CleanupWindowsUpdate_Title",
                        "WindowsOptimization_Action_CleanupWindowsUpdate_Description",
                        WindowsUpdateCacheCommands),
                    CreateCommandAction(
                        "cleanup.componentStore",
                        "WindowsOptimization_Action_CleanupComponentStore_Title",
                        "WindowsOptimization_Action_CleanupComponentStore_Description",
                        ComponentStoreCommands),
                    CreateCommandAction(
                        "cleanup.dotnetNative",
                        "WindowsOptimization_Action_CleanupDotNet_Title",
                        "WindowsOptimization_Action_CleanupDotNet_Description",
                        DotnetNativeImageCommands,
                        recommended: false)
                });

    // Performance Cleanup Category
    private static WindowsOptimizationCategoryDefinition CreateCleanupPerformanceCategory() =>
            new(
                "cleanup.performance",
                "WindowsOptimization_Category_CleanupPerformance_Title",
                "WindowsOptimization_Category_CleanupPerformance_Description",
                new[]
                {
                    CreateCommandAction(
                        "cleanup.prefetch",
                        "WindowsOptimization_Action_CleanupPrefetch_Title",
                        "WindowsOptimization_Action_CleanupPrefetch_Description",
                        PrefetchCommands,
                        recommended: false)
                });

    // Custom Cleanup Category
    private static WindowsOptimizationCategoryDefinition CreateCleanupCustomCategory() =>
            new(
                "cleanup.custom",
                "WindowsOptimization_Category_CleanupCustom_Title",
                "WindowsOptimization_Category_CleanupCustom_Description",
                new[]
                {
                    new WindowsOptimizationActionDefinition(
                        CustomCleanupActionKey,
                        "WindowsOptimization_Action_CleanupCustom_Title",
                        "WindowsOptimization_Action_CleanupCustom_Description",
                        ExecuteCustomCleanupAsync,
                        Recommended: false)
                });

    private static Task ExecuteCustomCleanupAsync(CancellationToken cancellationToken)
    {
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        var rules = settings.Store.CustomCleanupRules ?? new List<CustomCleanupRule>();

        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(rule.DirectoryPath))
                continue;

            var directoryPath = Environment.ExpandEnvironmentVariables(rule.DirectoryPath.Trim());

            if (!Directory.Exists(directoryPath))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Custom cleanup skipped. Directory not found. [path={directoryPath}]");
                continue;
            }

            var normalizedExtensions = (rule.Extensions ?? [])
                .Select(NormalizeExtension)
                .Where(extension => !string.IsNullOrEmpty(extension))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (normalizedExtensions.Length == 0)
                continue;

            var extensionsSet = new HashSet<string>(normalizedExtensions, StringComparer.OrdinalIgnoreCase);
            var searchOption = rule.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            try
            {
                foreach (var file in Directory.EnumerateFiles(directoryPath, "*", searchOption))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string extension;
                    try
                    {
                        extension = Path.GetExtension(file);
                    }
                    catch
                    {
                        continue;
                    }

                    if (!extensionsSet.Contains(extension))
                        continue;

                    try
                    {
                        File.Delete(file);

                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Custom cleanup deleted file. [path={file}]");
                    }
                    catch (Exception ex)
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace($"Custom cleanup failed to delete file. [path={file}]", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Custom cleanup failed to enumerate directory. [path={directoryPath}]", ex);
            }
        }

        return Task.CompletedTask;
    }

    private static Task ExecuteAppxCleanupAsync(CancellationToken cancellationToken)
    {
        var settings = IoCContainer.Resolve<ApplicationSettings>();
        var packages = settings.Store.AppxPackagesToRemove ?? new List<string>();

        if (packages.Count == 0)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"AppX cleanup skipped. No packages selected.");
            return Task.CompletedTask;
        }

        var sanitizedPackages = packages
            .Where(package => !string.IsNullOrWhiteSpace(package))
            .Select(package => package.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(package => package.Replace("'", "''"))
            .ToList();

        if (sanitizedPackages.Count == 0)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace((FormattableString)$"AppX cleanup skipped. Selected packages list empty after sanitization.");
            return Task.CompletedTask;
        }

        var packageList = string.Join("','", sanitizedPackages);
        // Support both PackageFullName and PackageId
        var command =
            $"powershell -NoProfile -ExecutionPolicy Bypass -Command \"foreach ($pkg in @('{packageList}')) {{ try {{ $appx = Get-AppxPackage -AllUsers | Where-Object {{ $_.PackageFullName -eq $pkg -or $_.Name -eq $pkg }}; if ($appx) {{ $appx | Remove-AppxPackage -ErrorAction SilentlyContinue }} }} catch {{}} try {{ Get-AppxProvisionedPackage -Online | Where-Object {{ $_.PackageName -eq $pkg -or $_.DisplayName -eq $pkg }} | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue }} catch {{}} }}\"";

        return ExecuteCommandsSequentiallyAsync(cancellationToken, command);
    }

    private static bool AreRegistryTweaksApplied(IEnumerable<RegistryValueDefinition> tweaks)
    {
        foreach (var tweak in tweaks)
        {
            try
            {
                var currentValue = ToolkitRegistry.GetValue<object?>(tweak.Hive, tweak.SubKey, tweak.ValueName, null);
                if (currentValue is null)
                    return false;

                if (!RegistryValueEquals(currentValue, tweak.Value, tweak.Kind))
                    return false;
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    private static bool RegistryValueEquals(object currentValue, object expectedValue, RegistryValueKind kind)
    {
        try
        {
            return kind switch
            {
                RegistryValueKind.DWord or RegistryValueKind.QWord => Convert.ToInt64(currentValue) == Convert.ToInt64(expectedValue),
                RegistryValueKind.String or RegistryValueKind.ExpandString => string.Equals(Convert.ToString(currentValue), Convert.ToString(expectedValue), StringComparison.Ordinal),
                _ => Equals(currentValue, expectedValue)
            };
        }
        catch
        {
            return false;
        }
    }

    private static Task ApplyRegistryTweaksAsync(CancellationToken cancellationToken, IEnumerable<RegistryValueDefinition> tweaks)
    {
        foreach (var tweak in tweaks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApplyRegistryTweak(tweak);
        }

        return Task.CompletedTask;
    }

    private static bool AreServicesDisabled(IEnumerable<string> services)
    {
        foreach (var serviceName in services.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var startValue = ToolkitRegistry.GetValue<int>("HKEY_LOCAL_MACHINE",
                    $@"SYSTEM\CurrentControlSet\Services\{serviceName}", "Start", -1);
                if (startValue != 4)
                    return false;
            }
            catch
            {
                return false;
            }

            try
            {
                using var serviceController = new ServiceController(serviceName);
                if (serviceController.Status is not ServiceControllerStatus.Stopped and not ServiceControllerStatus.StopPending)
                    return false;
            }
            catch (InvalidOperationException)
            {
                // Service not found – treat as already disabled.
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    private static Task DisableServicesAsync(CancellationToken cancellationToken, IEnumerable<string> services)
    {
        foreach (var serviceName in services.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DisableService(serviceName);
        }

        return Task.CompletedTask;
    }

    private static async Task ExecuteCommandsSequentiallyAsync(CancellationToken cancellationToken, params string[] commands)
    {
        foreach (var command in commands)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ExecuteCommandLineAsync(command, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void ApplyRegistryTweak(RegistryValueDefinition tweak)
    {
        try
        {
            ToolkitRegistry.SetValue(tweak.Hive, tweak.SubKey, tweak.ValueName, tweak.Value, true, tweak.Kind);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Registry tweak applied. [hive={tweak.Hive}, key={tweak.SubKey}, value={tweak.ValueName}, kind={tweak.Kind}, data={tweak.Value}]");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to apply registry tweak. [hive={tweak.Hive}, key={tweak.SubKey}, value={tweak.ValueName}]", ex);
        }
    }

    private static void DisableService(string serviceName)
    {
        try
        {
            ToolkitRegistry.SetValue("HKEY_LOCAL_MACHINE", $@"SYSTEM\CurrentControlSet\Services\{serviceName}", "Start", 4, true, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to set service start type. [service={serviceName}]", ex);
        }

        try
        {
            using var serviceController = new ServiceController(serviceName);

            if (serviceController.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
                return;

            serviceController.Stop();
            serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Service stopped. [service={serviceName}]");
        }
        catch (InvalidOperationException)
        {
            // Service not found, ignore.
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to stop service. [service={serviceName}]", ex);
        }
    }

    private static async Task ExecuteCommandLineAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c " + command,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await Task.WhenAll(process.WaitForExitAsync(cancellationToken), outputTask, errorTask).ConfigureAwait(false);

            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Command executed. [command={command}, exitCode={process.ExitCode}, output={outputTask.Result}, error={errorTask.Result}]");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to execute command. [command={command}]", ex);
        }
    }

    private static string NormalizeExtension(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        return trimmed.StartsWith(".", StringComparison.Ordinal) ? trimmed : "." + trimmed;
    }

    private static Task ExecuteStartMenuDisableAsync(CancellationToken cancellationToken)
    {
        // Apply registry tweaks
        foreach (var tweak in StartMenuDisableTweaks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ApplyRegistryTweak(tweak);
        }

        // Notify system of registry changes
        NotifyExplorerSettingsChanged();

        // Restart Explorer to apply changes immediately
        RestartExplorer();

        return Task.CompletedTask;
    }

    private static bool AreStartMenuTweaksApplied()
    {
        return AreRegistryTweaksApplied(StartMenuDisableTweaks);
    }

    private static unsafe void NotifyExplorerSettingsChanged()
    {
        try
        {
            const string policy = "Policy";
            fixed (void* ptr = policy)
            {
                PInvoke.SendNotifyMessage(HWND.HWND_BROADCAST, PInvoke.WM_SETTINGCHANGE, 0, new IntPtr(ptr));
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to notify Explorer of settings change.", ex);
        }
    }

    private static void RestartExplorer()
    {
        try
        {
            // Use taskkill command to gracefully terminate explorer
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c taskkill /f /im explorer.exe & start explorer.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit(10000);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Explorer restart command executed. [exitCode={process.ExitCode}]");
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to restart Explorer.", ex);
        }
    }

}
