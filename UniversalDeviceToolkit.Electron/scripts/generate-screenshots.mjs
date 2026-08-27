import { app, BrowserWindow } from 'electron'
import { writeFileSync, mkdirSync } from 'fs'
import { resolve, join } from 'path'
import { pathToFileURL } from 'url'

const ASSETS_DIR = resolve('../Assets')
mkdirSync(ASSETS_DIR, { recursive: true })

const targetLang = process.argv[2] || 'zh-CN'
const isEn = targetLang === 'en' || targetLang === 'en-US'
const langCode = isEn ? 'en' : 'zh-CN'
const hostCulture = isEn ? 'en' : 'zh-Hans'
const outputFilename = isEn ? 'Screenshot_main.png' : 'Screenshot_zh-hans.png'
const outputPath = join(ASSETS_DIR, outputFilename)

const MOCK_PRELOAD_CODE = `
const { contextBridge } = require('electron')

function makeSnapshot(step = 0) {
  const cpuUsage = Math.round(28 + Math.sin(step * 0.5) * 8)
  const cpuTemp = Math.round(54 + Math.sin(step * 0.3) * 4)
  const cpuClock = Math.round(2600 + Math.cos(step * 0.4) * 300)
  const gpuUsage = Math.round(12 + Math.cos(step * 0.6) * 5)
  const gpuTemp = Math.round(46 + Math.sin(step * 0.4) * 3)
  const gpuClock = Math.round(1850 + Math.sin(step * 0.5) * 150)
  return {
    ts: new Date(Date.now() - (30 - step) * 1000).toISOString(),
    source: 'mixed',
    initialized: true,
    isHybrid: true,
    info: {
      cpuName: 'Intel Core i9-14900HX',
      gpuName: 'GeForce RTX 4060',
      gpuIsIntegrated: false
    },
    cpu: {
      temperature: cpuTemp,
      usage: cpuUsage,
      fanSpeed: 2400,
      power: 45,
      voltage: 1.15,
      coreClockMax: cpuClock,
      coreClockAvg: cpuClock - 200
    },
    gpu: {
      usage: gpuUsage,
      temperature: gpuTemp,
      coreClock: gpuClock,
      memoryClock: 8000,
      power: 35,
      voltage: 0.95,
      fanSpeed: 2100,
      vramUsedMb: 2450,
      vramTotalMb: 8192
    },
    battery: {
      chargeLevel: 97,
      health: 0.9608,
      temperature: Math.round(31 + Math.sin(step * 0.2) * 2),
      chargeRate: Math.round(25000 + Math.sin(step * 0.3) * 3000),
      voltage: 16.8,
      designCapacity: 80000,
      fullChargeCapacity: 76800,
      cycleCount: 42,
      isCharging: true
    },
    memory: {
      usage: 38,
      usedMb: 12450,
      totalMb: 32768
    }
  }
}

const APPLICATION_SETTINGS = {
  Language: ${JSON.stringify(langCode)},
  Theme: 'Dark',
  FontFamily: 'system',
  AnimationsEnabled: true,
  AccentColorSource: 'Custom',
  AccentColor: { R: 255, G: 33, B: 33 },
  ApplyAccentColorToTheme: false
}

const DASHBOARD_CONFIG = {
  showSensors: true,
  sensorsRefreshIntervalSeconds: 1,
  groups: [
    { type: 'Power', items: ['PowerMode', 'BatteryMode', 'AlwaysOnUsb'] },
    { type: 'Graphics', items: ['HybridMode'] },
    { type: 'Display', items: ['RefreshRate', 'OverDrive', 'Hdr'] },
    { type: 'Other', items: ['TouchpadLock', 'FnLock', 'WinKeyLock'] }
  ]
}

const FEATURES_LIST = [
  { key: 'powerMode', supported: true, stateType: 'PowerModeState' },
  { key: 'battery', supported: true, stateType: 'BatteryModeState' },
  { key: 'alwaysOnUsb', supported: true, stateType: 'AlwaysOnUsbState' },
  { key: 'hybridMode', supported: true, stateType: 'HybridModeState' },
  { key: 'refreshRate', supported: true, stateType: 'RefreshRate' },
  { key: 'overDrive', supported: true, stateType: 'Boolean' },
  { key: 'hdr', supported: true, stateType: 'Boolean' },
  { key: 'touchpadLock', supported: true, stateType: 'Boolean' },
  { key: 'fnLock', supported: true, stateType: 'Boolean' },
  { key: 'winKey', supported: true, stateType: 'Boolean' }
]

const listeners = new Map()

function emit(event, data) {
  const set = listeners.get(event)
  if (!set) return
  for (const callback of set) callback(data)
}

const mockBridge = {
  platform: 'win32',
  installerSelection: null,
  async invoke(method, params) {
    if (method === 'host.isReady') return true
    if (method === 'device.info' || method === 'system.info') {
      return { vendor: 'Lenovo', model: 'Legion Y9000P IRX9', machineType: 'Legion Y9000P IRX9', serialNumber: 'PF4XXXXX', biosVersion: 'KWCN44WW', isCompatible: true }
    }
    if (method === 'system.getAccentColor' || method === 'system.accentColor.get') {
      return { color: '#0078D4', r: 0, g: 120, b: 212 }
    }
    if (method === 'system.powerAdapterStatus') return { status: 'Connected' }
    if (method === 'localization.getCulture' || method === 'localization.setCulture') {
      return { culture: ${JSON.stringify(hostCulture)} }
    }
    if (method === 'dashboard.getConfig') return DASHBOARD_CONFIG
    if (method === 'dashboard.saveConfig') return { saved: true }
    if (method === 'dashboard.getHardwareSupport') return { cpu: 'Intel Core i9-14900HX', gpu: 'GeForce RTX 4060', battery: 'L22B4PC0' }

    if (method === 'feature.list') return { features: FEATURES_LIST }
    if (method === 'feature.getSupported') return { supported: true }
    if (method === 'feature.isHdrBlocked') return { blocked: false }
    if (method === 'feature.getState') {
      const k = params && params.feature
      if (k === 'powerMode') return { state: 'Balance' }
      if (k === 'battery') return { state: 'Conservation' }
      if (k === 'alwaysOnUsb') return { state: 'OnAlways' }
      if (k === 'hybridMode') return { state: 'Hybrid' }
      if (k === 'refreshRate') return { state: { width: 2560, height: 1600, refreshRate: 240 } }
      if (k === 'overDrive') return { state: true }
      if (k === 'hdr') return { state: false }
      if (k === 'touchpadLock') return { state: false }
      if (k === 'fnLock') return { state: true }
      if (k === 'winKey') return { state: false }
      return { state: null }
    }
    if (method === 'feature.getStates') {
      const k = params && params.feature
      if (k === 'powerMode') return { states: ['Quiet', 'Balance', 'Performance', 'Custom'] }
      if (k === 'battery') return { states: ['Normal', 'Conservation', 'RapidCharge'] }
      if (k === 'alwaysOnUsb') return { states: ['Off', 'OnWhenSleeping', 'OnAlways'] }
      if (k === 'hybridMode') return { states: ['Hybrid', 'dGPU', 'iGPU', 'HybridAuto'] }
      if (k === 'refreshRate') return { states: [{ width: 2560, height: 1600, refreshRate: 60 }, { width: 2560, height: 1600, refreshRate: 165 }, { width: 2560, height: 1600, refreshRate: 240 }] }
      return { states: [] }
    }
    if (method === 'feature.setState') return { ok: true }

    if (method === 'sensors.getStatus') return { initialized: true, isHybrid: true, cpuName: 'Intel Core i9-14900HX', gpuName: 'GeForce RTX 4060', gpuIsIntegrated: false }
    if (method === 'sensors.getSnapshot' || method === 'sensors.getDetailed') return makeSnapshot(30)
    if (method === 'sensors.subscribe') return { subscribed: true, effectiveIntervalSec: 1 }
    if (method === 'sensors.unsubscribe') return { unsubscribed: true }
    if (method === 'sensors.getFps') return { fps: 144, lowFps: 120, frameTimeMs: 6.94 }
    if (method === 'sensors.subscribeFps' || method === 'sensors.unsubscribeFps') return { monitoring: true }
    if (method === 'sensors.getSettings') {
      return {
        enableHardwareSensors: true,
        showCpuAverageFrequency: false,
        displayMemoryInGigabytes: true,
        visibleSections: ['CPU', 'Battery', 'GPU'],
        sectionOrder: ['CPU', 'Battery', 'GPU']
      }
    }
    if (method === 'sensors.setSettings') return { saved: true }

    if (method === 'settings.get') return { scope: params && params.scope, value: APPLICATION_SETTINGS }
    if (method === 'settings.getAll') {
      return {
        scopes: {
          application: APPLICATION_SETTINGS,
          dashboard: DASHBOARD_CONFIG,
          hardwareSensors: {
            selectedGpuIsIgpu: false,
            showCpuAverageFrequency: false,
            displayMemoryInGigabytes: true,
            visibleSections: ['CPU', 'Battery', 'GPU'],
            sectionOrder: ['CPU', 'Battery', 'GPU']
          }
        }
      }
    }
    if (method === 'settings.set') return true
    if (method === 'settings.save') return { saved: ['application'] }

    if (method === 'optimization.getRules' || method === 'optimization.getCategories') return { categories: [], rules: [] }
    if (method === 'automation.getPipelines') return { pipelines: [] }
    if (method === 'app.update.status') return { status: 'Disabled', disable: true }
    if (method === 'keyboard.getState' || method === 'keyboard.backlight.getState') return { supported: false }
    return {}
  },
  async getHostStatus() {
    return { running: true, ready: true, lastError: null, readyPayload: {} }
  },
  on(event, callback) {
    if (!listeners.has(event)) listeners.set(event, new Set())
    listeners.get(event).add(callback)
    if (event === 'host:status' || event === 'host.ready') {
      setTimeout(() => callback({ running: true, ready: true }), 10)
    }
    if (event === 'sensors.updated') {
      let step = 0
      for (let i = 0; i < 8; i++) {
        setTimeout(() => callback(makeSnapshot(i)), i * 40)
      }
      const interval = setInterval(() => {
        step++
        callback(makeSnapshot(8 + step))
      }, 400)
      return () => clearInterval(interval)
    }
    return () => {
      listeners.get(event) && listeners.get(event).delete(callback)
    }
  },
  async isMaximized() { return false },
  async isFullscreen() { return false },
  onFullscreenChanged() { return () => {} },
  onMaximizedChanged() { return () => {} },
  setTrayLanguage() {},
  refreshTrayMenu() {},
  setThemeSource() {},
  async setUiScale(scale) { return { ok: true, scale } },
  async getMemoryUsage() { return { processes: [], totalMB: 48 } },
  log() {},
  minimize() {},
  maximizeToggle() {},
  closeWindow() {},
  async setBackgroundMaterial() {},
  async openLogFolder() {},
  getPathForFile() { return '' }
}

contextBridge.exposeInMainWorld('bridge', mockBridge)
`

