/**
 * DPI-aware typography — port of WPF Utils/DpiAwareTypography.cs.
 * Chromium already applies the Windows display scale to CSS px, so the
 * user-scale component is the only multiplier needed here.
 */

export function devicePixelRatio(): number {
  return typeof window !== 'undefined' && window.devicePixelRatio > 0
    ? window.devicePixelRatio
    : 1.0
}

/** CSS px per physical pixel at the current DPI (96 DIP baseline). */
export function cssPxPerPhysicalPixel(): number {
  return devicePixelRatio()
}

/** Design px (WPF DIPs at 100% scale) → CSS px for the current display. */
export function dipToCssPx(dip: number): number {
  return dip
}

/** WPF UserScale-adjusted size for a design-px token. */
export function userScaled(dip: number, userScale: number): number {
  return dip * userScale
}
