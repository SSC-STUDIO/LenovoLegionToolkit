import { useEffect, useRef } from 'react'
import * as echarts from 'echarts'

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
}

export default function SensorGauge({
  value,
  min = 0,
  max = 100,
  unit,
  label,
  color = '#1677ff'
}: SensorGaugeProps): React.JSX.Element {
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
    const numeric = value != null && Number.isFinite(value) ? value : null
    chart.setOption({
      series: [
        {
          type: 'gauge',
          name: label ?? '',
          min,
          max,
          startAngle: 210,
          endAngle: -30,
          center: ['50%', '62%'],
          radius: '100%',
          progress: {
            show: true,
            width: 10,
            itemStyle: { color }
          },
          axisLine: {
            lineStyle: { width: 10, color: [[1, 'rgba(0, 0, 0, 0.08)']] }
          },
          axisTick: { show: false },
          splitLine: { show: false },
          axisLabel: { show: false },
          pointer: { show: false },
          anchor: { show: false },
          title: {
            show: label != null,
            offsetCenter: [0, '88%'],
            fontSize: 12,
            color: 'rgba(0, 0, 0, 0.45)'
          },
          detail: {
            show: true,
            offsetCenter: [0, '58%'],
            fontSize: 22,
            fontWeight: 600,
            color: '#262626',
            formatter: (): string =>
              numeric == null ? '--' : `${numeric.toFixed(0)}${unit ? ` ${unit}` : ''}`
          },
          data: [{ value: numeric ?? min }]
        }
      ]
    })
  }, [value, min, max, unit, label, color])

  return <div ref={containerRef} style={{ width: '100%', height: 160 }} />
}
