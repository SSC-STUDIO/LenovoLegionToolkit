import { useEffect, useState } from 'react'
import { Modal, Select, Spin, Switch, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { useSettingsStore } from '../../stores/settingsStore'

/**
 * Parity modal for WPF Windows/Settings/NotificationsSettingsWindow:
 * toggles for every notification category plus always-on-top, all-screens,
 * position and duration. Every change is persisted immediately, like the
 * WPF window (each toggle synchronizes the store on click). While "Don't
 * show notifications" is on, the other cards are disabled.
 */

interface NotificationsModalProps {
  open: boolean
  onClose: () => void
}

interface NotificationFields {
  dontShowNotifications: boolean
  successNotifications: boolean
  notificationSound: boolean
  alwaysOnTop: boolean
  onAllScreens: boolean
  position: string
  duration: string
  updateAvailable: boolean
  capsNumLock: boolean
  fnLock: boolean
  touchpadLock: boolean
  keyboardBacklight: boolean
  cameraLock: boolean
  microphone: boolean
  powerMode: boolean
  refreshRate: boolean
  acAdapter: boolean
  smartKey: boolean
  automation: boolean
}

const DEFAULT_FIELDS: NotificationFields = {
  dontShowNotifications: false,
  successNotifications: true,
  notificationSound: false,
  alwaysOnTop: false,
  onAllScreens: false,
  position: 'BottomRight',
  duration: 'Normal',
  updateAvailable: true,
  capsNumLock: false,
  fnLock: false,
  touchpadLock: true,
  keyboardBacklight: true,
  cameraLock: true,
  microphone: true,
  powerMode: false,
  refreshRate: true,
  acAdapter: false,
  smartKey: false,
  automation: true
}

const NOTIFICATION_POSITIONS = [
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

const NOTIFICATION_DURATIONS = ['Short', 'Normal', 'Long'] as const

function readBoolean(record: Record<string, unknown>, key: string, fallback: boolean): boolean {
  const value = record[key]
  return typeof value === 'boolean' ? value : fallback
}

function readString(record: Record<string, unknown>, key: string, fallback: string): string {
  const value = record[key]
  return typeof value === 'string' && value.length > 0 ? value : fallback
}

function parseFields(value: unknown): NotificationFields {
  const store = (value ?? {}) as Record<string, unknown>
  const notifications = (store.Notifications ?? {}) as Record<string, unknown>
  return {
    dontShowNotifications: readBoolean(store, 'DontShowNotifications', false),
    successNotifications: readBoolean(notifications, 'SuccessNotifications', true),
    notificationSound: readBoolean(notifications, 'NotificationSound', false),
    alwaysOnTop: readBoolean(store, 'NotificationAlwaysOnTop', false),
    onAllScreens: readBoolean(store, 'NotificationOnAllScreens', false),
    position: readString(store, 'NotificationPosition', 'BottomRight'),
    duration: readString(store, 'NotificationDuration', 'Normal'),
    updateAvailable: readBoolean(notifications, 'UpdateAvailable', true),
    capsNumLock: readBoolean(notifications, 'CapsNumLock', false),
    fnLock: readBoolean(notifications, 'FnLock', false),
    touchpadLock: readBoolean(notifications, 'TouchpadLock', true),
    keyboardBacklight: readBoolean(notifications, 'KeyboardBacklight', true),
    cameraLock: readBoolean(notifications, 'CameraLock', true),
    microphone: readBoolean(notifications, 'Microphone', true),
    powerMode: readBoolean(notifications, 'PowerMode', false),
    refreshRate: readBoolean(notifications, 'RefreshRate', true),
    acAdapter: readBoolean(notifications, 'ACAdapter', false),
    smartKey: readBoolean(notifications, 'SmartKey', false),
    automation: readBoolean(notifications, 'AutomationNotification', true)
  }
}

interface ToggleRow {
  key: Exclude<keyof NotificationFields, 'position' | 'duration'>
  titleKey: string
  descKey?: string
}

const TOGGLE_ROWS: ToggleRow[] = [
  { key: 'successNotifications', titleKey: 'notificationsSettingsWindowsuccessNotificationstitle', descKey: 'notificationsSettingsWindowsuccessNotificationsmessage' },
  { key: 'notificationSound', titleKey: 'notificationsSettingsWindownotificationSoundtitle', descKey: 'notificationsSettingsWindownotificationSoundmessage' },
  { key: 'alwaysOnTop', titleKey: 'notificationsSettingsWindownotificationAlwaysOnToptitle', descKey: 'notificationsSettingsWindownotificationAlwaysOnTopmessage' },
  { key: 'onAllScreens', titleKey: 'notificationsSettingsWindownotificationOnAllScreenstitle', descKey: 'notificationsSettingsWindownotificationOnAllScreensmessage' },
  { key: 'updateAvailable', titleKey: 'notificationsSettingsWindowupdatestitle' },
  { key: 'capsNumLock', titleKey: 'notificationsSettingsWindowcapsAndNumLock' },
  { key: 'fnLock', titleKey: 'notificationsSettingsWindowfnLock' },
  { key: 'touchpadLock', titleKey: 'notificationsSettingsWindowtouchpadLock' },
  { key: 'keyboardBacklight', titleKey: 'notificationsSettingsWindowkeyboardBacklight' },
  { key: 'cameraLock', titleKey: 'notificationsSettingsWindowcamera' },
  { key: 'microphone', titleKey: 'notificationsSettingsWindowmicrophone' },
  { key: 'powerMode', titleKey: 'notificationsSettingsWindowpowerMode' },
  { key: 'refreshRate', titleKey: 'notificationsSettingsWindowrefreshRate' },
  { key: 'acAdapter', titleKey: 'notificationsSettingsWindowaCAdapter' },
  { key: 'smartKey', titleKey: 'notificationsSettingsWindowsmartKey' },
  { key: 'automation', titleKey: 'notificationsSettingsWindowautomation' }
]

function positionKey(value: string): string {
  const lower = value.charAt(0).toLowerCase() + value.slice(1)
  return `settings.display.notificationPositions.${lower}`
}

function durationKey(value: string): string {
  const lower = value.charAt(0).toLowerCase() + value.slice(1)
  return `settings.display.notificationDurations.${lower}`
}

export default function NotificationsModal({
  open,
  onClose
}: NotificationsModalProps): React.JSX.Element {
  const { t } = useTranslation()
  const [loading, setLoading] = useState(true)
  const [fields, setFields] = useState<NotificationFields>(DEFAULT_FIELDS)

  useEffect(() => {
    if (!open) return
    let cancelled = false
    setLoading(true)
    settingsApi
      .get('application')
      .then((result) => {
        if (!cancelled) setFields(parseFields(result.value))
      })
      .catch((reason: unknown) => {
        if (!cancelled) void message.error((reason as Error).message)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [open])

  const persist = async (patch: Partial<NotificationFields>): Promise<void> => {
    const next = { ...fields, ...patch }
    setFields(next)
    try {
      const result = await settingsApi.get('application')
      const current = (result.value ?? {}) as Record<string, unknown>
      const notifications = (current.Notifications ?? {}) as Record<string, unknown>
      const merged = {
        ...current,
        DontShowNotifications: next.dontShowNotifications,
        NotificationAlwaysOnTop: next.alwaysOnTop,
        NotificationOnAllScreens: next.onAllScreens,
        NotificationPosition: next.position,
        NotificationDuration: next.duration,
        Notifications: {
          ...notifications,
          SuccessNotifications: next.successNotifications,
          NotificationSound: next.notificationSound,
          UpdateAvailable: next.updateAvailable,
          CapsNumLock: next.capsNumLock,
          FnLock: next.fnLock,
          TouchpadLock: next.touchpadLock,
          KeyboardBacklight: next.keyboardBacklight,
          CameraLock: next.cameraLock,
          Microphone: next.microphone,
          PowerMode: next.powerMode,
          RefreshRate: next.refreshRate,
          ACAdapter: next.acAdapter,
          SmartKey: next.smartKey,
          AutomationNotification: next.automation
        }
      }
      useSettingsStore.getState().setScope('application', merged)
      await settingsApi.set('application', merged)
      await settingsApi.save(['application'])
    } catch (reason) {
      void message.error((reason as Error).message)
      setFields(fields)
    }
  }

  const notificationsDisabled = fields.dontShowNotifications

  return (
    <Modal
      open={open}
      title={t('notificationsSettingsWindowtitle')}
      width={500}
      footer={null}
      onCancel={onClose}
    >
      {loading ? (
        <div style={{ textAlign: 'center', padding: 24 }}>
          <Spin />
        </div>
      ) : (
        <div style={{ maxHeight: '60vh', overflowY: 'auto', paddingRight: 8 }}>
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: 16,
              padding: '10px 0',
              borderBottom: '1px solid rgba(128,128,128,0.15)'
            }}
          >
            <div>
              <div style={{ fontWeight: 600 }}>{t('notificationsSettingsWindowdontShowNotificationstitle')}</div>
              <div style={{ opacity: 0.65, fontSize: 12, whiteSpace: 'pre-line' }}>
                {t('notificationsSettingsWindowdontShowNotificationsmessage')}
              </div>
            </div>
            <Switch
              className="udt-settings-switch"
              checked={fields.dontShowNotifications}
              onChange={(checked) => void persist({ dontShowNotifications: checked })}
            />
          </div>

          {TOGGLE_ROWS.map((row) => (
            <div
              key={row.key}
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: 16,
                padding: '10px 0',
                borderBottom: '1px solid rgba(128,128,128,0.15)'
              }}
            >
              <div>
                <div style={{ fontWeight: 600 }}>{t(row.titleKey)}</div>
                {row.descKey != null && (
                  <div style={{ opacity: 0.65, fontSize: 12, whiteSpace: 'pre-line' }}>
                    {t(row.descKey)}
                  </div>
                )}
              </div>
              <Switch
                className="udt-settings-switch"
                disabled={notificationsDisabled}
                checked={fields[row.key]}
                onChange={(checked) => void persist({ [row.key]: checked })}
              />
            </div>
          ))}

          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: 16,
              padding: '10px 0',
              borderBottom: '1px solid rgba(128,128,128,0.15)'
            }}
          >
            <div style={{ fontWeight: 600 }}>{t('notificationsSettingsWindownotificationPositiontitle')}</div>
            <Select<string>
              className="udt-settings-select"
              style={{ minWidth: 200 }}
              disabled={notificationsDisabled}
              value={fields.position}
              onChange={(value) => void persist({ position: value })}
              options={NOTIFICATION_POSITIONS.map((value) => ({
                value,
                label: t(positionKey(value))
              }))}
            />
          </div>

          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              gap: 16,
              padding: '10px 0'
            }}
          >
            <div style={{ fontWeight: 600 }}>{t('notificationsSettingsWindownotificationDurationtitle')}</div>
            <Select<string>
              className="udt-settings-select"
              style={{ minWidth: 200 }}
              disabled={notificationsDisabled}
              value={fields.duration}
              onChange={(value) => void persist({ duration: value })}
              options={NOTIFICATION_DURATIONS.map((value) => ({
                value,
                label: t(durationKey(value))
              }))}
            />
          </div>
        </div>
      )}
    </Modal>
  )
}
