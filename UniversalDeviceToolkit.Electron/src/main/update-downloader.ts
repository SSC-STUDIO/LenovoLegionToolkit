/**
 * Update downloader — Electron-side counterpart of the Electron UpdateChecker.
 * The host only reports availability/version; the main process resolves the
 * release asset from the GitHub API, downloads it with progress events and
 * launches the installer (Windows: NSIS silently; macOS: `open` the .dmg;
 * Linux: run the AppImage).
 */
import { app } from 'electron'
import { chmodSync, createWriteStream, existsSync, mkdirSync, readFileSync } from 'fs'
import { join } from 'path'
import { spawn } from 'child_process'
import { get as httpsGet } from 'https'

const REPO_OWNER = 'SSC-STUDIO'
const REPO_NAME = 'UniversalDeviceToolkit'
const API_LATEST = `https://api.github.com/repos/${REPO_OWNER}/${REPO_NAME}/releases/latest`
const USER_AGENT = 'UniversalDeviceToolkit-Electron'

export type InstallChannel = 'full' | 'online'

/** Packed by Scripts/Build-ElectronInstaller.ps1 (`full` or `online`). */
export function readInstallChannel(): InstallChannel {
  const candidates = [
    join(process.resourcesPath, 'install-channel'),
    join(app.getAppPath(), 'resources', 'install-channel')
  ]
  for (const candidate of candidates) {
    try {
      if (!existsSync(candidate)) continue
      const value = readFileSync(candidate, 'utf8').trim().toLowerCase()
      if (value === 'online') return 'online'
      if (value === 'full') return 'full'
    } catch {
      // Keep looking; missing markers default to Full.
    }
  }
  return 'full'
}

/** electron-builder artifact name per platform: NSIS .exe / .dmg / .AppImage. */
function assetPatternForPlatform(): RegExp {
  if (process.platform === 'darwin') return /UniversalDeviceToolkit.*\.dmg$/i
  if (process.platform === 'linux') return /UniversalDeviceToolkit.*\.AppImage$/i
  if (readInstallChannel() === 'online') {
    return /UniversalDeviceToolkit_v.+_Online_Setup\.exe$|UniversalDeviceToolkitOnlineSetup-.+\.exe$/i
  }
  return /UniversalDeviceToolkit_v.+_Full_Setup\.exe$|UniversalDeviceToolkitSetup-.+\.exe$/i
}

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
}

function fetchJson(url: string): Promise<unknown> {
  return new Promise((resolve, reject) => {
    const request = httpsGet(
      url,
      { headers: { 'User-Agent': USER_AGENT, Accept: 'application/vnd.github+json' }, timeout: 20000 },
      (response) => {
        if (response.statusCode !== 200) {
          response.resume()
          reject(new Error(`GitHub API returned ${response.statusCode ?? 'no status'}`))
          return
        }
        let body = ''
        response.setEncoding('utf8')
        response.on('data', (chunk: string) => {
          body += chunk
        })
        response.on('end', () => {
          try {
            resolve(JSON.parse(body))
          } catch (error) {
            reject(error instanceof Error ? error : new Error('Invalid JSON from GitHub API'))
          }
        })
      }
    )
    request.on('error', reject)
    request.on('timeout', () => {
      request.destroy(new Error('GitHub API request timed out'))
    })
  })
}

/** Resolves the latest release + the Electron setup asset (electron-builder artifactName). */
export async function getLatestRelease(): Promise<UpdateReleaseInfo | null> {
  try {
    const release = (await fetchJson(API_LATEST)) as {
      tag_name?: string
      published_at?: string
      body?: string
      assets?: Array<{ name?: string; browser_download_url?: string; size?: number }>
    }
    const assetPattern = assetPatternForPlatform()
    const asset = (release.assets ?? []).find((item) => item.name != null && assetPattern.test(item.name))
    if (asset?.browser_download_url == null || asset.name == null) {
      return null
    }
    return {
      version: release.tag_name ?? 'latest',
      url: `https://github.com/${REPO_OWNER}/${REPO_NAME}/releases/latest`,
      assetUrl: asset.browser_download_url,
      assetName: asset.name,
      assetSize: asset.size ?? 0,
      releaseNotes: release.body ?? null,
      releaseDate: release.published_at ?? null
    }
  } catch {
    return null
  }
}

