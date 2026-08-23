#!/usr/bin/env node
/**
 * Linux recording / demo Host stub.
 *
 * Speaks the Electron NDJSON JSON-RPC protocol (stdin requests, stdout
 * `{ id, result }` / `{ id, error }` / `{ event, data }`). Electron stays UI-only;
 * this process is a stand-in for UniversalDeviceToolkit.Host so the renderer can
 * paint gauges, lists, and settings without Windows hardware.
 *
 * Honest limits: model is "Linux Desktop" (never a Legion SKU). Legion EC / WMI /
 * RGB / Vantage / Hotkeys stay unsupported with FeatureNotSupported (-1001).
 */
import { hostname, cpus, totalmem, freemem, platform, release, arch } from 'node:os'
import { createInterface } from 'node:readline'

const FEATURE_NOT_SUPPORTED = -1001
const PLATFORM_NOT_SUPPORTED = -32099
const INVALID_PARAMS = -32602
const LEGION_REASON =
  'Vendor hardware features require Windows Host (LibreHardwareMonitor, WMI, Legion EC).'

class RpcError extends Error {
  constructor(code, message) {
    super(message)
    this.code = code
  }
}

function write(obj) {
  process.stdout.write(`${JSON.stringify(obj)}\n`)
}

function wave(periodSec, min, max, phase = 0) {
  const t = Date.now() / 1000
  const u = (Math.sin((2 * Math.PI * t) / periodSec + phase) + 1) / 2
  return min + (max - min) * u
}

function round(value, digits = 1) {
  const f = 10 ** digits
  return Math.round(value * f) / f
}

function cpuModelName() {
  const name = cpus()[0]?.model?.trim()
  return name && name.length > 0 ? name : 'Generic x86_64'
}

let lastCpuSample = null

function readCpuUsagePercent() {
  const list = cpus()
  let idle = 0
  let total = 0
  for (const cpu of list) {
    const times = cpu.times
    idle += times.idle
    total += times.user + times.nice + times.sys + times.idle + times.irq
  }
  let usage = wave(11, 12, 28, 0.4)
  if (lastCpuSample != null && total > lastCpuSample.total) {
    const idleDelta = idle - lastCpuSample.idle
    const totalDelta = total - lastCpuSample.total
    if (totalDelta > 0) usage = Math.max(0, Math.min(100, (1 - idleDelta / totalDelta) * 100))
  }
  lastCpuSample = { idle, total }
  // Headless VMs often sit at ~0%; keep a visible idle-desktop band so gauges move.
  if (usage < 8) usage = wave(9, 9, 18, 0.2)
  return round(usage, 1)
}

function memorySnapshot() {
  const total = totalmem()
  const free = freemem()
  const used = Math.max(0, total - free)
  const totalMb = total / (1024 * 1024)
  const usedMb = used / (1024 * 1024)
  return {
    usage: round((used / total) * 100, 1),
    usedMb: round(usedMb, 0),
    totalMb: round(totalMb, 0),
    highestTemperature: round(wave(29, 36, 42, 1.1), 1)
  }
}

function buildSnapshot() {
  const cpuUsage = readCpuUsagePercent()
  const mem = memorySnapshot()
  const cpuTemp = round(wave(17, 48, 62, 0.0), 1)
  const cpuClock = round(wave(13, 2100, 3400, 0.7), 0)
  const gpuUsage = round(wave(15, 8, 34, 1.4), 1)
  const gpuTemp = round(wave(19, 44, 61, 0.9), 1)
  const gpuClock = round(wave(14, 1200, 1850, 0.3), 0)
  const batteryLevel = round(wave(90, 68, 82, 2.2), 0)
  // Renderer formatRate prepends +/- and also keeps toFixed's sign, so send
  // a positive mW charging rate (isCharging true) rather than a negative
  // discharge that would render as "--12.50 W".
  const chargeRate = round(wave(21, 7200, 14800, 0.5), 0)
  return {
    ts: new Date().toISOString(),
    source: 'vendor',
    initialized: true,
    isHybrid: false,
    info: {
      cpuName: cpuModelName(),
      gpuName: 'Generic GPU',
      gpuIsIntegrated: true
    },
    cpu: {
      temperature: cpuTemp,
      usage: cpuUsage,
      fanSpeed: round(wave(16, 1800, 2600, 0.6), 1),
      power: round(wave(12, 18, 42, 0.8), 1),
      powerCores: round(wave(12, 12, 28, 0.85), 1),
      powerMemory: round(wave(18, 2, 6, 1.2), 1),
      powerPlatform: round(wave(20, 22, 48, 0.4), 1),
      voltage: round(wave(22, 1.12, 1.28, 0.15), 2),
      coreClockMax: 4200,
      coreClockAvg: cpuClock,
      pCoreClock: cpuClock,
      eCoreClock: round(cpuClock * 0.72, 0)
    },
    gpu: {
      usage: gpuUsage,
      temperature: gpuTemp,
      coreClock: gpuClock,
      memoryClock: round(wave(16, 6000, 7000, 0.2), 0),
      power: round(wave(14, 18, 55, 1.0), 1),
      voltage: round(wave(24, 0.82, 0.98, 0.4), 2),
      vramTemperature: round(wave(23, 46, 58, 1.6), 1),
      hotSpotTemperature: round(gpuTemp + 8, 1),
      vramUtilization: round(wave(31, 22, 48, 0.7), 1),
      vramUsedMb: round(wave(40, 1800, 4200, 0.1), 0),
      vramTotalMb: 8192,
      pcieRxThroughput: round(wave(8, 40, 220, 0.3), 0),
      pcieTxThroughput: round(wave(8, 20, 160, 1.1), 0),
      fanSpeed: round(wave(18, 1400, 2200, 1.3), 1)
    },
    memory: mem,
    battery: {
      chargeLevel: batteryLevel,
      health: 0.942,
      temperature: round(wave(27, 29, 35, 0.8), 1),
      avgTemperature: 31.4,
      chargeRate,
      minDischargeRate: -21000,
      maxDischargeRate: -4200,
      voltage: round(wave(33, 11.4, 12.4, 0.2), 2),
      designCapacity: 80000,
      fullChargeCapacity: 75360,
      cycleCount: 142,
      manufactureDate: '2024-03-12',
      firstUseDate: '2024-04-02',
      isCharging: true,
      isLowBattery: batteryLevel < 20,
      isLowPowerAdapter: false,
      modelName: 'Generic Battery'
    },
    motherboard: { highestTemperature: round(wave(35, 38, 46, 0.5), 1) },
    storage: { temperatures: [round(wave(41, 32, 41, 0.2), 1), round(wave(37, 34, 43, 1.8), 1)] }
  }
}

