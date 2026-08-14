import './bridge/initWebBridge'
import React, { useEffect, useState } from 'react'
import ReactDOM from 'react-dom/client'
import { HashRouter } from 'react-router-dom'
import { ConfigProvider, message, theme } from 'antd'
import enUS from 'antd/locale/en_US'
import zhCN from 'antd/locale/zh_CN'
import i18n from './i18n'
import logger from './utils/logger'
import { initNotifications } from './notifications'
import { initCrashReportListener } from './notifications/crashListener'
import { settingsApi } from './api/settings'
import { useSettingsStore } from './stores/settingsStore'
import { useThemeStore } from './stores/themeStore'
import { useTheme } from './theme/useTheme'
import { bootstrapThemeDocument } from './theme/bootstrapTheme'
import App from './App'
import './styles/global.css'
import './styles/skeleton.css'

bootstrapThemeDocument()

// Platform adaptation: [data-platform] on <html> drives platform-specific CSS
// (font stack, scrollbars). bridge.platform is process.platform from the main
// process ('darwin' on macOS). Browser `dev:web` sets platform 'web' via the
// shim; if no bridge is present, do not pretend we are Windows.
const RUNTIME_PLATFORM = window.bridge?.platform ?? 'web'
document.documentElement.dataset.platform = RUNTIME_PLATFORM

logger.info('renderer starting', { platform: window.bridge?.platform ?? 'unknown' })
void initNotifications()
void initCrashReportListener()

// Electron SnackbarHelper parity: transient action feedback lives at the bottom
// edge (see .ant-message override in global.css), distinct from the corner
// notification stack (AppNotificationHost).
message.config({ maxCount: 3 })

/** Apply the saved AnimationsEnabled flag to <html> (drives window/dialog CSS). */
function applyAnimationsSetting(application?: unknown): void {
  const enabled =
    typeof application === 'object' &&
    application !== null &&
    (application as Record<string, unknown>)['AnimationsEnabled'] !== false
  document.documentElement.classList.toggle('udt-animations-off', !enabled)
}

function Root(): React.JSX.Element {
  // Keep theme mode + accent synced for the whole app (not only Settings).
  useTheme()
  const themeMode = useThemeStore((s) => s.themeMode)
  const themePreference = useThemeStore((s) => s.themePreference)
  const colorPrimary = useThemeStore((s) => s.colorPrimary)
  const [locale, setLocale] = useState(i18n.language.startsWith('zh') ? zhCN : enUS)

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', themeMode)
    document.documentElement.style.colorScheme = themeMode
    // Keep DWM backdrop materials (mica/acrylic) matching the in-app theme —
    // otherwise a dark app on a light system shows a washed-out white backdrop.
    // Only pin nativeTheme while the user forces Light/Dark: a pinned
    // themeSource makes prefers-color-scheme report the override instead of
    // the real OS theme, so "follow system" must reset it to 'system' for
    // matchMedia (bootstrapTheme / useTheme) to resolve the actual OS mode
    // and for the OS change listener to keep firing.
    window.bridge?.setThemeSource?.(themePreference === 'system' ? 'system' : themeMode)
  }, [themeMode, themePreference])

  useEffect(() => {
    const root = document.documentElement
    if (colorPrimary) {
      root.style.setProperty('--udt-accent', colorPrimary)
      // Electron --udt-accent-secondary is the accent at 90% opacity.
      root.style.setProperty('--udt-accent-secondary', `color-mix(in srgb, ${colorPrimary} 90%, transparent)`)
    } else {
      root.style.removeProperty('--udt-accent')
      root.style.removeProperty('--udt-accent-secondary')
    }
  }, [colorPrimary])

  useEffect(() => {
    const handleLanguageChanged = (lng: string): void => {
      setLocale(lng.startsWith('zh') ? zhCN : enUS)
    }
    i18n.on('languageChanged', handleLanguageChanged)
    return () => {
      i18n.off('languageChanged', handleLanguageChanged)
    }
  }, [])

  useEffect(() => {
    // Apply saved AnimationsEnabled on startup and keep it in sync when the
    // setting changes (settings.changed event or the Settings page toggle).
    let disposed = false
    const applyFromScopes = (): void => {
      if (!disposed) applyAnimationsSetting(useSettingsStore.getState().scopes.application)
    }
    const refreshAnimations = (): void => {
      void useSettingsStore
        .getState()
        .load(['application'])
        .then(applyFromScopes)
        .catch((error: unknown) => {
          if (!disposed) logger.warn('failed to refresh animation settings', error)
        })
    }
    const offChanged = settingsApi.onChanged(({ scope }) => {
      if (scope === 'application') {
        refreshAnimations()
      }
    })
    refreshAnimations()
    return () => {
      disposed = true
      offChanged()
    }
  }, [])

  return (
    <ConfigProvider
      locale={locale}
      theme={{
        algorithm: themeMode === 'dark' ? theme.darkAlgorithm : theme.defaultAlgorithm,
        cssVar: { key: 'udt' },
        token: {
          borderRadius: 8,
          borderRadiusLG: 18,
          borderRadiusSM: 8,
          controlHeight: 32,
          fontSize: 13,
          ...(themeMode === 'dark'
            ? {
                colorBgLayout: '#202020',
                colorBgContainer: '#303030',
                colorBorderSecondary: 'rgba(255,255,255,0.08)'
              }
            : {}),
          ...(colorPrimary ? { colorPrimary } : {})
        },
        components: {
          Select: {
            borderRadiusLG: 12
          },
          Cascader: {
            borderRadiusLG: 12
          },
          DatePicker: {
            borderRadiusLG: 12
          },
          Dropdown: {
            borderRadiusLG: 12,
            borderRadiusSM: 8
          },
          Mentions: {
            borderRadiusLG: 12
          }
        }
      }}
    >
      <HashRouter>
        <App />
      </HashRouter>
    </ConfigProvider>
  )
}

ReactDOM.createRoot(document.getElementById('root') as HTMLElement).render(
  <React.StrictMode>
    <Root />
  </React.StrictMode>
)
