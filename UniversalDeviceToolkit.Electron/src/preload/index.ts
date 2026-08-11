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
  setBackgroundMaterial: (material: 'none' | 'mica' | 'acrylic'): Promise<void> =>
    ipcRenderer.invoke('window:set-background-material', material),
  openLogFolder: (): Promise<void> => ipcRenderer.invoke('shell:open-log-folder'),
  openExternal: (url: string): Promise<{ opened: boolean }> =>
    ipcRenderer.invoke('shell:open-external', url),
  openPath: (path: string): Promise<{ opened: boolean }> =>
    ipcRenderer.invoke('shell:open-path', path),
  quitApp: (): void => ipcRenderer.send('app:quit'),
  selectPluginFiles: (): Promise<string[]> => ipcRenderer.invoke('dialog:select-plugin-files'),
  selectJsonFile: (): Promise<string | null> => ipcRenderer.invoke('dialog:select-json-file'),
  selectExeFile: (): Promise<string | null> => ipcRenderer.invoke('dialog:select-exe-file'),
  isMaximized: (): Promise<boolean> => ipcRenderer.invoke('window:is-maximized'),
  isFullscreen: (): Promise<boolean> => ipcRenderer.invoke('window:is-fullscreen'),
  onFullscreenChanged: (callback: (fullscreen: boolean) => void): (() => void) => {
    const listener = (_event: Electron.IpcRendererEvent, fullscreen: boolean): void => {
      callback(fullscreen)
    }
    ipcRenderer.on('window:fullscreen-changed', listener)
    return () => ipcRenderer.removeListener('window:fullscreen-changed', listener)
  },
  onMaximizedChanged: (callback: (maximized: boolean) => void): (() => void) => {
    const listener = (_event: Electron.IpcRendererEvent, maximized: boolean): void => {
      callback(maximized)
    }
    ipcRenderer.on('window:maximized-changed', listener)
    return () => ipcRenderer.removeListener('window:maximized-changed', listener)
  }
}

contextBridge.exposeInMainWorld('bridge', bridge)
