import { useEffect, useRef } from 'react'
import * as echarts from 'echarts'
import { useThemeStore } from '../../stores/themeStore'

export interface TrendSeries {
  name: string
  color: string
  data: (number | null)[]
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
  height = 120
}: TrendChartProps): React.JSX.Element {
  const containerRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<echarts.ECharts | null>(null)
  const isDark = useThemeStore((s) => s.themeMode === 'dark')

  useEffect(() => {
    const el = containerRef.current
    if (!el) return
    const chart = echarts.init(el)
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
    const axisColor = isDark ? 'rgba(255, 255, 255, 0.22)' : 'rgba(0, 0, 0, 0.15)'
    const labelColor = isDark ? 'rgba(255, 255, 255, 0.45)' : 'rgba(0, 0, 0, 0.45)'
    chart.setOption({
      animation: false,
      tooltip: { trigger: 'axis' },
      grid: {
        top: 10,
        left: 12,
        right: 12,
        bottom: 24,
        containLabel: true
      },
      xAxis: {
        type: 'category',
        boundaryGap: false,
        data: labels,
        axisLine: { lineStyle: { color: axisColor, width: 1 } },
        axisTick: { show: false },
        axisLabel: { fontSize: 10, color: labelColor, hideOverlap: true, margin: 8 }
      },
      yAxis: {
        type: 'value',
        scale: true,
        axisLine: { show: false },
        axisTick: { show: false },
        axisLabel: { fontSize: 10, color: labelColor, margin: 8 },
        splitLine: { lineStyle: { color: axisColor, opacity: 0.48 } }
      },
      series: series.map((s) => ({
        name: s.name,
        type: 'line' as const,
        showSymbol: false,
        smooth: true,
        lineStyle: { width: 2, color: s.color },
        itemStyle: { color: s.color },
        areaStyle: {
          color: {
            type: 'linear' as const,
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              { offset: 0, color: withAlpha(s.color, 0.3) },
              { offset: 0.48, color: withAlpha(s.color, 0.16) },
              { offset: 1, color: withAlpha(s.color, 0.04) }
            ]
          }
        },
        data: s.data
      }))
    })
  }, [series, labels, isDark])

  return <div ref={containerRef} style={{ width: '100%', height }} />
}
