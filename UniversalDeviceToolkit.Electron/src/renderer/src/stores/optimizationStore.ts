import { create } from 'zustand'
import {
  optimizationApi,
  type NetworkAccelerationConfig,
  type NetworkAccelerationStatus,
  type OptimizationCategoryDefinition
} from '../api/optimization'

export interface OptimizationStoreState {
  categories: OptimizationCategoryDefinition[]
  networkStatus: NetworkAccelerationStatus | null
  loading: boolean
  error: string | null
}

export interface OptimizationStoreActions {
  load: () => Promise<void>
  apply: (keys: string[]) => Promise<boolean>
  revert: (keys: string[]) => Promise<boolean>
  applyRecommended: () => Promise<boolean>
  estimate: (keys: string[]) => Promise<number>
  runCleanup: (keys: string[]) => Promise<boolean>
  loadNetwork: () => Promise<void>
  saveNetworkConfig: (config: NetworkAccelerationConfig) => Promise<boolean>
  startNetwork: () => Promise<boolean>
  stopNetwork: () => Promise<boolean>
}

export type OptimizationStore = OptimizationStoreState & OptimizationStoreActions

export const useOptimizationStore = create<OptimizationStore>((set, get) => ({
  categories: [],
  networkStatus: null,
  loading: false,
  error: null,

  async load() {
    if (get().loading) return
    set({ loading: true, error: null })
    try {
      const { categories } = await optimizationApi.getCategories()
      set({ categories })
    } catch (error) {
      set({ error: (error as Error).message })
    } finally {
      set({ loading: false })
    }
  },

  async apply(keys) {
    if (keys.length === 0) return true
    try {
      const res = await optimizationApi.apply(keys)
      if (!res.applied) return false
      await get().load()
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async revert(keys) {
    if (keys.length === 0) return true
    try {
      const res = await optimizationApi.revert(keys)
      if (!res.reverted) return false
      await get().load()
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async applyRecommended() {
    try {
      const res = await optimizationApi.applyRecommended()
      if (!res.applied) return false
      await get().load()
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async estimate(keys) {
    if (keys.length === 0) return 0
    try {
      const res = await optimizationApi.estimateCleanup(keys)
      return res.bytes
    } catch (error) {
      set({ error: (error as Error).message })
      return 0
    }
  },

  async runCleanup(keys) {
    if (keys.length === 0) return true
    try {
      const res = await optimizationApi.runCleanup(keys)
      if (!res.done) return false
      await get().load()
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async loadNetwork() {
    try {
      const status = await optimizationApi.networkGetStatus()
      set({ networkStatus: status })
    } catch (error) {
      set({ error: (error as Error).message })
    }
  },

  async saveNetworkConfig(config) {
    try {
      const res = await optimizationApi.networkSaveConfig(config)
      if (!res.saved) return false
      await get().loadNetwork()
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async startNetwork() {
    try {
      const res = await optimizationApi.networkStart()
      if (!res.ok) return false
      await get().loadNetwork()
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async stopNetwork() {
    try {
      const res = await optimizationApi.networkStop()
      if (!res.ok) return false
      await get().loadNetwork()
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  }
}))
