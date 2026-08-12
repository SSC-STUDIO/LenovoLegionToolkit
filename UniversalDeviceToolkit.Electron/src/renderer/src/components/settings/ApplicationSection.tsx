import { useEffect, useState } from 'react'
import { Select, Switch, message } from 'antd'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { appApi } from '../../api/app'
import { softwareApi, type SoftwareDisablerApp, type SoftwareStatus } from '../../api/software'
import { startupApi, type AutorunState } from '../../api/startup'
import { useSettingsStore } from '../../stores/settingsStore'
import { SettingsCard } from './SettingsCard'
import HardwareSensorSectionsModal from './HardwareSensorSectionsModal'

type AppSettings = Record<string, unknown>

const SENSOR_REFRESH_INTERVALS = [1, 2, 3, 5]

const AUTORUN_OPTIONS: { value: AutorunState; labelKey: string }[] = [
  { value: 'Enabled', labelKey: 'settings.application.autorunOptions.enabled' },
  { value: 'EnabledDelayed', labelKey: 'settings.application.autorunOptions.enabledDelayed' },
  { value: 'Disabled', labelKey: 'settings.application.autorunOptions.disabled' }
]

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
    // WPF Autorun writes the registry Run key; Electron applies the login item.
    if (field === 'Autorun') {
      appApi
        .setAutorun(checked)
        .catch(() => message.error(t('settings.saveFailed')))
    }
  }

  const hardwareSensorsEnabled = readBoolean(app, 'EnableHardwareSensors')
  const [autorunState, setAutorunState] = useState<AutorunState>('Disabled')
  const [autorunLoaded, setAutorunLoaded] = useState(false)

  useEffect(() => {
    let cancelled = false
    startupApi
      .getAutorun()
      .then((result) => {
        if (cancelled) return
        setAutorunState(result.state)
      })
      .catch(() => undefined)
      .finally(() => {
        if (!cancelled) setAutorunLoaded(true)
      })
    return () => {
      cancelled = true
    }
  }, [])

  const handleAutorunChange = (state: AutorunState): void => {
    const previous = autorunState
    setAutorunState(state)
    startupApi
      .setAutorun(state)
      .then((result) => {
        setAutorunState(result.state)
        setScope('application', { ...app, Autorun: result.state })
      })
      .catch(() => {
        setAutorunState(previous)
        message.error(t('settings.saveFailed'))
      })
  }
  const dashboardScope =
    typeof scopes.dashboard === 'object' && scopes.dashboard !== null
      ? (scopes.dashboard as AppSettings)
      : {}
  const sensorRefreshInterval =
    typeof dashboardScope['SensorsRefreshIntervalSeconds'] === 'number' &&
    Number.isFinite(dashboardScope['SensorsRefreshIntervalSeconds']) &&
    (dashboardScope['SensorsRefreshIntervalSeconds'] as number) >= 1
      ? (dashboardScope['SensorsRefreshIntervalSeconds'] as number)
      : 1

  const handleSensorIntervalChange = (value: number): void => {
    const next: AppSettings = { ...dashboardScope, SensorsRefreshIntervalSeconds: value }
    setScope('dashboard', next)
    settingsApi
      .set('dashboard', next)
      .then(() => settingsApi.save(['dashboard']))
      .catch(() => message.error(t('settings.saveFailed')))
  }

  const startupFields = TOGGLE_ITEMS.filter((item) =>
    ['MinimizeToTray', 'MinimizeOnClose'].includes(item.field)
  )
  const sensorFields = TOGGLE_ITEMS.filter((item) => item.field === 'EnableHardwareSensors')
  const noticeFields = TOGGLE_ITEMS.filter((item) =>
    ['DisableUnsupportedHardwareWarning', 'DontShowNotifications', 'ExtensionsEnabled'].includes(item.field)
  )

  return (
    <div className="udt-settings-section udt-settings-section--application">
      <div className="udt-settings-group-title">{t('settings.application.groupStartup')}</div>
      <SettingsCard
        title={t('settings.application.autorun')}
        description={t('settings.application.autorunDesc')}
        action={
          <Select<AutorunState>
            className="udt-settings-select"
            value={autorunLoaded ? autorunState : undefined}
            loading={!autorunLoaded}
            onChange={handleAutorunChange}
            options={AUTORUN_OPTIONS.map((option) => ({
              value: option.value,
              label: t(option.labelKey)
            }))}
          />
        }
      />
      {startupFields.map((item) => (
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
        title={t('settings.application.animationsEnabled')}
        description={t('settings.application.animationsEnabledDesc')}
        action={
          <Switch
            className="udt-settings-switch"
            checked={readBoolean(app, 'AnimationsEnabled')}
            onChange={(value) => {
              handleToggle('AnimationsEnabled', value)
              document.documentElement.classList.toggle('udt-animations-off', !value)
            }}
          />
        }
      />

      <div className="udt-settings-group-title">{t('settings.application.groupSensors')}</div>
      {sensorFields.map((item) => (
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
      <SettingsCard
        title={t('settings.application.sensorRefreshInterval')}
        description={t('settings.application.sensorRefreshIntervalDesc')}
        action={
          <Select<number>
            className="udt-settings-select"
            value={sensorRefreshInterval}
            onChange={handleSensorIntervalChange}
            options={SENSOR_REFRESH_INTERVALS.map((seconds) => ({
              value: seconds,
              label: `${seconds} s`
            }))}
          />
        }
      />
      <HardwareSensorSectionsModal
        open={sensorSectionsOpen}
        onClose={() => setSensorSectionsOpen(false)}
        onSaved={() => void load()}
      />

      <div className="udt-settings-group-title">{t('settings.application.groupNotifications')}</div>
      {noticeFields.map((item) => (
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

      <div className="udt-settings-group-title">{t('settings.application.groupSoftware')}</div>
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
