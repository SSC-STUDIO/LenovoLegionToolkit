import { app, BrowserWindow, clipboard, dialog, ipcMain, powerMonitor, shell } from 'electron'
import { join } from 'path'
import { existsSync, mkdirSync } from 'fs'
import { tmpdir } from 'os'
import { hostClient } from './host-client'
import { initSingleInstance, setMainWindowRef } from './single-instance'
import { initTray, destroyTray, refreshTrayMenu, updateTrayLanguage } from './tray'
import { initOsdWindow, destroyOsdWindow } from './osd-window'
import { initStatusWindow, destroyStatusWindow, showStatusWindow } from './status-window'
import { flags, describeFlags, toHostArgs } from './flags'
import { attachResizeStability, attachMaximizeWorkAreaClamp } from './window-helpers'
import { listPowerPlans, setActivePowerPlan } from './power-plans'
import { restartSystem, shutdownSystem, sleepSystem } from './system-power'
import { downloadLatestUpdate, getLatestRelease, launchInstaller, type DownloadProgress } from './update-downloader'

app.setAppUserModelId('com.universaldevicetoolkit.app')

// Force the app name to use the product name everywhere (taskbar window preview,
// notifications, dev tools, etc.) instead of falling back to package.json name which
// still ends in '-electron' and would surface as a separate 'Electron' window in the
// Windows 11 taskbar window preview alongside the main app.
app.setName('Universal Device Toolkit')
// Disable the noisy Chromium DevTools shortcut defaults; the app never opens DevTools
// in production, and an unsuppressed F12 would spawn a hidden 'Electron' frame that the
// taskbar window preview surfaces as a third entry next to the main window.
app.commandLine.appendSwitch('disable-features', 'OutOfBlinkCors')

// WPF renders in DIPs while Chromium applies the Windows display scale to CSS.
// This keeps the Electron renderer at the original client's physical density.
const RENDERER_ZOOM_FACTOR = 5 / 6
const PROJECT_ROOT = join(__dirname, '..', '..')

if (!initSingleInstance()) {
  app.exit(0)
}

let mainWindow: BrowserWindow | null = null
let isQuitting = false

function resolveHostPath(): string {
  const fromEnv = process.env['UDT_HOST_PATH']
  if (fromEnv) return fromEnv

  // __dirname = <project>/out/main -> project root is two levels up.
  const candidates = [
    // packaged: Host copied into resources/host by electron-builder
    join(process.resourcesPath ?? '', 'host', 'UniversalDeviceToolkit.Host.exe'),
    // dev: sibling repo folder next to the Electron project
    join(PROJECT_ROOT, '..', 'UniversalDeviceToolkit.Host', 'bin', 'x64', 'Debug',
      'net10.0-windows10.0.26100.0', 'win-x64', 'UniversalDeviceToolkit.Host.exe'),
    // dev: Release build
    join(PROJECT_ROOT, '..', 'UniversalDeviceToolkit.Host', 'bin', 'x64', 'Release',
      'net10.0-windows10.0.26100.0', 'win-x64', 'UniversalDeviceToolkit.Host.exe'),
    // fallback: explicit build output inside this project
    join(PROJECT_ROOT, 'host', 'UniversalDeviceToolkit.Host.exe')
  ]

  for (const candidate of candidates) {
    if (existsSync(candidate)) return candidate
  }
  return candidates[0]
}

function forwardHostEvents(window: BrowserWindow): void {
  // Forward all host events (sensors.updated, settings.changed, osd.changed, …).
  // A fixed whitelist previously dropped sensors.updated, so gauges stayed at "-".
  hostClient.onAny((event, data) => {
    if (!window.isDestroyed()) {
      window.webContents.send('bridge:event', event, data)
    }
  })
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
  // Prefer the tracked multi-size ICO (resources/ + buildResources/). The old
  // build/icon.ico path lived under a gitignored Build/ folder on Windows, so
  // packaged CI builds fell back to the default Electron icon/name.
  const candidates = [
    join(PROJECT_ROOT, 'resources', 'icon.ico'),
    join(PROJECT_ROOT, 'buildResources', 'icon.ico'),
    join(PROJECT_ROOT, 'resources', 'icon.png')
  ]
  return candidates.find((candidate) => existsSync(candidate))
}

