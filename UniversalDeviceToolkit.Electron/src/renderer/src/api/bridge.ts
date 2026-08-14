export interface BridgeError {
  code: number
  message: string
}

export interface BridgeResponse<T> {
  result: T
}

export interface BridgeEvent<T> {
  event: string
  data: T
}

export type JsonValue = unknown

export interface HostStatus {
  running: boolean
  ready: boolean
  lastError: string | null
  readyPayload: unknown
}

const bridge = window.bridge

const HOST_ERROR_CODE = /\[UDT:(-?\d+)\]/

/** Strip Electron's verbose ipcRenderer.invoke wrapper from Error.message. */
export function sanitizeBridgeError(error: unknown): string {
  const raw = error instanceof Error ? error.message : String(error)
  return raw
    .replace(/^Error invoking remote method '[^']+':\s*/i, '')
    .replace(/^Error:\s*/i, '')
    .trim()
}

/** JSON-RPC code preserved by the Host client as `[UDT:<code>]`. */
export function parseHostErrorCode(error: unknown): number | null {
  const match = sanitizeBridgeError(error).match(HOST_ERROR_CODE)
  if (match == null) return null
  const code = Number(match[1])
  return Number.isFinite(code) ? code : null
}

export function stripHostErrorPrefix(message: string): string {
  return message.replace(HOST_ERROR_CODE, '').trim()
}

export function isHostUnavailableError(message: string): boolean {
  return /host is not running|host did not become ready|host exited|host spawn failed|host executable not found/i.test(
    message
  )
}

/**
 * Map stable Host codes to UI copy. Unknown codes keep the Host message
 * without the `[UDT:]` prefix.
 */
export function localizeHostError(
  error: unknown,
  translate: (key: string, options?: { defaultValue: string }) => string
): string {
  const raw = sanitizeBridgeError(error)
  const fallback = stripHostErrorPrefix(raw) || raw
  const code = parseHostErrorCode(raw)
  switch (code) {
    case -1006:
      return translate('optimization.elevationRequired', { defaultValue: fallback })
    case -1010:
      return translate('optimization.network.proxyMissing', { defaultValue: fallback })
    case -1011:
      return translate('optimization.network.hostsModeRefused', { defaultValue: fallback })
    case -1012:
      return translate('optimization.network.startRefused', { defaultValue: fallback })
    case -32099:
      return translate('common.notSupportedOnPlatform', { defaultValue: fallback })
    default:
      return fallback
  }
}

/** Typed invoke wrapper over the preload bridge. */
export async function invoke<T = JsonValue>(method: string, params: JsonValue = {}): Promise<T> {
  if (!bridge) {
    throw new Error('Bridge is not available')
  }
  try {
    const result = (await bridge.invoke(method, params)) as T
    return result
  } catch (error) {
    throw new Error(sanitizeBridgeError(error))
  }
}

/** Subscribe to a host event; returns an unsubscribe function. */
export function on<T = JsonValue>(event: string, callback: (data: T) => void): () => void {
  if (!bridge) {
    return () => undefined
  }
  return bridge.on(event, callback as (data: unknown) => void)
}

export async function getHostStatus(): Promise<HostStatus> {
  if (!bridge?.getHostStatus) {
    return { running: false, ready: false, lastError: 'Bridge is not available', readyPayload: null }
  }
  return bridge.getHostStatus()
}

/**
 * Wait until the Host has published {@code host.ready} (or is already ready).
 * Prefers the synchronous status snapshot so a fast Host boot is not missed.
 */
export async function waitForHostReady(timeoutMs = 45000): Promise<void> {
  const status = await getHostStatus().catch(() => null)
  if (status?.ready) return
  if (status?.lastError && !status.running) {
    throw new Error(status.lastError)
  }

  await new Promise<void>((resolve, reject) => {
    let settled = false
    const timer = window.setTimeout(() => {
      finish(() =>
        reject(new Error(status?.lastError ?? 'Host did not become ready in time'))
      )
    }, timeoutMs)

    const offReady = on('host.ready', () => {
      finish(() => resolve())
    })
    const offError = on<{ message?: string; fatal?: boolean }>('host.error', (data) => {
      if (data?.fatal) {
        finish(() => reject(new Error(data.message ?? 'Host failed to start')))
      }
    })

    const finish = (action: () => void): void => {
      if (settled) return
      settled = true
      window.clearTimeout(timer)
      offReady()
      offError()
      action()
    }

    // Re-check after subscribing to close the race with a just-emitted host.ready.
    void getHostStatus()
      .then((latest) => {
        if (latest.ready) finish(() => resolve())
        else if (latest.lastError && !latest.running) {
          finish(() => reject(new Error(latest.lastError ?? 'Host failed to start')))
        }
      })
      .catch(() => undefined)
  })
}
