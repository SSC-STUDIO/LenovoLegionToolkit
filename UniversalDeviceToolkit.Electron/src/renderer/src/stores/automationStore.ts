import { create } from 'zustand'
import { automationApi } from '../api/automation'
import type { AutomationPipeline, AutomationState } from '../api/automation'

export interface AutomationStore {
  state: AutomationState
  /** $type discriminators of steps supported on this machine (e.g. "powerMode"). */
  steps: string[]
  loaded: boolean
  loading: boolean
  error: string | null
  load: () => Promise<void>
  setEnabled: (enabled: boolean) => Promise<boolean>
  save: (pipelines: AutomationPipeline[], isEnabled?: boolean) => Promise<boolean>
  runNow: (pipelineId: string) => Promise<boolean>
}

const defaultState: AutomationState = { isEnabled: false, pipelines: [] }

export const useAutomationStore = create<AutomationStore>()((set, get) => ({
  state: defaultState,
  steps: [],
  loaded: false,
  loading: false,
  error: null,

  async load() {
    if (get().loading) return
    set({ loading: true, error: null })
    try {
      const [state, supported] = await Promise.all([
        automationApi.getState(),
        automationApi.getSupportedSteps(),
      ])
      set({ state, steps: supported.steps, loaded: true })
    } catch (error) {
      set({ error: (error as Error).message })
    } finally {
      set({ loading: false })
    }
  },

  async setEnabled(enabled) {
    try {
      const res = await automationApi.setEnabled(enabled)
      if (!res.ok) return false
      set({ state: { ...get().state, isEnabled: enabled } })
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async save(pipelines, isEnabled) {
    try {
      const res = await automationApi.savePipelines(pipelines, isEnabled)
      if (!res.saved) return false
      await get().load()
      window.bridge?.refreshTrayMenu?.()
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async runNow(pipelineId) {
    try {
      const res = await automationApi.runNow(pipelineId)
      return res.ok
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },
}))
