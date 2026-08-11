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
  },
  minimize: (): void => ipcRenderer.send('window:minimize'),
  maximizeToggle: (): void => ipcRenderer.send('window:maximize-toggle'),
  closeWindow: (): void => ipcRenderer.send('window:close'),
  isMaximized: (): Promise<boolean> => ipcRenderer.invoke('window:is-maximized'),
  onMaximizedChanged: (callback: (maximized: boolean) => void): (() => void) => {
    const listener = (_event: Electron.IpcRendererEvent, maximized: boolean): void => {
      callback(maximized)
    }
    ipcRenderer.on('window:maximized-changed', listener)
    return () => ipcRenderer.removeListener('window:maximized-changed', listener)
  }
}

contextBridge.exposeInMainWorld('bridge', bridge)
