import { create } from 'zustand'
import { sensorsApi, type FpsData, type SensorSnapshot, type SensorsSettings, type SensorsStatus } from '../api/sensors'

interface SensorsStoreState {
  status: SensorsStatus | null
  snapshot: SensorSnapshot | null
  fps: FpsData | null
  settings: SensorsSettings | null
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
      set({ snapshot })
    } catch (error) {
      set({ error: (error as Error).message })
    }
  },

  start: async (intervalSec = 1) => {
    if (get().subscribed) return
    try {
      await sensorsApi.subscribe(intervalSec)
      offUpdated.push(sensorsApi.onUpdated((snapshot) => set({ snapshot })))
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
