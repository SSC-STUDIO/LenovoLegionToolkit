/**
 * Mirrors WPF SkeletonAnimationTokens + SkeletonShimmer brush math.
 *
 * The Electron skeleton blocks are rendered with CSS (`.udt-skeleton` /
 * `--udt-anim-shimmer`), but the WPF tuning values and the overlay-color
 * compositing are kept here as the single source of truth for anything that
 * needs to reproduce the exact sweep (staggered shimmer, custom bones).
 */

export const skeletonAnimationTokens = {
  /** Classic 4.x-style calm cycle: long enough to look fluid, not a hard wipe. */
  durationSeconds: 1.7,
  sweepFrom: -1.25,
  sweepTo: 1.25,
  /** Wider stagger than the ultra-tight wave — fewer borders peak on the same frame. */
  staggerStepSeconds: 0.055,
  staggerMaxSeconds: 0.32,
  /** Keep the bone visible while the contrast band moves across it in either theme. */
  breathingFloorOpacity: 0.85
} as const

/**
 * Mirrors SkeletonShimmer.RestartSubtreeCore automatic delay:
 * index * staggerStep, capped at staggerMax.
 */
export function resolveStaggerDelay(index: number): number {
  return Math.min(index * skeletonAnimationTokens.staggerStepSeconds, skeletonAnimationTokens.staggerMaxSeconds)
}

export interface Rgba {
  r: number
  g: number
  b: number
  a: number
}

function parseHexColor(color: string): Rgb {
  const match = /^#?([0-9a-f]{6})$/i.exec(color.trim())
  if (!match) return { r: 0x80, g: 0x80, b: 0x80 }
  const value = parseInt(match[1], 16)
  return { r: (value >> 16) & 0xff, g: (value >> 8) & 0xff, b: value & 0xff }
}

interface Rgb {
  r: number
  g: number
  b: number
}

function lerpChannel(from: number, to: number, amount: number): number {
  return Math.min(255, Math.max(0, Math.round(from + (to - from) * amount)))
}

function toHex(color: Rgb): string {
  return `#${[color.r, color.g, color.b].map((channel) => channel.toString(16).padStart(2, '0')).join('')}`
}

/**
 * Mirrors SkeletonShimmer.ResolveShimmerOverlayColors: contrast is resolved
 * against the actual surface color (not the theme), so custom accent and
 * high-contrast surfaces stay readable.
 */
export function resolveShimmerOverlayColors(baseColor: string, isLight: boolean): { start: Rgba; peak: Rgba } {
  const base = parseHexColor(baseColor)
  const luminance = (0.2126 * base.r + 0.7152 * base.g + 0.0722 * base.b) / 255.0
  const useDarkOverlay = isLight ? luminance >= 0.5 : luminance >= 0.58

  if (useDarkOverlay) {
    // Dark overlay (for light backgrounds) — enhanced contrast in light mode.
    const edgeAlpha = isLight ? 0x28 / 255 : 0x1c / 255
    const peakAlpha = isLight ? 0x58 / 255 : 0x46 / 255
    return { start: { r: 0, g: 0, b: 0, a: edgeAlpha }, peak: { r: 0, g: 0, b: 0, a: peakAlpha } }
  }

  // Light overlay (for dark backgrounds).
  return { start: { r: 255, g: 255, b: 255, a: 0x1c / 255 }, peak: { r: 255, g: 255, b: 255, a: 0x46 / 255 } }
}

/** Mirrors SkeletonShimmer.CompositeOverlay: alpha-blends an overlay onto a base color. */
export function compositeOverlay(baseColor: string, overlay: Rgba): Rgb {
  const base = parseHexColor(baseColor)
  if (overlay.a <= 0) return base
  return {
    r: lerpChannel(base.r, overlay.r, overlay.a),
    g: lerpChannel(base.g, overlay.g, overlay.a),
    b: lerpChannel(base.b, overlay.b, overlay.a)
  }
}

/**
 * Mirrors SkeletonShimmer.CreateShimmerBrush gradient stops: a wide
 * soft-shoulder band (peak @ 48%) reading as a smooth wave rather than a
 * narrow stripe. Returns a `linear-gradient(90deg, ...)` CSS value.
 */
export function createShimmerGradient(baseColor: string, isLight: boolean): string {
  const overlay = resolveShimmerOverlayColors(baseColor, isLight)
  const edge = toHex(compositeOverlay(baseColor, overlay.start))
  const peak = toHex(compositeOverlay(baseColor, overlay.peak))

  const stops = [
    `${edge} 0%`,
    `${edge} 14%`,
    `${edge} 30%`,
    `${peak} 48%`,
    `${edge} 66%`,
    `${edge} 84%`,
    `${edge} 100%`
  ]
  return `linear-gradient(90deg, ${stops.join(', ')})`
}
