/**
 * System power actions — Electron-side counterpart of the Electron Lib power
 * controller (restart/shutdown/sleep). `shutdown.exe` handles elevation the
 * same way the Electron client does (process elevation for power actions).
 */
import { spawn } from 'child_process'

function runShutdown(args: string[]): Promise<{ ok: boolean }> {
  return new Promise((resolve) => {
    const child = spawn('shutdown.exe', args, {
      windowsHide: true,
      detached: true,
      stdio: 'ignore'
    })
    child.on('error', () => resolve({ ok: false }))
    child.on('exit', (code) => resolve({ ok: code === 0 }))
  })
}

export function restartSystem(): Promise<{ ok: boolean }> {
  return runShutdown(['/r', '/t', '0'])
}

export function shutdownSystem(): Promise<{ ok: boolean }> {
  return runShutdown(['/s', '/t', '0'])
}

export function sleepSystem(): Promise<{ ok: boolean }> {
  return runShutdown(['/h'])
}
