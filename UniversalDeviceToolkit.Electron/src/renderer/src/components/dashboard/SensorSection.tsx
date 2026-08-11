import './sensor.css'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import type { SensorsBattery, SensorsCpu } from '../../api/sensors'
import { useSensorsStore } from '../../stores/sensorsStore'
import { useSettingsStore } from '../../stores/settingsStore'
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
const REFRESH_INTERVALS = [1, 2, 3, 5]

type TemperatureUnit = 'C' | 'F'

interface SensorMetric {
  label: string
  value: string
  valueColor?: string
  barValue: number
  barMax: number
  icon?: React.JSX.Element
}

interface SensorDetail {
  label: string
  value: string
}

interface SensorPanelProps {
  title: string
  model?: string | null
  gauge: React.JSX.Element
  metrics: SensorMetric[]
  series: TrendSeries[]
  labels: string[]
  warnings?: React.JSX.Element
  /** Detail rows grouped per column (WPF detail panel: two equal columns). */
  details?: SensorDetail[][]
  /** Optional block above the detail columns (battery mini gauges). */
  detailsHeader?: React.JSX.Element
}

// Formatting mirrors SensorsControl.Formatting.cs / UpdateValue():
// frequency MHz→GHz 1 decimal, temperature 0 decimals, fan 1 decimal,
// health percent 2 decimals, rate signed W 2 decimals, invalid → "-".
function formatFrequency(mhz: number | null | undefined): string {
  if (mhz == null || !Number.isFinite(mhz) || mhz < 0) return '-'
  return `${(mhz / 1000).toFixed(1)} GHz`
}

