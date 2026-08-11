import { useEffect } from 'react'
import { ColorPicker, Select, message } from 'antd'
import type { Color } from 'antd/es/color-picker'
import { useTranslation } from 'react-i18next'
import { settingsApi } from '../../api/settings'
import { LANGUAGES, changeLanguage } from '../../i18n'
import { useSettingsStore } from '../../stores/settingsStore'
import { UI_SCALE_OPTIONS } from '../../stores/themeStore'
import { useTheme } from '../../theme/useTheme'
import { SettingsCard } from './SettingsCard'

type AppSettings = Record<string, unknown>

type ThemePreference = 'System' | 'Light' | 'Dark'

/**
 * Temperature unit preference for the sensor dashboard.
 *
 * The value is persisted both to the backend 'application' scope (kept for the
 * host-side consumers such as the OSD / status tray) and to localStorage
 * 'udt-temperature-unit' so the renderer can read it synchronously. Sensor
 * sections should use getTemperatureUnit() below to format values.
 */
export type TemperatureUnit = 'C' | 'F'

const TEMPERATURE_UNIT_STORAGE_KEY = 'udt-temperature-unit'

/** Returns the current temperature unit ('C' or 'F'). */
export function getTemperatureUnit(): TemperatureUnit {
  try {
    return localStorage.getItem(TEMPERATURE_UNIT_STORAGE_KEY) === 'F' ? 'F' : 'C'
  } catch {
    return 'C'
  }
}

interface AccentColorRGB {
  R: number
  G: number
  B: number
}

const DEFAULT_ACCENT_HEX = '#ff2121'

/** All selectable languages; the current one is sorted to the top. */
const LANGUAGE_OPTIONS = LANGUAGES.map((language) => ({
  value: language.code,
  label: language.name
}))

const THEME_OPTIONS: { value: ThemePreference; labelKey: string; previewClass: string }[] = [
  { value: 'Light', labelKey: 'settings.appearance.themeOptions.light', previewClass: 'udt-theme-option--light' },
  { value: 'Dark', labelKey: 'settings.appearance.themeOptions.dark', previewClass: 'udt-theme-option--dark' },
  { value: 'System', labelKey: 'settings.appearance.themeOptions.system', previewClass: 'udt-theme-option--system' }
]

const TEMPERATURE_UNIT_OPTIONS: { value: TemperatureUnit; label: string }[] = [
  { value: 'C', label: '°C' },
  { value: 'F', label: '°F' }
]

/**
 * UI scale levels aligned with the WPF app
 * (Compact 0.90 / Standard 1.0 / Large 1.10 / ExtraLarge 1.25).
 */
const UI_SCALE_OPTIONS_LABELED = UI_SCALE_OPTIONS.map((value) => ({
  value,
  label: `${Math.round(value * 100)}%`
}))

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

function readThemePreference(app: AppSettings): ThemePreference {
  const value = readString(app, 'Theme')
  return value === 'Light' || value === 'Dark' ? value : 'System'
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
  const { setThemeMode, setAccent, uiScale, setUiScale } = useTheme()
  const scopes = useSettingsStore((s) => s.scopes)
  const load = useSettingsStore((s) => s.load)
  const setScope = useSettingsStore((s) => s.setScope)

  const rawApp = scopes.application
  const app: AppSettings =
    typeof rawApp === 'object' && rawApp !== null ? (rawApp as AppSettings) : {}

  useEffect(() => {
    void load()
  }, [load])

  // Keep localStorage 'udt-temperature-unit' in sync with the backend value so
  // getTemperatureUnit() reflects the persisted preference from the first run.
  useEffect(() => {
    const backendUnit = app['TemperatureUnit']
    if (backendUnit === 'C' || backendUnit === 'F') {
      localStorage.setItem(TEMPERATURE_UNIT_STORAGE_KEY, backendUnit)
    }
  }, [app['TemperatureUnit']])

  const accentColor = readAccentColor(app)
  const accentHex = accentColor ? accentColorToHex(accentColor) : undefined

  const currentLanguage = LANGUAGE_OPTIONS.some((option) => option.value === i18n.language)
    ? i18n.language
    : 'en'
  const languageOptions = [...LANGUAGE_OPTIONS].sort((a, b) => {
    if (a.value === currentLanguage) return -1
    if (b.value === currentLanguage) return 1
    return a.label.localeCompare(b.label, undefined, { sensitivity: 'base' })
  })

  const handleLanguageChange = (value: string): void => {
    void changeLanguage(value)
  }

  const handleTemperatureUnitChange = (value: TemperatureUnit): void => {
    // Renderer-facing preference (read by sensor sections via getTemperatureUnit).
    localStorage.setItem(TEMPERATURE_UNIT_STORAGE_KEY, value)
    // Keep the backend 'application' scope in sync for host-side consumers.
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

  const handleUiScaleChange = (value: number): void => {
    setUiScale(value)
  }

  const selectedTheme = readThemePreference(app)

  return (
    <div className="udt-settings-section udt-settings-section--appearance">
      <SettingsCard
        title={t('settings.appearance.language')}
        description={t('settings.appearance.languageDesc')}
        action={
          <Select<string>
            className="udt-settings-select udt-settings-select--language"
            value={currentLanguage}
            options={languageOptions}
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
            value={getTemperatureUnit()}
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
            className="udt-settings-select udt-settings-select--scale"
            value={uiScale}
            options={UI_SCALE_OPTIONS_LABELED}
            onChange={handleUiScaleChange}
          />
        }
      />
    </div>
  )
}
