import { useEffect } from 'react'
import { settingsApi } from '../api/settings'
import { systemApi } from '../api/system'
import { applyUiScale, useThemeStore } from '../stores/themeStore'
import type { ThemeMode } from '../stores/themeStore'

type ThemePreference = 'System' | 'Light' | 'Dark'
type AccentColorSource = 'System' | 'Custom'

interface ApplicationSettings {
  Theme?: ThemePreference
  AccentColor?: { R: number; G: number; B: number } | null
  AccentColorSource?: AccentColorSource
}

const THEME_STORAGE_KEY = 'udt.theme'
const ACCENT_SOURCE_STORAGE_KEY = 'udt.accent-source'
const ACCENT_COLOR_STORAGE_KEY = 'udt.accent'
const DEFAULT_SYSTEM_ACCENT_HEX = '#0078d4'

/**
 * Renderer-authoritative accent preference. The host application scope may lag
 * behind (async save) or drop unknown fields, so the choice made in the
 * appearance settings is persisted locally and wins over the host value.
 */
export function storeAccentPreference(source: 'System' | 'Custom', hex?: string): void {
  try {
    if (source === 'Custom' && hex) {
      localStorage.setItem(ACCENT_SOURCE_STORAGE_KEY, 'Custom')
      localStorage.setItem(ACCENT_COLOR_STORAGE_KEY, hex)
    } else if (source === 'System') {
      localStorage.setItem(ACCENT_SOURCE_STORAGE_KEY, 'System')
      localStorage.removeItem(ACCENT_COLOR_STORAGE_KEY)
    } else {
      localStorage.removeItem(ACCENT_SOURCE_STORAGE_KEY)
      localStorage.removeItem(ACCENT_COLOR_STORAGE_KEY)
    }
  } catch {
    // localStorage unavailable — the host scope remains the only source.
  }
}

function storedAccentPreference(): { source: 'System' | 'Custom'; hex?: string } | null {
  try {
    const source = localStorage.getItem(ACCENT_SOURCE_STORAGE_KEY)
    if (source === 'Custom') {
      const hex = localStorage.getItem(ACCENT_COLOR_STORAGE_KEY)
      if (hex && /^#[0-9a-f]{6}$/i.test(hex)) return { source: 'Custom', hex }
      return null
    }
    if (source === 'System') return { source: 'System' }
  } catch {
    // ignore
  }
  return null
}

function systemPrefersDark(): boolean {
  return window.matchMedia('(prefers-color-scheme: dark)').matches
}

function rgbToHex(color: { R: number; G: number; B: number }): string {
  const toHex = (value: number): string => value.toString(16).padStart(2, '0')
  return `#${toHex(color.R)}${toHex(color.G)}${toHex(color.B)}`
}

function storedThemePreference(): ThemePreference | null {
  const stored = localStorage.getItem(THEME_STORAGE_KEY)
  if (stored === 'light' || stored === 'dark') return stored === 'light' ? 'Light' : 'Dark'
  if (stored === 'system') return 'System'
  return null
}

export interface ThemeController {
  themeMode: ThemeMode
  colorPrimary?: string
  uiScale: number
  setThemeMode: (mode: ThemeMode) => void
  setAccent: (color?: string) => void
  setUiScale: (scale: number) => void
}

export function useTheme(): ThemeController {
  const themeMode = useThemeStore((s) => s.themeMode)
  const colorPrimary = useThemeStore((s) => s.colorPrimary)
  const uiScale = useThemeStore((s) => s.uiScale)
  const setThemeMode = useThemeStore((s) => s.setThemeMode)
  const setAccent = useThemeStore((s) => s.setAccent)
  const setUiScale = useThemeStore((s) => s.setUiScale)

  // Keep the document scaling in sync with the store (the store also applies
  // the initial scale at module load so the whole app is scaled from launch).
  useEffect(() => {
    applyUiScale(uiScale)
  }, [uiScale])

  useEffect(() => {
    let disposed = false
    let preference: ThemePreference = 'System'
    let media: MediaQueryList | null = null
    let systemAccentHex = DEFAULT_SYSTEM_ACCENT_HEX

    const onSystemChange = (): void => {
      if (disposed || preference !== 'System') return
      setThemeMode(systemPrefersDark() ? 'dark' : 'light')
    }

    // Electron ThemeManager.SetColor always applies the resolved accent.
    // ApplyAccentColorToTheme only gates the tinted surface palette (style
    // preset), not --udt-accent / selection rings / Ant Design colorPrimary.
    const resolveAccent = (settings?: ApplicationSettings): string => {
      // Renderer choice wins over the host value (async host reads may be stale
      // or drop the accent fields, which previously reset the picked color).
      const local = storedAccentPreference()
      if (local) {
        return local.source === 'Custom' && local.hex ? local.hex : systemAccentHex
      }
      if (settings?.AccentColorSource === 'Custom' && settings.AccentColor) {
        return rgbToHex(settings.AccentColor)
      }
      return systemAccentHex
    }

    const apply = (settings?: ApplicationSettings): void => {
      const stored = storedThemePreference()
      media?.removeEventListener('change', onSystemChange)
      if (stored === 'System') {
        // Renderer-authoritative "follow system" (host value may be stale).
        preference = 'System'
        media = window.matchMedia('(prefers-color-scheme: dark)')
        media.addEventListener('change', onSystemChange)
        onSystemChange()
      } else if (stored) {
        preference = 'System'
        media = null
        setThemeMode(stored === 'Dark' ? 'dark' : 'light')
      } else {
        preference = settings?.Theme ?? 'System'
        if (preference === 'System') {
          media = window.matchMedia('(prefers-color-scheme: dark)')
          media.addEventListener('change', onSystemChange)
          onSystemChange()
        } else {
          media = null
          setThemeMode(preference === 'Dark' ? 'dark' : 'light')
        }
      }
      setAccent(resolveAccent(settings))
    }

    const load = (): void => {
      settingsApi
        .get('application')
        .then((res) => {
          if (!disposed) apply(res.value as ApplicationSettings)
        })
        .catch(() => undefined)
    }

    apply()
    systemApi
      .getAccentColor()
      .then((color) => {
        if (disposed) return
        systemAccentHex = rgbToHex({ R: color.r, G: color.g, B: color.b })
        load()
      })
      .catch(() => {
        load()
      })

    const offChanged = settingsApi.onChanged((data) => {
      if (data.scope !== 'application') return
      load()
    })

    return () => {
      disposed = true
      media?.removeEventListener('change', onSystemChange)
      offChanged()
    }
  }, [setAccent, setThemeMode])

  return { themeMode, colorPrimary, uiScale, setThemeMode, setAccent, setUiScale }
}
