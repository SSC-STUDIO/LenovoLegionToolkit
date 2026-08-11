import { invoke, on } from './bridge'

export interface SensorsInfo {
  cpuName?: string | null
  gpuName?: string | null
  gpuIsIntegrated?: boolean
}

export interface SensorsCpu {
  temperature?: number | null
  usage?: number | null
  fanSpeed?: number | null
  power?: number | null
  powerCores?: number | null
  powerMemory?: number | null
  powerPlatform?: number | null
  voltage?: number | null
  coreClockMax?: number | null
  coreClockAvg?: number | null
  pCoreClock?: number | null
  eCoreClock?: number | null
}

export interface SensorsGpu {
  usage?: number | null
  temperature?: number | null
  coreClock?: number | null
  memoryClock?: number | null
  power?: number | null
  voltage?: number | null
  vramTemperature?: number | null
  hotSpotTemperature?: number | null
  vramUtilization?: number | null
  vramUsedMb?: number | null
  vramTotalMb?: number | null
  pcieRxThroughput?: number | null
  pcieTxThroughput?: number | null
  fanSpeed?: number | null
}

export interface SensorsMemory {
  usage?: number | null
  usedMb?: number | null
  totalMb?: number | null
  highestTemperature?: number | null
}

export interface SensorsBattery {
  chargeLevel?: number | null
  health?: number | null
  temperature?: number | null
  chargeRate?: number | null
  voltage?: number | null
  designCapacity?: number | null
  fullChargeCapacity?: number | null
  isCharging?: boolean
  isLowBattery?: boolean
  isLowPowerAdapter?: boolean
  modelName?: string | null
}

export interface SensorSnapshot {
  ts: string
  source: 'LibreHardwareMonitor' | 'vendor' | 'mixed'
  initialized: boolean
  isHybrid?: boolean
  info?: SensorsInfo
  cpu?: SensorsCpu
  gpu?: SensorsGpu
  memory?: SensorsMemory
  battery?: SensorsBattery
  motherboard?: { highestTemperature?: number | null }
  storage?: { temperatures?: (number | null)[] }
}

export interface SensorsStatus {
  initialized: boolean
  isHybrid?: boolean
  cpuName?: string | null
  gpuName?: string | null
  gpuIsIntegrated?: boolean
  initialState?: string
}

export interface FpsData {
  process?: string | null
  fps?: number | null
  lowFps?: number | null
  frameTimeMs?: number | null
}

export interface SensorsSettings {
  enableHardwareSensors?: boolean
  osdRefreshIntervalSec?: number
  selectedGpuIsIgpu?: boolean
  showCpuAverageFrequency?: boolean
  displayMemoryInGigabytes?: boolean
  visibleSections?: string[]
  sectionOrder?: string[]
}

export const sensorsApi = {
  getStatus: (): Promise<SensorsStatus> => invoke<SensorsStatus>('sensors.getStatus'),
  getSnapshot: (): Promise<SensorSnapshot> => invoke<SensorSnapshot>('sensors.getSnapshot'),
  getDetailed: (): Promise<SensorSnapshot> => invoke<SensorSnapshot>('sensors.getDetailed'),
  subscribe: (intervalSec = 1): Promise<{ subscribed: boolean; effectiveIntervalSec: number }> =>
    invoke('sensors.subscribe', { intervalSec }),
  unsubscribe: (): Promise<{ unsubscribed: boolean }> => invoke('sensors.unsubscribe'),
  getFps: (): Promise<FpsData> => invoke<FpsData>('sensors.getFps'),
  subscribeFps: (blacklist?: string[]): Promise<{ monitoring: boolean }> =>
    invoke('sensors.subscribeFps', blacklist ? { blacklist } : {}),
  unsubscribeFps: (): Promise<{ monitoring: boolean }> => invoke('sensors.unsubscribeFps'),
  getSettings: (): Promise<SensorsSettings> => invoke<SensorsSettings>('sensors.getSettings'),
  setSettings: (partial: SensorsSettings): Promise<{ saved: boolean }> =>
    invoke('sensors.setSettings', partial),
  onUpdated: (callback: (snapshot: SensorSnapshot) => void): (() => void) =>
    on<SensorSnapshot>('sensors.updated', callback),
  onFpsUpdated: (callback: (data: FpsData) => void): (() => void) =>
    on<FpsData>('sensors.fpsUpdated', callback)
}
