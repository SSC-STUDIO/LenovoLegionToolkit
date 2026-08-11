import { BrowserWindow, screen } from 'electron'
import { hostClient } from './host-client'

/**
 * On-screen display (OSD) —port of the WPF OsdWindowBase family:
 *
 * - OsdWindowBase.cs: frameless, transparent, always-on-top window; saved
 *   position restore, edge snapping (SnapThreshold), click-through while
 *   locked, sensor refresh loop (OsdRefreshInterval), severity coloring.
 * - OsdBarWindow.xaml(.cs): horizontal bar at the top center.
 * - OsdPanelWindow.xaml(.cs): vertical panel at the left edge.
 *
 * Style ("Panel" | "Bar"), appearance, thresholds and sensor items come from
 * the "osd" settings scope (osd.json). Position is persisted back into the
 * same scope on drag end. Sensor data is polled via sensors.getSnapshot and
 * FPS via sensors.subscribeFps (ref-counted in the host, so the renderer can
 * subscribe at the same time). OSD visibility is driven by the host's
 * "osd.changed" events (automation steps) and the showOsd setting.
 */

type OsdState = 'Hidden' | 'Show' | 'Toggle'

interface OsdEventData {
  state: OsdState
}

/** Snapshot projection —mirrors api/sensors.ts SensorSnapshot (camelCase). */
interface OsdSnapshot {
  ts?: string
  isHybrid?: boolean
  cpu?: {
    temperature?: number | null
    usage?: number | null
    fanSpeed?: number | null
    power?: number | null
    coreClockMax?: number | null
    coreClockAvg?: number | null
    pCoreClock?: number | null
    eCoreClock?: number | null
  }
  gpu?: {
    usage?: number | null
    temperature?: number | null
    coreClock?: number | null
    power?: number | null
    vramTemperature?: number | null
    vramUtilization?: number | null
    vramUsedMb?: number | null
    vramTotalMb?: number | null
    fanSpeed?: number | null
  }
  memory?: {
    usage?: number | null
    usedMb?: number | null
    totalMb?: number | null
    highestTemperature?: number | null
  }
  motherboard?: {
    highestTemperature?: number | null
  }
  storage?: {
    temperatures?: (number | null)[]
  }
}

/** FPS projection —mirrors MapFpsData in SensorsHandlers.cs. */
interface OsdFpsData {
  fps?: number | null
  lowFps?: number | null
  frameTimeMs?: number | null
}

/** "osd" settings scope —mirrors OsdSettingsStore in UniversalDeviceToolkit.Lib. */
interface OsdSettingsStore {
  showOsd: boolean
  osdRefreshInterval: number
  selectedStyleIndex: number
  items: string[]
  backgroundOpacity: number
  backgroundColor: string
  fontSize: number
  cornerRadiusTop: number
  cornerRadiusBottom: number
  isLocked: boolean
  panelPositionX: number | null
  panelPositionY: number | null
  barPositionX: number | null
  barPositionY: number | null
  tempThresholdWarning: number
  tempThresholdCritical: number
  usageThresholdWarning: number
  usageThresholdCritical: number
  fpsThresholdCritical: number
  lowFpsDeltaThreshold: number
  categoryColor: string
  labelColor: string
  valueColor: string
  warningColor: string
  criticalColor: string
  separatorColor: string
  snapThreshold: number
}

/** OsdItem enum names (Enums.cs) —persisted verbatim in osd.json. */
const OSD_ITEMS = [
  'Fps',
  'LowFps',
  'FrameTime',
  'CpuFrequency',
  'CpuPCoreFrequency',
  'CpuECoreFrequency',
  'CpuUtilization',
  'CpuTemperature',
  'CpuPower',
  'CpuFan',
  'GpuFrequency',
  'GpuUtilization',
  'GpuTemperature',
  'GpuVramUtilization',
  'GpuVramTemperature',
  'GpuPower',
  'GpuFan',
  'MemoryUtilization',
  'MemoryTemperature',
  'Disk1Temperature',
  'Disk2Temperature',
  'PchTemperature',
  'PchFan'
] as const

type OsdItemName = (typeof OSD_ITEMS)[number]

const DEFAULT_OSD_SETTINGS: OsdSettingsStore = {
  showOsd: false,
  osdRefreshInterval: 1,
  selectedStyleIndex: 0,
  items: [...OSD_ITEMS],
  backgroundOpacity: 0.6,
  backgroundColor: '#1E1E1E',
  fontSize: 12,
  cornerRadiusTop: 6,
  cornerRadiusBottom: 6,
  isLocked: false,
  panelPositionX: null,
  panelPositionY: null,
  barPositionX: null,
  barPositionY: null,
  tempThresholdWarning: 75,
  tempThresholdCritical: 90,
  usageThresholdWarning: 70,
  usageThresholdCritical: 90,
  fpsThresholdCritical: 30,
  lowFpsDeltaThreshold: 30,
  categoryColor: '#2196F3',
  labelColor: '#ADFF2F',
  valueColor: '#FFFFFF',
  warningColor: '#FFFF00',
  criticalColor: '#FF0000',
  separatorColor: '#555555',
  snapThreshold: 20
}

