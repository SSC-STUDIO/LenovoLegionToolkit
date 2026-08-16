import { useCallback, useEffect, useRef, useState } from 'react'
import {
  Apps24Regular,
  ArrowSync24Regular,
  Desktop24Regular,
  Eye24Regular,
  Key24Regular,
  PaintBrush24Regular,
  PlugConnected24Regular,
  Power24Regular
} from '../components/icons/fluent'
import { Tooltip } from 'antd'
import { useTranslation } from 'react-i18next'
import { isHostUnavailableError, sanitizeBridgeError } from '../api/bridge'
import { featuresApi, type FeatureKey } from '../api/features'
import { useLoadingStore } from '../stores/loadingStore'
import { useSettingsStore } from '../stores/settingsStore'
import AppearanceSection from '../components/settings/AppearanceSection'
import ApplicationSection from '../components/settings/ApplicationSection'
import { PowerSection } from '../components/settings/PowerSection'
import { DisplaySection } from '../components/settings/DisplaySection'
import { SmartKeysSection } from '../components/settings/SmartKeysSection'
import { UpdateSection } from '../components/settings/UpdateSection'
import { IntegrationsSection } from '../components/settings/IntegrationsSection'
import { OsdSection } from '../components/settings/OsdSection'
import { SettingsLoadError } from '../components/settings/SettingsLoadError'
import { SkeletonList } from '../components/Skeleton'
import '../components/settings/settings.css'

type SectionKey =
  | 'appearance'
  | 'application'
  | 'smartKeys'
  | 'display'
  | 'power'
  | 'update'
  | 'integrations'
  | 'osd'

/** Features that only exist on Lenovo hardware-control capable machines. */
const LENOVO_FEATURE_KEYS: readonly FeatureKey[] = [
  'alwaysOnUsb',
  'battery',
  'batteryNightCharge',
  'dpiScale',
  'flipToStart',
  'fnLock',
  'hdr',
  'hybridMode',
  'igpuMode',
  'instantBoot',
  'itsMode',
  'microphone',
  'oneLevelWhiteKeyboard',
  'overDrive',
  'panelLogo',
  'portsBacklight',
  'powerMode',
  'refreshRate',
  'resolution',
  'touchpadLock',
  'whiteKeyboard',
  'winKey'
]

const NAV_ITEMS: { key: SectionKey; labelKey: string; icon: React.JSX.Element }[] = [
  { key: 'appearance', labelKey: 'settings.nav.appearance', icon: <PaintBrush24Regular /> },
  { key: 'application', labelKey: 'settings.nav.application', icon: <Apps24Regular /> },
  { key: 'power', labelKey: 'settings.nav.power', icon: <Power24Regular /> },
  { key: 'display', labelKey: 'settings.nav.display', icon: <Desktop24Regular /> },
  { key: 'smartKeys', labelKey: 'settings.nav.smartKeys', icon: <Key24Regular /> },
  { key: 'update', labelKey: 'settings.nav.update', icon: <ArrowSync24Regular /> },
  { key: 'integrations', labelKey: 'settings.nav.integrations', icon: <PlugConnected24Regular /> },
  { key: 'osd', labelKey: 'settings.nav.osd', icon: <Eye24Regular /> }
]

const HARDWARE_GATED_KEYS: readonly SectionKey[] = ['smartKeys', 'display', 'power']

const NAV_MIN_WIDTH = 150
const NAV_MAX_WIDTH = 480
const NAV_DEFAULT_WIDTH = 180

function renderSection(key: SectionKey): React.JSX.Element {
  switch (key) {
    case 'appearance': return <AppearanceSection />
    case 'application': return <ApplicationSection />
    case 'smartKeys': return <SmartKeysSection />
    case 'display': return <DisplaySection />
    case 'power': return <PowerSection />
    case 'update': return <UpdateSection />
    case 'integrations': return <IntegrationsSection />
    case 'osd': return <OsdSection />
  }
}

