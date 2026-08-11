/**
 * Clipboard process-list API (port of WPF ClipboardExtensions). Unlike
 * api/bridge.ts — which routes to the host JSON-RPC process — these channels
 * are served by the Electron main process, so they go through the dedicated
 * preload bridge methods instead of bridge.invoke.
 */

const bridge = window.bridge

/** Port of WPF ClipboardExtensions.SetProcesses: write one path per line. */
export async function writeLines(lines: string[]): Promise<{ ok: boolean }> {
  if (!bridge?.writeClipboardLines) {
    throw new Error('Bridge is not available')
  }
  return bridge.writeClipboardLines(lines)
}

/** Port of WPF ClipboardExtensions.GetProcesses: existing paths, deduplicated. */
export async function readExistingPaths(): Promise<string[]> {
  if (!bridge?.readClipboardExistingPaths) {
    throw new Error('Bridge is not available')
  }
  return bridge.readClipboardExistingPaths()
}

export const clipboardApi = {
  writeLines,
  readExistingPaths
}
