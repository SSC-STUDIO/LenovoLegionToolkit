import { resolve } from 'path'
import { defineConfig } from 'electron-vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  main: {},
  preload: {
    build: {
      rollupOptions: {
        input: {
          index: resolve(__dirname, 'src/preload/index.ts'),
          // Guest preload for plugin web pages hosted in <webview> elements.
          'plugin-host': resolve(__dirname, 'src/preload/plugin-host.ts')
        }
      }
    }
  },
  renderer: {
    resolve: {
      alias: {
        '@renderer': resolve('src/renderer/src')
      }
    },
    plugins: [react()],
    build: {
      rollupOptions: {
        output: {
          // Split heavy vendor libs out of the main entry chunk so each loads
          // on demand and the renderer keeps its memory footprint smaller:
          // antd + React → vendor, echarts → charts (only imported by the
          // dashboard gauges/trends, which are lazy routes).
          manualChunks: {
            vendor: ['react', 'react-dom', 'react-router-dom', 'antd'],
            charts: ['echarts/core', 'echarts/charts', 'echarts/components', 'echarts/renderers'],
            icons: ['@fluentui/react-icons']
          }
        }
      }
    }
  }
})