const FEATURE_KEYS = [
  'alwaysOnUsb',
  'battery',
  'batteryNightCharge',
  'flipToStart',
  'fnLock',
  'gSync',
  'hdr',
  'hybridMode',
  'igpuMode',
  'itsMode',
  'instantBoot',
  'microphone',
  'overDrive',
  'panelLogo',
  'portsBacklight',
  'powerMode',
  'refreshRate',
  'resolution',
  'dpiScale',
  'speaker',
  'touchpadLock',
  'whiteKeyboard',
  'winKey',
  'oneLevelWhiteKeyboard'
]

const FEATURE_STATE_TYPES = {
  alwaysOnUsb: 'AlwaysOnUSBState',
  battery: 'BatteryState',
  batteryNightCharge: 'BatteryNightChargeState',
  flipToStart: 'FlipToStartState',
  fnLock: 'FnLockState',
  gSync: 'GSyncState',
  hdr: 'HDRState',
  hybridMode: 'HybridModeState',
  igpuMode: 'IGPUModeState',
  itsMode: 'ITSModeState',
  instantBoot: 'InstantBootState',
  microphone: 'MicrophoneState',
  overDrive: 'OverDriveState',
  panelLogo: 'PanelLogoState',
  portsBacklight: 'PortsBacklightState',
  powerMode: 'PowerModeState',
  refreshRate: 'RefreshRate',
  resolution: 'Resolution',
  dpiScale: 'DpiScale',
  speaker: 'SpeakerState',
  touchpadLock: 'TouchpadLockState',
  whiteKeyboard: 'WhiteKeyboardBacklightState',
  winKey: 'WinKeyState',
  oneLevelWhiteKeyboard: 'OneLevelWhiteKeyboardBacklightState'
}

/** OS-level features that do not need Legion EC. Legion-only keys stay unsupported. */
const osFeatures = {
  microphone: { states: ['Off', 'On'], current: 'On' },
  speaker: { states: ['Off', 'On'], current: 'On' },
  hdr: { states: ['Off', 'On'], current: 'Off' },
  resolution: {
    states: [
      { Width: 1920, Height: 1080 },
      { Width: 1600, Height: 900 }
    ],
    current: { Width: 1600, Height: 900 }
  },
  refreshRate: {
    states: [{ Frequency: 60 }, { Frequency: 75 }],
    current: { Frequency: 60 }
  },
  dpiScale: {
    states: [{ Scale: 100 }, { Scale: 125 }, { Scale: 150 }],
    current: { Scale: 100 }
  }
}

function isOsFeature(key) {
  return Object.prototype.hasOwnProperty.call(osFeatures, key)
}

function action(key, title, description, recommended = true, applied = false) {
  return { key, title, description, recommended, applied }
}

