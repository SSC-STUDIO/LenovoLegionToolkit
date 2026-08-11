/**
 * Mirrors WPF SmartKeyHelper pure logic: Fn+F9 single/double press
 * disambiguation and the smart-key action rotation.
 *
 * The hardware key listener itself (SpecialKeyListener / FnKeysDisabler) is
 * host-side; this module provides the renderer with the identical timing and
 * rotation semantics for anything that must reproduce the WPF behavior.
 */

export const SMART_KEY_DOUBLE_PRESS_INTERVAL_MS = 500

export type SmartKeyPressResult = 'single' | 'double'

/**
 * Mirrors SmartKeyHelper.SpecialKeyListener_Changed: a second press within
 * the interval is a double press (fired immediately); otherwise the single
 * press fires after the interval elapses so a following press can supersede
 * it.
 */
export class SmartKeyPressDetector {
  private lastPressAt = 0
  private pendingSingle: ReturnType<typeof setTimeout> | null = null

  press(onSingle: () => void, onDouble: () => void): SmartKeyPressResult {
    const now = Date.now()
    const diff = now - this.lastPressAt
    this.lastPressAt = now

    if (this.pendingSingle !== null) {
      clearTimeout(this.pendingSingle)
      this.pendingSingle = null
    }

    if (diff < SMART_KEY_DOUBLE_PRESS_INTERVAL_MS) {
      onDouble()
      return 'double'
    }

    this.pendingSingle = setTimeout(() => {
      this.pendingSingle = null
      onSingle()
    }, SMART_KEY_DOUBLE_PRESS_INTERVAL_MS)
    return 'single'
  }

  dispose(): void {
    if (this.pendingSingle !== null) {
      clearTimeout(this.pendingSingle)
      this.pendingSingle = null
    }
  }
}

export type SmartKeyRotation =
  /** No action configured: bring the app to the foreground. */
  | { kind: 'foreground' }
  /** Empty action id: do nothing. */
  | { kind: 'none' }
  /** Run the current action and advance the list pointer to the next one. */
  | { kind: 'run'; actionToRun: string; nextActionId: string; nextActionList: string[] }

/**
 * Mirrors SmartKeyHelper.ProcessSpecialKey rotation:
 * - null action id → foreground;
 * - empty action id → no-op;
 * - empty action list → treat the current id as the only entry;
 * - otherwise run the entry at the current index and persist the next index.
 */
export function rotateSmartKeyAction(
  actionList: string[],
  actionId: string | null | undefined
): SmartKeyRotation {
  if (actionId === null || actionId === undefined) return { kind: 'foreground' }
  if (actionId === '') return { kind: 'none' }

  let list = actionList
  if (list.length === 0) list = [actionId]

  const currentIndex = Math.max(0, list.indexOf(actionId))
  const nextIndex = (currentIndex + 1) % list.length
  const actionToRun = list[currentIndex]

  return { kind: 'run', actionToRun, nextActionId: list[nextIndex], nextActionList: list }
}
