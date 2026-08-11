import { invoke } from './bridge'

/**
 * Windows power-plan bridge — renderer counterpart of WPF
 * WindowsPowerPlanController.GetPowerPlans(). The host is expected to expose:
 *   powerPlans.getList -> { plans: [{ guid, name, isActive }] }
 */

export interface WindowsPowerPlan {
  guid: string
  name: string
  isActive: boolean
}

export const DEFAULT_POWER_PLAN_GUID = '00000000-0000-0000-0000-000000000000'

export const powerPlansApi = {
  getList: (): Promise<{ plans: WindowsPowerPlan[] }> =>
    invoke<{ plans: WindowsPowerPlan[] }>('powerPlans.getList', {}),
  /** Activates a power plan immediately via `powercfg /setactive` (main process). */
  setActive: (guid: string): Promise<{ ok: boolean }> =>
    invoke<{ ok: boolean }>('powerPlans.setActive', { guid })
}
