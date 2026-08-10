export interface Bridge {
  invoke: (method: string, params?: unknown) => Promise<unknown>
  on: (event: string, callback: (data: unknown) => void) => () => void
}

declare global {
  interface Window {
    bridge?: Bridge
  }
}

export {}
