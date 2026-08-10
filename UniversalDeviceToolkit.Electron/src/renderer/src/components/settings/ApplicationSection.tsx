import { useEffect } from 'react'
import { Switch, Typography, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { useSettingsStore } from '../../stores/settingsStore'

type AppSettings = Record<string, unknown>

interface ToggleItem {
  field: string
  labelKey: string
  descKey: string
}

const TOGGLE_ITEMS: ToggleItem[] = [
  {
    field: 'MinimizeToTray',
    labelKey: 'settings.application.minimizeToTray',
    descKey: 'settings.application.minimizeToTrayDesc'
  },
  {
    field: 'MinimizeOnClose',
    labelKey: 'settings.application.minimizeOnClose',
    descKey: 'settings.application.minimizeOnCloseDesc'
  },
  {
    field: 'DisableUnsupportedHardwareWarning',
    labelKey: 'settings.application.disableUnsupportedWarning',
    descKey: 'settings.application.disableUnsupportedWarningDesc'
  },
  {
    field: 'EnableHardwareSensors',
    labelKey: 'settings.application.enableHardwareSensors',
    descKey: 'settings.application.enableHardwareSensorsDesc'
  },
  {
    field: 'DontShowNotifications',
    labelKey: 'settings.application.dontShowNotifications',
    descKey: 'settings.application.dontShowNotificationsDesc'
  },
  {
    field: 'ExtensionsEnabled',
    labelKey: 'settings.application.extensionsEnabled',
    descKey: 'settings.application.extensionsEnabledDesc'
  }
]

function readBoolean(app: AppSettings, key: string): boolean {
  return app[key] === true
}

export default function ApplicationSection(): React.JSX.Element {
  const { t } = useTranslation()
  const scopes = useSettingsStore((s) => s.scopes)
  const load = useSettingsStore((s) => s.load)
  const setScope = useSettingsStore((s) => s.setScope)

  const rawApp = scopes.application
  const app: AppSettings =
    typeof rawApp === 'object' && rawApp !== null ? (rawApp as AppSettings) : {}

  useEffect(() => {
    void load()
  }, [load])

  const handleToggle = (field: string, checked: boolean): void => {
    const next: AppSettings = { ...app, [field]: checked }
    setScope('application', next)
    settingsApi
      .set('application', next)
      .then(() => settingsApi.save(['application']))
      .catch(() => message.error(t('settings.saveFailed')))
  }

  return (
    <div>
      <Typography.Title level={4}>{t('settings.nav.application')}</Typography.Title>
      {TOGGLE_ITEMS.map((item) => {
        const checked = readBoolean(app, item.field)
        return (
          <div
            key={item.field}
            style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              padding: '14px 0',
              borderBottom: '1px solid rgba(128, 128, 128, 0.15)'
            }}
          >
            <div>
              <Typography.Text>{t(item.labelKey)}</Typography.Text>
              <div>
                <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                  {t(item.descKey)}
                </Typography.Text>
              </div>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <Typography.Text type="secondary">
                {checked ? t('settings.application.valueOn') : t('settings.application.valueOff')}
              </Typography.Text>
              <Switch checked={checked} onChange={(value) => handleToggle(item.field, value)} />
            </div>
          </div>
        )
      })}
    </div>
  )
}
