import { useEffect, useMemo, useRef } from 'react'
import { Col, Row, Tag, theme } from 'antd'
import { useTranslation } from 'react-i18next'
import { useSensorsStore } from '../../stores/sensorsStore'
import { useThemeStore } from '../../stores/themeStore'
import SensorGauge, { formatSensorValue } from './SensorGauge'
import TrendChart, { type TrendSeries } from './TrendChart'

const TEMP_WARNING = 60
const TEMP_CRITICAL = 75
const COLOR_WARNING = '#F7630C'
const COLOR_CRITICAL = '#E81123'
const TREND_POINTS = 60
const ROW_HEIGHT = 24
const TREND_HEIGHT = 120

// WPF chart palette (DesignTokens.xaml): temperature = amber, utilization = blue.
const CPU_TEMP_LINE = '#D9883B'
const GPU_TEMP_LINE = '#4F9DF7'
const MEMORY_COLOR = '#52C41A'

interface TrendHistory {
  labels: string[]
  cpu: (number | null)[]
  gpu: (number | null)[]
}

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

function formatFrequency(mhz: number | null | undefined): string {
  if (mhz == null || !Number.isFinite(mhz)) return '--'
  return mhz >= 1000 ? `${(mhz / 1000).toFixed(2)} GHz` : `${mhz.toFixed(0)} MHz`
}

function temperatureColor(temp: number | null | undefined, fallback: string): string {
  if (temp == null || !Number.isFinite(temp)) return fallback
  if (temp > TEMP_CRITICAL) return COLOR_CRITICAL
  if (temp >= TEMP_WARNING) return COLOR_WARNING
  return fallback
}

export interface SensorCardProps {
  title: string
  model?: string | null
  labelColor: string
  valueColor: string
  cardBackground: string
  cardBorder: string
  panelBackground: string
  gauge: React.JSX.Element
  metrics: { label: string; value: string; color?: string }[]
  trend: React.JSX.Element
}

function SensorCard({
  title,
  model,
  labelColor,
  valueColor,
  cardBackground,
  cardBorder,
  panelBackground,
  gauge,
  metrics,
  trend
}: SensorCardProps): React.JSX.Element {
  return (
    <div
      style={{
        background: cardBackground,
        borderRadius: 18,
        border: `1px solid ${cardBorder}`,
        padding: '8px 10px',
        height: '100%',
        minWidth: 0
      }}
    >
      <div
        style={{
          display: 'flex',
          alignItems: 'baseline',
          justifyContent: 'space-between',
          gap: 8,
          minWidth: 0
        }}
      >
        <span style={{ fontSize: 19, fontWeight: 500, color: valueColor, whiteSpace: 'nowrap' }}>
          {title}
        </span>
        {model != null && model !== '' && (
          <span
            title={model}
            style={{
              fontSize: 14,
              color: labelColor,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
              minWidth: 0
            }}
          >
            {model}
          </span>
        )}
      </div>

      <div style={{ display: 'flex', gap: 14, marginTop: 10, alignItems: 'center' }}>
        {gauge}
        <div
          style={{
            flex: 1,
            minWidth: 0,
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center'
          }}
        >
          {metrics.map((row, index) => (
            <div
              key={index}
              style={{
                height: ROW_HEIGHT,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                gap: 8,
                minWidth: 0
              }}
            >
              <span style={{ fontSize: 14, color: labelColor, flexShrink: 0 }}>{row.label}</span>
              <span
                style={{
                  fontSize: 14,
                  fontWeight: 500,
                  color: row.color ?? valueColor,
                  whiteSpace: 'nowrap'
                }}
              >
                {row.value}
              </span>
            </div>
          ))}
        </div>
      </div>

      <div
        style={{
          marginTop: 14,
          height: TREND_HEIGHT,
          borderRadius: 12,
          background: panelBackground,
          overflow: 'hidden'
        }}
      >
        {trend}
      </div>
    </div>
  )
}

