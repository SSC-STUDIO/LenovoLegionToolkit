import { useEffect } from 'react'
import { settingsApi } from '../api/settings'
import { useThemeStore } from '../stores/themeStore'
import type { ThemeMode } from '../stores/themeStore'

type ThemePreference = 'System' | 'Light' | 'Dark'

interface ApplicationSettings {
  Theme?: ThemePreference
  AccentColor?: { R: number; G: number; B: number } | null
}

const THEME_STORAGE_KEY = 'udt.theme'

function systemPrefersDark(): boolean {
  return window.matchMedia('(prefers-color-scheme: dark)').matches
}

function rgbToHex(color: { R: number; G: number; B: number }): string {
  const toHex = (value: number): string => value.toString(16).padStart(2, '0')
  return `#${toHex(color.R)}${toHex(color.G)}${toHex(color.B)}`
}

function storedThemeMode(): ThemeMode | null {
  const stored = localStorage.getItem(THEME_STORAGE_KEY)
  return stored === 'light' || stored === 'dark' ? stored : null
}

export interface ThemeController {
  themeMode: ThemeMode
  colorPrimary?: string
  setThemeMode: (mode: ThemeMode) => void
  setAccent: (color?: string) => void
}

export function useTheme(): ThemeController {
  const themeMode = useThemeStore((s) => s.themeMode)
  const colorPrimary = useThemeStore((s) => s.colorPrimary)
  const setThemeMode = useThemeStore((s) => s.setThemeMode)
  const setAccent = useThemeStore((s) => s.setAccent)

  useEffect(() => {
    let disposed = false
    let preference: ThemePreference = 'System'
    let media: MediaQueryList | null = null
    let offChanged: (() => void) | undefined

    const onSystemChange = (): void => {
      if (disposed || preference !== 'System') return
      setThemeMode(systemPrefersDark() ? 'dark' : 'light')
    }

    const apply = (settings?: ApplicationSettings): void => {
      const stored = storedThemeMode()
      if (stored) {
        preference = 'System'
        media?.removeEventListener('change', onSystemChange)
        media = null
        setThemeMode(stored)
      } else {
        preference = settings?.Theme ?? 'System'
        media?.removeEventListener('change', onSystemChange)
        if (preference === 'System') {
          media = window.matchMedia('(prefers-color-scheme: dark)')
          media.addEventListener('change', onSystemChange)
          onSystemChange()
        } else {
          media = null
          setThemeMode(preference === 'Dark' ? 'dark' : 'light')
        }
      }
      setAccent(settings?.AccentColor ? rgbToHex(settings.AccentColor) : undefined)
    }

    apply()
    settingsApi
      .get('application')
      .then((res) => {
        if (!disposed) apply(res.value as ApplicationSettings)
      })
      .catch(() => undefined)

    offChanged = settingsApi.onChanged((data) => {
      if (data.scope !== 'application') return
      settingsApi
        .get('application')
        .then((res) => {
          if (!disposed) apply(res.value as ApplicationSettings)
        })
        .catch(() => undefined)
    })

    return () => {
      disposed = true
      media?.removeEventListener('change', onSystemChange)
      offChanged?.()
    }
  }, [setAccent, setThemeMode])

  return { themeMode, colorPrimary, setThemeMode, setAccent }
}