/** OsdItem label map (Resource.en.resx: Osd_*, OsdItem_*, SensorsControl_*). */
const ITEM_LABELS: Record<OsdItemName, string> = {
  Fps: 'FPS',
  LowFps: '1% Low',
  FrameTime: 'Frame Time',
  CpuFrequency: 'Core Clock',
  CpuPCoreFrequency: 'P-Core Clock',
  CpuECoreFrequency: 'E-Core Clock',
  CpuUtilization: 'Utilization',
  CpuTemperature: 'Temperature',
  CpuPower: 'Power',
  CpuFan: 'Fan',
  GpuFrequency: 'Core Clock',
  GpuUtilization: 'Utilization',
  GpuTemperature: 'Core Temp',
  GpuVramUtilization: 'VRAM Utilization',
  GpuVramTemperature: 'VRAM Temp',
  GpuPower: 'Power',
  GpuFan: 'Fan',
  MemoryUtilization: 'Utilization',
  MemoryTemperature: 'Temperature',
  Disk1Temperature: 'Disk 1 Temperature',
  Disk2Temperature: 'Disk 2 Temperature',
  PchTemperature: 'PCH Temperature',
  PchFan: 'Fan'
}

/** Group definitions —mirrors the _measurementGroups of the WPF windows. */
interface OsdGroupDef {
  label: string
  items: OsdItemName[]
}

const PANEL_GROUPS: OsdGroupDef[] = [
  { label: 'FPS', items: ['Fps', 'LowFps', 'FrameTime'] },
  {
    label: 'CPU',
    items: [
      'CpuFrequency',
      'CpuPCoreFrequency',
      'CpuECoreFrequency',
      'CpuUtilization',
      'CpuTemperature',
      'CpuPower',
      'CpuFan'
    ]
  },
  {
    label: 'GPU',
    items: [
      'GpuFrequency',
      'GpuUtilization',
      'GpuTemperature',
      'GpuVramUtilization',
      'GpuVramTemperature',
      'GpuPower',
      'GpuFan'
    ]
  },
  { label: 'RAM', items: ['MemoryUtilization', 'MemoryTemperature'] },
  {
    label: 'PCH',
    items: ['PchTemperature', 'PchFan', 'Disk1Temperature', 'Disk2Temperature']
  }
]

const BAR_GROUPS: OsdGroupDef[] = [
  { label: 'FPS', items: ['Fps', 'LowFps', 'FrameTime'] },
  {
    label: 'CPU',
    items: [
      'CpuFrequency',
      'CpuPCoreFrequency',
      'CpuECoreFrequency',
      'CpuUtilization',
      'CpuTemperature',
      'CpuPower',
      'CpuFan'
    ]
  },
  {
    label: 'GPU',
    items: [
      'GpuFrequency',
      'GpuUtilization',
      'GpuTemperature',
      'GpuVramUtilization',
      'GpuVramTemperature',
      'GpuPower',
      'GpuFan'
    ]
  },
  { label: 'RAM', items: ['MemoryUtilization', 'MemoryTemperature'] },
  {
    label: 'PCH',
    items: ['PchTemperature', 'PchFan', 'Disk1Temperature', 'Disk2Temperature']
  }
]

const OSD_WIDTH = 320
const OSD_HEIGHT = 96

let osdWindow: BrowserWindow | null = null
let unsubscribe: (() => void) | null = null
let unsubscribeSettings: (() => void) | null = null
let unsubscribeFps: (() => void) | null = null
let unsubscribeDisplay: (() => void) | null = null

let settings: OsdSettingsStore = { ...DEFAULT_OSD_SETTINGS }
let showCpuAverageFrequency = false
let displayMemoryInGigabytes = false
let temperatureUnit: 'C' | 'F' = 'C'

let lastSnapshot: OsdSnapshot | null = null
let lastFps: OsdFpsData | null = null

let visible = false
let pageLoaded = false
let refreshTimer: NodeJS.Timeout | null = null
let refreshInFlight = false
let fpsSubscribed = false
let positionSaveTimer: NodeJS.Timeout | null = null
let lastAppearanceSignature = ''

// ── settings ────────────────────────────────────────────────────────────────

