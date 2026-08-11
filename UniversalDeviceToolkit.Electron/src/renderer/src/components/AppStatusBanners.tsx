import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import AppStatusBanner from './AppStatusBanner'
import { updateApi } from '../api/update'
import type { SoftwareDisablerApp } from '../api/software'
import { useSettingsStore } from '../stores/settingsStore'
import { useSoftwareStore } from '../stores/softwareStore'
import { useStatusBannerStore } from '../stores/statusBannerStore'

const BANNER_UPDATE = 'updateAvailable'
const BANNER_PLUGINS_DISABLED = 'pluginExtensionsDisabled'

const SOFTWARE_BANNERS: { app: SoftwareDisablerApp; id: string; messageKey: string }[] = [
  { app: 'vantage', id: 'vantageRunning', messageKey: 'statusBanner.vantageRunning' },
  { app: 'legionZone', id: 'legionZoneRunning', messageKey: 'statusBanner.legionZoneRunning' },
  { app: 'fnKeys', id: 'fnKeysRunning', messageKey: 'statusBanner.fnKeysRunning' }
]

/**
 * Host for persistent status banners — port of the WPF MainWindow status
 * notification stack (AppStatusBanner instances): update available, plugin
 * extensions disabled and the three software-conflict banners (Vantage /
 * Legion Zone / Lenovo Hotkeys running).
 */
export default function AppStatusBanners(): React.JSX.Element {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const banners = useStatusBannerStore((s) => s.banners)
  const show = useStatusBannerStore((s) => s.show)
  const hide = useStatusBannerStore((s) => s.hide)
  const remove = useStatusBannerStore((s) => s.remove)
  const scopes = useSettingsStore((s) => s.scopes)
  const load = useSettingsStore((s) => s.load)
  const softwareStatuses = useSoftwareStore((s) => s.statuses)
  const softwareStart = useSoftwareStore((s) => s.start)

  useEffect(() => {
    let cancelled = false
    void updateApi
      .check(false)
      .then((result) => {
        if (cancelled || !result?.available) return
        const version = typeof result.version === 'string' && result.version ? result.version : null
        show({
          id: BANNER_UPDATE,
          severity: 'Success',
          message: version
            ? t('statusBanner.updateAvailableWithVersion', { version })
            : t('statusBanner.updateAvailable'),
          persistent: true,
          closable: false,
          onClick: () => navigate('/settings')
        })
      })
      .catch(() => undefined)
    return () => {
      cancelled = true
    }
  }, [navigate, show, t])

  useEffect(() => {
    void load(['application'])
  }, [load])

  useEffect(() => {
    const app =
      typeof scopes.application === 'object' && scopes.application !== null
        ? (scopes.application as Record<string, unknown>)
        : {}
    if (app.ExtensionsEnabled === false) {
      show({
        id: BANNER_PLUGINS_DISABLED,
        severity: 'Warning',
        message: t('statusBanner.pluginExtensionsDisabled'),
        persistent: true,
        closable: true
      })
    } else {
      // Programmatic removal — do not remember it as a user dismissal.
      remove(BANNER_PLUGINS_DISABLED)
    }
  }, [scopes.application, show, remove, t])

  // WPF MainWindow software disabler indicators: banners follow the
  // VantageDisabler / LegionZoneDisabler / FnKeysDisabler status (polled).
  useEffect(() => {
    const stop = softwareStart(5000)
    return stop
  }, [softwareStart])

  useEffect(() => {
    for (const banner of SOFTWARE_BANNERS) {
      if (softwareStatuses[banner.app] === 'Enabled') {
        show({
          id: banner.id,
          severity: 'Warning',
          message: t(banner.messageKey),
          persistent: true,
          closable: false
        })
      } else {
        remove(banner.id)
      }
    }
  }, [softwareStatuses, show, remove, t])

  if (banners.length === 0) return <></>
  return (
    <div className="udt-status-banner-stack">
      {banners.map((banner) => (
        <AppStatusBanner
          key={banner.id}
          severity={banner.severity}
          message={banner.message}
          closable={banner.closable}
          onClick={banner.onClick}
          onClosed={() => hide(banner.id)}
        />
      ))}
    </div>
  )
}
