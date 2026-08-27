import { Suspense, lazy, type ReactNode } from 'react'
import { Spin } from 'antd'
import { Navigate, Route, Routes } from 'react-router-dom'
import { isInstallerOptionalFeatureEnabled, type InstallerOptionalFeature } from '../../shared/installer-selection'
import { CapabilityGate } from './layout/CapabilityRedirect'
import AppLayout from './layout/AppLayout'

const DashboardPage = lazy(() => import('./pages/DashboardParityPage'))
const SettingsPage = lazy(() => import('./pages/SettingsPage'))
const AutomationPage = lazy(() => import('./pages/AutomationPage'))
const KeyboardBacklightPage = lazy(() => import('./pages/KeyboardBacklightPage'))
const MacroPage = lazy(() => import('./pages/MacroPage'))
const WindowsOptimizationPage = lazy(() => import('./pages/WindowsOptimizationPage'))
const AboutPage = lazy(() => import('./pages/AboutPage'))

function PageFallback(): React.JSX.Element {
  return (
    <div style={{ display: 'flex', justifyContent: 'center', padding: 48 }}>
      <Spin size="large" />
    </div>
  )
}

function InstalledFeatureRoute({
  feature,
  children
}: {
  feature: InstallerOptionalFeature
  children: ReactNode
}): React.JSX.Element {
  if (!isInstallerOptionalFeatureEnabled(window.bridge?.installerSelection?.features, feature)) {
    return <Navigate to="/dashboard" replace />
  }
  return <>{children}</>
}

export default function App(): React.JSX.Element {
  return (
    <Suspense fallback={<PageFallback />}>
      <CapabilityGate>
        <Routes>
          <Route element={<AppLayout />}>
            <Route path="/" element={<Navigate to="/dashboard" replace />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/settings" element={<SettingsPage />} />
            <Route path="/automation" element={<InstalledFeatureRoute feature="automation"><AutomationPage /></InstalledFeatureRoute>} />
            <Route path="/keyboard" element={<InstalledFeatureRoute feature="keyboard"><KeyboardBacklightPage /></InstalledFeatureRoute>} />
            <Route path="/macro" element={<InstalledFeatureRoute feature="macro"><MacroPage /></InstalledFeatureRoute>} />
            <Route path="/optimization" element={<InstalledFeatureRoute feature="windowsOptimization"><WindowsOptimizationPage /></InstalledFeatureRoute>} />
            <Route path="/about" element={<AboutPage />} />
          </Route>
        </Routes>
      </CapabilityGate>
    </Suspense>
  )
}
