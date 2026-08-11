/**
 * Fullscreen detection helpers — port of WPF Utils/FullscreenHelper.cs.
 * The other-app fullscreen probe (Win32 GetForegroundWindow) runs host-side;
 * this module exposes the renderer-observable parts.
 */

export function isWindowFullscreen(): boolean {
  const bridge = window.bridge
  if (bridge == null || bridge.isFullscreen == null) return false
  void bridge.isFullscreen().then((value) => {
    latestFullscreen = value
  })
  return latestFullscreen
}

let latestFullscreen = false

/** Subscribes to main-process fullscreen changes; returns an unsubscribe fn. */
export function onFullscreenChanged(callback: (fullscreen: boolean) => void): () => void {
  const bridge = window.bridge
  if (bridge == null || bridge.onFullscreenChanged == null) return () => undefined
  return bridge.onFullscreenChanged((value) => {
    latestFullscreen = value
    callback(value)
  })
}
