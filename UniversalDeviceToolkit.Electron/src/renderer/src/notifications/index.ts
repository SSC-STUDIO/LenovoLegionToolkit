import { message } from 'antd'
import { on } from '../api/bridge'

export interface AppNotificationRequest {
  title: string
  message: string
  severity: 'Success' | 'Info' | 'Warning' | 'Error'
  isPersistent?: boolean
  progressPercent?: number
}

let unsubscribe: (() => void) | undefined

function showNotification(data: AppNotificationRequest): void {
  const content = data.message ? `${data.title}: ${data.message}` : data.title
  switch (data.severity) {
    case 'Success':
      message.success(content)
      break
    case 'Info':
      message.info(content)
      break
    case 'Warning':
      message.warning(content)
      break
    case 'Error':
      message.error(content)
      break
  }
}

export function initNotifications(): () => void {
  if (unsubscribe) {
    return unsubscribe
  }
  unsubscribe = on<AppNotificationRequest>('notifications.changed', showNotification)
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
