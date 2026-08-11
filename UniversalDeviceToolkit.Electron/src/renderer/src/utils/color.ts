/**
 * Color utilities — port of the RGBColor conversion/mix logic used by the WPF
 * client (UniversalDeviceToolkit.Lib RGBColor struct + ColorExtensions).
 * All functions are pure and side-effect free.
 */

export interface RgbColor {
  r: number
  g: number
  b: number
}

export interface HsvColor {
  h: number
  s: number
  v: number
}

const HEX_PATTERN = /^#?[0-9a-f]{6}$/i

function clampByte(value: number): number {
  return Math.min(255, Math.max(0, Math.round(value)))
}

function clampUnit(value: number): number {
  return Math.min(1, Math.max(0, value))
}

/** Parses "#RRGGBB" or "RRGGBB" into an RGB triple. Falls back to black. */
export function hexToRgb(hex: string): RgbColor {
  const normalized = hex.trim().replace(/^#/, '')
  if (!HEX_PATTERN.test(hex.trim()) || normalized.length !== 6) return { r: 0, g: 0, b: 0 }
  return {
    r: parseInt(normalized.slice(0, 2), 16),
    g: parseInt(normalized.slice(2, 4), 16),
    b: parseInt(normalized.slice(4, 6), 16)
  }
}

/** Converts an RGB triple to "#RRGGBB". */
export function rgbToHex(rgb: RgbColor): string {
  const toHex = (v: number): string => clampByte(v).toString(16).padStart(2, '0')
  return `#${toHex(rgb.r)}${toHex(rgb.g)}${toHex(rgb.b)}`
}

/** Converts an RGB triple to HSV (h in degrees 0-360, s/v in 0-100). */
export function rgbToHsv({ r, g, b }: RgbColor): HsvColor {
  const rn = r / 255
  const gn = g / 255
  const bn = b / 255
  const max = Math.max(rn, gn, bn)
  const min = Math.min(rn, gn, bn)
  const delta = max - min
  let h = 0
  if (delta !== 0) {
    if (max === rn) h = 60 * (((gn - bn) / delta) % 6)
    else if (max === gn) h = 60 * ((bn - rn) / delta + 2)
    else h = 60 * ((rn - gn) / delta + 4)
  }
  if (h < 0) h += 360
  return { h, s: max === 0 ? 0 : (delta / max) * 100, v: max * 100 }
}

/** Converts an HSV triple (h 0-360, s/v 0-100) to RGB. */
export function hsvToRgb({ h, s, v }: HsvColor): RgbColor {
  const hh = ((h % 360) + 360) % 360
  const c = (v / 100) * (s / 100)
  const x = c * (1 - Math.abs(((hh / 60) % 2) - 1))
  const m = v / 100 - c
  let r = 0
  let g = 0
  let b = 0
  if (hh < 60) {
    r = c
    g = x
  } else if (hh < 120) {
    r = x
    g = c
  } else if (hh < 180) {
    g = c
    b = x
  } else if (hh < 240) {
    g = x
    b = c
  } else if (hh < 300) {
    r = x
    b = c
  } else {
    r = c
    b = x
  }
  return { r: (r + m) * 255, g: (g + m) * 255, b: (b + m) * 255 }
}

/** Converts HSV to "#RRGGBB". */
export function hsvToHex(hsv: HsvColor): string {
  return rgbToHex(hsvToRgb(hsv))
}

/** Converts "#RRGGBB" to HSV. */
export function hexToHsv(hex: string): HsvColor {
  return rgbToHsv(hexToRgb(hex))
}

/**
 * Adjusts the brightness (value channel) of a color.
 * @param delta factor in -1..1: positive lightens toward white, negative darkens toward black.
 */
export function adjustBrightness(color: RgbColor, delta: number): RgbColor {
  const amount = clampUnit(Math.abs(delta))
  if (delta >= 0) {
    const target = 255
    return {
      r: color.r + (target - color.r) * amount,
      g: color.g + (target - color.g) * amount,
      b: color.b + (target - color.b) * amount
    }
  }
  return {
    r: color.r * (1 - amount),
    g: color.g * (1 - amount),
    b: color.b * (1 - amount)
  }
}

/**
 * Mixes two colors.
 * @param amount 0 = entirely `a`, 1 = entirely `b`.
 */
export function mix(a: RgbColor, b: RgbColor, amount: number): RgbColor {
  const t = clampUnit(amount)
  return {
    r: a.r + (b.r - a.r) * t,
    g: a.g + (b.g - a.g) * t,
    b: a.b + (b.b - a.b) * t
  }
}

/** Relative luminance (WCAG), 0..1 — used to pick readable text on a background. */
export function relativeLuminance({ r, g, b }: RgbColor): number {
  const linear = (v: number): number => {
    const c = v / 255
    return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4)
  }
  return 0.2126 * linear(r) + 0.7152 * linear(g) + 0.0722 * linear(b)
}

/** Returns "#FFFFFF" or "#000000" depending on which contrasts better with the given color. */
export function contrastingText(color: RgbColor): string {
  return relativeLuminance(color) > 0.5 ? '#000000' : '#ffffff'
}
