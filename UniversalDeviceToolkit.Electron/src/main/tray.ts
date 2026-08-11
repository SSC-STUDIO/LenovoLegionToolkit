import { app, BrowserWindow, Menu, Tray, nativeImage, nativeTheme } from 'electron'
import { join } from 'path'

let tray: Tray | null = null

function trayIconPath(): string {
  const dark = nativeTheme.shouldUseDarkColors
  const file = dark ? 'tray-light.png' : 'tray-dark.png'
  // dev: __dirname = out/main; resources live next to the app root
  return join(__dirname, '..', '..', 'resources', file)
}

function updateTrayIcon(): void {
  if (!tray) return
  const image = nativeImage.createFromPath(trayIconPath())
  if (!image.isEmpty()) {
    tray.setImage(image)
  }
}

function showWindow(window: BrowserWindow | null): void {
  if (!window || window.isDestroyed()) return
  if (window.isMinimized()) window.restore()
  window.show()
  window.focus()
}

function toggleWindow(window: BrowserWindow | null): void {
  if (!window || window.isDestroyed()) return
  if (window.isVisible() && !window.isMinimized()) {
    window.hide()
  } else {
    showWindow(window)
  }
}

export interface TrayOptions {
  /** Mirrors the WPF --disable-tray-tooltip flag. */
  disableTooltip?: boolean
}

export function initTray(getWindow: () => BrowserWindow | null, options?: TrayOptions): void {
  if (tray) return

  const image = nativeImage.createFromPath(trayIconPath())
  tray = new Tray(image.isEmpty() ? nativeImage.createEmpty() : image)
  if (!options?.disableTooltip) {
    tray.setToolTip('Universal Device Toolkit')
  }
  tray.setContextMenu(
    Menu.buildFromTemplate([
      { label: '显示 / 隐藏', click: () => toggleWindow(getWindow()) },
      { type: 'separator' },
      // Mirrors the WPF tray StatusWindow: opens the renderer status popup
      // (power mode, sensors, battery, update availability).
      {
        label: 'Status / 状态',
        click: () => {
          const win = getWindow()
          if (!win || win.isDestroyed()) return
          if (win.isMinimized()) win.restore()
          win.show()
          win.focus()
          win.webContents.send('bridge:event', 'tray:status', null)
        }
      },
      { type: 'separator' },
      { label: '退出', click: () => app.quit() }
    ])
  )
  // Mirrors NotifyIcon: WM_LBUTTONUP raises OnClick (bring to foreground) and
  // WM_RBUTTONUP opens the context menu (Electron shows it automatically).
  tray.on('click', () => showWindow(getWindow()))
  tray.on('double-click', () => showWindow(getWindow()))

  nativeTheme.on('updated', updateTrayIcon)
}

export function destroyTray(): void {
  nativeTheme.removeListener('updated', updateTrayIcon)
  if (!tray) return
  tray.destroy()
  tray = null
}
