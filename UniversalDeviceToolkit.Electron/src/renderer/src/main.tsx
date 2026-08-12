import React, { useEffect, useState } from 'react'
import ReactDOM from 'react-dom/client'
import { HashRouter } from 'react-router-dom'
import { ConfigProvider, message, theme } from 'antd'
import enUS from 'antd/locale/en_US'
import zhCN from 'antd/locale/zh_CN'
import i18n from './i18n'
import { initNotifications } from './notifications'
import { initCrashReportListener } from './notifications/crashListener'
import { settingsApi } from './api/settings'
import { useSettingsStore } from './stores/settingsStore'
import { useThemeStore } from './stores/themeStore'
import { useTheme } from './theme/useTheme'
import App from './App'
import './styles/global.css'

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
  const colorPrimary = useThemeStore((s) => s.colorPrimary)
  const [locale, setLocale] = useState(i18n.language.startsWith('zh') ? zhCN : enUS)

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', themeMode)
  }, [themeMode])

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
      if (disposed) applyAnimationsSetting(useSettingsStore.getState().scopes.application)
    }
    const offChanged = settingsApi.onChanged(() => {
      void useSettingsStore.getState().load(['application']).then(applyFromScopes)
    })
    void useSettingsStore
      .getState()
      .load(['application'])
      .then(() => {
        applyAnimationsSetting(useSettingsStore.getState().scopes.application)
      })
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
          fontSize: 13,
          ...(themeMode === 'dark'
            ? {
                colorBgLayout: '#202020',
                colorBgContainer: '#303030',
                colorBorderSecondary: 'rgba(255,255,255,0.08)'
              }
            : {}),
          ...(colorPrimary ? { colorPrimary } : {})
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
