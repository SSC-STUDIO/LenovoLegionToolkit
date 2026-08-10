import { invoke } from './bridge'

export type NetworkAccelerationMode = 'Off' | 'SystemProxy' | 'Hosts' | 'DiagnosticsOnly'

export interface OptimizationActionDefinition {
  key: string
  title: string
  description: string
  recommended: boolean
  /** applied state: true = applied, false = not applied, null = unknown */
  applied: boolean | null
}

export interface OptimizationCategoryDefinition {
  key: string
  title: string
  description: string
  pluginId: string | null
  hasSettings: boolean
  actions: OptimizationActionDefinition[]
}

export interface NetworkDomainSubItem {
  id: string
  displayName: string
  domain: string
  enabled: boolean
  isBeta: boolean
}

export interface NetworkDomainGroup {
  id: string
  displayName: string
  enabled: boolean
  isFavorite: boolean
  domains: string[]
  subItems: NetworkDomainSubItem[]
  iconKey: string | null
  description: string | null
}

export interface NetworkRecoverySnapshotMetadata {
  capturedAtUtc: string | null
  snapshotPath: string | null
  hadSystemProxy: boolean
  hadHostsBlock: boolean
  hadPacPath: boolean
  notes: string | null
}

export interface NetworkAccelerationConfig {
  accelerationEnabled: boolean
  mode: NetworkAccelerationMode
  listenPort: number
  domainGroups: NetworkDomainGroup[]
  dnsServer: string | null
  dohUrl: string | null
  certificateFingerprintSha256: string | null
  lastRecoverySnapshot: NetworkRecoverySnapshotMetadata | null
  showInNavigation: boolean
}

export interface NetworkAccelerationStatus {
  config: NetworkAccelerationConfig
  isBackendReady: boolean
  isRunning: boolean
  statusText: string
}

export interface OptimizationApi {
  getCategories(): Promise<{ categories: OptimizationCategoryDefinition[] }>
  apply(actionKeys: string[]): Promise<{ applied: boolean }>
  revert(actionKeys: string[]): Promise<{ reverted: boolean }>
  applyRecommended(): Promise<{ applied: boolean }>
  getActionStatus(actionKey: string): Promise<{ applied: boolean | null }>
  estimateCleanup(actionKeys: string[]): Promise<{ bytes: number }>
  runCleanup(actionKeys: string[]): Promise<{ done: boolean }>
  networkGetStatus(): Promise<NetworkAccelerationStatus>
  networkSaveConfig(config: NetworkAccelerationConfig): Promise<{ saved: boolean }>
  networkStart(): Promise<{ ok: boolean }>
  networkStop(): Promise<{ ok: boolean }>
}

export const optimizationApi: OptimizationApi = {
  async getCategories() {
    return invoke<{ categories: OptimizationCategoryDefinition[] }>('optimization.getCategories', {})
  },

  async apply(actionKeys) {
    return invoke<{ applied: boolean }>('optimization.apply', { actionKeys })
  },

  async revert(actionKeys) {
    return invoke<{ reverted: boolean }>('optimization.revert', { actionKeys })
  },

  async applyRecommended() {
    return invoke<{ applied: boolean }>('optimization.applyRecommended', {})
  },

  async getActionStatus(actionKey) {
    return invoke<{ applied: boolean | null }>('optimization.getActionStatus', { actionKey })
  },

  async estimateCleanup(actionKeys) {
    return invoke<{ bytes: number }>('cleanup.estimate', { actionKeys })
  },

  async runCleanup(actionKeys) {
    return invoke<{ done: boolean }>('cleanup.run', { actionKeys })
  },

  async networkGetStatus() {
    return invoke<NetworkAccelerationStatus>('network.getStatus', {})
  },

  async networkSaveConfig(config) {
    return invoke<{ saved: boolean }>('network.saveConfig', { config })
  },

  async networkStart() {
    return invoke<{ ok: boolean }>('network.start', {})
  },

  async networkStop() {
    return invoke<{ ok: boolean }>('network.stop', {})
  }
}
