import { useEffect, useRef } from 'react'
import * as echarts from 'echarts'

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

export default function TrendChart({
  series,
  labels,
  height = 200
}: TrendChartProps): React.JSX.Element {
  const containerRef = useRef<HTMLDivElement>(null)
  const chartRef = useRef<echarts.ECharts | null>(null)

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
    chart.setOption({
      animation: false,
      tooltip: { trigger: 'axis' },
      legend: { top: 0 },
      grid: { top: 36, left: 8, right: 16, bottom: 8, containLabel: true },
      xAxis: {
        type: 'category',
        boundaryGap: false,
        data: labels
      },
      yAxis: { type: 'value', scale: true },
      series: series.map((s) => ({
        name: s.name,
        type: 'line',
        showSymbol: false,
        smooth: true,
        lineStyle: { width: 2, color: s.color },
        itemStyle: { color: s.color },
        data: s.data
      }))
    })
  }, [series, labels])

  return <div ref={containerRef} style={{ width: '100%', height }} />
}
