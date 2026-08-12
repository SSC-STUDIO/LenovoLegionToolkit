import { useEffect, useState } from 'react'
import { Select, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { bootLogoApi } from '../../api/bootLogo'
import { settingsApi } from '../../api/settings'
import { useSettingsStore } from '../../stores/settingsStore'
import { SettingsCard } from './SettingsCard'
import WindowBackdropSetting from './WindowBackdropSetting'
import BootLogoModal from './BootLogoModal'
import ExcludeRefreshRatesModal from './ExcludeRefreshRatesModal'
import NavigationItemsModal from './NavigationItemsModal'
import NotificationsModal from './NotificationsModal'

const NOTIFICATION_POSITIONS: Array<{ value: string; i18nKey: string }> = [
  { value: 'BottomRight', i18nKey: 'settings.display.notificationPositions.bottomRight' },
  { value: 'BottomCenter', i18nKey: 'settings.display.notificationPositions.bottomCenter' },
  { value: 'BottomLeft', i18nKey: 'settings.display.notificationPositions.bottomLeft' },
  { value: 'CenterLeft', i18nKey: 'settings.display.notificationPositions.centerLeft' },
  { value: 'TopLeft', i18nKey: 'settings.display.notificationPositions.topLeft' },
  { value: 'TopCenter', i18nKey: 'settings.display.notificationPositions.topCenter' },
  { value: 'TopRight', i18nKey: 'settings.display.notificationPositions.topRight' },
  { value: 'CenterRight', i18nKey: 'settings.display.notificationPositions.centerRight' },
  { value: 'Center', i18nKey: 'settings.display.notificationPositions.center' }
]

const NOTIFICATION_DURATIONS: Array<{ value: string; i18nKey: string }> = [
  { value: 'Short', i18nKey: 'settings.display.notificationDurations.short' },
  { value: 'Normal', i18nKey: 'settings.display.notificationDurations.normal' },
  { value: 'Long', i18nKey: 'settings.display.notificationDurations.long' }
]

export function DisplaySection(): React.JSX.Element {
  const { t } = useTranslation()
  const { scopes, load, setScope } = useSettingsStore()
  const [navigationItemsOpen, setNavigationItemsOpen] = useState(false)
  const [notificationsOpen, setNotificationsOpen] = useState(false)
  const [excludeRefreshRatesOpen, setExcludeRefreshRatesOpen] = useState(false)
  const [bootLogoOpen, setBootLogoOpen] = useState(false)
  const [bootLogoSupported, setBootLogoSupported] = useState(false)

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    let cancelled = false
    bootLogoApi
      .getStatus()
      .then(() => {
        if (!cancelled) setBootLogoSupported(true)
      })
      .catch(() => {
        // The boot logo feature is hidden when it is unsupported or the host
        // does not expose it, mirroring the Electron BootLogo.IsSupportedAsync gate.
      })
    return () => {
      cancelled = true
    }
  }, [])

  const app = (scopes.application ?? {}) as Record<string, unknown>
  const notificationPosition = (app['NotificationPosition'] as string | undefined) ?? 'BottomRight'
  const notificationDuration = (app['NotificationDuration'] as string | undefined) ?? 'Normal'
  const excludedRefreshRates = (app['ExcludedRefreshRates'] ?? []) as Array<{ Frequency: number }>

  const persistApplication = async (patch: Record<string, unknown>): Promise<void> => {
    const current = (scopes.application ?? {}) as Record<string, unknown>
    const next = { ...current, ...patch }
    setScope('application', next)
    try {
      await settingsApi.set('application', next)
      await settingsApi.save(['application'])
      message.success(t('settings.saved'))
    } catch {
      message.error(t('settings.saveFailed'))
    }
  }

  return (
    <div className="udt-settings-section udt-settings-section--display">
      <SettingsCard
        title={t('settings.display.navigationItems')}
        description={t('wpf.navigationItemsSettingsWindowdescription')}
        onClick={() => setNavigationItemsOpen(true)}
      />
      {bootLogoSupported && (
        <SettingsCard
          title={t('settings.display.bootLogo')}
          description={t('settings.display.bootLogoDesc')}
          onClick={() => setBootLogoOpen(true)}
        />
      )}
      <SettingsCard
        title={t('settings.display.notifications')}
        description={t('settings.display.notificationsDesc')}
        onClick={() => setNotificationsOpen(true)}
      />
      <WindowBackdropSetting application={app} persist={(patch) => void persistApplication(patch)} />
      <SettingsCard
        title={t('settings.display.notificationPosition')}
        action={
          <Select<string>
            className="udt-settings-select"
            value={notificationPosition}
            onChange={(value) => void persistApplication({ NotificationPosition: value })}
            options={NOTIFICATION_POSITIONS.map((option) => ({
              value: option.value,
              label: t(option.i18nKey)
            }))}
          />
        }
      />
      <SettingsCard
        title={t('settings.display.notificationDuration')}
        action={
          <Select<string>
            className="udt-settings-select"
            value={notificationDuration}
            onChange={(value) => void persistApplication({ NotificationDuration: value })}
            options={NOTIFICATION_DURATIONS.map((option) => ({
              value: option.value,
              label: t(option.i18nKey)
            }))}
          />
        }
      />
      <SettingsCard
        title={t('settings.display.excludedRefreshRates')}
        description={t('settings.display.excludedRefreshRatesDesc')}
        onClick={() => setExcludeRefreshRatesOpen(true)}
      >
        {excludedRefreshRates.length === 0 ? (
          <div className="udt-settings-card__empty">
            {t('settings.display.excludedRefreshRatesEmpty')}
          </div>
        ) : (
          <div className="udt-settings-switch-list">
            {excludedRefreshRates.map((rate) => (
              <div key={rate.Frequency} className="udt-settings-switch-list__row">
                <span className="udt-settings-switch-list__label">{rate.Frequency}Hz</span>
              </div>
            ))}
          </div>
        )}
        <div className="udt-settings-card__hint">
          {t('settings.display.excludedRefreshRatesManageHint')}
        </div>
      </SettingsCard>

      <NavigationItemsModal
        open={navigationItemsOpen}
        onClose={() => setNavigationItemsOpen(false)}
      />
      <NotificationsModal open={notificationsOpen} onClose={() => setNotificationsOpen(false)} />
      <ExcludeRefreshRatesModal
        open={excludeRefreshRatesOpen}
        onClose={() => setExcludeRefreshRatesOpen(false)}
        onSaved={() => void load()}
      />
      <BootLogoModal open={bootLogoOpen} onClose={() => setBootLogoOpen(false)} />
    </div>
  )
}