function mergeSettings(value: unknown): void {
  if (!value || typeof value !== 'object') return
  // The host serializes settings stores with their .NET property names
  // (PascalCase); the in-memory model below uses camelCase.
  const raw = value as Record<string, string | number | boolean | unknown[] | null>
  const merged: OsdSettingsStore = { ...DEFAULT_OSD_SETTINGS, ...settings }

  const take = <K extends keyof OsdSettingsStore>(
    pascal: string,
    key: K,
    fallback: OsdSettingsStore[K]
  ): void => {
    const v = raw[pascal]
    merged[key] = v === undefined || v === null ? fallback : (v as OsdSettingsStore[K])
  }

  take('ShowOsd', 'showOsd', settings.showOsd)
  take('OsdRefreshInterval', 'osdRefreshInterval', settings.osdRefreshInterval)
  take('SelectedStyleIndex', 'selectedStyleIndex', settings.selectedStyleIndex)
  take('BackgroundOpacity', 'backgroundOpacity', settings.backgroundOpacity)
  take('BackgroundColor', 'backgroundColor', settings.backgroundColor)
  take('FontSize', 'fontSize', settings.fontSize)
  take('CornerRadiusTop', 'cornerRadiusTop', settings.cornerRadiusTop)
  take('CornerRadiusBottom', 'cornerRadiusBottom', settings.cornerRadiusBottom)
  take('IsLocked', 'isLocked', settings.isLocked)
  take('PanelPositionX', 'panelPositionX', settings.panelPositionX)
  take('PanelPositionY', 'panelPositionY', settings.panelPositionY)
  take('BarPositionX', 'barPositionX', settings.barPositionX)
  take('BarPositionY', 'barPositionY', settings.barPositionY)
  take('TempThresholdWarning', 'tempThresholdWarning', settings.tempThresholdWarning)
  take('TempThresholdCritical', 'tempThresholdCritical', settings.tempThresholdCritical)
  take('UsageThresholdWarning', 'usageThresholdWarning', settings.usageThresholdWarning)
  take('UsageThresholdCritical', 'usageThresholdCritical', settings.usageThresholdCritical)
  take('FpsThresholdCritical', 'fpsThresholdCritical', settings.fpsThresholdCritical)
  take('LowFpsDeltaThreshold', 'lowFpsDeltaThreshold', settings.lowFpsDeltaThreshold)
  take('CategoryColor', 'categoryColor', settings.categoryColor)
  take('LabelColor', 'labelColor', settings.labelColor)
  take('ValueColor', 'valueColor', settings.valueColor)
  take('WarningColor', 'warningColor', settings.warningColor)
  take('CriticalColor', 'criticalColor', settings.criticalColor)
  take('SeparatorColor', 'separatorColor', settings.separatorColor)
  take('SnapThreshold', 'snapThreshold', settings.snapThreshold)

  if (Array.isArray(raw['Items'])) {
    merged.items = (raw['Items'] as unknown[]).filter(
      (item): item is OsdItemName =>
        typeof item === 'string' && (OSD_ITEMS as readonly string[]).includes(item)
    )
  }

  settings = merged
}

/** Convert the camelCase model back to the PascalCase host store shape. */
function toHostStore(store: OsdSettingsStore): Record<string, unknown> {
  const pascal = (key: string): string => key.charAt(0).toUpperCase() + key.slice(1)
  const result: Record<string, unknown> = {}
  for (const [key, value] of Object.entries(store)) {
    result[pascal(key)] = value
  }
  return result
}

async function readSettings(): Promise<void> {
  try {
    const result = (await hostClient.invoke('settings.get', { scope: 'osd' })) as
      | { value?: unknown }
      | null
      | undefined
    mergeSettings(result?.value)
  } catch (error) {
    console.error('[osd] failed to read settings:', error)
  }
}

/** Retry the initial read —the host may still be starting up. */
async function readSettingsWithRetry(attempts = 10): Promise<void> {
  for (let attempt = 0; attempt < attempts; attempt++) {
    try {
      const result = (await hostClient.invoke('settings.get', { scope: 'osd' })) as
        | { value?: unknown }
        | null
        | undefined
      mergeSettings(result?.value)
      return
    } catch {
      await new Promise((resolve) => setTimeout(resolve, 500))
    }
  }
}

async function readSiblingSettings(): Promise<void> {
  try {
    const [hardware, application] = (await Promise.all([
      hostClient.invoke('settings.get', { scope: 'hardwareSensors' }),
      hostClient.invoke('settings.get', { scope: 'application' })
    ])) as [{ value?: Record<string, unknown> }, { value?: Record<string, unknown> }]
    const hardwareRaw = (hardware.value ?? {}) as Record<string, unknown>
    const applicationRaw = (application.value ?? {}) as Record<string, unknown>
    showCpuAverageFrequency = hardwareRaw['ShowCpuAverageFrequency'] === true
    displayMemoryInGigabytes = hardwareRaw['DisplayMemoryInGigabytes'] === true
    const unit = applicationRaw['TemperatureUnit']
    temperatureUnit = unit === 'F' ? 'F' : 'C'
  } catch {
    // Keep defaults when the host is not reachable yet.
  }
}

