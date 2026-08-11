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
// riding the arc end, 350ms cubic ease-out value animation.
const DEFAULT_SIZE = 110
const START_ANGLE = 225
const END_ANGLE = -45
const SWEEP_ANGLE = 270
const ANIMATION_MS = 350
const TIP_DIAMETER = 9
const TIP_STROKE_WIDTH = 2.1
const GLOW_EXTRA_THICKNESS = 5
const GLOW_ALPHA = 0.25

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
  const containerRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<ECharts | null>(null)
  const isDark = useThemeStore((s) => s.themeMode === 'dark')

  const buildOption = useCallback(
    (): EChartsCoreOption => {
      const numeric = value != null && Number.isFinite(value) ? value : null
      const ratio = numeric == null ? 0 : Math.min(1, Math.max(0, (numeric - min) / (max - min)))
      const valueText = numeric == null ? '-' : numeric.toFixed(digits)
      const primary = isDark ? 'rgba(255, 255, 255, 0.92)' : 'rgba(0, 0, 0, 0.89)'
      const secondary = isDark ? 'rgba(255, 255, 255, 0.77)' : 'rgba(0, 0, 0, 0.62)'
      // Caption = secondary at 0.7 opacity (WPF: Opacity="0.7" on caption TextBlock).
      const captionColor = isDark ? 'rgba(255, 255, 255, 0.54)' : 'rgba(0, 0, 0, 0.43)'
      const track = isDark ? 'rgba(255, 255, 255, 0.18)' : 'rgba(0, 0, 0, 0.14)'
      const valueFontSize = Math.min(Math.max(size * 0.2, 14), 40)
      const captionFontSize = Math.min(Math.max(size * 0.1, 10), 18)
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
      // White tip dot riding the leading edge of the value arc. ECharts has no
      // native support, so a graphic circle is placed on the arc end point:
      // screen angle = 135° (lower-left) + 270° * ratio, clockwise (y-down).
      // Hidden while the arc is zero-length (value null or at/under min),
      // matching WPF collapsing the tip when the sweep is empty.
      const radiusPx = size * 0.45
      const tipAngleRad = ((START_ANGLE - SWEEP_ANGLE * ratio) * Math.PI) / 180
      // ECharts 6 runtime still supports primitive 'circle' graphic elements, but its
      // published types only expose group/path/image/text — cast to keep the visual.
      const tip = (
        numeric == null || ratio <= 0
          ? []
          : [
              {
                type: 'circle',
                z: 10,
                silent: true,
                shape: { cx: 0, cy: 0, r: TIP_DIAMETER / 2 },
                position: [
                  size / 2 + radiusPx * Math.cos(tipAngleRad),
                  size / 2 - radiusPx * Math.sin(tipAngleRad)
                ],
                style: { fill: '#ffffff', stroke: color, lineWidth: TIP_STROKE_WIDTH }
              }
            ]
      ) as unknown as EChartsCoreOption['graphic']

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
        data: [{ value: numeric ?? min }]
      }

      return {
        animationDuration: ANIMATION_MS,
        animationDurationUpdate: ANIMATION_MS,
        animationEasing: 'cubicOut',
        animationEasingUpdate: 'cubicOut',
        graphic: tip,
        series: [
          {
            ...baseSeries,
            silent: true,
            axisLine: { show: false },
            // Glow underlay: same arc, 5px thicker, ring color at 25% opacity.
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
            },
            title: {
              show: label != null && label !== '',
              offsetCenter: [0, '88%'],
              fontSize: captionFontSize,
              color: captionColor
            },
            detail: {
              show: true,
              offsetCenter: [0, '0%'],
              formatter: (): string =>
                unit ? `{value|${valueText}}{unit| ${unit}}` : `{value|${valueText}}`,
              rich: {
                value: { fontSize: valueFontSize, fontWeight: 500, lineHeight: valueFontSize * 1.4, color: primary },
                unit: {
                  fontSize: captionFontSize,
                  lineHeight: valueFontSize * 1.4,
                  verticalAlign: 'bottom',
                  color: secondary
                }
              }
            }
          }
        ]
      }
    },
    [value, min, max, unit, label, color, digits, isDark, size, thickness]
  )

  useEffect(() => {
    const el = containerRef.current
    if (!el) return
    const chart = init(el)
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
      style={{ width: size, height: size, flexShrink: 0 }}
    />
  )
}
