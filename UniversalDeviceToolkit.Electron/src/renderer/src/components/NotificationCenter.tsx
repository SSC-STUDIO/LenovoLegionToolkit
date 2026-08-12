import '../notifications/notifications.css'
import { useEffect, useRef, useState } from 'react'
import {
  CheckCircleFilled,
  CloseCircleFilled,
  CloseOutlined,
  InfoCircleFilled,
  WarningFilled
} from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import { useSettingsStore } from '../stores/settingsStore'
import { useNotificationCenter, type NotificationItem, type NotificationSeverity } from '../notifications/notificationCenterStore'

/**
 * Right-corner notification stack — port of Electron AppNotificationHost +
 * NotificationItemViewModel: severity icon/color, ×N merge badge, progress
 * bar, per-toast auto-close (hover pauses the timer), close button.
 */

const SEVERITY_COLORS: Record<NotificationSeverity, string> = {
  Success: '#2eb871',
  Info: '#3e8ae0',
  Warning: '#e6a23c',
  Error: '#e84a5f'
}

function SeverityIcon({ severity }: { severity: NotificationSeverity }): React.JSX.Element {
  switch (severity) {
    case 'Success':
      return <CheckCircleFilled />
    case 'Warning':
      return <WarningFilled />
    case 'Error':
      return <CloseCircleFilled />
    default:
      return <InfoCircleFilled />
  }
}

/** Electron SystemSounds.Asterisk equivalent — short two-tone beep. */
function playNotificationSound(): void {
  try {
    const AudioContextClass = window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext
    if (AudioContextClass === undefined) return
    const context = new AudioContextClass()
    const now = context.currentTime
    const playTone = (frequency: number, start: number, duration: number): void => {
      const oscillator = context.createOscillator()
      const gain = context.createGain()
      oscillator.type = 'sine'
      oscillator.frequency.value = frequency
      gain.gain.setValueAtTime(0.08, start)
      gain.gain.exponentialRampToValueAtTime(0.001, start + duration)
      oscillator.connect(gain)
      gain.connect(context.destination)
      oscillator.start(start)
      oscillator.stop(start + duration)
    }
    playTone(880, now, 0.12)
    playTone(1320, now + 0.1, 0.16)
    window.setTimeout(() => {
      void context.close()
    }, 400)
  } catch {
    // Best-effort sound playback.
  }
}

function NotificationToast({ item }: { item: NotificationItem }): React.JSX.Element {
  const { t } = useTranslation()
  const pause = useNotificationCenter((s) => s.pause)
  const resume = useNotificationCenter((s) => s.resume)
  const dismiss = useNotificationCenter((s) => s.dismiss)
  // Close-out animation: slide out before the store removes the item.
  const [closing, setClosing] = useState(false)
  const closeTimerRef = useRef<number | null>(null)

  useEffect(
    () => () => {
      if (closeTimerRef.current !== null) window.clearTimeout(closeTimerRef.current)
    },
    []
  )

  const handleClose = (): void => {
    if (closing) return
    setClosing(true)
    closeTimerRef.current = window.setTimeout(() => dismiss(item.id), 180)
  }

  const color = SEVERITY_COLORS[item.severity]
  const hasProgress = typeof item.progressPercent === 'number'
  const percent = hasProgress ? Math.min(100, Math.max(0, item.progressPercent ?? 0)) : 0
  const title = item.mergeCount > 1 ? `${item.title} ×${item.mergeCount}` : item.title
  const classes = ['udt-notification-item']
  if (closing) classes.push('udt-notification-item--closing')

  return (
    <div
      className={classes.join(' ')}
      role="status"
      onMouseEnter={() => pause(item.id)}
      onMouseLeave={() => resume(item.id)}
    >
      <span className="udt-notification-item__icon" style={{ color }} aria-hidden="true">
        <SeverityIcon severity={item.severity} />
      </span>
      <div className="udt-notification-item__copy">
        <div className="udt-notification-item__title" title={title}>
          {title}
        </div>
        {item.message != null && item.message.trim() !== '' && (
          <div className="udt-notification-item__message">{item.message}</div>
        )}
        {hasProgress && (
          <div className="udt-notification-item__progress" aria-hidden="true">
            <div className="udt-notification-item__progress-fill" style={{ width: `${percent}%` }} />
          </div>
        )}
      </div>
      <button
        type="button"
        className="udt-notification-item__close"
        aria-label={t('common.close', { defaultValue: 'Close' })}
        onClick={handleClose}
      >
        <CloseOutlined />
      </button>
    </div>
  )
}

function readApplicationScope(): Record<string, unknown> {
  const scopes = useSettingsStore.getState().scopes
  return typeof scopes.application === 'object' && scopes.application !== null
    ? (scopes.application as Record<string, unknown>)
    : {}
}

/** Suppression + sound settings (Electron AppNotificationHost.ShouldSuppress/TryPlaySound). */
export function readNotificationPreferences(): {
  suppressed: boolean
  suppressSuccess: boolean
  playSound: boolean
  duration: 'Short' | 'Normal' | 'Long'
  position: string
} {
  const app = readApplicationScope()
  const notifications =
    typeof app['Notifications'] === 'object' && app['Notifications'] !== null
      ? (app['Notifications'] as Record<string, unknown>)
      : {}
  const duration = app['NotificationDuration']
  const position = app['NotificationPosition']
  return {
    suppressed: app['DontShowNotifications'] === true,
    suppressSuccess: notifications['SuccessNotifications'] === false,
    playSound: notifications['NotificationSound'] === true,
    duration:
      duration === 'Short' || duration === 'Long' ? duration : ('Normal' as const),
    position: typeof position === 'string' ? position : 'BottomRight'
  }
}

export function maybePlayNotificationSound(): void {
  if (readNotificationPreferences().playSound) playNotificationSound()
}

/** Electron NotificationPosition → placement CSS class. */
function positionClass(position: string): string {
  switch (position) {
    case 'BottomCenter':
      return 'udt-notification-center--bottom-center'
    case 'BottomLeft':
      return 'udt-notification-center--bottom-left'
    case 'CenterLeft':
      return 'udt-notification-center--center-left'
    case 'TopLeft':
      return 'udt-notification-center--top-left'
    case 'TopCenter':
      return 'udt-notification-center--top-center'
    case 'TopRight':
      return 'udt-notification-center--top-right'
    case 'CenterRight':
      return 'udt-notification-center--center-right'
    default:
      return 'udt-notification-center--bottom-right'
  }
}

export default function NotificationCenter(): React.JSX.Element {
  const { t } = useTranslation()
  const items = useNotificationCenter((s) => s.items)
  const settingsReady = useSettingsStore((s) => s.loading === false)
  const position = readNotificationPreferences().position

  useEffect(() => {
    void useSettingsStore.getState().load(['application'])
  }, [])

  if (items.length === 0 || !settingsReady) return <></>

  const classes = ['udt-notification-center', positionClass(position)]

  return (
    <div className={classes.join(' ')} aria-label={t('common.notifications')}>
      {items.map((item) => (
        <NotificationToast key={item.id} item={item} />
      ))}
    </div>
  )
}
