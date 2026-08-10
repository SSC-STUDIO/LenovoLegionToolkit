import { Route, Routes } from 'react-router-dom'
import AppLayout from './layout/AppLayout'
import AboutPage from './pages/AboutPage'
import AutomationPage from './pages/AutomationPage'
import DashboardPage from './pages/DashboardPage'
import HomePage from './pages/HomePage'
import KeyboardBacklightPage from './pages/KeyboardBacklightPage'
import MacroPage from './pages/MacroPage'
import PluginExtensionsPage from './pages/PluginExtensionsPage'
import SettingsPage from './pages/SettingsPage'
import WindowsOptimizationPage from './pages/WindowsOptimizationPage'

export default function App(): React.JSX.Element {
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route path="/" element={<HomePage />} />
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
  )
}
