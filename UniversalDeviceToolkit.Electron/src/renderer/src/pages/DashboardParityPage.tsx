import { useEffect, useState } from 'react'
import { Edit24Regular } from '@fluentui/react-icons'
import { useTranslation } from 'react-i18next'
import { dashboardApi, type DashboardConfig, type DashboardGroup } from '../api/dashboard'
import EditDashboardModal from '../components/dashboard-parity/EditDashboardModal'
import DashboardFeatureGroups from '../components/dashboard-parity/DashboardFeatureGroupsHardware'
import { DEFAULT_DASHBOARD_GROUPS } from '../components/dashboard-parity/dashboardItems'
import SensorSection from '../components/dashboard/SensorSection'
import { SkeletonList } from '../components/Skeleton'
import { useFeaturesStore } from '../stores/featuresStore'
import { useLoadingStore } from '../stores/loadingStore'
import { useSensorsStore } from '../stores/sensorsStore'
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

  useEffect(() => {
    let cancelled = false
    const loadingId = useLoadingStore.getState().start(
      t('loading.dashboard', { defaultValue: 'Loading dashboard…' }),
      { canCancel: false }
    )

    Promise.all([
      useSensorsStore.getState().start(),
      useFeaturesStore.getState().load(),
      dashboardApi.getConfig()
    ])
      .then(([, , dashboardConfig]) => {
        if (cancelled) return
        setConfig(dashboardConfig)
        useLoadingStore.getState().finish(loadingId)
      })
      .catch((reason: unknown) => {
        if (cancelled) return
        const message = (reason as Error).message
        setError(message)
        useLoadingStore.getState().fail(loadingId, message)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
      useLoadingStore.getState().finish(loadingId)
      void useSensorsStore.getState().stop()
    }
  }, [])

  if (loading) {
    return (
      <div className="udt-parity-dashboard__loading">
        <SkeletonList rows={3} />
      </div>
    )
  }

  if (error != null || config == null) {
    return (
      <div className="udt-parity-dashboard__error">
        <h2>{t('common.error')}</h2>
        {error != null && <p>{error}</p>}
      </div>
    )
  }

  return (
    <div className="udt-parity-dashboard">
      <h1>{t('dashboard.title')}</h1>
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
