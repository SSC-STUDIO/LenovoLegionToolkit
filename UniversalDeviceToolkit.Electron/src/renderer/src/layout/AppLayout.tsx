import { useCallback, useEffect, useMemo, useState } from 'react'
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
import NotificationCenter from '../components/NotificationCenter'
import UtilsModalHost from '../components/utils/UtilsModalHost'
import StartupGates from '../components/utils/StartupGates'
import { openStatusModal } from '../components/utils/StatusModal'
import { on } from '../api/bridge'
import { useSettingsStore } from '../stores/settingsStore'
import WindowBackdropController from '../theme/WindowBackdropController'
import './NavigationParity.css'

const NAV_WIDTH_COLLAPSED_FALLBACK = 70
const NAV_WIDTH_COLLAPSED_CSS = '--udt-nav-width-collapsed'
const NAV_WIDTH_EXPANDED_CSS = '--udt-nav-width-expanded'
const DESIGN_WINDOW_WIDTH = 1300
const ABSOLUTE_MAX_EXPANDED = 420
const MIN_CONTENT_WIDTH = 700
const NAV_COLLAPSED_STORAGE_KEY = 'udt.navCollapsed'

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

// Keyboard navigation order: main items first, then footer items (Electron
// NavigationStoreExtensions.Items + Footer).
const ALL_NAV_ITEMS: NavItemDef[] = [...MAIN_ITEMS, ...FOOTER_ITEMS]

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
  const scopes = useSettingsStore((s) => s.scopes)
  const loadSettings = useSettingsStore((s) => s.load)
  const [collapsed, setCollapsed] = useState(() => {
    try {
      return localStorage.getItem(NAV_COLLAPSED_STORAGE_KEY) === '1'
    } catch {
      return false
    }
  })
  const [windowWidth, setWindowWidth] = useState(() => window.innerWidth)

  // Load the application scope once so navigation visibility settings apply.
  useEffect(() => {
    void loadSettings(['application'])
  }, [loadSettings])

  // Electron MainWindow.UpdateNavigationItemsVisibilityFromSettings: dashboard and
  // settings are always visible; everything else defaults to visible unless
  // NavigationItemsVisibility opts it out. Plugin Extensions stays visible by
  // default (ExtensionsEnabled only controls whether extensions load).
  const navVisibility = useMemo(() => {
    const app = (scopes.application ?? {}) as Record<string, unknown>
    return ((app.NavigationItemsVisibility as Record<string, boolean> | undefined) ?? {})
  }, [scopes.application])

  const isNavItemVisible = useCallback(
    (item: NavItemDef): boolean => {
      const pageTagMap: Record<string, string> = {
        '/dashboard': 'dashboard',
        '/settings': 'settings',
        '/keyboard': 'keyboard',
        '/automation': 'automation',
        '/macro': 'macro',
        '/optimization': 'windowsOptimization',
        '/plugins': 'pluginExtensions',
        '/about': 'about'
      }
      const pageTag: string = pageTagMap[item.key] ?? item.key.replace('/', '')
      if (pageTag === 'dashboard' || pageTag === 'settings' || pageTag === 'pluginExtensions') return true
      if (navVisibility[pageTag] === false) return false
      return true
    },
    [navVisibility]
  )

  const visibleMainItems = useMemo(() => MAIN_ITEMS.filter(isNavItemVisible), [isNavItemVisible])
  const visibleFooterItems = useMemo(() => FOOTER_ITEMS.filter(isNavItemVisible), [isNavItemVisible])

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

  // Persist the navigation collapse state across sessions (Electron parity:
  // NavigationStore saves NavigationPaneExpanded on exit and restores it).
  useEffect(() => {
    try {
      localStorage.setItem(NAV_COLLAPSED_STORAGE_KEY, collapsed ? '1' : '0')
    } catch {
      // localStorage unavailable — collapse state stays in-memory only
    }
  }, [collapsed])

  // Tray navigation (Electron TrayHelper → NavigationStore.Navigate) and optional
  // status popup (legacy Electron-only; not part of the original tray menu).
  useEffect(() => {
    const offNavigate = on('tray:navigate', (data) => {
      const route = (data as { route?: string } | null)?.route
      if (typeof route === 'string' && route.length > 0) navigate(route)
    })
    const offStatus = on('tray:status', () => {
      void openStatusModal()
    })
    return () => {
      offNavigate()
      offStatus()
    }
  }, [navigate])

  // Alt+ArrowLeft/ArrowRight page switching — port of Electron
  // NavigationStoreExtensions.NavigateToPrevious/NavigateToNext: cycles through
  // MAIN_ITEMS followed by FOOTER_ITEMS, wrapping around at both ends.
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent): void => {
      if (!event.altKey) return
      if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return
      event.preventDefault()
      const currentIndex = ALL_NAV_ITEMS.findIndex((item) => isRouteActive(location.pathname, item.key))
      let nextIndex: number
      if (event.key === 'ArrowRight') {
        nextIndex = (currentIndex + 1 + ALL_NAV_ITEMS.length) % ALL_NAV_ITEMS.length
      } else {
        const index = currentIndex < 0 ? 0 : currentIndex - 1
        nextIndex = index < 0 ? ALL_NAV_ITEMS.length - 1 : index
      }
      navigate(ALL_NAV_ITEMS[nextIndex].key)
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [location.pathname, navigate])

  // Ctrl+Tab / Ctrl+Shift+Tab page switching + Ctrl+1..9 direct jump — port of
  // Electron MainWindow key bindings (NavigationStore.NavigateToNext/Previous and
  // the numbered nav-item shortcuts).
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent): void => {
      if (!event.ctrlKey || event.altKey || event.metaKey) return
      if (event.key === 'Tab') {
        event.preventDefault()
        const currentIndex = ALL_NAV_ITEMS.findIndex((item) => isRouteActive(location.pathname, item.key))
        const nextIndex = event.shiftKey
          ? (currentIndex - 1 + ALL_NAV_ITEMS.length) % ALL_NAV_ITEMS.length
          : (currentIndex + 1) % ALL_NAV_ITEMS.length
        navigate(ALL_NAV_ITEMS[nextIndex].key)
        return
      }
      const digit = Number(event.key)
      if (Number.isInteger(digit) && digit >= 1 && digit <= ALL_NAV_ITEMS.length) {
        const target = ALL_NAV_ITEMS[digit - 1]
        if (target && !isRouteActive(location.pathname, target.key)) {
          event.preventDefault()
          navigate(target.key)
        }
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [location.pathname, navigate])

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
      <NotificationCenter />
      {/* Electron MainWindow._statusNotificationStack: bottom-right overlay, not in-flow. */}
      <AppStatusBanners />
      <TitleBar />
      <div className="udt-app-shell__body">
        <nav
          aria-label={t('common.navigation')}
          className="udt-nav udt-nav--electron-parity"
          style={{ width: navWidth }}
        >
          <div className="udt-nav__scroll">
            <div className="udt-nav-group">{visibleMainItems.map(renderItem)}</div>
          </div>
          <div className="udt-nav-group udt-nav-group--footer">{visibleFooterItems.map(renderItem)}</div>
          <button
            type="button"
            aria-label={collapsed ? t('common.expandNavigation') : t('common.collapseNavigation')}
            className={`udt-nav-toggle${collapsed ? ' udt-nav-toggle--collapsed' : ''}`}
            onClick={() => setCollapsed((value) => !value)}
          >
            {collapsed ? <ChevronRight16Regular /> : <ChevronLeft16Regular />}
          </button>
        </nav>
        <div className="udt-app-shell__content">
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