/** Persist the whole in-memory store; settings.set replaces the full scope. */
async function writeSettings(): Promise<void> {
  try {
    await hostClient.invoke('settings.set', { scope: 'osd', value: toHostStore(settings) })
    await hostClient.invoke('settings.save', { scopes: ['osd'] })
  } catch (error) {
    console.error('[osd] failed to save settings:', error)
  }
}

// ── window lifecycle ────────────────────────────────────────────────────────

function isBarStyle(): boolean {
  return settings.selectedStyleIndex === 1
}

function savedPosition(): { x: number | null; y: number | null } {
  return isBarStyle()
    ? { x: settings.barPositionX, y: settings.barPositionY }
    : { x: settings.panelPositionX, y: settings.panelPositionY }
}

function savePosition(x: number, y: number): void {
  if (isBarStyle()) {
    settings.barPositionX = x
    settings.barPositionY = y
  } else {
    settings.panelPositionX = x
    settings.panelPositionY = y
  }
  if (positionSaveTimer) clearTimeout(positionSaveTimer)
  positionSaveTimer = setTimeout(() => {
    positionSaveTimer = null
    void writeSettings()
  }, 400)
}

function isPositionOnScreen(x: number, y: number): boolean {
  try {
    screen.getDisplayMatching({ x, y, width: 1, height: 1 })
    return true
  } catch {
    return false
  }
}

function setDefaultWindowPosition(): void {
  const win = osdWindow
  if (!win || win.isDestroyed()) return
  const { workArea } = screen.getPrimaryDisplay()
  const [width, height] = win.getSize()
  if (isBarStyle()) {
    win.setPosition(
      Math.round(workArea.x + (workArea.width - width) / 2),
      Math.round(workArea.y)
    )
  } else {
    win.setPosition(
      Math.round(workArea.x),
      Math.round(workArea.y + (workArea.height - height) / 2)
    )
  }
}

function setWindowPosition(): void {
  const win = osdWindow
  if (!win || win.isDestroyed()) return
  const saved = savedPosition()
  if (saved.x !== null && saved.y !== null && isPositionOnScreen(saved.x, saved.y)) {
    win.setPosition(Math.round(saved.x), Math.round(saved.y))
    return
  }
  setDefaultWindowPosition()
}

/** WPF OnMouseLeftButtonDown snapping + clamping against the work area. */
function snapAndClampPosition(): void {
  const win = osdWindow
  if (!win || win.isDestroyed()) return
  const [x, y] = win.getPosition()
  const [width, height] = win.getSize()
  const { workArea } = screen.getDisplayMatching({ x, y, width, height })
  const threshold = Math.max(0, settings.snapThreshold)

  let left = x
  let top = y
  if (Math.abs(left - workArea.x) < threshold) left = workArea.x
  else if (Math.abs(workArea.x + workArea.width - (left + width)) < threshold) {
    left = workArea.x + workArea.width - width
  }

  if (Math.abs(top - workArea.y) < threshold) top = workArea.y
  else if (Math.abs(workArea.y + workArea.height - (top + height)) < threshold) {
    top = workArea.y + workArea.height - height
  }

  left = Math.min(Math.max(left, workArea.x), workArea.x + workArea.width - width)
  top = Math.min(Math.max(top, workArea.y), workArea.y + workArea.height - height)

  if (left !== x || top !== y) win.setPosition(Math.round(left), Math.round(top))
  savePosition(Math.round(left), Math.round(top))
}

function onDisplayMetricsChanged(): void {
  const win = osdWindow
  if (!win || win.isDestroyed()) return
  const [x, y] = win.getPosition()
  if (!isPositionOnScreen(x, y)) setDefaultWindowPosition()
}

// ── data refresh ────────────────────────────────────────────────────────────

const FPS_ITEMS: OsdItemName[] = ['Fps', 'LowFps', 'FrameTime']

function fpsItemsActive(): boolean {
  return FPS_ITEMS.some((item) => settings.items.includes(item))
}

function updateFpsSubscription(): void {
  const shouldSubscribe = visible && fpsItemsActive()
  if (shouldSubscribe && !fpsSubscribed) {
    fpsSubscribed = true
    unsubscribeFps = hostClient.on('sensors.fpsUpdated', (data) => {
      if (!visible) return
      lastFps = (data ?? null) as OsdFpsData | null
      updateValues()
    })
    void hostClient.invoke('sensors.subscribeFps', {}).catch((error) => {
      console.error('[osd] failed to subscribe FPS:', error)
    })
  } else if (!shouldSubscribe && fpsSubscribed) {
    fpsSubscribed = false
    unsubscribeFps?.()
    unsubscribeFps = null
    void hostClient.invoke('sensors.unsubscribeFps', {}).catch(() => undefined)
  }
}

function startRefresh(): void {
  stopRefresh()
  const intervalMs = Math.max(500, Math.round(settings.osdRefreshInterval * 1000))
  refreshTimer = setInterval(() => {
    void refreshOnce()
  }, intervalMs)
  void refreshOnce()
  updateFpsSubscription()
}

