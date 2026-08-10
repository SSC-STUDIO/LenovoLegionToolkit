import { invoke } from './bridge'

/** The 24 feature keys exposed by the host's `feature` domain. */
export type FeatureKey =
  | 'alwaysOnUsb'
  | 'battery'
  | 'batteryNightCharge'
  | 'flipToStart'
  | 'fnLock'
  | 'gSync'
  | 'hdr'
  | 'hybridMode'
  | 'igpuMode'
  | 'itsMode'
  | 'instantBoot'
  | 'microphone'
  | 'overDrive'
  | 'panelLogo'
  | 'portsBacklight'
  | 'powerMode'
  | 'refreshRate'
  | 'resolution'
  | 'dpiScale'
  | 'speaker'
  | 'touchpadLock'
  | 'whiteKeyboard'
  | 'winKey'
  | 'oneLevelWhiteKeyboard'

export interface FeatureInfo {
  key: FeatureKey
  supported: boolean
  /** CLR state type name, e.g. "PowerModeState" (enum) or "Resolution" (struct). */
  stateType: string
}

export interface FeatureSupportedResult {
  supported: boolean
}

export interface FeatureStateResult {
  state: unknown
}

export interface FeatureStatesResult {
  states: unknown[]
}

export interface SetFeatureStateResult {
  ok: boolean
  partial?: boolean
}

export interface FeaturesApi {
  list(): Promise<FeatureInfo[]>
  getSupported(feature: FeatureKey): Promise<FeatureSupportedResult>
  getStates(feature: FeatureKey): Promise<FeatureStatesResult>
  getState(feature: FeatureKey): Promise<FeatureStateResult>
  setState(feature: FeatureKey, state: unknown): Promise<SetFeatureStateResult>
}

export const featuresApi: FeaturesApi = {
  async list() {
    const result = await invoke<{ features: FeatureInfo[] }>('feature.list', {})
    return result.features
  },

  async getSupported(feature) {
    return invoke<FeatureSupportedResult>('feature.getSupported', { feature })
  },

  async getStates(feature) {
    return invoke<FeatureStatesResult>('feature.getStates', { feature })
  },

  async getState(feature) {
    return invoke<FeatureStateResult>('feature.getState', { feature })
  },

  async setState(feature, state) {
    return invoke<SetFeatureStateResult>('feature.setState', { feature, state })
  },
}
