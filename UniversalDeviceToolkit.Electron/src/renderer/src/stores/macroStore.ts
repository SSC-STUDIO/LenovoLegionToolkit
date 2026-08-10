import { create } from 'zustand'
import { macroApi } from '../api/macro'
import type { MacroEvent, MacroRecordingMode, MacroState, SaveMacroSequenceParams } from '../api/macro'

export interface MacroStore {
  state: MacroState
  loaded: boolean
  loading: boolean
  error: string | null
  load: () => Promise<void>
  setEnabled: (enabled: boolean) => Promise<boolean>
  play: (key: number) => Promise<boolean>
  startRecording: (mode: MacroRecordingMode, key: number) => Promise<boolean>
  stopRecording: () => Promise<MacroEvent[] | null>
  saveSequence: (params: SaveMacroSequenceParams) => Promise<boolean>
  clearSequence: (key: number) => Promise<boolean>
}

const defaultState: MacroState = { isEnabled: false, slots: [] }

export const useMacroStore = create<MacroStore>()((set, get) => ({
  state: defaultState,
  loaded: false,
  loading: false,
  error: null,

  async load() {
    if (get().loading) return
    set({ loading: true, error: null })
    try {
      const state = await macroApi.getState()
      set({ state, loaded: true })
    } catch (error) {
      set({ error: (error as Error).message })
    } finally {
      set({ loading: false })
    }
  },

  async setEnabled(enabled) {
    try {
      const res = await macroApi.setEnabled(enabled)
      if (!res.ok) return false
      set({ state: { ...get().state, isEnabled: enabled } })
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async play(key) {
    try {
      const res = await macroApi.play(key)
      return res.ok
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async startRecording(mode, key) {
    try {
      const res = await macroApi.startRecording(mode, key)
      return res.ok
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async stopRecording() {
    try {
      const res = await macroApi.stopRecording()
      return res.events
    } catch (error) {
      set({ error: (error as Error).message })
      return null
    }
  },

  async saveSequence(params) {
    try {
      const res = await macroApi.saveSequence(params)
      if (!res.ok) return false
      await get().load()
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async clearSequence(key) {
    try {
      const res = await macroApi.clearSequence(key)
      if (!res.ok) return false
      await get().load()
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },
}))
