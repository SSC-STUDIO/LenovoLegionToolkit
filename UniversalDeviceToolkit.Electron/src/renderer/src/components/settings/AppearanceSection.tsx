import { useEffect, useState } from 'react'
import { Checkbox, Select, message } from 'antd'
import { useTranslation } from 'react-i18next'
import ColorPicker from '../ColorPicker'
import { settingsApi } from '../../api/settings'
import { systemApi } from '../../api/system'
import { LANGUAGES, changeLanguage } from '../../i18n'
import { useSettingsStore } from '../../stores/settingsStore'
import { UI_SCALE_OPTIONS, useThemeStore } from '../../stores/themeStore'
import { storeAccentPreference } from '../../theme/useTheme'
import {
  applyAccentSurfacePalette,
  clearAccentSurfacePalette,
  createAccentPalette
} from '../../theme/accentPalette'
import { SettingsCard } from './SettingsCard'

type AppSettings = Record<string, unknown>

type ThemePreference = 'System' | 'Light' | 'Dark'
type AccentColorSource = 'System' | 'Custom'

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

/** Fallback when Windows accent cannot be read (AccentColorPresets.Swatches[0]). */
const DEFAULT_SYSTEM_ACCENT_HEX = '#0078d4'

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
 * UI scale levels aligned with the Electron app
 * (Compact 0.90 / Standard 1.0 / Large 1.10 / ExtraLarge 1.25).
 */
const UI_SCALE_OPTIONS_LABELED = UI_SCALE_OPTIONS.map((value) => ({
  value,
  label: `${Math.round(value * 100)}%`
}))

/**
 * Solid accent presets from Lib Theme/AccentColorPresets.cs (system rainbow is separate).
 */
const ACCENT_PRESETS: { hex: string; key: string }[] = [
  { hex: '#0078d4', key: 'Blue' },
  { hex: '#b146c2', key: 'Purple' },
  { hex: '#e3008c', key: 'Pink' },
  { hex: '#e81123', key: 'Red' },
  { hex: '#f7630c', key: 'Orange' },
  { hex: '#ffb900', key: 'Amber' },
  { hex: '#107c10', key: 'Green' },
  { hex: '#808080', key: 'Gray' }
]

const SYSTEM_ACCENT_GRADIENT =
  'linear-gradient(135deg, #f13b50 0%, #742ac4 28%, #1a98f2 52%, #06d3a5 76%, #ffd62e 100%)'

function readString(app: AppSettings, key: string): string | undefined {
  const value = app[key]
  return typeof value === 'string' ? value : undefined
}

function readBool(app: AppSettings, key: string, fallback: boolean): boolean {
  const value = app[key]
  return typeof value === 'boolean' ? value : fallback
}

function readThemePreference(app: AppSettings): ThemePreference {
  const value = readString(app, 'Theme')
  return value === 'Light' || value === 'Dark' ? value : 'System'
}

