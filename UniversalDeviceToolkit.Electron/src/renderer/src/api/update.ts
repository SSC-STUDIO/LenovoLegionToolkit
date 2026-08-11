import { invoke, on } from './bridge'

export interface UpdateReleaseInfo {
  version: string
  url: string
  assetUrl: string
  assetName: string
  assetSize: number
  releaseNotes: string | null
  releaseDate: string | null
}

export interface DownloadProgress {
  percent: number
  receivedBytes: number
  totalBytes: number
  done: boolean
  error?: string
  elapsedMs?: number
}

export const updateApi = {
  check: (force = false) =>
    invoke<{ available: boolean; version?: string | null; error?: string | null }>('app.update.check', { force }),
  status: () => invoke<{ status: string; disable: boolean }>('app.update.status'),
  /** Resolves the Electron installer asset from the GitHub latest release. */
  getRelease: () => invoke<{ release: UpdateReleaseInfo | null }>('update.getRelease', {}),
  /** Downloads the installer in the main process; progress arrives via onDownloadProgress. */
  download: () => invoke<{ ok: boolean; path?: string; error?: string }>('update.download', {}),
  /** Launches the downloaded NSIS installer silently and quits the app. */
  launchInstaller: (path: string) =>
    invoke<{ ok: boolean }>('update.launchInstaller', { path }),
  onDownloadProgress: (cb: (progress: DownloadProgress) => void): (() => void) =>
    on<DownloadProgress>('update.download-progress', cb)
}