function stopRefresh(): void {
  if (refreshTimer) {
    clearInterval(refreshTimer)
    refreshTimer = null
  }
  refreshInFlight = false
}

async function refreshOnce(): Promise<void> {
  if (refreshInFlight || !visible) return
  refreshInFlight = true
  try {
    const snapshot = (await hostClient.invoke('sensors.getSnapshot', {})) as OsdSnapshot | null
    if (visible && snapshot) lastSnapshot = snapshot
  } catch {
    // Keep the last known values when a poll fails.
  } finally {
    refreshInFlight = false
  }
  updateValues()
}

// ── rendering ───────────────────────────────────────────────────────────────

function isHybrid(): boolean {
  return lastSnapshot?.isHybrid === true
}

/** WPF UpdateMeasurementControlsVisibility: hybrid CPUs show P/E cores only. */
function isItemVisible(item: OsdItemName): boolean {
  if (!settings.items.includes(item)) return false
  if (isHybrid()) {
    if (item === 'CpuFrequency') return false
  } else if (item === 'CpuPCoreFrequency' || item === 'CpuECoreFrequency') {
    return false
  }
  return true
}

function groupVisible(group: OsdGroupDef): boolean {
  return group.items.some(isItemVisible)
}

interface ValueRender {
  text: string
  color: string
}

function severityColor(value: number, warning: number, critical: number, base: string): string {
  if (value >= critical) return settings.criticalColor
  return value >= warning ? settings.warningColor : base
}

function dash(): ValueRender {
  return { text: '-', color: settings.valueColor }
}

function formatTemperature(raw: number): string {
  if (temperatureUnit === 'F') {
    return `${(raw * 9) / 5 + 32}°F`
  }
  return `${raw}°C`
}

function temperatureValue(raw: number | null | undefined): ValueRender {
  if (typeof raw !== 'number' || Number.isNaN(raw) || raw < 0) return dash()
  return {
    text: formatTemperature(raw),
    color: severityColor(
      raw,
      settings.tempThresholdWarning,
      settings.tempThresholdCritical,
      settings.valueColor
    )
  }
}

function usageValue(raw: number | null | undefined): ValueRender {
  if (typeof raw !== 'number' || Number.isNaN(raw) || raw < 0) return dash()
  return {
    text: `${raw.toFixed(0)}%`,
    color: severityColor(
      raw,
      settings.usageThresholdWarning,
      settings.usageThresholdCritical,
      settings.valueColor
    )
  }
}

function frequencyValue(raw: number | null | undefined): ValueRender {
  if (typeof raw !== 'number' || Number.isNaN(raw) || raw < 0) return dash()
  return { text: `${raw.toFixed(0)} MHz`, color: settings.valueColor }
}

function powerValue(raw: number | null | undefined): ValueRender {
  if (typeof raw !== 'number' || Number.isNaN(raw) || raw < 0) return dash()
  return { text: `${raw.toFixed(1)} W`, color: settings.valueColor }
}

function fanValue(raw: number | null | undefined): ValueRender {
  if (typeof raw !== 'number' || Number.isNaN(raw) || raw < 0) return dash()
  return { text: `${raw.toFixed(0)} RPM`, color: settings.valueColor }
}

/** WPF GetMemoryDisplayText: GB when enabled, otherwise percent. */
function memoryValue(
  usage: number | null | undefined,
  used: number | null | undefined,
  total: number | null | undefined
): ValueRender {
  const severity = severityColor(
    usage ?? -1,
    settings.usageThresholdWarning,
    settings.usageThresholdCritical,
    settings.valueColor
  )
  if (displayMemoryInGigabytes) {
    if (typeof used === 'number' && used >= 0 && typeof total === 'number' && total > 0) {
      return { text: `${used.toFixed(1)}/${total.toFixed(1)} GB`, color: severity }
    }
    if (typeof used === 'number' && used >= 0) {
      return { text: `${used.toFixed(1)} GB`, color: severity }
    }
    return dash()
  }
  return usageValue(usage)
}

function fpsValue(): ValueRender {
  const fps = lastFps?.fps
  if (typeof fps !== 'number' || !Number.isFinite(fps) || fps <= 0) return dash()
  const text = `${Math.round(fps)}`
  const color = fps < settings.fpsThresholdCritical ? settings.criticalColor : settings.valueColor
  return { text, color }
}

function lowFpsValue(): ValueRender {
  const low = lastFps?.lowFps
  if (typeof low !== 'number' || !Number.isFinite(low) || low <= 0) return dash()
  const fps = lastFps?.fps
  const delta = typeof fps === 'number' && fps >= 0 ? fps - low : 0
  const color =
    delta >= settings.lowFpsDeltaThreshold ? settings.criticalColor : settings.valueColor
  return { text: `${Math.round(low)}`, color }
}

