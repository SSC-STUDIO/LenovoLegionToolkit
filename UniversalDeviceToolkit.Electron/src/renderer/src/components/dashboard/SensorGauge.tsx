import { useCallback, useEffect, useRef } from 'react'
import { init, type ECharts, type EChartsCoreOption } from '../../utils/echarts'
import { useThemeStore } from '../../stores/themeStore'

export interface SensorGaugeProps {
  value?: number | null
  min?: number
  max?: number
  unit?: string
  label?: string
  color?: string
  digits?: number
  size?: number
  thickness?: number
}

// Matches WPF RadialGaugeControl: open-bottom 270° ring (start 225° / end -45°),
// track rgba(255,255,255,0.18), value arc gradient (lighten 0.35 at start),
// glow (arc + 5px, 25% ring color), white tip dot (9px, 2.1px ring stroke)
// riding the arc end, cubic ease-out value animation.
const DEFAULT_SIZE = 110
const START_ANGLE = 225
const END_ANGLE = -45
const SWEEP_ANGLE = 270
const ANIMATION_MS = 480
const TIP_DIAMETER = 9
const TIP_STROKE_WIDTH = 2.1
const GLOW_EXTRA_THICKNESS = 5
const GLOW_ALPHA = 0.25
/** Hide the caption once the ring is too small for a value + label stack. */
const CAPTION_MIN_DIAMETER = 80

function lighten(hex: string, amount: number): string {
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  const channel = (c: number): number => Math.round(c + (255 - c) * amount)
  return `rgb(${channel(r)}, ${channel(g)}, ${channel(b)})`
}

function hexToRgba(hex: string, alpha: number): string {
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value))
}

/** Identity of the gauge skeleton (everything except the live value). */
function skeletonKey(props: {
  min: number
  max: number
  color: string
  thickness: number
  isDark: boolean
}): string {
  return `${props.isDark ? 'd' : 'l'}|${props.min}|${props.max}|${props.color}|${props.thickness}`
}

function applyReadoutMetrics(el: HTMLElement, diameter: number): void {
  const valueFont = clamp(diameter * 0.2, 12, 28)
  const captionFont = clamp(diameter * 0.095, 9, 13)
  const gap = Math.max(2, Math.round(diameter * 0.035))
  el.style.setProperty('--udt-sensor-gauge-value-size', `${valueFont}px`)
  el.style.setProperty('--udt-sensor-gauge-caption-size', `${captionFont}px`)
  el.style.setProperty('--udt-sensor-gauge-gap', `${gap}px`)
  el.classList.toggle('udt-sensor-gauge--compact', diameter < CAPTION_MIN_DIAMETER)
}

