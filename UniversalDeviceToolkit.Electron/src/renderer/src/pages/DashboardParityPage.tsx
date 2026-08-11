import { useEffect, useState } from 'react'
import { EditOutlined } from '@ant-design/icons'
import { Spin } from 'antd'
import { useTranslation } from 'react-i18next'
import { dashboardApi, type DashboardConfig, type DashboardGroup } from '../api/dashboard'
import EditDashboardModal from '../components/dashboard-parity/EditDashboardModal'
import DashboardFeatureGroups from '../components/dashboard-parity/DashboardFeatureGroupsHardware'
import { DEFAULT_DASHBOARD_GROUPS } from '../components/dashboard-parity/dashboardItems'
import SensorSection from '../components/dashboard/SensorSection'
import { useFeaturesStore } from '../stores/featuresStore'
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

    Promise.all([
      useSensorsStore.getState().start(),
      useFeaturesStore.getState().load(),
      dashboardApi.getConfig()
    ])
      .then(([, , dashboardConfig]) => {
        if (cancelled) return
        setConfig(dashboardConfig)
      })
      .catch((reason: unknown) => {
        if (cancelled) return
        setError((reason as Error).message)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })

    return () => {
      cancelled = true
      void useSensorsStore.getState().stop()
    }
  }, [])

  if (loading) {
    return <div className="udt-parity-dashboard__loading"><Spin size="large" /></div>
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
        <EditOutlined />
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
