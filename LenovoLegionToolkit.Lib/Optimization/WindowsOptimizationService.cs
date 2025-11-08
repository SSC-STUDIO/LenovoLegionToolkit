using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using Microsoft.Win32;

using ToolkitRegistry = LenovoLegionToolkit.Lib.System.Registry;

namespace LenovoLegionToolkit.Lib.Optimization;

public class WindowsOptimizationService
{
    private static readonly (string Hive, string SubKey, string ValueName, object Value, RegistryValueKind Kind)[] _registryTweaks =
    [
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowTaskViewButton", 0, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer", "EnableAutoTray", 0, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarGlomLevel", 2, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Control Panel\Desktop", "MenuShowDelay", "0", RegistryValueKind.String),
        ("HKEY_CURRENT_USER", @"Control Panel\Desktop", "AutoEndTasks", "1", RegistryValueKind.String),
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "LaunchTo", 1, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "HideFileExt", 0, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Hidden", 1, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSyncProviderNotifications", 0, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338387Enabled", 0, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-310093Enabled", 0, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightFeatures", 1, RegistryValueKind.DWord),
        ("HKEY_CURRENT_USER", @"Software\Policies\Microsoft\Windows\CloudContent", "DisableSuggestionsWindowsTips", 1, RegistryValueKind.DWord),
        ("HKEY_LOCAL_MACHINE", @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord),
        ("HKEY_LOCAL_MACHINE", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord),
        ("HKEY_LOCAL_MACHINE", @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0, RegistryValueKind.DWord),
        ("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 1, RegistryValueKind.DWord),
        ("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1, RegistryValueKind.DWord),
        ("HKEY_LOCAL_MACHINE", @"SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon", 1, RegistryValueKind.DWord),
        ("HKEY_LOCAL_MACHINE", @"SOFTWARE\Policies\Microsoft\Windows\Explorer", "DisableNotificationCenter", 1, RegistryValueKind.DWord),
        ("HKEY_LOCAL_MACHINE", @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", "TcpTimedWaitDelay", 30, RegistryValueKind.DWord)
    ];

    private static readonly string[] _servicesToDisable =
    [
        "DiagTrack",
        "diagnosticshub.standardcollector.service",
        "DoSvc",
        "RemoteRegistry",
        "SysMain",
        "WSearch",
        "WerSvc"
    ];

    private static readonly string[] _performanceCommandLineTweaks =
    [
        "powercfg -setactive SCHEME_MIN",
        "powercfg -h off"
    ];

    private static readonly string[] _cleanupCommands =
    [
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Terminal Server Client\\Cache\\*\" >nul 2>&1",
        "del /f /s /q \"%SystemRoot%\\SoftwareDistribution\\Download\\*\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\INetCache\\*\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\INetCookies\\*\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db\" >nul 2>&1",
        "del /f /s /q \"%LocalAppData%\\Local\\D3DSCache\\*\" >nul 2>&1",
        "rd /s /q \"%WinDir%\\assembly\\NativeImages_v4.0.30319_32\" >nul 2>&1 & rd /s /q \"%WinDir%\\assembly\\NativeImages_v4.0.30319_64\" >nul 2>&1",
        "del /f /s /q \"%SystemRoot%\\SoftwareDistribution\\DeliveryOptimization\\*\" >nul 2>&1",
        "dism /Online /Cleanup-Image /StartComponentCleanup /ResetBase",
        "powershell -Command \"Get-AppxPackage -AllUsers | Where-Object {$_.Status -eq 'Error'} | Remove-AppxPackage -ErrorAction SilentlyContinue\"",
        "del /f /s /q \"%SystemRoot%\\Logs\\*\" >nul 2>&1",
        "del /f /s /q \"%ProgramData%\\Microsoft\\Windows\\WER\\ReportQueue\\*\" >nul 2>&1",
        "del /f /s /q \"%ProgramData%\\Microsoft\\Diagnosis\\*\" >nul 2>&1",
        "del /f /s /q \"%SystemRoot%\\Minidump\\*.dmp\" >nul 2>&1 & del /f /q \"%SystemRoot%\\memory.dmp\" >nul 2>&1",
        "del /f /s /q \"%ProgramData%\\Microsoft\\Windows Defender\\Scans\\*\" >nul 2>&1",
        "del /f /s /q \"%SystemRoot%\\WinSxS\\Temp\\*\" >nul 2>&1",
        "del /f /s /q \"%SystemRoot%\\Temp\\*\" >nul 2>&1",
        "del /f /s /q \"%SystemDrive%\\*.dmp\" >nul 2>&1",
        "rd /s /q \"%SystemDrive%\\$Recycle.bin\" >nul 2>&1",
        "del /f /s /q \"%SystemDrive%\\Windows\\Temp\\*\" >nul 2>&1 & del /f /s /q \"%SystemRoot%\\Temp\\*\" >nul 2>&1 & del /f /s /q \"%TEMP%\\*\" >nul 2>&1",
        "del /f /s /q \"%SystemRoot%\\Prefetch\\*\" >nul 2>&1"
    ];

    public Task ApplyPerformanceOptimizationsAsync(CancellationToken cancellationToken) =>
        Task.Run(async () =>
        {
            foreach (var tweak in _registryTweaks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ApplyRegistryTweak(tweak);
            }

            foreach (var serviceName in _servicesToDisable.Distinct(StringComparer.InvariantCultureIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                DisableService(serviceName);
            }

            foreach (var command in _performanceCommandLineTweaks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ExecuteCommandLineAsync(command, cancellationToken).ConfigureAwait(false);
            }
        }, cancellationToken);

    public Task RunCleanupAsync(CancellationToken cancellationToken) =>
        Task.Run(async () =>
        {
            foreach (var command in _cleanupCommands)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ExecuteCommandLineAsync(command, cancellationToken).ConfigureAwait(false);
            }
        }, cancellationToken);

    private static void ApplyRegistryTweak((string Hive, string SubKey, string ValueName, object Value, RegistryValueKind Kind) tweak)
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
}

