import { create } from 'zustand'
import type { AppStatusBannerSeverity } from '../components/AppStatusBanner'

export interface StatusBannerItem {
  id: string
  severity: AppStatusBannerSeverity
  message: string
  /** Mirrors WPF IsPersistent: non-persistent banners are the first to be hidden on overflow. */
  persistent: boolean
  closable: boolean
  onClick?: () => void
}

interface StatusBannerStore {
  banners: StatusBannerItem[]
  /** Banners the user explicitly dismissed (WPF: Closed event). */
  dismissed: Set<string>
  show: (banner: StatusBannerItem) => void
  /** User dismissed the banner; the id is remembered so it is not shown again this session. */
  hide: (id: string) => void
  /** Programmatic removal (condition no longer true); does NOT remember dismissal. */
  remove: (id: string) => void
  /** Shows max N visible banners; non-persistent banners overflow first (WPF EnforceStatusNotificationLimit). */
  limit: number
}

const DEFAULT_LIMIT = 3

export const useStatusBannerStore = create<StatusBannerStore>((set) => ({
  banners: [],
  dismissed: new Set(),
  limit: DEFAULT_LIMIT,

  show: (banner) => {
    set((state) => {
      if (state.dismissed.has(banner.id)) return state
      const next = [...state.banners.filter((b) => b.id !== banner.id), banner]
      // WPF EnforceStatusNotificationLimit: persistent banners always stay,
      // non-persistent ones overflow first (oldest removed first).
      const persistent = next.filter((b) => b.persistent)
      const maxNonPersistent = Math.max(0, state.limit - persistent.length)
      const nonPersistent = next.filter((b) => !b.persistent).slice(-maxNonPersistent)
      return { banners: [...persistent, ...nonPersistent] }
    })
  },

  hide: (id) => {
    set((state) => ({
      banners: state.banners.filter((b) => b.id !== id),
      dismissed: new Set([...state.dismissed, id])
    }))
  },

  remove: (id) => {
    set((state) => ({
      banners: state.banners.filter((b) => b.id !== id)
    }))
  }
}))

export function clearStatusBanners(): void {
  useStatusBannerStore.setState({ banners: [] })
}