/** Device info shape — mirrors WPF MachineCompatibility.MachineInformation. */
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
 * Mirrors WPF MainWindow.OpenLog: reveal the logs folder in Explorer. Electron's
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

function createWindow(): void {
  const icon = resolveWindowIcon()

  mainWindow = new BrowserWindow({
    width: 1000,
    height: 640,
    show: false,
    title: 'Universal Device Toolkit',
    autoHideMenuBar: true,
    frame: false,
    backgroundColor: '#00000000',
    backgroundMaterial: 'mica',
    ...(icon ? { icon } : {}),
    webPreferences: {
      preload: join(__dirname, '../preload/index.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false
    }
  })

  mainWindow.webContents.setZoomFactor(RENDERER_ZOOM_FACTOR)

  // Port of WPF WindowResizeStabilityHelper: track live move/size loops so
  // heavy per-frame work can be skipped while the user drags a window edge.
  attachResizeStability(mainWindow)
  // Port of WPF WindowMaximizeWorkAreaHelper: keep the maximized window inside
  // the monitor work area (safety net for desktop-dock overlays).
  attachMaximizeWorkAreaClamp(mainWindow)

  mainWindow.on('ready-to-show', () => {
    if (!mainWindow || mainWindow.isDestroyed()) return
    if (flags.minimized) {
      // Mirrors WPF --minimized: start hidden in the tray instead of showing.
      mainWindow.hide()
    } else {
      mainWindow.show()
    }
  })

  mainWindow.on('close', (event) => {
    if (isQuitting) return
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
  const hostPath = resolveHostPath()
  const hostArgs = toHostArgs(flags)
  console.log(`[main] starting host: ${hostPath}${hostArgs.length > 0 ? ` ${hostArgs.join(' ')}` : ''}`)
  hostClient.start(hostPath, hostArgs)
}

app.whenReady().then(() => {
  console.log('[main] app ready')
  if (flags.isTraceEnabled) {
    console.log(`[main] flags: ${describeFlags(flags)}`)
  }

  ipcMain.handle('bridge:invoke', async (_event, method: string, params?: unknown) => {
    // Main-side methods reachable through the generic renderer bridge without
    // preload changes (WPF MainWindow.OpenLog / LoadDeviceInfo equivalents).
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
    // Mirrors WPF --disable-update-checker (UpdateChecker.Disable): the host
    // does not know this switch, so short-circuit update requests here.
    if (flags.disableUpdateChecker) {
      if (method === 'app.update.check') {
        return { available: false, version: null, error: '--disable-update-checker' }
      }
      if (method === 'app.update.status') {
        return { status: 'Disabled', disable: true }
      }
    }
    // Windows power-plan bridge (WPF WindowsPowerPlanController): the host has
    // no powercfg/WMI channel, so the main process answers from `powercfg`.
    if (method === 'powerPlans.getList') {
      return listPowerPlans().then(
        (plans) => ({ plans }),
        () => ({ plans: [] })
      )
    }
    if (method === 'powerPlans.setActive') {
      const guid = (params as { guid?: unknown } | null)?.guid
      if (typeof guid !== 'string' || guid.length === 0) {
        throw new Error('A power plan GUID is required.')
      }
      return setActivePowerPlan(guid).then(() => ({ ok: true }))
    }
    // System power actions (WPF Lib PowerActions): restart/shutdown/sleep.
    if (method === 'power.restart') return restartSystem()
    if (method === 'power.shutdown') return shutdownSystem()
    if (method === 'power.sleep') return sleepSystem()
    // Update download/install (WPF UpdateWindow flow): resolve the GitHub
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

  // Port of WPF FullscreenHelper (renderer-observable window state).
  ipcMain.handle('window:is-fullscreen', () => mainWindow?.isFullScreen() ?? false)

  mainWindow?.on('enter-full-screen', () => {
    mainWindow?.webContents.send('window:fullscreen-changed', true)
  })

  mainWindow?.on('leave-full-screen', () => {
    mainWindow?.webContents.send('window:fullscreen-changed', false)
  })

  ipcMain.handle('window:set-background-material', (_event, material: unknown) => {
    if (material !== 'none' && material !== 'mica' && material !== 'acrylic') {
      throw new Error(`Unsupported window background material: ${String(material)}`)
    }
    mainWindow?.setBackgroundMaterial(material)
  })

  ipcMain.handle('shell:open-log-folder', async () => {
    const result = await hostClient.invoke('app.getLogPath', {}) as { path?: unknown }
    if (typeof result.path !== 'string' || result.path.length === 0) {
      throw new Error('The host did not provide a log file path.')
    }
    shell.showItemInFolder(result.path)
  })

  // Mirrors WPF MainWindow.OpenLog: open the logs directory (created on demand).
  ipcMain.handle('log.open-folder', () => openLogFolder())

  // Mirrors WPF MainWindow.LoadDeviceInfo (MachineCompatibility.GetMachineInformationAsync).
  ipcMain.handle('device.info', () => getDeviceInfo())

  // Mirrors WPF AboutPage "Data" / "Temp" folder buttons (Folders.AppData / Path.GetTempPath).
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

  // Mirrors WPF Process.Start(url) / explorer.exe for file paths (used by the
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

  // Port of WPF ClipboardExtensions.SetProcesses/GetProcesses: one line per
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

  // Mirrors WPF UnsupportedWindow.Exit / LanguageSelectorWindow.Exit: terminate
  // the whole application instead of hiding the window.
  ipcMain.on('app:quit', () => {
    isQuitting = true
    app.quit()
  })

  // Mirrors WPF SettingsApplicationBehaviorControl Autorun (registry Run key):
  // Electron persists the login item via setLoginItemSettings.
  ipcMain.handle('app:set-autorun', (_event, enabled: unknown) => {
    const openAtLogin = enabled === true
    app.setLoginItemSettings({ openAtLogin })
    return { ok: true, enabled: openAtLogin }
  })

  ipcMain.handle('app:get-autorun', () => {
    return { enabled: app.getLoginItemSettings().openAtLogin }
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

  // Mirrors WPF ProcessAutomationPipelineTriggerTabItemControl AddButton_Click
  // (OpenFileDialog with the exe filter).
  ipcMain.handle('dialog:select-exe-file', async () => {
    const options = {
      title: 'Open',
      properties: ['openFile'] as ('openFile')[],
      filters: [{ name: 'Exe Files (.exe)', extensions: ['exe'] }]
    }
    const owner = mainWindow ?? BrowserWindow.getFocusedWindow()
    const result = owner == null
      ? await dialog.showOpenDialog(options)
      : await dialog.showOpenDialog(owner, options)
    return result.canceled ? null : (result.filePaths[0] ?? null)
  })

  // Mirrors WPF PlaySoundAutomationStepControl file picker (audio files).
  ipcMain.handle('dialog:select-audio-file', async () => {
    const options = {
      title: 'Import',
      defaultPath: 'C:\\Windows\\Media',
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

  // Tray menu: language + quick-action refresh (WPF TrayHelper PipelinesChanged).
  ipcMain.on('tray:set-language', (_event, lang: unknown) => {
    if (typeof lang === 'string' && lang.length > 0) updateTrayLanguage(lang)
  })
  ipcMain.on('tray:refresh', () => {
    refreshTrayMenu()
  })

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
        app.setLoginItemSettings({ openAtLogin: result?.value?.Autorun === true })
      } catch (error) {
        console.error('[main] failed to apply persisted Autorun setting:', error)
      }
    })()
  })
  createWindow()
  setMainWindowRef(() => mainWindow)
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

  // Forward OS power/session events to the renderer (WPF MainWindow listened
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
 * Mirrors WPF AppDomain_UnhandledException / CrashReportHelper: save a crash
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