function waitForLoad(win) {
  return new Promise((resolveLoad, reject) => {
    const timer = setTimeout(() => reject(new Error('did-finish-load timeout')), 20000)
    win.webContents.once('did-finish-load', () => {
      clearTimeout(timer)
      resolveLoad()
    })
    win.webContents.once('did-fail-load', (_e, code, desc) => {
      clearTimeout(timer)
      reject(new Error('did-fail-load ' + code + ' ' + desc))
    })
  })
}

app.whenReady().then(async () => {
  mkdirSync(resolve('out/preload'), { recursive: true })
  writeFileSync(resolve('out/preload/screenshot-preload.js'), MOCK_PRELOAD_CODE)

  const win = new BrowserWindow({
    width: 1300,
    height: 850,
    show: true,
    frame: false,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: resolve('out/preload/screenshot-preload.js')
    }
  })

  win.webContents.on('console-message', (_event, _level, message) => {
    console.log('[RENDERER]:', message)
  })

  const entryUrl = pathToFileURL(resolve('out/renderer/index.html')).href + '#/dashboard'
  await win.loadURL(entryUrl)

  await win.webContents.executeJavaScript(
    "localStorage.setItem('udt-language', " + JSON.stringify(langCode) + ");" +
    "localStorage.setItem('udt.lang', " + JSON.stringify(langCode) + ");" +
    "localStorage.setItem('i18nextLng', " + JSON.stringify(langCode) + ");" +
    "localStorage.setItem('udt.theme', 'dark');" +
    "localStorage.setItem('udt.accent-source', 'Custom');" +
    "localStorage.setItem('udt.accent', '#ff2121');" +
    "localStorage.setItem('udt.accent-tints', 'false');" +
    "localStorage.setItem('udt.font-family', 'system');" +
    "document.documentElement.setAttribute('data-theme', 'dark');" +
    "document.documentElement.style.colorScheme = 'dark';"
  )

  const reloaded = waitForLoad(win)
  win.reload()
  await reloaded
  await new Promise((r) => setTimeout(r, 5000))

  let image = null
  for (let attempt = 1; attempt <= 4; attempt++) {
    try {
      image = await win.capturePage()
      break
    } catch (error) {
      console.warn('capturePage attempt', attempt, error)
      await new Promise((r) => setTimeout(r, 800))
    }
  }
  if (image == null) throw new Error('capturePage failed')
  writeFileSync(outputPath, image.toPNG())
  console.log('Saved screenshot:', outputPath)
  win.close()
  app.quit()
}).catch((error) => {
  console.error(error)
  app.quit()
  process.exit(1)
})
