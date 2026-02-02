using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace LenovoLegionToolkit.Lib.Optimization;

public static class WindowsOptimizationDefinitions
{
    private const int DefaultTcpTtl = 64;
    private const int MaxTcpWindowSize = 65535;
    private const int DefaultTcpTimedWaitDelay = 30;
    private const int DefaultDnsMaxCacheTtl = 3600;
    private const int DefaultDnsMaxNegativeCacheTtl = 300;

    public static RegistryValueDefinition Reg(string hive, string subKey, string valueName, object value, RegistryValueKind kind)
        => new(hive, subKey, valueName, value, kind);

    public static readonly IReadOnlyList<RegistryValueDefinition> ExplorerTaskbarTweaks =
    [
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer", "EnableAutoTray", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarGlomLevel", 2, RegistryValueKind.DWord)
    ];

    public static readonly IReadOnlyList<RegistryValueDefinition> StartMenuDisableTweaks =
    [
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 1, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoStartMenuPinnedList", 1, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoStartMenuMorePrograms", 1, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoStartMenuMFUprogramsList", 1, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord)
    ];

    public static readonly IReadOnlyList<RegistryValueDefinition> ExplorerResponsivenessTweaks =
    [
        Reg("HKEY_CURRENT_USER", @"Control Panel\Desktop", "MenuShowDelay", "0", RegistryValueKind.String),
        Reg("HKEY_CURRENT_USER", @"Control Panel\Desktop", "AutoEndTasks", "1", RegistryValueKind.String),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1, RegistryValueKind.DWord)
    ];

    public static readonly IReadOnlyList<RegistryValueDefinition> ExplorerVisibilityTweaks =
    [
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", 1, RegistryValueKind.DWord)
    ];

    public static readonly IReadOnlyList<RegistryValueDefinition> ExplorerSuggestionsTweaks =
    [
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSyncProviderNotifications", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338387Enabled", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-310093Enabled", 0, RegistryValueKind.DWord)
    ];

    public static readonly IReadOnlyList<RegistryValueDefinition> TelemetryTweaks =
    [
        Reg("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightFeatures", 1, RegistryValueKind.DWord),
        Reg("HKEY_CURRENT_USER", @"Software\Policies\Microsoft\Windows\CloudContent", "DisableSuggestionsWindowsTips", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord)
    ];

    public static readonly IReadOnlyList<RegistryValueDefinition> MultimediaTweaks =
    [
        Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0, RegistryValueKind.DWord)
    ];

    public static readonly IReadOnlyList<RegistryValueDefinition> MemoryTweaks =
    [
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1, RegistryValueKind.DWord)
    ];

    public static readonly IReadOnlyList<RegistryValueDefinition> NotificationTweaks =
    [
        Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SOFTWARE\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter", 1, RegistryValueKind.DWord)
    ];

    public static readonly IReadOnlyList<string> DiagnosticsServices = ["DiagTrack", "diagnosticshub.standardcollector.service", "DoSvc"];
    public static readonly IReadOnlyList<string> SysMainService = ["SysMain"];
    public static readonly IReadOnlyList<string> SearchService = ["WSearch"];
    public static readonly IReadOnlyList<string> RemoteRegistryService = ["RemoteRegistry"];
    public static readonly IReadOnlyList<string> ErrorReportingService = ["WerSvc"];

    public static readonly IReadOnlyList<string> RemoteDesktopCacheCommands =
    [
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Terminal Server Client\\Cache\\*\" >nul 2>&1"
    ];

    public static readonly IReadOnlyList<string> WindowsUpdateCacheCommands =
    [
        "del /f /s /q \"%SystemRoot%\\SoftwareDistribution\\Download\\*\" >nul 2>&1",
        "del /f /s /q \"%SystemRoot%\\SoftwareDistribution\\DeliveryOptimization\\*\" >nul 2>&1"
    ];

    public static readonly IReadOnlyList<string> BrowserCacheCommands =
    [
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\INetCache\\*\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\INetCookies\\*\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Edge\\User Data\\Default\\Cache\\*\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Edge\\User Data\\Default\\Code Cache\\*\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Google\\Chrome\\User Data\\Default\\Cache\\*\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Google\\Chrome\\User Data\\Default\\Code Cache\\*\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Mozilla\\Firefox\\Profiles\\*\\cache2\\*\" >nul 2>&1"
    ];

    public static readonly IReadOnlyList<string> AppLeftoverCommands =
    [
        "del /f /s /q \"%LocalAppData%\\Temp\\*\" >nul 2>&1",
        "del /f /s /q \"%AppData%\\Local\\Temp\\*\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\WER\\*\" >nul 2>&1",
        "del /f /s /q \"%ProgramData%\\Microsoft\\Windows\\WER\\*\" >nul 2>&1"
    ];

    public static readonly IReadOnlyList<string> ThumbnailCacheCommands =
    [
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Local\\D3DSCache\\*\" >nul 2>&1"
    ];

    public static readonly IReadOnlyList<string> DotnetNativeImageCommands =
    [
        "rd /s /q \"%WinDir%\\assembly\\NativeImages_v4.0.30319_32\" >nul 2>&1",
        "rd /s /q \"%WinDir%\\assembly\\NativeImages_v4.0.30319_64\" >nul 2>&1"
    ];

    public static readonly IReadOnlyList<string> SystemLogCommands =
    [
        "del /f /s /q \"%SystemRoot%\\Logs\\*\" >nul 2>&1",
        "del /f /s /q \"%ProgramData%\\Microsoft\\Windows\\WER\\ReportQueue\\*\" >nul 2>&1",
        "del /f /s /q \"%ProgramData%\\Microsoft\\Diagnosis\\*\" >nul 2>&1"
    ];

    public static readonly IReadOnlyList<string> CrashDumpCommands =
    [
        "del /f /s /q \"%SystemRoot%\\Minidump\\*.dmp\" >nul 2>&1",
        "del /f /q \"%SystemRoot%\\memory.dmp\" >nul 2>&1",
        "del /f /s /q \"%SystemDrive%\\*.dmp\" >nul 2>&1"
    ];

    public static readonly IReadOnlyList<string> DefenderCommands =
    [
        "del /f /s /q \"%ProgramData%\\Microsoft\\Windows Defender\\Scans\\*\" >nul 2>&1"
    ];

    public static readonly IReadOnlyList<string> TempCommands =
    [
        "del /f /s /q \"%SystemRoot%\\Temp\\*\" >nul 2>&1",
        "del /f /s /q \"%SystemDrive%\\Windows\\Temp\\*\" >nul 2>&1",
        "del /f /s /q \"%TEMP%\\*\" >nul 2>&1"
    ];

    public static readonly IReadOnlyList<string> RecycleBinCommands =
    [
        "rd /s /q \"%SystemDrive%\\$Recycle.bin\" >nul 2>&1"
    ];

    public static readonly IReadOnlyList<string> PrefetchCommands =
    [
        "del /f /s /q \"%SystemRoot%\\Prefetch\\*\" >nul 2>&1"
    ];

    public static readonly IReadOnlyList<string> PowerPlanCommands =
    [
        "powercfg -setactive SCHEME_MIN",
        "powercfg -h off"
    ];

    public static readonly IReadOnlyList<RegistryValueDefinition> NetworkAccelerationTweaks =
    [
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpAckFrequency", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TCPNoDelay", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "Tcp1323Opts", 3, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "DefaultTTL", DefaultTcpTtl, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "EnablePMTUBHDetect", 0, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "EnablePMTUDiscovery", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "GlobalMaxTcpWindowSize", MaxTcpWindowSize, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpMaxDupAcks", 2, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "SackOpts", 1, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpTimedWaitDelay", DefaultTcpTimedWaitDelay, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", "MaxCacheTtl", DefaultDnsMaxCacheTtl, RegistryValueKind.DWord),
        Reg("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", "MaxNegativeCacheTtl", DefaultDnsMaxNegativeCacheTtl, RegistryValueKind.DWord)
    ];

    public static readonly IReadOnlyList<string> NetworkOptimizationCommands =
    [
        "ipconfig /flushdns",
        "netsh winsock reset",
        "netsh int ip reset"
    ];

    public static readonly IReadOnlyList<string> ComponentStoreCommands =
    [
        "dism /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
        "del /f /s /q \"%SystemRoot%\\WinSxS\\Temp\\*\" >nul 2>&1"
    ];
}
