import { screen } from 'electron'

/**
 * Mirrors WPF ScreenHelper: thread-safe snapshots of the connected displays
 * with DPI info, expressed in DIPs.
 *
 * Electron's `screen` module already tracks monitor geometry; `workArea` is
 * reported in DIPs (matching the WPF conversion `workArea * 96 / dpi`) and
 * `scaleFactor` converts back to the physical DPI the WPF side read via
 * GetDpiForMonitor.
 */

export interface ScreenInfo {
  x: number
  y: number
  width: number
  height: number
  dpiX: number
  dpiY: number
  isPrimary: boolean
}

function toScreenInfo(display: Electron.Display, primaryId: number): ScreenInfo {
  const { x, y, width, height } = display.workArea
  const scale = display.scaleFactor > 0 ? display.scaleFactor : 1
  const dpi = Math.round(96 * scale)
  return {
    x,
    y,
    width,
    height,
    dpiX: dpi,
    dpiY: dpi,
    isPrimary: display.id === primaryId
  }
}

/** Mirrors ScreenHelper.GetScreensSnapshot(). */
export function getScreensSnapshot(): ScreenInfo[] {
  try {
    const displays = screen.getAllDisplays()
    const primaryId = screen.getPrimaryDisplay().id
    return displays.map((display) => toScreenInfo(display, primaryId))
  } catch {
    return []
  }
}

/** Mirrors ScreenHelper.PrimaryScreen. */
export function getPrimaryScreen(): ScreenInfo | undefined {
  return getScreensSnapshot().find((item) => item.isPrimary)
}
