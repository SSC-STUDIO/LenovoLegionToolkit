import './fanCurve.css'
import { useCallback, useEffect, useId, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'

// Port of Electron Controls/FanCurveControl: 10 vertical integer sliders (0-10 fan
// speed steps) plotted as a polyline with an area fill on a rounded chart
// surface. Left-to-right values are kept non-decreasing (VerifyValues), each
// slider is clamped to the minimum fan table (if provided), and hovering or
// dragging a thumb shows a tooltip with per-sensor "temp °C @ rpm RPM" rows.

export type FanCurveSensorType = 'CPU' | 'CPUSensor' | 'GPU' | 'GPU2'

export interface FanCurveSensorData {
  type: FanCurveSensorType
  fanSpeeds: number[]
  temps: number[]
}

export interface FanCurveEditorProps {
  value: number[]
  minimum?: number[]
  sensors?: FanCurveSensorData[]
  disabled?: boolean
  height?: number
  onChange?: (value: number[]) => void
  onChangeCommit?: (value: number[]) => void
}

const STEP_MIN = 0
const STEP_MAX = 10
const THUMB_SIZE = 18
const THUMB_RADIUS = THUMB_SIZE / 2
const TRACK_MARGIN_Y = 10
const PADDING_X = 14
const PADDING_TOP = 12
const PADDING_BOTTOM = 14
const GRID_LINES = 5
const LINE_COLOR = '#4f9df7'
const CELSIUS = '\u00B0C'

const SENSOR_LABEL_KEYS: Record<FanCurveSensorType, string> = {
  CPU: 'cpu',
  CPUSensor: 'cpuSensor',
  GPU: 'gpu',
  GPU2: 'gpu2'
}

const SENSOR_ORDER: FanCurveSensorType[] = ['CPU', 'CPUSensor', 'GPU', 'GPU2']

export default function FanCurveEditor({
  value,
  minimum,
  sensors = [],
  disabled = false,
  height = 230,
  onChange,
  onChangeCommit
}: FanCurveEditorProps): React.JSX.Element {
  const { t } = useTranslation()
  const gradientId = useId()
  const plotRef = useRef<HTMLDivElement>(null)
  const [contentWidth, setContentWidth] = useState(0)
  const [dragIndex, setDragIndex] = useState<number | null>(null)
  const [hoverIndex, setHoverIndex] = useState<number | null>(null)

  const values = value.length >= 2 ? value.slice(0, 10) : []
  const count = values.length
  const contentHeight = height - PADDING_TOP - PADDING_BOTTOM
  const rangeTop = TRACK_MARGIN_Y + THUMB_RADIUS
  const rangeBottom = contentHeight - TRACK_MARGIN_Y - THUMB_RADIUS

  useEffect(() => {
    const el = plotRef.current
    if (!el) return
    const update = (): void => {
      setContentWidth(Math.max(0, el.clientWidth - PADDING_X * 2))
    }
    update()
    const observer = new ResizeObserver(update)
    observer.observe(el)
    return () => observer.disconnect()
  }, [])

  const valueY = useCallback(
    (v: number): number => {
      if (rangeBottom <= rangeTop) return rangeBottom
      const ratio = Math.min(1, Math.max(0, (v - STEP_MIN) / (STEP_MAX - STEP_MIN)))
      return rangeBottom - ratio * (rangeBottom - rangeTop)
    },
    [rangeTop, rangeBottom]
  )

  const applyValue = useCallback(
    (index: number, raw: number): number[] => {
      let v = Math.min(STEP_MAX, Math.max(STEP_MIN, Math.round(raw)))
      if (minimum && minimum[index] != null) v = Math.max(v, minimum[index])
      const next = [...values]
      next[index] = v
      for (let i = 0; i < index; i++) if (next[i] > v) next[i] = v
      for (let i = index + 1; i < next.length; i++) if (next[i] < v) next[i] = v
      return next
    },
    [values, minimum]
  )

  const valueFromPointer = (clientY: number): number => {
    const el = plotRef.current
    if (!el) return 0
    const rect = el.getBoundingClientRect()
    const y = clientY - rect.top - PADDING_TOP
    const span = rangeBottom - rangeTop
    const ratio = span > 0 ? 1 - (y - rangeTop) / span : 0
    return ratio * STEP_MAX
  }

  const handlePointerDown = (index: number) => (e: React.PointerEvent<HTMLDivElement>): void => {
    if (disabled) return
    e.preventDefault()
    e.currentTarget.setPointerCapture(e.pointerId)
    setDragIndex(index)
    onChange?.(applyValue(index, valueFromPointer(e.clientY)))
  }

  const handlePointerMove = (e: React.PointerEvent<HTMLDivElement>): void => {
    if (dragIndex == null) return
    onChange?.(applyValue(dragIndex, valueFromPointer(e.clientY)))
  }

  const handlePointerUp = (e: React.PointerEvent<HTMLDivElement>): void => {
    if (dragIndex == null) return
    const next = applyValue(dragIndex, valueFromPointer(e.clientY))
    onChange?.(next)
    onChangeCommit?.(next)
    setDragIndex(null)
  }

  const points = values.map((v, i) => ({
    x: ((i + 0.5) / count) * contentWidth,
    y: valueY(v)
  }))

  const linePath = points
    .map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(2)},${p.y.toFixed(2)}`)
    .join(' ')

  const areaPath =
    points.length > 0
      ? `${linePath} L${points[points.length - 1].x.toFixed(2)},${rangeBottom.toFixed(2)} L${points[0].x.toFixed(2)},${rangeBottom.toFixed(2)} Z`
      : ''

  const axisTemps = sensors.find((s) => s.temps.length >= count)?.temps

  const formatTempLabel = (index: number): string => {
    const temp = axisTemps?.[index]
    return temp == null || temp >= 127 ? '-' : `${temp}${CELSIUS}`
  }

  const formatTooltipValue = (sensor: FanCurveSensorData, index: number, v: number): string => {
    const temp = sensor.temps[index]
    if (temp == null || temp >= 127) return '-'
    const rpmIndex = v - 1
    if (rpmIndex < 0) return `0 ${t('fanCurve.rpm')}`
    const rpm = sensor.fanSpeeds[rpmIndex]
    if (rpm == null) return '-'
    return `${temp}${CELSIUS} @ ${rpm} ${t('fanCurve.rpm')}`
  }

  const activeIndex = dragIndex ?? hoverIndex
  const tooltipRows =
    activeIndex == null || count === 0
      ? []
      : SENSOR_ORDER.map((type) => {
          const sensor = sensors.find((s) => s.type === type)
          if (!sensor) return null
          return {
            label: t(`fanCurve.${SENSOR_LABEL_KEYS[type]}`),
            value: formatTooltipValue(sensor, activeIndex, values[activeIndex] ?? 0)
          }
        }).filter((row): row is { label: string; value: string } => row != null)

  const tooltipX = activeIndex == null ? 0 : PADDING_X + (points[activeIndex]?.x ?? 0)
  const tooltipY = activeIndex == null ? 0 : PADDING_TOP + (points[activeIndex]?.y ?? 0)

  return (
    <div className={`udt-fan-curve${disabled ? ' udt-fan-curve--disabled' : ''}`}>
      <div className="udt-fan-curve__body">
        <div className="udt-fan-curve__y-labels">
          <span className="udt-fan-curve__y-label udt-fan-curve__y-label--top">
            {t('fanCurve.fanSpeedMax')}
          </span>
          <span className="udt-fan-curve__y-label">80%</span>
          <span className="udt-fan-curve__y-label">60%</span>
          <span className="udt-fan-curve__y-label">40%</span>
          <span className="udt-fan-curve__y-label">20%</span>
          <span className="udt-fan-curve__y-label udt-fan-curve__y-label--bottom">
            {t('fanCurve.fanSpeed')}
          </span>
        </div>

        <div className="udt-fan-curve__plot-wrap">
          <div className="udt-fan-curve__plot" ref={plotRef} style={{ height }}>
            {contentWidth > 0 && count > 1 && (
              <svg
                className="udt-fan-curve__graph"
                width={contentWidth}
                height={contentHeight}
                aria-hidden="true"
              >
                <defs>
                  <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0" stopColor={LINE_COLOR} stopOpacity={0.431} />
                    <stop offset="1" stopColor={LINE_COLOR} stopOpacity={0.094} />
                  </linearGradient>
                </defs>
                {Array.from({ length: GRID_LINES + 1 }, (_, i) => {
                  const y = rangeTop + ((rangeBottom - rangeTop) * i) / GRID_LINES
                  return (
                    <line
                      key={i}
                      x1={0}
                      y1={y}
                      x2={contentWidth}
                      y2={y}
                      style={{ stroke: 'var(--udt-chart-gridline)' }}
                      strokeWidth={0.75}
                      opacity={0.7}
                    />
                  )
                })}
                <path d={areaPath} fill={`url(#${gradientId})`} />
                <path
                  d={linePath}
                  fill="none"
                  style={{ stroke: 'var(--udt-chart-utilization)' }}
                  strokeWidth={2.25}
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            )}

            {contentWidth > 0 && count > 1 && (
              <div className="udt-fan-curve__sliders">
                {values.map((_, i) => {
                  const x = points[i].x
                  const y = points[i].y
                  return (
                    <div
                      key={i}
                      className="udt-fan-curve__column"
                      style={{ left: `${(i / count) * 100}%`, width: `${100 / count}%` }}
                      onPointerDown={handlePointerDown(i)}
                      onPointerMove={handlePointerMove}
                      onPointerUp={handlePointerUp}
                      onPointerCancel={handlePointerUp}
                      onPointerEnter={() => {
                        if (dragIndex == null) setHoverIndex(i)
                      }}
                      onPointerLeave={() => {
                        if (dragIndex == null) setHoverIndex(null)
                      }}
                    >
                      <div
                        className={`udt-fan-curve__thumb${dragIndex === i ? ' udt-fan-curve__thumb--dragging' : ''}`}
                        style={{ left: x - THUMB_RADIUS, top: y - THUMB_RADIUS }}
                      />
                    </div>
                  )
                })}
              </div>
            )}
          </div>

          {tooltipRows.length > 0 && activeIndex != null && (
            <div
              className="udt-fan-curve__tooltip"
              style={{ left: tooltipX, top: tooltipY }}
            >
              {tooltipRows.map((row) => (
                <div key={row.label} className="udt-fan-curve__tooltip-row">
                  <dt>{row.label}</dt>
                  <dd>{row.value}</dd>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="udt-fan-curve__x-labels">
          {values.map((_, i) => (
            <span key={i} className="udt-fan-curve__x-label">
              {formatTempLabel(i)}
            </span>
          ))}
        </div>
      </div>
    </div>
  )
}