const CATEGORIES = [
  {
    key: 'explorer',
    title: 'WindowsOptimization_Category_Explorer_Title',
    description: 'WindowsOptimization_Category_Explorer_Description',
    pluginId: null,
    hasSettings: false,
    actions: [
      action('explorer.taskbar', 'WindowsOptimization_Action_ExplorerTaskbar_Title', 'WindowsOptimization_Action_ExplorerTaskbar_Description', true, true),
      action('explorer.startMenu', 'WindowsOptimization_Action_ExplorerStartMenu_Title', 'WindowsOptimization_Action_ExplorerStartMenu_Description', false),
      action('explorer.responsiveness', 'WindowsOptimization_Action_ExplorerResponsiveness_Title', 'WindowsOptimization_Action_ExplorerResponsiveness_Description', true, true),
      action('explorer.visibility', 'WindowsOptimization_Action_ExplorerVisibility_Title', 'WindowsOptimization_Action_ExplorerVisibility_Description'),
      action('explorer.suggestions', 'WindowsOptimization_Action_ExplorerSuggestions_Title', 'WindowsOptimization_Action_ExplorerSuggestions_Description')
    ]
  },
  {
    key: 'performance',
    title: 'WindowsOptimization_Category_Performance_Title',
    description: 'WindowsOptimization_Category_Performance_Description',
    pluginId: null,
    hasSettings: false,
    actions: [
      action('performance.multimedia', 'WindowsOptimization_Action_PerformanceMultimedia_Title', 'WindowsOptimization_Action_PerformanceMultimedia_Description'),
      action('performance.memory', 'WindowsOptimization_Action_PerformanceMemory_Title', 'WindowsOptimization_Action_PerformanceMemory_Description', true, true),
      action('performance.notifications', 'WindowsOptimization_Action_PerformanceNotifications_Title', 'WindowsOptimization_Action_PerformanceNotifications_Description', false),
      action('performance.telemetry', 'WindowsOptimization_Action_PerformanceTelemetry_Title', 'WindowsOptimization_Action_PerformanceTelemetry_Description'),
      action('performance.powerPlan', 'WindowsOptimization_Action_PerformancePowerPlan_Title', 'WindowsOptimization_Action_PerformancePowerPlan_Description')
    ]
  },
  {
    key: 'services',
    title: 'WindowsOptimization_Category_Services_Title',
    description: 'WindowsOptimization_Category_Services_Description',
    pluginId: null,
    hasSettings: false,
    actions: [
      action('services.diagnostics', 'WindowsOptimization_Action_ServicesDiagnostics_Title', 'WindowsOptimization_Action_ServicesDiagnostics_Description'),
      action('services.sysmain', 'WindowsOptimization_Action_ServicesSysMain_Title', 'WindowsOptimization_Action_ServicesSysMain_Description'),
      action('services.search', 'WindowsOptimization_Action_ServicesSearch_Title', 'WindowsOptimization_Action_ServicesSearch_Description', false),
      action('services.remoteRegistry', 'WindowsOptimization_Action_ServicesRemoteRegistry_Title', 'WindowsOptimization_Action_ServicesRemoteRegistry_Description', true, true),
      action('services.errorReporting', 'WindowsOptimization_Action_ServicesErrorReporting_Title', 'WindowsOptimization_Action_ServicesErrorReporting_Description')
    ]
  },
  {
    key: 'cleanup.cache',
    title: 'WindowsOptimization_Category_CleanupCache_Title',
    description: 'WindowsOptimization_Category_CleanupCache_Description',
    pluginId: null,
    hasSettings: false,
    actions: [
      action('cleanup.browserCache', 'WindowsOptimization_Action_CleanupBrowserCache_Title', 'WindowsOptimization_Action_CleanupBrowserCache_Description'),
      action('cleanup.appLeftovers', 'WindowsOptimization_Action_CleanupAppLeftovers_Title', 'WindowsOptimization_Action_CleanupAppLeftovers_Description'),
      action('cleanup.thumbnailCache', 'WindowsOptimization_Action_CleanupThumbnailCache_Title', 'WindowsOptimization_Action_CleanupThumbnailCache_Description'),
      action('cleanup.remoteDesktopCache', 'WindowsOptimization_Action_CleanupRemoteDesktop_Title', 'WindowsOptimization_Action_CleanupRemoteDesktop_Description')
    ]
  },
  {
    key: 'cleanup.systemFiles',
    title: 'WindowsOptimization_Category_CleanupSystemFiles_Title',
    description: 'WindowsOptimization_Category_CleanupSystemFiles_Description',
    pluginId: null,
    hasSettings: false,
    actions: [
      action('cleanup.tempFiles', 'WindowsOptimization_Action_CleanupTempFiles_Title', 'WindowsOptimization_Action_CleanupTempFiles_Description'),
      action('cleanup.logs', 'WindowsOptimization_Action_CleanupLogs_Title', 'WindowsOptimization_Action_CleanupLogs_Description'),
      action('cleanup.registry', 'WindowsOptimization_Action_CleanupRegistry_Title', 'WindowsOptimization_Action_CleanupRegistry_Description', false),
      action('cleanup.crashDumps', 'WindowsOptimization_Action_CleanupCrashDumps_Title', 'WindowsOptimization_Action_CleanupCrashDumps_Description'),
      action('cleanup.recycleBin', 'WindowsOptimization_Action_CleanupRecycleBin_Title', 'WindowsOptimization_Action_CleanupRecycleBin_Description'),
      action('cleanup.defender', 'WindowsOptimization_Action_CleanupDefender_Title', 'WindowsOptimization_Action_CleanupDefender_Description', false)
    ]
  },
  {
    key: 'cleanup.systemComponents',
    title: 'WindowsOptimization_Category_CleanupSystemComponents_Title',
    description: 'WindowsOptimization_Category_CleanupSystemComponents_Description',
    pluginId: null,
    hasSettings: false,
    actions: [
      action('cleanup.windowsUpdate', 'WindowsOptimization_Action_CleanupWindowsUpdate_Title', 'WindowsOptimization_Action_CleanupWindowsUpdate_Description'),
      action('cleanup.componentStore', 'WindowsOptimization_Action_CleanupComponentStore_Title', 'WindowsOptimization_Action_CleanupComponentStore_Description'),
      action('cleanup.dotnetNative', 'WindowsOptimization_Action_CleanupDotNet_Title', 'WindowsOptimization_Action_CleanupDotNet_Description', false)
    ]
  },
  {
    key: 'cleanup.performance',
    title: 'WindowsOptimization_Category_CleanupPerformance_Title',
    description: 'WindowsOptimization_Category_CleanupPerformance_Description',
    pluginId: null,
    hasSettings: false,
    actions: [action('cleanup.prefetch', 'WindowsOptimization_Action_CleanupPrefetch_Title', 'WindowsOptimization_Action_CleanupPrefetch_Description', false)]
  },
  {
    key: 'cleanup.largeFiles',
    title: 'WindowsOptimization_Category_CleanupLargeFiles_Title',
    description: 'WindowsOptimization_Category_CleanupLargeFiles_Description',
    pluginId: null,
    hasSettings: false,
    actions: [action('cleanup.largeFiles', 'WindowsOptimization_Action_CleanupLargeFiles_Title', 'WindowsOptimization_Action_CleanupLargeFiles_Description', false)]
  },
  {
    key: 'cleanup.custom',
    title: 'WindowsOptimization_Category_CleanupCustom_Title',
    description: 'WindowsOptimization_Category_CleanupCustom_Description',
    pluginId: null,
    hasSettings: true,
    actions: [action('cleanup.custom', 'WindowsOptimization_Action_CleanupCustom_Title', 'WindowsOptimization_Action_CleanupCustom_Description', false)]
  }
]