export default function SensorGauge({
  value,
  min = 0,
  max = 100,
  unit,
  label,
  color = '#4f9df7',
  digits = 0,
  size = DEFAULT_SIZE,
  thickness = 6
}: SensorGaugeProps): React.JSX.Element {
  const rootRef = useRef<HTMLDivElement>(null)
  const ringRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<ECharts | null>(null)
  const measuredRef = useRef(size)
  const valueRef = useRef(value)
  const minRef = useRef(min)
  const maxRef = useRef(max)
  const colorRef = useRef(color)
  valueRef.current = value
  minRef.current = min
  maxRef.current = max
  colorRef.current = color
  const isDark = useThemeStore((s) => s.themeMode === 'dark')
  // Cached skeleton option; only the value/data slice is updated per tick.
  // Cleared on dispose so React Strict Mode remounts re-apply the ring.
  const baseOptionRef = useRef<{ key: string; option: EChartsCoreOption } | null>(null)

  const buildSkeleton = useCallback((): EChartsCoreOption => {
    const track = isDark ? 'rgba(255, 255, 255, 0.18)' : 'rgba(0, 0, 0, 0.18)'
    const arcGradient = {
      type: 'linear' as const,
      x: 0,
      y: 0,
      x2: 1,
      y2: 1,
      colorStops: [
        { offset: 0, color: lighten(color, 0.35) },
        { offset: 1, color }
      ]
    }

    const baseSeries = {
      type: 'gauge' as const,
      startAngle: START_ANGLE,
      endAngle: END_ANGLE,
      center: ['50%', '50%'],
      radius: '90%',
      min,
      max,
      axisTick: { show: false },
      splitLine: { show: false },
      axisLabel: { show: false },
      pointer: { show: false },
      anchor: { show: false },
      // Readout is an HTML overlay — never draw ECharts title/detail text.
      title: { show: false },
      detail: { show: false },
      data: [{ value: min }]
    }

    return {
      animationDuration: ANIMATION_MS,
      animationDurationUpdate: ANIMATION_MS,
      animationEasing: 'cubicOut',
      animationEasingUpdate: 'cubicOut',
      graphic: [],
      series: [
        {
          ...baseSeries,
          silent: true,
          axisLine: { show: false },
          progress: {
            show: true,
            width: thickness + GLOW_EXTRA_THICKNESS,
            roundCap: true,
            itemStyle: { color: hexToRgba(color, GLOW_ALPHA) }
          }
        },
        {
          ...baseSeries,
          axisLine: {
            roundCap: true,
            lineStyle: { width: thickness, color: [[1, track]] }
          },
          progress: {
            show: true,
            width: thickness,
            roundCap: true,
            itemStyle: { color: arcGradient }
          }
        }
      ]
    }
  }, [min, max, color, isDark, thickness])

  const syncTip = useCallback((): void => {
    const chart = chartRef.current
    if (!chart) return
    const numeric = valueRef.current
    const lo = minRef.current
    const hi = maxRef.current
    const ringColor = colorRef.current
    const finite = numeric != null && Number.isFinite(numeric) ? numeric : null
    const ratio = finite == null ? 0 : Math.min(1, Math.max(0, (finite - lo) / (hi - lo)))
    const diameter = measuredRef.current
    const radiusPx = diameter * 0.45
    const tipAngleRad = ((START_ANGLE - SWEEP_ANGLE * ratio) * Math.PI) / 180
    const tip = (
      finite == null || ratio <= 0
        ? []
        : [
            {
              type: 'circle',
              z: 10,
              silent: true,
              shape: { cx: 0, cy: 0, r: TIP_DIAMETER / 2 },
              position: [
                diameter / 2 + radiusPx * Math.cos(tipAngleRad),
                diameter / 2 - radiusPx * Math.sin(tipAngleRad)
              ],
              style: { fill: '#ffffff', stroke: ringColor, lineWidth: TIP_STROKE_WIDTH }
            }
          ]
    ) as unknown as EChartsCoreOption['graphic']

    chart.setOption({
      graphic: tip,
      series: [{ data: [{ value: finite ?? lo }] }, { data: [{ value: finite ?? lo }] }]
    })
  }, [])

  useEffect(() => {
    const ring = ringRef.current
    const root = rootRef.current
    if (!ring || !root) return
    const chart = init(ring)
    chartRef.current = chart
    baseOptionRef.current = null

    const applySize = (): void => {
      const diameter = Math.round(root.getBoundingClientRect().width)
      if (diameter <= 0) return
      measuredRef.current = diameter
      applyReadoutMetrics(root, diameter)
      chart.resize()
      syncTip()
    }

    applySize()
    const observer = new ResizeObserver(applySize)
    observer.observe(root)
    window.addEventListener('resize', applySize)
    return () => {
      window.removeEventListener('resize', applySize)
      observer.disconnect()
      chart.dispose()
      chartRef.current = null
      baseOptionRef.current = null
    }
  }, [syncTip])

  // Skeleton path: full setOption only when static props or theme change,
  // and always after a fresh chart instance (Strict Mode remount).
  useEffect(() => {
    const chart = chartRef.current
    if (!chart) return
    const key = skeletonKey({ min, max, color, thickness, isDark })
    const base = baseOptionRef.current
    if (base !== null && base.key === key) return
    const option = buildSkeleton()
    baseOptionRef.current = { key, option }
    chart.setOption(option, { notMerge: true })
    syncTip()
  }, [buildSkeleton, min, max, color, thickness, isDark, syncTip])

  useEffect(() => {
    syncTip()
  }, [value, min, max, color, syncTip])

  const numeric = value != null && Number.isFinite(value) ? value.toFixed(digits) : '—'
  const aria = [numeric, unit, label].filter((part) => part != null && part !== '').join(' ')
  const hasCaption = label != null && label !== ''

  return (
    <div
      ref={rootRef}
      className="udt-sensor-gauge"
      style={{ maxWidth: size }}
      role="img"
      aria-label={aria}
    >
      <div ref={ringRef} className="udt-sensor-gauge__ring" />
      <div className="udt-sensor-gauge__readout">
        <span className="udt-sensor-gauge__value">
          {numeric}
          {unit != null && unit !== '' && <span className="udt-sensor-gauge__unit">{unit}</span>}
        </span>
        {hasCaption && (
          <span className="udt-sensor-gauge__caption">{label}</span>
        )}
      </div>
    </div>
  )
}
