import { useEffect } from 'react'
import { Card, Col, Descriptions, Row, Tag } from 'antd'
import { useTranslation } from 'react-i18next'
import { useSensorsStore } from '../../stores/sensorsStore'
import SensorGauge, { formatSensorValue } from './SensorGauge'

const GAUGE_COLORS = {
  cpu: '#fa541c',
  gpu: '#1677ff',
  memory: '#52c41a'
} as const

function toGigabytes(mb: number | null | undefined): number | null {
  return mb == null ? null : mb / 1024
}

function metricText(
  value: number | null | undefined,
  unit: string,
  digits: number,
  fallback: string
): string {
  if (value == null || !Number.isFinite(value)) return fallback
  return `${value.toFixed(digits)} ${unit}`
}

export default function SensorSection(): React.JSX.Element {
  const { t } = useTranslation()
  const snapshot = useSensorsStore((s) => s.snapshot)

  useEffect(() => {
    const store = useSensorsStore.getState()
    void store.loadStatus()
    void store.loadSnapshot()
    void store.start(1)
    return () => {
      void useSensorsStore.getState().stop()
    }
  }, [])

  const cpu = snapshot?.cpu
  const gpu = snapshot?.gpu
  const memory = snapshot?.memory
  const cpuName = snapshot?.info?.cpuName
  const gpuName = snapshot?.info?.gpuName
  const storageTemps = snapshot?.storage?.temperatures
  const notAvailable = t('dashboard.notAvailable')

  const cpuTitle = cpuName ? `${t('dashboard.cpu')} · ${cpuName}` : t('dashboard.cpu')
  const gpuTitle = gpuName ? `${t('dashboard.gpu')} · ${gpuName}` : t('dashboard.gpu')

  return (
    <div>
      {snapshot?.source === 'vendor' && (
        <Tag color="orange" style={{ marginBottom: 12 }}>
          vendor
        </Tag>
      )}
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={8}>
          <Card title={cpuTitle} style={{ height: '100%' }}>
            <SensorGauge
              value={cpu?.temperature}
              max={100}
              unit="°C"
              label={t('dashboard.temperature')}
              color={GAUGE_COLORS.cpu}
            />
            <Descriptions
              size="small"
              column={1}
              items={[
                {
                  key: 'usage',
                  label: t('dashboard.usage'),
                  children: metricText(cpu?.usage, '%', 0, notAvailable)
                },
                {
                  key: 'power',
                  label: t('dashboard.power'),
                  children: metricText(cpu?.power, 'W', 0, notAvailable)
                },
                {
                  key: 'fanSpeed',
                  label: t('dashboard.fanSpeed'),
                  children: metricText(cpu?.fanSpeed, 'RPM', 0, notAvailable)
                }
              ]}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={8}>
          <Card title={gpuTitle} style={{ height: '100%' }}>
            <SensorGauge
              value={gpu?.temperature}
              max={100}
              unit="°C"
              label={t('dashboard.temperature')}
              color={GAUGE_COLORS.gpu}
            />
            <Descriptions
              size="small"
              column={1}
              items={[
                {
                  key: 'usage',
                  label: t('dashboard.usage'),
                  children: metricText(gpu?.usage, '%', 0, notAvailable)
                },
                {
                  key: 'power',
                  label: t('dashboard.power'),
                  children: metricText(gpu?.power, 'W', 0, notAvailable)
                },
                {
                  key: 'fanSpeed',
                  label: t('dashboard.fanSpeed'),
                  children: metricText(gpu?.fanSpeed, 'RPM', 0, notAvailable)
                },
                {
                  key: 'vram',
                  label: t('dashboard.vram'),
                  children: metricText(toGigabytes(gpu?.vramUsedMb), 'GB', 1, notAvailable)
                }
              ]}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={8}>
          <Card title={t('dashboard.memory')} style={{ height: '100%' }}>
            <SensorGauge
              value={memory?.usage}
              max={100}
              unit="%"
              label={t('dashboard.usage')}
              color={GAUGE_COLORS.memory}
            />
            <Descriptions
              size="small"
              column={1}
              items={[
                {
                  key: 'usedMb',
                  label: t('dashboard.memoryUsed'),
                  children: metricText(toGigabytes(memory?.usedMb), 'GB', 1, notAvailable)
                },
                {
                  key: 'totalMb',
                  label: t('dashboard.memoryTotal'),
                  children: metricText(toGigabytes(memory?.totalMb), 'GB', 1, notAvailable)
                },
                ...(storageTemps && storageTemps.length > 0
                  ? [
                      {
                        key: 'storageTemp',
                        label: t('dashboard.storageTemp'),
                        children:
                          storageTemps.map((v) => formatSensorValue(v, 0)).join(' / ') + ' °C'
                      }
                    ]
                  : [])
              ]}
            />
          </Card>
        </Col>
      </Row>
    </div>
  )
}
