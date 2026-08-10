import React, { useEffect, useState } from 'react'
import ReactDOM from 'react-dom/client'
import { HashRouter } from 'react-router-dom'
import { ConfigProvider, theme } from 'antd'
import enUS from 'antd/locale/en_US'
import zhCN from 'antd/locale/zh_CN'
import i18n from './i18n'
import { useThemeStore } from './stores/themeStore'
import App from './App'
import './styles/global.css'

function Root(): React.JSX.Element {
  const themeMode = useThemeStore((s) => s.themeMode)
  const colorPrimary = useThemeStore((s) => s.colorPrimary)
  const [locale, setLocale] = useState(i18n.language.startsWith('zh') ? zhCN : enUS)

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
        token: colorPrimary ? { colorPrimary } : undefined
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
