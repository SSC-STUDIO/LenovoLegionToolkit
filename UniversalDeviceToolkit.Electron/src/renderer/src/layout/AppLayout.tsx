import { useCallback, useEffect, useState } from 'react'
import {
  Apps24Filled,
  Apps24Regular,
  ChevronLeft16Regular,
  ChevronRight16Regular,
  Gauge24Filled,
  Gauge24Regular,
  Home24Filled,
  Home24Regular,
  Info24Filled,
  Info24Regular,
  Keyboard24Filled,
  Keyboard24Regular,
  ReceiptPlay24Filled,
  ReceiptPlay24Regular,
  Rocket24Filled,
  Rocket24Regular,
  Settings24Filled,
  Settings24Regular
} from '@fluentui/react-icons'
import { useTranslation } from 'react-i18next'
import { Outlet, useLocation, useNavigate } from 'react-router-dom'
import TitleBar from '../components/TitleBar'
import AppStatusBanners from '../components/AppStatusBanners'
import LoadingOverlay from '../components/LoadingOverlay'
import UtilsModalHost from '../components/utils/UtilsModalHost'
import StartupGates from '../components/utils/StartupGates'
import { openStatusModal } from '../components/utils/StatusModal'
import { on } from '../api/bridge'
import WindowBackdropController from '../theme/WindowBackdropController'
import './NavigationParity.css'

const NAV_WIDTH_COLLAPSED_FALLBACK = 70
const NAV_WIDTH_COLLAPSED_CSS = '--udt-nav-width-collapsed'
const NAV_WIDTH_EXPANDED_CSS = '--udt-nav-width-expanded'
const DESIGN_WINDOW_WIDTH = 1300
const ABSOLUTE_MAX_EXPANDED = 420
const MIN_CONTENT_WIDTH = 700

function readCssNumber(name: string, fallback: number): number {
  const raw = getComputedStyle(document.documentElement).getPropertyValue(name).trim()
  const value = Number.parseFloat(raw)
  return Number.isFinite(value) && value > 0 ? value : fallback
}

function getExpandedWidth(windowWidth: number): number {
  const preferred = readCssNumber(NAV_WIDTH_EXPANDED_CSS, 220)
  if (!windowWidth || windowWidth <= 0 || Number.isNaN(windowWidth)) return preferred
  const scaled = preferred * (windowWidth / DESIGN_WINDOW_WIDTH)
  const contentBudget = Math.max(preferred, windowWidth - MIN_CONTENT_WIDTH)
  const ratioCap = windowWidth * 0.28
  const upper = Math.min(ABSOLUTE_MAX_EXPANDED, Math.min(contentBudget, Math.max(preferred, ratioCap)))
  return Math.min(Math.max(scaled, preferred), upper)
}

interface NavItemDef {
  key: string
  icon: (filled: boolean) => React.ReactNode
  labelKey: string
}

const MAIN_ITEMS: NavItemDef[] = [
  { key: '/dashboard', icon: (filled) => filled ? <Home24Filled /> : <Home24Regular />, labelKey: 'nav.dashboard' },
  { key: '/keyboard', icon: (filled) => filled ? <Keyboard24Filled /> : <Keyboard24Regular />, labelKey: 'nav.keyboard' },
  { key: '/automation', icon: (filled) => filled ? <Rocket24Filled /> : <Rocket24Regular />, labelKey: 'nav.automation' },
  { key: '/macro', icon: (filled) => filled ? <ReceiptPlay24Filled /> : <ReceiptPlay24Regular />, labelKey: 'nav.macro' },
  { key: '/optimization', icon: (filled) => filled ? <Gauge24Filled /> : <Gauge24Regular />, labelKey: 'nav.windowsOptimization' }
]

const FOOTER_ITEMS: NavItemDef[] = [
  { key: '/plugins', icon: (filled) => filled ? <Apps24Filled /> : <Apps24Regular />, labelKey: 'nav.pluginExtensions' },
  { key: '/settings', icon: (filled) => filled ? <Settings24Filled /> : <Settings24Regular />, labelKey: 'nav.settings' },
  { key: '/about', icon: (filled) => filled ? <Info24Filled /> : <Info24Regular />, labelKey: 'nav.about' }
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
      <span className="udt-nav-icon" aria-hidden="true">{item.icon(active)}</span>
      <span className="udt-nav-label">{label}</span>
    </button>
  )
}

export default function AppLayout(): React.JSX.Element {
  const { t } = useTranslation()
  const location = useLocation()
  const navigate = useNavigate()
  const [collapsed, setCollapsed] = useState(false)
  const [windowWidth, setWindowWidth] = useState(() => window.innerWidth)

  const navWidth = collapsed
    ? readCssNumber(NAV_WIDTH_COLLAPSED_CSS, NAV_WIDTH_COLLAPSED_FALLBACK)
    : getExpandedWidth(windowWidth)

  const handleResize = useCallback((): void => {
    setWindowWidth(window.innerWidth)
  }, [])

  useEffect(() => {
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [handleResize])

  // Tray "Status / 状态" menu item → renderer status popup (WPF StatusWindow).
  useEffect(() => {
    const off = on('tray:status', () => {
      void openStatusModal()
    })
    return off
  }, [])

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
      <WindowBackdropController />
      <StartupGates />
      <UtilsModalHost />
      <LoadingOverlay />
      <TitleBar />
      <div className="udt-app-shell__body">
        <nav
          aria-label="navigation"
          className="udt-nav udt-nav--wpf-parity"
          style={{ width: navWidth }}
        >
          <div className="udt-nav__scroll">
            <div className="udt-nav-group">{MAIN_ITEMS.map(renderItem)}</div>
          </div>
          <div className="udt-nav-group udt-nav-group--footer">{FOOTER_ITEMS.map(renderItem)}</div>
          <button
            type="button"
            aria-label={collapsed ? 'expand-navigation' : 'collapse-navigation'}
            className={`udt-nav-toggle${collapsed ? ' udt-nav-toggle--collapsed' : ''}`}
            onClick={() => setCollapsed((value) => !value)}
          >
            {collapsed ? <ChevronRight16Regular /> : <ChevronLeft16Regular />}
          </button>
        </nav>
        <div className="udt-app-shell__content">
          <AppStatusBanners />
          <main className="udt-app-shell__main">
            <div key={location.pathname} className="udt-page-enter">
              <Outlet />
            </div>
          </main>
        </div>
      </div>
    </div>
  )
}