function downloadToFile(url: string, destination: string, onProgress: (progress: DownloadProgress) => void): Promise<string> {
  return new Promise((resolve, reject) => {
    const download = (target: string, redirectsLeft: number): void => {
      const request = httpsGet(target, { headers: { 'User-Agent': USER_AGENT }, timeout: 60000 }, (response) => {
        const status = response.statusCode ?? 0
        if (status >= 300 && status < 400 && response.headers.location != null) {
          response.resume()
          if (redirectsLeft <= 0) {
            reject(new Error('Too many redirects'))
            return
          }
          download(new URL(response.headers.location, target).toString(), redirectsLeft - 1)
          return
        }
        if (status !== 200) {
          response.resume()
          reject(new Error(`Download failed with status ${status}`))
          return
        }
        const totalBytes = Number(response.headers['content-length'] ?? 0)
        let receivedBytes = 0
        const output = createWriteStream(destination)
        response.on('data', (chunk: Buffer) => {
          receivedBytes += chunk.length
          onProgress({
            percent: totalBytes > 0 ? Math.min(100, (receivedBytes / totalBytes) * 100) : 0,
            receivedBytes,
            totalBytes,
            done: false
          })
        })
        response.pipe(output)
        output.on('finish', () => {
          onProgress({ percent: 100, receivedBytes, totalBytes, done: true })
          resolve(destination)
        })
        output.on('error', reject)
      })
      request.on('error', reject)
      request.on('timeout', () => {
        request.destroy(new Error('Download timed out'))
      })
    }
    download(url, 5)
  })
}

function updatesDirectory(): string {
  const dir = join(app.getPath('userData'), 'updates')
  mkdirSync(dir, { recursive: true })
  return dir
}

/**
 * Downloads the latest installer. Returns the local file path on success;
 * rejects on any failure.
 */
export async function downloadLatestUpdate(onProgress: (progress: DownloadProgress) => void): Promise<string> {
  const release = await getLatestRelease()
  if (release == null) {
    throw new Error('No compatible installer asset found in the latest release')
  }
  const destination = join(updatesDirectory(), release.assetName)
  if (existsSync(destination)) {
    onProgress({ percent: 100, receivedBytes: release.assetSize, totalBytes: release.assetSize, done: true })
    return destination
  }
  return downloadToFile(release.assetUrl, destination, onProgress)
}

/**
 * Launches the installer and exits the app: NSIS silently on Windows
 * (Electron /SILENT /RESTARTAPPLICATIONS), `open` on macOS (mounts the dmg),
 * direct spawn on Linux (AppImage needs the executable bit first).
 */
export async function launchInstaller(installerPath: string): Promise<{ ok: boolean }> {
  return new Promise((resolve) => {
    let child: ReturnType<typeof spawn>
    if (process.platform === 'darwin') {
      child = spawn('open', [installerPath], {
        detached: true,
        stdio: 'ignore'
      })
    } else if (process.platform === 'linux') {
      // Downloaded files lose the executable bit; AppImage refuses to run
      // without it. Failures here surface through the spawn error below.
      try {
        chmodSync(installerPath, 0o755)
      } catch {
        // ignore — the spawn error carries the real reason
      }
      child = spawn(installerPath, [], {
        detached: true,
        stdio: 'ignore'
      })
    } else {
      child = spawn(installerPath, ['/SILENT', '/RESTARTAPPLICATIONS'], {
        detached: true,
        stdio: 'ignore',
        windowsHide: false
      })
    }
    child.on('error', () => resolve({ ok: false }))
    child.on('spawn', () => {
      resolve({ ok: true })
      app.quit()
    })
  })
}