export default function SensorSection(): React.JSX.Element {
  const { t } = useTranslation()
  const { token } = theme.useToken()
  const isDark = useThemeStore((s) => s.themeMode === 'dark')
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

  const historyRef = useRef<TrendHistory>({ labels: [], cpu: [], gpu: [] })
  const lastTsRef = useRef<string | null>(null)

  if (snapshot?.ts != null && lastTsRef.current !== snapshot.ts) {
    lastTsRef.current = snapshot.ts
    const history = historyRef.current
    history.labels.push(new Date(snapshot.ts).toLocaleTimeString([], { hour12: false }))
    history.cpu.push(snapshot.cpu?.temperature ?? null)
    history.gpu.push(snapshot.gpu?.temperature ?? null)
    if (history.labels.length > TREND_POINTS) {
      history.labels.shift()
      history.cpu.shift()
      history.gpu.shift()
    }
  }

  const trendSeries = useMemo<TrendSeries[]>(() => {
    const history = historyRef.current
    return [
      { name: t('dashboard.sensor.cpu'), color: CPU_TEMP_LINE, data: [...history.cpu] },
      { name: t('dashboard.sensor.gpu'), color: GPU_TEMP_LINE, data: [...history.gpu] }
    ]
  }, [snapshot, t])

  const cpu = snapshot?.cpu
  const gpu = snapshot?.gpu
  const memory = snapshot?.memory
  const cpuName = snapshot?.info?.cpuName
  const gpuName = snapshot?.info?.gpuName
  const storageTemps = snapshot?.storage?.temperatures
  const notAvailable = t('dashboard.notAvailable')

  const accent = token.colorPrimary
  const valueColor = token.colorText
  const labelColor = token.colorTextSecondary
  const cardBackground = isDark ? '#303030' : token.colorBgContainer
  const cardBorder = isDark ? 'rgba(255, 255, 255, 0.10)' : token.colorBorderSecondary
  const panelBackground = isDark ? 'rgba(255, 255, 255, 0.045)' : token.colorFillQuaternary
  const trendLabels = [...historyRef.current.labels]

  const storageTempValue =
    storageTemps != null && storageTemps.length > 0
      ? `${storageTemps.map((v) => formatSensorValue(v, 0)).join(' / ')} °C`
      : null
  const storageTempMax = Math.max(
    ...(storageTemps ?? []).filter((v): v is number => v != null && Number.isFinite(v)),
    Number.NEGATIVE_INFINITY
  )

  const cpuGaugeColor = temperatureColor(cpu?.temperature, accent)
  const gpuGaugeColor = temperatureColor(gpu?.temperature, accent)

  const vramUsed = toGigabytes(gpu?.vramUsedMb)
  const vramTotal = toGigabytes(gpu?.vramTotalMb)
  const vramText =
    vramUsed == null
      ? notAvailable
      : `${vramUsed.toFixed(1)} GB${vramTotal != null ? ` / ${vramTotal.toFixed(1)} GB` : ''}`

  return (
    <div>
      {snapshot?.source === 'vendor' && (
        <Tag color="orange" style={{ marginBottom: 12 }}>
          vendor
        </Tag>
      )}
      <Row gutter={[16, 16]}>
        <Col xs={24} xl={8}>
          <SensorCard
            title={t('dashboard.sensor.cpu')}
            model={cpuName}
            labelColor={labelColor}
            valueColor={valueColor}
            cardBackground={cardBackground}
            cardBorder={cardBorder}
            panelBackground={panelBackground}
            gauge={
              <SensorGauge
                value={cpu?.temperature}
                max={100}
                unit="°C"
                label={t('dashboard.sensor.temperature')}
                color={cpuGaugeColor}
              />
            }
            metrics={[
              {
                label: t('dashboard.sensor.temperature'),
                value: metricText(cpu?.temperature, '°C', 0, notAvailable),
                color: temperatureColor(cpu?.temperature, valueColor)
              },
              {
                label: t('dashboard.sensor.usage'),
                value: metricText(cpu?.usage, '%', 0, notAvailable)
              },
              {
                label: t('dashboard.sensor.frequency'),
                value: formatFrequency(cpu?.coreClockAvg ?? cpu?.coreClockMax)
              }
            ]}
            trend={<TrendChart series={trendSeries} labels={trendLabels} height={TREND_HEIGHT} />}
          />
        </Col>
        <Col xs={24} xl={8}>
          <SensorCard
            title={t('dashboard.sensor.gpu')}
            model={gpuName}
            labelColor={labelColor}
            valueColor={valueColor}
            cardBackground={cardBackground}
            cardBorder={cardBorder}
            panelBackground={panelBackground}
            gauge={
              <SensorGauge
                value={gpu?.temperature}
                max={100}
                unit="°C"
                label={t('dashboard.sensor.temperature')}
                color={gpuGaugeColor}
              />
            }
            metrics={[
              {
                label: t('dashboard.sensor.temperature'),
                value: metricText(gpu?.temperature, '°C', 0, notAvailable),
                color: temperatureColor(gpu?.temperature, valueColor)
              },
              {
                label: t('dashboard.sensor.usage'),
                value: metricText(gpu?.usage, '%', 0, notAvailable)
              },
              {
                label: t('dashboard.sensor.vram'),
                value: vramText
              },
              {
                label: t('dashboard.sensor.fanSpeed'),
                value: metricText(gpu?.fanSpeed, 'RPM', 0, notAvailable)
              }
            ]}
            trend={<TrendChart series={trendSeries} labels={trendLabels} height={TREND_HEIGHT} />}
          />
        </Col>
        <Col xs={24} xl={8}>
          <SensorCard
            title={t('dashboard.sensor.memory')}
            model={null}
            labelColor={labelColor}
            valueColor={valueColor}
            cardBackground={cardBackground}
            cardBorder={cardBorder}
            panelBackground={panelBackground}
            gauge={
              <SensorGauge
                value={memory?.usage}
                max={100}
                unit="%"
                label={t('dashboard.sensor.usage')}
                color={MEMORY_COLOR}
              />
            }
            metrics={[
              {
                label: t('dashboard.sensor.usage'),
                value: metricText(memory?.usage, '%', 0, notAvailable)
              },
              {
                label: t('dashboard.memoryUsed'),
                value:
                  memory?.usedMb == null
                    ? notAvailable
                    : `${toGigabytes(memory.usedMb)?.toFixed(1)} GB`
              },
              {
                label: t('dashboard.memoryTotal'),
                value:
                  memory?.totalMb == null
                    ? notAvailable
                    : `${toGigabytes(memory.totalMb)?.toFixed(1)} GB`
              },
              ...(storageTempValue != null
                ? [
                    {
                      label: t('dashboard.storageTemp'),
                      value: storageTempValue,
                      color: temperatureColor(
                        Number.isFinite(storageTempMax) ? storageTempMax : null,
                        valueColor
                      )
                    }
                  ]
                : [])
            ]}
            trend={<TrendChart series={trendSeries} labels={trendLabels} height={TREND_HEIGHT} />}
          />
        </Col>
      </Row>
    </div>
  )
}
