import { create } from 'zustand'
import { sensorsApi, type FpsData, type SensorSnapshot, type SensorsSettings, type SensorsStatus } from '../api/sensors'

export const TREND_POINTS = 60

export interface SensorTrendHistory {
  labels: string[]
  cpuTemperature: (number | null)[]
  cpuUsage: (number | null)[]
  cpuClock: (number | null)[]
  gpuTemperature: (number | null)[]
  gpuUsage: (number | null)[]
  gpuClock: (number | null)[]
  memoryUsage: (number | null)[]
}

function emptyTrend(): SensorTrendHistory {
  return {
    labels: [],
    cpuTemperature: [],
    cpuUsage: [],
    cpuClock: [],
    gpuTemperature: [],
    gpuUsage: [],
    gpuClock: [],
    memoryUsage: []
  }
}

function pushTrend(history: SensorTrendHistory, snapshot: SensorSnapshot): SensorTrendHistory {
  const labels = [...history.labels, new Date(snapshot.ts).toLocaleTimeString([], { hour12: false })]
  const cpuTemperature = [...history.cpuTemperature, snapshot.cpu?.temperature ?? null]
  const cpuUsage = [...history.cpuUsage, snapshot.cpu?.usage ?? null]
  const cpuClock = [...history.cpuClock, snapshot.cpu?.coreClockAvg ?? snapshot.cpu?.coreClockMax ?? null]
  const gpuTemperature = [...history.gpuTemperature, snapshot.gpu?.temperature ?? null]
  const gpuUsage = [...history.gpuUsage, snapshot.gpu?.usage ?? null]
  const gpuClock = [...history.gpuClock, snapshot.gpu?.coreClock ?? null]
  const memoryUsage = [...history.memoryUsage, snapshot.memory?.usage ?? null]

  if (labels.length > TREND_POINTS) {
    labels.shift()
    cpuTemperature.shift()
    cpuUsage.shift()
    cpuClock.shift()
    gpuTemperature.shift()
    gpuUsage.shift()
    gpuClock.shift()
    memoryUsage.shift()
  }
  return { labels, cpuTemperature, cpuUsage, cpuClock, gpuTemperature, gpuUsage, gpuClock, memoryUsage }
}

interface SensorsStoreState {
  status: SensorsStatus | null
  snapshot: SensorSnapshot | null
  fps: FpsData | null
  settings: SensorsSettings | null
  trend: SensorTrendHistory
  subscribed: boolean
  loading: boolean
  error: string | null
}

interface SensorsStoreActions {
  loadStatus: () => Promise<void>
  loadSnapshot: () => Promise<void>
  start: (intervalSec?: number) => Promise<void>
  stop: () => Promise<void>
  loadSettings: () => Promise<void>
  saveSettings: (partial: SensorsSettings) => Promise<void>
}

const offUpdated: (() => void)[] = []

export const useSensorsStore = create<SensorsStoreState & SensorsStoreActions>((set, get) => ({
  status: null,
  snapshot: null,
  fps: null,
  settings: null,
  trend: emptyTrend(),
  subscribed: false,
  loading: false,
  error: null,

  loadStatus: async () => {
    try {
      const status = await sensorsApi.getStatus()
      set({ status })
    } catch (error) {
      set({ error: (error as Error).message })
    }
  },

  loadSnapshot: async () => {
    try {
      const snapshot = await sensorsApi.getSnapshot()
      set({ snapshot, trend: pushTrend(get().trend, snapshot) })
    } catch (error) {
      set({ error: (error as Error).message })
    }
  },

  start: async (intervalSec = 1) => {
    if (get().subscribed) return
    try {
      await sensorsApi.subscribe(intervalSec)
      offUpdated.push(
        sensorsApi.onUpdated((snapshot) =>
          set((state) => ({ snapshot, trend: pushTrend(state.trend, snapshot) }))
        )
      )
      offUpdated.push(sensorsApi.onFpsUpdated((fps) => set({ fps })))
      set({ subscribed: true })
    } catch (error) {
      set({ error: (error as Error).message })
    }
  },

  stop: async () => {
    if (!get().subscribed) return
    try {
      await sensorsApi.unsubscribe()
    } catch (error) {
      set({ error: (error as Error).message })
    }
    while (offUpdated.length > 0) {
      offUpdated.pop()?.()
    }
    set({ subscribed: false })
  },

  loadSettings: async () => {
    try {
      const settings = await sensorsApi.getSettings()
      set({ settings })
    } catch (error) {
      set({ error: (error as Error).message })
    }
  },

  saveSettings: async (partial) => {
    try {
      await sensorsApi.setSettings(partial)
      const settings = await sensorsApi.getSettings()
      set({ settings })
    } catch (error) {
      set({ error: (error as Error).message })
    }
  }
}))
