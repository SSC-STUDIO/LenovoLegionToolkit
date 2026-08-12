/**
 * Animation tokens — port of Electron Styles/Animations.xaml + AnimationTokens.xaml
 * (exposed as CSS classes in global.css; constants kept for JS-driven timing).
 */
export const Animations = {
  pageEnterDurationMs: 200,
  pageEnterOffsetPx: 6,
  cardHoverOffsetPx: 2,
  cardHoverShadowLift: 4,
  buttonPressScale: 0.95,
  shimmerDurationMs: 1700,
} as const

export const PAGE_ENTER_CLASS = 'udt-page-enter'
export const CARD_HOVER_CLASS = 'udt-card--hoverable'

/** Returns the CSS transition string for a hover-lift card. */
export function cardHoverTransition(): string {
  return 'transform 0.15s ease, box-shadow 0.15s ease'
}
