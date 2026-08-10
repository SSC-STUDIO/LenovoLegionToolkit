import { BrowserWindow, screen } from 'electron'
import { hostClient } from './host-client'

type OsdState = 'Hidden' | 'Show' | 'Toggle'

interface OsdEventData {
  state: OsdState
}

const OSD_WIDTH = 320
const OSD_HEIGHT = 96

let osdWindow: BrowserWindow | null = null
let unsubscribe: (() => void) | null = null

function buildOsdUrl(state: OsdState): string {
  const time = new Date().toLocaleTimeString()
  const html = [
    '<!DOCTYPE html>',
    '<html>',
    '<head>',
    '<meta charset="utf-8">',
    '<style>',
    'html,body{margin:0;padding:0;background:transparent;overflow:hidden;font-family:"Segoe UI",system-ui,sans-serif;}',
    'body{height:100vh;display:flex;flex-direction:column;align-items:center;justify-content:center;}',
    '.title{font-size:20px;font-weight:600;color:#ffffff;}',
    '.meta{font-size:12px;color:rgba(255,255,255,0.75);margin-top:4px;}',
    '</style>',
    '</head>',
    `<body><div class="title">OSD</div><div class="meta">${state} · ${time}</div></body>`,
    '</html>'
  ].join('')
  return `data:text/html;charset=utf-8,${encodeURIComponent(html)}`
}

function positionAtBottomRight(window: BrowserWindow): void {
  const { workArea } = screen.getPrimaryDisplay()
  const [width, height] = window.getSize()
  window.setPosition(
    workArea.x + workArea.width - width - 16,
    workArea.y + workArea.height - height - 16
  )
}

function showOsd(state: OsdState): void {
  const window = osdWindow
  if (!window || window.isDestroyed()) return
  void window.loadURL(buildOsdUrl(state)).then(() => {
    if (!window.isDestroyed()) {
      positionAtBottomRight(window)
      window.show()
    }
  })
}

function handleOsdChanged(data: unknown): void {
  const state = (data as OsdEventData | null)?.state
  if (state === 'Hidden') {
    osdWindow?.hide()
  } else if (state === 'Toggle') {
    if (osdWindow?.isVisible()) {
      osdWindow.hide()
    } else {
      showOsd('Toggle')
    }
  } else if (state === 'Show') {
    showOsd('Show')
  }
}

export function initOsdWindow(): void {
  if (osdWindow && !osdWindow.isDestroyed()) return

  osdWindow = new BrowserWindow({
    width: OSD_WIDTH,
    height: OSD_HEIGHT,
    show: false,
    frame: false,
    transparent: true,
    backgroundColor: '#00000000',
    alwaysOnTop: true,
    skipTaskbar: true,
    resizable: false,
    focusable: false,
    hasShadow: false,
    webPreferences: {
      sandbox: true
    }
  })

  osdWindow.on('closed', () => {
    osdWindow = null
  })

  if (!unsubscribe) {
    unsubscribe = hostClient.on('osd.changed', handleOsdChanged)
  }
}

export function destroyOsdWindow(): void {
  unsubscribe?.()
  unsubscribe = null
  if (osdWindow && !osdWindow.isDestroyed()) {
    osdWindow.destroy()
  }
  osdWindow = null
}
