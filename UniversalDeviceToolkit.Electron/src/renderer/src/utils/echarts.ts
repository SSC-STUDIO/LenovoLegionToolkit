/**
 * Tree-shaken echarts registration — keeps the renderer bundle small by
 * importing only the chart types/components the app actually uses.
 *
 * SVGRenderer instead of CanvasRenderer: the sensor gauges and trend charts
 * are low-frequency (1 Hz) simple line/gauge visuals, and SVG keeps the DOM
 * smaller than a backing canvas — lower renderer memory. If a future chart
 * needs canvas (dense scatter, large data), swap the renderer here.
 */
import { use, type EChartsCoreOption, type EChartsType } from 'echarts/core'
import { GaugeChart, LineChart } from 'echarts/charts'
import {
  GraphicComponent,
  GridComponent,
  MarkLineComponent,
  TooltipComponent
} from 'echarts/components'
import { SVGRenderer } from 'echarts/renderers'

use([
  GaugeChart,
  LineChart,
  GridComponent,
  TooltipComponent,
  GraphicComponent,
  MarkLineComponent,
  SVGRenderer
])

export type ECharts = EChartsType
export type EChartsOption = EChartsCoreOption
export type { EChartsCoreOption }

export { init } from 'echarts/core'
