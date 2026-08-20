export const PLUGIN_HOST_INVOKE_CHANNEL = 'plugin-host:invoke'
export const PLUGIN_HOST_RESPONSE_CHANNEL = 'plugin-host:response'
export const PLUGIN_HOST_EVENT_CHANNEL = 'plugin-host:event'

export const PLUGIN_WEBVIEW_PREFERENCES =
  'contextIsolation=yes, nodeIntegration=no, sandbox=yes, webSecurity=yes'

const PLUGIN_PARTITION_PREFIX = 'persist:plugin-'

const PLUGIN_CONFIG_METHODS = new Set(['plugins.getConfig', 'plugins.setConfig'])

const PLUGIN_DIALOG_METHODS = new Set([
  'dialog:open-file',
  'dialog:save-file',
  'dialog:select-folder',
  'dialog:open-path'
])

const OFFICIAL_PLUGIN_RPC_PREFIXES: Readonly<Record<string, readonly string[]>> = {
  'custom-mouse': ['plugin.customMouse.'],
  'shell-integration': ['plugin.shell.'],
  'vive-tool': ['plugin.vive.']
}

export interface PluginWebviewEmbedder {
  send: (channel: string, ...args: unknown[]) => void
  stop: () => void
  getURL?: () => string
  loadURL?: (url: string) => void
  setAttribute: (name: string, value: string) => void
  addEventListener: (type: string, listener: (event: Event) => void) => void
  removeEventListener: (type: string, listener: (event: Event) => void) => void
}

export interface PluginWebviewSession {
  pluginId: string
  entryUrl: string
  directoryUrl: string | null
  invoke: (method: string, params?: unknown) => Promise<unknown>
}

export interface PluginWebPreferences {
  nodeIntegration?: boolean
  nodeIntegrationInSubFrames?: boolean
  contextIsolation?: boolean
  sandbox?: boolean
  webSecurity?: boolean
  allowRunningInsecureContent?: boolean
  preload?: string
  preloadURL?: string
  nativeWindowOpen?: boolean
}

export interface PluginAttachParams {
  src?: string
  partition?: string
}

function kebabToCamel(pluginId: string): string {
  let result = ''
  let upperNext = false
  for (let index = 0; index < pluginId.length; index += 1) {
    const character = pluginId[index]
    if (character === '-') {
      upperNext = true
      continue
    }
    result += upperNext ? character.toUpperCase() : character
    upperNext = false
  }
  return result
}

function pluginRpcPrefixes(pluginId: string): readonly string[] {
  const official = OFFICIAL_PLUGIN_RPC_PREFIXES[pluginId]
  if (official != null) return official
  const camelId = kebabToCamel(pluginId)
  if (camelId.length === 0) return []
  return [`plugin.${camelId}.`]
}

export function isAllowedPluginBridgeMethod(pluginId: string, method: string): boolean {
  if (pluginId.length === 0) return false
  if (typeof method !== 'string' || method.length === 0) return false
  if (PLUGIN_CONFIG_METHODS.has(method)) return true
  if (PLUGIN_DIALOG_METHODS.has(method)) return true
  for (const prefix of pluginRpcPrefixes(pluginId)) {
    if (method.startsWith(prefix) && method.length > prefix.length) return true
  }
  return false
}

export function bindPluginBridgeParams(
  pluginId: string,
  method: string,
  params: unknown
): unknown {
  if (!PLUGIN_CONFIG_METHODS.has(method)) return params
  const record =
    params != null && typeof params === 'object' && !Array.isArray(params)
      ? { ...(params as Record<string, unknown>) }
      : {}
  record.pluginId = pluginId
  return record
}

function isFileUrl(url: string): boolean {
  try {
    return new URL(url).protocol === 'file:'
  } catch {
    return false
  }
}

function stripUrlTail(url: string): string {
  const hash = url.indexOf('#')
  const withoutHash = hash >= 0 ? url.slice(0, hash) : url
  const query = withoutHash.indexOf('?')
  return query >= 0 ? withoutHash.slice(0, query) : withoutHash
}

export function isPluginWebviewPartition(partition: string | undefined): boolean {
  return (
    typeof partition === 'string' &&
    partition.startsWith(PLUGIN_PARTITION_PREFIX) &&
    partition.length > PLUGIN_PARTITION_PREFIX.length
  )
}

export function isPathInsideDirectory(url: string, directoryUrl: string): boolean {
  let file: URL
  let directory: URL
  try {
    file = new URL(url)
    directory = new URL(directoryUrl)
  } catch {
    return false
  }
  if (file.protocol !== 'file:' || directory.protocol !== 'file:') return false

  let filePath: string
  let directoryPath: string
  try {
    filePath = decodeURIComponent(file.pathname).toLowerCase().replace(/\\/g, '/')
    directoryPath = decodeURIComponent(directory.pathname).toLowerCase().replace(/\\/g, '/')
  } catch {
    return false
  }
  if (!directoryPath.endsWith('/')) directoryPath += '/'
  if (filePath === directoryPath.slice(0, -1)) return true
  return filePath.startsWith(directoryPath)
}

