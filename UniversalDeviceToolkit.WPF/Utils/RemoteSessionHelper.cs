using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UniversalDeviceToolkit.Lib.Utils;

namespace UniversalDeviceToolkit.WPF.Utils;

/// <summary>
/// Detects whether the application is running inside a remote-desktop session
/// (RDP, VNC, Citrix, etc.). Used to enable software-rendering fallbacks and to
/// avoid expensive operations that perform poorly under remote-display protocols.
/// </summary>
internal static class RemoteSessionHelper
{
    /// <summary>
    /// True when the current process is running in a session that is not the
    /// local console session (i.e. RDP / VNC / terminal-services session).
    /// </summary>
    public static bool IsRemoteSession
    {
        get
        {
            try
            {
                // 1) WinForms exposes the most reliable detector:
                //    true when the local input desktop is not the same as the
                //    session desktop, which is the case for every RDP client.
                if (SystemInformation.TerminalServerSession)
                    return true;

                // 2) "SESSIONNAME" is "Console" for the local physical console
                //    session and "RDP-Tcp#N" (or similar) for remote sessions.
                var name = Environment.GetEnvironmentVariable("SESSIONNAME");
                if (!string.IsNullOrEmpty(name) &&
                    !string.Equals(name, "Console", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // 3) The WTS API exposes the session ID. Session 0 is the
                //    services session; for interactive desktop sessions the
                //    WTSGetActiveConsoleSessionId returns the local console.
                //    If GetCurrentSessionId differs from the console session
                //    id, we are attached through a remote-control channel.
                if (GetCurrentSessionId() != WTSGetActiveConsoleSessionId())
                    return true;
            }
            catch (Exception ex)
            {
                // Detection failures must never crash startup.
                Log.Instance.TraceOnce(
                    "remote-session-detect",
                    "Remote session detection failed; assuming local session.",
                    ex);
            }

            return false;
        }
    }

    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("kernel32.dll")]
    private static extern uint WTSGetCurrentProcessId();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ProcessIdToSessionId(uint processId, out uint sessionId);

    private static uint GetCurrentSessionId()
    {
        try
        {
            ProcessIdToSessionId(WTSGetCurrentProcessId(), out var sessionId);
            return sessionId;
        }
        catch (Exception ex)
        {
            Log.Instance.TraceOnce(
                "remote-session-id",
                "ProcessIdToSessionId failed during remote session detection.",
                ex);
            return 0;
        }
    }
}
