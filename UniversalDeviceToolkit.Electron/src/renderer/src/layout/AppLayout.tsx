import { useState } from 'react'
import {
  AppstoreOutlined,
  DashboardOutlined,
  HomeOutlined,
  InfoCircleOutlined,
  KeyOutlined,
  LeftOutlined,
  MacCommandOutlined,
  RightOutlined,
  RocketOutlined,
  SettingOutlined
} from '@ant-design/icons'
import { theme } from 'antd'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import TitleBar from '../components/TitleBar'
import { useTheme } from '../theme/useTheme'

const NAV_WIDTH_COLLAPSED = 70
const NAV_WIDTH_EXPANDED = 220

interface NavItemDef {
  key: string
  icon: React.ReactNode
  labelKey: string
}

const MAIN_ITEMS: NavItemDef[] = [
  { key: '/dashboard', icon: <HomeOutlined />, labelKey: 'nav.dashboard' },
  { key: '/keyboard', icon: <KeyOutlined />, labelKey: 'nav.keyboardBacklight' },
  { key: '/automation', icon: <RocketOutlined />, labelKey: 'nav.automation' },
  { key: '/macro', icon: <MacCommandOutlined />, labelKey: 'nav.macro' },
  { key: '/optimization', icon: <DashboardOutlined />, labelKey: 'nav.windowsOptimization' }
]

const FOOTER_ITEMS: NavItemDef[] = [
  { key: '/plugins', icon: <AppstoreOutlined />, labelKey: 'nav.pluginExtensions' },
  { key: '/settings', icon: <SettingOutlined />, labelKey: 'nav.settings' },
  { key: '/about', icon: <InfoCircleOutlined />, labelKey: 'nav.about' }
]

function isRouteActive(pathname: string, key: string): boolean {
  return pathname === key || pathname.startsWith(`${key}/`)
}

interface NavItemProps {
  item: NavItemDef
  label: string
  collapsed: boolean
  active: boolean
  onClick: () => void
}

function NavItem({ item, label, collapsed, active, onClick }: NavItemProps): React.JSX.Element {
  const className = ['udt-nav-item', collapsed && 'udt-nav-item--collapsed', active && 'udt-nav-item--active']
    .filter(Boolean)
    .join(' ')
  return (
    <button
      type="button"
      className={className}
      title={collapsed ? label : undefined}
      aria-current={active ? 'page' : undefined}
      onClick={onClick}
    >
      <span className="udt-nav-accent" />
      <span className="udt-nav-icon">{item.icon}</span>
      <span className="udt-nav-label">{label}</span>
    </button>
  )
}

export default function AppLayout(): React.JSX.Element {
  const { t } = useTranslation()
  const { themeMode } = useTheme()
  const { token } = theme.useToken()
  const location = useLocation()
  const navigate = useNavigate()
  const [collapsed, setCollapsed] = useState(false)

  const isDark = themeMode === 'dark'
  const navVars = {
    '--udt-nav-bg': 'var(--udt-bg-window, var(--udt-color-bg-layout, #f5f5f5))',
    '--udt-nav-border': token.colorSplit,
    '--udt-nav-accent': token.colorPrimary,
    '--udt-nav-text': token.colorTextSecondary,
    '--udt-nav-text-hover': token.colorText,
    '--udt-nav-text-active': token.colorText,
    '--udt-nav-active-bg': isDark ? 'rgba(255,255,255,0.12)' : token.colorFillSecondary,
    '--udt-nav-hover-bg': isDark ? 'rgba(255,255,255,0.08)' : token.colorFillTertiary
  } as React.CSSProperties

  const renderItem = (item: NavItemDef): React.JSX.Element => (
    <NavItem
      key={item.key}
      item={item}
      label={t(item.labelKey)}
      collapsed={collapsed}
      active={isRouteActive(location.pathname, item.key)}
      onClick={() => navigate(item.key)}
    />
  )

  return (
    <div style={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
      <nav
        aria-label="navigation"
        className="udt-nav"
        style={{ width: collapsed ? NAV_WIDTH_COLLAPSED : NAV_WIDTH_EXPANDED, ...navVars }}
      >
        <div className="udt-nav-group">{MAIN_ITEMS.map(renderItem)}</div>
        <div className="udt-nav-spacer" />
        <div className="udt-nav-group">{FOOTER_ITEMS.map(renderItem)}</div>
        <button
          type="button"
          aria-label={collapsed ? 'expand-navigation' : 'collapse-navigation'}
          className={`udt-nav-toggle${collapsed ? ' udt-nav-toggle--collapsed' : ''}`}
          onClick={() => setCollapsed((value) => !value)}
        >
          {collapsed ? <RightOutlined /> : <LeftOutlined />}
        </button>
      </nav>
      <div style={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
        <TitleBar />
        <main
          style={{
            flex: 1,
            overflow: 'auto',
            background: 'var(--udt-bg-window, var(--udt-color-bg-layout, #f5f5f5))',
            padding: '16px 16px 16px 24px'
          }}
        >
          <Outlet />
        </main>
      </div>
    </div>
  )
}
