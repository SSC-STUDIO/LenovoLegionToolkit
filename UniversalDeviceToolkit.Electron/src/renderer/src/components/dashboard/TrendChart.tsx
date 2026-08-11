import { useEffect, useRef } from 'react'
import { init, type ECharts } from '../../utils/echarts'
import { useThemeStore } from '../../stores/themeStore'

export interface TrendSeries {
  name: string
  color: string
  data: (number | null)[]
  max?: number
}

export interface TrendChartProps {
  series: TrendSeries[]
  labels: string[]
  height?: number
}

function withAlpha(hex: string, alpha: number): string {
  const r = parseInt(hex.slice(1, 3), 16)
  const g = parseInt(hex.slice(3, 5), 16)
  const b = parseInt(hex.slice(5, 7), 16)
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

export default function TrendChart({
  series,
  labels,
  height = 116
}: TrendChartProps): React.JSX.Element {
  const containerRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<ECharts | null>(null)
  const isDark = useThemeStore((s) => s.themeMode === 'dark')
  // WPF _smoothedAutoMax: auto-scaled series converge toward the observed max
  // instead of jumping (rise fast, fall slow).
  const smoothedMaxRef = useRef<Record<string, number>>({})

  useEffect(() => {
    const el = containerRef.current
    if (!el) return
    const chart = init(el)
    chartRef.current = chart
    const handleResize = (): void => {
      chart.resize()
    }
    window.addEventListener('resize', handleResize)
    return () => {
      window.removeEventListener('resize', handleResize)
      chart.dispose()
      chartRef.current = null
    }
  }, [])

  useEffect(() => {
    const chart = chartRef.current
    if (!chart) return
    const gridlineColor = isDark ? 'rgba(255, 255, 255, 0.10)' : 'rgba(0, 0, 0, 0.10)'
    const labelColor = isDark ? 'rgba(255, 255, 255, 0.53)' : 'rgba(0, 0, 0, 0.45)'
    const baselineColor = isDark ? '#d2a05a' : 'rgba(210, 160, 90, 0.85)'

    // WPF normalizes each series against its own Maximum (fixed or smoothed
    // auto observed*1.08), so all series share a 0..1 plot space where "100%"
    // is each series' own max.
    const smoothed = { ...smoothedMaxRef.current }
    const normalized = series.map((s) => {
      const fixedMax = s.max != null && s.max > 0 ? s.max : null
      let effectiveMax = fixedMax
      if (effectiveMax == null) {
        const observed = Math.max(
          1,
          ...s.data.filter((v): v is number => v != null && Number.isFinite(v) && v >= 0)
        )
        const target = observed * 1.08
        const previous = smoothed[s.name]
        effectiveMax = previous === undefined ? target : previous + (target - previous) * 0.35
        smoothed[s.name] = effectiveMax
      }
      const data = s.data.map((v) =>
        v == null || !Number.isFinite(v) ? null : Math.min(1, Math.max(0, v / effectiveMax))
      )
      return { name: s.name, color: s.color, data }
    })
    smoothedMaxRef.current = smoothed

    // Gridlines at 75%/50%/25% only (0.5px), warm baseline at the bottom edge.
    // Drawn via markLine on the first series since ECharts cannot hide the
    // 0%/100% splitLines individually.
    const gridlines = [0.75, 0.5, 0.25].map((y) => ({
      yAxis: y,
      lineStyle: { color: gridlineColor, width: 0.5 }
    }))
    const baseline = { yAxis: 0, lineStyle: { color: baselineColor, width: 1 } }

    chart.setOption({
      animation: false,
      grid: {
        top: 4,
        left: 26,
        right: 4,
        bottom: 4,
        containLabel: false
      },
      xAxis: {
        type: 'category',
        boundaryGap: false,
        data: labels,
        axisLine: { show: false },
        axisTick: { show: false },
        axisLabel: { show: false }
      },
      yAxis: {
        type: 'value',
        min: 0,
        max: 1,
        interval: 0.25,
        axisLine: { show: false },
        axisTick: { show: false },
        axisLabel: {
          show: true,
          fontSize: 9,
          color: labelColor,
          margin: 4,
          formatter: (value: number): string => {
            const percent = Math.round(value * 100)
            return percent <= 0 ? '' : `${percent}%`
          }
        },
        splitLine: { show: false }
      },
      series: normalized.map((s, index) => ({
        name: s.name,
        type: 'line' as const,
        showSymbol: false,
        smooth: 0.5,
        lineStyle: { width: 1.15, color: s.color, cap: 'round', join: 'round' },
        itemStyle: { color: s.color },
        areaStyle: {
          // WPF tapers the area polygon toward the latest point (right edge).
          // ECharts cannot reshape the path, so a diagonal gradient approximates
          // the silhouette: strongest top-left, fading toward the bottom-right
          // corner where the tapered tail sits.
          color: {
            type: 'linear' as const,
            x: 0,
            y: 0,
            x2: 1,
            y2: 1,
            colorStops: [
              { offset: 0, color: withAlpha(s.color, 0.298) },
              { offset: 0.48, color: withAlpha(s.color, 0.165) },
              { offset: 0.86, color: withAlpha(s.color, 0.082) },
              { offset: 1, color: withAlpha(s.color, 0.02) }
            ]
          }
        },
        markLine:
          index === 0
            ? {
                silent: true,
                symbol: 'none',
                animation: false,
                label: { show: false },
                data: [...gridlines, baseline]
              }
            : undefined,
        data: s.data
      }))
    })
  }, [series, labels, isDark])

  return <div ref={containerRef} style={{ width: '100%', height }} />
}
