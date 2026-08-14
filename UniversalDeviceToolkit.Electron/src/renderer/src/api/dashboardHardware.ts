import { invoke } from './bridge'

export type DiscreteGpuState =
  | 'Unknown'
  | 'NvidiaGpuNotFound'
  | 'MonitorConnected'
  | 'Active'
  | 'Inactive'
  | 'PoweredOff'

export interface DashboardHardwareState {
  discreteGpu: {
    supported: boolean
    state: DiscreteGpuState
    performanceState?: string | null
    processes: string[]
  }
  overclockDiscreteGpu: {
    supported: boolean
    enabled: boolean
    coreDeltaMhz: number
    memoryDeltaMhz: number
    maxCoreDeltaMhz: number
    maxMemoryDeltaMhz: number
  }
  turnOffMonitors: {
    supported: boolean
  }
}

export const dashboardHardwareApi = {
  getState(): Promise<DashboardHardwareState> {
    return invoke<DashboardHardwareState>('dashboardHardware.getState', {})
  },
  killGpuProcesses(): Promise<{ ok: boolean }> {
    return invoke<{ ok: boolean }>('dashboardHardware.killGpuProcesses', {})
  },
  restartGpu(): Promise<{ ok: boolean }> {
    return invoke<{ ok: boolean }>('dashboardHardware.restartGpu', {})
  },
  setOverclockEnabled(enabled: boolean): Promise<{ ok: boolean; enabled: boolean }> {
    return invoke<{ ok: boolean; enabled: boolean }>('dashboardHardware.setOverclockEnabled', { enabled })
  },
  setOverclock(coreDeltaMhz: number, memoryDeltaMhz: number): Promise<{ ok: boolean }> {
    return invoke<{ ok: boolean }>('dashboardHardware.setOverclock', { coreDeltaMhz, memoryDeltaMhz })
  },
  setMonitoring(enabled: boolean): Promise<{ ok: boolean; monitoring: boolean }> {
    return invoke<{ ok: boolean; monitoring: boolean }>('dashboardHardware.setMonitoring', { enabled })
  },
  turnOffMonitors(): Promise<{ ok: boolean }> {
    return invoke<{ ok: boolean }>('dashboardHardware.turnOffMonitors', {})
  }
}
