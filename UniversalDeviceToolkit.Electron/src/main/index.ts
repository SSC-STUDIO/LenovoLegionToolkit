import { app, BrowserWindow, ipcMain } from 'electron'
import { join } from 'path'
import { existsSync } from 'fs'
import { HostClient } from './host-client'

const hostClient = new HostClient()
let mainWindow: BrowserWindow | null = null

function resolveHostPath(): string {
  const fromEnv = process.env['UDT_HOST_PATH']
  if (fromEnv) return fromEnv

  // __dirname = <project>/out/main -> project root is two levels up.
  const projectRoot = join(__dirname, '..', '..')
  const candidates = [
    // dev: sibling repo folder next to the Electron project
    join(projectRoot, '..', 'UniversalDeviceToolkit.Host', 'bin', 'x64', 'Debug',
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
  for (const event of ['host.ready', 'host.initialized', 'host.log']) {
    hostClient.on(event, (data) => {
      if (!window.isDestroyed()) {
        window.webContents.send('bridge:event', event, data)
      }
    })
  }
}

function createWindow(): void {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    show: false,
    autoHideMenuBar: true,
    webPreferences: {
      preload: join(__dirname, '../preload/index.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false
    }
  })

  mainWindow.on('ready-to-show', () => {
    mainWindow?.show()
  })

  mainWindow.on('closed', () => {
    mainWindow = null
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

  startHost()
  createWindow()
  if (mainWindow) forwardHostEvents(mainWindow)

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow()
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
