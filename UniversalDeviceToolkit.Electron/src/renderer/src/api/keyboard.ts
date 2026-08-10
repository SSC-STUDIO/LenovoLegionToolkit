import { invoke } from './bridge'

export type KeyboardMode = 'rgb' | 'spectrum' | 'none'

export type RgbPreset = 'Off' | 'One' | 'Two' | 'Three' | 'Four'
export type RgbEffect = 'Static' | 'Breath' | 'Smooth' | 'WaveRTL' | 'WaveLTR'
export type RgbSpeed = 'Slowest' | 'Slow' | 'Fast' | 'Fastest'
export type RgbBrightness = 'Low' | 'High'

export interface RgbColor {
  R: number
  G: number
  B: number
}

export interface RgbPresetDescription {
  Effect: RgbEffect
  Speed: RgbSpeed
  Brightness: RgbBrightness
  Zone1: RgbColor
  Zone2: RgbColor
  Zone3: RgbColor
  Zone4: RgbColor
}

export interface RgbState {
  SelectedPreset: RgbPreset
  Presets: Partial<Record<RgbPreset, RgbPresetDescription>>
}

export type SpectrumLayoutName = 'KeyboardOnly' | 'KeyboardAndFront' | 'Full' | 'FullAlternative'
export type KeyboardLayoutName = 'Ansi' | 'Iso' | 'Jis' | 'Keyboard24Zone'

export type SpectrumEffectType =
  | 'Always'
  | 'RainbowScrew'
  | 'RainbowWave'
  | 'ColorChange'
  | 'ColorWave'
  | 'ColorPulse'
  | 'Smooth'
  | 'Rain'
  | 'Ripple'
  | 'Type'
  | 'AudioBounce'
  | 'AudioRipple'
  | 'AuroraSync'

export type SpectrumSpeed = 'None' | 'Speed1' | 'Speed2' | 'Speed3'
export type SpectrumDirection = 'None' | 'BottomToTop' | 'TopToBottom' | 'LeftToRight' | 'RightToLeft'
export type SpectrumClockwiseDirection = 'None' | 'Clockwise' | 'CounterClockwise'

export interface SpectrumEffect {
  Type: SpectrumEffectType
  Speed: SpectrumSpeed
  Direction: SpectrumDirection
  ClockwiseDirection: SpectrumClockwiseDirection
  Colors: RgbColor[]
  Keys: number[]
}

export interface SpectrumLayoutResult {
  spectrumLayout: SpectrumLayoutName
  keyboardLayout: KeyboardLayoutName
  keys: number[]
}

export interface RgbStateResult {
  state: RgbState
}

export interface SpectrumProfileDescriptionResult {
  profile: number
  effects: SpectrumEffect[]
}

export interface KeyboardApi {
  detect(): Promise<{ mode: KeyboardMode }>
  getRgbState(): Promise<RgbStateResult>
  setRgbState(state: RgbState): Promise<{ ok: boolean }>
  setPreset(preset: RgbPreset): Promise<RgbStateResult>
  nextPreset(): Promise<RgbStateResult>
  takeOwnership(enable: boolean, restorePreset?: boolean): Promise<{ ok: boolean }>
  spectrumGetLayout(): Promise<SpectrumLayoutResult>
  spectrumGetBrightness(): Promise<{ brightness: number }>
  spectrumSetBrightness(brightness: number): Promise<{ ok: boolean }>
  spectrumGetLogo(): Promise<{ isOn: boolean }>
  spectrumSetLogo(isOn: boolean): Promise<{ ok: boolean }>
  spectrumGetProfile(): Promise<{ profile: number }>
  spectrumSetProfile(profile: number): Promise<{ ok: boolean }>
  spectrumGetProfileDesc(profile: number): Promise<SpectrumProfileDescriptionResult>
  spectrumSetProfileDesc(profile: number, effects: SpectrumEffect[]): Promise<{ ok: boolean }>
}

export const keyboardApi: KeyboardApi = {
  async detect() {
    return invoke<{ mode: KeyboardMode }>('keyboard.detect', {})
  },

  async getRgbState() {
    return invoke<RgbStateResult>('rgb.getState', {})
  },

  async setRgbState(state) {
    return invoke<{ ok: boolean }>('rgb.setState', { state })
  },

  async setPreset(preset) {
    return invoke<RgbStateResult>('rgb.setPreset', { preset })
  },

  async nextPreset() {
    return invoke<RgbStateResult>('rgb.nextPreset', {})
  },

  async takeOwnership(enable, restorePreset) {
    return invoke<{ ok: boolean }>(
      'rgb.takeOwnership',
      restorePreset === undefined ? { enable } : { enable, restorePreset }
    )
  },

  async spectrumGetLayout() {
    return invoke<SpectrumLayoutResult>('spectrum.getLayout', {})
  },

  async spectrumGetBrightness() {
    return invoke<{ brightness: number }>('spectrum.getBrightness', {})
  },

  async spectrumSetBrightness(brightness) {
    return invoke<{ ok: boolean }>('spectrum.setBrightness', { brightness })
  },

  async spectrumGetLogo() {
    return invoke<{ isOn: boolean }>('spectrum.getLogoStatus', {})
  },

  async spectrumSetLogo(isOn) {
    return invoke<{ ok: boolean }>('spectrum.setLogoStatus', { isOn })
  },

  async spectrumGetProfile() {
    return invoke<{ profile: number }>('spectrum.getProfile', {})
  },

  async spectrumSetProfile(profile) {
    return invoke<{ ok: boolean }>('spectrum.setProfile', { profile })
  },

  async spectrumGetProfileDesc(profile) {
    return invoke<SpectrumProfileDescriptionResult>('spectrum.getProfileDescription', { profile })
  },

  async spectrumSetProfileDesc(profile, effects) {
    return invoke<{ ok: boolean }>('spectrum.setProfileDescription', { profile, effects })
  }
}
