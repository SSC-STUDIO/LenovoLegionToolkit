/**
 * Guest preload for plugin web pages hosted in <webview> elements.
 *
 * Injects `window.pluginHost` into the plugin's own page:
 *   - invoke(method, params) -> Promise — routed to the host JSON-RPC backend
 *     via the main window's preload bridge (sendToHost round trip).
 *   - on(event, callback) -> unsubscribe — subscribe to bridge events
 *     (e.g. notifications.changed, sensors.updated).
 *
 * The main process forwards `plugin-host:*` ipc-messages to hostClient and
 * pushes responses/events back into the guest webContents.
 */
import { contextBridge, ipcRenderer } from 'electron'

interface PendingRequest {
  resolve: (value: unknown) => void
  reject: (error: Error) => void
}

let requestSeq = 0
const pending = new Map<number, PendingRequest>()

ipcRenderer.on('plugin-host:response', (_event, id: number, result: unknown, error: string | null) => {
  const request = pending.get(id)
  if (!request) return
  pending.delete(id)
  if (error != null) {
    request.reject(new Error(error))
  } else {
    request.resolve(result)
  }
})

ipcRenderer.on('plugin-host:event', (_event, name: string, data: unknown) => {
  const listeners = eventListeners.get(name)
  if (!listeners) return
  for (const listener of listeners) {
    try {
      listener(data)
    } catch {
      // a broken plugin listener must not break the event loop
    }
  }
})

const eventListeners = new Map<string, Set<(data: unknown) => void>>()

contextBridge.exposeInMainWorld('pluginHost', {
  invoke(method: string, params?: unknown): Promise<unknown> {
    return new Promise((resolve, reject) => {
      const id = ++requestSeq
      pending.set(id, { resolve, reject })
      ipcRenderer.sendToHost('plugin-host:invoke', id, method, params)
    })
  },
  on(event: string, callback: (data: unknown) => void): () => void {
    let listeners = eventListeners.get(event)
    if (!listeners) {
      listeners = new Set()
      eventListeners.set(event, listeners)
    }
    listeners.add(callback)
    return () => {
      listeners?.delete(callback)
    }
  }
})
