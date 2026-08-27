import { resolve } from 'path'
import { defineConfig } from 'electron-vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  main: {},
  preload: {},
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
          // Split only modules that are already in the module graph. Naming a
          // barrel package (especially @fluentui/react-icons) as a chunk entry
          // can pull the entire export surface into the renderer.
          manualChunks(id) {
            if (id.includes('node_modules/echarts')) return 'charts'
            if (id.includes('node_modules/@fluentui/react-icons')) return 'icons'
            if (
              id.includes('node_modules/antd') ||
              id.includes('node_modules/@ant-design') ||
              id.includes('node_modules/react-dom') ||
              id.includes('node_modules/react-router') ||
              id.includes('node_modules/react/')
            ) {
              return 'vendor'
            }
            return undefined
          }
        }
      }
    }
  }
})
