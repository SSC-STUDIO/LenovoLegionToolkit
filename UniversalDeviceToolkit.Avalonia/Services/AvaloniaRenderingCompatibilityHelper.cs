using Avalonia;
using UniversalDeviceToolkit.Shared.Logging;
#if WINDOWS
using System.Runtime.InteropServices;
using UniversalDeviceToolkit.Lib.Settings;
#endif

namespace UniversalDeviceToolkit.Avalonia.Services;

/// <summary>
/// Ports the WPF <c>RenderingCompatibilityHelper</c> behaviors that apply to
/// Avalonia. Software rendering cannot be toggled at runtime in Avalonia, so
/// the helper decides once, before the windowing subsystem initializes, and
/// configures the Win32 platform options accordingly.
/// </summary>
public static class AvaloniaRenderingCompatibilityHelper
{
    /// <summary>
    /// Applies platform compatibility options to the Avalonia app builder.
    /// Must run before <c>UsePlatformDetect</c> because the Win32 subsystem
    /// captures its options when the extension is invoked. Safe to call on
    /// every platform; non-Windows builds are a no-op.
    /// </summary>
    public static AppBuilder Configure(AppBuilder builder)
    {
#if WINDOWS
        if (IsSoftwareRenderingRequested())
        {
            // Avalonia 11.3: Win32PlatformOptions has no UseSoftwareRendering
            // flag; forcing the rendering mode list to Software only is the
            // supported software-rendering path (RDP / forced software mode).
            builder.With(new Win32PlatformOptions
            {
                RenderingMode = new[] { Win32RenderingMode.Software },
            });
        }
#endif
        return builder;
    }

    /// <summary>
    /// Whether the host should use software rendering for this session.
    /// True when the persisted ForceSoftwareRendering toggle is set or when an
    /// RDP session is detected (WPF parity: remote sessions always render in
    /// software even without the persisted toggle).
    /// </summary>
    public static bool IsSoftwareRenderingRequested()
    {
#if WINDOWS
        try
        {
            if (ShouldForceSoftwareRendering())
                return true;
        }
        catch (Exception ex)
        {
            SharedLog.Warning("Failed to read software-rendering preference; continuing with defaults.", ex);
        }

        if (IsRemoteSession())
        {
            if (SharedLog.IsTraceEnabled)
                SharedLog.Trace("Remote desktop session detected; enabling software rendering.");
            return true;
        }
#endif
        return false;
    }

#if WINDOWS
    /// <summary>
    /// Reads the persisted ForceSoftwareRendering preference from the shared
    /// Windows settings store.
    /// </summary>
    public static bool ShouldForceSoftwareRendering()
    {
        var settings = WindowsAvaloniaSettingsService.SharedApplicationSettings;
        return settings.Store.ForceSoftwareRendering;
    }

    /// <summary>
    /// Detects a remote desktop session via Win32 GetSystemMetrics(SM_REMOTESESSION),
    /// the same signal SystemInformation.TerminalServerSession exposes to WPF.
    /// </summary>
    public static bool IsRemoteSession()
    {
        try
        {
            return GetSystemMetrics(SM_REMOTESESSION) != 0;
        }
        catch (Exception ex)
        {
            SharedLog.Warning("Failed to detect a remote desktop session.", ex);
            return false;
        }
    }

    private const int SM_REMOTESESSION = 0x1000;

    [DllImport("user32.dll", EntryPoint = "GetSystemMetrics")]
    private static extern int GetSystemMetrics(int index);
#endif
}
