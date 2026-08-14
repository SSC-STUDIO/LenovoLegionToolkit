/**
 * Shared UI-visibility helper: pauses dashboard polling when the main window
 * is hidden (tray) or the document is backgrounded.
 */

export function subscribeUiVisibility(listener: (active: boolean) => void): () => void {
  const onBridge = (data: unknown): void => {
    listener((data as { active?: boolean } | null)?.active !== false)
  }
  const offBridge = window.bridge?.on('app:ui-visibility', onBridge)
  const onDocument = (): void => {
    listener(!document.hidden)
  }
  document.addEventListener('visibilitychange', onDocument)
  return () => {
    offBridge?.()
    document.removeEventListener('visibilitychange', onDocument)
  }
}
