export interface AppNotificationRequest {
  title: string
  message: string
  severity: 'Success' | 'Info' | 'Warning' | 'Error'
  durationMs?: number
  isPersistent?: boolean
}

/**
 * P1 未实现：docs/PROTOCOL.md 暂无 notifications 事件，Host 尚未通过
 * AppNotificationService.Changed 推送通知；待协议补充后在此接入 antd message。
 */
export function initNotifications(): void {
  // no-op until the host publishes the notifications.changed event
}
