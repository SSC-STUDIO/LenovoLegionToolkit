import { app, BrowserWindow, clipboard, dialog, ipcMain, nativeTheme, powerMonitor, shell } from 'electron'
import { join, dirname } from 'path'
import { existsSync, mkdirSync, unlinkSync, writeFileSync } from 'fs'
import { tmpdir } from 'os'
import { hostClient } from './host-client'
import {
  initMainLogger,
  isValidLogLevel,
  logsDirectory,
  writeHostLog,
  writeRendererLog
} from './logger'
import { initSingleInstance, setMainWindowRef } from './single-instance'
import { initTray, destroyTray, refreshTrayMenu, updateTrayLanguage } from './tray'
import { initOsdWindow, destroyOsdWindow } from './osd-window'
import { initStatusWindow, destroyStatusWindow, showStatusWindow } from './status-window'
import { flags, describeFlags, toHostArgs } from './flags'
import { attachResizeStability, attachMaximizeWorkAreaClamp } from './window-helpers'
import { listPowerPlans, setActivePowerPlan } from './power-plans'
import { restartSystem, shutdownSystem, sleepSystem } from './system-power'
import { downloadLatestUpdate, getLatestRelease, launchInstaller, type DownloadProgress } from './update-downloader'
import { installApplicationMenu } from './menu'

// The Windows AppUserModelId (taskbar grouping, notifications) is meaningless
// on macOS/Linux; setting it there is a no-op, so keep the call Windows-only.
if (process.platform === 'win32') {
  app.setAppUserModelId('com.universaldevicetoolkit.app')
}

// Force the app name to use the product name everywhere (taskbar window preview,
// notifications, dev tools, etc.) instead of falling back to package.json name which
// still ends in '-electron' and would surface as a separate 'Electron' window in the
// Windows 11 taskbar window preview alongside the main app.
app.setName('Universal Device Toolkit')
// Disable the noisy Chromium DevTools shortcut defaults; the app never opens DevTools
// in production, and an unsuppressed F12 would spawn a hidden 'Electron' frame that the
// taskbar window preview surfaces as a third entry next to the main window.
app.commandLine.appendSwitch('disable-features', 'OutOfBlinkCors')
// --single-process merges renderers into the main process so memory usage can be
// inspected as a single entry (debug/dev only).
if (flags.singleProcess) {
  app.commandLine.appendSwitch('single-process')
}

// Electron renders in DIPs while Chromium applies the Windows display scale to CSS.
// This keeps the Electron renderer at the original client's physical density.
// The 5/6 correction is Windows-only (Windows display scale vs. client DPI);
// Linux/macOS map one CSS px to one DIP, so the zoom factor stays 1 there.
const RENDERER_ZOOM_FACTOR = process.platform === 'win32' ? 5 / 6 : 1
const PROJECT_ROOT = join(__dirname, '..', '..')

if (!initSingleInstance()) {
  app.exit(0)
}

let mainWindow: BrowserWindow | null = null
let isQuitting = false

function resolveHostPath(): string {
  const fromEnv = process.env['UDT_HOST_PATH']
  if (fromEnv) {
    if (!existsSync(fromEnv)) {
      throw new Error(`UDT_HOST_PATH does not exist: ${fromEnv}`)
    }
    return fromEnv
  }

  // __dirname = <project>/out/main -> project root is two levels up.
  // The .NET Host is a console binary: UniversalDeviceToolkit.Host.exe on
  // Windows, extension-less executable on Linux/macOS.
  const hostExeName =
    process.platform === 'win32' ? 'UniversalDeviceToolkit.Host.exe' : 'UniversalDeviceToolkit.Host'
  const tfm = 'net10.0-windows10.0.26100.0'
  const candidates: string[] = []

  // Packaged builds only: Host is copied into resources/host by electron-builder.
  // In dev, process.resourcesPath points at Electron's own resources and must not
  // take priority over the sibling Host project output.
  if (app.isPackaged) {
    candidates.push(join(process.resourcesPath, 'host', hostExeName))
  }

  // Dev: sibling repo folder next to the Electron project (Windows layout),
  // staged publish folders for every platform, and a local host/ staging dir.
  if (process.platform === 'win32') {
    candidates.push(
      join(PROJECT_ROOT, '..', 'UniversalDeviceToolkit.Host', 'bin', 'x64', 'Debug',
        tfm, 'win-x64', hostExeName),
      join(PROJECT_ROOT, '..', 'UniversalDeviceToolkit.Host', 'bin', 'x64', 'Release',
        tfm, 'win-x64', hostExeName),
      // CI/installer staging: Release.yml publishes the Host here and
      // electron-builder copies resources/host from it.
      join(PROJECT_ROOT, '..', 'UniversalDeviceToolkit.Host', 'publish', 'win-x64', hostExeName)
    )
  } else if (process.platform === 'darwin') {
    candidates.push(
      join(PROJECT_ROOT, '..', 'UniversalDeviceToolkit.Host', 'publish', `osx-${process.arch}`, hostExeName),
      join(PROJECT_ROOT, '..', 'UniversalDeviceToolkit.Host', 'publish', 'osx-x64', hostExeName),
      join(PROJECT_ROOT, '..', 'UniversalDeviceToolkit.Host', 'publish', 'osx-arm64', hostExeName)
    )
  } else {
    candidates.push(
      join(PROJECT_ROOT, '..', 'UniversalDeviceToolkit.Host', 'publish', `linux-${process.arch}`, hostExeName),
      join(PROJECT_ROOT, '..', 'UniversalDeviceToolkit.Host', 'publish', 'linux-x64', hostExeName),
      join(PROJECT_ROOT, '..', 'UniversalDeviceToolkit.Host', 'publish', 'linux-arm64', hostExeName)
    )
  }
  candidates.push(
    // fallback: explicit build output inside this project / staged publish folder
    join(PROJECT_ROOT, 'host', hostExeName)
  )

  for (const candidate of candidates) {
    if (existsSync(candidate)) return candidate
  }

  throw new Error(
    `Host executable not found. Build UniversalDeviceToolkit.Host or set UDT_HOST_PATH.\n` +
      candidates.map((path) => `  - ${path}`).join('\n')
  )
}