const PLUGINS = [
  {
    id: 'custom-mouse',
    name: 'Cursor & Pointer',
    description:
      'Personalize your mouse experience with theme-aware cursor styles, Windows pointer speed, button swapping, and safe cursor backup and restore.',
    author: 'SSC-STUDIO',
    version: '2.0.0-preview.1',
    icon: 'Pen24',
    iconBackground: '#2563EB',
    tags: ['mouse', 'cursor', 'productivity'],
    isSystemPlugin: false,
    dependencies: [],
    releaseDate: '2026-01-01',
    fileSize: 0,
    updateAvailable: false,
    state: 'NotInstalled',
    directory: null,
    webPage: null,
    capabilities: {
      settingsPage: false,
      featurePage: false,
      optimizationCategory: true,
      webPage: true,
      executableEntryPoint: false
    }
  },
  {
    id: 'shell-integration',
    name: 'Nilesoft Shell Manager',
    description:
      'Manage Nilesoft Shell registration and its UDT-managed configuration. Requires Nilesoft Shell to be installed.',
    author: 'SSC-STUDIO',
    version: '2.0.0-preview.1',
    icon: 'Folder24',
    iconBackground: '#0F766E',
    tags: ['system', 'shell', 'context-menu'],
    isSystemPlugin: true,
    dependencies: [],
    releaseDate: '2026-01-01',
    fileSize: 0,
    updateAvailable: false,
    state: 'NotInstalled',
    directory: null,
    webPage: null,
    capabilities: {
      settingsPage: false,
      featurePage: false,
      optimizationCategory: true,
      webPage: true,
      executableEntryPoint: false
    }
  },
  {
    id: 'vive-tool',
    name: 'ViVeTool',
    description:
      'Unlock hidden Windows features and customize your system with ViVeTool — the ultimate Windows feature flag manager.',
    author: 'SSC-STUDIO',
    version: '2.0.0-preview.1',
    icon: 'Code24',
    iconBackground: '#7C3AED',
    tags: ['windows', 'feature-flags', 'tweaks'],
    isSystemPlugin: false,
    dependencies: [],
    releaseDate: '2026-01-01',
    fileSize: 0,
    updateAvailable: false,
    state: 'NotInstalled',
    directory: null,
    webPage: null,
    capabilities: {
      settingsPage: false,
      featurePage: false,
      optimizationCategory: false,
      webPage: true,
      executableEntryPoint: false
    }
  }
]

let dashboardConfig = {
  showSensors: true,
  sensorsRefreshIntervalSeconds: 1,
  groups: [
    {
      type: 'Power',
      items: [
        'PowerMode',
        'ItsMode',
        'BatteryMode',
        'BatteryNightChargeMode',
        'AlwaysOnUsb',
        'InstantBoot',
        'FlipToStart'
      ]
    },
    { type: 'Graphics', items: ['HybridMode', 'DiscreteGpu', 'OverclockDiscreteGpu'] },
    {
      type: 'Display',
      items: ['Resolution', 'RefreshRate', 'DpiScale', 'Hdr', 'OverDrive', 'TurnOffMonitors']
    },
    {
      type: 'Other',
      items: [
        'Microphone',
        'WhiteKeyboardBacklight',
        'PanelLogoBacklight',
        'PortsBacklight',
        'TouchpadLock',
        'FnLock',
        'WinKeyLock'
      ]
    }
  ]
}

