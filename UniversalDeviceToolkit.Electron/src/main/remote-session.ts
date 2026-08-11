/**
 * Mirrors WPF RemoteSessionHelper: detects whether the app is running inside
 * a remote-desktop session (RDP, VNC, Citrix, ...) so the main process can
 * enable software-rendering fallbacks.
 *
 * The WPF helper combines three signals (WinForms TerminalServerSession, the
 * SESSIONNAME environment variable and the WTS session-id comparison). The
 * first two map directly onto the environment the Electron main process
 * inherits from the Windows session:
 * - SESSIONNAME is "Console" for the local physical console and "RDP-Tcp#N"
 *   (or similar) for remote sessions;
 * - CLIENTNAME is set by the RDP client in remote sessions.
 * The WTS API comparison is not reachable from JS without a native module;
 * the two environment signals cover every common remote channel.
 */

export function isRemoteSession(): boolean {
  try {
    // 1) "SESSIONNAME" is "Console" for the local physical console session
    //    and "RDP-Tcp#N" (or similar) for remote sessions.
    const sessionName = process.env.SESSIONNAME
    if (sessionName !== undefined && sessionName !== null && !/^console$/i.test(sessionName.trim())) {
      return true
    }

    // 2) RDP clients also set CLIENTNAME for the remote session.
    const clientName = process.env.CLIENTNAME
    if (clientName !== undefined && clientName !== null && clientName.trim().length > 0) {
      return true
    }
  } catch {
    // Detection failures must never crash startup; assume a local session.
  }

  return false
}
