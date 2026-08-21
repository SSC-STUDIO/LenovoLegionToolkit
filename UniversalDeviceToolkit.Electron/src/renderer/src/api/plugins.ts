import { invoke, on } from './bridge'
import { createPluginsApi } from './pluginsCore'

export type {
  InstallProgress,
  PluginCapabilities,
  PluginInstalledEvent,
  PluginOperationOutcome,
  PluginScanOutcome,
  PluginsApi,
  PluginState,
  PluginUpdate,
  PluginView
} from './pluginsCore'
export {
  createPluginsApi,
  normalizePluginListResult,
  normalizePluginOperationOutcome,
  normalizePluginScanOutcome,
  normalizePluginUninstallOutcome,
  normalizePluginView,
  resolvePluginWebPageValue
} from './pluginsCore'

export const pluginsApi = createPluginsApi(invoke, on)
