using System.Collections.Generic;

namespace LenovoLegionToolkit.Plugins.DockLauncher.Services;

/// <summary>
/// Window service interface for managing windows
/// </summary>
public interface IWindowService
{
    /// <summary>
    /// Get all windows for a process ID
    /// </summary>
    List<WindowInfo> GetWindowsForProcess(int processId);

    /// <summary>
    /// Minimize a window
    /// </summary>
    bool MinimizeWindow(nint windowHandle);

    /// <summary>
    /// Restore a window
    /// </summary>
    bool RestoreWindow(nint windowHandle);

    /// <summary>
    /// Check if a window is minimized
    /// </summary>
    bool IsWindowMinimized(nint windowHandle);

    /// <summary>
    /// Bring window to foreground
    /// </summary>
    bool BringToForeground(nint windowHandle);
}

/// <summary>
/// Window information
/// </summary>
public class WindowInfo
{
    public nint Handle { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ProcessId { get; set; }
    public bool IsMinimized { get; set; }
    public bool IsVisible { get; set; }
}


