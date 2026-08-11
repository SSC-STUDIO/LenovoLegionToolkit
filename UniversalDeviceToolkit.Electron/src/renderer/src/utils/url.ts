/**
 * Safe external URL handling — port of WPF Extensions/UriExtensions.cs.
 * Only HTTP/HTTPS schemes are allowed to prevent command injection.
 */

const DANGEROUS_SCHEME_CHARS = [':', '/', '\\', '*', '?', '"', '<', '>', '|']

const BLOCKED_SCHEME_PREFIXES = [
  'ms-',
  'file',
  'javascript',
  'vbscript',
  'data',
  'about',
  'shell'
]

/**
 * SECURITY: Only allow HTTP and HTTPS schemes, mirroring the WPF allow-list.
 * Rejects file://, ms-* (ms-settings, ms-windows-store) and any custom scheme.
 */
export function isSafeExternalUrl(url: string): boolean {
  let scheme = ''
  try {
    const parsed = new URL(url)
    scheme = parsed.protocol.replace(/:$/, '').toLowerCase()
  } catch {
    return false
  }

  if (!scheme) return false
  if (scheme.split('').some((ch) => DANGEROUS_SCHEME_CHARS.includes(ch))) return false
  if (BLOCKED_SCHEME_PREFIXES.some((prefix) => scheme.startsWith(prefix))) return false
  if (scheme !== 'http' && scheme !== 'https') return false
  return true
}

/**
 * Opens an external URL through the default browser. Only http/https URLs are
 * ever opened; anything else is ignored (returns false).
 */
export function openExternalUrl(url: string): boolean {
  if (!isSafeExternalUrl(url)) return false
  const win = window.open(url, '_blank', 'noopener,noreferrer')
  return win != null
}
