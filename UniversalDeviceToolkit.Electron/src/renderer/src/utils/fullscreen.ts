/**
 * Fullscreen detection helpers — port of Electron Utils/FullscreenHelper.cs.
 * The other-app fullscreen probe (Win32 GetForegroundWindow) runs host-side;
 * this module exposes the renderer-observable parts.
 */

let latestFullscreen = false
let queryGeneration = 0
let inflight: Promise<void> | null = null

function applyFullscreen(value: boolean, token: number): void {
  if (token !== queryGeneration) return
  latestFullscreen = value
}

function refreshFullscreenSnapshot(): void {
  const bridge = window.bridge
  if (bridge == null || bridge.isFullscreen == null) return
  if (inflight != null) return
  const token = ++queryGeneration
  inflight = bridge
    .isFullscreen()
    .then((value) => {
      applyFullscreen(value, token)
    })
    .catch(() => {
      applyFullscreen(false, token)
    })
    .finally(() => {
      if (token === queryGeneration) inflight = null
    })
}

export function isWindowFullscreen(): boolean {
  refreshFullscreenSnapshot()
  return latestFullscreen
}

/** Subscribes to main-process fullscreen changes; returns an unsubscribe fn. */
export function onFullscreenChanged(callback: (fullscreen: boolean) => void): () => void {
  const bridge = window.bridge
  if (bridge == null || bridge.onFullscreenChanged == null) return () => undefined
  refreshFullscreenSnapshot()
  return bridge.onFullscreenChanged((value) => {
    latestFullscreen = value
    callback(value)
  })
}
