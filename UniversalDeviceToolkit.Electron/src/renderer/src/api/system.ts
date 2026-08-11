import { invoke } from './bridge'

/**
 * Machine information — mirror of the host `system.info` handler
 * (UniversalDeviceToolkit.Host → Compatibility.GetMachineInformationAsync).
 * The WPF MachineInformation exposes a serial number too, but the host does
 * not currently forward it; consumers fall back to "-".
 */
export interface SystemInfo {
  vendor?: string | null
  model?: string | null
  machineType?: string | null
  biosVersion?: string | null
  serialNumber?: string | null
  isCompatible?: boolean
}

/** Mirror of Lib PowerAdapterStatus (Power.IsPowerAdapterConnectedAsync). */
export type PowerAdapterStatus = 'Connected' | 'ConnectedLowWattage' | 'Disconnected'

export const systemApi = {
  info: (): Promise<SystemInfo> => invoke<SystemInfo>('system.info'),
  /** Power adapter connection state (PowerModeControl warning parity). */
  powerAdapterStatus: (): Promise<{ status: PowerAdapterStatus }> =>
    invoke<{ status: PowerAdapterStatus }>('system.powerAdapterStatus')
}
