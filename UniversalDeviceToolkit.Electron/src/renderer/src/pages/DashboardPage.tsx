import { useEffect, useState } from 'react'
import { Result, Spin, Typography } from 'antd'
import { useTranslation } from 'react-i18next'
import { dashboardApi, type DashboardConfig, type DashboardGroup } from '../api/dashboard'
import { featuresApi } from '../api/features'
import { useSensorsStore } from '../stores/sensorsStore'
import FeatureGroupGrid, { type DashboardGroupConfig } from '../components/dashboard/FeatureGroupGrid'
import SensorSection from '../components/dashboard/SensorSection'

function mapGroups(config: DashboardConfig): DashboardGroupConfig[] {
  return (config.groups ?? []).map((group: DashboardGroup) => ({
    type: group.type,
    customName: group.customName ?? undefined,
    items: group.items
  }))
}

export default function DashboardPage(): React.JSX.Element {
  const { t } = useTranslation()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [config, setConfig] = useState<DashboardConfig | null>(null)
  const [groups, setGroups] = useState<DashboardGroupConfig[]>([])

  useEffect(() => {
    let cancelled = false
    Promise.all([
      useSensorsStore.getState().start(),
      featuresApi.list(),
      dashboardApi.getConfig()
    ])
      .then(([, , dashboardConfig]) => {
        if (cancelled) return
        setConfig(dashboardConfig)
        setGroups(mapGroups(dashboardConfig))
        setLoading(false)
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setError((err as Error).message)
        setLoading(false)
      })
    return () => {
      cancelled = true
      void useSensorsStore.getState().stop()
    }
  }, [])

  if (loading) {
    return (
      <div
        style={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          minHeight: 320
        }}
      >
        <Spin size="large" />
      </div>
    )
  }

  if (error) {
    return <Result status="error" title={t('common.error')} subTitle={error} />
  }

  return (
    <div className="udt-dashboard-page">
      <Typography.Title level={3} className="udt-dashboard-page__title">
        {t('dashboard.title')}
      </Typography.Title>
      {config?.showSensors && <SensorSection />}
      <FeatureGroupGrid groups={groups} />
    </div>
  )
}
