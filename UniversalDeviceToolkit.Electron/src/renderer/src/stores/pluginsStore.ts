import { create } from 'zustand'
import { pluginsApi } from '../api/plugins'
import type { PluginView } from '../api/plugins'

export interface PluginsStore {
  plugins: PluginView[]
  /** pluginId -> availableVersion from the latest update check. */
  updates: Record<string, string>
  /** pluginId -> progress percentage while a download is in flight. */
  installingIds: Record<string, number>
  loading: boolean
  /** True when the online store was unreachable and the list degraded to installed-only. */
  offline: boolean
  error: string | null
  load: (force?: boolean) => Promise<void>
  install: (pluginId: string) => Promise<boolean>
  uninstall: (pluginId: string) => Promise<{ ok: boolean; dependencyBlocked: boolean }>
  refresh: () => Promise<void>
  importFile: (path: string) => Promise<boolean>
}

export const usePluginsStore = create<PluginsStore>()((set, get) => ({
  plugins: [],
  updates: {},
  installingIds: {},
  loading: false,
  offline: false,
  error: null,

  async load(force = false) {
    if (get().loading) return
    set({ loading: true, error: null })
    try {
      const [listResult, updatesResult] = await Promise.all([
        pluginsApi.list(force),
        pluginsApi.checkUpdates(),
      ])
      const updates: Record<string, string> = {}
      for (const update of updatesResult.updates) updates[update.id] = update.availableVersion
      set({
        plugins: listResult.plugins,
        updates,
        offline: !listResult.online,
        loading: false,
      })
    } catch (error) {
      set({ error: (error as Error).message, loading: false })
    }
  },

  async install(pluginId) {
    try {
      set({ installingIds: { ...get().installingIds, [pluginId]: 0 } })
      const result = await pluginsApi.install(pluginId)
      if (result.ok) await get().load()
      return result.ok
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    } finally {
      const next = { ...get().installingIds }
      delete next[pluginId]
      set({ installingIds: next })
    }
  },

  async uninstall(pluginId) {
    try {
      const result = await pluginsApi.uninstall(pluginId)
      if (result.ok) await get().load()
      return { ok: result.ok, dependencyBlocked: result.dependencyBlocked ?? false }
    } catch (error) {
      set({ error: (error as Error).message })
      return { ok: false, dependencyBlocked: false }
    }
  },

  async refresh() {
    try {
      await pluginsApi.refresh()
      await get().load(true)
    } catch (error) {
      set({ error: (error as Error).message })
    }
  },

  async importFile(path) {
    try {
      const result = await pluginsApi.importFile(path)
      if (result.ok) await get().load()
      return result.ok
    } catch (error) {
      set({ error: (error as Error).message })
      return false
    }
  },
}))

pluginsApi.onInstallProgress((progress) => {
  usePluginsStore.setState((state) => {
    if (!(progress.pluginId in state.installingIds)) return state
    if (progress.phase === 'completed' || progress.phase === 'failed') {
      const rest = { ...state.installingIds }
      delete rest[progress.pluginId]
      return { installingIds: rest }
    }
    return { installingIds: { ...state.installingIds, [progress.pluginId]: progress.progressPercentage } }
  })
})

pluginsApi.onInstalled(() => {
  void usePluginsStore.getState().load()
})

pluginsApi.onUninstalled(() => {
  void usePluginsStore.getState().load()
})
