/**
 * Skeleton animation tokens — mirrors Electron SkeletonAnimationTokens +
 * SkeletonShimmer brush math. CSS in styles/skeleton.css consumes the same
 * durations/stagger; this module is the TS source of truth for components.
 */

import type { CSSProperties } from 'react'

export const skeletonAnimationTokens = {
  durationSeconds: 1.7,
  sweepFrom: -1.25,
  sweepTo: 1.25,
  staggerStepSeconds: 0.055,
  staggerMaxSeconds: 0.32,
  breathingFloorOpacity: 0.85
} as const

export type SkeletonBoneVariant = 'default' | 'muted' | 'on-card' | 'chart' | 'static'

export type ShimmerDelayStyle = CSSProperties & { '--udt-shimmer-delay'?: string }

/** Mirrors SkeletonShimmer.RestartSubtreeCore automatic delay. */
export function resolveStaggerDelay(index: number): number {
  return Math.min(
    index * skeletonAnimationTokens.staggerStepSeconds,
    skeletonAnimationTokens.staggerMaxSeconds
  )
}

/** Negative delay keeps each bone permanently de-phased (WPF DelaySeconds = -1 pattern). */
export function shimmerDelayStyle(step = 0): ShimmerDelayStyle {
  return { '--udt-shimmer-delay': `${-resolveStaggerDelay(step)}s` }
}

export function skeletonBoneClass(
  variant: SkeletonBoneVariant = 'default',
  extra?: string
): string {
  const parts = ['udt-skeleton']
  if (variant !== 'default') {
    parts.push(`udt-skeleton--${variant}`)
  }
  if (extra) parts.push(extra)
  return parts.join(' ')
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

/** Luminance-aware overlay colors for programmatic shimmer gradients. */
export function resolveShimmerOverlayColors(baseColor: string, isLight: boolean): { start: Rgba; peak: Rgba } {
  const base = parseHexColor(baseColor)
  const luminance = (0.2126 * base.r + 0.7152 * base.g + 0.0722 * base.b) / 255.0
  const useDarkOverlay = isLight ? luminance >= 0.5 : luminance >= 0.58

  if (useDarkOverlay) {
    const edgeAlpha = isLight ? 0x28 / 255 : 0x1c / 255
    const peakAlpha = isLight ? 0x58 / 255 : 0x46 / 255
    return { start: { r: 0, g: 0, b: 0, a: edgeAlpha }, peak: { r: 0, g: 0, b: 0, a: peakAlpha } }
  }

  return { start: { r: 255, g: 255, b: 255, a: 0x1c / 255 }, peak: { r: 255, g: 255, b: 255, a: 0x46 / 255 } }
}

export function compositeOverlay(baseColor: string, overlay: Rgba): Rgb {
  const base = parseHexColor(baseColor)
  if (overlay.a <= 0) return base
  return {
    r: lerpChannel(base.r, overlay.r, overlay.a),
    g: lerpChannel(base.g, overlay.g, overlay.a),
    b: lerpChannel(base.b, overlay.b, overlay.a)
  }
}

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
