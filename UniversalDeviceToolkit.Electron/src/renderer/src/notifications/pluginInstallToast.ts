import i18n from '../i18n'
import { pluginsApi } from '../api/plugins'
import { usePluginsStore } from '../stores/pluginsStore'
import { completeProgressToast, startProgressToast, updateProgressToast } from './progressToast'
import type { ProgressToastId } from './progressToast'

/**
 * Mirrors Electron PluginInstallNotificationBridge: mirrors plugin download
 * progress into a persistent progress toast so users see installs even after
 * navigating away from the extensions page. The page's own success/failure
 * feedback remains the completion signal.
 */

const toastsByPluginId = new Map<string, ProgressToastId>()

function pluginDisplayName(pluginId: string): string {
  const plugin = usePluginsStore.getState().plugins.find((item) => item.id === pluginId)
  return plugin && plugin.name.trim().length > 0 ? plugin.name : pluginId
}

function fallbackStatusText(percent: number): string {
  if (percent > 0) return `${i18n.t('plugins.downloading')} ${Math.round(percent)}%`
  return i18n.t('plugins.preparingDownload')
}

function updateToast(pluginId: string, percent: number, statusText?: string): void {
  const existing = toastsByPluginId.get(pluginId)
  const title = pluginDisplayName(pluginId)
  const status = statusText !== undefined && statusText.trim().length > 0
    ? statusText
    : fallbackStatusText(percent)

  if (existing === undefined) {
    toastsByPluginId.set(pluginId, startProgressToast(title, status))
    return
  }
  updateProgressToast(existing, percent, status)
}

function completeToast(pluginId: string): void {
  const existing = toastsByPluginId.get(pluginId)
  if (existing === undefined) return
  toastsByPluginId.delete(pluginId)
  completeProgressToast(existing)
}

/** Mirrors the Electron bridge's Sync: any plugin that stopped installing dismisses its toast. */
function syncCompleted(installingIds: Record<string, number>): void {
  for (const pluginId of [...toastsByPluginId.keys()]) {
    if (!(pluginId in installingIds)) completeToast(pluginId)
  }
}

export function initPluginInstallToast(): void {
  usePluginsStore.subscribe((state) => {
    syncCompleted(state.installingIds)
  })

  pluginsApi.onInstallProgress((progress) => {
    if (progress.phase === 'completed' || progress.phase === 'failed') {
      completeToast(progress.pluginId)
      return
    }
    updateToast(progress.pluginId, progress.progressPercentage, progress.statusText)
  })

  syncCompleted(usePluginsStore.getState().installingIds)
}
