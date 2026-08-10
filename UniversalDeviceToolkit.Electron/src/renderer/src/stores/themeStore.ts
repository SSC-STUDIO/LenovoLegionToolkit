import { create } from 'zustand'

export type ThemeMode = 'light' | 'dark'

export interface ThemeStore {
  themeMode: ThemeMode
  colorPrimary?: string
  setThemeMode: (mode: ThemeMode) => void
  setAccent: (color?: string) => void
}

export const useThemeStore = create<ThemeStore>()((set) => ({
  themeMode: 'dark',
  colorPrimary: undefined,
  setThemeMode: (themeMode) => set({ themeMode }),
  setAccent: (colorPrimary) => set({ colorPrimary })
}))
