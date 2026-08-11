/**
 * Tree-shaken echarts registration — keeps the renderer bundle small by
 * importing only the chart types/components the app actually uses.
 */
import { use, type EChartsCoreOption, type EChartsType } from 'echarts/core'
import { GaugeChart, LineChart } from 'echarts/charts'
import {
  DataZoomComponent,
  GraphicComponent,
  GridComponent,
  MarkLineComponent,
  TooltipComponent
} from 'echarts/components'
import { CanvasRenderer } from 'echarts/renderers'

use([
  GaugeChart,
  LineChart,
  GridComponent,
  TooltipComponent,
  DataZoomComponent,
  GraphicComponent,
  MarkLineComponent,
  CanvasRenderer
])

export type ECharts = EChartsType
export type EChartsOption = EChartsCoreOption
export type { EChartsCoreOption }

export { init } from 'echarts/core'
