import { notification } from 'antd'
import { createElement } from 'react'
import type { ReactNode } from 'react'
import { on } from '../api/bridge'
import i18n from '../i18n'
import { progressToastDescription } from './progressToast'
import { initPluginInstallToast } from './pluginInstallToast'
import './notifications.css'

export interface AppNotificationRequest {
  title: string
  message: string
  severity: 'Success' | 'Info' | 'Warning' | 'Error'
  isPersistent?: boolean
  progressPercent?: number
}

/** Host severity -> antd notification type (WPF AppNotificationSeverity). */
const NOTIFICATION_TYPE: Record<
  AppNotificationRequest['severity'],
  'success' | 'info' | 'warning' | 'error'
> = {
  Success: 'success',
  Info: 'info',
  Warning: 'warning',
  Error: 'error'
}

/** WPF NotificationDuration.Short — default auto-close for non-persistent toasts. */
const AUTO_CLOSE_SECONDS = 3
/**
 * WPF merge semantics, simplified: notifications with the same title (and
 * severity) arriving within this window merge into a single toast with a ×N
 * badge; each merge extends the window (WPF resets the auto-close timer).
 */
const MERGE_WINDOW_MS = 3000
/** WPF AppNotificationHost.TrimIfNeeded hard cap on live toasts. */
const MAX_NOTIFICATIONS = 30
/** WPF NotificationToastWidth. */
const TOAST_WIDTH = 400
/** Completed progress toasts linger briefly before auto-closing. */
const COMPLETE_CLOSE_SECONDS = 2

const MERGE_SEPARATOR = '\u001f'

interface MergeState {
  count: number
  expiresAt: number
  timer: number | undefined
}

/** Live merge counters, keyed by severity + title. */
const mergeStates = new Map<string, MergeState>()

let unsubscribe: (() => void) | undefined

/** Stable antd key per logical toast; reuse updates the toast in place. */
function notificationKey(data: AppNotificationRequest): string {
  return `udt-host:${data.severity}${MERGE_SEPARATOR}${data.title}`
}

function updateMergeState(data: AppNotificationRequest): MergeState {
  const key = notificationKey(data)
  const now = Date.now()
  let state = mergeStates.get(key)
  if (state === undefined || now > state.expiresAt) {
    state = { count: 1, expiresAt: now + MERGE_WINDOW_MS, timer: undefined }
  } else {
    state.count += 1
    state.expiresAt = now + MERGE_WINDOW_MS
  }
  if (state.timer !== undefined) window.clearTimeout(state.timer)
  state.timer = window.setTimeout(() => {
    if (mergeStates.get(key) === state) mergeStates.delete(key)
  }, MERGE_WINDOW_MS + 100)
  mergeStates.set(key, state)
  return state
}

function mergedCountText(count: number): string {
  return i18n.t('notifications.mergedCount', { count, defaultValue: '×{{count}}' })
}

function buildDescription(data: AppNotificationRequest, mergeCount: number): ReactNode {
  const text: ReactNode[] = []
  if (typeof data.message === 'string' && data.message.trim().length > 0) {
    text.push(createElement('span', { key: 'message' }, data.message))
  }
  if (mergeCount > 1) {
    text.push(
      createElement('span', { key: 'merged', className: 'udt-host-notice__merged' }, mergedCountText(mergeCount))
    )
  }
  const textNode =
    text.length > 0 ? createElement('div', { className: 'udt-host-notice__text' }, text) : ''

  if (typeof data.progressPercent !== 'number') {
    return text.length > 0 ? textNode : undefined
  }
  const percent = Math.min(100, Math.max(0, data.progressPercent))
  // antd's `showProgress` is a countdown-only boolean bar, so percent-based
  // host progress reuses the shared progress toast bar (progressToast.tsx).
  return progressToastDescription(textNode, percent)
}

function resolveDuration(data: AppNotificationRequest): number {
  // WPF: persistent toasts never auto-close (duration 0 = no auto-close).
  if (data.isPersistent) return 0
  if (typeof data.progressPercent === 'number' && data.progressPercent >= 100) {
    return COMPLETE_CLOSE_SECONDS
  }
  return AUTO_CLOSE_SECONDS
}

function showNotification(data: AppNotificationRequest): void {
  const state = updateMergeState(data)
  notification[NOTIFICATION_TYPE[data.severity]]({
    key: notificationKey(data),
    title: data.title,
    description: buildDescription(data, state.count),
    placement: 'topRight',
    duration: resolveDuration(data),
    pauseOnHover: true,
    closable: true,
    classNames: { root: 'udt-host-notice' },
    style: { width: TOAST_WIDTH }
  })
}

export function initNotifications(): () => void {
  if (unsubscribe) {
    return unsubscribe
  }
  // Stacking (WPF-style right-corner pile) and hover-pause are antd 6
  // defaults; configure them explicitly so the contract stays visible.
  notification.config({
    placement: 'topRight',
    duration: AUTO_CLOSE_SECONDS,
    pauseOnHover: true,
    maxCount: MAX_NOTIFICATIONS
  })
  unsubscribe = on<AppNotificationRequest>('notifications.changed', showNotification)
  initPluginInstallToast()
  return unsubscribe
}

/**
 * Dev/test helper: pushes a sample host notification through the same
 * pipeline so the toast host can be exercised without the C# host.
 */
export function notifyTest(overrides: Partial<AppNotificationRequest> = {}): void {
  showNotification({
    title: 'Test Notification',
    message: 'This is a test notification.',
    severity: 'Info',
    ...overrides
  })
}
