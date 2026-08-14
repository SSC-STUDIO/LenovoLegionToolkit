import type { PluginView } from '../api/pluginsCore'

export type PluginFilterValue = 'all' | 'installed' | 'notInstalled'

export interface PluginExtensionsSummary {
  totalCount: number
  installedCount: number
  updateCount: number
  installableIds: string[]
  updatableIds: string[]
}

export interface PluginCardActions {
  installed: boolean
  canInstallOrUpdate: boolean
  canConfigure: boolean
  canOpenWebPage: boolean
  canOpenCapability: boolean
  canUninstall: boolean
}

export interface PluginOperationBatchResult<T> {
  succeeded: T[]
  failed: T[]
}

export type UninstallFeedback = 'dependencyBlocked' | 'failed' | null

export function filterPlugins(
  plugins: readonly PluginView[],
  filter: PluginFilterValue,
  search: string
): PluginView[] {
  const query = search.trim().toLowerCase()
  return plugins.filter((plugin) => {
    const installed = Boolean(plugin.installedVersion)
    if (filter === 'installed' && !installed) return false
    if (filter === 'notInstalled' && installed) return false
    if (query.length === 0) return true
    return (
      plugin.name.toLowerCase().includes(query) ||
      plugin.description.toLowerCase().includes(query) ||
      plugin.id.toLowerCase().includes(query) ||
      plugin.tags.some((tag) => tag.toLowerCase().includes(query))
    )
  })
}

export function summarizePlugins(plugins: readonly PluginView[]): PluginExtensionsSummary {
  const installedCount = plugins.filter((plugin) => Boolean(plugin.installedVersion)).length
  const updateCount = plugins.filter((plugin) => plugin.updateAvailable).length
  const installableIds = plugins
    .filter((plugin) => !plugin.installedVersion && !plugin.isSystemPlugin)
    .map((plugin) => plugin.id)
  const updatableIds = plugins
    .filter((plugin) => plugin.updateAvailable)
    .map((plugin) => plugin.id)

  return {
    totalCount: plugins.length,
    installedCount,
    updateCount,
    installableIds,
    updatableIds
  }
}

export function pluginCardActions(plugin: PluginView): PluginCardActions {
  const installed = Boolean(plugin.installedVersion)
  const supportsCapability =
    plugin.capabilities.settingsPage ||
    plugin.capabilities.featurePage ||
    plugin.capabilities.optimizationCategory ||
    plugin.capabilities.executableEntryPoint

  return {
    installed,
    canInstallOrUpdate: !installed || plugin.updateAvailable,
    canConfigure: installed && plugin.capabilities.settingsPage,
    canOpenWebPage: installed && Boolean(plugin.webPage),
    canOpenCapability: installed && supportsCapability,
    canUninstall: installed
  }
}

export function uninstallFeedback(result: {
  ok: boolean
  dependencyBlocked: boolean
}): UninstallFeedback {
  if (result.dependencyBlocked) return 'dependencyBlocked'
  return result.ok ? null : 'failed'
}

export async function runPluginOperations<T>(
  items: readonly T[],
  operation: (item: T) => Promise<boolean>
): Promise<PluginOperationBatchResult<T>> {
  const succeeded: T[] = []
  const failed: T[] = []

  for (const item of items) {
    try {
      if (await operation(item)) {
        succeeded.push(item)
      } else {
        failed.push(item)
      }
    } catch {
      failed.push(item)
    }
  }

  return { succeeded, failed }
}

export function pluginFileName(path: string): string {
  return path.split(/[\\/]/).pop() ?? path
}
