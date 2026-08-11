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

export function initTray(getWindow: () => BrowserWindow | null): void {
  if (tray) return

  const image = nativeImage.createFromPath(trayIconPath())
  tray = new Tray(image.isEmpty() ? nativeImage.createEmpty() : image)
  tray.setToolTip('Universal Device Toolkit')
  tray.setContextMenu(
    Menu.buildFromTemplate([
      { label: '显示 / 隐藏', click: () => toggleWindow(getWindow()) },
      { type: 'separator' },
      { label: '退出', click: () => app.quit() }
    ])
  )
  tray.on('double-click', () => showWindow(getWindow()))

  nativeTheme.on('updated', updateTrayIcon)
}

export function destroyTray(): void {
  nativeTheme.removeListener('updated', updateTrayIcon)
  if (!tray) return
  tray.destroy()
  tray = null
}
