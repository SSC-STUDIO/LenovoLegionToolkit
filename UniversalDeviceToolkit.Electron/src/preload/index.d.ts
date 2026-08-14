export interface Bridge {
  /** Runtime platform ('darwin' on macOS) — drives native title bar layout. */
  platform: string
  invoke: (method: string, params?: unknown) => Promise<unknown>
  getHostStatus: () => Promise<{
    running: boolean
    ready: boolean
    lastError: string | null
    readyPayload: unknown
  }>
  on: (event: string, callback: (data: unknown) => void) => () => void
  minimize: () => void
  maximizeToggle: () => void
  closeWindow: () => void
  setBackgroundMaterial: (material: 'none' | 'mica' | 'acrylic') => Promise<void>
  openLogFolder: () => Promise<void>
  /** Renderer → main log channel (leveled; lands in userData/logs/renderer.log). */
  log: (level: string, message: string) => void
  openAppFolder: (kind: 'data' | 'temp' | 'log') => Promise<{ opened: boolean }>
  openExternal: (url: string) => Promise<{ opened: boolean }>
  openPath: (path: string) => Promise<{ opened: boolean }>
  quitApp: () => void
  selectPluginFiles: () => Promise<string[]>
  selectJsonFile: () => Promise<string | null>
  selectExeFile: () => Promise<string | null>
  selectAudioFile: () => Promise<string | null>
  isMaximized: () => Promise<boolean>
  getPluginPreloadPath: () => Promise<string>
  onMaximizedChanged: (callback: (maximized: boolean) => void) => () => void
  isFullscreen: () => Promise<boolean>
  onFullscreenChanged: (callback: (fullscreen: boolean) => void) => () => void
  setTrayLanguage: (lang: string) => void
  refreshTrayMenu: () => void
  writeClipboardLines: (lines: string[]) => Promise<{ ok: boolean }>
  readClipboardExistingPaths: () => Promise<string[]>
  setAutorun: (enabled: boolean) => Promise<{ ok: boolean; enabled: boolean }>
  getAutorun: () => Promise<{ enabled: boolean }>
  setThemeSource: (source: 'system' | 'light' | 'dark') => void
  /** Applies platformBaseZoom x scale to every surface in the main process. */
  setUiScale: (scale: number) => Promise<{ ok: boolean; scale: number }>
  /** Real production memory footprint across every Electron process (MB). */
  getMemoryUsage: () => Promise<{
    processes: Array<{ name: string; type: string; workingSetMB: number }>
    totalMB: number
  }>
}

declare global {
  interface Window {
    bridge?: Bridge
  }
}

export {}
