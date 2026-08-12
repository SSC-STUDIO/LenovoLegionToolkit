import { invoke, on } from './bridge'

export type PluginState = 'Installed' | 'NotInstalled'

export interface PluginCapabilities {
  settingsPage: boolean
  featurePage: boolean
  optimizationCategory: boolean
  executableEntryPoint: boolean
}

/** Plugin view as projected by the host's plugins.list handler. */
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
  /** Plugin package root directory (installed plugins only). */
  directory?: string | null
  /** Relative web UI entry (contributes.webPage), e.g. "web/index.html". */
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

export interface PluginsApi {
  list: (forceRefresh?: boolean) => Promise<{ plugins: PluginView[]; online: boolean }>
  checkUpdates: () => Promise<{ updates: PluginUpdate[] }>
  install: (pluginId: string) => Promise<{ ok: boolean }>
  uninstall: (pluginId: string) => Promise<{ ok: boolean; dependencyBlocked?: boolean }>
  importFile: (filePath: string) => Promise<{ ok: boolean }>
  refresh: () => Promise<{ registeredCount: number }>
  onInstallProgress: (cb: (data: InstallProgress) => void) => () => void
  onInstalled: (cb: (data: PluginInstalledEvent) => void) => () => void
  onUninstalled: (cb: (data: PluginInstalledEvent) => void) => () => void
}

export const pluginsApi: PluginsApi = {
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
    return invoke<{ ok: boolean }>('plugins.install', { pluginId })
  },

  async uninstall(pluginId) {
    return invoke<{ ok: boolean; dependencyBlocked?: boolean }>('plugins.uninstall', { pluginId })
  },

  async importFile(filePath) {
    return invoke<{ ok: boolean }>('plugins.import', { filePath })
  },

  async refresh() {
    return invoke<{ registeredCount: number }>('plugins.refresh', {})
  },

  onInstallProgress(cb) {
    return on<InstallProgress>('plugins.installProgress', cb)
  },

  onInstalled(cb) {
    return on<PluginInstalledEvent>('plugins.installed', cb)
  },

  onUninstalled(cb) {
    return on<PluginInstalledEvent>('plugins.uninstalled', cb)
  },
}
