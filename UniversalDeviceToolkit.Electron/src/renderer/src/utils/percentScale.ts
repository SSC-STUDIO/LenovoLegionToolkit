/**
 * Mirrors Electron PercentageToScaleConverter: converts a percentage into a
 * clamped [0, 1] scale factor used for progress fills / gauges.
 */
export function percentToScale(percent: number): number {
  const value = Number.isFinite(percent) ? percent : 0
  return Math.min(1, Math.max(0, value / 100))
}
