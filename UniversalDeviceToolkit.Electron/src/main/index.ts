import { app, BrowserWindow, dialog, ipcMain, shell } from 'electron'
import { join } from 'path'
import { existsSync } from 'fs'
import { hostClient } from './host-client'
import { initSingleInstance, setMainWindowRef } from './single-instance'
import { initTray, destroyTray } from './tray'
import { initOsdWindow, destroyOsdWindow } from './osd-window'
import { flags, describeFlags, toHostArgs } from './flags'
import { attachResizeStability } from './window-helpers'

app.setAppUserModelId('com.universaldevicetoolkit.app')

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
  for (const event of ['host.ready', 'host.initialized', 'host.log', 'notifications.changed']) {
    hostClient.on(event, (data) => {
      if (!window.isDestroyed()) {
        window.webContents.send('bridge:event', event, data)
      }
    })
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

function createWindow(): void {
  // Packaged Windows builds inherit the canonical ICO embedded by electron-builder.
  // Development uses that same multi-size ICO instead of a rescaled single PNG.
  const developmentIcon = app.isPackaged ? {} : { icon: join(PROJECT_ROOT, 'build', 'icon.ico') }

  mainWindow = new BrowserWindow({
    width: 1000,
    height: 640,
    show: false,
    autoHideMenuBar: true,
    frame: false,
    backgroundColor: '#00000000',
    backgroundMaterial: 'mica',
    ...developmentIcon,
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

  ipcMain.handle('bridge:invoke', (_event, method: string, params?: unknown) => {
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

  // Mirrors WPF UnsupportedWindow.Exit / LanguageSelectorWindow.Exit: terminate
  // the whole application instead of hiding the window.
  ipcMain.on('app:quit', () => {
    isQuitting = true
    app.quit()
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

  startHost()
  createWindow()
  setMainWindowRef(() => mainWindow)
  initTray(() => mainWindow, { disableTooltip: flags.disableTrayTooltip })
  initOsdWindow()
  if (mainWindow) forwardHostEvents(mainWindow)

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow()
      initTray(() => mainWindow, { disableTooltip: flags.disableTrayTooltip })
      initOsdWindow()
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

process.on('uncaughtException', (error) => {
  console.error('[main] uncaughtException:', error)
})
