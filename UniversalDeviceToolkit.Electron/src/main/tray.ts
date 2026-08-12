import { app, BrowserWindow, Menu, Tray, nativeImage, nativeTheme, type MenuItemConstructorOptions } from 'electron'
import { join } from 'path'
import { clearTrayIconCache, trayIconForSymbol, trayNavIcon } from './tray-icons'
import { localizePipelineName, setTrayLanguage, trayStrings } from './tray-i18n'

let tray: Tray | null = null
let getWindow: (() => BrowserWindow | null) | null = null
let invokeHost: ((method: string, params?: unknown) => Promise<unknown>) | null = null
let refreshTimer: ReturnType<typeof setTimeout> | null = null
let building = false

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

async function buildMenuTemplate(): Promise<MenuItemConstructorOptions[]> {
  const s = trayStrings()
  const visibility = await readNavigationVisibility()
  const quickActions = await readQuickActions()

  const items: MenuItemConstructorOptions[] = []

  for (const nav of NAV_ITEMS) {
    if (!isNavVisible(nav.visibilityKey, visibility)) continue
    items.push({
      label: nav.label(),
      icon: trayNavIcon(nav.iconId),
      click: () => navigate(nav.route)
    })
  }

  items.push({ type: 'separator' })

  // WPF TrayHelper.SetAutomationItemsAsync: the automation block gets its own
  // separator, inserted after the navigation separator and before Open/Close.
  if (quickActions.length > 0) {
    items.push({ type: 'separator' })
  }
  for (const pipeline of quickActions) {
    items.push({
      label: localizePipelineName(pipeline.name),
      icon: trayIconForSymbol(pipeline.iconName ?? 'Play24'),
      click: () => {
        void runQuickAction(pipeline.id)
      }
    })
  }

  // Open / Close are text-only (no icons), matching WPF TrayHelper / Fig 2.
  items.push({
    label: s.open,
    click: () => showWindow(getWindow?.() ?? null)
  })

  items.push({
    label: s.close,
    click: () => {
      // Mirrors WPF Resource.Close → App.ShutdownAsync(true).
      app.quit()
    }
  })

  return items
}

async function rebuildContextMenu(): Promise<void> {
  if (!tray) return
  if (building) {
    scheduleRebuild(100)
    return
  }
  building = true
  try {
    const template = await buildMenuTemplate()
    if (!tray) return
    tray.setContextMenu(Menu.buildFromTemplate(template))
  } catch (error) {
    console.error('[tray] failed to rebuild context menu:', error)
  } finally {
    building = false
  }
}

function scheduleRebuild(delayMs = 50): void {
  if (refreshTimer) clearTimeout(refreshTimer)
  refreshTimer = setTimeout(() => {
    refreshTimer = null
    void rebuildContextMenu()
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

  // Placeholder until async rebuild finishes (avoids empty right-click).
  tray.setContextMenu(
    Menu.buildFromTemplate([
      { label: trayStrings().open, click: () => showWindow(getWindow?.() ?? null) },
      { label: trayStrings().close, click: () => app.quit() }
    ])
  )

  // Mirrors NotifyIcon: WM_LBUTTONUP raises OnClick (bring to foreground) and
  // WM_RBUTTONUP opens the context menu (Electron shows it automatically).
  tray.on('click', () => showWindow(getWindow?.() ?? null))
  tray.on('double-click', () => showWindow(getWindow?.() ?? null))

  nativeTheme.on('updated', onNativeThemeUpdated)

  scheduleRebuild(0)
}

function onNativeThemeUpdated(): void {
  clearTrayIconCache()
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
  if (!tray) return
  tray.destroy()
  tray = null
  getWindow = null
  invokeHost = null
  clearTrayIconCache()
}

// Re-export for callers that only import tray.ts
export { getTrayLanguage, setTrayLanguage } from './tray-i18n'
