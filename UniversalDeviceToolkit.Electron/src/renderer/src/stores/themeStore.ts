import { create } from 'zustand'

export type ThemeMode = 'light' | 'dark'

export interface ThemeStore {
  themeMode: ThemeMode
  colorPrimary?: string
  /** UI scale factor (1.0 = default). Independent of Windows display scaling. */
  uiScale: number
  setThemeMode: (mode: ThemeMode) => void
  setAccent: (color?: string) => void
  setUiScale: (scale: number) => void
}

/**
 * Selectable UI scale levels, aligned with the WPF app
 * (Compact 0.90 / Standard 1.0 / Large 1.10 / ExtraLarge 1.25).
 */
export const UI_SCALE_OPTIONS = [0.9, 1, 1.1, 1.25] as const
export type UiScale = (typeof UI_SCALE_OPTIONS)[number]

const UI_SCALE_STORAGE_KEY = 'udt-ui-scale'

function readStoredUiScale(): number {
  try {
    const stored = localStorage.getItem(UI_SCALE_STORAGE_KEY)
    const parsed = stored != null ? Number(stored) : NaN
    if (Number.isFinite(parsed) && (UI_SCALE_OPTIONS as readonly number[]).includes(parsed)) {
      return parsed
    }
  } catch {
    /* ignore quota / private mode */
  }
  return 1
}

/**
 * Applies the UI scale to the whole interface.
 *
 * The app stylesheet is px-based, so changing the root font-size alone would
 * not scale anything; CSS `zoom` (Chromium / Electron) scales layout and text
 * together and is the layout-adaptive equivalent of the WPF AppScale.
 * Scale 1.0 resets the document to its default rendering.
 */
export function applyUiScale(scale: number): void {
  const html = document.documentElement
  if (scale === 1) {
    html.style.removeProperty('zoom')
  } else {
    html.style.zoom = String(scale)
  }
}

export const useThemeStore = create<ThemeStore>()((set) => ({
  themeMode: 'dark',
  colorPrimary: undefined,
  uiScale: readStoredUiScale(),
  setThemeMode: (themeMode) => set({ themeMode }),
  setAccent: (colorPrimary) => set({ colorPrimary }),
  setUiScale: (uiScale) => {
    set({ uiScale })
    try {
      localStorage.setItem(UI_SCALE_STORAGE_KEY, String(uiScale))
    } catch {
      /* ignore quota / private mode */
    }
    applyUiScale(uiScale)
  }
}))

// Apply the persisted scale once at startup (themeStore is imported by main.tsx
// before the first render, so the whole interface is scaled from launch).
applyUiScale(useThemeStore.getState().uiScale)
