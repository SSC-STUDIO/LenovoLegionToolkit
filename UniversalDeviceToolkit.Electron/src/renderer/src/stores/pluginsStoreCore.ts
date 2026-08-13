import type {
  InstallProgress,
  PluginOperationOutcome,
  PluginScanOutcome,
  PluginsApi,
  PluginView
} from '../api/pluginsCore'

export interface PluginsStoreState {
  plugins: PluginView[]
  updates: Record<string, string>
  installingIds: Record<string, number>
  loading: boolean
  offline: boolean
  error: string | null
  lastOperationOutcome: PluginOperationOutcome | null
  lastScanOutcome: PluginScanOutcome | null
  load: (force?: boolean) => Promise<void>
  install: (pluginId: string) => Promise<boolean>
  uninstall: (pluginId: string) => Promise<{ ok: boolean; dependencyBlocked: boolean }>
  refresh: () => Promise<void>
  importFile: (path: string) => Promise<boolean>
}

export type PluginsStoreSetter = (
  update:
    | Partial<PluginsStoreState>
    | ((state: PluginsStoreState) => Partial<PluginsStoreState>)
) => void

export type PluginsStoreGetter = () => PluginsStoreState

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error)
}

function operationError(
  fallback: string,
  outcome: PluginOperationOutcome
): string {
  const recovery = outcome.recoveryPath ?? outcome.recoveryId
  return `${outcome.error ?? fallback}${recovery ? ` Recovery: ${recovery}` : ''}`
}

export function createPluginsStoreState(
  api: PluginsApi,
  set: PluginsStoreSetter,
  get: PluginsStoreGetter
): PluginsStoreState {
  return {
    plugins: [],
    updates: {},
    installingIds: {},
    loading: false,
    offline: false,
    error: null,
    lastOperationOutcome: null,
    lastScanOutcome: null,

    async load(force = false) {
      if (get().loading) return
      set({ loading: true, error: null })
      try {
        const [listResult, updatesResult] = await Promise.all([
          api.list(force),
          api.checkUpdates()
        ])
        const updates: Record<string, string> = {}
        for (const update of updatesResult.updates) {
          updates[update.id] = update.availableVersion
        }
        set({
          plugins: listResult.plugins,
          updates,
          offline: !listResult.online,
          loading: false
        })
      } catch (error) {
        set({ error: errorMessage(error), loading: false })
      }
    },

    async install(pluginId) {
      set({
        error: null,
        installingIds: { ...get().installingIds, [pluginId]: 0 }
      })
      try {
        const result = await api.install(pluginId)
        if (!result.ok || result.degraded) {
          set({
            lastOperationOutcome: result,
            error: operationError(`Failed to install plugin: ${pluginId}`, result)
          })
          return false
        }
        await get().load()
        set({ lastOperationOutcome: result })
        return true
      } catch (error) {
        set({ error: errorMessage(error) })
        return false
      } finally {
        const next = { ...get().installingIds }
        delete next[pluginId]
        set({ installingIds: next })
      }
    },

    async uninstall(pluginId) {
      set({ error: null })
      try {
        const result = await api.uninstall(pluginId)
        const dependencyBlocked = result.dependencyBlocked ?? false
        if (result.ok) {
          await get().load()
        } else if (!dependencyBlocked) {
          set({ error: `Failed to uninstall plugin: ${pluginId}` })
        }
        return { ok: result.ok, dependencyBlocked }
      } catch (error) {
        set({ error: errorMessage(error) })
        return { ok: false, dependencyBlocked: false }
      }
    },

    async refresh() {
      set({ error: null })
      try {
        const outcome = await api.refresh()
        await get().load(true)
        set({
          lastScanOutcome: outcome,
          error:
            outcome.ok && !outcome.degraded
              ? null
              : outcome.failures
                  .map((failure) =>
                    operationError('Plugin refresh candidate failed.', failure)
                  )
                  .join('\n') || 'Plugin refresh completed with degraded state.'
        })
      } catch (error) {
        set({ error: errorMessage(error) })
      }
    },

    async importFile(path) {
      set({ error: null })
      try {
        const result = await api.importFile(path)
        if (!result.ok || result.degraded) {
          set({
            lastOperationOutcome: result,
            error: operationError(`Failed to import plugin package: ${path}`, result)
          })
          return false
        }
        await get().load()
        set({ lastOperationOutcome: result })
        return true
      } catch (error) {
        set({ error: errorMessage(error) })
        return false
      }
    }
  }
}

export function reduceInstallProgress(
  state: PluginsStoreState,
  progress: InstallProgress
): Partial<PluginsStoreState> | null {
  if (!(progress.pluginId in state.installingIds)) return null
  if (progress.phase === 'completed' || progress.phase === 'failed') {
    const next = { ...state.installingIds }
    delete next[progress.pluginId]
    return { installingIds: next }
  }
  return {
    installingIds: {
      ...state.installingIds,
      [progress.pluginId]: progress.progressPercentage
    }
  }
}
