import { create } from 'zustand'
import { keyboardApi } from '../api/keyboard'
import type {
  KeyboardMode,
  RgbPreset,
  RgbState,
  SpectrumEffect,
  SpectrumLayoutResult
} from '../api/keyboard'

export interface KeyboardSpectrumState {
  layout: SpectrumLayoutResult | null
  brightness: number
  logo: boolean
  profile: number
  effects: SpectrumEffect[]
}

export interface KeyboardStore {
  mode: KeyboardMode | null
  rgbState: RgbState | null
  spectrum: KeyboardSpectrumState
  loading: boolean
  error: string | null
  load: () => Promise<void>
  setRgb: (state: RgbState) => Promise<boolean>
  setPreset: (preset: RgbPreset) => Promise<boolean>
  nextPreset: () => Promise<boolean>
  setBrightness: (value: number) => Promise<boolean>
  setLogo: (value: boolean) => Promise<boolean>
  setProfile: (profile: number) => Promise<boolean>
  loadProfileDesc: (profile: number) => Promise<boolean>
  saveProfileDesc: (profile: number, effects: SpectrumEffect[]) => Promise<boolean>
}

const EMPTY_SPECTRUM: KeyboardSpectrumState = {
  layout: null,
  brightness: 0,
  logo: false,
  profile: 1,
  effects: []
}

export const useKeyboardStore = create<KeyboardStore>()((set, get) => ({
  mode: null,
  rgbState: null,
  spectrum: EMPTY_SPECTRUM,
  loading: false,
  error: null,

  async load() {
    if (get().loading) return
    set({ loading: true, error: null })
    try {
      const { mode } = await keyboardApi.detect()

      if (mode === 'rgb') {
        const { state } = await keyboardApi.getRgbState()
        set({ mode, rgbState: state })
      } else if (mode === 'spectrum') {
        const [layout, brightness, logo, profile] = await Promise.all([
          keyboardApi.spectrumGetLayout(),
          keyboardApi.spectrumGetBrightness(),
          keyboardApi.spectrumGetLogo(),
          keyboardApi.spectrumGetProfile()
        ])

        let effects: SpectrumEffect[] = []
        try {
          const desc = await keyboardApi.spectrumGetProfileDesc(profile.profile)
          effects = desc.effects
        } catch {
          // Profile description read is best-effort; the list stays empty.
        }

        set({
          mode,
          spectrum: {
            layout,
            brightness: brightness.brightness,
            logo: logo.isOn,
            profile: profile.profile,
            effects
          }
        })
      } else {
        set({ mode: 'none' })
      }
    } catch (error) {
      set({ error: (error as Error).message })
    } finally {
      set({ loading: false })
    }
  },

  async setRgb(state) {
    try {
      await keyboardApi.setRgbState(state)
      set({ rgbState: state })
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async setPreset(preset) {
    try {
      const { state } = await keyboardApi.setPreset(preset)
      set({ rgbState: state })
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async nextPreset() {
    try {
      const { state } = await keyboardApi.nextPreset()
      set({ rgbState: state })
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async setBrightness(value) {
    try {
      await keyboardApi.spectrumSetBrightness(value)
      set({ spectrum: { ...get().spectrum, brightness: value } })
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async setLogo(value) {
    try {
      await keyboardApi.spectrumSetLogo(value)
      set({ spectrum: { ...get().spectrum, logo: value } })
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async setProfile(profile) {
    try {
      await keyboardApi.spectrumSetProfile(profile)
      set({ spectrum: { ...get().spectrum, profile } })
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async loadProfileDesc(profile) {
    try {
      const desc = await keyboardApi.spectrumGetProfileDesc(profile)
      set({ spectrum: { ...get().spectrum, profile: desc.profile, effects: desc.effects } })
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },

  async saveProfileDesc(profile, effects) {
    try {
      await keyboardApi.spectrumSetProfileDesc(profile, effects)
      set({ spectrum: { ...get().spectrum, profile, effects } })
      return true
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  }
}))