function readAccentColorSource(app: AppSettings): AccentColorSource {
  return readString(app, 'AccentColorSource') === 'Custom' ? 'Custom' : 'System'
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

function colorsEqual(a: AccentColorRGB | undefined, hex: string): boolean {
  if (a == null) return false
  return accentColorToHex(a).toLowerCase() === hex.toLowerCase()
}

function isPresetHex(hex: string): boolean {
  return ACCENT_PRESETS.some((preset) => preset.hex.toLowerCase() === hex.toLowerCase())
}

function EyedropperIcon(): React.JSX.Element {
  return (
    <svg
      className="udt-settings-swatch__eyedropper-icon"
      viewBox="0 0 24 24"
      width="18"
      height="18"
      aria-hidden="true"
      focusable="false"
    >
      <path
        fill="currentColor"
        d="M11.1 16.2 7.8 19.5a2.1 2.1 0 0 1-3-3l3.3-3.3 3 3Zm8.2-12.5a2.2 2.2 0 0 0-3.1 0l-1.5 1.5 3.1 3.1 1.5-1.5a2.2 2.2 0 0 0 0-3.1Zm-4.6 2.5L6.2 16.7l1.1 1.1 8.5-8.5-1.1-1.1Z"
      />
    </svg>
  )
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
  // Electron theme preview: Light/Dark cards render two identical panes; the System
  // card is split - one light half and one dark half.
  const isSystem = option.value === 'System'
  const singleMode: 'light' | 'dark' = option.value === 'Light' ? 'light' : 'dark'
  const panes: ('light' | 'dark')[] = isSystem ? ['light', 'dark'] : [singleMode, singleMode]

  const renderPane = (mode: 'light' | 'dark', index: number): React.JSX.Element => (
    <span key={index} className={`udt-theme-option__pane udt-theme-option__pane--${mode}`}>
      <span className="udt-theme-option__bar">
        {index === 0 ? (
          <>
            <span className="udt-theme-option__dot udt-theme-option__dot--red" />
            <span className="udt-theme-option__dot udt-theme-option__dot--yellow" />
            <span className="udt-theme-option__dot udt-theme-option__dot--green" />
          </>
        ) : (
          <span className="udt-theme-option__bar-blank" />
        )}
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
  )

  return (
    <button
      type="button"
      className={`udt-theme-option ${option.previewClass}${selected ? ' udt-theme-option--selected' : ''}`}
      onClick={onClick}
      aria-pressed={selected}
    >
      <span className="udt-theme-option__preview">
        <span className="udt-theme-option__split">{panes.map(renderPane)}</span>
      </span>
      <span className="udt-theme-option__label">{label}</span>
    </button>
  )
}

export default function AppearanceSection(): React.JSX.Element {
  const { t, i18n } = useTranslation()
  const setThemeMode = useThemeStore((s) => s.setThemeMode)
  const themeMode = useThemeStore((s) => s.themeMode)
  const setAccent = useThemeStore((s) => s.setAccent)
  const uiScale = useThemeStore((s) => s.uiScale)
  const setUiScale = useThemeStore((s) => s.setUiScale)
  const scopes = useSettingsStore((s) => s.scopes)
  const load = useSettingsStore((s) => s.load)
  const setScope = useSettingsStore((s) => s.setScope)
  const [systemAccentHex, setSystemAccentHex] = useState(DEFAULT_SYSTEM_ACCENT_HEX)

  const rawApp = scopes.application
  const app: AppSettings =
    typeof rawApp === 'object' && rawApp !== null ? (rawApp as AppSettings) : {}

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    let cancelled = false
    systemApi
      .getAccentColor()
      .then((color) => {
        if (!cancelled) {
          setSystemAccentHex(accentColorToHex({ R: color.r, G: color.g, B: color.b }))
        }
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [])

  // Keep localStorage 'udt-temperature-unit' in sync with the backend value so
  // getTemperatureUnit() reflects the persisted preference from the first run.
  useEffect(() => {
    const backendUnit = app['TemperatureUnit']
    if (backendUnit === 'C' || backendUnit === 'F') {
      localStorage.setItem(TEMPERATURE_UNIT_STORAGE_KEY, backendUnit)
    }
  }, [app['TemperatureUnit']])

  const accentSource = readAccentColorSource(app)
  const accentColor = readAccentColor(app)
  const accentHex = accentColor ? accentColorToHex(accentColor) : undefined
  const applyAccentToSystem = readBool(app, 'ApplyAccentColorToSystem', true)
  const applyAccentToTheme = readBool(app, 'ApplyAccentColorToTheme', true)
  const customPickerSelected =
    accentSource === 'Custom' && accentHex != null && !isPresetHex(accentHex)

  const currentLanguage = LANGUAGE_OPTIONS.some((option) => option.value === i18n.language)
    ? i18n.language
    : 'en'
  const languageOptions = [...LANGUAGE_OPTIONS].sort((a, b) => {
    if (a.value === currentLanguage) return -1
    if (b.value === currentLanguage) return 1
    return a.label.localeCompare(b.label, undefined, { sensitivity: 'base' })
  })

  const persistApplication = (next: AppSettings): void => {
    setScope('application', next)
    settingsApi
      .set('application', next)
      .then(() => settingsApi.save(['application']))
      .catch(() => message.error(t('settings.saveFailed')))
  }

  /**
   * Push the resolved accent into the theme store immediately.
   * Matches Electron ThemeManager.SetColor: the accent always applies to
   * --udt-accent / Ant Design colorPrimary. ApplyAccentColorToTheme only
   * controls the tinted surface palette (ThemeStylePreset), not the accent.
   */
  const applyAccentToUi = (next: AppSettings): void => {
    const source = readAccentColorSource(next)
    if (source === 'System') {
      setAccent(systemAccentHex)
      return
    }
    const custom = readAccentColor(next)
    setAccent(custom ? accentColorToHex(custom) : systemAccentHex)
  }

  /** When the theme-style checkbox is on, picking an accent resets to Default (Electron). */
  const themeStylePresetForAccentPick = (): string | undefined =>
    applyAccentToTheme ? 'Default' : undefined

  const applyAccentToWindowsIfEnabled = (rgb: AccentColorRGB, enabled: boolean): void => {
    if (!enabled) return
    void systemApi.setAccentColor({ r: rgb.R, g: rgb.G, b: rgb.B }).catch(() => undefined)
  }

  const handleLanguageChange = (value: string): void => {
    void changeLanguage(value)
  }

  const handleTemperatureUnitChange = (value: TemperatureUnit): void => {
    localStorage.setItem(TEMPERATURE_UNIT_STORAGE_KEY, value)
    persistApplication({ ...app, TemperatureUnit: value })
  }

  const handleThemeChange = (value: ThemePreference): void => {
    // Persist the renderer-side choice (useTheme.storedThemePreference() reads
    // it and wins over the async host value - same protection as the accent).
    try {
      localStorage.setItem('udt.theme', value === 'System' ? 'system' : value === 'Dark' ? 'dark' : 'light')
    } catch {
      // ignore
    }
    if (value === 'System') {
      setThemeMode(window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light')
    } else {
      setThemeMode(value === 'Dark' ? 'dark' : 'light')
    }
    persistApplication({ ...app, Theme: value })
  }

  const handleApplyAccentToSystemChange = (checked: boolean): void => {
    const next: AppSettings = { ...app, ApplyAccentColorToSystem: checked }
    persistApplication(next)
    if (checked && accentSource === 'Custom' && accentColor != null) {
      applyAccentToWindowsIfEnabled(accentColor, true)
    }
  }

  /**
   * Applies or clears the accent-tinted surface palette immediately, matching
   * Electron ThemeManager.ApplyStylePreset gated by ApplyAccentColorToTheme.
   */
  const applyAccentSurfaceTint = (enabled: boolean, hex?: string): void => {
    if (enabled && hex) {
      applyAccentSurfacePalette(createAccentPalette(hex, themeMode === 'dark'))
    } else {
      clearAccentSurfacePalette()
    }
  }

  const handleApplyAccentToThemeChange = (checked: boolean): void => {
    // Palette tint gate only - do not clear/reapply the accent itself (Electron parity).
    const next: AppSettings = { ...app, ApplyAccentColorToTheme: checked }
    if (checked && accentSource === 'Custom') {
      next.ThemeStylePreset = 'Default'
    }
    persistApplication(next)
    applyAccentSurfaceTint(checked, accentHex ?? systemAccentHex)
  }

  const handleSystemAccent = (): void => {
    const next: AppSettings = {
      ...app,
      AccentColorSource: 'System',
      ...(themeStylePresetForAccentPick() != null
        ? { ThemeStylePreset: themeStylePresetForAccentPick() }
        : {})
    }
    persistApplication(next)
    storeAccentPreference('System')
    applyAccentToUi(next)
    applyAccentSurfaceTint(applyAccentToTheme, systemAccentHex)
  }

  const handleAccentPreset = (hex: string): void => {
    const rgb = hexToAccentColor(hex)
    const next: AppSettings = {
      ...app,
      AccentColorSource: 'Custom',
      AccentColor: rgb,
      ...(themeStylePresetForAccentPick() != null
        ? { ThemeStylePreset: themeStylePresetForAccentPick() }
        : {})
    }
    persistApplication(next)
    storeAccentPreference('Custom', hex)
    applyAccentToUi(next)
    applyAccentToWindowsIfEnabled(rgb, applyAccentToSystem)
    applyAccentSurfaceTint(applyAccentToTheme, hex)
  }

  const handleCustomAccent = (hex: string): void => {
    const rgb = hexToAccentColor(hex)
    const next: AppSettings = {
      ...app,
      AccentColorSource: 'Custom',
      AccentColor: rgb,
      ...(themeStylePresetForAccentPick() != null
        ? { ThemeStylePreset: themeStylePresetForAccentPick() }
        : {})
    }
    persistApplication(next)
    storeAccentPreference('Custom', hex)
    applyAccentToUi(next)
    applyAccentToWindowsIfEnabled(rgb, applyAccentToSystem)
    applyAccentSurfaceTint(applyAccentToTheme, hex)
  }

  const handleUiScaleChange = (value: number): void => {
    setUiScale(value)
  }

  const selectedTheme = readThemePreference(app)
  const customPickerValue = accentHex ?? systemAccentHex

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
      <SettingsCard title={t('wpf.settingsPagethemeModetitle', { defaultValue: t('settings.appearance.theme') })}>
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

        <div className="udt-theme-accent-options">
          <Checkbox
            checked={applyAccentToSystem}
            onChange={(event) => handleApplyAccentToSystemChange(event.target.checked)}
          >
            {t('wpf.settingsPageapplyAccentColorToThemetitle')}
          </Checkbox>
          <Checkbox
            checked={applyAccentToTheme}
            onChange={(event) => handleApplyAccentToThemeChange(event.target.checked)}
          >
            {t('wpf.settingsPageapplyAccentColorToThemeStyletitle')}
          </Checkbox>
        </div>

        <div className="udt-theme-accent-divider" role="separator" />

        <div className="udt-theme-accent-title">
          {t('wpf.settingsPageaccentColorPresetstitle', {
            defaultValue: t('settings.appearance.accentColor')
          })}
        </div>
        <div className="udt-settings-swatches">
          <button
            type="button"
            className={`udt-settings-swatch${accentSource === 'System' ? ' udt-settings-swatch--selected' : ''}`}
            style={{ background: SYSTEM_ACCENT_GRADIENT }}
            aria-label={t('settings.appearance.accentColorSource.system', {
              defaultValue: 'System'
            })}
            title={t('settings.appearance.accentColorSource.system', { defaultValue: 'System' })}
            onClick={handleSystemAccent}
          />
          {ACCENT_PRESETS.map((preset) => {
            const selected =
              accentSource === 'Custom' && colorsEqual(accentColor, preset.hex)
            return (
              <button
                key={preset.key}
                type="button"
                className={`udt-settings-swatch${selected ? ' udt-settings-swatch--selected' : ''}`}
                style={{ background: preset.hex }}
                aria-label={preset.key}
                title={preset.key}
                onClick={() => handleAccentPreset(preset.hex)}
              />
            )
          })}
          <div
            className={`udt-settings-swatch-picker${
              customPickerSelected ? ' udt-settings-swatch-picker--selected' : ''
            }`}
          >
            <ColorPicker
              value={customPickerValue}
              size={40}
              tooltip={t('settings.appearance.accentColorSource.custom', {
                defaultValue: 'Custom'
              })}
              onChangeDelayed={handleCustomAccent}
            >
              <EyedropperIcon />
            </ColorPicker>
          </div>
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