const APPLICATION_SETTINGS = {
  Theme: 'Light',
  AccentColorSource: 'System',
  ApplyAccentColorToSystem: false,
  ApplyAccentColorToTheme: true,
  Language: 'zh-CN',
  AnimationsEnabled: true,
  MinimizeToTray: true,
  MinimizeOnClose: false,
  TemperatureUnit: 'C',
  WindowBackdropStyle: 'Windows',
  Notifications: {
    DisableNotifications: false,
    NotificationDuration: 5,
    SuccessNotifications: true,
    NotificationSound: false
  },
  NavigationItemsVisibility: {}
}

const HARDWARE_SENSORS_SETTINGS = {
  EnableHardwareSensors: true,
  SelectedGpuIsIgpu: false,
  ShowCpuAverageFrequency: true,
  DisplayMemoryInGigabytes: false,
  VisibleSections: ['CPU', 'Battery', 'GPU'],
  SectionOrder: ['CPU', 'Battery', 'GPU'],
  visibleSections: ['CPU', 'Battery', 'GPU'],
  sectionOrder: ['CPU', 'Battery', 'GPU']
}

const settingsStore = {
  application: APPLICATION_SETTINGS,
  osd: {},
  hardwareSensors: HARDWARE_SENSORS_SETTINGS,
  balanceMode: {},
  godMode: {},
  gpuOverclock: {},
  integrations: {},
  lampArray: {},
  fanCurves: {},
  packageDownloader: {},
  rgbKeyboard: {},
  spectrumKeyboard: {},
  sunriseSunset: {},
  updateCheck: { Disable: true, UpdateCheckFrequency: 'Weekly', IncludePrereleaseUpdates: false },
  networkAcceleration: {},
  batteryHealthAlerts: {},
  dashboard: { SensorsRefreshIntervalSeconds: 1, ShowSensors: true }
}

const SOFTWARE_STEPS = [
  'delay',
  'notification',
  'osd',
  'playSound',
  'run',
  'macro',
  'hideMainWindow',
  'showMainWindow',
  'displayBrightness',
  'microphone',
  'speaker',
  'hdr',
  'resolution',
  'refreshRate',
  'dpiScale',
  'quickAction'
]

let automationEnabled = true
let automationPipelines = [
  {
    id: '3f1c8a10-5b2e-4c91-9d6a-0a1b2c3d4e5f',
    name: '开机通知',
    iconName: 'Rocket24Regular',
    trigger: { $type: 'onStartup' },
    steps: [
      { $type: 'notification', text: 'Universal Device Toolkit 已启动' },
      { $type: 'delay', state: { delaySeconds: 2 } }
    ],
    isExclusive: true
  },
  {
    id: '7a9e2b44-1c0d-4f33-8e77-9b0c1d2e3f40',
    name: 'CPU 温度提醒',
    iconName: 'Gauge24Regular',
    trigger: {
      $type: 'hardwareSensor',
      metric: 'CpuTemperature',
      comparison: 'GreaterThanOrEqual',
      threshold: 90,
      duration: '00:00:05',
      cooldown: '00:01:00'
    },
    steps: [{ $type: 'notification', text: 'CPU 温度偏高' }],
    isExclusive: false
  },
  {
    id: 'c2d3e4f5-6677-8899-aabb-ccddeeff0011',
    name: '专注模式',
    iconName: 'Play24Regular',
    trigger: null,
    steps: [
      { $type: 'osd', state: 'Show' },
      { $type: 'notification', text: '已切换到专注模式' }
    ],
    isExclusive: false
  }
]

let macroEnabled = true
let macroSlots = [
  {
    key: 0x61,
    source: 'Keyboard',
    repeatCount: 1,
    ignoreDelays: false,
    interruptOnOtherKey: true,
    events: [
      { source: 'Keyboard', direction: 'Down', key: 0x41, x: 0, y: 0, delayMs: 0 },
      { source: 'Keyboard', direction: 'Up', key: 0x41, x: 0, y: 0, delayMs: 40 },
      { source: 'Keyboard', direction: 'Down', key: 0x42, x: 0, y: 0, delayMs: 80 },
      { source: 'Keyboard', direction: 'Up', key: 0x42, x: 0, y: 0, delayMs: 40 }
    ]
  }
]

let customCleanupRules = []
let gameBoostConfig = {
  autoGameBoost: true,
  boostGamePriority: true,
  optimizeCpuAffinity: true,
  suppressBackgroundProcesses: true,
  muteNotifications: false,
  gamePowerPlanGuid: null,
  customGameProcesses: [],
  backgroundWhitelist: ['obs64', 'discord', 'steam', 'code']
}
let gameBoostStatus = {
  isBoosting: false,
  activeGameProcess: null,
  activeGameProcessId: null,
  boostedProcesses: [],
  suppressedProcessesCount: 0
}

let sensorTimer = null
let networkConfig = {
  accelerationEnabled: false,
  mode: 'Off',
  listenPort: 7890,
  domainGroups: [
    {
      id: 'steam',
      displayName: 'Steam',
      enabled: false,
      isFavorite: false,
      domains: ['steampowered.com'],
      subItems: [],
      iconKey: 'steam',
      description: null
    },
    {
      id: 'github',
      displayName: 'GitHub',
      enabled: false,
      isFavorite: false,
      domains: ['github.com'],
      subItems: [],
      iconKey: 'github',
      description: null
    }
  ],
  dnsServer: null,
  dohUrl: null,
  certificateFingerprintSha256: null,
  lastRecoverySnapshot: null,
  showInNavigation: true
}

