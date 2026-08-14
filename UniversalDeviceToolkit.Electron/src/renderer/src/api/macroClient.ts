export type MacroSource = 'Keyboard' | 'Mouse'
export type MacroDirection = 'Unknown' | 'Down' | 'Up' | 'Wheel' | 'HorizontalWheel' | 'Move'
export type MacroRecordingMode = 'Keyboard' | 'KeyboardMouse' | 'KeyboardMouseMovement'

export interface MacroEvent {
  source: MacroSource
  direction: MacroDirection
  /** Virtual key code for keyboard events, button id (1/2/3/.../wheel delta) for mouse. */
  key: number
  x: number
  y: number
  /** Delay in milliseconds since the previous event. */
  delayMs: number
}

export interface MacroSlot {
  /** Virtual key code (numpad 0x60-0x69) this sequence is bound to. */
  key: number
  source: MacroSource
  repeatCount: number
  ignoreDelays: boolean
  interruptOnOtherKey: boolean
  events: MacroEvent[]
}

export interface MacroState {
  isEnabled: boolean
  slots: MacroSlot[]
}

export interface SaveMacroSequenceParams {
  key: number
  repeatCount: number
  ignoreDelays: boolean
  interruptOnOtherKey: boolean
  events: MacroEvent[]
}

export interface MacroApi {
  getState(): Promise<MacroState>
  setEnabled(enabled: boolean): Promise<{ ok: boolean }>
  play(key: number): Promise<{ ok: boolean }>
  startRecording(mode: MacroRecordingMode, key: number): Promise<{ ok: boolean }>
  stopRecording(): Promise<{ events: MacroEvent[] }>
  saveSequence(params: SaveMacroSequenceParams): Promise<{ ok: boolean }>
  clearSequence(key: number): Promise<{ ok: boolean }>
}

export type MacroInvoke = <T>(method: string, params: unknown) => Promise<T>

export function createMacroApi(invoke: MacroInvoke): MacroApi {
  return {
    async getState() {
      return invoke<MacroState>('macro.getState', {})
    },

    async setEnabled(enabled) {
      return invoke<{ ok: boolean }>('macro.setEnabled', { enabled })
    },

    async play(key) {
      return invoke<{ ok: boolean }>('macro.play', { key })
    },

    async startRecording(mode, key) {
      return invoke<{ ok: boolean }>('macro.startRecording', { mode, key })
    },

    async stopRecording() {
      return invoke<{ events: MacroEvent[] }>('macro.stopRecording', {})
    },

    async saveSequence(params) {
      return invoke<{ ok: boolean }>('macro.saveSequence', params)
    },

    async clearSequence(key) {
      return invoke<{ ok: boolean }>('macro.clearSequence', { key })
    }
  }
}
