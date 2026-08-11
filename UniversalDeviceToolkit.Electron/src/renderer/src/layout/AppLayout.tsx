import { useState } from 'react'
import {
  AppstoreOutlined,
  DashboardOutlined,
  HomeOutlined,
  InfoCircleOutlined,
  LeftOutlined,
  MacCommandOutlined,
  RightOutlined,
  RocketOutlined,
  SettingOutlined
} from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import TitleBar from '../components/TitleBar'
import { useTheme } from '../theme/useTheme'
import './AppLayout.css'

const NAV_WIDTH_COLLAPSED = 64
const NAV_WIDTH_EXPANDED = 240

interface NavItemDef {
  key: string
  icon: React.ReactNode
  labelKey: string
}

const MAIN_ITEMS: NavItemDef[] = [
  { key: '/dashboard', icon: <HomeOutlined />, labelKey: 'nav.dashboard' },
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
  const location = useLocation()
  const navigate = useNavigate()
  const [collapsed, setCollapsed] = useState(false)

  const isDark = themeMode === 'dark'
  const navVars = {
    '--udt-nav-bg': isDark ? '#202020' : '#edf7fb',
    '--udt-nav-border': isDark ? 'rgba(255,255,255,0.08)' : '#d8e6ed',
    '--udt-nav-accent': '#416aa1',
    '--udt-nav-text': isDark ? 'rgba(255,255,255,0.65)' : '#4e5965',
    '--udt-nav-text-hover': isDark ? 'rgba(255,255,255,0.9)' : '#28323c',
    '--udt-nav-text-active': isDark ? 'rgba(255,255,255,0.92)' : '#2e3741',
    '--udt-nav-active-bg': isDark ? 'rgba(255,255,255,0.08)' : 'rgba(255,255,255,0.86)',
    '--udt-nav-hover-bg': isDark ? 'rgba(255,255,255,0.06)' : 'rgba(255,255,255,0.52)'
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
    <div className="udt-app-shell">
      <TitleBar />
      <div className="udt-app-shell__body">
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
        <div className="udt-app-shell__content">
          <main className="udt-app-shell__main">
            <Outlet />
          </main>
        </div>
      </div>
    </div>
  )
}
