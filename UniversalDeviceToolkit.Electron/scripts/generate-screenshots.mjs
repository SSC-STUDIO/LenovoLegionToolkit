import { app, BrowserWindow } from 'electron'
import { writeFileSync, mkdirSync } from 'fs'
import { resolve, join } from 'path'
import { pathToFileURL } from 'url'

const ASSETS_DIR = resolve('../Assets')
mkdirSync(ASSETS_DIR, { recursive: true })

const targetLang = process.argv[2] || 'zh-CN'
const isEn = targetLang === 'en' || targetLang === 'en-US'
const langCode = isEn ? 'en' : 'zh-CN'
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
      health: 96.08,
      temperature: 31,
      chargeRate: 0,
      voltage: 16.8,
      designCapacity: 80000,
      fullChargeCapacity: 76800,
      cycleCount: 42,
      isCharging: false
    },
    memory: {
      usage: 38,
      usedMb: 12450,
      totalMb: 32768
    }
  }
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

const mockBridge = {
  platform: 'win32',
  installerSelection: null,
  async invoke(method, params) {
    if (method === 'host.isReady') return true
    if (method === 'device.info' || method === 'system.info') return { model: 'Legion Y9000P IRX9', machineName: 'Legion Y9000P IRX9', serialNumber: 'PF4XXXXX', biosVersion: 'KWCN44WW' }
    if (method === 'system.getAccentColor') return { color: '#0078D4', r: 0, g: 120, b: 212 }
    if (method === 'localization.setCulture') return { synchronized: true }
    if (method === 'dashboard.getConfig') return DASHBOARD_CONFIG
    if (method === 'dashboard.getHardwareSupport') return { cpu: 'Intel Core i9-14900HX', gpu: 'GeForce RTX 4060', battery: 'L22B4PC0' }
    
    // Feature API
    if (method === 'feature.list') return { features: FEATURES_LIST }
    if (method === 'feature.getSupported') return { supported: true }
    if (method === 'feature.isHdrBlocked') return { blocked: false }
    if (method === 'feature.getState') {
      const k = params?.feature
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
      const k = params?.feature
      if (k === 'powerMode') return { states: ['Quiet', 'Balance', 'Performance', 'Custom'] }
      if (k === 'battery') return { states: ['Normal', 'Conservation', 'RapidCharge'] }
      if (k === 'alwaysOnUsb') return { states: ['Off', 'OnWhenSleeping', 'OnAlways'] }
      if (k === 'hybridMode') return { states: ['Hybrid', 'dGPU', 'iGPU', 'HybridAuto'] }
      if (k === 'refreshRate') return { states: [{ width: 2560, height: 1600, refreshRate: 60 }, { width: 2560, height: 1600, refreshRate: 165 }, { width: 2560, height: 1600, refreshRate: 240 }] }
      return { states: [] }
    }
    if (method === 'feature.setState') return { ok: true }

    // Sensors API
    if (method === 'sensors.getStatus') return { initialized: true, isHybrid: true, cpuName: 'Intel Core i9-14900HX', gpuName: 'GeForce RTX 4060', gpuIsIntegrated: false }
    if (method === 'sensors.getSnapshot') return makeSnapshot(30)
    if (method === 'sensors.getDetailed') return makeSnapshot(30)
    if (method === 'sensors.subscribe') return { subscribed: true, effectiveIntervalSec: 1 }
    if (method === 'sensors.unsubscribe') return { unsubscribed: true }
    if (method === 'sensors.getFps') return { fps: 144, lowFps: 120, frameTimeMs: 6.94 }
    if (method === 'sensors.getSettings') return { enableHardwareSensors: true, showCpuAverageFrequency: false, displayMemoryInGigabytes: true }

    // Settings API
    if (method === 'settings.get') return { application: { Language: ` + JSON.stringify(langCode) + `, Theme: 'Dark', FontFamily: 'system', AnimationsEnabled: true } }
    if (method === 'settings.getAll') return { application: { Language: ` + JSON.stringify(langCode) + `, Theme: 'Dark', FontFamily: 'system', AnimationsEnabled: true }, appearance: {} }
    if (method === 'settings.set') return true
    if (method === 'settings.save') return true

    if (method === 'optimization.getRules') return []
    if (method === 'automation.getPipelines') return []
    if (method === 'plugins.getInstalled') return []
    if (method === 'app.update.status') return { status: 'Disabled', disable: true }
    return null
  },
  async getHostStatus() {
    return { running: true, ready: true, lastError: null, readyPayload: {} }
  },
  on(event, callback) {
    if (!listeners.has(event)) listeners.set(event, new Set())
    listeners.get(event).add(callback)
    if (event === 'host:status') {
      setTimeout(() => callback({ running: true, ready: true }), 10)
    }
    if (event === 'sensors.updated') {
      let step = 0
      for (let i = 0; i < 20; i++) {
        setTimeout(() => callback(makeSnapshot(i)), i * 30)
      }
      const interval = setInterval(() => {
        step++
        callback(makeSnapshot(20 + step))
      }, 500)
      return () => clearInterval(interval)
    }
    return () => {
      listeners.get(event)?.delete(callback)
    }
  },
  async isMaximized() { return false },
  async isFullscreen() { return false },
  onFullscreenChanged(cb) { return () => {} },
  onMaximizedChanged(cb) { return () => {} },
  setTrayLanguage(lang) {},
  refreshTrayMenu() {},
  setThemeSource(source) {},
  async setUiScale(scale) { return { ok: true, scale } },
  async getMemoryUsage() { return { processes: [], totalMB: 48 } },
  log(level, message) {},
  minimize() {},
  maximizeToggle() {},
  closeWindow() {},
  async setBackgroundMaterial(mat) {},
  async openLogFolder() {},
  async getPluginPreloadPath() { return '' },
  getPathForFile() { return '' }
}

contextBridge.exposeInMainWorld('bridge', mockBridge)
`

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

  const entryUrl = pathToFileURL(resolve('out/renderer/index.html')).href
  await win.loadURL(entryUrl)

  // Configure language and theme in localStorage
  await win.webContents.executeJavaScript(
    "localStorage.setItem('udt-language', " + JSON.stringify(langCode) + ");" +
    "localStorage.setItem('udt.lang', " + JSON.stringify(langCode) + ");" +
    "localStorage.setItem('i18nextLng', " + JSON.stringify(langCode) + ");" +
    "localStorage.setItem('udt.theme', 'dark');" +
    "localStorage.setItem('udt.font-family', 'system');" +
    "document.documentElement.setAttribute('data-theme', 'dark');" +
    "document.documentElement.style.colorScheme = 'dark';"
  )

  // Reload so i18n bootstraps with target language cleanly
  await win.loadURL(entryUrl)

  await new Promise((r) => setTimeout(r, 3200))

  const image = await win.webContents.capturePage({ x: 0, y: 0, width: 1300, height: 850 })
  writeFileSync(outputPath, image.toPNG())
  console.log('Saved screenshot:', outputPath)
  win.close()
  app.quit()
})
