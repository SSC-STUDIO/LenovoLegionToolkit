import { invoke, on } from './bridge'

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
