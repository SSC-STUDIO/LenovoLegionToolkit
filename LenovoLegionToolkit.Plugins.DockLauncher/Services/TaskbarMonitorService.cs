using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace LenovoLegionToolkit.Plugins.DockLauncher.Services;

/// <summary>
/// Service to monitor taskbar applications and auto-add them to dock
/// </summary>
public class TaskbarMonitorService
{
    private readonly IApplicationService _applicationService;
    private System.Windows.Threading.DispatcherTimer? _monitorTimer;
    private readonly HashSet<string> _trackedProcesses = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public event EventHandler<ApplicationDetectedEventArgs>? ApplicationDetected;
    public event EventHandler<ApplicationClosedEventArgs>? ApplicationClosed;

    public TaskbarMonitorService(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    public void StartMonitoring(int intervalSeconds = 2)
    {
        StopMonitoring();

        _monitorTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(intervalSeconds)
        };
        _monitorTimer.Tick += MonitorTimer_Tick;
        _monitorTimer.Start();

        if (Log.Instance.IsTraceEnabled)
            Log.Instance.Trace($"TaskbarMonitorService: Started monitoring (interval: {intervalSeconds}s)");
    }

    public void StopMonitoring()
    {
        _monitorTimer?.Stop();
        _monitorTimer = null;
    }

    private void MonitorTimer_Tick(object? sender, EventArgs e)
    {
        _ = Task.Run(() => ScanTaskbarApplications());
    }

    private void ScanTaskbarApplications()
    {
        try
        {
            var currentProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allProcesses = Process.GetProcesses();

            foreach (var process in allProcesses)
            {
                try
                {
                    // Skip system processes and processes without windows
                    if (process.MainWindowHandle == IntPtr.Zero)
                        continue;

                    // Skip processes without executable path
                    string? executablePath = null;
                    try
                    {
                        executablePath = process.MainModule?.FileName;
                    }
                    catch
                    {
                        // Access denied or other error
                        continue;
                    }

                    if (string.IsNullOrEmpty(executablePath) || !File.Exists(executablePath))
                        continue;

                    // Skip system processes
                    var processName = process.ProcessName.ToLowerInvariant();
                    if (IsSystemProcess(processName))
                        continue;

                    // Check if window is visible and not minimized
                    if (!IsWindowVisibleAndValid(process.MainWindowHandle))
                        continue;

                    currentProcesses.Add(executablePath);

                    // Check if this is a new process
                    lock (_lock)
                    {
                        if (!_trackedProcesses.Contains(executablePath))
                        {
                            _trackedProcesses.Add(executablePath);
                            
                            var appName = Path.GetFileNameWithoutExtension(executablePath);
                            
                            ApplicationDetected?.Invoke(this, new ApplicationDetectedEventArgs
                            {
                                ExecutablePath = executablePath,
                                ProcessName = appName,
                                ProcessId = process.Id
                            });

                            if (Log.Instance.IsTraceEnabled)
                                Log.Instance.Trace($"TaskbarMonitorService: Detected new application - {appName} ({executablePath})");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"TaskbarMonitorService: Error processing process {process.ProcessName}: {ex.Message}", ex);
                }
                finally
                {
                    process.Dispose();
                }
            }

            // Check for closed processes
            lock (_lock)
            {
                var closedProcesses = _trackedProcesses.Except(currentProcesses).ToList();
                foreach (var closedPath in closedProcesses)
                {
                    _trackedProcesses.Remove(closedPath);
                    
                    ApplicationClosed?.Invoke(this, new ApplicationClosedEventArgs
                    {
                        ExecutablePath = closedPath
                    });

                    if (Log.Instance.IsTraceEnabled)
                        Log.Instance.Trace($"TaskbarMonitorService: Application closed - {closedPath}");
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"TaskbarMonitorService: Error scanning taskbar applications: {ex.Message}", ex);
        }
    }

    private bool IsWindowVisibleAndValid(nint hWnd)
    {
        try
        {
            if (hWnd == IntPtr.Zero)
                return false;

            unsafe
            {
                if (!PInvoke.IsWindowVisible((HWND)hWnd))
                    return false;

                // Check if window has a title (not a background window)
                var length = PInvoke.GetWindowTextLength((HWND)hWnd);
                if (length == 0)
                {
                    // Some valid windows might not have titles, check class name
                    var className = GetWindowClassName((HWND)hWnd);
                    if (string.IsNullOrEmpty(className))
                        return false;
                }

                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    private unsafe string GetWindowClassName(HWND hWnd)
    {
        try
        {
            var buffer = stackalloc char[256];
            var length = PInvoke.GetClassName((HWND)hWnd, buffer, 256);
            if (length > 0)
            {
                return new string(buffer, 0, length);
            }
        }
        catch
        {
            // Ignore errors
        }
        return string.Empty;
    }

    private bool IsSystemProcess(string processName)
    {
        var systemProcesses = new[]
        {
            "dwm", "csrss", "winlogon", "services", "lsass", "svchost",
            "explorer", "sihost", "taskhostw", "dllhost", "runtimebroker",
            "conhost", "smss", "spoolsv", "audiodg", "fontdrvhost",
            "wininit", "winlogon", "wmiprvse", "searchindexer", "searchprotocolhost",
            "searchfilterhost", "mousocoreworker", "applicationframehost",
            "shellexperiencehost", "startmenuexperiencehost", "lockapp",
            "textinputhost", "searchapp", "calculator", "microsoft.photos",
            "microsoft.windows.photos", "microsoft.windows.cortana"
        };

        return systemProcesses.Contains(processName, StringComparer.OrdinalIgnoreCase);
    }

    public List<string> GetTrackedApplications()
    {
        lock (_lock)
        {
            return _trackedProcesses.ToList();
        }
    }
}

/// <summary>
/// Event args for application detected
/// </summary>
public class ApplicationDetectedEventArgs : EventArgs
{
    public string ExecutablePath { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public int ProcessId { get; set; }
}

/// <summary>
/// Event args for application closed
/// </summary>
public class ApplicationClosedEventArgs : EventArgs
{
    public string ExecutablePath { get; set; } = string.Empty;
}


