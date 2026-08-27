import { app, BrowserWindow, dialog, ipcMain, nativeTheme, shell, systemPreferences } from 'electron'
import { promises as fs } from 'node:fs'
import { execFile, spawn } from 'node:child_process'
import { promisify } from 'node:util'
import { basename, dirname, join, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { featureFlag, isNetworkProxySidecarFile, normalizeFeatures } from './features.mjs'

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
const payloadRoot = app.isPackaged
  ? join(process.resourcesPath, 'payload')
  : join(projectRoot, 'dist', 'win-unpacked')
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

async function installApplication(options) {
  if (setupIsPreview) throw new Error('Preview mode does not install files.')
  if (isInstalling) throw new Error('Installation is already in progress.')
  if (!payloadRoot) throw new Error('The embedded application payload is missing.')
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
    await copyPayload(destination, features)
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
  const payloadFiles = await collectFiles(payloadRoot).catch(() => [])
  const payloadSize = payloadFiles.reduce((sum, file) => sum + file.size, 0)
  const stats = await directoryStats(dirname(defaultInstallPath()))
  const logoPath = app.isPackaged
    ? join(process.resourcesPath, 'installer-assets', 'icon.png')
    : join(projectRoot, 'resources', 'icon.png')
  const logoData = await fs.readFile(logoPath).then((data) => `data:image/png;base64,${data.toString('base64')}`).catch(() => '')
  return {
    version: await displayVersion(),
    defaultPath: defaultInstallPath(),
    availableBytes: stats.available,
    totalBytes: stats.total,
    payloadBytes: payloadSize,
    architecture: process.arch === 'x64' ? 'Windows x64' : `Windows ${process.arch}`,
    isUninstaller: setupIsUninstaller,
    isPreview: setupIsPreview,
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
app.whenReady().then(() => {
  if (!app.requestSingleInstanceLock()) {
    app.quit()
    return
  }
  createWindow()
})
