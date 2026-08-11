import { useEffect } from 'react'
import { ColorPicker, Radio, Select, Typography, message } from 'antd'
import type { Color } from 'antd/es/color-picker'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { changeLanguage, supportedLanguages } from '../../i18n'
import { useSettingsStore } from '../../stores/settingsStore'
import { useTheme } from '../../theme/useTheme'

type AppSettings = Record<string, unknown>

type ThemePreference = 'System' | 'Light' | 'Dark'
type TemperatureUnit = 'C' | 'F'

interface AccentColorRGB {
  R: number
  G: number
  B: number
}

const LANGUAGE_OPTIONS: { value: (typeof supportedLanguages)[number]; label: string }[] = [
  { value: 'zh-CN', label: '简体中文' },
  { value: 'en-US', label: 'English' }
]

const THEME_OPTIONS: { value: ThemePreference; labelKey: string }[] = [
  { value: 'System', labelKey: 'settings.appearance.themeOptions.system' },
  { value: 'Light', labelKey: 'settings.appearance.themeOptions.light' },
  { value: 'Dark', labelKey: 'settings.appearance.themeOptions.dark' }
]

const TEMPERATURE_UNIT_OPTIONS: { value: TemperatureUnit; label: string }[] = [
  { value: 'C', label: '°C' },
  { value: 'F', label: '°F' }
]

const APP_SCALE_OPTIONS = [80, 90, 100, 110, 125] as const

function readString(app: AppSettings, key: string): string | undefined {
  const value = app[key]
  return typeof value === 'string' ? value : undefined
}

function readNumber(app: AppSettings, key: string): number | undefined {
  const value = app[key]
  return typeof value === 'number' && Number.isFinite(value) ? value : undefined
}

function readThemePreference(app: AppSettings): ThemePreference {
  const value = readString(app, 'Theme')
  return value === 'Light' || value === 'Dark' ? value : 'System'
}

function readTemperatureUnit(app: AppSettings): TemperatureUnit {
  return readString(app, 'TemperatureUnit') === 'F' ? 'F' : 'C'
}

function readAccentColor(app: AppSettings): AccentColorRGB | undefined {
  const value = app['AccentColor']
  if (
    value != null &&
    typeof value === 'object' &&
    typeof (value as AccentColorRGB).R === 'number' &&
    typeof (value as AccentColorRGB).G === 'number' &&
    typeof (value as AccentColorRGB).B === 'number'
  ) {
    return value as AccentColorRGB
  }
  return undefined
}

function accentColorToHex(color: AccentColorRGB): string {
  const toHex = (value: number): string => value.toString(16).padStart(2, '0')
  return `#${toHex(color.R)}${toHex(color.G)}${toHex(color.B)}`
}

function SettingRow({
  label,
  control
}: {
  label: string
  control: React.JSX.Element
}): React.JSX.Element {
  return (
    <div className="udt-settings-card-row">
      <Typography.Text className="udt-settings-card-row__label">{label}</Typography.Text>
      <div className="udt-settings-card-row__control">{control}</div>
    </div>
  )
}

export default function AppearanceSection(): React.JSX.Element {
  const { t, i18n } = useTranslation()
  const { setThemeMode, setAccent } = useTheme()
  const scopes = useSettingsStore((s) => s.scopes)
  const load = useSettingsStore((s) => s.load)
  const setScope = useSettingsStore((s) => s.setScope)

  const rawApp = scopes.application
  const app: AppSettings =
    typeof rawApp === 'object' && rawApp !== null ? (rawApp as AppSettings) : {}

  useEffect(() => {
    void load()
  }, [load])

  const accentColor = readAccentColor(app)
  const accentHex = accentColor ? accentColorToHex(accentColor) : undefined

  const storedScale = readNumber(app, 'AppScale')
  const appScale =
    storedScale != null && (APP_SCALE_OPTIONS as readonly number[]).includes(storedScale)
      ? storedScale
      : 100

  const handleLanguageChange = (value: string): void => {
    localStorage.setItem('udt.lang', value)
    void changeLanguage(value)
  }

  const handleTemperatureUnitChange = (value: TemperatureUnit): void => {
    const next: AppSettings = { ...app, TemperatureUnit: value }
    setScope('application', next)
    settingsApi
      .set('application', next)
      .then(() => settingsApi.save(['application']))
      .catch(() => message.error(t('settings.saveFailed')))
  }

  const handleThemeChange = (value: ThemePreference): void => {
    localStorage.removeItem('udt.theme')
    if (value === 'System') {
      setThemeMode(window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
    } else {
      setThemeMode(value === 'Dark' ? 'dark' : 'light')
    }
    const next: AppSettings = { ...app, Theme: value }
    setScope('application', next)
    settingsApi
      .set('application', next)
      .then(() => settingsApi.save(['application']))
      .catch(() => message.error(t('settings.saveFailed')))
  }

  const handleAccentChange = (value: Color, css: string): void => {
    setAccent(css)
    const rgb = value.toRgb()
    const next: AppSettings = { ...app, AccentColor: { R: rgb.r, G: rgb.g, B: rgb.b } }
    setScope('application', next)
    settingsApi.set('application', next).catch(() => message.error(t('settings.saveFailed')))
  }

  const handleAppScaleChange = (value: number): void => {
    const next: AppSettings = { ...app, AppScale: value }
    setScope('application', next)
    settingsApi
      .set('application', next)
      .then(() => settingsApi.save(['application']))
      .catch(() => message.error(t('settings.saveFailed')))
  }

  return (
    <div className="udt-settings-section udt-settings-section--appearance">
      <SettingRow
        label={t('settings.appearance.language')}
        control={
          <Select<string>
            value={i18n.language.startsWith('zh') ? 'zh-CN' : 'en-US'}
            options={LANGUAGE_OPTIONS}
            onChange={handleLanguageChange}
            style={{ width: 160 }}
          />
        }
      />
      <SettingRow
        label={t('settings.appearance.temperatureUnit')}
        control={
          <Select<TemperatureUnit>
            value={readTemperatureUnit(app)}
            options={TEMPERATURE_UNIT_OPTIONS}
            onChange={handleTemperatureUnitChange}
            style={{ width: 160 }}
          />
        }
      />
      <SettingRow
        label={t('settings.appearance.theme')}
        control={
          <Radio.Group
            value={readThemePreference(app)}
            options={THEME_OPTIONS.map((option) => ({
              value: option.value,
              label: t(option.labelKey)
            }))}
            onChange={(e) => handleThemeChange(e.target.value as ThemePreference)}
          />
        }
      />
      <SettingRow
        label={t('settings.appearance.accentColor')}
        control={<ColorPicker key={accentHex ?? 'none'} defaultValue={accentHex} onChange={handleAccentChange} />}
      />
      <SettingRow
        label={t('settings.appearance.appScale')}
        control={
          <Select<number>
            value={appScale}
            options={APP_SCALE_OPTIONS.map((value) => ({ value, label: `${value}%` }))}
            onChange={handleAppScaleChange}
            style={{ width: 160 }}
          />
        }
      />
    </div>
  )
}
