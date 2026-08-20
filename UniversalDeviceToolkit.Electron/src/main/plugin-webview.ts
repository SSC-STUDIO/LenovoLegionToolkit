import type { App, WebContents } from 'electron'
import {
  PLUGIN_HOST_INVOKE_CHANNEL,
  PLUGIN_HOST_RESPONSE_CHANNEL,
  PLUGIN_HOST_EVENT_CHANNEL,
  PLUGIN_WEBVIEW_PREFERENCES,
  type PluginWebviewEmbedder,
  type PluginWebviewSession,
  type PluginWebPreferences,
  type PluginAttachParams,
  isAllowedPluginBridgeMethod,
  bindPluginBridgeParams,
  isPluginWebviewPartition,
  isPathInsideDirectory,
  isAllowedPluginNavigationUrl,
  parsePluginHostInvokeArgs,
  dispatchPluginHostInvoke,
  bindPluginWebviewEmbedder
} from '../shared/plugin-webview'

export {
  PLUGIN_HOST_INVOKE_CHANNEL,
  PLUGIN_HOST_RESPONSE_CHANNEL,
  PLUGIN_HOST_EVENT_CHANNEL,
  PLUGIN_WEBVIEW_PREFERENCES,
  type PluginWebviewEmbedder,
  type PluginWebviewSession,
  type PluginWebPreferences,
  type PluginAttachParams,
  isAllowedPluginBridgeMethod,
  bindPluginBridgeParams,
  isPluginWebviewPartition,
  isPathInsideDirectory,
  isAllowedPluginNavigationUrl,
  parsePluginHostInvokeArgs,
  dispatchPluginHostInvoke,
  bindPluginWebviewEmbedder
}

export type PluginGuestContents = WebContents
export type PluginWebviewApp = App

function isFileUrl(url: string): boolean {
  try {
    return new URL(url).protocol === 'file:'
  } catch {
    return false
  }
}

function asPreventable(event: unknown): { preventDefault: () => void } | null {
  if (event == null || typeof event !== 'object') return null
  const preventDefault = (event as { preventDefault?: unknown }).preventDefault
  if (typeof preventDefault !== 'function') return null
  return event as { preventDefault: () => void }
}

function denyNonFileNavigation(event: unknown, url: unknown): void {
  if (typeof url === 'string' && isFileUrl(url)) return
  asPreventable(event)?.preventDefault()
}

export function lockPluginWebviewPreferences(
  webPreferences: PluginWebPreferences,
  preloadPath: string
): void {
  webPreferences.nodeIntegration = false
  webPreferences.nodeIntegrationInSubFrames = false
  webPreferences.contextIsolation = true
  webPreferences.sandbox = true
  webPreferences.webSecurity = true
  webPreferences.allowRunningInsecureContent = false
  webPreferences.nativeWindowOpen = false
  webPreferences.preload = preloadPath
  delete webPreferences.preloadURL
}

export function attachPluginWebviewContents(
  contents: PluginGuestContents,
  preloadPath: string
): void {
  contents.on('will-attach-webview', (event, webPreferences, params) => {
    const attach = (params ?? {}) as PluginAttachParams
    const src = typeof attach.src === 'string' ? attach.src : ''
    if (!isFileUrl(src) || !isPluginWebviewPartition(attach.partition)) {
      asPreventable(event)?.preventDefault()
      return
    }
    if (webPreferences == null || typeof webPreferences !== 'object') {
      asPreventable(event)?.preventDefault()
      return
    }
    lockPluginWebviewPreferences(webPreferences as PluginWebPreferences, preloadPath)
  })

  if (contents.getType() !== 'webview') return

  contents.setWindowOpenHandler(() => ({ action: 'deny' }))
  contents.on('will-navigate', (event, url) => {
    denyNonFileNavigation(event, url)
  })
  contents.on('will-redirect', (event, url) => {
    denyNonFileNavigation(event, url)
  })
}

export function installPluginWebviewGuards(preloadPath: string, app: PluginWebviewApp): void {
  app.on('web-contents-created', (_event, contents) => {
    attachPluginWebviewContents(contents, preloadPath)
  })
}
