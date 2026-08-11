import { invoke } from './bridge'

/**
 * Software disabler API — mirror of the host `software.*` handlers
 * (SoftwareDisablerHandlers → Lib SoftwareDisabler VantageDisabler /
 * LegionZoneDisabler / FnKeysDisabler).
 */

export type SoftwareDisablerApp = 'vantage' | 'legionZone' | 'fnKeys'

/** Mirror of Lib SoftwareStatus. */
export type SoftwareStatus = 'Enabled' | 'Disabled' | 'NotFound'

export interface SoftwareStatusResult {
  status: SoftwareStatus
  /** Whether the machine is a supported Legion machine (card visibility parity). */
  isLegionMachine: boolean
}

export const softwareApi = {
  getStatus: (app: SoftwareDisablerApp): Promise<SoftwareStatusResult> =>
    invoke<SoftwareStatusResult>('software.getStatus', { app }),

  setEnabled: (app: SoftwareDisablerApp, enabled: boolean): Promise<{ ok: boolean; status: SoftwareStatus }> =>
    invoke<{ ok: boolean; status: SoftwareStatus }>('software.setEnabled', { app, enabled })
}
