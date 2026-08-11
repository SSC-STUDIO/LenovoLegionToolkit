import { Suspense, lazy } from 'react'
import { Spin } from 'antd'
import { Navigate, Route, Routes } from 'react-router-dom'
import AppLayout from './layout/AppLayout'

const DashboardPage = lazy(() => import('./pages/DashboardPage'))
const SettingsPage = lazy(() => import('./pages/SettingsPage'))
const AutomationPage = lazy(() => import('./pages/AutomationPage'))
const KeyboardBacklightPage = lazy(() => import('./pages/KeyboardBacklightPage'))
const MacroPage = lazy(() => import('./pages/MacroPage'))
const WindowsOptimizationPage = lazy(() => import('./pages/WindowsOptimizationPage'))
const PluginExtensionsPage = lazy(() => import('./pages/PluginExtensionsPage'))
const AboutPage = lazy(() => import('./pages/AboutPage'))

function PageFallback(): React.JSX.Element {
  return (
    <div style={{ display: 'flex', justifyContent: 'center', padding: 48 }}>
      <Spin size="large" />
    </div>
  )
}

export default function App(): React.JSX.Element {
  return (
    <Suspense fallback={<PageFallback />}>
      <Routes>
        <Route element={<AppLayout />}>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/settings" element={<SettingsPage />} />
          <Route path="/automation" element={<AutomationPage />} />
          <Route path="/keyboard" element={<KeyboardBacklightPage />} />
          <Route path="/macro" element={<MacroPage />} />
          <Route path="/optimization" element={<WindowsOptimizationPage />} />
          <Route path="/plugins" element={<PluginExtensionsPage />} />
          <Route path="/about" element={<AboutPage />} />
        </Route>
      </Routes>
    </Suspense>
  )
}
