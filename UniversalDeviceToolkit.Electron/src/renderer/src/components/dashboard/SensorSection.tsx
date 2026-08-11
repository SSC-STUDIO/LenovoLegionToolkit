import './sensor.css'
import { useEffect, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import type { SensorsBattery } from '../../api/sensors'
import { useSensorsStore } from '../../stores/sensorsStore'
import { useThemeStore } from '../../stores/themeStore'
import SensorGauge from './SensorGauge'
import TrendChart, { type TrendSeries } from './TrendChart'

const CPU_UTILIZATION = '#4f9df7'
const CPU_CLOCK = '#6fbf73'
const CPU_TEMPERATURE = '#d9883b'
const GPU_UTILIZATION = '#4f9df7'
const GPU_CLOCK = '#6fbf73'
const GPU_TEMPERATURE = '#d9883b'
const BATTERY_LEVEL = '#6fbf73'
const BATTERY_RATE = '#6fbf73'
const BATTERY_TEMPERATURE = '#d9883b'
const BATTERY_CAUTION = '#e0a92e'
const BATTERY_CRITICAL = '#e05656'
const BATTERY_LOW_THRESHOLD = 20
const CHART_HEIGHT = 116

interface SensorMetric {
  label: string
  value: string
  valueColor?: string
  barValue: number
  barMax: number
}

interface SensorPanelProps {
  title: string
  model?: string | null
  gauge: React.JSX.Element
  metrics: SensorMetric[]
  series: TrendSeries[]
  labels: string[]
  warnings?: React.JSX.Element
}

// Formatting mirrors SensorsControl.Formatting.cs / UpdateValue():
// frequency MHz→GHz 1 decimal, temperature 0 decimals, fan 1 decimal,
// health percent 2 decimals, rate signed W 2 decimals, invalid → "-".
function formatFrequency(mhz: number | null | undefined): string {
  if (mhz == null || !Number.isFinite(mhz) || mhz < 0) return '-'
  return `${(mhz / 1000).toFixed(1)} GHz`
}

function formatTemperature(c: number | null | undefined): string {
  if (c == null || !Number.isFinite(c) || c < 0) return '-'
  return `${c.toFixed(0)} °C`
}

function formatFan(speed: number | null | undefined): string {
  if (speed == null || !Number.isFinite(speed) || speed < 0) return '-'
  return `${speed.toFixed(1)} RPM`
}

function formatHealth(health: number | null | undefined): string {
  if (health == null || !Number.isFinite(health) || health < 0) return '-'
  return `${(health * 100).toFixed(2)}%`
}

function formatRate(mw: number | null | undefined): string {
  if (mw == null || !Number.isFinite(mw) || mw === -1) return '-'
  const w = mw / 1000
  const sign = w > 0 ? '+' : w < 0 ? '-' : ''
  return `${sign}${w.toFixed(2)} W`
}

// WPF UpdateValue: value < 0 → bar zeroed; max < 0 → max = max(value, 1).
function metricBar(
  value: number | null | undefined,
  max: number | null | undefined,
  fallbackMax: number
): { barValue: number; barMax: number } {
  if (value == null || !Number.isFinite(value) || value < 0) return { barValue: 0, barMax: 1 }
  if (max != null && Number.isFinite(max) && max > 0) return { barValue: value, barMax: max }
  return { barValue: value, barMax: Math.max(value, fallbackMax) }
}

function barPercent(metric: SensorMetric): number {
  if (metric.barMax <= 0 || metric.barValue == null || !Number.isFinite(metric.barValue)) return 0
  return Math.min(100, Math.max(0, (metric.barValue / metric.barMax) * 100))
}

// WPF temperature thresholds: < 50 °C → green, 50–79 °C → caution, ≥ 80 °C → critical.
function temperatureColor(temp: number | null | undefined): string | undefined {
  if (temp == null || !Number.isFinite(temp) || temp < 0) return undefined
  if (temp >= 80) return '#eb6b6b'
  if (temp >= 50) return '#e0a92e'
  return '#6fbf73'
}

// Battery ring: green >= 80 health, caution 60-79, critical < 60;
// low charge level overrides the ring to caution (WPF IsLowBattery → ChartCautionBrush).
function batteryGaugeColor(battery: SensorsBattery | undefined): string {
  const level = battery?.chargeLevel
  if (level != null && Number.isFinite(level) && level >= 0 && level <= BATTERY_LOW_THRESHOLD) {
    return BATTERY_CAUTION
  }
  const health = battery?.health
  if (health != null && Number.isFinite(health) && health >= 0) {
    const percent = health * 100
    if (percent < 60) return BATTERY_CRITICAL
    if (percent < 80) return BATTERY_CAUTION
  }
  return BATTERY_LEVEL
}

// WPF SymbolIcon "Battery024", FontSizeDisplaySection (25).
function BatteryIcon(): React.JSX.Element {
  return (
    <svg width="25" height="13" viewBox="0 0 25 13" fill="none" stroke="currentColor" strokeWidth="1.2" aria-hidden="true">
      <rect x="0.6" y="0.6" width="20.2" height="11.8" rx="2.6" />
      <path d="M23 4.2v4.6" strokeLinecap="round" />
      <rect x="3.2" y="3.2" width="9" height="6.6" rx="1.4" fill="currentColor" stroke="none" />
    </svg>
  )
}

function SensorPanel({ title, model, gauge, metrics, series, labels, warnings }: SensorPanelProps): React.JSX.Element {
  return (
    <section className="udt-sensor-panel">
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
              <dt>{metric.label}</dt>
              <div className="udt-sensor-panel__metric-track">
                <div className="udt-sensor-panel__metric-fill" style={{ width: `${barPercent(metric)}%` }} />
              </div>
              <dd style={metric.valueColor != null ? { color: metric.valueColor } : undefined} title={metric.value}>
                {metric.value}
              </dd>
            </div>
          ))}
        </dl>
      </div>
      {warnings}
      <div className="udt-sensor-panel__chart">
        <TrendChart series={series} labels={labels} height={CHART_HEIGHT} />
      </div>
      <div className="udt-sensor-panel__legend">
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
        { name: t('dashboard.sensor.usage'), color: CPU_UTILIZATION, data: [...trend.cpuUsage], max: 100 },
        { name: t('dashboard.sensor.frequency'), color: CPU_CLOCK, data: [...trend.cpuClock] },
        { name: t('dashboard.sensor.temperature'), color: CPU_TEMPERATURE, data: [...trend.cpuTemperature], max: 110 }
      ] satisfies TrendSeries[],
      battery: [
        { name: t('dashboard.sensor.rate'), color: BATTERY_RATE, data: [] },
        { name: t('dashboard.sensor.temperature'), color: BATTERY_TEMPERATURE, data: [], max: 60 }
      ] satisfies TrendSeries[],
      gpu: [
        { name: t('dashboard.sensor.usage'), color: GPU_UTILIZATION, data: [...trend.gpuUsage], max: 100 },
        { name: t('dashboard.sensor.frequency'), color: GPU_CLOCK, data: [...trend.gpuClock] },
        { name: t('dashboard.sensor.temperature'), color: GPU_TEMPERATURE, data: [...trend.gpuTemperature], max: 110 }
      ] satisfies TrendSeries[]
    }),
    [trend, t]
  )

  const cpu = snapshot?.cpu
  const gpu = snapshot?.gpu
  const battery = snapshot?.battery
  const labels = trend.labels
  const valueColor = isDark ? 'rgba(255, 255, 255, 0.77)' : 'rgba(0, 0, 0, 0.62)'

  const cpuClock = cpu?.coreClockAvg ?? cpu?.coreClockMax
  const cpuTemperature = temperatureColor(cpu?.temperature)
  const gpuTemperature = temperatureColor(gpu?.temperature)
  const batteryTemperature = temperatureColor(battery?.temperature)

  const healthPercent =
    battery?.health != null && Number.isFinite(battery.health) && battery.health >= 0 ? battery.health * 100 : null

  const rateValid =
    battery?.chargeRate != null && Number.isFinite(battery.chargeRate) && battery.chargeRate !== -1
  const rateBarValue = rateValid ? Math.min(Math.abs(battery.chargeRate!) / 1000, 100) : 0

  const batteryWarnings =
    battery != null ? (
      <div className="udt-sensor-panel__warnings">
        <div className="udt-sensor-panel__warning">{t('dashboard.sensor.lowPowerAdapter')}</div>
        {battery.chargeLevel != null && Number.isFinite(battery.chargeLevel) && battery.chargeLevel <= BATTERY_LOW_THRESHOLD && (
          <div className="udt-sensor-panel__warning">{t('dashboard.sensor.batteryLow')}</div>
        )}
        <div className="udt-sensor-panel__battery-icon">
          <BatteryIcon />
        </div>
      </div>
    ) : undefined

  return (
    <div className="udt-sensors">
      <div className="udt-sensor-board">
        <div className="udt-sensor-board__grid">
          <SensorPanel
            title={t('dashboard.sensor.cpu')}
            model={snapshot?.info?.cpuName}
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
              {
                label: t('dashboard.sensor.frequency'),
                value: formatFrequency(cpuClock),
                ...metricBar(cpuClock, cpu?.coreClockMax, 5000)
              },
              {
                label: t('dashboard.sensor.temperature'),
                value: formatTemperature(cpu?.temperature),
                valueColor: cpuTemperature ?? valueColor,
                ...metricBar(cpu?.temperature, null, 100)
              },
              {
                label: t('dashboard.sensor.fan'),
                value: formatFan(cpu?.fanSpeed),
                ...metricBar(cpu?.fanSpeed, null, 5000)
              }
            ]}
            series={trendSeries.cpu}
            labels={labels}
          />
          <SensorPanel
            title={t('dashboard.sensor.battery')}
            gauge={
              <SensorGauge
                value={battery?.chargeLevel}
                max={100}
                unit="%"
                label={t('dashboard.sensor.charge')}
                color={batteryGaugeColor(battery)}
              />
            }
            metrics={[
              {
                label: t('dashboard.sensor.health'),
                value: formatHealth(battery?.health),
                ...metricBar(healthPercent, 100, 100)
              },
              {
                label: t('dashboard.sensor.temperature'),
                value: formatTemperature(battery?.temperature),
                valueColor: batteryTemperature ?? valueColor,
                ...metricBar(battery?.temperature, 60, 60)
              },
              {
                label: t('dashboard.sensor.rate'),
                value: formatRate(battery?.chargeRate),
                barValue: rateBarValue,
                barMax: 100
              }
            ]}
            series={trendSeries.battery}
            labels={labels}
            warnings={batteryWarnings}
          />
          <SensorPanel
            title={t('dashboard.sensor.gpu')}
            model={snapshot?.info?.gpuName}
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
              {
                label: t('dashboard.sensor.frequency'),
                value: formatFrequency(gpu?.coreClock),
                ...metricBar(gpu?.coreClock, null, 2500)
              },
              {
                label: t('dashboard.sensor.temperature'),
                value: formatTemperature(gpu?.temperature),
                valueColor: gpuTemperature ?? valueColor,
                ...metricBar(gpu?.temperature, null, 100)
              },
              {
                label: t('dashboard.sensor.fan'),
                value: formatFan(gpu?.fanSpeed),
                ...metricBar(gpu?.fanSpeed, null, 5000)
              }
            ]}
            series={trendSeries.gpu}
            labels={labels}
          />
        </div>
      </div>
    </div>
  )
}
