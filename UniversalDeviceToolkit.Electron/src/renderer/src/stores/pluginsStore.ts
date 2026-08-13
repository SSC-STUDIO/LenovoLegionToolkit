import { create } from 'zustand'
import { pluginsApi } from '../api/plugins'
import {
  createPluginsStoreState,
  reduceInstallProgress
} from './pluginsStoreCore'
import type { PluginsStoreState } from './pluginsStoreCore'

export type PluginsStore = PluginsStoreState

export const usePluginsStore = create<PluginsStore>()((set, get) =>
  createPluginsStoreState(pluginsApi, set, get)
)

pluginsApi.onInstallProgress((progress) => {
  usePluginsStore.setState((state) => {
    return reduceInstallProgress(state, progress) ?? state
  })
})

pluginsApi.onInstalled(() => {
  void usePluginsStore.getState().load()
})

pluginsApi.onUninstalled(() => {
  void usePluginsStore.getState().load()
})
