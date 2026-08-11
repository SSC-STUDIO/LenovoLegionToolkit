/**
 * Responsive breakpoints — port of WPF Utils/LayoutBreakpoints.cs.
 */
export const LayoutBreakpoints = {
  windowMinWidth: 1024,
  windowMinHeight: 640,
  dashboardUltraWide: 2000,
  dashboardWide: 1500,
  dashboardStandard: 1000,
  sensorsUltraWide: 2000,
  sensorsWide: 1500,
  sensorsStandard: 900,
  navigationDesignWidth: 1300,
  navigationMinContentWidth: 700,
  navigationMaxExpandedWidth: 420,
  progressBarUltraWideMax: 400,
  progressBarWideMax: 320,
  progressBarStandardMax: 260,
  progressBarCompactMax: 260,
} as const

export type DashboardColumnCount = 1 | 2 | 3

/** Dashboard column layout for a given viewport width (below standard = 1 column). */
export function dashboardColumns(width: number): DashboardColumnCount {
  if (width >= LayoutBreakpoints.dashboardUltraWide) return 3
  if (width >= LayoutBreakpoints.dashboardWide) return 3
  if (width >= LayoutBreakpoints.dashboardStandard) return 2
  return 1
}

/** Sensor layout tier for a given viewport width. */
export type SensorLayoutTier = 'compact' | 'standard' | 'wide' | 'ultraWide'

export function sensorLayoutTier(width: number): SensorLayoutTier {
  if (width >= LayoutBreakpoints.sensorsUltraWide) return 'ultraWide'
  if (width >= LayoutBreakpoints.sensorsWide) return 'wide'
  if (width >= LayoutBreakpoints.sensorsStandard) return 'standard'
  return 'compact'
}
