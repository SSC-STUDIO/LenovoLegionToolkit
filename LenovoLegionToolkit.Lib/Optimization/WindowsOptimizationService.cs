using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using LenovoLegionToolkit.Lib.Settings;
using Microsoft.Win32;

using ToolkitRegistry = LenovoLegionToolkit.Lib.System.Registry;

namespace LenovoLegionToolkit.Lib.Optimization;

public record WindowsOptimizationActionDefinition(
    string Key,
    string TitleResourceKey,
    string DescriptionResourceKey,
    Func<CancellationToken, Task> ExecuteAsync,
    bool Recommended = true);

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

    private static readonly IReadOnlyList<WindowsOptimizationCategoryDefinition> _categories = BuildCategories();
    private static readonly IReadOnlyDictionary<string, WindowsOptimizationActionDefinition> _actionsByKey =
        _categories.SelectMany(category => category.Actions)
            .GroupBy(action => action.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<WindowsOptimizationCategoryDefinition> GetCategories() => _categories;

    public async Task ExecuteActionsAsync(IEnumerable<string> actionKeys, CancellationToken cancellationToken)
    {
        if (actionKeys is null)
            return;

        foreach (var key in actionKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_actionsByKey.TryGetValue(key, out var action))
                continue;

            await action.ExecuteAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task ApplyPerformanceOptimizationsAsync(CancellationToken cancellationToken)
    {
        var keys = _categories
            .Where(category => !string.Equals(category.Key, CleanupCategoryKey, StringComparison.OrdinalIgnoreCase))
            .SelectMany(category => category.Actions.Where(action => action.Recommended).Select(action => action.Key));

        return ExecuteActionsAsync(keys, cancellationToken);
    }

    public Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        var keys = _categories
            .Where(category => string.Equals(category.Key, CleanupCategoryKey, StringComparison.OrdinalIgnoreCase))
            .SelectMany(category => category.Actions.Where(action => action.Recommended).Select(action => action.Key));

        return ExecuteActionsAsync(keys, cancellationToken);
    }

    private static IReadOnlyList<WindowsOptimizationCategoryDefinition> BuildCategories()
    {
        static RegistryValueDefinition Reg(string hive, string subKey, string valueName, object value, RegistryValueKind kind) =>
            new(hive, subKey, valueName, value, kind);

        var explorerTaskbarTweaks = new[]
        {
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0, RegistryValueKind.DWord),
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0, RegistryValueKind.DWord),
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer", "EnableAutoTray", 0, RegistryValueKind.DWord),
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarGlomLevel", 2, RegistryValueKind.DWord)
        };

        var explorerResponsivenessTweaks = new[]
        {
            Reg("HKEY_CURRENT_USER", @"Control Panel\Desktop", "MenuShowDelay", "0", RegistryValueKind.String),
            Reg("HKEY_CURRENT_USER", @"Control Panel\Desktop", "AutoEndTasks", "1", RegistryValueKind.String),
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1, RegistryValueKind.DWord)
        };

        var explorerVisibilityTweaks = new[]
        {
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0, RegistryValueKind.DWord),
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", 1, RegistryValueKind.DWord)
        };

        var explorerSuggestionsTweaks = new[]
        {
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSyncProviderNotifications", 0, RegistryValueKind.DWord),
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338387Enabled", 0, RegistryValueKind.DWord),
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0, RegistryValueKind.DWord),
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0, RegistryValueKind.DWord),
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-310093Enabled", 0, RegistryValueKind.DWord)
        };

        var telemetryTweaks = new[]
        {
            Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0, RegistryValueKind.DWord),
            Reg("HKEY_CURRENT_USER", @"Software\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightFeatures", 1, RegistryValueKind.DWord),
            Reg("HKEY_CURRENT_USER", @"Software\Policies\Microsoft\Windows\CloudContent", "DisableSuggestionsWindowsTips", 1, RegistryValueKind.DWord),
            Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord)
        };

        var multimediaTweaks = new[]
        {
            Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord),
            Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0, RegistryValueKind.DWord)
        };

        var memoryTweaks = new[]
        {
            Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1, RegistryValueKind.DWord),
            Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1, RegistryValueKind.DWord)
        };

        var notificationTweaks = new[]
        {
            Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon", 1, RegistryValueKind.DWord),
            Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter", 1, RegistryValueKind.DWord)
        };

        var diagnosticsServices = new[] { "DiagTrack", "diagnosticshub.standardcollector.service", "DoSvc" };
        var sysMainService = new[] { "SysMain" };
        var searchService = new[] { "WSearch" };
        var remoteRegistryService = new[] { "RemoteRegistry" };
        var errorReportingService = new[] { "WerSvc" };

        var remoteDesktopCacheCommands = new[]
        {
            "del /f /s /q \"%LocalAppData%\\Microsoft\\Terminal Server Client\\Cache\\*\" >nul 2>&1"
        };

        var windowsUpdateCacheCommands = new[]
        {
            "del /f /s /q \"%SystemRoot%\\SoftwareDistribution\\Download\\*\" >nul 2>&1",
            "del /f /s /q \"%SystemRoot%\\SoftwareDistribution\\DeliveryOptimization\\*\" >nul 2>&1"
        };

        var browserCacheCommands = new[]
        {
            "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\INetCache\\*\" >nul 2>&1",
            "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\INetCookies\\*\" >nul 2>&1"
        };

        var thumbnailCacheCommands = new[]
        {
            "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db\" >nul 2>&1",
            "del /f /s /q \"%LocalAppData%\\Local\\D3DSCache\\*\" >nul 2>&1"
        };

        var dotnetNativeImageCommands = new[]
        {
            "rd /s /q \"%WinDir%\\assembly\\NativeImages_v4.0.30319_32\" >nul 2>&1 & rd /s /q \"%WinDir%\\assembly\\NativeImages_v4.0.30319_64\" >nul 2>&1"
        };

        var systemLogCommands = new[]
        {
            "del /f /s /q \"%SystemRoot%\\Logs\\*\" >nul 2>&1",
            "del /f /s /q \"%ProgramData%\\Microsoft\\Windows\\WER\\ReportQueue\\*\" >nul 2>&1",
            "del /f /s /q \"%ProgramData%\\Microsoft\\Diagnosis\\*\" >nul 2>&1"
        };

        var crashDumpCommands = new[]
        {
            "del /f /s /q \"%SystemRoot%\\Minidump\\*.dmp\" >nul 2>&1",
            "del /f /q \"%SystemRoot%\\memory.dmp\" >nul 2>&1",
            "del /f /s /q \"%SystemDrive%\\*.dmp\" >nul 2>&1"
        };

        var defenderCommands = new[]
        {
            "del /f /s /q \"%ProgramData%\\Microsoft\\Windows Defender\\Scans\\*\" >nul 2>&1"
        };

        var tempCommands = new[]
        {
            "del /f /s /q \"%SystemRoot%\\Temp\\*\" >nul 2>&1",
            "del /f /s /q \"%SystemDrive%\\Windows\\Temp\\*\" >nul 2>&1",
            "del /f /s /q \"%TEMP%\\*\" >nul 2>&1"
        };

        var recycleBinCommands = new[]
        {
            "rd /s /q \"%SystemDrive%\\$Recycle.bin\" >nul 2>&1"
        };

        var prefetchCommands = new[]
        {
            "del /f /s /q \"%SystemRoot%\\Prefetch\\*\" >nul 2>&1"
        };

        var componentStoreCommands = new[]
        {
            "dism /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
            "del /f /s /q \"%SystemRoot%\\WinSxS\\Temp\\*\" >nul 2>&1"
        };

        var categories = new List<WindowsOptimizationCategoryDefinition>
        {
            new(
                "explorer",
                "WindowsOptimization_Category_Explorer_Title",
                "WindowsOptimization_Category_Explorer_Description",
                new[]
                {
                    new WindowsOptimizationActionDefinition(
                        "explorer.taskbar",
                        "WindowsOptimization_Action_ExplorerTaskbar_Title",
                        "WindowsOptimization_Action_ExplorerTaskbar_Description",
                        ct => ApplyRegistryTweaksAsync(ct, explorerTaskbarTweaks)),
                    new WindowsOptimizationActionDefinition(
                        "explorer.responsiveness",
                        "WindowsOptimization_Action_ExplorerResponsiveness_Title",
                        "WindowsOptimization_Action_ExplorerResponsiveness_Description",
                        ct => ApplyRegistryTweaksAsync(ct, explorerResponsivenessTweaks)),
                    new WindowsOptimizationActionDefinition(
                        "explorer.visibility",
                        "WindowsOptimization_Action_ExplorerVisibility_Title",
                        "WindowsOptimization_Action_ExplorerVisibility_Description",
                        ct => ApplyRegistryTweaksAsync(ct, explorerVisibilityTweaks)),
                    new WindowsOptimizationActionDefinition(
                        "explorer.suggestions",
                        "WindowsOptimization_Action_ExplorerSuggestions_Title",
                        "WindowsOptimization_Action_ExplorerSuggestions_Description",
                        ct => ApplyRegistryTweaksAsync(ct, explorerSuggestionsTweaks))
                }),
            new(
                "performance",
                "WindowsOptimization_Category_Performance_Title",
                "WindowsOptimization_Category_Performance_Description",
                new[]
                {
                    new WindowsOptimizationActionDefinition(
                        "performance.multimedia",
                        "WindowsOptimization_Action_PerformanceMultimedia_Title",
                        "WindowsOptimization_Action_PerformanceMultimedia_Description",
                        ct => ApplyRegistryTweaksAsync(ct, multimediaTweaks)),
                    new WindowsOptimizationActionDefinition(
                        "performance.memory",
                        "WindowsOptimization_Action_PerformanceMemory_Title",
                        "WindowsOptimization_Action_PerformanceMemory_Description",
                        ct => ApplyRegistryTweaksAsync(ct, memoryTweaks)),
                    new WindowsOptimizationActionDefinition(
                        "performance.notifications",
                        "WindowsOptimization_Action_PerformanceNotifications_Title",
                        "WindowsOptimization_Action_PerformanceNotifications_Description",
                        ct => ApplyRegistryTweaksAsync(ct, notificationTweaks), Recommended: false),
                    new WindowsOptimizationActionDefinition(
                        "performance.telemetry",
                        "WindowsOptimization_Action_PerformanceTelemetry_Title",
                        "WindowsOptimization_Action_PerformanceTelemetry_Description",
                        ct => ApplyRegistryTweaksAsync(ct, telemetryTweaks)),
                    new WindowsOptimizationActionDefinition(
                        "performance.powerPlan",
                        "WindowsOptimization_Action_PerformancePowerPlan_Title",
                        "WindowsOptimization_Action_PerformancePowerPlan_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, "powercfg -setactive SCHEME_MIN", "powercfg -h off"))
                }),
            new(
                "services",
                "WindowsOptimization_Category_Services_Title",
                "WindowsOptimization_Category_Services_Description",
                new[]
                {
                    new WindowsOptimizationActionDefinition(
                        "services.diagnostics",
                        "WindowsOptimization_Action_ServicesDiagnostics_Title",
                        "WindowsOptimization_Action_ServicesDiagnostics_Description",
                        ct => DisableServicesAsync(ct, diagnosticsServices)),
                    new WindowsOptimizationActionDefinition(
                        "services.sysmain",
                        "WindowsOptimization_Action_ServicesSysMain_Title",
                        "WindowsOptimization_Action_ServicesSysMain_Description",
                        ct => DisableServicesAsync(ct, sysMainService)),
                    new WindowsOptimizationActionDefinition(
                        "services.search",
                        "WindowsOptimization_Action_ServicesSearch_Title",
                        "WindowsOptimization_Action_ServicesSearch_Description",
                        ct => DisableServicesAsync(ct, searchService), Recommended: false),
                    new WindowsOptimizationActionDefinition(
                        "services.remoteRegistry",
                        "WindowsOptimization_Action_ServicesRemoteRegistry_Title",
                        "WindowsOptimization_Action_ServicesRemoteRegistry_Description",
                        ct => DisableServicesAsync(ct, remoteRegistryService)),
                    new WindowsOptimizationActionDefinition(
                        "services.errorReporting",
                        "WindowsOptimization_Action_ServicesErrorReporting_Title",
                        "WindowsOptimization_Action_ServicesErrorReporting_Description",
                        ct => DisableServicesAsync(ct, errorReportingService))
                }),
            new(
                CleanupCategoryKey,
                "WindowsOptimization_Category_Cleanup_Title",
                "WindowsOptimization_Category_Cleanup_Description",
                new[]
                {
                    new WindowsOptimizationActionDefinition(
                        "cleanup.remoteDesktopCache",
                        "WindowsOptimization_Action_CleanupRemoteDesktop_Title",
                        "WindowsOptimization_Action_CleanupRemoteDesktop_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, remoteDesktopCacheCommands)),
                    new WindowsOptimizationActionDefinition(
                        "cleanup.windowsUpdate",
                        "WindowsOptimization_Action_CleanupWindowsUpdate_Title",
                        "WindowsOptimization_Action_CleanupWindowsUpdate_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, windowsUpdateCacheCommands)),
                    new WindowsOptimizationActionDefinition(
                        "cleanup.browserCache",
                        "WindowsOptimization_Action_CleanupBrowserCache_Title",
                        "WindowsOptimization_Action_CleanupBrowserCache_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, browserCacheCommands)),
                    new WindowsOptimizationActionDefinition(
                        "cleanup.thumbnailCache",
                        "WindowsOptimization_Action_CleanupThumbnailCache_Title",
                        "WindowsOptimization_Action_CleanupThumbnailCache_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, thumbnailCacheCommands)),
                    new WindowsOptimizationActionDefinition(
                        "cleanup.dotnetNative",
                        "WindowsOptimization_Action_CleanupDotNet_Title",
                        "WindowsOptimization_Action_CleanupDotNet_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, dotnetNativeImageCommands), Recommended: false),
                    new WindowsOptimizationActionDefinition(
                        "cleanup.logs",
                        "WindowsOptimization_Action_CleanupLogs_Title",
                        "WindowsOptimization_Action_CleanupLogs_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, systemLogCommands)),
                    new WindowsOptimizationActionDefinition(
                        "cleanup.crashDumps",
                        "WindowsOptimization_Action_CleanupCrashDumps_Title",
                        "WindowsOptimization_Action_CleanupCrashDumps_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, crashDumpCommands)),
                    new WindowsOptimizationActionDefinition(
                        "cleanup.defender",
                        "WindowsOptimization_Action_CleanupDefender_Title",
                        "WindowsOptimization_Action_CleanupDefender_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, defenderCommands), Recommended: false),
                    new WindowsOptimizationActionDefinition(
                        "cleanup.tempFiles",
                        "WindowsOptimization_Action_CleanupTempFiles_Title",
                        "WindowsOptimization_Action_CleanupTempFiles_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, tempCommands)),
                    new WindowsOptimizationActionDefinition(
                        AppxCleanupActionKey,
                        "WindowsOptimization_Action_CleanupAppx_Title",
                        "WindowsOptimization_Action_CleanupAppx_Description",
                        ExecuteAppxCleanupAsync,
                        Recommended: false),
                    new WindowsOptimizationActionDefinition(
                        "cleanup.recycleBin",
                        "WindowsOptimization_Action_CleanupRecycleBin_Title",
                        "WindowsOptimization_Action_CleanupRecycleBin_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, recycleBinCommands)),
                    new WindowsOptimizationActionDefinition(
                        "cleanup.prefetch",
                        "WindowsOptimization_Action_CleanupPrefetch_Title",
                        "WindowsOptimization_Action_CleanupPrefetch_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, prefetchCommands), Recommended: false),
                    new WindowsOptimizationActionDefinition(
                        CustomCleanupActionKey,
                        "WindowsOptimization_Action_CleanupCustom_Title",
                        "WindowsOptimization_Action_CleanupCustom_Description",
                        ExecuteCustomCleanupAsync,
                        Recommended: false),
                    new WindowsOptimizationActionDefinition(
                        "cleanup.componentStore",
                        "WindowsOptimization_Action_CleanupComponentStore_Title",
                        "WindowsOptimization_Action_CleanupComponentStore_Description",
                        ct => ExecuteCommandsSequentiallyAsync(ct, componentStoreCommands))
                })
        };

        return categories;
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
        var command =
            $"powershell -NoProfile -ExecutionPolicy Bypass -Command \"foreach ($pkg in @('{packageList}')) {{ try {{ Get-AppxPackage -AllUsers $pkg | Remove-AppxPackage -ErrorAction SilentlyContinue }} catch {{}} try {{ Get-AppxProvisionedPackage -Online | Where-Object DisplayName -eq $pkg | Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue }} catch {{}} }}\"";

        return ExecuteCommandsSequentiallyAsync(cancellationToken, command);
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

    private readonly record struct RegistryValueDefinition(string Hive, string SubKey, string ValueName, object Value, RegistryValueKind Kind);
}
