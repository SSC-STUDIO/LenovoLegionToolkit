using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using LenovoLegionToolkit.Lib.Utils;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace LenovoLegionToolkit.Plugins.DockLauncher.Services;

/// <summary>
/// Window service implementation
/// </summary>
public class WindowService : IWindowService
{
    public unsafe List<WindowInfo> GetWindowsForProcess(int processId)
    {
        var windows = new List<WindowInfo>();
        var targetProcessId = (uint)processId;
        
        try
        {
            PInvoke.EnumWindows((HWND hWnd, LPARAM lParam) =>
            {
                try
                {
                    uint windowProcessId = 0;
                    PInvoke.GetWindowThreadProcessId(hWnd, &windowProcessId);
                    
                    if (windowProcessId == targetProcessId)
                    {
                        var isVisible = PInvoke.IsWindowVisible(hWnd);
                        if (isVisible)
                        {
                            var windowText = GetWindowText((nint)hWnd);
                            var placement = GetWindowPlacement((nint)hWnd);
                            var isMinimized = placement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMINIMIZED;
                            
                            windows.Add(new WindowInfo
                            {
                                Handle = (nint)hWnd,
                                Title = windowText,
                                ProcessId = (int)windowProcessId,
                                IsMinimized = isMinimized,
                                IsVisible = isVisible
                            });
                        }
                    }
                }
                catch
                {
                    // Ignore errors for individual windows
                }
                
                return true;
            }, new LPARAM(0));
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error getting windows for process {processId}: {ex.Message}", ex);
        }
        
        return windows;
    }

    public bool MinimizeWindow(nint windowHandle)
    {
        try
        {
            PInvoke.ShowWindow((HWND)windowHandle, SHOW_WINDOW_CMD.SW_MINIMIZE);
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error minimizing window: {ex.Message}", ex);
            return false;
        }
    }

    public bool RestoreWindow(nint windowHandle)
    {
        try
        {
            PInvoke.ShowWindow((HWND)windowHandle, SHOW_WINDOW_CMD.SW_RESTORE);
            PInvoke.SetForegroundWindow((HWND)windowHandle);
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error restoring window: {ex.Message}", ex);
            return false;
        }
    }

    public bool IsWindowMinimized(nint windowHandle)
    {
        try
        {
            var placement = GetWindowPlacement(windowHandle);
            return placement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMINIMIZED;
        }
        catch
        {
            return false;
        }
    }

    public bool BringToForeground(nint windowHandle)
    {
        try
        {
            PInvoke.ShowWindow((HWND)windowHandle, SHOW_WINDOW_CMD.SW_RESTORE);
            PInvoke.SetForegroundWindow((HWND)windowHandle);
            return true;
        }
        catch (Exception ex)
        {
            if (Log.Instance.IsTraceEnabled)
                Log.Instance.Trace($"Error bringing window to foreground: {ex.Message}", ex);
            return false;
        }
    }

    private string GetWindowText(nint hWnd)
    {
        try
        {
            var length = PInvoke.GetWindowTextLength((HWND)hWnd);
            if (length == 0)
                return string.Empty;
            
            unsafe
            {
                var buffer = stackalloc char[(int)length + 1];
                PInvoke.GetWindowText((HWND)hWnd, buffer, length + 1);
                return new string(buffer);
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    private WINDOWPLACEMENT GetWindowPlacement(nint hWnd)
    {
        var placement = new WINDOWPLACEMENT
        {
            length = (uint)Marshal.SizeOf<WINDOWPLACEMENT>()
        };
        
        try
        {
            PInvoke.GetWindowPlacement((HWND)hWnd, ref placement);
        }
        catch
        {
            // Return default if failed
        }
        
        return placement;
    }

}

