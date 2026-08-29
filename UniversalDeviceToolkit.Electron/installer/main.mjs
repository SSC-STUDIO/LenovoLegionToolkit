process.noAsar = true

import { app, BrowserWindow, dialog, ipcMain, nativeTheme, shell, systemPreferences } from 'electron'
import * as nodeFs from 'node:fs'
import { execFile, spawn } from 'node:child_process'
import { promisify } from 'node:util'
import { basename, dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { featureFlag, isNetworkProxySidecarFile, normalizeFeatures } from './features.mjs'

let originalFs = null
try {
  const mod = await import('original-fs')
  originalFs = mod.default || mod
} catch {
  // Pure node environment / test runner
}
const fs = originalFs?.promises ?? nodeFs.promises

const execFileAsync = promisify(execFile)
const installerRoot = dirname(fileURLToPath(import.meta.url))
const projectRoot = dirname(installerRoot)
const setupIsPreview = process.argv.includes('--preview')
const previewThemeValue = process.argv.find((argument) => argument.startsWith('--preview-theme='))?.split('=', 2)[1]
const previewThemeMode = setupIsPreview && (previewThemeValue === 'light' || previewThemeValue === 'dark')
  ? previewThemeValue
  : null
const previewAccentValue = process.argv.find((argument) => argument.startsWith('--preview-accent='))?.split('=', 2)[1]
const previewAccentColor = setupIsPreview && /^#[0-9a-f]{6}$/i.test(previewAccentValue ?? '')
  ? previewAccentValue
  : null

async function resolvePayloadRoot() {
  const exeDir = dirname(process.execPath)
  const appDir = app.getAppPath()
  const candidates = [
    join(process.resourcesPath, 'payload'),
    join(exeDir, 'resources', 'payload'),
    join(exeDir, 'app', 'resources', 'payload'),
    join(exeDir, '..', 'resources', 'payload'),
    join(appDir, 'resources', 'payload'),
    join(appDir, '..', 'resources', 'payload'),
    join(appDir, '..', 'payload'),
    join(projectRoot, 'dist', 'win-unpacked'),
    join(projectRoot, 'dist', 'custom-installer', 'win-unpacked', 'resources', 'payload'),
    join(projectRoot, '..', 'BuildInstallerPayload', 'full')
  ]
  for (const candidate of candidates) {
    try {
      const stats = await fs.stat(join(candidate, 'UniversalDeviceToolkit.exe'))
      if (stats.isFile()) return candidate
    } catch {
      // Continue checking
    }
  }
  return null
}

async function resolveLogoData() {
  const exeDir = dirname(process.execPath)
  const appDir = app.getAppPath()
  const candidates = [
    join(process.resourcesPath, 'installer-assets', 'icon.png'),
    join(exeDir, 'resources', 'installer-assets', 'icon.png'),
    join(exeDir, 'installer-assets', 'icon.png'),
    join(appDir, 'resources', 'icon.png'),
    join(projectRoot, 'resources', 'icon.png')
  ]
  for (const candidate of candidates) {
    try {
      const data = await fs.readFile(candidate)
      return `data:image/png;base64,${data.toString('base64')}`
    } catch {
      // Continue checking
    }
  }
  return ''
}

const validLanguages = new Set([
  'en', 'zh-CN', 'zh-Hant', 'ja', 'de', 'fr', 'es', 'it', 'pt-BR', 'pt', 'ru',
  'uk', 'pl', 'cs', 'sk', 'hu', 'ro', 'bg', 'tr', 'el', 'ar', 'lv', 'nl-NL',
  'vi', 'uz-Latn-UZ'
])
const validDeviceModes = new Set(['auto', 'basic'])
const setupIsUninstaller = process.argv.includes('--uninstall') || basename(process.execPath).toLowerCase() === 'uninstall.exe'
let mainWindow = null
let isInstalling = false

function accentColor() {
  try {
    const value = systemPreferences.getAccentColor()
    if (/^[0-9a-f]{8}$/i.test(value)) return `#${value.slice(-6)}`
    if (/^[0-9a-f]{6}$/i.test(value)) return `#${value}`
  } catch {
    // Some non-Windows Electron builds do not expose a system accent color.
  }
  return '#ff2a38'
}

function themeInfo() {
  return {
    mode: previewThemeMode ?? (nativeTheme.shouldUseDarkColors ? 'dark' : 'light'),
    accent: previewAccentColor ?? accentColor()
  }
}

async function displayVersion() {
  if (app.isPackaged) return app.getVersion()
  try {
    const metadata = JSON.parse(await fs.readFile(join(projectRoot, 'package.json'), 'utf8'))
    if (typeof metadata.version === 'string' && metadata.version.length > 0) return metadata.version
  } catch {
    // Fall back to Electron's development version when metadata is unavailable.
  }
  return app.getVersion()
}

function broadcastTheme() {
  if (mainWindow && !mainWindow.isDestroyed()) mainWindow.webContents.send('installer:theme', themeInfo())
}

function defaultInstallPath() {
  const programFiles = process.env.ProgramW6432 ?? process.env.ProgramFiles
  if (programFiles) return join(programFiles, 'UniversalDeviceToolkit')
  return join(process.env.LOCALAPPDATA ?? process.env.USERPROFILE ?? process.cwd(), 'Programs', 'UniversalDeviceToolkit')
}

function emitProgress(payload) {
  if (mainWindow && !mainWindow.isDestroyed()) mainWindow.webContents.send('installer:progress', payload)
}

async function directoryStats(path) {
  try {
    const stats = await fs.statfs(path)
    return { available: Number(stats.bavail) * Number(stats.bsize), total: Number(stats.blocks) * Number(stats.bsize) }
  } catch {
    return { available: null, total: null }
  }
}

async function collectFiles(root) {
  const files = []
  async function visit(current, relative) {
    const entries = await fs.readdir(current, { withFileTypes: true })
    for (const entry of entries) {
      const entryRelative = relative ? join(relative, entry.name) : entry.name
      const entryPath = join(current, entry.name)
      if (entry.isDirectory()) await visit(entryPath, entryRelative)
      else if (entry.isFile()) {
        const stat = await fs.stat(entryPath)
        files.push({ path: entryPath, relative: entryRelative, size: stat.size })
      }
    }
  }
  await visit(root, '')
  return files
}

async function copyPayload(destination, features) {
  const payloadRoot = await resolvePayloadRoot()
  const files = await collectFiles(payloadRoot)
  const selected = files.filter((file) => features.networkAcceleration || !isNetworkProxySidecarFile(file.relative))
  const totalBytes = selected.reduce((sum, file) => sum + file.size, 0)
  let copiedBytes = 0
  await fs.mkdir(destination, { recursive: true })
  for (const file of selected) {
    const target = join(destination, file.relative)
    await fs.mkdir(dirname(target), { recursive: true })
    await fs.copyFile(file.path, target)
    copiedBytes += file.size
    emitProgress({
      phase: 'copying',
      completedBytes: copiedBytes,
      totalBytes,
      percent: totalBytes === 0 ? 100 : (copiedBytes / totalBytes) * 100,
      file: file.relative.replaceAll('\\', '/')
    })
  }
}

async function writeSelection(destination, language, deviceMode, features) {
  const contents = [
    '[installation]',
    `language=${language}`,
    `deviceMode=${deviceMode}`,
    `windowsOptimization=${featureFlag(features.windowsOptimization)}`,
    `networkAcceleration=${featureFlag(features.networkAcceleration)}`,
    `automation=${featureFlag(features.automation)}`,
    `macro=${featureFlag(features.macro)}`,
    `keyboard=${featureFlag(features.keyboard)}`,
    ''
  ].join('\n')
  await fs.writeFile(join(destination, 'installer-selection.ini'), contents, 'utf8')
}

async function createShortcut(path, target, description) {
  try {
    const written = shell.writeShortcutLink(path, {
      target,
      cwd: dirname(target),
      description,
      icon: target,
      iconIndex: 0
    })
    if (!written) throw new Error(`Unable to create shortcut: ${path}`)
  } catch (error) {
    emitProgress({ phase: 'warning', message: error instanceof Error ? error.message : String(error) })
  }
}

async function registerUninstall(destination, uninstallPath) {
  if (process.platform !== 'win32') return
  const key = 'HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\UniversalDeviceToolkit'
  const values = [
    ['DisplayName', 'REG_SZ', 'Universal Device Toolkit'],
    ['DisplayVersion', 'REG_SZ', app.getVersion()],
    ['Publisher', 'REG_SZ', 'Universal Device Toolkit Contributors'],
    ['InstallLocation', 'REG_SZ', destination],
    ['DisplayIcon', 'REG_SZ', uninstallPath],
    ['UninstallString', 'REG_SZ', `"${uninstallPath}" --uninstall`]
  ]
  for (const [name, type, value] of values) {
    try {
      await execFileAsync('reg.exe', ['ADD', key, '/v', name, '/t', type, '/d', value, '/f'], { windowsHide: true })
    } catch (error) {
      emitProgress({ phase: 'warning', message: error instanceof Error ? error.message : String(error) })
      break
    }
  }
}

async function removeUninstallRegistration() {
  if (process.platform !== 'win32') return
  try {
    await execFileAsync('reg.exe', [
      'DELETE',
      'HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\UniversalDeviceToolkit',
      '/f'
    ], { windowsHide: true })
  } catch {
    // The registry key may already be gone; removal should remain idempotent.
  }
}

async function downloadPayloadArchive(version, destinationFile) {
  const assetName = `UniversalDeviceToolkit_v${version}_Online_win-x64.zip`
  const mirrors = [
    `https://ghfast.top/https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/v${version}/${assetName}`,
    `https://ghproxy.net/https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/v${version}/${assetName}`,
    `https://github.com/SSC-STUDIO/UniversalDeviceToolkit/releases/download/v${version}/${assetName}`
  ]

  let lastError = null
  for (let i = 0; i < mirrors.length; i++) {
    const url = mirrors[i]
    try {
      emitProgress({
        phase: 'downloading',
        percent: 0,
        completedBytes: 0,
        totalBytes: 180 * 1024 * 1024,
        speed: '连接中...',
        file: `正在连接下载节点 (${i + 1}/${mirrors.length})...`,
        message: '正在准备下载核心应用包...'
      })

      const response = await fetch(url, { redirect: 'follow' })
      if (!response.ok) {
        throw new Error(`HTTP ${response.status} ${response.statusText}`)
      }

      const contentLength = Number(response.headers.get('content-length')) || 0
      const totalBytes = contentLength > 0 ? contentLength : 180 * 1024 * 1024
      const fileStream = nodeFs.createWriteStream(destinationFile)
      const reader = response.body.getReader()
      let completedBytes = 0
      let lastTime = Date.now()
      let lastCompleted = 0
      let currentSpeed = '0.0 MB/s'

      while (true) {
        const { done, value } = await reader.read()
        if (done) break
        fileStream.write(Buffer.from(value))
        completedBytes += value.length

        const now = Date.now()
        if (now - lastTime >= 250) {
          const deltaSec = (now - lastTime) / 1000
          const deltaBytes = completedBytes - lastCompleted
          const mbPerSec = (deltaBytes / (1024 * 1024)) / deltaSec
          currentSpeed = `${mbPerSec.toFixed(1)} MB/s`
          lastTime = now
          lastCompleted = completedBytes

          const percent = Math.min(100, totalBytes > 0 ? (completedBytes / totalBytes) * 100 : 0)
          emitProgress({
            phase: 'downloading',
            percent,
            completedBytes,
            totalBytes,
            speed: currentSpeed,
            file: `正在下载核心应用包 (${percent.toFixed(0)}%)`,
            message: `${(completedBytes / (1024 * 1024)).toFixed(1)} MB / ${(totalBytes / (1024 * 1024)).toFixed(1)} MB (${currentSpeed})`
          })
        }
      }

      await new Promise((resolve, reject) => {
        fileStream.end(resolve)
        fileStream.on('error', reject)
      })

      return true
    } catch (error) {
      lastError = error
      try { await fs.rm(destinationFile, { force: true }) } catch {}
    }
  }
  throw lastError ?? new Error('所有在线下载节点连接失败，请检查网络设置或代理。')
}

async function extractPayloadArchive(archivePath, destination) {
  emitProgress({
    phase: 'extracting',
    percent: 0,
    file: '正在解压核心组件...',
    message: '正在解压并准备应用运行环境...'
  })
  await fs.mkdir(destination, { recursive: true })
  try {
    await execFileAsync('tar', ['-xf', archivePath, '-C', destination], { windowsHide: true })
  } catch {
    await execFileAsync('powershell.exe', [
      '-NoProfile',
      '-ExecutionPolicy',
      'Bypass',
      '-Command',
      `Expand-Archive -LiteralPath '${archivePath}' -DestinationPath '${destination}' -Force`
    ], { windowsHide: true })
  }
}

async function installApplication(options) {
  if (setupIsPreview) throw new Error('Preview mode does not install files.')
  if (isInstalling) throw new Error('Installation is already in progress.')
  const destination = resolve(String(options?.destination ?? ''))
  const language = String(options?.language ?? '')
  const deviceMode = String(options?.deviceMode ?? '')
  const features = normalizeFeatures(options?.features)
  if (!destination || destination === resolve(dirname(process.execPath))) throw new Error('Choose a different installation folder.')
  if (!validLanguages.has(language)) throw new Error('Unsupported installer language.')
  if (!validDeviceModes.has(deviceMode)) throw new Error('Unsupported device mode.')

  isInstalling = true
  try {
    emitProgress({ phase: 'preparing', percent: 0, file: '' })
    const payloadRoot = await resolvePayloadRoot()
    if (payloadRoot) {
      await copyPayload(destination, features)
    } else {
      const version = await displayVersion()
      const tempArchive = join(app.getPath('temp'), `udt-payload-${version}-${Date.now()}.zip`)
      try {
        await downloadPayloadArchive(version, tempArchive)
        await extractPayloadArchive(tempArchive, destination)
      } finally {
        try { await fs.rm(tempArchive, { force: true }) } catch {}
      }
      if (!features.networkAcceleration) {
        const sidecars = [
          join(destination, 'resources', 'host', 'UniversalDeviceToolkit.NetworkProxy.exe'),
          join(destination, 'resources', 'host', 'UniversalDeviceToolkit.NetworkProxy.dll'),
          join(destination, 'resources', 'host', 'UniversalDeviceToolkit.NetworkProxy.runtimeconfig.json'),
          join(destination, 'resources', 'host', 'UniversalDeviceToolkit.NetworkProxy.deps.json'),
          join(destination, 'resources', 'host', 'UniversalDeviceToolkit.NetworkProxy.pdb')
        ]
        for (const file of sidecars) {
          try { await fs.rm(file, { force: true }) } catch {}
        }
      }
    }

    await writeSelection(destination, language, deviceMode, features)

    const installedExe = join(destination, 'UniversalDeviceToolkit.exe')
    const uninstallExe = join(destination, 'uninstall.exe')
    if (process.platform === 'win32' && resolve(process.execPath) !== resolve(uninstallExe)) {
      await fs.copyFile(process.execPath, uninstallExe)
    }
    if (process.platform === 'win32') {
      const startMenu = join(process.env.APPDATA ?? destination, 'Microsoft', 'Windows', 'Start Menu', 'Programs')
      const desktop = join(process.env.USERPROFILE ?? destination, 'Desktop')
      await fs.mkdir(startMenu, { recursive: true })
      await fs.mkdir(desktop, { recursive: true })
      await createShortcut(join(startMenu, 'Universal Device Toolkit.lnk'), installedExe, 'Universal Device Toolkit')
      await createShortcut(join(desktop, 'Universal Device Toolkit.lnk'), installedExe, 'Universal Device Toolkit')
      await registerUninstall(destination, uninstallExe)
    }
    emitProgress({ phase: 'complete', percent: 100, file: '' })
    return { destination, executable: installedExe }
  } finally {
    isInstalling = false
  }
}

function quotePowerShell(value) {
  return `'${value.replaceAll("'", "''")}'`
}

async function uninstallApplication() {
  const target = resolve(dirname(process.execPath))
  await removeUninstallRegistration()
  const scriptPath = join(app.getPath('temp'), `udt-uninstall-${process.pid}.ps1`)
  const script = [
    '$ErrorActionPreference = "SilentlyContinue"',
    'Start-Sleep -Milliseconds 800',
    `Remove-Item -LiteralPath ${quotePowerShell(target)} -Recurse -Force`,
    `Remove-Item -LiteralPath ${quotePowerShell(scriptPath)} -Force`
  ].join('\n')
  await fs.writeFile(scriptPath, script, 'utf8')
  const child = spawn('powershell.exe', ['-NoProfile', '-WindowStyle', 'Hidden', '-ExecutionPolicy', 'Bypass', '-File', scriptPath], {
    detached: true,
    stdio: 'ignore',
    windowsHide: true
  })
  child.unref()
  return { scheduled: true }
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1080,
    height: 720,
    minWidth: 940,
    minHeight: 640,
    resizable: false,
    frame: false,
    show: false,
    backgroundColor: themeInfo().mode === 'dark' ? '#171717' : '#f3f5f8',
    title: setupIsUninstaller ? 'Universal Device Toolkit 卸载' : 'Universal Device Toolkit 安装',
    webPreferences: {
      preload: join(installerRoot, 'preload.mjs'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false
    }
  })
  mainWindow.loadFile(join(installerRoot, 'index.html'))
  mainWindow.once('ready-to-show', () => mainWindow?.show())
  mainWindow.on('closed', () => { mainWindow = null })
}

