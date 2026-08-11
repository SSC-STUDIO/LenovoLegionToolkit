/**
 * Mirrors WPF PluginIconResolver pure logic: monogram creation and
 * symbol-vs-monogram icon kind resolution.
 *
 * Filesystem-backed resolution (icon.png / plugin.png / logo.png discovery
 * next to the plugin metadata) is performed host-side; the host projects the
 * result into `PluginView.icon` / `PluginView.iconBackground`.
 */

export type PluginIconKind = 'symbol' | 'image' | 'monogram'

export interface PluginIconDescriptor {
  kind: PluginIconKind
  /** Fallback symbol name when no image/monogram is available. */
  symbol: string
  /** Resolved image path (host-side only; undefined in the renderer). */
  imagePath?: string
  monogram: string
}

/** Mirrors PathSecurity.IsValidFileName. */
export function isValidPluginId(pluginId: string): boolean {
  if (pluginId.length === 0 || pluginId === '.' || pluginId === '..') return false
  return !/[<>:"/\\|?*\u0000-\u001f]/u.test(pluginId)
}

export function normalizePluginId(pluginId: string): string {
  const trimmed = pluginId.trim()
  return isValidPluginId(trimmed) ? trimmed : 'plugin'
}

/**
 * Mirrors PluginIconResolver.CreateMonogram:
 * - source is the plugin name when present, otherwise the (normalized) id;
 * - tokens are split on non-alphanumeric separators;
 * - two tokens whose first token is short (<= 2 chars) or starts with a digit
 *   combine their first letters;
 * - otherwise the first two letters of the first token are used.
 */
export function createMonogram(pluginName: string | null | undefined, pluginId: string): string {
  const source = pluginName !== null && pluginName !== undefined && pluginName.trim().length > 0
    ? pluginName
    : pluginId
  const separators = [...new Set([...source].filter((ch) => !/[0-9a-zA-Z]/u.test(ch)))]
  const tokens = source
    .split(separators.length > 0 ? new RegExp(`[${escapeRegExp(separators.join(''))}]`) : /(?!x)x/u)
    .map((token) => token.trim())
    .filter((token) => token.length > 0)

  if (tokens.length === 0) return 'P'

  if (tokens.length > 1 && (tokens[0].length <= 2 || /[0-9]/.test(tokens[0][0] ?? ''))) {
    return `${tokens[0][0] ?? ''}${tokens[1][0] ?? ''}`.toUpperCase()
  }

  return tokens[0].slice(0, 2).toUpperCase()
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')
}

/**
 * Mirrors PluginIconResolver.Resolve for the non-filesystem path: when the
 * icon value is a recognized symbol name the descriptor is a symbol,
 * otherwise the descriptor falls back to the monogram.
 */
export function resolvePluginIconDescriptor(
  pluginId: string,
  pluginName: string | null | undefined,
  iconValue: string | null | undefined
): PluginIconDescriptor {
  const safePluginId = normalizePluginId(pluginId)
  const monogram = createMonogram(pluginName, safePluginId)
  const symbol = iconValue !== null && iconValue !== undefined && iconValue.trim().length > 0
    ? iconValue.trim()
    : 'apps'

  return monogram.length > 0
    ? { kind: 'monogram', symbol, monogram }
    : { kind: 'symbol', symbol, monogram: 'P' }
}
