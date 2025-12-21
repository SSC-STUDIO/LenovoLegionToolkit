using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace LenovoLegionToolkit.Plugins.DockLauncher.Services;

/// <summary>
/// Monitor for window minimize events to trigger dock animations
/// </summary>
public class WindowMinimizeMonitor
{
    private readonly IWindowService _windowService;
    private readonly Dictionary<string, HashSet<nint>> _trackedWindows = new();
    private System.Windows.Threading.DispatcherTimer? _monitorTimer;
    private readonly object _lock = new();

    public event EventHandler<WindowMinimizedEventArgs>? WindowMinimized;

    public WindowMinimizeMonitor(IWindowService windowService)
    {
        _windowService = windowService;
    }

    public void StartMonitoring(string executablePath, int intervalMs = 500)
    {
        lock (_lock)
        {
            if (!_trackedWindows.ContainsKey(executablePath))
            {
                _trackedWindows[executablePath] = new HashSet<nint>();
            }

            if (_monitorTimer == null)
            {
                _monitorTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(intervalMs)
                };
                _monitorTimer.Tick += MonitorTimer_Tick;
                _monitorTimer.Start();
            }
        }
    }

    public void StopMonitoring()
    {
        _monitorTimer?.Stop();
        _monitorTimer = null;
        
        lock (_lock)
        {
            _trackedWindows.Clear();
        }
    }

    private void MonitorTimer_Tick(object? sender, EventArgs e)
    {
        _ = Task.Run(() => CheckMinimizedWindows());
    }

    private void CheckMinimizedWindows()
    {
        try
        {
            lock (_lock)
            {
                foreach (var kvp in _trackedWindows.ToList())
                {
                    var executablePath = kvp.Key;
                    var trackedHandles = kvp.Value;

                    // Get current windows for this executable
                    var processIds = GetProcessIds(executablePath);
                    var currentWindows = new HashSet<nint>();

                    foreach (var processId in processIds)
                    {
                        var windows = _windowService.GetWindowsForProcess(processId);
                        foreach (var window in windows)
                        {
                            currentWindows.Add(window.Handle);

                            // Check if window was just minimized
                            if (window.IsMinimized && !trackedHandles.Contains(window.Handle))
                            {
                                // New minimized window
                                trackedHandles.Add(window.Handle);
                                
                                WindowMinimized?.Invoke(this, new WindowMinimizedEventArgs
                                {
                                    ExecutablePath = executablePath,
                                    WindowHandle = window.Handle
                                });

                                if (Log.Instance.IsTraceEnabled)
                                    Log.Instance.Trace($"WindowMinimizeMonitor: Window minimized - {executablePath}");
                            }
                            else if (!window.IsMinimized && trackedHandles.Contains(window.Handle))
                            {
                                // Window restored, remove from tracking
                                trackedHandles.Remove(window.Handle);
                            }
                        }
                    }

                    // Remove handles that no longer exist
                    trackedHandles.RemoveWhere(h => !currentWindows.Contains(h));
                }
            }
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"WindowMinimizeMonitor: Error checking windows: {ex.Message}", ex);
        }
    }

    private List<int> GetProcessIds(string executablePath)
    {
        try
        {
            var processName = System.IO.Path.GetFileNameWithoutExtension(executablePath);
            var processes = System.Diagnostics.Process.GetProcessesByName(processName);
            var processIds = new List<int>();

            foreach (var process in processes)
            {
                try
                {
                    if (string.Equals(process.MainModule?.FileName, executablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        processIds.Add(process.Id);
                    }
                }
                catch
                {
                    // Ignore access denied
                }
                finally
                {
                    process.Dispose();
                }
            }

            return processIds;
        }
        catch
        {
            return new List<int>();
        }
    }
}

/// <summary>
/// Event args for window minimized
/// </summary>
public class WindowMinimizedEventArgs : EventArgs
{
    public string ExecutablePath { get; set; } = string.Empty;
    public nint WindowHandle { get; set; }
}


