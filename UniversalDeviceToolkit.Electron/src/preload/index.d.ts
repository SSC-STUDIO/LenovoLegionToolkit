export interface Bridge {
  invoke: (method: string, params?: unknown) => Promise<unknown>
  on: (event: string, callback: (data: unknown) => void) => () => void
  minimize: () => void
  maximizeToggle: () => void
  closeWindow: () => void
  setBackgroundMaterial: (material: 'none' | 'mica' | 'acrylic') => Promise<void>
  openLogFolder: () => Promise<void>
  selectPluginFiles: () => Promise<string[]>
  isMaximized: () => Promise<boolean>
  onMaximizedChanged: (callback: (maximized: boolean) => void) => () => void
}

declare global {
  interface Window {
    bridge?: Bridge
  }
}

export {}
