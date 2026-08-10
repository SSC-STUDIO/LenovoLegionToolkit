import { create } from 'zustand'
import { settingsApi, type SettingsScope } from '../api/settings'

interface SettingsStoreState {
  scopes: Record<string, unknown>
  loading: boolean
  load: (scopes?: SettingsScope[]) => Promise<void>
  setScope: (scope: string, value: unknown) => void
  save: (scopes?: SettingsScope[]) => Promise<void>
}

export const useSettingsStore = create<SettingsStoreState>((set, get) => ({
  scopes: {},
  loading: false,

  load: async (scopes) => {
    set({ loading: true })
    try {
      const result = await settingsApi.getAll(scopes)
      set({ scopes: result.scopes })
    } finally {
      set({ loading: false })
    }
  },

  setScope: (scope, value) => {
    set((state) => ({
      scopes: { ...state.scopes, [scope]: value }
    }))
  },

  save: async (scopes) => {
    const target = scopes ?? (Object.keys(get().scopes) as SettingsScope[])
    await settingsApi.save(target)
    await get().load(target)
  }
}))

/** Subscribe to settings.changed and auto-reload affected scopes. */
export function initSettingsSync(): () => void {
  return settingsApi.onChanged(({ scope }) => {
    const store = useSettingsStore.getState()
    if (store.scopes[scope] !== undefined) {
      void store.load([scope as SettingsScope])
    }
  })
}
