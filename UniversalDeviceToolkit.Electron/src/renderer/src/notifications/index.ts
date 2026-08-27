import { on } from '../api/bridge'
import {
  useNotificationCenter,
  type AppNotificationRequest,
  type NotificationSettings
} from './notificationCenterStore'
import { maybePlayNotificationSound, readNotificationPreferences } from '../components/NotificationCenter'
import './notifications.css'

/**
 * Bridges the host `notifications.changed` events into the notification
 * center (port of Electron AppNotificationHost: right-corner stacking, per-item
 * auto-close, ×N merging, progress bars, hover pause).
 */

let unsubscribe: (() => void) | undefined

/**
 * Renderer-originated toast. Same pipeline as host `notifications.changed`
 * (suppression, duration, merge, sound) so UI errors share the notification
 * center instead of a one-off overlay.
 */
export function notify(data: AppNotificationRequest): void {
  const prefs = readNotificationPreferences()
  // Electron AppNotificationHost.ShouldSuppress.
  if (prefs.suppressed) return
  if (data.severity === 'Success' && prefs.suppressSuccess) return

  const settings: NotificationSettings = { duration: prefs.duration }
  useNotificationCenter.getState().push(data, settings)

  // Electron TryPlaySound (only on new toasts; merges keep the original sound).
  if (data.progressPercent === undefined) maybePlayNotificationSound()
}

export function initNotifications(): () => void {
  if (unsubscribe) {
    return unsubscribe
  }
  unsubscribe = on<AppNotificationRequest>('notifications.changed', notify)
  return unsubscribe
}

/**
 * Dev/test helper: pushes a sample host notification through the same
 * pipeline so the toast host can be exercised without the C# host.
 */
export function notifyTest(overrides: Partial<AppNotificationRequest> = {}): void {
  notify({
    title: 'Test Notification',
    message: 'This is a test notification.',
    severity: 'Info',
    ...overrides
  })
}
