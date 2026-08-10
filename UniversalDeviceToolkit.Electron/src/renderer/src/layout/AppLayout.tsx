import { useState } from 'react'
import {
  AppstoreOutlined,
  DashboardOutlined,
  HomeOutlined,
  InfoCircleOutlined,
  KeyOutlined,
  MacCommandOutlined,
  MoonOutlined,
  RocketOutlined,
  SettingOutlined,
  SunOutlined
} from '@ant-design/icons'
import { Button, Layout, Menu, Select, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import { changeLanguage, supportedLanguages } from '../i18n'
import { useTheme } from '../theme/useTheme'

const { Sider, Header, Content } = Layout

const LANGUAGE_OPTIONS: { value: (typeof supportedLanguages)[number]; label: string }[] = [
  { value: 'zh-CN', label: '简体中文' },
  { value: 'en-US', label: 'English' }
]

const NAV_ITEMS: { key: string; icon: React.JSX.Element; labelKey: string }[] = [
  { key: '/dashboard', icon: <HomeOutlined />, labelKey: 'nav.dashboard' },
  { key: '/keyboard', icon: <KeyOutlined />, labelKey: 'nav.keyboardBacklight' },
  { key: '/automation', icon: <RocketOutlined />, labelKey: 'nav.automation' },
  { key: '/macro', icon: <MacCommandOutlined />, labelKey: 'nav.macro' },
  { key: '/optimization', icon: <DashboardOutlined />, labelKey: 'nav.windowsOptimization' },
  { key: '/plugins', icon: <AppstoreOutlined />, labelKey: 'nav.pluginExtensions' },
  { key: '/settings', icon: <SettingOutlined />, labelKey: 'nav.settings' },
  { key: '/about', icon: <InfoCircleOutlined />, labelKey: 'nav.about' }
]

export default function AppLayout(): React.JSX.Element {
  const { t, i18n } = useTranslation()
  const location = useLocation()
  const navigate = useNavigate()
  const { themeMode, setThemeMode } = useTheme()
  const [collapsed, setCollapsed] = useState(false)
  const [language, setLanguage] = useState(i18n.language.startsWith('zh') ? 'zh-CN' : 'en-US')

  const handleLanguageChange = (value: string): void => {
    setLanguage(value as (typeof supportedLanguages)[number])
    localStorage.setItem('udt.lang', value)
    void changeLanguage(value)
  }

  const handleToggleTheme = (): void => {
    const next = themeMode === 'dark' ? 'light' : 'dark'
    localStorage.setItem('udt.theme', next)
    setThemeMode(next)
  }

  return (
    <Layout style={{ height: '100vh' }}>
      <Sider collapsible collapsed={collapsed} onCollapse={setCollapsed}>
        <div
          style={{
            height: 48,
            margin: 12,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center'
          }}
        >
          <Typography.Text strong style={{ color: '#fff', whiteSpace: 'nowrap' }}>
            {t('app.name')}
          </Typography.Text>
        </div>
        <Menu
          theme="dark"
          mode="inline"
          selectedKeys={[location.pathname]}
          items={NAV_ITEMS.map((item) => ({ ...item, label: t(item.labelKey) }))}
          onClick={({ key }) => navigate(key)}
        />
      </Sider>
      <Layout>
        <Header
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            paddingInline: 24
          }}
        >
          <Typography.Text strong>{t('app.name')}</Typography.Text>
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <Select
              value={language}
              options={LANGUAGE_OPTIONS}
              onChange={handleLanguageChange}
              style={{ width: 120 }}
            />
            <Button
              aria-label="toggle theme"
              icon={themeMode === 'dark' ? <SunOutlined /> : <MoonOutlined />}
              onClick={handleToggleTheme}
            />
          </div>
        </Header>
        <Content style={{ padding: 24, overflow: 'auto' }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  )
}