/** Events that arrived before the BrowserWindow could receive IPC. */
const bufferedHostEvents: Array<{ event: string; data: unknown }> = []
const BUFFERED_HOST_EVENT_LIMIT = 64
let hostEventsAttached = false

function sendHostEventToWebviewGuests(event: string, data: unknown): void {
  if (!mainWindow || mainWindow.isDestroyed()) return
  const { webContents } = require('electron') as typeof import('electron')
  for (const webContentsItem of webContents.getAllWebContents()) {
    if (webContentsItem === mainWindow.webContents) continue
    if (webContentsItem.getType() === 'webview' && !webContentsItem.isDestroyed()) {
      webContentsItem.send('plugin-host:event', event, data)
    }
  }
}

function bufferOrSendHostEvent(event: string, data: unknown): void {
  sendHostEventToWebviewGuests(event, data)
  if (mainWindow && !mainWindow.isDestroyed() && !mainWindow.webContents.isLoadingMainFrame()) {
    mainWindow.webContents.send('bridge:event', event, data)
    return
  }
  bufferedHostEvents.push({ event, data })
  if (bufferedHostEvents.length > BUFFERED_HOST_EVENT_LIMIT) {
    bufferedHostEvents.shift()
  }
}

/**
 * Bridges plugin web pages (hosted in <webview> guests) to the host JSON-RPC
 * backend. The guest preload (plugin-host.ts) sends `plugin-host:invoke`
 * messages via sendToHost; responses are pushed back into the guest frame.
 */
function attachPluginHostBridge(): void {
  if (!mainWindow) return
  mainWindow.webContents.on('ipc-message', (event, channel, ...args) => {
    if (channel !== 'plugin-host:invoke') return
    const [id, method, params] = args as [number, string, unknown]
    hostClient
      .invoke(method, params)
      .then(
        (result) => event.senderFrame?.send('plugin-host:response', id, result, null),
        (error: Error) => event.senderFrame?.send('plugin-host:response', id, null, error.message)
      )
  })
}

function flushBufferedHostEvents(window: BrowserWindow): void {
  if (window.isDestroyed()) return
  // Always re-emit the latest host.ready so late renderer subscribers (dashboard
  // waitForHost, title bar, startup gates) observe readiness after a fast Host boot.
  const readyPayload = hostClient.lastReadyPayload
  if (hostClient.isReady) {
    window.webContents.send('bridge:event', 'host.ready', readyPayload ?? {})
  }
  for (const item of bufferedHostEvents) {
    if (item.event === 'host.ready') continue
    window.webContents.send('bridge:event', item.event, item.data)
  }
  bufferedHostEvents.length = 0
}

function attachHostEventForwarding(): void {
  if (hostEventsAttached) return
  hostEventsAttached = true
  // Forward all host events (sensors.updated, settings.changed, osd.changed, …).
  // A fixed whitelist previously dropped sensors.updated, so gauges stayed at "-".
  hostClient.onAny((event, data) => {
    // Aggregate host log lines into the unified log folder.
    if (event === 'host.log') {
      writeHostLog(typeof data === 'string' ? data : JSON.stringify(data))
    }
    bufferOrSendHostEvent(event, data)
  })
}

function forwardHostEvents(window: BrowserWindow): void {
  attachHostEventForwarding()
  const flush = (): void => flushBufferedHostEvents(window)
  window.webContents.on('did-finish-load', flush)
  if (!window.webContents.isLoadingMainFrame()) {
    flush()
  }
}

type MinimizeSetting = 'MinimizeOnClose' | 'MinimizeToTray'

async function shouldMinimizeToTray(keys: MinimizeSetting[]): Promise<boolean> {
  try {
    const result = (await hostClient.invoke('settings.get', { scope: 'application' })) as
      | { value?: Record<string, unknown> }
      | null
      | undefined
    return keys.some((key) => result?.value?.[key] === true)
  } catch (error) {
    console.error('[main] failed to read settings:', error)
    return false
  }
}

function resolveWindowIcon(): string | undefined {
  // Windows uses the multi-size ICO; Linux/macOS need a PNG (ICO is ignored).
  if (process.platform === 'win32') {
    const candidates = [
      join(PROJECT_ROOT, 'resources', 'icon.ico'),
      join(PROJECT_ROOT, 'buildResources', 'icon.ico'),
      join(PROJECT_ROOT, 'resources', 'icon.png')
    ]
    return candidates.find((candidate) => existsSync(candidate))
  }
  const candidates = [
    join(PROJECT_ROOT, 'resources', 'icon.png'),
    join(PROJECT_ROOT, 'buildResources', 'icon-512.png')
  ]
  return candidates.find((candidate) => existsSync(candidate))
}

/** Device info shape — mirrors Electron MachineCompatibility.MachineInformation. */
interface DeviceInfo {
  vendor: string
  model: string
  machineType: string
  serialNumber: string
  biosVersion: string
  processor?: DeviceInfoProcessor | null
  videoController?: DeviceInfoVideoController | null
  memory?: DeviceInfoMemory | null
  warranty?: DeviceInfoWarranty | null
}

