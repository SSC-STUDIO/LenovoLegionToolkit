import { notification } from 'antd'
import type { CSSProperties, ReactNode } from 'react'
import { percentToScale } from '../utils/percentScale'

/**
 * Mirrors WPF ProgressToastHelper: persistent bottom-right progress toasts
 * that are created once, updated in place and dismissed on completion.
 * Safe no-ops when the notification host is unavailable.
 */

export type ProgressToastId = number

const TOAST_WIDTH = 360

let nextToastId = 1
const titles = new Map<ProgressToastId, string>()

function progressTrackStyle(): CSSProperties {
  return {
    height: 4,
    marginTop: 8,
    borderRadius: 2,
    background: 'rgba(128, 128, 128, 0.25)',
    overflow: 'hidden'
  }
}

function progressFillStyle(percent: number): CSSProperties {
  return {
    height: '100%',
    width: `${Math.round(percentToScale(percent) * 100)}%`,
    borderRadius: 2,
    background: 'var(--ant-color-primary, #1677ff)',
    transition: 'width 0.2s ease'
  }
}

export function progressToastDescription(message: ReactNode, percent: number): ReactNode {
  return (
    <div>
      <div>{message}</div>
      <div style={progressTrackStyle()}>
        <div style={progressFillStyle(percent)} />
      </div>
    </div>
  )
}

/** Start a persistent progress toast; returns the id used for updates. */
export function startProgressToast(title: string, message?: ReactNode): ProgressToastId {
  const id = nextToastId++
  titles.set(id, title)
  notification.open({
    key: `udt-progress-${id}`,
    message: title,
    description: progressToastDescription(message ?? '', 0),
    placement: 'bottomRight',
    duration: false,
    closable: false,
    style: { width: TOAST_WIDTH }
  })
  return id
}

/** Update an existing progress toast in place. */
export function updateProgressToast(id: ProgressToastId, percent: number, message?: ReactNode): void {
  const title = titles.get(id)
  if (title === undefined) return
  notification.open({
    key: `udt-progress-${id}`,
    message: title,
    description: progressToastDescription(message ?? '', percent),
    placement: 'bottomRight',
    duration: false,
    closable: false,
    style: { width: TOAST_WIDTH }
  })
}

/** Dismiss a progress toast. */
export function completeProgressToast(id: ProgressToastId): void {
  titles.delete(id)
  notification.destroy(`udt-progress-${id}`)
}
