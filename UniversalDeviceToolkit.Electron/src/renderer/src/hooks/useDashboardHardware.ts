import { useCallback, useEffect, useState } from 'react'
import {
  dashboardHardwareApi,
  type DashboardHardwareState
} from '../api/dashboardHardware'

export interface DashboardHardwareResult {
  state: DashboardHardwareState | null
  error: string | null
  refresh: () => Promise<void>
}

export function useDashboardHardware(): DashboardHardwareResult {
  const [state, setState] = useState<DashboardHardwareState | null>(null)
  const [error, setError] = useState<string | null>(null)

  const refresh = useCallback(async (): Promise<void> => {
    try {
      const next = await dashboardHardwareApi.getState()
      setState(next)
      setError(null)
    } catch (reason) {
      setError((reason as Error).message)
    }
  }, [])

  useEffect(() => {
    const initialLoad = window.setTimeout(() => void refresh(), 0)
    const interval = window.setInterval(() => void refresh(), 5_000)
    return () => {
      window.clearTimeout(initialLoad)
      window.clearInterval(interval)
    }
  }, [refresh])

  return { state, error, refresh }
}
