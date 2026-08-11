export interface Bridge {
  invoke: (method: string, params?: unknown) => Promise<unknown>
  on: (event: string, callback: (data: unknown) => void) => () => void
  minimize: () => void
  maximizeToggle: () => void
  closeWindow: () => void
  setBackgroundMaterial: (material: 'none' | 'mica' | 'acrylic') => Promise<void>
  openLogFolder: () => Promise<void>
  openExternal: (url: string) => Promise<{ opened: boolean }>
  openPath: (path: string) => Promise<{ opened: boolean }>
  quitApp: () => void
  selectPluginFiles: () => Promise<string[]>
  selectJsonFile: () => Promise<string | null>
  selectExeFile: () => Promise<string | null>
  isMaximized: () => Promise<boolean>
  onMaximizedChanged: (callback: (maximized: boolean) => void) => () => void
  isFullscreen: () => Promise<boolean>
  onFullscreenChanged: (callback: (fullscreen: boolean) => void) => () => void
}

declare global {
  interface Window {
    bridge?: Bridge
  }
}

export {}