export default function SettingsPage(): React.JSX.Element {
  const { t } = useTranslation()
  const [active, setActive] = useState<SectionKey>('appearance')
  const [navWidth, setNavWidth] = useState(NAV_DEFAULT_WIDTH)
  const [supportsLenovoHardware, setSupportsLenovoHardware] = useState(true)
  const [pageLoading, setPageLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [scopesReady, setScopesReady] = useState(false)
  const [reloadToken, setReloadToken] = useState(0)
  const resizeCleanupRef = useRef<(() => void) | null>(null)
  const contentRef = useRef<HTMLElement | null>(null)

  useEffect(() => {
    if (contentRef.current) {
      contentRef.current.scrollTop = 0
    }
  }, [active])

  useEffect(() => {
    return () => {
      resizeCleanupRef.current?.()
      resizeCleanupRef.current = null
    }
  }, [])

  const retry = useCallback(() => {
    setLoadError(null)
    setScopesReady(false)
    setPageLoading(true)
    setReloadToken((value) => value + 1)
  }, [])

  useEffect(() => {
    let cancelled = false
    // SettingsPage owns its loading chrome: SkeletonList already mirrors the
    // section cards, so keep this session silent and skip the global overlay.
    const loadingId = useLoadingStore.getState().start(
      t('loading.settings', { defaultValue: 'Loading settings…' }),
      { canCancel: false, silent: true }
    )

    const loadPage = async (): Promise<void> => {
      const featuresPromise = featuresApi.list()
      await useSettingsStore.getState().load()
      if (cancelled) return

      let nextSupportsLenovoHardware = true
      try {
        const infos = await featuresPromise
        if (cancelled) return
        nextSupportsLenovoHardware = infos.some(
          (info) => info.supported && LENOVO_FEATURE_KEYS.includes(info.key)
        )
      } catch {
        // Keep the default (all sections visible) when the probe fails.
      }
      if (cancelled) return

      setSupportsLenovoHardware(nextSupportsLenovoHardware)
      setScopesReady(true)
      setLoadError(null)
      setPageLoading(false)
    }

    loadPage()
      .catch((reason: unknown) => {
        if (cancelled) return
        const raw = sanitizeBridgeError(reason)
        setScopesReady(false)
        setLoadError(
          isHostUnavailableError(raw)
            ? t('home.hostUnavailable', {
                defaultValue:
                  'The backend host is not running. Wait a moment and retry, or restart the app.'
              })
            : raw || t('settings.loadFailed', { defaultValue: 'Failed to load settings' })
        )
        setPageLoading(false)
      })
      .finally(() => {
        useLoadingStore.getState().finish(loadingId)
      })
    return () => {
      cancelled = true
      useLoadingStore.getState().finish(loadingId)
    }
  }, [reloadToken, t])

  const navItems = supportsLenovoHardware
    ? NAV_ITEMS
    : NAV_ITEMS.filter((item) => !HARDWARE_GATED_KEYS.includes(item.key))

  useEffect(() => {
    if (!navItems.some((item) => item.key === active)) {
      setActive('appearance')
    }
  }, [navItems, active])

  const startResize = (event: React.PointerEvent<HTMLDivElement>): void => {
    event.preventDefault()
    resizeCleanupRef.current?.()
    const startX = event.clientX
    const startWidth = navWidth
    const onMove = (moveEvent: PointerEvent): void => {
      const next = startWidth + (moveEvent.clientX - startX)
      setNavWidth(Math.min(NAV_MAX_WIDTH, Math.max(NAV_MIN_WIDTH, next)))
    }
    const onUp = (): void => {
      window.removeEventListener('pointermove', onMove)
      window.removeEventListener('pointerup', onUp)
      resizeCleanupRef.current = null
    }
    window.addEventListener('pointermove', onMove)
    window.addEventListener('pointerup', onUp)
    resizeCleanupRef.current = () => {
      window.removeEventListener('pointermove', onMove)
      window.removeEventListener('pointerup', onUp)
    }
  }

  const editorsReady = !pageLoading && loadError == null && scopesReady

  return (
    <div className="udt-settings-page">
      <header className="udt-settings-page__header">
        <h1 className="udt-settings-page__title">{t('settings.title')}</h1>
        <p className="udt-settings-page__description">{t('settings.description')}</p>
      </header>
      <div className="udt-settings-page__surface">
        <nav
          className="udt-settings-page__nav"
          style={{ '--udt-settings-nav-width': `${navWidth}px` } as React.CSSProperties}
          aria-label={t('settings.title')}
        >
          <ul className="udt-settings-page__nav-list">
            {navItems.map((item) => {
              const isActive = item.key === active
              return (
                <li key={item.key}>
                  <button
                    type="button"
                    className={`udt-settings-nav-item${isActive ? ' udt-settings-nav-item--active' : ''}`}
                    aria-current={isActive ? 'true' : undefined}
                    onClick={() => setActive(item.key)}
                  >
                    <span className="udt-settings-nav-item__accent" />
                    <span className="udt-settings-nav-item__icon">{item.icon}</span>
                    <Tooltip title={t(item.labelKey)}>
                      <span className="udt-settings-nav-item__label">{t(item.labelKey)}</span>
                    </Tooltip>
                  </button>
                </li>
              )
            })}
          </ul>
        </nav>
        <div
          className="udt-settings-page__splitter"
          role="separator"
          aria-orientation="vertical"
          onPointerDown={startResize}
        />
        <section
          ref={contentRef}
          key={editorsReady ? active : 'settings-pending'}
          className="udt-settings-page__content udt-settings-page__content-anim"
          aria-label={t(`settings.nav.${active}`)}
        >
          <header className="udt-settings-page__section-header">
            <h2 className="udt-settings-page__section-title">{t(`settings.nav.${active}`)}</h2>
          </header>
          {pageLoading ? (
            <SkeletonList rows={4} withIcon={false} accessory="select" />
          ) : loadError != null || !scopesReady ? (
            <SettingsLoadError message={loadError} onRetry={retry} />
          ) : (
            renderSection(active)
          )}
        </section>
      </div>
    </div>
  )
}