ipcMain.handle('installer:info', async () => {
  const payloadRoot = await resolvePayloadRoot()
  const payloadFiles = payloadRoot ? await collectFiles(payloadRoot).catch(() => []) : []
  const payloadSize = payloadFiles.reduce((sum, file) => sum + file.size, 0)
  const isOnline = payloadRoot === null
  const stats = await directoryStats(dirname(defaultInstallPath()))
  const logoData = await resolveLogoData()
  return {
    version: await displayVersion(),
    defaultPath: defaultInstallPath(),
    availableBytes: stats.available,
    totalBytes: stats.total,
    payloadBytes: payloadSize || 450 * 1024 * 1024,
    architecture: process.arch === 'x64' ? 'Windows x64' : `Windows ${process.arch}`,
    isUninstaller: setupIsUninstaller,
    isPreview: setupIsPreview,
    isOnline,
    platform: process.platform,
    logoData,
    theme: themeInfo()
  }
})

ipcMain.handle('installer:theme-info', () => themeInfo())

ipcMain.handle('installer:choose-directory', async () => {
  const result = await dialog.showOpenDialog(mainWindow, {
    title: '选择安装位置',
    defaultPath: defaultInstallPath(),
    properties: ['openDirectory', 'createDirectory']
  })
  return result.canceled || result.filePaths.length === 0 ? null : result.filePaths[0]
})

ipcMain.handle('installer:install', (_event, options) => installApplication(options))
ipcMain.handle('installer:uninstall', () => uninstallApplication())
ipcMain.handle('installer:launch', (_event, executable) => {
  if (typeof executable !== 'string' || executable.length === 0) throw new Error('Installed executable path is missing.')
  const child = spawn(executable, [], { detached: true, stdio: 'ignore', windowsHide: true })
  child.unref()
  return { launched: true }
})
ipcMain.on('installer:minimize', () => mainWindow?.minimize())
ipcMain.on('installer:close', () => mainWindow?.close())

app.on('window-all-closed', () => app.quit())
nativeTheme.themeSource = previewThemeMode ?? 'system'
nativeTheme.on('updated', broadcastTheme)
systemPreferences.on('color-changed', broadcastTheme)
systemPreferences.on('accent-color-changed', broadcastTheme)
const gotLock = app.requestSingleInstanceLock()
if (!gotLock) {
  app.quit()
} else {
  app.on('second-instance', () => {
    if (mainWindow && !mainWindow.isDestroyed()) {
      if (mainWindow.isMinimized()) mainWindow.restore()
      mainWindow.show()
      mainWindow.focus()
    }
  })
  app.whenReady().then(createWindow)
}
