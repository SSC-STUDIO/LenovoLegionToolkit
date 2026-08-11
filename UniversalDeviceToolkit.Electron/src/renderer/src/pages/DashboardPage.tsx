import { useEffect, useState } from 'react'
import { Select, Spin } from 'antd'
import { DashboardOutlined, SettingOutlined } from '@ant-design/icons'
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

const BATTERY_MODE_OPTIONS = [
  { value: 'conservation', label: '养护模式' },
  { value: 'normal', label: '普通模式' },
  { value: 'rapidCharge', label: '快充模式' }
]

export default function DashboardPage(): React.JSX.Element {
  const { t } = useTranslation()
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [config, setConfig] = useState<DashboardConfig | null>(null)
  const [groups, setGroups] = useState<DashboardGroupConfig[]>([])
  const [powerMode, setPowerMode] = useState<string>('performance')
  const [batteryMode, setBatteryMode] = useState<string>('normal')

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
    return (
      <div className="udt-page-error">
        <h2>{t('common.error')}</h2>
        <p>{error}</p>
      </div>
    )
  }

  return (
    <div className="udt-dashboard-page">
      <h1 className="udt-dashboard-page__title">{t('dashboard.title')}</h1>
      {config?.showSensors !== false && <SensorSection />}
      <div className="udt-feature-groups">
        <section className="udt-feature-group">
          <h3>{t('dashboard.group.power')}</h3>
          <div className="udt-feature-group__items">
            <div className="udt-feature-card">
              <div className="udt-feature-card__content">
                <span className="udt-feature-card__icon"><DashboardOutlined /></span>
                <div className="udt-feature-card__copy">
                  <div className="udt-feature-card__title">{t('feature.powerMode')}</div>
                  <div className="udt-feature-card__description">{t('feature.powerMode.desc')}</div>
                  <div className="udt-feature-card__hint">{t('feature.powerMode.hint')}</div>
                </div>
                <div className="udt-feature-card__accessory">
                  <Select
                    size="small"
                    className="udt-feature-card__select"
                    value={powerMode}
                    options={POWER_MODE_OPTIONS}
                    onChange={(value) => setPowerMode(value)}
                  />
                  <ButtonText icon={<SettingOutlined />} />
                </div>
              </div>
            </div>
            <div className="udt-feature-card">
              <div className="udt-feature-card__content">
                <span className="udt-feature-card__icon"><ThunderboltIcon /></span>
                <div className="udt-feature-card__copy">
                  <div className="udt-feature-card__title">{t('feature.battery')}</div>
                  <div className="udt-feature-card__description">{t('feature.battery.desc')}</div>
                </div>
                <div className="udt-feature-card__accessory">
                  <Select
                    size="small"
                    className="udt-feature-card__select"
                    value={batteryMode}
                    options={BATTERY_MODE_OPTIONS}
                    onChange={(value) => setBatteryMode(value)}
                  />
                </div>
              </div>
            </div>
          </div>
        </section>
      </div>
      <FeatureGroupGrid groups={groups} />
    </div>
  )
}

function ButtonText({ icon }: { icon: React.ReactNode }): React.JSX.Element {
  return (
    <button type="button" className="udt-feature-card__config-btn" aria-label="settings">
      {icon}
    </button>
  )
}

function ThunderboltIcon(): React.JSX.Element {
  return (
    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
      <path d="M13 2L4 14h6l-1 8 9-12h-6l1-8z" strokeLinejoin="round" />
    </svg>
  )
}
