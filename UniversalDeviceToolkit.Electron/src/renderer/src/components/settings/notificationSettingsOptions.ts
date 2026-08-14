import type { TFunction } from 'i18next'

/** Matches Electron NotificationsSettingsWindow position enum. */
export const NOTIFICATION_POSITIONS = [
  'BottomRight',
  'BottomCenter',
  'BottomLeft',
  'CenterLeft',
  'TopLeft',
  'TopCenter',
  'TopRight',
  'CenterRight',
  'Center'
] as const

export const NOTIFICATION_DURATIONS = ['Short', 'Normal', 'Long'] as const

export type NotificationPosition = (typeof NOTIFICATION_POSITIONS)[number]
export type NotificationDuration = (typeof NOTIFICATION_DURATIONS)[number]

function positionI18nKey(value: string): string {
  const lower = value.charAt(0).toLowerCase() + value.slice(1)
  return `settings.display.notificationPositions.${lower}`
}

function durationI18nKey(value: string): string {
  const lower = value.charAt(0).toLowerCase() + value.slice(1)
  return `settings.display.notificationDurations.${lower}`
}

export function buildNotificationPositionOptions(t: TFunction): Array<{ value: string; label: string }> {
  return NOTIFICATION_POSITIONS.map((value) => ({
    value,
    label: t(positionI18nKey(value))
  }))
}

export function buildNotificationDurationOptions(t: TFunction): Array<{ value: string; label: string }> {
  return NOTIFICATION_DURATIONS.map((value) => ({
    value,
    label: t(durationI18nKey(value))
  }))
}
