import { contextBridge, ipcRenderer } from 'electron'

const bridge = {
  invoke: (method: string, params?: unknown): Promise<unknown> =>
    ipcRenderer.invoke('bridge:invoke', method, params),
  on: (event: string, callback: (data: unknown) => void): (() => void) => {
    const listener = (_event: Electron.IpcRendererEvent, receivedEvent: string, data: unknown): void => {
      if (receivedEvent === event) callback(data)
    }
    ipcRenderer.on('bridge:event', listener)
    return () => ipcRenderer.removeListener('bridge:event', listener)
  }
}

contextBridge.exposeInMainWorld('bridge', bridge)
