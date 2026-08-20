import type { TFunction } from 'i18next'

/** Matches Electron NotificationsSettingsWindow position enum (left positions removed to avoid nav collision). */
export const NOTIFICATION_POSITIONS = [
  'BottomRight',
  'BottomCenter',
  'TopRight',
  'TopCenter',
  'CenterRight',
  'Center'
] as const

export const NOTIFICATION_DURATIONS = ['Short', 'Normal', 'Long'] as const

export type NotificationPosition = (typeof NOTIFICATION_POSITIONS)[number]
export type NotificationDuration = (typeof NOTIFICATION_DURATIONS)[number]

export function sanitizeNotificationPosition(raw?: unknown): NotificationPosition {
  if (typeof raw !== 'string') return 'BottomRight'
  if ((NOTIFICATION_POSITIONS as readonly string[]).includes(raw)) {
    return raw as NotificationPosition
  }
  // Remap deprecated left-side positions to right-side equivalents
  if (raw === 'TopLeft') return 'TopRight'
  if (raw === 'BottomLeft') return 'BottomRight'
  if (raw === 'CenterLeft') return 'CenterRight'
  return 'BottomRight'
}

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
