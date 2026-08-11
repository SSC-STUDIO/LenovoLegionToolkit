/**
 * Main-thread dispatch helpers — port of WPF Utils/MainThreadDispatcher.cs.
 * In the renderer, the React/Scheduler main thread is the UI thread;
 * these wrappers keep the call site contract identical.
 */

export function dispatch(callback: () => void): void {
  if (typeof window !== 'undefined' && typeof requestAnimationFrame === 'function') {
    requestAnimationFrame(() => callback())
  } else {
    queueMicrotask(() => callback())
  }
}

export function dispatchAsync(callback: () => Promise<void>): Promise<void> {
  return new Promise((resolve, reject) => {
    dispatch(() => {
      callback().then(resolve, reject)
    })
  })
}

/** WPF Dispatcher.InvokeAsync equivalent: runs after the current frame. */
export function invokeAsync(callback: () => void): Promise<void> {
  return dispatchAsync(async () => callback())
}