// FormatTemperature: °F conversion when the appearance setting is F.
function formatTemperature(c: number | null | undefined, unit: TemperatureUnit = 'C'): string {
  if (c == null || !Number.isFinite(c) || c < 0) return '-'
  if (unit === 'F') {
    return `${(c * (9 / 5) + 32).toFixed(0)} °F`
  }
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

// FormatMemoryClock: 0.0 MHz (WPF _gpuMemoryClockText).
function formatMemoryClock(mhz: number | null | undefined): string {
  if (mhz == null || !Number.isFinite(mhz) || mhz < 0) return '-'
  return `${mhz.toFixed(1)} MHz`
}

// FormatWattHours: mWh → Wh, 2 decimals (WPF "{0:0.00} Wh").
function formatWattHours(mwh: number | null | undefined): string {
  if (mwh == null || !Number.isFinite(mwh) || mwh <= 0) return '-'
  return `${(mwh / 1000).toFixed(2)} Wh`
}

// WPF UpdateBatteryHealthGauge ring color: green >= 80, caution 60–79, critical < 60.
function batteryHealthColor(healthPercent: number | null | undefined): string {
  if (healthPercent == null || !Number.isFinite(healthPercent) || healthPercent < 0) return BATTERY_LEVEL
  if (healthPercent >= 80) return BATTERY_LEVEL
  if (healthPercent >= 60) return BATTERY_CAUTION
  return BATTERY_CRITICAL
}

// FormatPower: wattage 0.# decimals.
function formatPower(w: number | null | undefined): string {
  if (w == null || !Number.isFinite(w) || w < 0) return '-'
  return `${w.toFixed(1)} W`
}

// FormatVoltage: 0.000 V.
function formatVoltage(v: number | null | undefined): string {
  if (v == null || !Number.isFinite(v) || v <= 0) return '-'
  return `${v.toFixed(3)} V`
}

// FormatUsageInGigabytes: "x.x / y.y GB (z%)", "x.x GB (z%)" or "x.x GB".
function formatUsageInGigabytes(
  usedMb: number | null | undefined,
  totalMb: number | null | undefined,
  percentage: number | null | undefined = -1
): string {
  if (usedMb == null || !Number.isFinite(usedMb) || usedMb < 0) {
    return percentage != null && Number.isFinite(percentage) && percentage >= 0
      ? `${percentage.toFixed(0)}%`
      : '-'
  }
  const usedGb = usedMb / 1024
  const totalGb = totalMb != null && Number.isFinite(totalMb) && totalMb > 0 ? totalMb / 1024 : 0
  const percent =
    percentage != null && Number.isFinite(percentage) && percentage >= 0
      ? percentage
      : totalGb > 0
        ? (usedGb / totalGb) * 100
        : -1
  const base = totalGb > 0 ? `${usedGb.toFixed(1)} / ${totalGb.toFixed(1)} GB` : `${usedGb.toFixed(1)} GB`
  return percent >= 0 ? `${base} (${percent.toFixed(0)}%)` : base
}

// FormatThroughput: B/s → KB/s → MB/s → GB/s (0.00 decimals).
function formatThroughput(bytesPerSecond: number | null | undefined): string {
  if (bytesPerSecond == null || !Number.isFinite(bytesPerSecond) || bytesPerSecond < 0) return '-'
  const kb = 1024
  const mb = kb * 1024
  const gb = mb * 1024
  if (bytesPerSecond >= gb) return `${(bytesPerSecond / gb).toFixed(2)} GB/s`
  if (bytesPerSecond >= mb) return `${(bytesPerSecond / mb).toFixed(2)} MB/s`
  if (bytesPerSecond >= kb) return `${(bytesPerSecond / kb).toFixed(2)} KB/s`
  return `${bytesPerSecond.toFixed(0)} B/s`
}

// FormatThroughputPair: "Rx x\nTx y".
function formatThroughputPair(rx: number | null | undefined, tx: number | null | undefined): string {
  const rxText = formatThroughput(rx)
  const txText = formatThroughput(tx)
  if (rxText === '-' && txText === '-') return '-'
  if (rxText === '-') return `Tx ${txText}`
  if (txText === '-') return `Rx ${rxText}`
  return `Rx ${rxText} / Tx ${txText}`
}

// FormatCpuPowerBreakdown: "12 W | Cores 8.5 W | Memory 3.2 W | Platform 1.1 W".
function formatCpuPowerBreakdown(cpu: SensorsCpu, labels: { cores: string; memory: string; platform: string }): string {
  const parts: string[] = []
  const total = cpu.power
  if (total != null && Number.isFinite(total) && total >= 0) parts.push(formatPower(total))
  if (cpu.powerCores != null && Number.isFinite(cpu.powerCores) && cpu.powerCores > 0) {
    parts.push(`${labels.cores} ${cpu.powerCores.toFixed(1)} W`)
  }
  if (cpu.powerMemory != null && Number.isFinite(cpu.powerMemory) && cpu.powerMemory > 0) {
    parts.push(`${labels.memory} ${cpu.powerMemory.toFixed(1)} W`)
  }
  if (cpu.powerPlatform != null && Number.isFinite(cpu.powerPlatform) && cpu.powerPlatform > 0) {
    parts.push(`${labels.platform} ${cpu.powerPlatform.toFixed(1)} W`)
  }
  return parts.length > 0 ? parts.join(' | ') : '-'
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
// Thresholds are always evaluated on the °C value, before unit conversion.
function temperatureColor(temp: number | null | undefined): string | undefined {
  if (temp == null || !Number.isFinite(temp) || temp < 0) return undefined
  if (temp >= 80) return '#eb6b6b'
  if (temp >= 50) return '#e0a92e'
  return '#6fbf73'
}

// Battery ring: green >= 80 health, caution 60-79, critical < 60;
// low charge level overrides the ring to caution (WPF IsLowBattery → ChartCautionBrush).
function batteryGaugeColor(battery: SensorsBattery | undefined): string {
  if (battery?.isLowBattery === true) {
    return BATTERY_CAUTION
  }
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
function BatteryIcon({ level, charging }: { level?: number | null; charging?: boolean }): React.JSX.Element {
  const clamped =
    level != null && Number.isFinite(level) ? Math.min(100, Math.max(0, level)) : 40
  const fillWidth = charging ? 14 : Math.max(2, (clamped / 100) * 14)
  return (
    <svg width="25" height="13" viewBox="0 0 25 13" fill="none" stroke="currentColor" strokeWidth="1.2" aria-hidden="true">
      <rect x="0.6" y="0.6" width="20.2" height="11.8" rx="2.6" />
      <path d="M23 4.2v4.6" strokeLinecap="round" />
      <rect x="3.2" y="3.2" width={fillWidth} height="6.6" rx="1.4" fill="currentColor" stroke="none" />
    </svg>
  )
}

// Compact metric-row glyph (temperature) — matches the visual weight of WPF stat rows.
function TemperatureIcon({ color }: { color?: string }): React.JSX.Element {
  return (
    <svg
      className="udt-sensor-panel__metric-icon"
      width="14"
      height="14"
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      style={color != null ? { color } : undefined}
    >
      <path
        d="M7.2 1.6h1.6a1.2 1.2 0 0 1 1.2 1.2v6.1a2.8 2.8 0 1 1-4 0V2.8A1.2 1.2 0 0 1 7.2 1.6Z"
        stroke="currentColor"
        strokeWidth="1.2"
      />
      <circle cx="8" cy="11.2" r="1.5" fill="currentColor" />
      <path d="M8 4v5.2" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" />
    </svg>
  )
}

function SensorPanel({ title, model, gauge, metrics, series, labels, warnings, details, detailsHeader }: SensorPanelProps): React.JSX.Element {
  const { t } = useTranslation()
  const [detailsExpanded, setDetailsExpanded] = useState(false)
  const hasDetails = details != null && details.length > 0
  return (
    // WPF CardControl_PreviewMouseLeftButtonDown: double-click (500ms threshold)
    // toggles the detail panel; React's native onDoubleClick fires within that window.
    <section
      className="udt-sensor-panel"
      onDoubleClick={() => {
        if (hasDetails) setDetailsExpanded((value) => !value)
      }}
    >
      <div className="udt-sensor-panel__heading">
        <h2>{title}</h2>
        {model != null && model !== '' && (
          <span className="udt-sensor-panel__model" title={model}>
            {model}
          </span>
        )}
        {hasDetails && (
          <button
            type="button"
            className={`udt-sensor-panel__details-toggle${detailsExpanded ? ' udt-sensor-panel__details-toggle--expanded' : ''}`}
            onClick={() => setDetailsExpanded((value) => !value)}
            aria-expanded={detailsExpanded}
          >
            {t('dashboard.sensor.details')}
          </button>
        )}
      </div>
      <div className="udt-sensor-panel__summary">
        {gauge}
        <dl className="udt-sensor-panel__metrics">
          {metrics.map((metric) => (
            <div key={metric.label} className="udt-sensor-panel__metric">
              <dt>
                {metric.icon}
                {metric.label}
              </dt>
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
      {detailsExpanded && hasDetails && (
        <div className="udt-sensor-panel__details">
          {detailsHeader}
          <div className="udt-sensor-panel__details-cols">
            {details.map((column, columnIndex) => (
              <div key={columnIndex} className="udt-sensor-panel__details-col">
                {column
                  .filter((row) => row.value !== '-')
                  .map((row) => (
                    <div key={row.label} className="udt-sensor-panel__detail">
                      <dt>{row.label}</dt>
                      <dd title={row.value}>{row.value}</dd>
                    </div>
                  ))}
              </div>
            ))}
          </div>
        </div>
      )}
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
  const scopes = useSettingsStore((state) => state.scopes)

  const temperatureUnit: TemperatureUnit = (() => {
    const appearance =
      typeof scopes.appearance === 'object' && scopes.appearance !== null
        ? (scopes.appearance as Record<string, unknown>)
        : {}
    return appearance['TemperatureUnit'] === 'F' ? 'F' : 'C'
  })()

  useEffect(() => {
    const store = useSensorsStore.getState()
    void store.loadStatus()
    void store.loadSnapshot()
    void store.start(1)
    void useSettingsStore.getState().load()
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
        { name: t('dashboard.sensor.rate'), color: BATTERY_RATE, data: [...trend.batteryRate] },
        { name: t('dashboard.sensor.temperature'), color: BATTERY_TEMPERATURE, data: [...trend.batteryTemperature], max: 60 }
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
  const memory = snapshot?.memory
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

  const isLowBattery =
    battery?.isLowBattery === true ||
    (battery?.chargeLevel != null &&
      Number.isFinite(battery.chargeLevel) &&
      battery.chargeLevel <= BATTERY_LOW_THRESHOLD)
  const isLowPowerAdapter = battery?.isLowPowerAdapter === true

  const batteryWarnings =
    battery != null && (isLowPowerAdapter || isLowBattery) ? (
      <div className="udt-sensor-panel__warnings">
        {isLowPowerAdapter && (
          <div className="udt-sensor-panel__warning">{t('dashboard.sensor.lowPowerAdapter')}</div>
        )}
        {isLowBattery && <div className="udt-sensor-panel__warning">{t('dashboard.sensor.batteryLow')}</div>}
        <div className="udt-sensor-panel__battery-icon">
          <BatteryIcon level={battery.chargeLevel} charging={battery.isCharging === true} />
        </div>
      </div>
    ) : undefined

  // Advanced details (SensorsControl detail window parity). Two columns; rows
  // whose value is "-" are collapsed (WPF UpdateDetailContainerVisibility).
  // Temp/voltage ranges fall back to the live value (WPF FormatFallbackRangeText).
  const cpuDetails: SensorDetail[][] = [
    [
      {
        label: t('dashboard.sensor.detail.power'),
        value: formatCpuPowerBreakdown(cpu ?? {}, {
          cores: t('dashboard.sensor.detail.powerCores'),
          memory: t('dashboard.sensor.detail.powerMemory'),
          platform: t('dashboard.sensor.detail.powerPlatform')
        })
      },
      { label: t('dashboard.sensor.voltage'), value: formatVoltage(cpu?.voltage) },
      { label: t('dashboard.sensor.detail.pCoreClock'), value: formatFrequency(cpu?.pCoreClock) },
      { label: t('dashboard.sensor.detail.eCoreClock'), value: formatFrequency(cpu?.eCoreClock) },
      {
        label: t('dashboard.sensor.detail.memoryUsage'),
        value: formatUsageInGigabytes(memory?.usedMb, memory?.totalMb, memory?.usage)
      }
    ],
    [
      { label: t('dashboard.sensor.temperature'), value: formatTemperature(cpu?.temperature, temperatureUnit) },
      { label: t('dashboard.sensor.voltageRange'), value: formatVoltage(cpu?.voltage) },
      {
        label: t('dashboard.sensor.memoryTemperature'),
        value: formatTemperature(memory?.highestTemperature, temperatureUnit)
      },
      {
        label: t('dashboard.sensor.ssdTemperature'),
        value: (() => {
          const temps = snapshot?.storage?.temperatures
          const valid = (temps ?? []).filter((value) => value != null && Number.isFinite(value) && value >= 0)
          return valid.length > 0
            ? valid.map((value) => formatTemperature(value, temperatureUnit)).join(' / ')
            : '-'
        })()
      }
    ]
  ]

  const gpuDetails: SensorDetail[][] = [
    [
      { label: t('sensorsControlmemoryClocktitle'), value: formatMemoryClock(gpu?.memoryClock) },
      {
        label: snapshot?.info?.gpuIsIntegrated
          ? t('dashboard.sensor.detail.sharedMemoryUsage')
          : t('dashboard.sensor.detail.vramUsage'),
        value: formatUsageInGigabytes(gpu?.vramUsedMb, gpu?.vramTotalMb, gpu?.vramUtilization)
      },
      { label: t('dashboard.sensor.detail.power'), value: formatPower(gpu?.power) },
      { label: t('dashboard.sensor.voltage'), value: formatVoltage(gpu?.voltage) },
      {
        label: t('dashboard.sensor.detail.pcieThroughput'),
        value: formatThroughputPair(gpu?.pcieRxThroughput, gpu?.pcieTxThroughput)
      }
    ],
    [
      { label: t('dashboard.sensor.vramTemperature'), value: formatTemperature(gpu?.vramTemperature, temperatureUnit) },
      { label: t('dashboard.sensor.detail.hotSpot'), value: formatTemperature(gpu?.hotSpotTemperature, temperatureUnit) },
      { label: t('dashboard.sensor.temperature'), value: formatTemperature(gpu?.temperature, temperatureUnit) },
      { label: t('dashboard.sensor.voltageRange'), value: formatVoltage(gpu?.voltage) }
    ]
  ]

  const designCapacity = battery?.designCapacity
  const fullChargeCapacity = battery?.fullChargeCapacity
  const designCapacityValid = designCapacity != null && Number.isFinite(designCapacity) && designCapacity > 0
  const fullChargeCapacityValid =
    fullChargeCapacity != null && Number.isFinite(fullChargeCapacity) && fullChargeCapacity > 0
  const fullChargePercent =
    designCapacityValid && fullChargeCapacityValid
      ? Math.min(100, Math.max(0, (fullChargeCapacity / designCapacity) * 100))
      : null
  const remainingWh =
    battery?.chargeLevel != null && Number.isFinite(battery.chargeLevel) && fullChargeCapacityValid
      ? (battery.chargeLevel / 100) * (fullChargeCapacity / 1000)
      : null

  // Battery detail header: 3 mini gauges (WPF GaugeSizeSM rings, 4px thick).
  const batteryGauges = (
    <div className="udt-sensor-panel__battery-gauges">
      <div className="udt-sensor-panel__battery-gauge">
        <SensorGauge
          value={battery?.chargeLevel}
          max={100}
          size={88}
          thickness={4}
          label={t('dashboard.sensor.capacity')}
          color={BATTERY_LEVEL}
        />
        <span className="udt-sensor-panel__battery-gauge-value">
          {remainingWh != null ? `${remainingWh.toFixed(2)} Wh` : '-'}
        </span>
      </div>
      <div className="udt-sensor-panel__battery-gauge">
        <SensorGauge
          value={fullChargePercent}
          max={100}
          size={88}
          thickness={4}
          label={t('dashboard.sensor.fullCapacity')}
          color={CPU_UTILIZATION}
        />
        <span className="udt-sensor-panel__battery-gauge-value">
          {fullChargeCapacityValid ? formatWattHours(fullChargeCapacity) : '-'}
        </span>
      </div>
      <div className="udt-sensor-panel__battery-gauge">
        <SensorGauge
          value={healthPercent}
          max={100}
          size={88}
          thickness={4}
          label={t('dashboard.sensor.health')}
          color={batteryHealthColor(healthPercent)}
        />
        <span className="udt-sensor-panel__battery-gauge-value">
          {designCapacityValid
            ? `${t('dashboard.sensor.designCapacity')}: ${formatWattHours(designCapacity)}`
            : '-'}
        </span>
      </div>
    </div>
  )

  const batteryDetails: SensorDetail[][] = [
    [
      { label: t('dashboard.sensor.powerRange'), value: '-' },
      { label: t('dashboard.sensor.cycles'), value: '-' }
    ],
    [
      { label: t('dashboard.sensor.date'), value: '-' },
      {
        label: t('dashboard.sensor.temperature'),
        value: formatTemperature(battery?.temperature, temperatureUnit)
      }
    ]
  ]

  const intervalSec = useSensorsStore((state) => state.intervalSec)
  const [refreshMenu, setRefreshMenu] = useState<{ x: number; y: number } | null>(null)
  const boardRef = useRef<HTMLDivElement>(null)
  const refreshMenuRef = useRef<HTMLDivElement>(null)

  // Close the refresh context menu on outside click / Escape.
  useEffect(() => {
    if (refreshMenu == null) return
    const close = (event: MouseEvent): void => {
      if (event.target instanceof Node && refreshMenuRef.current?.contains(event.target)) return
      setRefreshMenu(null)
    }
    const onKeyDown = (event: KeyboardEvent): void => {
      if (event.key === 'Escape') setRefreshMenu(null)
    }
    document.addEventListener('mousedown', close)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('mousedown', close)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [refreshMenu])

  // WPF SensorsControl refresh context menu: right-click on the sensors card.
  const openRefreshMenu = (event: React.MouseEvent<HTMLDivElement>): void => {
    event.preventDefault()
    const rect = boardRef.current?.getBoundingClientRect()
    if (rect == null) return
    setRefreshMenu({
      x: Math.max(0, Math.min(event.clientX - rect.left, rect.width - 148)),
      y: Math.max(0, Math.min(event.clientY - rect.top, rect.height - 168))
    })
  }

  const handleIntervalChange = (seconds: number): void => {
    setRefreshMenu(null)
    // start() re-subscribes with the new interval when already polling.
    void useSensorsStore.getState().start(seconds)
  }

  return (
    <div className="udt-sensors">
      <div className="udt-sensor-toolbar">
        <label className="udt-sensor-toolbar__label" htmlFor="udt-sensor-refresh-interval">
          {t('dashboard.sensor.refreshInterval')}
        </label>
        <select
          id="udt-sensor-refresh-interval"
          className="udt-select"
          value={intervalSec}
          onChange={(e) => handleIntervalChange(Number(e.target.value))}
        >
          {REFRESH_INTERVALS.map((seconds) => (
            <option key={seconds} value={seconds}>
              {seconds} s
            </option>
          ))}
        </select>
      </div>
      <div ref={boardRef} className="udt-sensor-board" onContextMenu={openRefreshMenu}>
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
                value: formatTemperature(cpu?.temperature, temperatureUnit),
                valueColor: cpuTemperature ?? valueColor,
                icon: <TemperatureIcon color={cpuTemperature ?? valueColor} />,
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
            details={cpuDetails}
          />
          <SensorPanel
            title={t('dashboard.sensor.battery')}
            model={battery?.modelName}
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
                value: formatTemperature(battery?.temperature, temperatureUnit),
                valueColor: batteryTemperature ?? valueColor,
                icon: <TemperatureIcon color={batteryTemperature ?? valueColor} />,
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
            details={batteryDetails}
            detailsHeader={batteryGauges}
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
                value: formatTemperature(gpu?.temperature, temperatureUnit),
                valueColor: gpuTemperature ?? valueColor,
                icon: <TemperatureIcon color={gpuTemperature ?? valueColor} />,
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
            details={gpuDetails}
          />
        </div>
        {refreshMenu != null && (
          <div
            ref={refreshMenuRef}
            className="udt-sensor-context-menu"
            style={{ left: refreshMenu.x, top: refreshMenu.y }}
            role="menu"
            aria-label={t('dashboard.sensor.refreshInterval')}
          >
            {REFRESH_INTERVALS.map((seconds) => (
              <button
                key={seconds}
                type="button"
                role="menuitem"
                className={`udt-sensor-context-menu__item${intervalSec === seconds ? ' udt-sensor-context-menu__item--checked' : ''}`}
                onClick={() => handleIntervalChange(seconds)}
              >
                <span className="udt-sensor-context-menu__check" aria-hidden="true">
                  {intervalSec === seconds ? '✓' : ''}
                </span>
                {seconds} s
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