export function isAllowedPluginNavigationUrl(
  url: string,
  entryUrl: string,
  directoryUrl: string | null
): boolean {
  if (typeof url !== 'string' || url.length === 0) return false
  if (!isFileUrl(url)) return false
  const stripped = stripUrlTail(url)
  if (entryUrl.length > 0 && stripped === stripUrlTail(entryUrl)) return true
  if (directoryUrl == null || directoryUrl.length === 0) return false
  return isPathInsideDirectory(url, directoryUrl)
}

export function parsePluginHostInvokeArgs(
  args: unknown[]
): { id: number; method: string; params: unknown } | null {
  if (args.length < 2) return null
  const id = args[0]
  const method = args[1]
  if (typeof id !== 'number' || !Number.isInteger(id)) return null
  if (typeof method !== 'string' || method.length === 0) return null
  return { id, method, params: args.length > 2 ? args[2] : undefined }
}

function copyUnknownArray(value: unknown): unknown[] {
  if (value == null || typeof value !== 'object') return []
  const lengthValue = (value as { length?: unknown }).length
  if (typeof lengthValue !== 'number' || !Number.isInteger(lengthValue) || lengthValue < 0) {
    return []
  }
  const copied: unknown[] = []
  const record = value as Record<number, unknown>
  for (let index = 0; index < lengthValue; index += 1) {
    copied.push(record[index])
  }
  return copied
}

function deliverPluginHostResponse(
  send: (channel: string, ...args: unknown[]) => void,
  id: number,
  result: unknown,
  error: string | null
): void {
  try {
    send(PLUGIN_HOST_RESPONSE_CHANNEL, id, result, error)
  } catch {
    // guest frame already gone; nothing to deliver
  }
}

export async function dispatchPluginHostInvoke(
  pluginId: string,
  id: number,
  method: string,
  params: unknown,
  invoke: (method: string, params?: unknown) => Promise<unknown>,
  send: (channel: string, ...args: unknown[]) => void
): Promise<void> {
  if (typeof id !== 'number' || !Number.isInteger(id)) return
  if (!isAllowedPluginBridgeMethod(pluginId, method)) {
    deliverPluginHostResponse(
      send,
      id,
      null,
      `Method '${method}' is not available to this plugin.`
    )
    return
  }
  try {
    const result = await invoke(method, bindPluginBridgeParams(pluginId, method, params))
    deliverPluginHostResponse(send, id, result, null)
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error)
    deliverPluginHostResponse(send, id, null, message)
  }
}

function eventUrl(event: Event): string {
  const record = event as Event & { url?: unknown }
  return typeof record.url === 'string' ? record.url : ''
}

function restorePluginEntry(webview: PluginWebviewEmbedder, entryUrl: string): void {
  try {
    webview.stop()
  } catch {
    // guest may already be gone
  }
  if (entryUrl.length === 0) return
  if (typeof webview.getURL === 'function') {
    try {
      if (stripUrlTail(webview.getURL()) === stripUrlTail(entryUrl)) return
    } catch {
      // getURL can throw if the guest is destroyed
    }
  }
  if (typeof webview.loadURL === 'function') {
    try {
      webview.loadURL(entryUrl)
      return
    } catch {
      // fall through to the src attribute
    }
  }
  webview.setAttribute('src', entryUrl)
}

export function bindPluginWebviewEmbedder(
  webview: PluginWebviewEmbedder,
  session: PluginWebviewSession
): () => void {
  const onIpcMessage = (event: Event): void => {
    const ipcEvent = event as Event & { channel?: unknown; args?: unknown }
    if (String(ipcEvent.channel ?? '') !== PLUGIN_HOST_INVOKE_CHANNEL) return
    const parsed = parsePluginHostInvokeArgs(copyUnknownArray(ipcEvent.args))
    if (parsed == null) return
    void dispatchPluginHostInvoke(
      session.pluginId,
      parsed.id,
      parsed.method,
      parsed.params,
      session.invoke,
      (channel, ...sendArgs) => {
        webview.send(channel, ...sendArgs)
      }
    )
  }

  const onNavigate = (event: Event): void => {
    const url = eventUrl(event)
    if (url.length === 0) return
    if (isAllowedPluginNavigationUrl(url, session.entryUrl, session.directoryUrl)) return
    if (typeof event.preventDefault === 'function') event.preventDefault()
    restorePluginEntry(webview, session.entryUrl)
  }

  const onPopup = (event: Event): void => {
    if (typeof event.preventDefault === 'function') event.preventDefault()
  }

  webview.addEventListener('ipc-message', onIpcMessage)
  webview.addEventListener('will-navigate', onNavigate)
  webview.addEventListener('will-redirect', onNavigate)
  webview.addEventListener('did-start-navigation', onNavigate)
  webview.addEventListener('did-navigate', onNavigate)
  webview.addEventListener('did-navigate-in-page', onNavigate)
  webview.addEventListener('new-window', onPopup)

  return () => {
    webview.removeEventListener('ipc-message', onIpcMessage)
    webview.removeEventListener('will-navigate', onNavigate)
    webview.removeEventListener('will-redirect', onNavigate)
    webview.removeEventListener('did-start-navigation', onNavigate)
    webview.removeEventListener('did-navigate', onNavigate)
    webview.removeEventListener('did-navigate-in-page', onNavigate)
    webview.removeEventListener('new-window', onPopup)
  }
}
