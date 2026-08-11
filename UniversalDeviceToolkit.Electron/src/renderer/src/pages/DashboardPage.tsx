import { useEffect, useState } from 'react'
import { Result, Select, Spin, Typography } from 'antd'
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

const POWER_MODE_OPTIONS = [
  { value: 'quiet', label: '安静' },
  { value: 'balance', label: '平衡' },
  { value: 'performance', label: '性能' },
  { value: 'extreme', label: '极限' },
  { value: 'godMode', label: '自定义' }
]

export default function DashboardPage(): React.JSX.Element {
  const { t } = useTranslation()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [config, setConfig] = useState<DashboardConfig | null>(null)
  const [groups, setGroups] = useState<DashboardGroupConfig[]>([])
  const [powerMode, setPowerMode] = useState<string>('performance')

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
      {config?.showSensors && <SensorSection />}
      <div className="udt-power-section">
        <h3 className="udt-power-section__title">{t('dashboard.group.power')}</h3>
        <div className="udt-power-card">
          <div className="udt-power-card__item">
            <div className="udt-power-card__info">
              <span className="udt-power-card__icon">⚡</span>
              <div>
                <div className="udt-power-card__label">{t('feature.powerMode')}</div>
                <div className="udt-power-card__desc">{t('feature.powerMode.desc')}</div>
                <div className="udt-power-card__hint">{t('feature.powerMode.hint')}</div>
              </div>
            </div>
            <div className="udt-power-card__action">
              <Select
                value={powerMode}
                onChange={setPowerMode}
                options={POWER_MODE_OPTIONS}
                style={{ width: 120 }}
                popupMatchSelectWidth={false}
              />
            </div>
          </div>
          <div className="udt-power-card__divider" />
          <div className="udt-power-card__item">
            <div className="udt-power-card__info">
              <span className="udt-power-card__icon">🔋</span>
              <div>
                <div className="udt-power-card__label">{t('feature.battery')}</div>
                <div className="udt-power-card__desc">{t('feature.battery.desc')}</div>
              </div>
            </div>
            <div className="udt-power-card__action">
              <Select
                defaultValue="standard"
                options={[
                  { value: 'standard', label: '养护模式' },
                  { value: 'max', label: '充满模式' },
                  { value: 'custom', label: '自定义' }
                ]}
                style={{ width: 120 }}
                popupMatchSelectWidth={false}
              />
            </div>
          </div>
        </div>
      </div>
      <FeatureGroupGrid groups={groups} />
    </div>
  )
}
