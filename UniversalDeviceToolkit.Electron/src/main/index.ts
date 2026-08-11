import { app, BrowserWindow, dialog, ipcMain, shell } from 'electron'
import { join } from 'path'
import { existsSync } from 'fs'
import { hostClient } from './host-client'
import { initSingleInstance, setMainWindowRef } from './single-instance'
import { initTray, destroyTray } from './tray'
import { initOsdWindow, destroyOsdWindow } from './osd-window'

app.setAppUserModelId('com.universaldevicetoolkit.app')

// WPF renders in DIPs while Chromium applies the Windows display scale to CSS.
// This keeps the Electron renderer at the original client's physical density.
const RENDERER_ZOOM_FACTOR = 5 / 6

if (!initSingleInstance()) {
  app.exit(0)
}

let mainWindow: BrowserWindow | null = null
let isQuitting = false

function resolveHostPath(): string {
  const fromEnv = process.env['UDT_HOST_PATH']
  if (fromEnv) return fromEnv

  // __dirname = <project>/out/main -> project root is two levels up.
  const projectRoot = join(__dirname, '..', '..')
  const candidates = [
    // packaged: Host copied into resources/host by electron-builder
    join(process.resourcesPath ?? '', 'host', 'UniversalDeviceToolkit.Host.exe'),
    // dev: sibling repo folder next to the Electron project
    join(projectRoot, '..', 'UniversalDeviceToolkit.Host', 'bin', 'x64', 'Debug',
      'net10.0-windows10.0.26100.0', 'win-x64', 'UniversalDeviceToolkit.Host.exe'),
    // dev: Release build
    join(projectRoot, '..', 'UniversalDeviceToolkit.Host', 'bin', 'x64', 'Release',
      'net10.0-windows10.0.26100.0', 'win-x64', 'UniversalDeviceToolkit.Host.exe'),
    // fallback: explicit build output inside this project
    join(projectRoot, 'host', 'UniversalDeviceToolkit.Host.exe')
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
  mainWindow = new BrowserWindow({
    width: 1000,
    height: 640,
    show: false,
    autoHideMenuBar: true,
    frame: false,
    icon: join(__dirname, '..', '..', 'resources', 'icon.png'),
    webPreferences: {
      preload: join(__dirname, '../preload/index.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false
    }
  })

  mainWindow.webContents.setZoomFactor(RENDERER_ZOOM_FACTOR)

  mainWindow.on('ready-to-show', () => {
    mainWindow?.show()
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
  console.log(`[main] starting host: ${hostPath}`)
  hostClient.start(hostPath)
}

app.whenReady().then(() => {
  console.log('[main] app ready')
  ipcMain.handle('bridge:invoke', (_event, method: string, params?: unknown) =>
    hostClient.invoke(method, params)
  )

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

  ipcMain.handle('shell:open-log-folder', async () => {
    const result = await hostClient.invoke('app.getLogPath', {}) as { path?: unknown }
    if (typeof result.path !== 'string' || result.path.length === 0) {
      throw new Error('The host did not provide a log file path.')
    }
    shell.showItemInFolder(result.path)
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

  startHost()
  createWindow()
  setMainWindowRef(() => mainWindow)
  initTray(() => mainWindow)
  initOsdWindow()
  if (mainWindow) forwardHostEvents(mainWindow)

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow()
      initTray(() => mainWindow)
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
