import { useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import AppStatusBanner from './AppStatusBanner'
import { updateApi } from '../api/update'
import type { SoftwareDisablerApp } from '../api/software'
import { useSoftwareStore } from '../stores/softwareStore'
import { useStatusBannerStore } from '../stores/statusBannerStore'

const BANNER_UPDATE = 'updateAvailable'

const SOFTWARE_BANNERS: { app: SoftwareDisablerApp; id: string; messageKey: string }[] = [
  { app: 'vantage', id: 'vantageRunning', messageKey: 'statusBanner.vantageRunning' },
  { app: 'legionZone', id: 'legionZoneRunning', messageKey: 'statusBanner.legionZoneRunning' },
  { app: 'fnKeys', id: 'fnKeysRunning', messageKey: 'statusBanner.fnKeysRunning' }
]

/**
 * Host for persistent corner toasts — port of Electron MainWindow._statusNotificationStack
 * (AppStatusBanner instances): update available, plugin extensions disabled, and
 * the three software-conflict warnings (Vantage / Legion Zone / Lenovo Hotkeys).
 * Renders as a bottom-right overlay so page content is not pushed down.
 */
export default function AppStatusBanners(): React.JSX.Element {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const banners = useStatusBannerStore((s) => s.banners)
  const show = useStatusBannerStore((s) => s.show)
  const hide = useStatusBannerStore((s) => s.hide)
  const remove = useStatusBannerStore((s) => s.remove)
  const softwareStatuses = useSoftwareStore((s) => s.statuses)
  const softwareStart = useSoftwareStore((s) => s.start)

  // Keep transient AppNotificationHost toasts stacked above these persistent
  // cards when both share the bottom-right corner (Electron single StackPanel).
  useEffect(() => {
    const offset = banners.length === 0 ? 0 : banners.length * 56 + Math.max(0, banners.length - 1) * 8
    document.documentElement.style.setProperty('--udt-status-banner-stack-height', `${offset}px`)
    return () => {
      document.documentElement.style.removeProperty('--udt-status-banner-stack-height')
    }
  }, [banners.length])

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

  // Electron MainWindow software disabler indicators: banners follow the
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
          // Electron AppStatusBanner always exposes a close button; session dismiss
          // matches Closed → Collapsed until the user leaves / status changes.
          closable: true
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