interface DeviceInfoProcessor {
  name?: string | null
  numberOfCores?: number | null
  numberOfLogicalProcessors?: number | null
  maxClockSpeedMHz?: number | null
}

interface DeviceInfoVideoController {
  name?: string | null
  adapterCompatibility?: string | null
  adapterRamBytes?: number | null
}

interface DeviceInfoMemory {
  totalCapacityBytes?: number | null
  moduleCount?: number | null
  configuredClockSpeedMHz?: number | null
  speedMHz?: number | null
}

interface DeviceInfoWarranty {
  startDate?: string | null
  endDate?: string | null
  link?: string | null
}

interface DeviceInfoHardware {
  processor?: DeviceInfoProcessor | null
  videoController?: DeviceInfoVideoController | null
  memory?: DeviceInfoMemory | null
}

const FALLBACK_DEVICE_INFO: DeviceInfo = {
  vendor: '',
  model: 'Universal Device Toolkit',
  machineType: '',
  serialNumber: '',
  biosVersion: ''
}

function sanitizeProcessor(value: unknown): DeviceInfoProcessor | null {
  if (!value || typeof value !== 'object') return null
  const source = value as Record<string, unknown>
  const processor: DeviceInfoProcessor = {}
  if (typeof source.name === 'string' && source.name.length > 0) processor.name = source.name
  if (typeof source.numberOfCores === 'number') processor.numberOfCores = source.numberOfCores
  if (typeof source.numberOfLogicalProcessors === 'number') {
    processor.numberOfLogicalProcessors = source.numberOfLogicalProcessors
  }
  if (typeof source.maxClockSpeedMHz === 'number') processor.maxClockSpeedMHz = source.maxClockSpeedMHz
  return processor.name ? processor : null
}

function sanitizeVideoController(value: unknown): DeviceInfoVideoController | null {
  if (!value || typeof value !== 'object') return null
  const source = value as Record<string, unknown>
  const videoController: DeviceInfoVideoController = {}
  if (typeof source.name === 'string' && source.name.length > 0) videoController.name = source.name
  if (typeof source.adapterCompatibility === 'string') {
    videoController.adapterCompatibility = source.adapterCompatibility
  }
  if (typeof source.adapterRamBytes === 'number') videoController.adapterRamBytes = source.adapterRamBytes
  return videoController.name ? videoController : null
}

function sanitizeMemory(value: unknown): DeviceInfoMemory | null {
  if (!value || typeof value !== 'object') return null
  const source = value as Record<string, unknown>
  const memory: DeviceInfoMemory = {}
  if (typeof source.totalCapacityBytes === 'number') memory.totalCapacityBytes = source.totalCapacityBytes
  if (typeof source.moduleCount === 'number') memory.moduleCount = source.moduleCount
  if (typeof source.configuredClockSpeedMHz === 'number') {
    memory.configuredClockSpeedMHz = source.configuredClockSpeedMHz
  }
  if (typeof source.speedMHz === 'number') memory.speedMHz = source.speedMHz
  return memory.totalCapacityBytes || memory.moduleCount || memory.configuredClockSpeedMHz || memory.speedMHz
    ? memory
    : null
}

function sanitizeWarranty(value: unknown): DeviceInfoWarranty | null {
  if (!value || typeof value !== 'object') return null
  const source = value as Record<string, unknown>
  const warranty: DeviceInfoWarranty = {}
  if (typeof source.startDate === 'string') warranty.startDate = source.startDate
  if (typeof source.endDate === 'string') warranty.endDate = source.endDate
  if (typeof source.link === 'string') warranty.link = source.link
  return warranty.startDate || warranty.endDate || warranty.link ? warranty : null
}

/** Best-effort device info via the host's system.info; never throws. */
async function getDeviceInfo(): Promise<DeviceInfo> {
  try {
    const result = (await hostClient.invoke('system.info', {})) as
      | (Partial<DeviceInfo> & { hardware?: DeviceInfoHardware | null })
      | null
      | undefined
    if (!result || typeof result !== 'object') return { ...FALLBACK_DEVICE_INFO }
    const hardware = result.hardware && typeof result.hardware === 'object' ? result.hardware : null
    return {
      vendor: typeof result.vendor === 'string' ? result.vendor : '',
      model:
        typeof result.model === 'string' && result.model.length > 0
          ? result.model
          : FALLBACK_DEVICE_INFO.model,
      machineType: typeof result.machineType === 'string' ? result.machineType : '',
      serialNumber: typeof result.serialNumber === 'string' ? result.serialNumber : '',
      biosVersion: typeof result.biosVersion === 'string' ? result.biosVersion : '',
      processor: sanitizeProcessor(hardware?.processor),
      videoController: sanitizeVideoController(hardware?.videoController),
      memory: sanitizeMemory(hardware?.memory),
      warranty: sanitizeWarranty(result.warranty)
    }
  } catch (error) {
    console.error('[main] failed to load device info:', error)
    return { ...FALLBACK_DEVICE_INFO }
  }
}

/**
 * Mirrors Electron MainWindow.OpenLog: reveal the logs folder in Explorer. Electron's
 * "logs" path is created on demand so the folder exists before the host has
 * written anything.
 */
async function openLogFolder(): Promise<{ ok: boolean }> {
  try {
    const dir = app.getPath('logs')
    if (!existsSync(dir)) mkdirSync(dir, { recursive: true })
    const error = await shell.openPath(dir)
    return { ok: error.length === 0 }
  } catch (error) {
    console.error('[main] failed to open log folder:', error)
    return { ok: false }
  }
}

