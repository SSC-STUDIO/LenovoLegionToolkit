/**
 * Window maximize/resize stability helpers — port of WPF
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
