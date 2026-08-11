/**
 * UI text size scaling — port of WPF Utils/AppTextSizeManager.cs and
 * Utils/DpiAwareTypography.cs (UserScale + AppScale).
 */

export type AppTextSize = 'Default' | 'Compact' | 'Large' | 'ExtraLarge'

const TEXT_SCALES: Record<AppTextSize, number> = {
  Default: 1.0,
  Compact: 0.9,
  Large: 1.1,
  ExtraLarge: 1.25,
}

export function textSizeScale(size: AppTextSize): number {
  return TEXT_SCALES[size] ?? 1.0
}

export type AppScale = number

/** AppScale is a percentage enum (100/125/150...): scale = value / 100. */
export function appScaleValue(scale: AppScale): number {
  return Number.isFinite(scale) && scale > 0 ? scale / 100 : 1.0
}

const SCALE_VAR = '--udt-font-scale'
const APP_SCALE_VAR = '--udt-app-scale'

/** Applies the WPF UserScale semantics by setting CSS variables on the root. */
export function applyTextScales(textSize: AppTextSize, appScale: AppScale = 100): void {
  const root = document.documentElement
  root.style.setProperty(SCALE_VAR, textSizeScale(textSize).toFixed(3))
  root.style.setProperty(APP_SCALE_VAR, appScaleValue(appScale).toFixed(3))
}

export function currentTextScale(): number {
  const raw = document.documentElement.style.getPropertyValue(SCALE_VAR)
  const value = Number(raw)
  return Number.isFinite(value) && value > 0 ? value : 1.0
}

/** Converts a design-px value to the user-scaled size (DpiAwareTypography.UserScale equivalent). */
export function userScaledPx(designPx: number): string {
  return `${(designPx * currentTextScale()).toFixed(2)}px`
}
