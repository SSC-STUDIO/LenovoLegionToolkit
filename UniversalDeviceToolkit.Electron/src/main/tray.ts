import { app, BrowserWindow, Menu, Tray, nativeImage } from 'electron'

let tray: Tray | null = null

const TRAY_ICON_DATA_URL =
  'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAADeSURBVDhPtZIrEsIwEIYrkUgkEomDJqI9AhKJRHKDNFvBETgCR0AiOQKyEtkZpruRZdJ2OnSTPhD8M5/a/feRbBD8U6F6r2RqYqGKNY/1KlLlTIA5C0AjgcoOGq+RKhbc08p2kkBPx/iFAHrZqbi36iwBM27wIYByZxIJdOGJQwhNt053784jbJRZVgWa3Z2EMULAXV1A45EHJ6ExqfdPTewEJyCgODRvkM95cAqdAxNAd54wDGat2cqe7U8/4TumEGg/qYjGE/e2qiehh2MaOmOfpMJt9b0aE9uxz/gBIPu9+GgGzAUAAAAASUVORK5CYII='

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

  const icon = nativeImage.createFromDataURL(TRAY_ICON_DATA_URL)
  tray = new Tray(icon)
  tray.setToolTip('Universal Device Toolkit')
  tray.setContextMenu(
    Menu.buildFromTemplate([
      { label: '显示 / 隐藏', click: () => toggleWindow(getWindow()) },
      { type: 'separator' },
      { label: '退出', click: () => app.quit() }
    ])
  )
  tray.on('double-click', () => showWindow(getWindow()))
}

export function destroyTray(): void {
  if (!tray) return
  tray.destroy()
  tray = null
}