function frameTimeValue(): ValueRender {
  const ft = lastFps?.frameTimeMs
  if (typeof ft !== 'number' || !Number.isFinite(ft) || ft <= 0.1) return dash()
  const text = `${ft.toFixed(1)}ms`
  const color = ft > 10 ? settings.criticalColor : settings.valueColor
  return { text, color }
}

function renderItem(item: OsdItemName): ValueRender {
  const cpu = lastSnapshot?.cpu
  const gpu = lastSnapshot?.gpu
  const memory = lastSnapshot?.memory
  const motherboard = lastSnapshot?.motherboard
  const storage = lastSnapshot?.storage
  switch (item) {
    case 'Fps':
      return fpsValue()
    case 'LowFps':
      return lowFpsValue()
    case 'FrameTime':
      return frameTimeValue()
    case 'CpuFrequency': {
      const raw = showCpuAverageFrequency ? cpu?.coreClockAvg : cpu?.coreClockMax
      return frequencyValue(typeof raw === 'number' && raw >= 0 ? raw : (cpu?.coreClockAvg ?? cpu?.coreClockMax))
    }
    case 'CpuPCoreFrequency':
      return frequencyValue(cpu?.pCoreClock)
    case 'CpuECoreFrequency':
      return frequencyValue(cpu?.eCoreClock)
    case 'CpuUtilization':
      return usageValue(cpu?.usage)
    case 'CpuTemperature':
      return temperatureValue(cpu?.temperature)
    case 'CpuPower':
      return powerValue(cpu?.power)
    case 'CpuFan':
      return fanValue(cpu?.fanSpeed)
    case 'GpuFrequency':
      return frequencyValue(gpu?.coreClock)
    case 'GpuUtilization':
      return usageValue(gpu?.usage)
    case 'GpuTemperature':
      return temperatureValue(gpu?.temperature)
    case 'GpuVramUtilization':
      return memoryValue(gpu?.vramUtilization, gpu?.vramUsedMb, gpu?.vramTotalMb)
    case 'GpuVramTemperature':
      return temperatureValue(gpu?.vramTemperature)
    case 'GpuPower':
      return powerValue(gpu?.power)
    case 'GpuFan':
      return fanValue(gpu?.fanSpeed)
    case 'MemoryUtilization':
      return memoryValue(memory?.usage, memory?.usedMb, memory?.totalMb)
    case 'MemoryTemperature':
      return temperatureValue(memory?.highestTemperature)
    case 'Disk1Temperature':
      return temperatureValue(storage?.temperatures?.[0])
    case 'Disk2Temperature':
      return temperatureValue(storage?.temperatures?.[1])
    case 'PchTemperature':
      return temperatureValue(motherboard?.highestTemperature)
    case 'PchFan':
      return dash()
  }
}

