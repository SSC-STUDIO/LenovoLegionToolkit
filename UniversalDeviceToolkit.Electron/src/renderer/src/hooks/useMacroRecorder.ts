import { useCallback, useEffect, useRef, useState } from 'react'
import type { MacroEvent, MacroRecordingMode } from '../api/macro'

export type MacroRecorderState = 'idle' | 'preparing' | 'recording'

const VK_ESCAPE = 0x1b

/**
 * Mouse button id → MacroEvent.key, mirroring the WPF MacroRecorder
 * ConvertToMacroEvent (1 = left, 2 = right, 3 = middle, xbutton flags << 16).
 */
const MOUSE_BUTTON_KEYS: Record<number, number> = {
  0: 1,
  1: 3,
  2: 2,
  3: 1 << 16,
  4: 2 << 16
}

/**
 * Renderer-side macro recorder — port of the WPF MacroRecorder + the
 * MacroRecordingWindow flow:
 *
 * - "KeyboardMouseMovement" first shows a 3-second "preparing" state
 *   (MacroSequenceControl.RecordAsync) before capturing starts.
 * - While recording, key/mouse input is captured into MacroEvent[]
 *   (delayMs between events, first event 0) and ESC (VK_ESCAPE) stops the
 *   recording with interrupted = true (MacroRecorder.LowLevelKeyboardProc).
 * - The capture phase runs in the renderer, so input is only captured while
 *   the app window is focused — the headless host cannot install global
 *   input hooks (macro.startRecording returns -1005).
 */
export function useMacroRecorder(
  onComplete: (events: MacroEvent[], interrupted: boolean) => void
): {
  state: MacroRecorderState
  start: (mode: MacroRecordingMode) => void
  stop: () => void
  cancel: () => void
} {
  const [state, setState] = useState<MacroRecorderState>('idle')
  const modeRef = useRef<MacroRecordingMode>('Keyboard')
  const eventsRef = useRef<MacroEvent[]>([])
  const lastRef = useRef(0)
  const onCompleteRef = useRef(onComplete)
  const preparingTimer = useRef<number | null>(null)

  useEffect(() => {
    onCompleteRef.current = onComplete
  }, [onComplete])

  const finish = useCallback((interrupted: boolean): void => {
    if (preparingTimer.current !== null) {
      window.clearTimeout(preparingTimer.current)
      preparingTimer.current = null
    }
    const captured = eventsRef.current
    eventsRef.current = []
    setState('idle')
    onCompleteRef.current(captured, interrupted)
  }, [])

  useEffect(() => {
    if (state !== 'recording') return

    const push = (event: Omit<MacroEvent, 'delayMs'>): void => {
      const now = performance.now()
      const delayMs = eventsRef.current.length === 0 ? 0 : now - lastRef.current
      lastRef.current = now
      eventsRef.current = [...eventsRef.current, { ...event, delayMs }]
    }

    const canMouse = modeRef.current !== 'Keyboard'
    const canMove = modeRef.current === 'KeyboardMouseMovement'

    const handleKeyDown = (e: KeyboardEvent): void => {
      if (e.keyCode === VK_ESCAPE) {
        e.preventDefault()
        finish(true)
        return
      }
      if (e.repeat) return
      push({ source: 'Keyboard', direction: 'Down', key: e.keyCode, x: 0, y: 0 })
    }

    const handleKeyUp = (e: KeyboardEvent): void => {
      if (e.keyCode === VK_ESCAPE) return
      push({ source: 'Keyboard', direction: 'Up', key: e.keyCode, x: 0, y: 0 })
    }

    const handleMouseDown = (e: MouseEvent): void => {
      if (!canMouse) return
      const key = MOUSE_BUTTON_KEYS[e.button]
      if (key === undefined) return
      push({ source: 'Mouse', direction: 'Down', key, x: e.clientX, y: e.clientY })
    }

    const handleMouseUp = (e: MouseEvent): void => {
      if (!canMouse) return
      const key = MOUSE_BUTTON_KEYS[e.button]
      if (key === undefined) return
      push({ source: 'Mouse', direction: 'Up', key, x: e.clientX, y: e.clientY })
    }

    const handleWheel = (e: WheelEvent): void => {
      if (!canMouse) return
      if (e.deltaY !== 0) {
        push({
          source: 'Mouse',
          direction: 'Wheel',
          key: Math.round(-e.deltaY),
          x: e.clientX,
          y: e.clientY
        })
      }
      if (e.deltaX !== 0) {
        push({
          source: 'Mouse',
          direction: 'HorizontalWheel',
          key: Math.round(e.deltaX),
          x: e.clientX,
          y: e.clientY
        })
      }
    }

    const handleMouseMove = (e: MouseEvent): void => {
      if (!canMove) return
      push({ source: 'Mouse', direction: 'Move', key: 0, x: e.clientX, y: e.clientY })
    }

    window.addEventListener('keydown', handleKeyDown, true)
    window.addEventListener('keyup', handleKeyUp, true)
    window.addEventListener('mousedown', handleMouseDown, true)
    window.addEventListener('mouseup', handleMouseUp, true)
    window.addEventListener('wheel', handleWheel, true)
    window.addEventListener('mousemove', handleMouseMove, true)
    return () => {
      window.removeEventListener('keydown', handleKeyDown, true)
      window.removeEventListener('keyup', handleKeyUp, true)
      window.removeEventListener('mousedown', handleMouseDown, true)
      window.removeEventListener('mouseup', handleMouseUp, true)
      window.removeEventListener('wheel', handleWheel, true)
      window.removeEventListener('mousemove', handleMouseMove, true)
    }
  }, [state, finish])

  const start = useCallback((mode: MacroRecordingMode): void => {
    modeRef.current = mode
    eventsRef.current = []
    lastRef.current = 0
    if (mode === 'KeyboardMouseMovement') {
      setState('preparing')
      preparingTimer.current = window.setTimeout(() => {
        preparingTimer.current = null
        setState('recording')
      }, 3000)
    } else {
      setState('recording')
    }
  }, [])

  const stop = useCallback((): void => {
    finish(false)
  }, [finish])

  const cancel = useCallback((): void => {
    finish(true)
  }, [finish])

  return { state, start, stop, cancel }
}
