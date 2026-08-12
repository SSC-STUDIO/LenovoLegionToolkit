/**
 * App-level API served by the Electron main process (not the host JSON-RPC).
 * Mirrors Electron Application behavior settings (Autorun → login item).
 */

const bridge = window.bridge

export async function setAutorun(enabled: boolean): Promise<{ ok: boolean; enabled: boolean }> {
  if (!bridge?.setAutorun) {
    throw new Error('Bridge is not available')
  }
  return bridge.setAutorun(enabled)
}

export async function getAutorun(): Promise<{ enabled: boolean }> {
  if (!bridge?.getAutorun) {
    throw new Error('Bridge is not available')
  }
  return bridge.getAutorun()
}

export const appApi = {
  setAutorun,
  getAutorun
}
