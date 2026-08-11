import { useCallback, useEffect, useRef } from 'react'
import * as echarts from 'echarts'
import { useThemeStore } from '../../stores/themeStore'

export function formatSensorValue(value: number | null | undefined, digits = 0): string {
  if (value == null || !Number.isFinite(value)) return '--'
  return value.toFixed(digits)
}

export interface SensorGaugeProps {
  value?: number | null
  min?: number
  max?: number
  unit?: string
  label?: string
  color?: string
  digits?: number
}

// Matches WPF RadialGaugeControl: open-bottom 270° ring (gap at bottom),
// round-cap arcs, gradient value arc.
const GAUGE_SIZE = 112
const ARC_THICKNESS = 6
const START_ANGLE = 225
const END_ANGLE = -45
const ANIMATION_MS = 350

function lighten(hex: string, amount: number): string {
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  const channel = (c: number): number => Math.round(c + (255 - c) * amount)
  return `rgb(${channel(r)}, ${channel(g)}, ${channel(b)})`
}

export default function SensorGauge({
  value,
  min = 0,
  max = 100,
  unit,
  label,
  color = '#1677ff',
  digits = 0
}: SensorGaugeProps): React.JSX.Element {
  const containerRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<echarts.ECharts | null>(null)
  const isDark = useThemeStore((s) => s.themeMode === 'dark')

  const buildOption = useCallback(
    (): echarts.EChartsOption => {
      const numeric = value != null && Number.isFinite(value) ? value : null
      const valueText = numeric == null ? '--' : numeric.toFixed(digits)
      const primary = isDark ? 'rgba(255, 255, 255, 0.92)' : 'rgba(0, 0, 0, 0.88)'
      const secondary = isDark ? 'rgba(255, 255, 255, 0.55)' : 'rgba(0, 0, 0, 0.45)'
      const track = isDark ? 'rgba(255, 255, 255, 0.10)' : 'rgba(0, 0, 0, 0.08)'
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
      return {
        animationDuration: ANIMATION_MS,
        animationDurationUpdate: ANIMATION_MS,
        series: [
          {
            type: 'gauge',
            startAngle: START_ANGLE,
            endAngle: END_ANGLE,
            center: ['50%', '50%'] as [string, string],
            radius: '92%',
            min,
            max,
            axisLine: {
              roundCap: true,
              lineStyle: { width: ARC_THICKNESS, color: [[1, track]] }
            },
            progress: {
              show: true,
              width: ARC_THICKNESS,
              roundCap: true,
              itemStyle: { color: arcGradient }
            },
            axisTick: { show: false },
            splitLine: { show: false },
            axisLabel: { show: false },
            pointer: { show: false },
            anchor: { show: false },
            title: {
              show: label != null && label !== '',
              offsetCenter: [0, '86%'],
              fontSize: 12,
              color: secondary
            },
            detail: {
              show: true,
              offsetCenter: [0, '2%'],
              formatter: (): string =>
                unit ? `{value|${valueText}}{unit| ${unit}}` : `{value|${valueText}}`,
              rich: {
                value: { fontSize: 24, fontWeight: 600, lineHeight: 30, color: primary },
                unit: {
                  fontSize: 12,
                  lineHeight: 30,
                  verticalAlign: 'bottom',
                  color: secondary
                }
              }
            },
            data: [{ value: numeric ?? min }]
          }
        ]
      }
    },
    [value, min, max, unit, label, color, digits, isDark]
  )

  useEffect(() => {
    const el = containerRef.current
    if (!el) return
    const chart = echarts.init(el)
    chartRef.current = chart
    return () => {
      chart.dispose()
      chartRef.current = null
    }
  }, [])

  useEffect(() => {
    const chart = chartRef.current
    if (!chart) return
    chart.setOption(buildOption())
  }, [buildOption])

  useEffect(() => {
    const chart = chartRef.current
    if (!chart) return
    const handleResize = (): void => {
      chart.resize()
      chart.setOption(buildOption())
    }
    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [buildOption])

  return (
    <div
      ref={containerRef}
      style={{ width: GAUGE_SIZE, height: GAUGE_SIZE, flexShrink: 0 }}
    />
  )
}
