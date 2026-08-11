import type { ReactNode } from 'react'
import { useNotificationCenter } from './notificationCenterStore'

/**
 * Mirrors WPF ProgressToastHelper: persistent progress toasts that are
 * created once, updated in place and dismissed on completion. They live in
 * the notification center stack (progress < 100% never auto-closes).
 */

export type ProgressToastId = number

let nextToastId = 1
const itemIds = new Map<ProgressToastId, string>()

function toMessage(percent: number, message?: ReactNode): string | undefined {
  if (message !== undefined && message !== null && message !== '') return String(message)
  if (percent > 0) return `${Math.round(percent)}%`
  return undefined
}

/** Start a persistent progress toast; returns the id used for updates. */
export function startProgressToast(title: string, message?: ReactNode): ProgressToastId {
  const id = nextToastId++
  const itemId = useNotificationCenter.getState().pushProgress(title, toMessage(0, message))
  itemIds.set(id, itemId)
  return id
}

/** Update an existing progress toast in place. */
export function updateProgressToast(id: ProgressToastId, percent: number, message?: ReactNode): void {
  const itemId = itemIds.get(id)
  if (itemId === undefined) return
  useNotificationCenter.getState().updateProgress(itemId, percent, toMessage(percent, message))
}

/** Dismiss a progress toast. */
export function completeProgressToast(id: ProgressToastId): void {
  const itemId = itemIds.get(id)
  itemIds.delete(id)
  if (itemId === undefined) return
  useNotificationCenter.getState().dismiss(itemId)
}
