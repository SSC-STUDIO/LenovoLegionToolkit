const WINDOWS_ABSOLUTE_PATH = /^([a-zA-Z]):\/(.*)$/
const URI_SCHEME = /^[a-zA-Z][a-zA-Z0-9+.-]*:/
const SAFE_UNC_HOST = /^[a-zA-Z0-9._-]+$/

export interface PluginWebviewEventTarget {
  addEventListener: (type: string, listener: () => void) => void
  removeEventListener: (type: string, listener: () => void) => void
}

function containsControlCharacter(value: string): boolean {
  for (let index = 0; index < value.length; index += 1) {
    const code = value.charCodeAt(index)
    if (code <= 0x1f || code === 0x7f) return true
  }
  return false
}

function encodedPathSegments(segments: readonly string[]): string[] | null {
  const encoded: string[] = []
  for (const segment of segments) {
    if (segment.length === 0 || segment === '.') continue
    if (segment === '..') return null
    encoded.push(encodeURIComponent(segment))
  }
  return encoded
}

export function fileUrlFromAbsolutePath(path: string): string | null {
  if (path.length === 0 || containsControlCharacter(path)) return null
  const normalized = path.replace(/\\/g, '/')

  const windowsPath = normalized.match(WINDOWS_ABSOLUTE_PATH)
  if (windowsPath != null) {
    const segments = encodedPathSegments(windowsPath[2].split('/'))
    if (segments == null) return null
    const suffix = segments.length > 0 ? segments.join('/') : ''
    return `file:///${windowsPath[1].toUpperCase()}:/${suffix}`
  }

  if (normalized.startsWith('//')) {
    const [host = '', ...pathSegments] = normalized.slice(2).split('/')
    if (!SAFE_UNC_HOST.test(host)) return null
    const segments = encodedPathSegments(pathSegments)
    if (segments == null) return null
    return `file://${host}/${segments.join('/')}`
  }

  if (normalized.startsWith('/')) {
    const segments = encodedPathSegments(normalized.slice(1).split('/'))
    if (segments == null) return null
    return `file:///${segments.join('/')}`
  }

  return null
}

function encodedRelativePath(path: string): string | null {
  if (path.length === 0 || containsControlCharacter(path)) return null
  const normalized = path.replace(/\\/g, '/')
  if (normalized.startsWith('/') || URI_SCHEME.test(normalized)) return null
  const segments = encodedPathSegments(normalized.split('/'))
  if (segments == null || segments.length === 0) return null
  return segments.join('/')
}

export function buildPluginPageSource(
  directory: string | null | undefined,
  webPage: string | null | undefined
): string | null {
  if (directory == null || webPage == null) return null
  const directoryUrl = fileUrlFromAbsolutePath(directory)
  const relativePath = encodedRelativePath(webPage)
  if (directoryUrl == null || relativePath == null) return null
  const separator = directoryUrl.endsWith('/') ? '' : '/'
  return `${directoryUrl}${separator}${relativePath}`
}

export function buildPluginPreloadUrl(path: string): string | null {
  return fileUrlFromAbsolutePath(path)
}

function wellFormedPluginId(pluginId: string): string {
  let result = ''
  for (let index = 0; index < pluginId.length; index += 1) {
    const code = pluginId.charCodeAt(index)
    if (code >= 0xd800 && code <= 0xdbff) {
      const next = pluginId.charCodeAt(index + 1)
      if (next >= 0xdc00 && next <= 0xdfff) {
        result += pluginId[index] + pluginId[index + 1]
        index += 1
      } else {
        result += '\ufffd'
      }
    } else if (code >= 0xdc00 && code <= 0xdfff) {
      result += '\ufffd'
    } else {
      result += pluginId[index]
    }
  }
  return result
}

export function buildPluginPartition(pluginId: string): string {
  const encodedId = encodeURIComponent(wellFormedPluginId(pluginId)).replace(
    /[!'()*]/g,
    (character) => `%${character.charCodeAt(0).toString(16).toUpperCase()}`
  )
  return `persist:plugin-${encodedId}`
}

export function bindPluginWebviewListeners(
  webview: PluginWebviewEventTarget,
  onDomReady: () => void,
  onLoadFailed: () => void
): () => void {
  webview.addEventListener('did-fail-load', onLoadFailed)
  webview.addEventListener('dom-ready', onDomReady)
  let active = true

  return () => {
    if (!active) return
    active = false
    webview.removeEventListener('did-fail-load', onLoadFailed)
    webview.removeEventListener('dom-ready', onDomReady)
  }
}
