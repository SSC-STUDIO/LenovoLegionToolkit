import { useEffect, useMemo } from 'react'
import { Tag, theme } from 'antd'
import { useTranslation } from 'react-i18next'
import { useSensorsStore } from '../../stores/sensorsStore'
import { useThemeStore } from '../../stores/themeStore'
import SensorGauge from './SensorGauge'
import { formatSensorValue } from '../../utils/format'
import TrendChart, { type TrendSeries } from './TrendChart'

const TEMP_WARNING = 60
const TEMP_CRITICAL = 75
const COLOR_WARNING = '#F8A636'
const COLOR_CRITICAL = '#F16D75'
const CPU_UTILIZATION = '#5B9EF5'
const CPU_CLOCK = '#76C985'
const CPU_TEMPERATURE = '#E4933D'
const GPU_UTILIZATION = '#5B9EF5'
const GPU_CLOCK = '#76C985'
const GPU_TEMPERATURE = '#E4933D'
const MEMORY_UTILIZATION = '#8ACA86'
const TREND_HEIGHT = 168

interface SensorMetric {
  label: string
  value: string
  color?: string
}

interface SensorPanelProps {
  title: string
  model?: string | null
  gauge: React.JSX.Element
  metrics: SensorMetric[]
  series: TrendSeries[]
  labels: string[]
  labelColor: string
  valueColor: string
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

function SensorPanel({
  title,
  model,
  gauge,
  metrics,
  series,
  labels,
  labelColor,
  valueColor
}: SensorPanelProps): React.JSX.Element {
  return (
    <section className="udt-sensor-panel" aria-label={title}>
      <div className="udt-sensor-panel__heading">
        <h2>{title}</h2>
        {model != null && model !== '' && (
          <span className="udt-sensor-panel__model" title={model}>
            {model}
          </span>
        )}
      </div>
      <div className="udt-sensor-panel__summary">
        {gauge}
        <dl className="udt-sensor-panel__metrics">
          {metrics.map((metric) => (
            <div key={metric.label} className="udt-sensor-panel__metric">
              <dt style={{ color: labelColor }}>{metric.label}</dt>
              <dd style={{ color: metric.color ?? valueColor }} title={metric.value}>
                {metric.value}
              </dd>
            </div>
          ))}
        </dl>
      </div>
      <div className="udt-sensor-panel__chart">
        <TrendChart series={series} labels={labels} height={TREND_HEIGHT} />
      </div>
      <div className="udt-sensor-panel__legend" aria-label={`${title} chart legend`}>
        {series.map((item) => (
          <span key={item.name}>
            <i style={{ backgroundColor: item.color }} />
            {item.name}
          </span>
        ))}
      </div>
    </section>
  )
}

export default function SensorSection(): React.JSX.Element {
  const { t } = useTranslation()
  const { token } = theme.useToken()
  const isDark = useThemeStore((state) => state.themeMode === 'dark')
  const snapshot = useSensorsStore((state) => state.snapshot)
  const trend = useSensorsStore((state) => state.trend)

  useEffect(() => {
    const store = useSensorsStore.getState()
    void store.loadStatus()
    void store.loadSnapshot()
    void store.start(1)
    return () => {
      void useSensorsStore.getState().stop()
    }
  }, [])

  const trendSeries = useMemo(
    () => ({
      cpu: [
        { name: t('dashboard.sensor.usage'), color: CPU_UTILIZATION, data: [...trend.cpuUsage] },
        { name: t('dashboard.sensor.frequency'), color: CPU_CLOCK, data: [...trend.cpuClock] },
        { name: t('dashboard.sensor.temperature'), color: CPU_TEMPERATURE, data: [...trend.cpuTemperature] }
      ] satisfies TrendSeries[],
      gpu: [
        { name: t('dashboard.sensor.usage'), color: GPU_UTILIZATION, data: [...trend.gpuUsage] },
        { name: t('dashboard.sensor.frequency'), color: GPU_CLOCK, data: [...trend.gpuClock] },
        { name: t('dashboard.sensor.temperature'), color: GPU_TEMPERATURE, data: [...trend.gpuTemperature] }
      ] satisfies TrendSeries[],
      memory: [
        { name: t('dashboard.sensor.usage'), color: MEMORY_UTILIZATION, data: [...trend.memoryUsage] }
      ] satisfies TrendSeries[]
    }),
    [trend, t]
  )

  const cpu = snapshot?.cpu
  const gpu = snapshot?.gpu
  const memory = snapshot?.memory
  const storageTemperatures = snapshot?.storage?.temperatures
  const notAvailable = t('dashboard.notAvailable')
  const labels = trend.labels
  const labelColor = isDark ? 'rgba(255, 255, 255, 0.78)' : token.colorTextSecondary
  const valueColor = isDark ? 'rgba(255, 255, 255, 0.94)' : token.colorText
  const vramUsed = toGigabytes(gpu?.vramUsedMb)
  const vramTotal = toGigabytes(gpu?.vramTotalMb)
  const vramText =
    vramUsed == null
      ? notAvailable
      : `${vramUsed.toFixed(1)} GB${vramTotal != null ? ` / ${vramTotal.toFixed(1)} GB` : ''}`
  const storageTemperatureText =
    storageTemperatures != null && storageTemperatures.length > 0
      ? `${storageTemperatures.map((value) => formatSensorValue(value, 0)).join(' / ')} °C`
      : null
  const storageTemperatureMax = Math.max(
    ...(storageTemperatures ?? []).filter((value): value is number => value != null && Number.isFinite(value)),
    Number.NEGATIVE_INFINITY
  )

  return (
    <div className="udt-sensors">
      {snapshot?.source === 'vendor' && (
        <Tag color="orange" className="udt-sensors__source-tag">
          vendor
        </Tag>
      )}
      <div className="udt-sensor-board">
        <div className="udt-sensor-board__grid">
          <SensorPanel
            title={t('dashboard.sensor.cpu')}
            model={snapshot?.info?.cpuName}
            labelColor={labelColor}
            valueColor={valueColor}
            gauge={
              <SensorGauge
                value={cpu?.usage}
                max={100}
                unit="%"
                label={t('dashboard.sensor.usage')}
                color={CPU_UTILIZATION}
              />
            }
            metrics={[
              { label: t('dashboard.sensor.usage'), value: metricText(cpu?.usage, '%', 0, notAvailable) },
              { label: t('dashboard.sensor.frequency'), value: formatFrequency(cpu?.coreClockAvg ?? cpu?.coreClockMax) },
              {
                label: t('dashboard.sensor.temperature'),
                value: metricText(cpu?.temperature, '°C', 0, notAvailable),
                color: temperatureColor(cpu?.temperature, valueColor)
              }
            ]}
            series={trendSeries.cpu}
            labels={labels}
          />
          <SensorPanel
            title={t('dashboard.sensor.memory')}
            labelColor={labelColor}
            valueColor={valueColor}
            gauge={
              <SensorGauge
                value={memory?.usage}
                max={100}
                unit="%"
                label={t('dashboard.sensor.usage')}
                color={MEMORY_UTILIZATION}
              />
            }
            metrics={[
              { label: t('dashboard.sensor.usage'), value: metricText(memory?.usage, '%', 0, notAvailable) },
              {
                label: t('dashboard.memoryUsed'),
                value: memory?.usedMb == null ? notAvailable : `${toGigabytes(memory.usedMb)?.toFixed(1)} GB`
              },
              {
                label: t('dashboard.memoryTotal'),
                value: memory?.totalMb == null ? notAvailable : `${toGigabytes(memory.totalMb)?.toFixed(1)} GB`
              },
              ...(storageTemperatureText == null
                ? []
                : [
                    {
                      label: t('dashboard.storageTemp'),
                      value: storageTemperatureText,
                      color: temperatureColor(
                        Number.isFinite(storageTemperatureMax) ? storageTemperatureMax : null,
                        valueColor
                      )
                    }
                  ])
            ]}
            series={trendSeries.memory}
            labels={labels}
          />
          <SensorPanel
            title={t('dashboard.sensor.gpu')}
            model={snapshot?.info?.gpuName}
            labelColor={labelColor}
            valueColor={valueColor}
            gauge={
              <SensorGauge
                value={gpu?.usage}
                max={100}
                unit="%"
                label={t('dashboard.sensor.usage')}
                color={GPU_UTILIZATION}
              />
            }
            metrics={[
              { label: t('dashboard.sensor.usage'), value: metricText(gpu?.usage, '%', 0, notAvailable) },
              { label: t('dashboard.sensor.frequency'), value: formatFrequency(gpu?.coreClock) },
              {
                label: t('dashboard.sensor.temperature'),
                value: metricText(gpu?.temperature, '°C', 0, notAvailable),
                color: temperatureColor(gpu?.temperature, valueColor)
              },
              { label: t('dashboard.sensor.vram'), value: vramText }
            ]}
            series={trendSeries.gpu}
            labels={labels}
          />
        </div>
      </div>
    </div>
  )
}
