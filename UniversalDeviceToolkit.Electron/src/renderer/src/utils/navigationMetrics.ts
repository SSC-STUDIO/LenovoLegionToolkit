/**
 * Navigation pane metrics — port of WPF Utils/NavigationPaneMetrics.cs.
 */
export const NAVIGATION_DESIGN_WIDTH = 1300
export const NAVIGATION_MIN_CONTENT_WIDTH = 700
export const NAVIGATION_MAX_EXPANDED_WIDTH = 420
export const NAVIGATION_COLLAPSED_WIDTH = 70
export const NAVIGATION_PREFERRED_EXPANDED_WIDTH = 220

export function getCollapsedWidth(): number {
  return NAVIGATION_COLLAPSED_WIDTH
}

export function getPreferredExpandedWidth(): number {
  return NAVIGATION_PREFERRED_EXPANDED_WIDTH
}

/** Max width the rail may occupy for the given window width. */
export function getMaxStretchWidth(windowWidth: number): number {
  const preferred = NAVIGATION_PREFERRED_EXPANDED_WIDTH
  if (windowWidth <= 0 || !Number.isFinite(windowWidth)) return preferred

  const scaled = preferred * (windowWidth / NAVIGATION_DESIGN_WIDTH)
  const contentBudget = Math.max(preferred, windowWidth - NAVIGATION_MIN_CONTENT_WIDTH)
  const ratioCap = windowWidth * 0.28
  const upper = Math.min(
    NAVIGATION_MAX_EXPANDED_WIDTH,
    Math.min(contentBudget, Math.max(preferred, ratioCap))
  )
  return Math.min(Math.max(scaled, preferred), upper)
}
