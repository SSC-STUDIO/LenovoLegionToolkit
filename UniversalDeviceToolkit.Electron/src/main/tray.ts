import { app, BrowserWindow, Tray, nativeImage, nativeTheme, type Rectangle } from 'electron'
import { join } from 'path'
import { trayIconSvgForSymbol, trayNavSvg } from './tray-icons'
import { localizePipelineName, setTrayLanguage, trayStrings } from './tray-i18n'
import {
  destroyTrayPopup,
  hideTrayPopup,
  isTrayPopupVisible,
  showTrayPopup,
  type TrayPopupNode
} from './tray-popup'

let tray: Tray | null = null
let getWindow: (() => BrowserWindow | null) | null = null
let invokeHost: ((method: string, params?: unknown) => Promise<unknown>) | null = null
let refreshTimer: ReturnType<typeof setTimeout> | null = null
let building = false
let lastTrayBounds: Rectangle | null = null

interface AutomationPipelineDto {
  id?: string
  iconName?: string | null
  name?: string | null
  trigger?: unknown
}

interface AutomationStateDto {
  pipelines?: AutomationPipelineDto[]
}

interface NavItem {
  route: string
  label: () => string
  iconId: 'home' | 'keyboard' | 'rocket' | 'macro' | 'gauge'
  visibilityKey?: string
}

const NAV_ITEMS: NavItem[] = [
  { route: '/dashboard', label: () => trayStrings().dashboard, iconId: 'home' },
  { route: '/keyboard', label: () => trayStrings().keyboard, iconId: 'keyboard', visibilityKey: 'keyboard' },
  { route: '/automation', label: () => trayStrings().automation, iconId: 'rocket', visibilityKey: 'automation' },
  { route: '/macro', label: () => trayStrings().macro, iconId: 'macro', visibilityKey: 'macro' },
  {
    route: '/optimization',
    label: () => trayStrings().windowsOptimization,
    iconId: 'gauge',
    visibilityKey: 'windowsOptimization'
  }
]

