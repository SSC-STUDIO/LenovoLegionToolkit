import { useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { formatDateForUi } from '../../utils/dateFormat'
import type { SensorsBattery, SensorsCpu, SensorsGpu, SensorsMemory } from '../../api/sensors'
import { formatUsageInGigabytes } from '../../utils/format'

export type TemperatureUnit = 'C' | 'F'

export interface SensorDetailRow {
  label: string
  value: string
}

// Formatting mirrors SensorsControl.Formatting.cs (Electron detail window parity):
// voltage 0.000 V, power 0.# W, frequency MHz→GHz 1 decimal, temperature 0
// decimals with °F conversion, GB 1 decimal, throughput 0.00 scaled.

export function formatVoltage(v: number | null | undefined): string {
  if (v == null || !Number.isFinite(v) || v <= 0) return '-'
  return `${v.toFixed(3)} V`
}

export function formatPower(w: number | null | undefined): string {
  if (w == null || !Number.isFinite(w) || w < 0) return '-'
  const rounded = Math.round(w * 10) / 10
  return `${Number.isInteger(rounded) ? rounded.toFixed(0) : rounded.toFixed(1)} W`
}

// FormatPowerKeepingPrevious: unknown value (-1 / null) keeps the last known text.
export function formatPowerKeepingPrevious(w: number | null | undefined, previousText: string): string {
  if (w != null && Number.isFinite(w) && w >= 0) return formatPower(w)
  return previousText !== '' && previousText !== '-' ? previousText : '-'
}

export function formatFrequency(mhz: number | null | undefined): string {
  if (mhz == null || !Number.isFinite(mhz) || mhz < 0) return '-'
  return `${(mhz / 1000).toFixed(1)} GHz`
}

export function formatTemperature(c: number | null | undefined, unit: TemperatureUnit = 'C'): string {
  if (c == null || !Number.isFinite(c) || c < 0) return '-'
  return unit === 'F' ? `${(c * (9 / 5) + 32).toFixed(0)} °F` : `${c.toFixed(0)} °C`
}

// FormatTemperaturePair: "a / b" with either side optional.
export function formatTemperaturePair(
  first: number | null | undefined,
  second: number | null | undefined,
  unit: TemperatureUnit = 'C'
): string {
  const a = first != null && Number.isFinite(first) && first >= 0 ? formatTemperature(first, unit) : null
  const b = second != null && Number.isFinite(second) && second >= 0 ? formatTemperature(second, unit) : null
  if (a != null && b != null) return `${a} / ${b}`
  if (a != null) return a
  if (b != null) return b
  return '-'
}

// FormatThroughput: B/s → KB/s → MB/s → GB/s (0.00 decimals).
export function formatThroughput(bytesPerSecond: number | null | undefined): string {
  if (bytesPerSecond == null || !Number.isFinite(bytesPerSecond) || bytesPerSecond < 0) return '-'
  const kb = 1024
  const mb = kb * 1024
  const gb = mb * 1024
  if (bytesPerSecond >= gb) return `${(bytesPerSecond / gb).toFixed(2)} GB/s`
  if (bytesPerSecond >= mb) return `${(bytesPerSecond / mb).toFixed(2)} MB/s`
  if (bytesPerSecond >= kb) return `${(bytesPerSecond / kb).toFixed(2)} KB/s`
  return `${bytesPerSecond.toFixed(0)} B/s`
}

// FormatThroughputPair: "Rx a\nTx b" (multi-line via pre-line whitespace).
export function formatThroughputPair(rx: number | null | undefined, tx: number | null | undefined): string {
  const rxText = formatThroughput(rx)
  const txText = formatThroughput(tx)
  if (rxText === '-' && txText === '-') return '-'
  if (rxText === '-') return `Tx ${txText}`
  if (txText === '-') return `Rx ${rxText}`
  return `Rx ${rxText}\nTx ${txText}`
}

// FormatCpuPowerBreakdown: "12 W | Cores 8.5 W | Memory 3.2 W | Platform 1.1 W".
export function formatCpuPowerBreakdown(
  cpu: SensorsCpu,
  labels: { cores: string; memory: string; platform: string }
): string {
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

// Battery capacities are delivered in mWh → Wh, 2 decimals.
export function formatWattHours(mwh: number | null | undefined): string {
  if (mwh == null || !Number.isFinite(mwh) || mwh <= 0) return '-'
  return `${(mwh / 1000).toFixed(2)} Wh`
}

// Battery health is a 0–1 ratio → percent, 2 decimals (Electron "{0:0.00}%").
export function formatHealthPercent(health: number | null | undefined): string {
  if (health == null || !Number.isFinite(health) || health < 0) return '-'
  return `${(health * 100).toFixed(2)}%`
}

export interface SensorDetailsProps {
  cpu?: SensorsCpu
  gpu?: SensorsGpu
  memory?: SensorsMemory
  battery?: SensorsBattery
  storageTemperatures?: (number | null)[]
  gpuIsIntegrated?: boolean
  temperatureUnit?: TemperatureUnit
}

// Builds the Electron detail-window row sets for the three sensor panels.
export function useSensorDetails(props: SensorDetailsProps): {
  cpu: SensorDetailRow[]
  gpu: SensorDetailRow[]
  battery: SensorDetailRow[]
} {
  const { t } = useTranslation()
  const unit = props.temperatureUnit ?? 'C'
  const lastBatteryPower = useRef('-')

  // Battery current power: charge rate in mW; -1/unknown keeps the last value.
  const chargeRateW =
    props.battery?.chargeRate != null && Number.isFinite(props.battery.chargeRate) && props.battery.chargeRate !== -1
      ? props.battery.chargeRate / 1000
      : -1
  const batteryPowerText = formatPowerKeepingPrevious(chargeRateW, lastBatteryPower.current)
  lastBatteryPower.current = batteryPowerText

  const storageTemps = (props.storageTemperatures ?? []).filter((v) => v != null && Number.isFinite(v) && v >= 0)
  const ssdTemperatureText =
    storageTemps.length > 0 ? storageTemps.map((v) => formatTemperature(v, unit)).join(' / ') : '-'

  const cpu: SensorDetailRow[] = [
    {
      label: t('dashboard.sensor.detail.power'),
      value: formatCpuPowerBreakdown(props.cpu ?? {}, {
        cores: t('dashboard.sensor.detail.powerCores'),
        memory: t('dashboard.sensor.detail.powerMemory'),
        platform: t('dashboard.sensor.detail.powerPlatform')
      })
    },
    { label: t('dashboard.sensor.voltage'), value: formatVoltage(props.cpu?.voltage) },
    { label: t('dashboard.sensor.detail.pCoreClock'), value: formatFrequency(props.cpu?.pCoreClock) },
    { label: t('dashboard.sensor.detail.eCoreClock'), value: formatFrequency(props.cpu?.eCoreClock) },
    {
      label: t('dashboard.sensor.detail.memoryUsage'),
      value: formatUsageInGigabytes(props.memory?.usedMb, props.memory?.totalMb, props.memory?.usage)
    },
    { label: t('dashboard.sensor.memoryTemperature'), value: formatTemperature(props.memory?.highestTemperature, unit) },
    { label: t('dashboard.sensor.ssdTemperature'), value: ssdTemperatureText }
  ]

  const gpu: SensorDetailRow[] = [
    { label: t('dashboard.sensor.detail.power'), value: formatPower(props.gpu?.power) },
    { label: t('dashboard.sensor.voltage'), value: formatVoltage(props.gpu?.voltage) },
    {
      label: props.gpuIsIntegrated
        ? t('dashboard.sensor.detail.sharedMemoryUsage')
        : t('dashboard.sensor.detail.vramUsage'),
      value: formatUsageInGigabytes(props.gpu?.vramUsedMb, props.gpu?.vramTotalMb, props.gpu?.vramUtilization)
    },
    {
      label: t('dashboard.sensor.detail.vramClock', { defaultValue: 'VRAM Clock' }),
      value: formatFrequency(props.gpu?.memoryClock)
    },
    { label: t('dashboard.sensor.vramTemperature'), value: formatTemperature(props.gpu?.vramTemperature, unit) },
    { label: t('dashboard.sensor.detail.hotSpot'), value: formatTemperature(props.gpu?.hotSpotTemperature, unit) },
    {
      label: t('dashboard.sensor.detail.pcieThroughput'),
      value: formatThroughputPair(props.gpu?.pcieRxThroughput, props.gpu?.pcieTxThroughput)
    }
  ]

  const battery: SensorDetailRow[] = [
    { label: t('dashboard.sensor.detail.designCapacity'), value: formatWattHours(props.battery?.designCapacity) },
    { label: t('dashboard.sensor.detail.fullChargeCapacity'), value: formatWattHours(props.battery?.fullChargeCapacity) },
    { label: t('dashboard.sensor.health'), value: formatHealthPercent(props.battery?.health) },
    {
      label: t('dashboard.sensor.cycles'),
      value:
        props.battery?.cycleCount != null && Number.isFinite(props.battery.cycleCount) && props.battery.cycleCount >= 0
          ? String(Math.round(props.battery.cycleCount))
          : '-'
    },
    {
      label: t('dashboard.sensor.date'),
      value: (() => {
        const iso = props.battery?.manufactureDate ?? props.battery?.firstUseDate
        if (iso == null || iso === '') return '-'
        const parsed = new Date(`${iso}T00:00:00`)
        return Number.isFinite(parsed.getTime()) ? formatDateForUi(parsed) : iso
      })()
    },
    { label: t('dashboard.sensor.voltage'), value: formatVoltage(props.battery?.voltage) },
    {
      label: t('dashboard.sensor.powerRange'),
      value: (() => {
        const minMw = props.battery?.minDischargeRate
        const maxMw = props.battery?.maxDischargeRate
        const toW = (mw: number): string => {
          const w = mw / 1000
          const sign = w > 0 ? '+' : w < 0 ? '-' : ''
          return `${sign}${Math.abs(w).toFixed(2)} W`
        }
        const minOk = minMw != null && Number.isFinite(minMw) && minMw !== -1
        const maxOk = maxMw != null && Number.isFinite(maxMw) && maxMw !== -1
        if (!minOk && !maxOk) return '-'
        if (minOk && maxOk) return `${toW(minMw)} ~ ${toW(maxMw)}`
        return toW((minOk ? minMw : maxMw) as number)
      })()
    },
    {
      label: t('dashboard.sensor.detail.currentPower', { defaultValue: 'Current Power' }),
      value: batteryPowerText
    }
  ]

  return { cpu, gpu, battery }
}

// Standalone three-card details panel (CPU / GPU / Battery).
export function SensorDetails(props: SensorDetailsProps): React.JSX.Element {
  const { t } = useTranslation()
  const { cpu, gpu, battery } = useSensorDetails(props)
  const cards: { title: string; rows: SensorDetailRow[] }[] = [
    { title: t('dashboard.sensor.cpu'), rows: cpu },
    { title: t('dashboard.sensor.gpu'), rows: gpu },
    { title: t('dashboard.sensor.battery'), rows: battery }
  ]
  return (
    <div className="udt-sensor-details">
      {cards.map((card) => (
        <section key={card.title} className="udt-sensor-details__card">
          <h3 className="udt-sensor-details__title">{card.title}</h3>
          <dl className="udt-sensor-details__rows">
            {card.rows.map((row) => (
              <div key={row.label} className="udt-sensor-details__row">
                <dt>{row.label}</dt>
                <dd title={row.value}>{row.value}</dd>
              </div>
            ))}
          </dl>
        </section>
      ))}
    </div>
  )
}

export default SensorDetails
