import { BrowserWindow, nativeTheme, screen, type Rectangle } from 'electron'

/**
 * Compact tray flyout — HTML popup replacing the native Windows 11 Menu.
 *
 * Win11 native menus lock row height at ~44px and treat captured NativeImage
 * icons as bitmap size (HiDPI capture → oversized, pixelated rows). Electron
 * TrayHelper used a Electron ContextMenu (~26–32px rows). This flyout matches
 * that density:
 *   header 11px / 21px tall, rows 26px, font 12px, icons 14px SVG,
 *   card padding 4px, separator 1px + 3px margins.
 */

export type TrayPopupCommand = string

export type TrayPopupNode =
  | { type: 'header'; label: string }
  | { type: 'separator' }
  | { type: 'item'; cmd: string; label: string; iconSvg?: string }

const SHADOW_GUTTER = 6
const MENU_MIN_WIDTH = 168
const BLUR_CLOSE_GRACE_MS = 180

let popup: BrowserWindow | null = null
let pageLoaded = false
let loadPromise: Promise<void> | null = null
let blurArmed = false
let onCommand: ((cmd: TrayPopupCommand) => void) | null = null

function isDark(): boolean {
  return nativeTheme.shouldUseDarkColors
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}

function renderNodes(nodes: TrayPopupNode[]): string {
  return nodes
    .map((node) => {
      if (node.type === 'header') {
        return `<div class="header">${escapeHtml(node.label)}</div>`
      }
      if (node.type === 'separator') {
        return '<div class="sep" role="separator"></div>'
      }
      const icon = node.iconSvg
        ? `<span class="icon">${node.iconSvg}</span>`
        : '<span class="icon icon--empty"></span>'
      return `<button type="button" class="item" role="menuitem" data-cmd="${escapeHtml(node.cmd)}">${icon}<span class="label">${escapeHtml(node.label)}</span></button>`
    })
    .join('')
}

function pageCss(): string {
  return [
    'html,body{margin:0;padding:0;background:transparent;overflow:hidden;',
    'font-family:"Segoe UI Variable","Segoe UI",system-ui,sans-serif;user-select:none;',
    'box-sizing:border-box;}',
    '*,*::before,*::after{box-sizing:border-box;}',
    'html.dark{color-scheme:dark;',
    '--bg:#2c2c2c;--text:#f3f3f3;--muted:#b0b0b0;--hover:rgba(255,255,255,.08);',
    '--stroke:rgba(255,255,255,.1);--shadow:0 4px 16px rgba(0,0,0,.4);}',
    'html.light{color-scheme:light;',
    '--bg:#ffffff;--text:#1a1a1a;--muted:#6b6b6b;--hover:rgba(0,0,0,.06);',
    '--stroke:rgba(0,0,0,.08);--shadow:0 4px 16px rgba(0,0,0,.18);}',
    `.shell{padding:${SHADOW_GUTTER}px;}`,
    `.menu{min-width:${MENU_MIN_WIDTH}px;max-width:240px;padding:4px;`,
    'border-radius:8px;border:1px solid var(--stroke);background:var(--bg);',
    'box-shadow:var(--shadow);}',
    '.header{padding:3px 8px 2px;font-size:11px;line-height:16px;font-weight:600;',
    'color:var(--muted);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}',
    '.sep{height:1px;margin:3px 6px;background:var(--stroke);}',
    '.item{display:flex;align-items:center;gap:8px;height:26px;width:100%;',
    'padding:0 8px;border:none;border-radius:4px;background:transparent;',
    'color:var(--text);font:inherit;font-size:12px;line-height:16px;',
    'text-align:left;cursor:pointer;-webkit-appearance:none;appearance:none;}',
    '.item:hover,.item:focus-visible{background:var(--hover);outline:none;}',
    '.icon{flex:0 0 14px;width:14px;height:14px;display:flex;align-items:center;',
    'justify-content:center;color:var(--text);}',
    '.icon svg{width:14px;height:14px;display:block;}',
    '.icon--empty{visibility:hidden;}',
    '.label{overflow:hidden;text-overflow:ellipsis;white-space:nowrap;}'
  ].join('')
}

function shellHtml(inner: string): string {
  const theme = isDark() ? 'dark' : 'light'
  return [
    '<!DOCTYPE html><html class="',
    theme,
    '"><head><meta charset="utf-8"><style>',
    pageCss(),
    '</style></head><body><div class="shell"><div class="menu" id="menu" role="menu">',
    inner,
    '</div></div><script>',
    "document.addEventListener('click',function(e){",
    "var el=e.target.closest('[data-cmd]');",
    "if(el) document.title='udt:'+el.getAttribute('data-cmd');",
    '});',
    "document.addEventListener('keydown',function(e){",
    "if(e.key==='Escape') document.title='udt:hide';",
    '});',
    '</script></body></html>'
  ].join('')
}

