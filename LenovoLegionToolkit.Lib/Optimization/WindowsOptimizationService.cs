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

    private readonly record struct RegistryValueDefinition(string Hive, string SubKey, string ValueName, object Value, RegistryValueKind Kind);


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
        "rd /s /q \"%WinDir%\\assembly\\NativeImages_v4.0.30319_32\" >nul 2>&1",
        "rd /s /q \"%WinDir%\\assembly\\NativeImages_v4.0.30319_64\" >nul 2>&1"
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

    private static readonly IReadOnlyList<RegistryValueDefinition> NetworkAccelerationTweaks =
    [
        // TCP/IP 优化
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpAckFrequency", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TCPNoDelay", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Tcp1323Opts", 3, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "DefaultTTL", 64, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "EnablePMTUBHDetect", 0, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "EnablePMTUDiscovery", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "GlobalMaxTcpWindowSize", 65535, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpMaxDupAcks", 2, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "SackOpts", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpTimedWaitDelay", 30, RegistryValueKind.DWord),
        // DNS 缓存优化
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", "MaxCacheTtl", 3600, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", "MaxNegativeCacheTtl", 300, RegistryValueKind.DWord)
    ];

    private static readonly IReadOnlyList<string> NetworkOptimizationCommands =
    [
        "ipconfig /flushdns",
        "netsh winsock reset",
        "netsh int ip reset"
    ];

    private static readonly IReadOnlyList<string> ComponentStoreCommands =
    [
        "dism /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
        "del /f /s /q \"%SystemRoot%\\WinSxS\\Temp\\*\" >nul 2>&1"
    ];

    // Build base categories once (excluding dynamic integration category)
    private static readonly IReadOnlyList<WindowsOptimizationCategoryDefinition> StaticBaseCategories = BuildCategories();

    private static IReadOnlyDictionary<string, WindowsOptimizationActionDefinition> GetActionsByKey()
    {
        return GetCategories()
            .SelectMany(category => category.Actions)
            .GroupBy(action => action.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<WindowsOptimizationCategoryDefinition> GetCategories()
    {
        var list = new List<WindowsOptimizationCategoryDefinition>(StaticBaseCategories);
        // Append beautification (right-click menu style) as a regular category
        var beautifyCategory = CreateBeautificationCategoryDynamic();
        if (beautifyCategory.Actions.Count > 0)
            list.Add(beautifyCategory);
        return list;
    }

    public async Task ExecuteActionsAsync(IEnumerable<string> actionKeys, CancellationToken cancellationToken)
    {
        if (actionKeys is null)
            return;

        var actionsByKey = GetActionsByKey();
        var executedCount = 0;
        var skippedCount = 0;

        foreach (var key in actionKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!actionsByKey.TryGetValue(key, out var action))
            {
                skippedCount++;
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Action not found, skipping. [key={key}]");
                continue;
            }

            try
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Executing action. [key={key}]");
                
                await action.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                executedCount++;
                
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Action executed successfully. [key={key}]");
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Action execution failed. [key={key}]", ex);
                throw; // Re-throw to let caller handle the error
            }
        }

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"Actions execution completed. [executed={executedCount}, skipped={skippedCount}]");
    }

    public Task ApplyPerformanceOptimizationsAsync(CancellationToken cancellationToken)
    {
        var keys = GetCategories()
            .Where(category => !string.Equals(category.Key, CleanupCategoryKey, StringComparison.OrdinalIgnoreCase))
            .SelectMany(category => category.Actions.Where(action => action.Recommended).Select(action => action.Key));

        return ExecuteActionsAsync(keys, cancellationToken);
    }

    public Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        var keys = GetCategories()
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

            var actionsByKey = GetActionsByKey();
            if (!actionsByKey.TryGetValue(key, out var action))
                continue;

            try
            {
                var size = await EstimateActionSizeAsync(key, cancellationToken).ConfigureAwait(false);
                totalSize += size;
            }
            catch (TaskCanceledException)
            {
                // Estimation was superseded by a newer request (e.g., user changed selection)
                // Silently skip without logging to avoid noisy traces.
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Failed to estimate cleanup size for action. [action={key}]", ex);
            }
        }

        return totalSize;
    }

    public async Task<long> EstimateActionSizeAsync(string actionKey, CancellationToken cancellationToken)
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
                Environment.ExpandEnvironmentVariables("%SystemDrive%\\"),
                filePattern: "*.dmp",
                cancellationToken: cancellationToken).ConfigureAwait(false),
            "cleanup.recycleBin" => await EstimateDirectorySizeAsync(
                Environment.ExpandEnvironmentVariables("%SystemDrive%\\$Recycle.bin"),
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
        var actionsByKey = GetActionsByKey();
        if (!actionsByKey.TryGetValue(actionKey, out var definition))
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

        CreateNetworkAccelerationCategory(),

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
                        ExplorerSuggestionsTweaks),

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

    private static WindowsOptimizationCategoryDefinition CreateNetworkAccelerationCategory() =>
            new(
                "network",
                "WindowsOptimization_Category_NetworkAcceleration_Title",
                "WindowsOptimization_Category_NetworkAcceleration_Description",
                new[]
                {
                CreateRegistryAction(
                        "network.acceleration",
                        "WindowsOptimization_Action_NetworkAcceleration_Title",
                        "WindowsOptimization_Action_NetworkAcceleration_Description",
                    NetworkAccelerationTweaks),
                CreateCommandAction(
                        "network.optimization",
                        "WindowsOptimization_Action_NetworkOptimization_Title",
                        "WindowsOptimization_Action_NetworkOptimization_Description",
                    NetworkOptimizationCommands,
                    recommended: false)
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
                    new WindowsOptimizationActionDefinition(
                        "cleanup.registry",
                        "WindowsOptimization_Action_CleanupRegistry_Title",
                        "WindowsOptimization_Action_CleanupRegistry_Description",
                        ExecuteRegistryCleanupAsync,
                        Recommended: false),
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






    // Nilesoft Shell context menu integration

    // CLS_ContextMenu = {BAE3934B-8A6A-4BFB-81BD-3FC599A1BAF1}

    // Beautification: Right-click menu style (classic/default) using Nilesoft Shell handler

    private static WindowsOptimizationCategoryDefinition CreateBeautificationCategoryDynamic()
    {
        var actions = new List<WindowsOptimizationActionDefinition>();
        // 使用 NilesoftShellHelper API 来判断是否安装
        var isInstalled = NilesoftShellHelper.IsInstalled();
        var isInstalledUsingShellExe = NilesoftShellHelper.IsInstalledUsingShellExe();

        // Dynamic action based on registration status (not just file existence)
        // If installed and registered (using shell.exe API), show "Uninstall" action; if not, show "Install/Enable" action
        if (isInstalledUsingShellExe)
        {
            // Shell is installed and registered - show "Uninstall" action
            actions.Add(new WindowsOptimizationActionDefinition(
                "beautify.contextMenu.uninstallShell",
                "WindowsOptimization_Action_NilesoftShell_Uninstall_Title",
                "WindowsOptimization_Action_NilesoftShell_Uninstall_Description",
                ct =>
                {
                    var shellExe = NilesoftShellHelper.GetNilesoftShellExePath();
                    if (string.IsNullOrWhiteSpace(shellExe))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace((FormattableString)$"Nilesoft Shell not found. Command skipped.");
                        return Task.CompletedTask;
                    }

                    // This action is now "Apply Modern Context Menu"
                    // When checked: if not installed/registered, register it
                    // When unchecked: unregister it (handled by HandleActionUncheckedAsync)
                    // Since this action is only shown when shell is already installed and registered,
                    // and IsAppliedAsync returns true when installed, checking this action should do nothing
                    // (it's already applied). Unchecking is handled separately.
                    if (!NilesoftShellHelper.IsInstalledUsingShellExe())
                    {
                        // If somehow not registered, register it
                        return ExecuteCommandsSequentiallyAsync(ct, $@"""{shellExe}"" -register -treat -restart");
                    }
                    
                    // Already installed and registered, no action needed
                    return Task.CompletedTask;
                },
                Recommended: false,
                IsAppliedAsync: async ct => 
                {
                    // Use shell.exe's API for more accurate installation status check
                    // Call the async version directly since we're already in an async context
                    return await NilesoftShellHelper.IsInstalledUsingShellExeAsync().ConfigureAwait(false);
                }));
        }
        else if (isInstalled)
        {
            // Shell.exe exists but not registered - show "Install/Enable" action
            actions.Add(new WindowsOptimizationActionDefinition(
                "beautify.contextMenu.enableClassic",
                "WindowsOptimization_Action_NilesoftShell_Enable_Title",
                "WindowsOptimization_Action_NilesoftShell_Enable_Description",
                ct =>
                {
                    var shellExe = NilesoftShellHelper.GetNilesoftShellExePath();
                    if (string.IsNullOrWhiteSpace(shellExe))
                    {
                        if (Log.Instance.IsTraceEnabled)
                            Log.Instance.Trace((FormattableString)$"Nilesoft Shell not found. Enable command skipped.");
                        return Task.CompletedTask;
                    }
                    // Register and enable the shell
                    return ExecuteCommandsSequentiallyAsync(ct, $@"""{shellExe}"" -register -treat -restart");
                },
                Recommended: true,  // 设置为推荐选项
                IsAppliedAsync: async ct => 
                {
                    // Use shell.exe's API for more accurate installation status check
                    // This matches what shell.exe /isinstalled returns
                    // Call the async version directly since we're already in an async context
                    return await NilesoftShellHelper.IsInstalledUsingShellExeAsync().ConfigureAwait(false);
                }));
        }
        // If shell.exe doesn't exist, don't show any action (can't install without the file)

        return new WindowsOptimizationCategoryDefinition(
            "beautify.contextMenu",
            "WindowsOptimization_Category_NilesoftShell_Title",
            "WindowsOptimization_Category_NilesoftShell_Description",
            actions);
    }

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

    private static Task ExecuteRegistryCleanupAsync(CancellationToken cancellationToken)
    {
        // Common user MRU/Recent registry keys (safe to clear)
        (RegistryHive Hive, string SubKey)[] targets =
        [
            (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU"),
            (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs"),
            (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths"),
            (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU"),
            (RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\LastVisitedPidlMRU")
        ];

        foreach (var (hive, subKey) in targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using var key = baseKey.OpenSubKey(subKey, writable: true);
                if (key is null)
                    continue;

                // Delete values
                foreach (var valueName in key.GetValueNames())
                {
                    try { key.DeleteValue(valueName, throwOnMissingValue: false); }
                    catch { /* ignore */ }
                }
                // Delete subkeys
                foreach (var child in key.GetSubKeyNames())
                {
                    try { key.DeleteSubKeyTree(child, throwOnMissingSubKey: false); }
                    catch { /* ignore */ }
                }
            }
            catch (Exception ex)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Registry cleanup failed. [key={hive}\\{subKey}]", ex);
            }
        }

        return Task.CompletedTask;
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
        // Validate command to prevent injection attacks
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be null or empty.", nameof(command));

        // Check for dangerous command injection patterns
        // These patterns could allow arbitrary command execution
        // Note: ">" and "<" are allowed for output redirection (e.g., ">nul 2>&1") as they are safe in cmd.exe /c context
        var dangerousPatterns = new[] { "&&", "||", "|", ";", "`", "$(" };
        foreach (var pattern in dangerousPatterns)
        {
            if (command.Contains(pattern, StringComparison.Ordinal))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Rejected potentially dangerous command: {command}");
                throw new ArgumentException($"Command contains potentially dangerous pattern: {pattern}", nameof(command));
            }
        }

        // Check for single "&" command separator (but allow "2>&1" and "1>&2" for output redirection)
        // Single "&" can be used to chain commands: "command1 & command2"
        // We check for " & " (with spaces on both sides) which is the most common command chaining pattern
        // We also check for "& " (ampersand followed by space) and " &" (space before ampersand)
        // but exclude "2>&1" and "1>&2" patterns where & is part of output redirection
        
        // Check for " & " (space before and after) - this is definitely command chaining
        if (command.Contains(" & ", StringComparison.Ordinal))
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Rejected potentially dangerous command: {command}");
            throw new ArgumentException("Command contains potentially dangerous pattern: & (command chaining)", nameof(command));
        }

        // Check for "& " (ampersand followed by space) - but exclude "2>&1" and "1>&2"
        var index = command.IndexOf("& ", StringComparison.Ordinal);
        if (index >= 0)
        {
            // If at start or if previous character is not '>' (not part of "2>&1" or "1>&2")
            if (index == 0 || (index > 0 && command[index - 1] != '>'))
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Rejected potentially dangerous command: {command}");
                throw new ArgumentException("Command contains potentially dangerous pattern: & (command chaining)", nameof(command));
            }
        }

        // Check for " &" (space before ampersand) - but exclude "2>&1" and "1>&2"
        index = command.IndexOf(" &", StringComparison.Ordinal);
        if (index >= 0)
        {
            // Check if this is part of a valid redirection pattern (2>&1 or 1>&2)
            // index points to the space in " &", so:
            // - command[index-2] and command[index-1] should be "2>" or "1>"
            // - command[index+1] should be '&'
            // - command[index+2] should be '1' or '2'
            bool isRedirectionPattern = false;
            if (index >= 2 && index + 2 < command.Length)
            {
                var charBeforeSpace = command[index - 1]; // Should be '>'
                var charTwoBeforeSpace = command[index - 2]; // Should be '2' or '1'
                var charAfterSpace = command[index + 1]; // Should be '&'
                var charAfterAmpersand = command[index + 2]; // Should be '1' or '2'
                
                // Valid patterns: "2>&1" or "1>&2"
                if (charBeforeSpace == '>' && charAfterSpace == '&' &&
                    ((charTwoBeforeSpace == '2' && charAfterAmpersand == '1') ||
                     (charTwoBeforeSpace == '1' && charAfterAmpersand == '2')))
                {
                    isRedirectionPattern = true;
                }
            }
            
            // If it's not a valid redirection pattern, it's potentially dangerous
            if (!isRedirectionPattern)
            {
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Rejected potentially dangerous command: {command}");
                throw new ArgumentException("Command contains potentially dangerous pattern: & (command chaining)", nameof(command));
            }
        }

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
                    WindowStyle = ProcessWindowStyle.Hidden,
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
            // First, kill explorer.exe
            var killInfo = new ProcessStartInfo
            {
                FileName = "taskkill.exe",
                Arguments = "/f /im explorer.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var killProcess = Process.Start(killInfo);
            if (killProcess != null)
            {
                killProcess.WaitForExit(5000);
                if (Log.Instance.IsTraceEnabled)
                    Log.Instance.Trace($"Explorer kill command executed. [exitCode={killProcess.ExitCode}]");
            }

            // Then, start explorer.exe
            var startInfo = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var startProcess = Process.Start(startInfo);
            if (startProcess != null && Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Explorer start command executed.");
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Failed to restart Explorer.", ex);
        }
    }

}
