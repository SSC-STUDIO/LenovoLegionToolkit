export type PluginState = 'Installed' | 'NotInstalled'

export interface PluginCapabilities {
  settingsPage: boolean
  featurePage: boolean
  optimizationCategory: boolean
  webPage?: boolean
  executableEntryPoint: boolean
}

export interface PluginView {
  id: string
  name: string
  description: string
  details?: string
  usageGuide?: string
  author: string
  version: string
  icon: string
  iconBackground?: string
  tags: string[]
  isSystemPlugin: boolean
  dependencies: string[]
  changelog?: string
  releaseDate: string
  fileSize: number
  installedVersion?: string
  updateAvailable: boolean
  availableVersion?: string
  state: PluginState
  directory?: string | null
  webPage?: string | null
  capabilities: PluginCapabilities
}

export interface PluginUpdate {
  id: string
  availableVersion: string
}

export interface InstallProgress {
  pluginId: string
  progressPercentage: number
  statusText: string
  phase: 'downloading' | 'completed' | 'failed'
}

export interface PluginInstalledEvent {
  pluginId: string
}

export interface PluginOperationOutcome {
  ok: boolean
  degraded: boolean
  unloadPending: boolean
  recoveryId?: string | null
  recoveryPath?: string | null
  error?: string | null
}

export interface PluginScanOutcome {
  ok: boolean
  registeredCount: number
  degraded: boolean
  unloadPending: boolean
  failures: PluginOperationOutcome[]
}

export interface PluginsApi {
  list: (forceRefresh?: boolean) => Promise<{ plugins: PluginView[]; online: boolean }>
  checkUpdates: () => Promise<{ updates: PluginUpdate[] }>
  install: (pluginId: string) => Promise<PluginOperationOutcome>
  uninstall: (pluginId: string) => Promise<PluginOperationOutcome & { dependencyBlocked?: boolean }>
  importFile: (filePath: string) => Promise<PluginOperationOutcome>
  refresh: () => Promise<PluginScanOutcome>
  onInstallProgress: (callback: (data: InstallProgress) => void) => () => void
  onInstalled: (callback: (data: PluginInstalledEvent) => void) => () => void
  onUninstalled: (callback: (data: PluginInstalledEvent) => void) => () => void
}

export type PluginBridgeInvoke = <T>(method: string, params?: unknown) => Promise<T>
export type PluginBridgeOn = <T>(event: string, callback: (data: T) => void) => () => void

export function createPluginsApi(
  invoke: PluginBridgeInvoke,
  on: PluginBridgeOn
): PluginsApi {
  return {
    async list(forceRefresh) {
      return invoke<{ plugins: PluginView[]; online: boolean }>(
        'plugins.list',
        forceRefresh ? { forceRefresh } : {}
      )
    },

    async checkUpdates() {
      return invoke<{ updates: PluginUpdate[] }>('plugins.checkUpdates', {})
    },

    async install(pluginId) {
      return invoke<PluginOperationOutcome>('plugins.install', { pluginId })
    },

    async uninstall(pluginId) {
      return invoke<PluginOperationOutcome & { dependencyBlocked?: boolean }>(
        'plugins.uninstall',
        { pluginId }
      )
    },

    async importFile(filePath) {
      return invoke<PluginOperationOutcome>('plugins.import', { filePath })
    },

    async refresh() {
      return invoke<PluginScanOutcome>('plugins.refresh', {})
    },

    onInstallProgress(callback) {
      return on<InstallProgress>('plugins.installProgress', callback)
    },

    onInstalled(callback) {
      return on<PluginInstalledEvent>('plugins.installed', callback)
    },

    onUninstalled(callback) {
      return on<PluginInstalledEvent>('plugins.uninstalled', callback)
    }
  }
}