function ensureWindow(): BrowserWindow {
  if (popup && !popup.isDestroyed()) return popup

  pageLoaded = false
  loadPromise = null
  popup = new BrowserWindow({
    width: MENU_MIN_WIDTH + SHADOW_GUTTER * 2,
    height: 200,
    show: false,
    frame: false,
    transparent: true,
    backgroundColor: '#00000000',
    alwaysOnTop: true,
    skipTaskbar: true,
    resizable: false,
    focusable: true,
    hasShadow: false,
    minimizable: false,
    maximizable: false,
    fullscreenable: false,
    webPreferences: {
      sandbox: true
    }
  })

  popup.setMenu(null)
  popup.on('closed', () => {
    popup = null
    pageLoaded = false
    loadPromise = null
    blurArmed = false
  })
  popup.on('blur', () => {
    if (blurArmed) hideTrayPopup()
  })
  popup.on('page-title-updated', (event, title) => {
    event.preventDefault()
    if (!title.startsWith('udt:')) return
    const cmd = title.slice(4)
    void popup?.webContents.executeJavaScript("document.title='udt-tray'").catch(() => undefined)
    if (cmd === 'hide') {
      hideTrayPopup()
      return
    }
    hideTrayPopup()
    onCommand?.(cmd)
  })

  return popup
}

async function loadShell(win: BrowserWindow): Promise<void> {
  if (pageLoaded) return
  if (!loadPromise) {
    loadPromise = win
      .loadURL(`data:text/html;charset=utf-8,${encodeURIComponent(shellHtml(''))}`)
      .then(() => {
        pageLoaded = true
      })
      .catch(() => undefined)
      .finally(() => {
        loadPromise = null
      })
  }
  await loadPromise
}

async function setMenuHtml(win: BrowserWindow, inner: string): Promise<void> {
  await loadShell(win)
  if (win.isDestroyed()) return
  const theme = isDark() ? 'dark' : 'light'
  await win.webContents.executeJavaScript(
    `document.documentElement.className=${JSON.stringify(theme)};` +
      `document.getElementById('menu').innerHTML=${JSON.stringify(inner)};`
  )
}

async function fitToContent(win: BrowserWindow): Promise<void> {
  try {
    const size = (await win.webContents.executeJavaScript(
      '[document.body.scrollWidth, document.body.scrollHeight]'
    )) as [number, number]
    const width = Math.max(1, Math.round(size[0]))
    const height = Math.max(1, Math.round(size[1]))
    if (!win.isDestroyed()) win.setContentSize(width, height)
  } catch {
    // Keep the last size if measurement fails.
  }
}

function positionNearTray(win: BrowserWindow, trayBounds: Rectangle): void {
  const anchor = {
    x: Math.round(trayBounds.x + trayBounds.width / 2),
    y: Math.round(trayBounds.y + trayBounds.height / 2)
  }
  const { workArea } = screen.getDisplayNearestPoint(anchor)
  const [width, height] = win.getSize()
  // Align the flyout's trailing edge with the tray icon; prefer opening above
  // (taskbar at bottom) and flip below if the tray sits at the top.
  let x = trayBounds.x + trayBounds.width - width + SHADOW_GUTTER
  let y = trayBounds.y - height + SHADOW_GUTTER
  if (y < workArea.y) {
    y = trayBounds.y + trayBounds.height - SHADOW_GUTTER
  }
  x = Math.max(workArea.x, Math.min(x, workArea.x + workArea.width - width))
  y = Math.max(workArea.y, Math.min(y, workArea.y + workArea.height - height))
  win.setPosition(Math.round(x), Math.round(y))
}

export function isTrayPopupVisible(): boolean {
  return popup != null && !popup.isDestroyed() && popup.isVisible()
}

export function hideTrayPopup(): void {
  blurArmed = false
  if (popup && !popup.isDestroyed() && popup.isVisible()) {
    popup.hide()
  }
}

export function destroyTrayPopup(): void {
  blurArmed = false
  onCommand = null
  if (popup && !popup.isDestroyed()) {
    popup.destroy()
  }
  popup = null
  pageLoaded = false
  loadPromise = null
}

/** Create the hidden flyout window at tray init so the first right-click is instant. */
export function prewarmTrayPopup(): void {
  const win = ensureWindow()
  void loadShell(win)
}

export async function showTrayPopup(
  nodes: TrayPopupNode[],
  trayBounds: Rectangle,
  handler: (cmd: TrayPopupCommand) => void
): Promise<void> {
  onCommand = handler
  const win = ensureWindow()
  const inner = renderNodes(nodes)
  await setMenuHtml(win, inner)
  if (win.isDestroyed()) return
  await fitToContent(win)
  if (win.isDestroyed()) return
  positionNearTray(win, trayBounds)
  blurArmed = false
  if (!win.isVisible()) win.show()
  win.focus()
  setTimeout(() => {
    blurArmed = true
  }, BLUR_CLOSE_GRACE_MS)
}
