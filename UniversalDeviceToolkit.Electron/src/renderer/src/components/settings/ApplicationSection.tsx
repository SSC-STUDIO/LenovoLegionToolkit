import { useEffect, useState } from 'react'
import { Switch, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { softwareApi, type SoftwareDisablerApp, type SoftwareStatus } from '../../api/software'
import { useSettingsStore } from '../../stores/settingsStore'
import { SettingsCard } from './SettingsCard'
import HardwareSensorSectionsModal from './HardwareSensorSectionsModal'

type AppSettings = Record<string, unknown>

interface ToggleItem {
  field: string
  labelKey: string
  descKey: string
}

const TOGGLE_ITEMS: ToggleItem[] = [
  {
    field: 'Autorun',
    labelKey: 'settings.application.autorun',
    descKey: 'settings.application.autorunDesc'
  },
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

/** WPF SettingsApplicationBehaviorControl software disabler cards. */
const DISABLER_ITEMS: { app: SoftwareDisablerApp; labelKey: string; descKey: string; errorKey: string }[] = [
  {
    app: 'vantage',
    labelKey: 'settings.application.disableVantage',
    descKey: 'settings.application.disableVantageDesc',
    errorKey: 'settingsPagedisableVantageerrortitle'
  },
  {
    app: 'legionZone',
    labelKey: 'settings.application.disableLegionZone',
    descKey: 'settings.application.disableLegionZoneDesc',
    errorKey: 'settingsPagedisableLegionZoneerrortitle'
  },
  {
    app: 'fnKeys',
    labelKey: 'settings.application.disableLenovoHotkeys',
    descKey: 'settings.application.disableLenovoHotkeysDesc',
    errorKey: 'settingsPagedisableLenovoHotkeyserrortitle'
  }
]

interface DisablerStatus {
  status: SoftwareStatus
  visible: boolean
  pending: boolean
}

export default function ApplicationSection(): React.JSX.Element {
  const { t } = useTranslation()
  const { scopes, load, setScope } = useSettingsStore()
  const [sensorSectionsOpen, setSensorSectionsOpen] = useState(false)
  const [disablers, setDisablers] = useState<Record<string, DisablerStatus>>({})

  useEffect(() => {
    let cancelled = false
    for (const item of DISABLER_ITEMS) {
      softwareApi
        .getStatus(item.app)
        .then((result) => {
          if (cancelled) return
          setDisablers((prev) => ({
            ...prev,
            [item.app]: {
              status: result.status,
              visible: result.isLegionMachine && result.status !== 'NotFound',
              pending: false
            }
          }))
        })
        .catch(() => {
          if (cancelled) return
          setDisablers((prev) => ({ ...prev, [item.app]: { status: 'NotFound', visible: false, pending: false } }))
        })
    }
    return () => {
      cancelled = true
    }
  }, [])

  const handleDisablerToggle = (app: SoftwareDisablerApp, checked: boolean): void => {
    setDisablers((prev) => ({ ...prev, [app]: { ...prev[app], pending: true } }))
    softwareApi
      .setEnabled(app, checked)
      .then((result) => {
        setDisablers((prev) => ({
          ...prev,
          [app]: { status: result.status, visible: true, pending: false }
        }))
      })
      .catch(() => {
        setDisablers((prev) => ({ ...prev, [app]: { ...prev[app], pending: false } }))
        const item = DISABLER_ITEMS.find((i) => i.app === app)
        message.error(t(item?.errorKey ?? 'settings.saveFailed', { defaultValue: t('settings.saveFailed') }))
      })
  }

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

  const hardwareSensorsEnabled = readBoolean(app, 'EnableHardwareSensors')

  return (
    <div className="udt-settings-section udt-settings-section--application">
      {TOGGLE_ITEMS.map((item) => (
        <SettingsCard
          key={item.field}
          title={t(item.labelKey)}
          description={t(item.descKey)}
          action={
            <Switch
              className="udt-settings-switch"
              checked={readBoolean(app, item.field)}
              onChange={(value) => handleToggle(item.field, value)}
            />
          }
        />
      ))}
      <SettingsCard
        title={t('settings.application.sensorSections')}
        description={t('settings.application.sensorSectionsDesc')}
        onClick={hardwareSensorsEnabled ? () => setSensorSectionsOpen(true) : undefined}
      />
      <HardwareSensorSectionsModal
        open={sensorSectionsOpen}
        onClose={() => setSensorSectionsOpen(false)}
        onSaved={() => void load()}
      />
      {DISABLER_ITEMS.map((item) => {
        const status = disablers[item.app]
        if (status === undefined || !status.visible) return null
        return (
          <SettingsCard
            key={item.app}
            title={t(item.labelKey)}
            description={t(item.descKey)}
            action={
              <Switch
                className="udt-settings-switch"
                checked={status.status === 'Disabled'}
                loading={status.pending}
                disabled={status.pending}
                onChange={(value) => handleDisablerToggle(item.app, value)}
              />
            }
          />
        )
      })}
    </div>
  )
}
