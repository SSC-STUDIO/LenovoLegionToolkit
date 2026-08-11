import { invoke, on } from './bridge'
import type { DashboardGroup } from './dashboard'

export type SettingsScope =
  | 'application'
  | 'osd'
  | 'hardwareSensors'
  | 'balanceMode'
  | 'godMode'
  | 'gpuOverclock'
  | 'integrations'
  | 'lampArray'
  | 'fanCurves'
  | 'packageDownloader'
  | 'rgbKeyboard'
  | 'spectrumKeyboard'
  | 'sunriseSunset'
  | 'updateCheck'
  | 'networkAcceleration'
  | 'batteryHealthAlerts'
  | 'dashboard'

export const settingsApi = {
  get: (scope: SettingsScope, path?: string) =>
    invoke<{ scope: string; value: unknown }>('settings.get', path ? { scope, path } : { scope }),
  getAll: (scopes?: SettingsScope[]) =>
    invoke<{ scopes: Record<string, unknown> }>('settings.getAll', scopes ? { scopes } : {}),
  set: (scope: SettingsScope, value: unknown) => invoke('settings.set', { scope, value }),
  save: (scopes?: SettingsScope[]) =>
    invoke<{ saved: string[] }>('settings.save', scopes ? { scopes } : {}),
  onChanged: (cb: (data: { scope: string; reason: string }) => void) => on('settings.changed', cb)
}

/**
 * Settings store models mirroring the WPF settings files. The host persists
 * these stores to JSON (dashboard.json / hardware_sensors.json / plugins.json);
 * these projections document the renderer-side schema. The dashboard store is
 * also exposed through dashboardApi (api/dashboard.ts) and the sensor store
 * through sensorsApi (api/sensors.ts).
 */

/** Settings/DashboardSettings.cs — dashboard.json (schema v4). */
export interface DashboardSettingsStore {
  /** Persisted schema version; the host normalizes legacy layouts on load. */
  schemaVersion?: number | null
  showSensors: boolean
  sensorsRefreshIntervalSeconds: number
  groups: DashboardGroup[] | null
}

/** Settings/HardwareSensorSettings.cs — hardware_sensors.json. */
export interface HardwareSensorSettingsStore {
  selectedGpuIsIgpu: boolean
  showCpuAverageFrequency: boolean
  displayMemoryInGigabytes: boolean
  /** Defaults to ['CPU', 'Battery', 'GPU']. */
  visibleSections: string[]
  /** Defaults to ['CPU', 'Battery', 'GPU']. */
  sectionOrder: string[]
}

/** Settings/PluginSettings.cs — plugins.json (plugin ID → culture, e.g. 'zh-Hans'). */
export interface PluginSettingsStore {
  /** Missing entry = plugin uses the application default language. */
  pluginLanguages: Record<string, string>
}
