import { message, notification } from 'antd'
import type { ReactNode } from 'react'
import { on } from '../api/bridge'
import { progressToastDescription } from './progressToast'
import { initPluginInstallToast } from './pluginInstallToast'

export interface AppNotificationRequest {
  title: string
  message: string
  severity: 'Success' | 'Info' | 'Warning' | 'Error'
  isPersistent?: boolean
  progressPercent?: number
}

let unsubscribe: (() => void) | undefined

/**
 * Mirrors WPF SnackbarHelper timeouts: success/info auto-dismiss after 5s,
 * warnings/errors stay for 8s and keep their close button.
 */
const MESSAGE_DURATION_MS: Record<AppNotificationRequest['severity'], number> = {
  Success: 5000,
  Info: 5000,
  Warning: 8000,
  Error: 8000
}

const MERGE_SEPARATOR = '\u001f'

function contentOf(data: AppNotificationRequest): string {
  return data.message ? `${data.title}: ${data.message}` : data.title
}

function showSnackbar(data: AppNotificationRequest): void {
  const severity = data.severity
  // Only merge identical success copy within the service window (avoids toast
  // storms for repeated identical operations). Errors/warnings stay distinct.
  const key = severity === 'Success' ? `success:${data.title}${MERGE_SEPARATOR}${data.message}` : undefined
  const content = contentOf(data)

  message.open({
    type: severity.toLowerCase() as 'success' | 'info' | 'warning' | 'error',
    key,
    content,
    duration: MESSAGE_DURATION_MS[severity] / 1000
  })
}

/** Progress-bearing host notifications render as persistent bottom-right toasts. */
const progressNotifications = new Map<string, string>()

function showProgressNotification(data: AppNotificationRequest): void {
  const percent = Math.min(100, Math.max(0, data.progressPercent ?? 0))
  const contentKey = contentOf(data)
  const existing = progressNotifications.get(contentKey)
  const notificationKey = existing ?? `udt-host-progress-${progressNotifications.size}`

  const description: ReactNode = progressToastDescription(
    data.message || (percent > 0 ? `${Math.round(percent)}%` : ''),
    percent
  )

  notification.open({
    key: notificationKey,
    message: data.title,
    description,
    placement: 'bottomRight',
    duration: percent >= 100 ? 2 : false,
    closable: false,
    style: { width: 360 }
  })

  if (existing === undefined) progressNotifications.set(contentKey, notificationKey)

  if (percent >= 100) {
    window.setTimeout(() => {
      notification.destroy(notificationKey)
      progressNotifications.delete(contentKey)
    }, 2500)
  }
}

function showNotification(data: AppNotificationRequest): void {
  if (typeof data.progressPercent === 'number') {
    showProgressNotification(data)
    return
  }
  showSnackbar(data)
}

export function initNotifications(): () => void {
  if (unsubscribe) {
    return unsubscribe
  }
  unsubscribe = on<AppNotificationRequest>('notifications.changed', showNotification)
  initPluginInstallToast()
  return unsubscribe
}

export function notifyTest(): void {
  showNotification({
    title: 'UDT Test Notification',
    message: 'notifications.changed 链路工作正常',
    severity: 'Info',
    isPersistent: false
  })
}
