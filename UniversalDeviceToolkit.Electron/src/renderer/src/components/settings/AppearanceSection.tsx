import { useEffect } from 'react'
import { ColorPicker, Select, message } from 'antd'
import type { Color } from 'antd/es/color-picker'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { changeLanguage, supportedLanguages } from '../../i18n'
import { useSettingsStore } from '../../stores/settingsStore'
import { useTheme } from '../../theme/useTheme'
import { SettingsCard } from './SettingsCard'

type AppSettings = Record<string, unknown>

type ThemePreference = 'System' | 'Light' | 'Dark'
type TemperatureUnit = 'C' | 'F'

interface AccentColorRGB {
  R: number
  G: number
  B: number
}

const DEFAULT_ACCENT_HEX = '#ff2121'

const LANGUAGE_OPTIONS: { value: (typeof supportedLanguages)[number]; label: string }[] = [
  { value: 'zh-CN', label: '简体中文' },
  { value: 'en-US', label: 'English' }
]

const THEME_OPTIONS: { value: ThemePreference; labelKey: string; previewClass: string }[] = [
  { value: 'Light', labelKey: 'settings.appearance.themeOptions.light', previewClass: 'udt-theme-option--light' },
  { value: 'Dark', labelKey: 'settings.appearance.themeOptions.dark', previewClass: 'udt-theme-option--dark' },
  { value: 'System', labelKey: 'settings.appearance.themeOptions.system', previewClass: 'udt-theme-option--system' }
]

const TEMPERATURE_UNIT_OPTIONS: { value: TemperatureUnit; label: string }[] = [
  { value: 'C', label: '°C' },
  { value: 'F', label: '°F' }
]

const APP_SCALE_OPTIONS = [80, 90, 100, 110, 125] as const

const ACCENT_PRESETS = [
  '#ff2121',
  '#ff7a00',
  '#ffc53d',
  '#52c41a',
  '#13c2c2',
  '#1677ff',
  '#722ed1',
  '#eb2f96'
]

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

function hexToAccentColor(hex: string): AccentColorRGB {
  const normalized = hex.replace('#', '')
  const value = Number.parseInt(normalized, 16)
  return { R: (value >> 16) & 0xff, G: (value >> 8) & 0xff, B: value & 0xff }
}

function ThemePreviewCard({
  option,
  selected,
  label,
  onClick
}: {
  option: (typeof THEME_OPTIONS)[number]
  selected: boolean
  label: string
  onClick: () => void
}): React.JSX.Element {
  return (
    <button
      type="button"
      className={`udt-theme-option${selected ? ' udt-theme-option--selected' : ''}`}
      onClick={onClick}
    >
      <span className={`udt-theme-option__preview ${option.previewClass}`}>
        <span className="udt-theme-option__bar">
          <span className="udt-theme-option__dot udt-theme-option__dot--red" />
          <span className="udt-theme-option__dot udt-theme-option__dot--yellow" />
          <span className="udt-theme-option__dot udt-theme-option__dot--green" />
        </span>
        <span className="udt-theme-option__body">
          <span className="udt-theme-option__sidebar">
            <span className="udt-theme-option__sline" />
            <span className="udt-theme-option__sline" />
            <span className="udt-theme-option__sline" />
          </span>
          <span className="udt-theme-option__content">
            <span className="udt-theme-option__cline udt-theme-option__cline--search" />
            <span className="udt-theme-option__cline" />
            <span className="udt-theme-option__cline" />
          </span>
        </span>
        <span className="udt-theme-option__dock">
          <span className="udt-theme-option__swatch" />
          <span className="udt-theme-option__swatch" />
          <span className="udt-theme-option__swatch" />
        </span>
      </span>
      <span className="udt-theme-option__label">{label}</span>
    </button>
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

  const persistAccent = (hex: string, rgb: AccentColorRGB): void => {
    setAccent(hex)
    const next: AppSettings = { ...app, AccentColor: rgb }
    setScope('application', next)
    settingsApi
      .set('application', next)
      .then(() => settingsApi.save(['application']))
      .catch(() => message.error(t('settings.saveFailed')))
  }

  const handleAccentPreset = (hex: string): void => {
    persistAccent(hex, hexToAccentColor(hex))
  }

  const handleAccentChange = (value: Color, css: string): void => {
    const rgb = value.toRgb()
    persistAccent(css, { R: rgb.r, G: rgb.g, B: rgb.b })
  }

  const handleAppScaleChange = (value: number): void => {
    const next: AppSettings = { ...app, AppScale: value }
    setScope('application', next)
    settingsApi
      .set('application', next)
      .then(() => settingsApi.save(['application']))
      .catch(() => message.error(t('settings.saveFailed')))
  }

  const selectedTheme = readThemePreference(app)

  return (
    <div className="udt-settings-section udt-settings-section--appearance">
      <SettingsCard
        title={t('settings.appearance.language')}
        description={t('settings.appearance.languageDesc')}
        action={
          <Select<string>
            className="udt-settings-select"
            value={i18n.language.startsWith('zh') ? 'zh-CN' : 'en-US'}
            options={LANGUAGE_OPTIONS}
            onChange={handleLanguageChange}
          />
        }
      />
      <SettingsCard
        title={t('settings.appearance.temperature')}
        description={t('settings.appearance.temperatureDesc')}
        action={
          <Select<TemperatureUnit>
            className="udt-settings-select"
            value={readTemperatureUnit(app)}
            options={TEMPERATURE_UNIT_OPTIONS}
            onChange={handleTemperatureUnitChange}
          />
        }
      />
      <SettingsCard title={t('settings.appearance.theme')}>
        <div className="udt-theme-options">
          {THEME_OPTIONS.map((option) => (
            <ThemePreviewCard
              key={option.value}
              option={option}
              selected={selectedTheme === option.value}
              label={t(option.labelKey)}
              onClick={() => handleThemeChange(option.value)}
            />
          ))}
        </div>
      </SettingsCard>
      <SettingsCard
        title={t('settings.appearance.accentColor')}
        description={t('settings.appearance.accentColorDesc')}
      >
        <div className="udt-settings-swatches">
          {ACCENT_PRESETS.map((preset) => (
            <button
              key={preset}
              type="button"
              className={`udt-settings-swatch${accentHex?.toLowerCase() === preset ? ' udt-settings-swatch--selected' : ''}`}
              style={{ background: preset }}
              aria-label={preset}
              title={preset}
              onClick={() => handleAccentPreset(preset)}
            />
          ))}
          <ColorPicker
            key={accentHex ?? 'none'}
            value={accentHex ?? DEFAULT_ACCENT_HEX}
            onChange={handleAccentChange}
            disabledAlpha
            showText={false}
          >
            <button type="button" className="udt-settings-swatch udt-settings-swatch--custom" title={t('settings.appearance.accentColor')} />
          </ColorPicker>
        </div>
      </SettingsCard>
      <SettingsCard
        title={t('settings.appearance.appScale')}
        description={t('settings.appearance.appScaleDesc')}
        action={
          <Select<number>
            className="udt-settings-select"
            value={appScale}
            options={APP_SCALE_OPTIONS.map((value) => ({ value, label: `${value}%` }))}
            onChange={handleAppScaleChange}
          />
        }
      />
    </div>
  )
}