function emitSettingsChanged(scope) {
  write({ event: 'settings.changed', data: { scope, reason: 'set' } })
}

function requireFeature(params) {
  const feature = params?.feature
  if (typeof feature !== 'string' || feature.length === 0) {
    throw new RpcError(INVALID_PARAMS, "Missing string parameter 'feature'.")
  }
  if (!FEATURE_KEYS.includes(feature)) {
    throw new RpcError(INVALID_PARAMS, `Unknown feature '${feature}'.`)
  }
  return feature
}

function handle(method, params) {
  switch (method) {
    case 'ping':
      return { pong: true, version: '6.0.0-linux-stub' }
    case 'app.getStatus':
      return { pid: process.pid, version: '6.0.0', logPath: '/tmp/udt-stub-host', culture: 'zh-CN' }
    case 'app.getLogPath':
      return { path: '/tmp/udt-stub-host' }
    case 'app.quit':
      setTimeout(() => process.exit(0), 50)
      return { quitting: true }
    case 'app.update.check':
      return { available: false, version: null, error: null }
    case 'app.update.status':
      return { status: 'idle', disable: false }
    case 'app.getAutorun':
      return { state: 'Disabled', enabled: false }
    case 'app.setAutorun':
      return { ok: false, enabled: false, state: 'Disabled' }
    case 'system.info':
      return {
        vendor: 'Generic',
        model: 'Linux Desktop',
        machineType: null,
        biosVersion: null,
        serialNumber: null,
        isCompatible: true
      }
    case 'device.info':
      return {
        vendor: 'Generic',
        model: 'Linux Desktop',
        machineType: null,
        serialNumber: null,
        biosVersion: null,
        processor: {
          name: cpuModelName(),
          numberOfCores: Math.max(1, Math.round(cpus().length / 2)),
          numberOfLogicalProcessors: cpus().length,
          maxClockSpeedMHz: 3400
        },
        videoController: {
          name: 'Generic GPU',
          adapterCompatibility: platform(),
          adapterRamBytes: 8 * 1024 * 1024 * 1024
        },
        memory: {
          totalCapacityBytes: totalmem(),
          moduleCount: 1,
          configuredClockSpeedMHz: 3200,
          speedMHz: 3200
        },
        warranty: null
      }
    case 'system.powerAdapterStatus':
      return { status: 'Disconnected' }
    case 'system.accentColor.get':
      return { r: 79, g: 157, b: 247 }
    case 'system.accentColor.set':
      return { applied: false }
    case 'localization.getCulture':
      return { culture: 'zh-CN' }
    case 'localization.setCulture':
      return { culture: params?.culture ?? 'zh-CN' }
    case 'dashboard.getConfig':
      return dashboardConfig
    case 'dashboard.saveConfig':
      if (params?.config && typeof params.config === 'object') dashboardConfig = params.config
      return { saved: true }
    case 'feature.list':
      return {
        features: FEATURE_KEYS.map((key) => ({
          key,
          supported: isOsFeature(key),
          stateType: FEATURE_STATE_TYPES[key] ?? 'String'
        }))
      }
    case 'feature.getSupported': {
      const feature = requireFeature(params)
      return { supported: isOsFeature(feature) }
    }
    case 'feature.getStates': {
      const feature = requireFeature(params)
      if (!isOsFeature(feature)) throw new RpcError(FEATURE_NOT_SUPPORTED, LEGION_REASON)
      return { states: osFeatures[feature].states }
    }
    case 'feature.getState': {
      const feature = requireFeature(params)
      if (!isOsFeature(feature)) throw new RpcError(FEATURE_NOT_SUPPORTED, LEGION_REASON)
      return { state: osFeatures[feature].current }
    }
    case 'feature.setState': {
      const feature = requireFeature(params)
      if (!isOsFeature(feature)) throw new RpcError(FEATURE_NOT_SUPPORTED, LEGION_REASON)
      osFeatures[feature].current = params?.state
      return { ok: true }
    }
    case 'feature.isHdrBlocked':
      return { blocked: false }
    case 'sensors.getStatus': {
      const snap = buildSnapshot()
      return {
        initialized: true,
        isHybrid: false,
        cpuName: snap.info.cpuName,
        gpuName: snap.info.gpuName,
        gpuIsIntegrated: snap.info.gpuIsIntegrated,
        initialState: 'ready'
      }
    }
    case 'sensors.getSnapshot':
    case 'sensors.getDetailed':
      return buildSnapshot()
    case 'sensors.subscribe': {
      if (sensorTimer) clearInterval(sensorTimer)
      const intervalSec = Math.max(0.5, Number(params?.intervalSec ?? 1))
      sensorTimer = setInterval(() => {
        write({ event: 'sensors.updated', data: buildSnapshot() })
      }, intervalSec * 1000)
      return { subscribed: true, effectiveIntervalSec: intervalSec }
    }
    case 'sensors.unsubscribe':
      if (sensorTimer) {
        clearInterval(sensorTimer)
        sensorTimer = null
      }
      return { unsubscribed: true }
    case 'sensors.getSettings':
      return {
        enableHardwareSensors: true,
        osdRefreshIntervalSec: 1,
        selectedGpuIsIgpu: false,
        showCpuAverageFrequency: true,
        displayMemoryInGigabytes: false,
        visibleSections: ['cpu', 'battery', 'gpu'],
        sectionOrder: ['cpu', 'battery', 'gpu']
      }
    case 'sensors.setSettings':
      return { saved: true }
    case 'sensors.getFps':
      return { process: null, fps: null, lowFps: null, frameTimeMs: null }
    case 'sensors.subscribeFps':
    case 'sensors.unsubscribeFps':
      return { monitoring: false }
    case 'settings.getAll': {
      const requested = Array.isArray(params?.scopes) ? params.scopes : null
      if (!requested) return { scopes: settingsStore }
      const scopes = {}
      for (const scope of requested) scopes[scope] = settingsStore[scope] ?? {}
      return { scopes }
    }
    case 'settings.get': {
      const scope = params?.scope ?? ''
      return { scope, value: settingsStore[scope] ?? {} }
    }
    case 'settings.set': {
      const scope = params?.scope ?? ''
      if (scope && params?.value != null && typeof params.value === 'object') {
        settingsStore[scope] = { ...(settingsStore[scope] ?? {}), ...params.value }
        emitSettingsChanged(scope)
      }
      return { scope, applied: true }
    }
    case 'settings.save':
      return { saved: Array.isArray(params?.scopes) ? params.scopes : Object.keys(settingsStore) }
    case 'settings.reload':
      return { reloaded: true }
    case 'optimization.getCategories':
      return { categories: CATEGORIES }
    case 'optimization.apply': {
      const keys = new Set(Array.isArray(params?.actionKeys) ? params.actionKeys : [])
      for (const category of CATEGORIES) {
        for (const item of category.actions) {
          if (keys.has(item.key)) item.applied = true
        }
      }
      return { applied: true }
    }
    case 'optimization.revert': {
      const keys = new Set(Array.isArray(params?.actionKeys) ? params.actionKeys : [])
      for (const category of CATEGORIES) {
        for (const item of category.actions) {
          if (keys.has(item.key)) item.applied = false
        }
      }
      return { reverted: true }
    }
    case 'optimization.applyRecommended':
      for (const category of CATEGORIES) {
        for (const item of category.actions) {
          if (item.recommended) item.applied = true
        }
      }
      return { applied: true }
    case 'optimization.getActionStatus': {
      for (const category of CATEGORIES) {
        const found = category.actions.find((item) => item.key === params?.actionKey)
        if (found) return { applied: found.applied }
      }
      return { applied: null }
    }
    case 'cleanup.estimate':
      return { bytes: 384 * 1024 * 1024 }
    case 'cleanup.run':
      return { done: true }
    case 'cleanup.getCustomRules':
      return { rules: customCleanupRules }
    case 'cleanup.saveCustomRules':
      customCleanupRules = Array.isArray(params?.rules) ? params.rules : customCleanupRules
      return { saved: true }
    case 'network.getStatus':
      return {
        config: networkConfig,
        isBackendReady: true,
        isRunning: false,
        statusText: 'Stopped'
      }
    case 'network.saveConfig':
      if (params?.config && typeof params.config === 'object') networkConfig = params.config
      return { saved: true }
    case 'network.start':
    case 'network.stop':
    case 'network.restore':
      return { ok: true }
    case 'network.getTrafficSnapshot':
      return { bytesUploaded: 0, bytesDownloaded: 0, activeConnections: 0, totalConnections: 0 }
    case 'network.getRuntimeSnapshot':
      return {
        healthStatus: 'Stopped',
        traffic: { bytesUploaded: 0, bytesDownloaded: 0, activeConnections: 0, totalConnections: 0 },
        connections: [],
        destinations: []
      }
    case 'network.detectNat':
      return { natType: 'Unknown', localIp: null, publicIp: null, internetAvailable: false, error: null }
    case 'network.detectDns':
      return { success: false, elapsedMs: 0, addresses: [], error: 'DNS probe is not available on this Linux session' }
    case 'network.detectIpv6':
      return { supported: false, address: null, error: null }
    case 'driver.getSettings':
      return {
        machineType: '',
        os: 'linux',
        osOptions: ['linux'],
        downloadPath: '/tmp',
        onlyShowUpdates: false,
        hiddenPackageIds: []
      }
    case 'driver.getPackages':
    case 'driver.getPackageStatuses':
      return { packages: [] }
    case 'driver.start':
    case 'driver.pause':
    case 'driver.install':
    case 'driver.uninstall':
      return { ok: false }
    case 'driver.setDownloadPath':
    case 'driver.setOnlyShowUpdates':
    case 'driver.setHiddenPackageIds':
      return { saved: true }
    case 'automation.getState':
      return { isEnabled: automationEnabled, pipelines: automationPipelines }
    case 'automation.setEnabled':
      automationEnabled = Boolean(params?.enabled)
      return { ok: true }
    case 'automation.savePipelines':
      if (Array.isArray(params?.pipelines)) automationPipelines = params.pipelines
      if (typeof params?.isEnabled === 'boolean') automationEnabled = params.isEnabled
      return { saved: true }
    case 'automation.runNow':
      return { ok: true }
    case 'automation.getSupportedSteps':
      return { steps: SOFTWARE_STEPS }
    case 'macro.getState':
      return { isEnabled: macroEnabled, slots: macroSlots }
    case 'macro.setEnabled':
      macroEnabled = Boolean(params?.enabled)
      return { ok: true }
    case 'macro.play':
      return { ok: true }
    case 'macro.startRecording':
      return { ok: true }
    case 'macro.saveSequence': {
      const next = {
        key: params?.key,
        source: 'Keyboard',
        repeatCount: params?.repeatCount ?? 1,
        ignoreDelays: Boolean(params?.ignoreDelays),
        interruptOnOtherKey: Boolean(params?.interruptOnOtherKey),
        events: Array.isArray(params?.events) ? params.events : []
      }
      const index = macroSlots.findIndex((slot) => slot.key === next.key)
      if (index >= 0) macroSlots[index] = next
      else macroSlots.push(next)
      return { ok: true }
    }
    case 'macro.clearSequence':
      macroSlots = macroSlots.filter((slot) => slot.key !== params?.key)
      return { ok: true }
    case 'macro.stopRecording':
      return { events: [] }
    case 'plugins.list':
      return { plugins: PLUGINS, online: false }
    case 'plugins.checkUpdates':
      return { updates: [] }
    case 'plugins.install':
    case 'plugins.uninstall':
    case 'plugins.import':
      return { ok: false, degraded: true, unloadPending: false, error: 'Plugins cannot load without Windows Host' }
    case 'plugins.refresh':
      return { ok: true, registeredCount: 0, degraded: true, unloadPending: false, failures: [] }
    case 'software.getStatus':
      return { status: 'NotFound', isLegionMachine: false }
    case 'software.setEnabled':
      return { ok: false, status: 'NotFound' }
    case 'ai.getStatus':
      return { supported: false, available: false, enabled: false }
    case 'ai.setEnabled':
      return { ok: false }
    case 'godMode.getState':
      return {}
    case 'godMode.setState':
    case 'godMode.apply':
      return { ok: false }
    case 'dashboardHardware.getState':
      return {
        discreteGpu: { supported: false, state: 'Unknown', performanceState: null, processes: [] },
        overclockDiscreteGpu: {
          supported: false,
          enabled: false,
          coreDeltaMhz: 0,
          memoryDeltaMhz: 0,
          maxCoreDeltaMhz: 0,
          maxMemoryDeltaMhz: 0
        },
        turnOffMonitors: { supported: false }
      }
    case 'dashboardHardware.setMonitoring':
      return { ok: true, monitoring: Boolean(params?.enabled) }
    case 'dashboardHardware.killGpuProcesses':
    case 'dashboardHardware.restartGpu':
    case 'dashboardHardware.setOverclock':
    case 'dashboardHardware.turnOffMonitors':
      return { ok: false }
    case 'dashboardHardware.setOverclockEnabled':
      return { ok: false, enabled: false }
    case 'wmi.getGodModeFnQ':
      return { supported: false, enabled: null }
    case 'wmi.setGodModeFnQ':
      return { ok: false }
    case 'keyboard.detect':
      return { mode: 'none' }
    case 'rgb.isSupported':
    case 'spectrum.isSupported':
      return { supported: false }
    case 'bootLogo.getStatus':
    case 'bootLogo.enable':
    case 'bootLogo.disable':
      throw new RpcError(FEATURE_NOT_SUPPORTED, LEGION_REASON)
    case 'update.getRelease':
      return { release: null }
    case 'update.download':
      return { ok: false, error: 'Updates are disabled in the Linux demo Host' }
    case 'update.launchInstaller':
      return { ok: false }
    case 'power.restart':
      return { ok: false }
    case 'powerPlans.getList':
      return { plans: [] }
    case 'powerPlans.setActive':
      return { ok: false }
    case 'gameBoost.getStatus':
      return gameBoostStatus
    case 'gameBoost.getConfig':
      return gameBoostConfig
    case 'gameBoost.saveConfig':
      if (params?.config && typeof params.config === 'object') gameBoostConfig = params.config
      return { saved: true }
    case 'gameBoost.boostNow':
    case 'gameBoost.revertNow':
      return { success: true, status: gameBoostStatus }
    default:
      return {}
  }
}

process.stdin.setEncoding('utf8')
const rl = createInterface({ input: process.stdin })
rl.on('line', (line) => {
  const trimmed = line.trim()
  if (!trimmed) return
  let message
  try {
    message = JSON.parse(trimmed)
  } catch {
    return
  }
  if (typeof message?.id !== 'number' || typeof message.method !== 'string') return
  try {
    const result = handle(message.method, message.params ?? {})
    write({ id: message.id, result })
  } catch (error) {
    const code = error instanceof RpcError ? error.code : -32603
    write({
      id: message.id,
      error: { code, message: error instanceof Error ? error.message : String(error) }
    })
  }
})

write({
  event: 'host.ready',
  data: {
    version: '6.0.0-linux-stub',
    platform: platform(),
    arch: arch(),
    osRelease: release(),
    hostname: hostname(),
    hardware: false
  }
})
