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

const bridge = window.bridge

/** Typed invoke wrapper over the preload bridge. */
export async function invoke<T = JsonValue>(method: string, params: JsonValue = {}): Promise<T> {
  if (!bridge) {
    throw new Error('Bridge is not available')
  }
  const result = (await bridge.invoke(method, params)) as T
  return result
}

/** Subscribe to a host event; returns an unsubscribe function. */
export function on<T = JsonValue>(event: string, callback: (data: T) => void): () => void {
  if (!bridge) {
    return () => undefined
  }
  return bridge.on(event, callback as (data: unknown) => void)
}
