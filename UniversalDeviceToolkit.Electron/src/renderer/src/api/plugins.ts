import { invoke, on } from './bridge'
import { createPluginsApi } from './pluginsCore'

export type {
  InstallProgress,
  PluginCapabilities,
  PluginInstalledEvent,
  PluginsApi,
  PluginState,
  PluginUpdate,
  PluginView
} from './pluginsCore'
export { createPluginsApi } from './pluginsCore'

export const pluginsApi = createPluginsApi(invoke, on)