function trayIconPath(): string {
  const dark = nativeTheme.shouldUseDarkColors
  const file = dark ? 'tray-light.png' : 'tray-dark.png'
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

function sendToRenderer(event: string, data: unknown = null): void {
  const win = getWindow?.() ?? null
  if (!win || win.isDestroyed()) return
  win.webContents.send('bridge:event', event, data)
}

function navigate(route: string): void {
  const win = getWindow?.() ?? null
  showWindow(win)
  // Match WPF TrayHelper delay so the window is foreground before navigation.
  setTimeout(() => sendToRenderer('tray:navigate', { route }), 500)
}

async function readNavigationVisibility(): Promise<Record<string, boolean>> {
  if (!invokeHost) return {}
  try {
    const result = (await invokeHost('settings.get', { scope: 'application' })) as
      | {
          value?: {
            navigationItemsVisibility?: Record<string, boolean>
            NavigationItemsVisibility?: Record<string, boolean>
          }
        }
      | null
      | undefined
    return (
      result?.value?.navigationItemsVisibility ??
      result?.value?.NavigationItemsVisibility ??
      {}
    )
  } catch (error) {
    console.error('[tray] failed to read navigation visibility:', error)
    return {}
  }
}

async function readQuickActions(): Promise<AutomationPipelineDto[]> {
  if (!invokeHost) return []
  try {
    const state = (await invokeHost('automation.getState', {})) as AutomationStateDto | null | undefined
    const pipelines = state?.pipelines ?? []
    // WPF TrayHelper: triggerless pipelines only, reversed so defaults appear near Open/Close.
    return pipelines.filter((p) => p.trigger == null).reverse()
  } catch (error) {
    console.error('[tray] failed to read quick actions:', error)
    return []
  }
}

async function runQuickAction(pipelineId: string | undefined): Promise<void> {
  if (!pipelineId || !invokeHost) return
  try {
    await invokeHost('automation.runNow', { pipelineId })
  } catch (error) {
    console.error('[tray] failed to run quick action:', error)
  }
}

function isNavVisible(key: string | undefined, visibility: Record<string, boolean>): boolean {
  if (!key) return true
  if (Object.prototype.hasOwnProperty.call(visibility, key)) return visibility[key] !== false
  return true
}

async function buildPopupNodes(): Promise<TrayPopupNode[]> {
  const s = trayStrings()
  const visibility = await readNavigationVisibility()
  const quickActions = await readQuickActions()
  const nodes: TrayPopupNode[] = []

  nodes.push({ type: 'header', label: 'Universal Device Toolkit' })
  nodes.push({ type: 'separator' })

  for (const nav of NAV_ITEMS) {
    if (!isNavVisible(nav.visibilityKey, visibility)) continue
    nodes.push({
      type: 'item',
      cmd: `nav:${nav.route}`,
      label: nav.label(),
      iconSvg: trayNavSvg(nav.iconId)
    })
  }

  if (quickActions.length > 0) {
    nodes.push({ type: 'separator' })
    for (const pipeline of quickActions) {
      nodes.push({
        type: 'item',
        cmd: `run:${pipeline.id ?? ''}`,
        label: localizePipelineName(pipeline.name),
        iconSvg: trayIconSvgForSymbol(pipeline.iconName ?? 'Play24')
      })
    }
  }

  nodes.push({ type: 'separator' })
  nodes.push({ type: 'item', cmd: 'open', label: s.open })
  nodes.push({ type: 'item', cmd: 'close', label: s.close })

  return nodes
}

function handlePopupCommand(cmd: string): void {
  if (cmd === 'open') {
    showWindow(getWindow?.() ?? null)
    return
  }
  if (cmd === 'close') {
    // Mirrors WPF Resource.Close → App.ShutdownAsync(true).
    app.quit()
    return
  }
  if (cmd.startsWith('nav:')) {
    navigate(cmd.slice(4))
    return
  }
  if (cmd.startsWith('run:')) {
    void runQuickAction(cmd.slice(4))
  }
}

async function openFlyout(bounds: Rectangle): Promise<void> {
  lastTrayBounds = bounds
  if (building) {
    scheduleRebuild(80)
    return
  }
  building = true
  try {
    const nodes = await buildPopupNodes()
    await showTrayPopup(nodes, bounds, handlePopupCommand)
  } catch (error) {
    console.error('[tray] failed to show flyout:', error)
  } finally {
    building = false
  }
}

function scheduleRebuild(delayMs = 50): void {
  if (refreshTimer) clearTimeout(refreshTimer)
  refreshTimer = setTimeout(() => {
    refreshTimer = null
    if (!isTrayPopupVisible() || !lastTrayBounds) return
    void openFlyout(lastTrayBounds)
  }, delayMs)
}

export interface TrayOptions {
  /** Mirrors the WPF --disable-tray-tooltip flag. */
  disableTooltip?: boolean
  invokeHost?: (method: string, params?: unknown) => Promise<unknown>
  /** Initial UI language (renderer `udt.lang` / i18n). Defaults to zh-CN. */
  language?: string
}

export function initTray(getWin: () => BrowserWindow | null, options?: TrayOptions): void {
  if (tray) return

  getWindow = getWin
  invokeHost = options?.invokeHost ?? null
  if (options?.language) setTrayLanguage(options.language)

  const image = nativeImage.createFromPath(trayIconPath())
  tray = new Tray(image.isEmpty() ? nativeImage.createEmpty() : image)
  if (!options?.disableTooltip) {
    tray.setToolTip('Universal Device Toolkit')
  }

  // Custom compact flyout instead of native Menu (Win11 row height is ~44px
  // and cannot be tightened). Left-click still brings the main window forward.
  tray.on('click', () => {
    hideTrayPopup()
    showWindow(getWindow?.() ?? null)
  })
  tray.on('double-click', () => {
    hideTrayPopup()
    showWindow(getWindow?.() ?? null)
  })
  tray.on('right-click', (_event, bounds) => {
    void openFlyout(bounds)
  })

  nativeTheme.on('updated', onNativeThemeUpdated)
}

function onNativeThemeUpdated(): void {
  updateTrayIcon()
  scheduleRebuild()
}

/** Rebuild after host ready / automation save / language change. */
export function refreshTrayMenu(): void {
  scheduleRebuild(0)
}

export function updateTrayLanguage(lang: string): void {
  setTrayLanguage(lang)
  scheduleRebuild()
}

export function destroyTray(): void {
  nativeTheme.removeListener('updated', onNativeThemeUpdated)
  if (refreshTimer) {
    clearTimeout(refreshTimer)
    refreshTimer = null
  }
  destroyTrayPopup()
  if (!tray) return
  tray.destroy()
  tray = null
  getWindow = null
  invokeHost = null
  lastTrayBounds = null
}

// Re-export for callers that only import tray.ts
export { getTrayLanguage, setTrayLanguage } from './tray-i18n'
