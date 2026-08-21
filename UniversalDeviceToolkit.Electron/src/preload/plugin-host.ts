/**
 * Guest preload for plugin web pages hosted in <webview> elements.
 *
 * Injects `window.pluginHost` into the plugin's own page:
 *   - invoke(method, params) -> Promise — sendToHost to the embedder <webview>
 *     `ipc-message` listener, which applies the plugin method whitelist and
 *     forwards allowed calls through the main window bridge to Host JSON-RPC.
 *   - on(event, callback) -> unsubscribe — plugin.* bridge events only
 *     (pushed into the guest webContents as plugin-host:event).
 */
import { contextBridge, ipcRenderer } from 'electron'

interface PendingRequest {
  resolve: (value: unknown) => void
  reject: (error: Error) => void
  timer: ReturnType<typeof setTimeout>
}

const INVOKE_TIMEOUT_MS = 60_000
const PLUGIN_HOST_INVOKE_CHANNEL = 'plugin-host:invoke'
const PLUGIN_HOST_RESPONSE_CHANNEL = 'plugin-host:response'
const PLUGIN_HOST_EVENT_CHANNEL = 'plugin-host:event'

let requestSeq = 0
const pending = new Map<number, PendingRequest>()
const eventListeners = new Map<string, Set<(data: unknown) => void>>()

function rejectPending(id: number, error: Error): void {
  const request = pending.get(id)
  if (request == null) return
  pending.delete(id)
  clearTimeout(request.timer)
  request.reject(error)
}

function resolvePending(id: number, result: unknown): void {
  const request = pending.get(id)
  if (request == null) return
  pending.delete(id)
  clearTimeout(request.timer)
  request.resolve(result)
}

ipcRenderer.on(
  PLUGIN_HOST_RESPONSE_CHANNEL,
  (_event, id: unknown, result: unknown, error: unknown) => {
    if (typeof id !== 'number') return
    if (typeof error === 'string') {
      rejectPending(id, new Error(error))
      return
    }
    resolvePending(id, result)
  }
)

ipcRenderer.on(PLUGIN_HOST_EVENT_CHANNEL, (_event, name: unknown, data: unknown) => {
  if (typeof name !== 'string' || !name.startsWith('plugin.')) return
  const listeners = eventListeners.get(name)
  if (listeners == null) return
  for (const listener of listeners) {
    try {
      listener(data)
    } catch {
      // a broken plugin listener must not break the event loop
    }
  }
})

contextBridge.exposeInMainWorld('pluginHost', {
  invoke(method: string, params?: unknown): Promise<unknown> {
    if (typeof method !== 'string' || method.length === 0) {
      return Promise.reject(new Error('A plugin host method name is required.'))
    }
    return new Promise((resolve, reject) => {
      const id = ++requestSeq
      const timer = setTimeout(() => {
        rejectPending(id, new Error('Plugin host invoke timed out.'))
      }, INVOKE_TIMEOUT_MS)
      pending.set(id, { resolve, reject, timer })
      ipcRenderer.sendToHost(PLUGIN_HOST_INVOKE_CHANNEL, id, method, params)
    })
  },
  on(event: string, callback: (data: unknown) => void): () => void {
    if (typeof event !== 'string' || !event.startsWith('plugin.')) {
      return () => undefined
    }
    let listeners = eventListeners.get(event)
    if (listeners == null) {
      listeners = new Set()
      eventListeners.set(event, listeners)
    }
    listeners.add(callback)
    return () => {
      listeners.delete(callback)
    }
  }
})
