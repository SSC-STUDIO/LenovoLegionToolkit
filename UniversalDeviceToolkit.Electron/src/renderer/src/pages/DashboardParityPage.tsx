import { useCallback, useEffect, useState } from 'react'
import { Edit24Regular } from '@fluentui/react-icons'
import { useTranslation } from 'react-i18next'
import { isHostUnavailableError, sanitizeBridgeError, waitForHostReady } from '../api/bridge'
import { dashboardApi, type DashboardConfig, type DashboardGroup } from '../api/dashboard'
import EditDashboardModal from '../components/dashboard-parity/EditDashboardModal'
import DashboardFeatureGroups from '../components/dashboard-parity/DashboardFeatureGroupsHardware'
import { DEFAULT_DASHBOARD_GROUPS } from '../components/dashboard-parity/dashboardItems'
import DashboardSkeleton from '../components/DashboardSkeleton'
import SensorSection from '../components/dashboard/SensorSection'
import { useFeaturesStore } from '../stores/featuresStore'
import { useLoadingStore } from '../stores/loadingStore'
import '../components/dashboard-parity/dashboardParity.css'

function normalizedGroups(config: DashboardConfig): DashboardGroup[] {
  return config.groups != null && config.groups.length > 0 ? config.groups : DEFAULT_DASHBOARD_GROUPS
}

export default function DashboardParityPage(): React.JSX.Element {
  const { t } = useTranslation()
  const [config, setConfig] = useState<DashboardConfig | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [editOpen, setEditOpen] = useState(false)
  const [reloadToken, setReloadToken] = useState(0)

  const retry = useCallback(() => {
    setError(null)
    setLoading(true)
    setConfig(null)
    setReloadToken((value) => value + 1)
  }, [])

  useEffect(() => {
    let cancelled = false
    // DashboardPage owns its loading chrome (Electron LoadingChromeOwnership.Page):
    // the page renders its own sensors+groups skeleton, so the session is
    // silent — the global spinner overlay never flashes on this page.
    const loadingId = useLoadingStore.getState().start(
      t('loading.dashboard', { defaultValue: 'Loading dashboard…' }),
      { canCancel: false, silent: true }
    )

    const load = async (): Promise<void> => {
      await waitForHostReady()
      if (cancelled) return

      // SensorSection owns the sensor subscription lifecycle (mount/unmount +
      // persisted interval); this page only loads the feature list and config.
      const [, dashboardConfig] = await Promise.all([
        useFeaturesStore.getState().load(),
        dashboardApi.getConfig()
      ])
      if (cancelled) return
      setConfig(dashboardConfig)
      useLoadingStore.getState().finish(loadingId)
    }

    load()
      .catch((reason: unknown) => {
        if (cancelled) return
        const raw = sanitizeBridgeError(reason)
        const message = isHostUnavailableError(raw)
          ? t('home.hostUnavailable', {
              defaultValue: 'The backend host is not running. Wait a moment and retry, or restart the app.'
            })
          : raw
        setError(message)
        // Dismiss the overlay so the page-level error + Retry action is visible.
        useLoadingStore.getState().finish(loadingId)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
      useLoadingStore.getState().finish(loadingId)
    }
  }, [reloadToken, t])

  if (loading) {
    return (
      <div className="udt-parity-dashboard udt-parity-dashboard__loading udt-content-wide">
        <DashboardSkeleton />
      </div>
    )
  }

  if (error != null || config == null) {
    return (
      <div className="udt-parity-dashboard__error">
        <h2>{t('common.error')}</h2>
        {error != null && <p>{error}</p>}
        <button type="button" className="udt-btn udt-btn--secondary" onClick={retry}>
          {t('common.retry', { defaultValue: 'Retry' })}
        </button>
      </div>
    )
  }

  return (
    <div className="udt-parity-dashboard udt-content-wide">
      {config.showSensors !== false && <SensorSection />}
      <DashboardFeatureGroups groups={normalizedGroups(config)} />
      <button
        type="button"
        className="udt-parity-dashboard__customize"
        onClick={() => setEditOpen(true)}
      >
        <Edit24Regular aria-hidden="true" />
        {t('dashboard.customize')}
      </button>
      {editOpen && (
        <EditDashboardModal
          config={config}
          onCancel={() => setEditOpen(false)}
          onSaved={() => {
            setEditOpen(false)
            void dashboardApi.getConfig().then(setConfig).catch(() => undefined)
          }}
        />
      )}
    </div>
  )
}
