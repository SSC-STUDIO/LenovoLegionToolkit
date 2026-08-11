import { useEffect, useState } from 'react'
import {
  ApiOutlined,
  AppstoreOutlined,
  BgColorsOutlined,
  DesktopOutlined,
  EyeOutlined,
  KeyOutlined,
  PoweroffOutlined,
  SyncOutlined
} from '@ant-design/icons'
import { useTranslation } from 'react-i18next'
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
  { key: 'appearance', labelKey: 'settings.nav.appearance', icon: <BgColorsOutlined /> },
  { key: 'application', labelKey: 'settings.nav.application', icon: <AppstoreOutlined /> },
  { key: 'power', labelKey: 'settings.nav.power', icon: <PoweroffOutlined /> },
  { key: 'display', labelKey: 'settings.nav.display', icon: <DesktopOutlined /> },
  { key: 'smartKeys', labelKey: 'settings.nav.smartKeys', icon: <KeyOutlined /> },
  { key: 'update', labelKey: 'settings.nav.update', icon: <SyncOutlined /> },
  { key: 'integrations', labelKey: 'settings.nav.integrations', icon: <ApiOutlined /> },
  { key: 'osd', labelKey: 'settings.nav.osd', icon: <EyeOutlined /> }
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

  useEffect(() => {
    let cancelled = false
    const loadingId = useLoadingStore.getState().start(
      t('loading.settings', { defaultValue: 'Loading settings…' }),
      { canCancel: false }
    )
    Promise.all([featuresApi.list(), useSettingsStore.getState().load()])
      .then(([infos]) => {
        if (cancelled) return
        setSupportsLenovoHardware(
          infos.some((info) => info.supported && LENOVO_FEATURE_KEYS.includes(info.key))
        )
        useLoadingStore.getState().finish(loadingId)
      })
      .catch(() => {
        // Keep the default (all sections visible) when the probe fails.
        useLoadingStore.getState().finish(loadingId)
      })
      .finally(() => {
        if (!cancelled) setPageLoading(false)
      })
    return () => {
      cancelled = true
      useLoadingStore.getState().finish(loadingId)
    }
  }, [])

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
    const startX = event.clientX
    const startWidth = navWidth
    const onMove = (moveEvent: PointerEvent): void => {
      const next = startWidth + (moveEvent.clientX - startX)
      setNavWidth(Math.min(NAV_MAX_WIDTH, Math.max(NAV_MIN_WIDTH, next)))
    }
    const onUp = (): void => {
      window.removeEventListener('pointermove', onMove)
      window.removeEventListener('pointerup', onUp)
    }
    window.addEventListener('pointermove', onMove)
    window.addEventListener('pointerup', onUp)
  }

  return (
    <div className="udt-settings-page">
      <header className="udt-settings-page__header">
        <h1 className="udt-settings-page__title">{t('settings.title')}</h1>
        <p className="udt-settings-page__description">{t('settings.description')}</p>
      </header>
      <div className="udt-settings-page__surface">
        <nav
          className="udt-settings-page__nav"
          style={{ width: navWidth, minWidth: NAV_MIN_WIDTH, maxWidth: NAV_MAX_WIDTH }}
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
                    <span className="udt-settings-nav-item__label" title={t(item.labelKey)}>
                      {t(item.labelKey)}
                    </span>
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
          key={active}
          className="udt-settings-page__content udt-settings-page__content-anim"
          aria-label={t(`settings.nav.${active}`)}
        >
          <header className="udt-settings-page__section-header">
            <h2 className="udt-settings-page__section-title">{t(`settings.nav.${active}`)}</h2>
          </header>
          {pageLoading ? <SkeletonList rows={4} /> : renderSection(active)}
        </section>
      </div>
    </div>
  )
}