/**
 * Linux autostart entry — mirrors the Windows registry Run key / macOS login
 * item. XDG autostart .desktop file under ~/.config/autostart; "Enabled" is
 * the file's existence.
 */
const AUTOSTART_FILE_NAME = 'universal-device-toolkit.desktop'

function linuxAutostartFilePath(): string {
  return join(app.getPath('home'), '.config', 'autostart', AUTOSTART_FILE_NAME)
}

function applyAutorun(enabled: boolean): void {
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

function readAutorun(): boolean {
  if (process.platform === 'linux') {
    return existsSync(linuxAutostartFilePath())
  }
  return app.getLoginItemSettings().openAtLogin
}

/**
 * Windows DWM backdrop: mica needs Windows 11 (build 22000+); older Windows
 * gets acrylic (Win10 1809+ system backdrop). macOS/Linux never call this.
 */
function windowsBackgroundMaterial(): 'mica' | 'acrylic' {
  const version = process.getSystemVersion()
  const build = Number(version.split('.').pop())
  if (version.startsWith('10') && Number.isFinite(build) && build >= 22000) {
    return 'mica'
  }
  return 'acrylic'
}

function createWindow(): void {
  const icon = resolveWindowIcon()
  const isMac = process.platform === 'darwin'

  mainWindow = new BrowserWindow({
    // Electron MainWindow: Width=1024 Height=640 MinWidth=1024 MinHeight=640. The
    // minimum keeps the optimization/cleanup two-column layout stable (Electron
    // never collapses it; the old 1000px default allowed the 1100px CSS-viewport
    // breakpoint to flip the grid to a single column mid-resize).
    width: 1024,
    height: 640,
    minWidth: 1024,
    minHeight: 640,
    show: false,
    title: 'Universal Device Toolkit',
    // macOS: native title bar with the traffic lights (red/yellow/green) at the
    // top-left and a vibrancy backdrop — the platform convention. Windows/Linux
    // keep the frameless custom title bar with right-aligned window buttons.
    ...(isMac
      ? {
          titleBarStyle: 'hiddenInset' as const,
          trafficLightPosition: { x: 12, y: 11 },
          vibrancy: 'under-window' as const,
          visualEffectState: 'active' as const
        }
      : process.platform === 'linux'
        ? {
            // Linux: frameless custom title bar like Windows. backgroundMaterial
            // (mica/acrylic) is a Windows-only API — Electron ignores it on
            // Linux, so an opaque window background keeps the load flash from
            // being jarring (matches the renderer's --udt-surface-window #202020).
            autoHideMenuBar: true,
            frame: false,
            backgroundColor: '#202020',
            backgroundMaterial: 'acrylic' as const
          }
        : {
            autoHideMenuBar: true,
            frame: false,
            backgroundColor: '#00000000',
            backgroundMaterial: windowsBackgroundMaterial()
          }),
    ...(icon ? { icon } : {}),
    webPreferences: {
      preload: join(__dirname, '../preload/index.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false,
      // Plugin web pages (contributes.webPage) are hosted in <webview> elements.
      webviewTag: true
    }
  })

  // The 5/6 correction compensates the Windows display scale vs. client DPI;
  // Linux/macOS map one CSS px to one DIP, so the zoom factor stays 1 there.
  if (process.platform === 'win32') {
    mainWindow.webContents.setZoomFactor(RENDERER_ZOOM_FACTOR)
  }

  // Port of Electron WindowResizeStabilityHelper: track live move/size loops so
  // heavy per-frame work can be skipped while the user drags a window edge.
  attachResizeStability(mainWindow)
  attachPluginHostBridge()
  // Port of Electron WindowMaximizeWorkAreaHelper: keep the maximized window inside
  // the monitor work area (safety net for desktop-dock overlays).
  attachMaximizeWorkAreaClamp(mainWindow)

  mainWindow.on('ready-to-show', () => {
    if (!mainWindow || mainWindow.isDestroyed()) return
    if (flags.minimized) {
      // Mirrors Electron --minimized: start hidden in the tray instead of showing.
      mainWindow.hide()
    } else {
      mainWindow.show()
    }
  })

  mainWindow.on('close', (event) => {
    if (isQuitting) return
    const win = mainWindow
    if (!win || win.isDestroyed()) return
    if (process.platform === 'darwin') {
      // macOS convention: the red traffic light hides the window instead of
      // closing it — the Dock icon stays and Cmd+Q is the real quit. Unlike
      // Windows/Linux this is unconditional (not tied to the minimize-to-tray
      // setting): the menu bar icon is the persistent handle for reopening.
      event.preventDefault()
      win.hide()
      return
    }
    void shouldMinimizeToTray(['MinimizeOnClose', 'MinimizeToTray']).then((toTray) => {
      if (!toTray || !mainWindow || mainWindow.isDestroyed()) return
      event.preventDefault()
      mainWindow.hide()
    })
  })

  mainWindow.on('minimize', () => {
    void shouldMinimizeToTray(['MinimizeToTray']).then((toTray) => {
      if (!toTray || !mainWindow || mainWindow.isDestroyed()) return
      mainWindow.hide()
    })
  })

  mainWindow.on('maximize', () => {
    const win = mainWindow
    if (win && !win.isDestroyed()) {
      win.webContents.send('window:maximized-changed', true)
    }
  })

  mainWindow.on('unmaximize', () => {
    const win = mainWindow
    if (win && !win.isDestroyed()) {
      win.webContents.send('window:maximized-changed', false)
    }
  })

  mainWindow.on('closed', () => {
    mainWindow = null
    destroyTray()
    destroyOsdWindow()
    destroyStatusWindow()
  })

  if (!app.isPackaged && process.env['ELECTRON_RENDERER_URL']) {
    mainWindow.loadURL(process.env['ELECTRON_RENDERER_URL'])
  } else {
    mainWindow.loadFile(join(__dirname, '../renderer/index.html'))
  }
}

function startHost(): void {
  attachHostEventForwarding()
  try {
    const hostPath = resolveHostPath()
    const hostArgs = toHostArgs(flags)
    console.log(`[main] starting host: ${hostPath}${hostArgs.length > 0 ? ` ${hostArgs.join(' ')}` : ''}`)
    hostClient.start(hostPath, hostArgs)
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error)
    console.error(`[main] failed to start host: ${message}`)
    hostClient.reportFatalError(message)
  }
}

app.whenReady().then(() => {
  initMainLogger()
  // Notifications: the renderer uses the in-app notification center (no OS
  // Notification API), so no permission wiring is needed here. Should native
  // notifications ever be added: macOS requires the Notification permission
  // (Electron requests it automatically on first use), Linux has no special
  // handling beyond the desktop environment's own notification settings.
  console.log('[main] app ready')
  if (flags.isTraceEnabled) {
    console.log(`[main] flags: ${describeFlags(flags)}`)
  }

  // Available before createWindow so the renderer can observe readiness immediately.
  ipcMain.handle('host:get-status', () => ({
    running: hostClient.isRunning,
    ready: hostClient.isReady,
    lastError: hostClient.lastFailure,
    readyPayload: hostClient.lastReadyPayload
  }))

  ipcMain.handle('bridge:invoke', async (_event, method: string, params?: unknown) => {
    // Main-side methods reachable through the generic renderer bridge without
    // preload changes (Electron MainWindow.OpenLog / LoadDeviceInfo equivalents).
    if (method === 'log.open-folder') return openLogFolder()
    if (method === 'device.info') return getDeviceInfo()
    if (method === 'status-window.show') {
      void showStatusWindow()
      return { shown: true }
    }
    // Native dialogs and system openers (renderer cleanup/automation UIs): the
    // headless host cannot show UI, so the main process answers these.
    if (method === 'dialog:select-json-file') {
      const options = {
        title: 'Import keyboard backlight profile',
        properties: ['openFile'] as ('openFile')[],
        filters: [{ name: 'Json Files', extensions: ['json'] }]
      }
      const owner = mainWindow ?? BrowserWindow.getFocusedWindow()
      const result = owner == null
        ? await dialog.showOpenDialog(options)
        : await dialog.showOpenDialog(owner, options)
      return result.canceled ? null : (result.filePaths[0] ?? null)
    }
    if (method === 'dialog:select-folder') {
      const options = {
        title: 'Select folder',
        properties: ['openDirectory'] as ('openDirectory')[]
      }
      const owner = mainWindow ?? BrowserWindow.getFocusedWindow()
      const result = owner == null
        ? await dialog.showOpenDialog(options)
        : await dialog.showOpenDialog(owner, options)
      return result.canceled ? null : (result.filePaths[0] ?? null)
    }
    if (method === 'dialog:open-path') {
      const path = (params as { path?: unknown } | null)?.path
      if (typeof path !== 'string' || path.length === 0) {
        throw new Error('A file path is required.')
      }
      const error = await shell.openPath(path)
      return { ok: error.length === 0 }
    }
    if (method === 'dialog:open-url') {
      const url = (params as { url?: unknown } | null)?.url
      if (typeof url !== 'string' || url.length === 0) {
        throw new Error('A URL is required.')
      }
      let parsed: URL
      try {
        parsed = new URL(url)
      } catch {
        throw new Error('Invalid URL.')
      }
      if (parsed.protocol !== 'https:' && parsed.protocol !== 'http:') {
        throw new Error('Only http(s) URLs can be opened.')
      }
      await shell.openExternal(parsed.toString())
      return { ok: true }
    }
    // Mirrors Electron --disable-update-checker (UpdateChecker.Disable): the host
    // does not know this switch, so short-circuit update requests here.
    if (flags.disableUpdateChecker) {
      if (method === 'app.update.check') {
        return { available: false, version: null, error: '--disable-update-checker' }
      }
      if (method === 'app.update.status') {
        return { status: 'Disabled', disable: true }
      }
    }
    // Windows power-plan bridge (Electron WindowsPowerPlanController): the host has
    // no powercfg/WMI channel, so the main process answers from `powercfg`.
    // powercfg does not exist on macOS/Linux — short-circuit instead of running it.
    if (method === 'powerPlans.getList') {
      if (process.platform !== 'win32') return { plans: [] }
      return listPowerPlans().then(
        (plans) => ({ plans }),
        () => ({ plans: [] })
      )
    }
    if (method === 'powerPlans.setActive') {
      if (process.platform !== 'win32') {
        throw new Error('Windows only')
      }
      const guid = (params as { guid?: unknown } | null)?.guid
      if (typeof guid !== 'string' || guid.length === 0) {
        throw new Error('A power plan GUID is required.')
      }
      return setActivePowerPlan(guid).then(() => ({ ok: true }))
    }
    // System power actions (Electron Lib PowerActions): restart/shutdown/sleep.
    if (method === 'power.restart') return restartSystem()
    if (method === 'power.shutdown') return shutdownSystem()
    if (method === 'power.sleep') return sleepSystem()
    // Update download/install (Electron UpdateWindow flow): resolve the GitHub
    // release asset, download it with progress events, launch the installer.
    if (method === 'update.getRelease') {
      return getLatestRelease().then((release) => ({ release }))
    }
    if (method === 'update.download') {
      const started = Date.now()
      return downloadLatestUpdate((progress: DownloadProgress) => {
        if (mainWindow && !mainWindow.isDestroyed()) {
          mainWindow.webContents.send('bridge:event', 'update.download-progress', {
            ...progress,
            elapsedMs: Date.now() - started
          })
        }
      }).then(
        (path) => ({ ok: true, path }),
        (error: Error) => ({ ok: false, error: error.message })
      )
    }
    if (method === 'update.launchInstaller') {
      const path = (params as { path?: unknown } | null)?.path
      if (typeof path !== 'string' || path.length === 0) {
        throw new Error('An installer path is required.')
      }
      return launchInstaller(path)
    }
    return hostClient.invoke(method, params)
  })

  ipcMain.on('window:minimize', () => mainWindow?.minimize())

  ipcMain.on('window:maximize-toggle', () => {
    if (!mainWindow) return
    if (mainWindow.isMaximized()) {
      mainWindow.unmaximize()
    } else {
      mainWindow.maximize()
    }
  })

  ipcMain.on('window:close', () => {
    if (isQuitting || !mainWindow) {
      mainWindow?.close()
      return
    }
    if (process.platform === 'darwin') {
      // Mirrors the native traffic light close: hide, never destroy.
      mainWindow.hide()
      return
    }
    void shouldMinimizeToTray(['MinimizeOnClose', 'MinimizeToTray']).then((toTray) => {
      if (!mainWindow || mainWindow.isDestroyed()) return
      if (toTray) {
        mainWindow.hide()
      } else {
        mainWindow.close()
      }
    })
  })

  ipcMain.handle('window:is-maximized', () => mainWindow?.isMaximized() ?? false)

  // Absolute path of the plugin webview guest preload (built by electron-vite
  // into out/preload/plugin-host.js).
  ipcMain.handle('plugin:preload-path', () => join(__dirname, '../preload/plugin-host.js'))

  // Port of Electron FullscreenHelper (renderer-observable window state).
  ipcMain.handle('window:is-fullscreen', () => mainWindow?.isFullScreen() ?? false)

  mainWindow?.on('enter-full-screen', () => {
    mainWindow?.webContents.send('window:fullscreen-changed', true)
  })

  mainWindow?.on('leave-full-screen', () => {
    mainWindow?.webContents.send('window:fullscreen-changed', false)
  })

  ipcMain.handle('window:set-background-material', (_event, material: unknown) => {
    // macOS backdrop is the fixed vibrancy chosen at window creation; there is
    // no runtime switch, so return a silent no-op instead of erroring.
    if (process.platform === 'darwin') return
    if (material !== 'none' && material !== 'mica' && material !== 'acrylic') {
      throw new Error(`Unsupported window background material: ${String(material)}`)
    }
    // Windows-only API (DWM backdrop); no-op elsewhere.
    if (process.platform === 'win32') {
      mainWindow?.setBackgroundMaterial(material)
    }
  })

  // DWM backdrop materials (mica/acrylic) follow the OS theme, not the in-app
  // theme — a dark app on a light system renders a washed-out white backdrop.
  // Pin nativeTheme to the in-app mode so the material matches (Windows only;
  // harmless no-op elsewhere).
  ipcMain.on('window:set-theme-source', (_event, source: unknown) => {
    if (source !== 'system' && source !== 'light' && source !== 'dark') return
    nativeTheme.themeSource = source
  })

  ipcMain.handle('shell:open-log-folder', async () => {
    // Unified log folder: main.log / renderer.log / host.log live together.
    shell.openPath(logsDirectory())
  })

  // Renderer → main log channel (utils/logger.ts double-writes console + file).
  ipcMain.on('log:write', (_event, payload: unknown) => {
    const record = (payload ?? {}) as { level?: unknown; message?: unknown }
    const level = isValidLogLevel(record.level) ? record.level : 'info'
    const message = typeof record.message === 'string' ? record.message : String(record.message ?? '')
    writeRendererLog(level, message)
  })

  // Mirrors Electron MainWindow.OpenLog: open the logs directory (created on demand).
  ipcMain.handle('log.open-folder', () => openLogFolder())

  // Mirrors Electron MainWindow.LoadDeviceInfo (MachineCompatibility.GetMachineInformationAsync).
  ipcMain.handle('device.info', () => getDeviceInfo())

  // Mirrors Electron AboutPage "Data" / "Temp" folder buttons (Folders.AppData / Path.GetTempPath).
  ipcMain.handle('shell:open-app-folder', async (_event, kind: unknown) => {
    let target: string
    if (kind === 'data') {
      // Host stores app data next to args.txt (Flags.cs mirrors Folders.AppData,
      // honoring the UDT_APPDATA_OVERRIDE environment variable).
      const override = process.env['UDT_APPDATA_OVERRIDE']
      target = override ?? join(process.env['LOCALAPPDATA'] ?? app.getPath('userData'), 'UniversalDeviceToolkit')
    } else if (kind === 'temp') {
      target = tmpdir()
    } else if (kind === 'log') {
      const result = await hostClient.invoke('app.getLogPath', {}) as { path?: unknown }
      if (typeof result.path !== 'string' || result.path.length === 0) {
        throw new Error('The host did not provide a log file path.')
      }
      target = result.path
    } else {
      throw new Error(`Unsupported folder kind: ${String(kind)}`)
    }
    if (!existsSync(target)) {
      throw new Error(`Folder does not exist: ${target}`)
    }
    const error = await shell.openPath(target)
    if (error) {
      throw new Error(error)
    }
    return { opened: true }
  })

  // Mirrors Electron Process.Start(url) / explorer.exe for file paths (used by the
  // Utils windows: update window, device info warranty link, crash report).
  ipcMain.handle('shell:open-external', async (_event, url: unknown) => {
    if (typeof url !== 'string' || url.length === 0) {
      throw new Error('A URL is required.')
    }
    let parsed: URL
    try {
      parsed = new URL(url)
    } catch {
      throw new Error('Invalid URL.')
    }
    if (parsed.protocol !== 'https:' && parsed.protocol !== 'http:') {
      throw new Error('Only http(s) URLs can be opened.')
    }
    await shell.openExternal(parsed.toString())
    return { opened: true }
  })

  ipcMain.handle('shell:open-path', async (_event, path: unknown) => {
    if (typeof path !== 'string' || path.length === 0) {
      throw new Error('A file path is required.')
    }
    const error = await shell.openPath(path)
    if (error) {
      throw new Error(error)
    }
    return { opened: true }
  })

  // Port of Electron ClipboardExtensions.SetProcesses/GetProcesses: one line per
  // executable path; reads are filtered to existing paths and deduplicated.
  ipcMain.handle('clipboard:write-lines', (_event, payload: unknown) => {
    const payloadLines = (payload as { lines?: unknown } | null)?.lines
    if (!Array.isArray(payloadLines) || payloadLines.some((line) => typeof line !== 'string')) {
      throw new Error('A lines array of strings is required.')
    }
    const lines = payloadLines as string[]
    clipboard.writeText(lines.join('\n'))
    return { ok: true }
  })

  ipcMain.handle('clipboard:read-existing-paths', () => {
    const paths = Array.from(new Set(
      clipboard.readText()
        .split(/\r\n|\n/)
        .map((line) => line.replace(/^"+|"+$/g, ''))
        .filter((path) => path.length > 0 && existsSync(path))
    ))
    return { paths }
  })

  // Mirrors Electron UnsupportedWindow.Exit / LanguageSelectorWindow.Exit: terminate
  // the whole application instead of hiding the window.
  ipcMain.on('app:quit', () => {
    isQuitting = true
    app.quit()
  })

  // Mirrors Electron SettingsApplicationBehaviorControl Autorun (registry Run key):
  // Windows uses setLoginItemSettings (registry), macOS the login item, Linux an
  // XDG autostart .desktop file.
  ipcMain.handle('app:set-autorun', (_event, enabled: unknown) => {
    const openAtLogin = enabled === true
    applyAutorun(openAtLogin)
    return { ok: true, enabled: openAtLogin }
  })

  ipcMain.handle('app:get-autorun', () => {
    return { enabled: readAutorun() }
  })

  ipcMain.handle('dialog:select-plugin-files', async () => {
    const options = {
      title: 'Import plugin packages',
      properties: ['openFile', 'multiSelections'] as ('openFile' | 'multiSelections')[],
      filters: [{ name: 'Plugin packages', extensions: ['zip'] }]
    }
    const owner = mainWindow ?? BrowserWindow.getFocusedWindow()
    const result = owner == null
      ? await dialog.showOpenDialog(options)
      : await dialog.showOpenDialog(owner, options)
    return result.canceled ? [] : result.filePaths
  })

  ipcMain.handle('dialog:select-json-file', async () => {
    const options = {
      title: 'Import keyboard backlight profile',
      properties: ['openFile'] as ('openFile')[],
      filters: [{ name: 'Json Files', extensions: ['json'] }]
    }
    const owner = mainWindow ?? BrowserWindow.getFocusedWindow()
    const result = owner == null
      ? await dialog.showOpenDialog(options)
      : await dialog.showOpenDialog(owner, options)
    return result.canceled ? null : (result.filePaths[0] ?? null)
  })

  // Mirrors Electron ProcessAutomationPipelineTriggerTabItemControl AddButton_Click
  // (OpenFileDialog with the exe filter). Windows filters .exe; macOS/Linux
  // leave the dialog unfiltered (an .app bundle or ELF/Mach-O binary is picked
  // by the user).
  ipcMain.handle('dialog:select-exe-file', async () => {
    const options = {
      title: 'Open',
      properties: ['openFile'] as ('openFile')[],
      ...(process.platform === 'win32'
        ? { filters: [{ name: 'Exe Files (.exe)', extensions: ['exe'] }] }
        : {})
    }
    const owner = mainWindow ?? BrowserWindow.getFocusedWindow()
    const result = owner == null
      ? await dialog.showOpenDialog(options)
      : await dialog.showOpenDialog(owner, options)
    return result.canceled ? null : (result.filePaths[0] ?? null)
  })

  // Mirrors Electron PlaySoundAutomationStepControl file picker (audio files).
  // C:\Windows\Media is a Windows-only default location.
  ipcMain.handle('dialog:select-audio-file', async () => {
    const options = {
      title: 'Import',
      ...(process.platform === 'win32' ? { defaultPath: 'C:\\Windows\\Media' } : {}),
      properties: ['openFile'] as ('openFile')[],
      filters: [
        { name: 'Audio Files', extensions: ['wav', 'mp3', 'ogg', 'flac', 'aac', 'm4a', 'wma'] },
        { name: 'All Files', extensions: ['*'] }
      ]
    }
    const owner = mainWindow ?? BrowserWindow.getFocusedWindow()
    const result = owner == null
      ? await dialog.showOpenDialog(options)
      : await dialog.showOpenDialog(owner, options)
    return result.canceled ? null : (result.filePaths[0] ?? null)
  })

  // Tray menu: language + quick-action refresh (Electron TrayHelper PipelinesChanged).
  ipcMain.on('tray:set-language', (_event, lang: unknown) => {
    if (typeof lang === 'string' && lang.length > 0) updateTrayLanguage(lang)
  })
  ipcMain.on('tray:refresh', () => {
    refreshTrayMenu()
  })

  // Attach forwarding before spawn so a fast host.ready is buffered rather than dropped.
  startHost()
  // Apply the persisted Autorun setting once the host is up (login item may
  // have drifted out of sync; the renderer toggle keeps it current afterwards).
  hostClient.on('host.ready', () => {
    void (async () => {
      try {
        const result = (await hostClient.invoke('settings.get', { scope: 'application' })) as
          | { value?: Record<string, unknown> }
          | null
          | undefined
        applyAutorun(result?.value?.Autorun === true)
      } catch (error) {
        console.error('[main] failed to apply persisted Autorun setting:', error)
      }
    })()
  })
  createWindow()
  setMainWindowRef(() => mainWindow)
  // macOS: install the native system menu bar (App/File/Edit/View/Window/Help).
  installApplicationMenu()
  const trayOpts = {
    disableTooltip: flags.disableTrayTooltip,
    invokeHost: (method: string, params?: unknown) => hostClient.invoke(method, params)
  }
  initTray(() => mainWindow, trayOpts)
  // Rebuild once host has loaded automation.json (default "停用 GPU" quick action).
  hostClient.on('host.initialized', () => refreshTrayMenu())
  hostClient.on('host.ready', () => refreshTrayMenu())
  // Navigation item visibility lives in application settings.
  hostClient.on('settings.changed', () => refreshTrayMenu())
  initOsdWindow()
  initStatusWindow()
  if (mainWindow) forwardHostEvents(mainWindow)

  // Forward OS power/session events to the renderer (Electron MainWindow listened
  // to the same transitions for OSD/status behavior).
  const systemEvents: Array<[string, string]> = [
    ['suspend', 'system.suspend'],
    ['resume', 'system.resume'],
    ['lock-screen', 'system.lock'],
    ['unlock-screen', 'system.unlock'],
    ['shutdown', 'system.shutdown']
  ]
  for (const [sourceEvent, bridgeEvent] of systemEvents) {
    powerMonitor.on(sourceEvent as 'suspend', () => {
      if (mainWindow && !mainWindow.isDestroyed()) {
        mainWindow.webContents.send('bridge:event', bridgeEvent, {})
      }
    })
  }

  app.on('activate', () => {
    const win = mainWindow
    if (win && !win.isDestroyed()) {
      // macOS: the window commonly still exists but is hidden (close hides it,
      // Cmd+Q is the only real quit). Show the existing window instead of
      // rebuilding it; also restores a minimized window.
      if (win.isMinimized()) win.restore()
      win.show()
      win.focus()
      return
    }
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow()
      initTray(() => mainWindow, trayOpts)
      initOsdWindow()
      initStatusWindow()
    }
  })
})

