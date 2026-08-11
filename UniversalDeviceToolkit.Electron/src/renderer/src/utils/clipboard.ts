import { clipboardApi } from '../api/clipboard'

/** Port of WPF ClipboardExtensions.SetProcesses: copy one executable path per line. */
export async function copyLines(lines: string[]): Promise<boolean> {
  const result = await clipboardApi.writeLines(lines)
  return result.ok === true
}

/** Port of WPF ClipboardExtensions.GetProcesses: existing paths only, deduplicated. */
export async function readProcessPaths(): Promise<string[]> {
  return clipboardApi.readExistingPaths()
}
