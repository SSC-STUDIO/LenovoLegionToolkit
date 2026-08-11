import React, { useEffect, useState } from 'react'
import ReactDOM from 'react-dom/client'
import { HashRouter } from 'react-router-dom'
import { ConfigProvider, theme } from 'antd'
import enUS from 'antd/locale/en_US'
import zhCN from 'antd/locale/zh_CN'
import i18n from './i18n'
import { initNotifications } from './notifications'
import { initCrashReportListener } from './notifications/crashListener'
import { useThemeStore } from './stores/themeStore'
import App from './App'
import './styles/global.css'

void initNotifications()
void initCrashReportListener()

function Root(): React.JSX.Element {
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
      // WPF --udt-accent-secondary is the accent at 90% opacity.
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