app.on('window-all-closed', () => {
  console.log('[main] window-all-closed')
  if (process.platform !== 'darwin') {
    app.quit()
  }
})

app.on('before-quit', (event) => {
  console.log('[main] before-quit, host running:', hostClient.isRunning)
  isQuitting = true
  if (!hostClient.isRunning) return
  event.preventDefault()
  void hostClient.stop().finally(() => {
    console.log('[main] host stopped, quitting')
    app.quit()
  })
})

/**
 * Mirrors Electron AppDomain_UnhandledException / CrashReportHelper: save a crash
 * report file under userData/crash-reports and surface it to the renderer
 * (CrashReportNotificationModal) via the bridge event bus.
 */
function writeCrashReport(error: Error, kind: 'uncaughtException' | 'unhandledRejection'): void {
  try {
    const fs = require('fs') as typeof import('fs')
    const dir = join(app.getPath('userData'), 'crash-reports')
    fs.mkdirSync(dir, { recursive: true })
    const stamp = new Date().toISOString().replace(/[:.]/g, '-')
    const filePath = join(dir, `udt-crash-${stamp}.txt`)
    const lines = [
      `[${kind}]`,
      `time: ${new Date().toLocaleString()}`,
      `version: ${app.getVersion()}`,
      `exception: ${error.name ?? 'Error'}`,
      `message: ${error.message ?? ''}`,
      `stack: ${error.stack ?? ''}`
    ]
    fs.writeFileSync(filePath, lines.join('\n') + '\n', 'utf8')
    const report = {
      path: filePath,
      timestamp: new Date().toLocaleString(),
      appVersion: app.getVersion(),
      exceptionType: error.name ?? 'Error',
      exceptionMessage: error.message ?? '',
      stackTrace: (error.stack ?? '').slice(0, 1200)
    }
    if (mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.webContents.send('bridge:event', 'app.crash', report)
    }
  } catch {
    // The crash reporter must never mask the original failure.
  }
}

process.on('uncaughtException', (error) => {
  console.error('[main] uncaughtException:', error)
  writeCrashReport(error, 'uncaughtException')
})

process.on('unhandledRejection', (reason) => {
  const error = reason instanceof Error ? reason : new Error(String(reason))
  console.error('[main] unhandledRejection:', error)
  writeCrashReport(error, 'unhandledRejection')
})

