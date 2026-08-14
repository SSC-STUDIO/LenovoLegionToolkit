/**
 * Login-item autostart for macOS/Linux.
 *
 * Windows autorun is owned by the Host (Autorun.Set scheduled task pointed at
 * the Electron shell via UDT_SHELL_PATH); the app:set-autorun IPC is a no-op
 * there. Linux uses an XDG autostart .desktop file under ~/.config/autostart
 * ("enabled" is the file's existence); macOS uses the Electron login item.
 */
import { app } from 'electron'
import { existsSync, mkdirSync, unlinkSync, writeFileSync } from 'fs'
import { dirname, join } from 'path'

const AUTOSTART_FILE_NAME = 'universal-device-toolkit.desktop'

function linuxAutostartFilePath(): string {
  return join(app.getPath('home'), '.config', 'autostart', AUTOSTART_FILE_NAME)
}

export function applyAutorun(enabled: boolean): void {
  if (process.platform === 'linux') {
    const filePath = linuxAutostartFilePath()
    if (enabled) {
      mkdirSync(dirname(filePath), { recursive: true })
      // Exec quotes the path (spaces in install locations). X-GNOME-Autostart
      // is understood by GNOME; other DEs fall back to the generic Desktop Entry.
      writeFileSync(
        filePath,
        [
          '[Desktop Entry]',
          'Type=Application',
          'Name=Universal Device Toolkit',
          `Exec="${process.execPath}"`,
          'X-GNOME-Autostart-enabled=true'
        ].join('\n') + '\n',
        'utf8'
      )
    } else if (existsSync(filePath)) {
      unlinkSync(filePath)
    }
    return
  }
  // Windows registry Run key / macOS login item via Electron.
  app.setLoginItemSettings({ openAtLogin: enabled })
}

export function readAutorun(): boolean {
  if (process.platform === 'linux') {
    return existsSync(linuxAutostartFilePath())
  }
  return app.getLoginItemSettings().openAtLogin
}