function hexToRgb(hex: string): { r: number; g: number; b: number } {
  const value = hex.replace(/^#/, '')
  const full = value.length === 3
    ? value.split('').map((c) => c + c).join('')
    : value
  const parsed = parseInt(full, 16)
  if (Number.isNaN(parsed)) return { r: 0x1e, g: 0x1e, b: 0x1e }
  return { r: (parsed >> 16) & 0xff, g: (parsed >> 8) & 0xff, b: parsed & 0xff }
}

/** WPF ApplyAppearanceSettings: background color + opacity alpha. */
function backgroundRgba(opacityFactor = 1): string {
  const { r, g, b } = hexToRgb(settings.backgroundColor)
  const alpha = Math.min(1, Math.max(0, settings.backgroundOpacity * opacityFactor))
  return `rgba(${r},${g},${b},${alpha.toFixed(3)})`
}

function cornerRadius(): string {
  return `${settings.cornerRadiusTop}px ${settings.cornerRadiusTop}px ${settings.cornerRadiusBottom}px ${settings.cornerRadiusBottom}px`
}

function panelGroupHtml(group: OsdGroupDef): string {
  const rows = group.items.filter(isItemVisible).map((item) => {
    const value = renderItem(item)
    return (
      '<div class="osd-row">' +
      `<span class="osd-label">${ITEM_LABELS[item]}</span>` +
      `<span class="osd-value" style="color:${value.color}">${value.text}</span>` +
      '</div>'
    )
  })
  return (
    '<div class="osd-panel-group">' +
    `<div class="osd-panel-header" style="color:${settings.categoryColor}">—${group.label} —</div>` +
    rows.join('') +
    '</div>'
  )
}

function barGroupHtml(group: OsdGroupDef): string {
  const cells = group.items.filter(isItemVisible).map((item) => {
    const value = renderItem(item)
    return `<span class="osd-bar-value" style="color:${value.color}">${value.text}</span>`
  })
  return (
    `<span class="osd-bar-label" style="color:${settings.categoryColor}">${group.label}</span>` +
    cells.join('')
  )
}

/** Values body —refreshed in place without reloading the document. */
function buildValuesHtml(): string {
  const groups = isBarStyle() ? BAR_GROUPS : PANEL_GROUPS
  const visibleGroups = groups.filter(groupVisible)

  if (isBarStyle()) {
    const parts: string[] = []
    visibleGroups.forEach((group, index) => {
      if (index > 0) {
        parts.push(`<span class="osd-bar-separator" style="background:${settings.separatorColor}"></span>`)
      }
      parts.push(barGroupHtml(group))
    })
    return `<div class="osd-root osd-root--bar">${parts.join('')}</div>`
  }

  const parts: string[] = []
  visibleGroups.forEach((group, index) => {
    if (index > 0) {
      const firstGap = index === 1
      parts.push(
        firstGap
          ? '<div class="osd-panel-separator osd-panel-separator--clear"></div>'
          : `<div class="osd-panel-separator" style="background:${settings.separatorColor}"></div>`
      )
    }
    parts.push(panelGroupHtml(group))
  })
  return `<div class="osd-root osd-root--panel">${parts.join('')}</div>`
}

function buildOsdUrl(): string {
  const isBar = isBarStyle()
  const fontSize = settings.fontSize
  const dragRegion = settings.isLocked ? '' : '-webkit-app-region: drag;'
  const css = [
    'html,body{margin:0;padding:0;background:transparent;overflow:hidden;',
    'font-family:"Segoe UI",system-ui,sans-serif;user-select:none;cursor:default;}',
    `body{${dragRegion}}`,
    `.osd-root--bar{display:flex;align-items:center;white-space:nowrap;`,
    `background:${backgroundRgba(0.8)};border-radius:${cornerRadius()};`,
    `padding:4px 10px;font-size:${fontSize}px;}`,
    `.osd-bar-label{font-weight:500;margin-right:8px;min-width:25px;text-align:center;}`,
    `.osd-bar-value{display:inline-block;min-width:${isBar ? 34 : 30}px;margin-right:8px;text-align:center;}`,
    `.osd-bar-separator{width:1px;height:12px;margin:0 10px;}`,
    `.osd-root--panel{min-width:220px;background:${backgroundRgba()};`,
    `border-radius:${cornerRadius()};padding:15px;font-size:${fontSize}px;}`,
    `.osd-panel-header{margin-bottom:5px;font-size:${Math.max(8, fontSize - 1)}px;font-weight:500;}`,
    `.osd-row{display:flex;align-items:center;justify-content:space-between;margin:3px 0 1px;}`,
    `.osd-label{color:${settings.labelColor};font-size:${Math.max(8, fontSize - 1)}px;margin-right:16px;}`,
    `.osd-value{color:${settings.valueColor};font-size:${fontSize + 1}px;text-align:right;}`,
    `.osd-panel-separator{height:1px;margin:8px 0;}`,
    `.osd-panel-separator--clear{height:8px;margin:0;}`
  ].join('')

  const html = [
    '<!DOCTYPE html>',
    '<html>',
    '<head>',
    '<meta charset="utf-8">',
    '<style>',
    css,
    '</style>',
    '</head>',
    `<body><div id="udt-root"></div></body>`,
    '</html>'
  ].join('')
  return `data:text/html;charset=utf-8,${encodeURIComponent(html)}`
}

async function fitToContent(): Promise<void> {
  const win = osdWindow
  if (!win || win.isDestroyed()) return
  try {
    const size = (await win.webContents.executeJavaScript(
      '[document.body.scrollWidth, document.body.scrollHeight]'
    )) as [number, number]
    if (win.isDestroyed()) return
    win.setSize(Math.max(1, Math.round(size[0])), Math.max(1, Math.round(size[1])))
  } catch {
    // Page not ready yet.
  }
}

function updateValues(): void {
  const win = osdWindow
  if (!win || win.isDestroyed() || !visible) return
  const html = buildValuesHtml()
  void win.webContents
    .executeJavaScript(`document.getElementById('udt-root').innerHTML = ${JSON.stringify(html)}`)
    .catch(() => undefined)
  void fitToContent()
}

async function applyAppearance(): Promise<void> {
  const win = osdWindow
  if (!win || win.isDestroyed()) return
  try {
    await win.loadURL(buildOsdUrl())
  } catch (error) {
    console.error('[osd] failed to load OSD page:', error)
  }
  if (win.isDestroyed()) return
  pageLoaded = true
  await fitToContent()
  win.setIgnoreMouseEvents(settings.isLocked)
  if (visible) updateValues()
}

/** Fields that require a full page rebuild when they change. */
function appearanceSignature(store: OsdSettingsStore): string {
  return [
    store.selectedStyleIndex,
    store.backgroundOpacity,
    store.backgroundColor,
    store.fontSize,
    store.cornerRadiusTop,
    store.cornerRadiusBottom,
    store.isLocked,
    store.categoryColor,
    store.labelColor,
    store.valueColor,
    store.warningColor,
    store.criticalColor,
    store.separatorColor
  ].join('|')
}

/** WPF ApplyAppearanceSettings + RecalculatePosition on settings change. */
function onSettingsChanged(data: unknown): void {
  const changed = (data as { scope?: string; reason?: string } | null)?.scope
  if (changed === 'osd') {
    void readSettings().then(() => {
      const win = osdWindow
      if (!win || win.isDestroyed()) return

      const signature = appearanceSignature(settings)
      if (signature !== lastAppearanceSignature) {
        lastAppearanceSignature = signature
        void applyAppearance()
      } else if (visible) {
        updateValues()
      }

      const resetRequested =
        (isBarStyle() && settings.barPositionX === null && settings.barPositionY === null) ||
        (!isBarStyle() && settings.panelPositionX === null && settings.panelPositionY === null)
      if (resetRequested) setDefaultWindowPosition()

      if (settings.showOsd && !visible) {
        showOsd()
      } else if (!settings.showOsd && visible) {
        hideOsd()
      }
    })
  } else if (changed === 'hardwareSensors' || changed === 'application') {
    void readSiblingSettings().then(() => {
      if (visible) updateValues()
    })
  }
}

// ── visibility ──────────────────────────────────────────────────────────────

function showOsd(): void {
  const win = osdWindow
  if (!win || win.isDestroyed()) return

  const apply = (): void => {
    if (win.isDestroyed()) return
    if (win.isVisible()) return
    win.show()
    visible = true
    settings.showOsd = true
    void writeSettings()
    startRefresh()
  }

  if (!pageLoaded) {
    void applyAppearance().then(apply)
  } else {
    apply()
  }
}

function hideOsd(): void {
  const win = osdWindow
  if (!win || win.isDestroyed()) return
  if (win.isVisible()) {
    win.hide()
  }
  visible = false
  settings.showOsd = false
  void writeSettings()
  stopRefresh()
  if (fpsSubscribed) {
    fpsSubscribed = false
    unsubscribeFps?.()
    unsubscribeFps = null
    void hostClient.invoke('sensors.unsubscribeFps', {}).catch(() => undefined)
  }
}

function handleOsdChanged(data: unknown): void {
  const state = (data as OsdEventData | null)?.state
  if (state === 'Hidden') {
    hideOsd()
  } else if (state === 'Toggle') {
    if (osdWindow?.isVisible()) {
      hideOsd()
    } else {
      showOsd()
    }
  } else if (state === 'Show') {
    showOsd()
  }
}

// ── public API ──────────────────────────────────────────────────────────────

export function initOsdWindow(): void {
  if (osdWindow && !osdWindow.isDestroyed()) return

  osdWindow = new BrowserWindow({
    width: OSD_WIDTH,
    height: OSD_HEIGHT,
    show: false,
    frame: false,
    transparent: true,
    backgroundColor: '#00000000',
    alwaysOnTop: true,
    skipTaskbar: true,
    resizable: false,
    focusable: false,
    hasShadow: false,
    webPreferences: {
      sandbox: true
    }
  })

  osdWindow.on('closed', () => {
    osdWindow = null
    pageLoaded = false
    visible = false
    stopRefresh()
  })

  osdWindow.on('moved', () => {
    if (visible) snapAndClampPosition()
  })

  if (!unsubscribe) {
    unsubscribe = hostClient.on('osd.changed', handleOsdChanged)
  }
  if (!unsubscribeSettings) {
    unsubscribeSettings = hostClient.on('settings.changed', onSettingsChanged)
  }
  if (!unsubscribeDisplay) {
    const listener = (): void => onDisplayMetricsChanged()
    screen.on('display-metrics-changed', listener)
    unsubscribeDisplay = () => screen.removeListener('display-metrics-changed', listener)
  }

  void readSettingsWithRetry().then(() => {
    void readSiblingSettings().then(async () => {
      if (!osdWindow || osdWindow.isDestroyed()) return
      lastAppearanceSignature = appearanceSignature(settings)
      await applyAppearance()
      if (settings.showOsd) {
        setWindowPosition()
        showOsd()
      }
    })
  })
}

export function destroyOsdWindow(): void {
  if (positionSaveTimer) {
    clearTimeout(positionSaveTimer)
    positionSaveTimer = null
  }
  unsubscribe?.()
  unsubscribe = null
  unsubscribeSettings?.()
  unsubscribeSettings = null
  unsubscribeFps?.()
  unsubscribeFps = null
  unsubscribeDisplay?.()
  unsubscribeDisplay = null
  stopRefresh()
  if (osdWindow && !osdWindow.isDestroyed()) {
    osdWindow.destroy()
  }
  osdWindow = null
  visible = false
  pageLoaded = false
}
