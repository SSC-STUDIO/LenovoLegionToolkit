/**
 * Power mode metadata — port of PowerModeStateExtensions:
 * per-state accent color used by the WPF client
 * (Quiet #357BF2, Balance white, Performance #D43333, Extreme #FF8C00, GodMode #6334E3).
 */

export const POWER_MODE_COLORS: Record<string, string> = {
  Quiet: '#357bf2',
  Balance: '#ffffff',
  Performance: '#d43333',
  Extreme: '#ff8c00',
  GodMode: '#6334e3'
}

/**
 * Accent color for a power mode state name ("Performance" | "performance").
 * Returns undefined for unknown states (WPF falls back to Transparent).
 */
export function powerModeColor(state: string | undefined | null): string | undefined {
  if (!state) return undefined
  return POWER_MODE_COLORS[state] ?? POWER_MODE_COLORS[state.charAt(0).toUpperCase() + state.slice(1)]
}
