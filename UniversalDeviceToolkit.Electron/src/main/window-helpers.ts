/**
 * Window maximize/resize stability helpers — port of Electron
 * Utils/WindowMaximizeWorkAreaHelper.cs + Utils/WindowResizeStabilityHelper.cs.
 *
 * Electron maximizes within the monitor work area natively, and Chromium
 * composites the client area during live resize without re-measuring every
 * frame; these helpers keep the *same contract* (work-area clamp + a
 * live-resize flag) for code that wants to skip heavy layout during a drag.
 */
import { BrowserWindow, screen } from 'electron'

/** True while the user is in a live move/size loop for this window. */
export function isLiveResizing(window: BrowserWindow): boolean {
  return liveResizingWindows.has(window.id)
}

const liveResizingWindows = new Set<number>()

/** Clamps a candidate bounds rect to the work area of the display it overlaps. */
export function constrainToWorkArea(rect: { x: number; y: number; width: number; height: number }): {
  x: number
  y: number
  width: number
  height: number
} {
  const display = screen.getDisplayMatching(rect)
  const area = display.workArea
  return {
    x: Math.max(area.x, Math.min(rect.x, area.x + area.width - Math.min(rect.width, area.width))),
    y: Math.max(area.y, Math.min(rect.y, area.y + area.height - Math.min(rect.height, area.height))),
    width: Math.min(rect.width, area.width),
    height: Math.min(rect.height, area.height),
  }
}

/** Attaches the live-resize tracking hooks. */
export function attachResizeStability(window: BrowserWindow): void {
  window.on('resize', () => {
    liveResizingWindows.add(window.id)
  })
  window.on('move', () => {
    liveResizingWindows.add(window.id)
  })
  const clear = (): void => {
    liveResizingWindows.delete(window.id)
  }
  window.on('resized', clear)
  window.on('moved', clear)
  window.on('closed', clear)
}

/**
 * Safety net for Electron WindowMaximizeWorkAreaHelper: Electron maximizes within the
 * monitor work area natively, but desktop-dock overlays (MyDockFinder etc.) can
 * still report a full-monitor maximize area that covers the taskbar/dock. Only
 * when the maximized bounds actually exceed the work area do we snap them back
 * with setBounds — the normal case leaves the native maximize animation alone.
 */
export function attachMaximizeWorkAreaClamp(window: BrowserWindow): void {
  const clamp = (): void => {
    if (window.isDestroyed() || !window.isMaximized() || window.isFullScreen()) return
    const bounds = window.getBounds()
    const display = screen.getDisplayNearestPoint({
      x: Math.round(bounds.x + bounds.width / 2),
      y: Math.round(bounds.y + bounds.height / 2)
    })
    const area = display.workArea
    const exceeds =
      bounds.x < area.x ||
      bounds.y < area.y ||
      bounds.width > area.width ||
      bounds.height > area.height
    if (!exceeds) return
    window.setBounds({ x: area.x, y: area.y, width: area.width, height: area.height })
  }
  // Measure after the native maximize has settled to avoid fighting the animation.
  window.on('maximize', () => setTimeout(clamp, 60))
}
