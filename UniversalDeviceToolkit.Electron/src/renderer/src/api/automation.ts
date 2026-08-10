import { invoke } from './bridge'

/** A pipeline step/trigger payload. Kept flexible: $type discriminator + opaque fields. */
export interface AutomationStepType extends Record<string, unknown> {
  $type: string
}

/** A single automation pipeline, as serialized by AutomationSettings ($type-preserving). */
export interface AutomationPipeline extends Record<string, unknown> {
  id?: string
  iconName?: string
  name?: string
  trigger?: Record<string, unknown> | null
  steps?: AutomationStepType[]
  isExclusive?: boolean
}

export interface AutomationState {
  isEnabled: boolean
  pipelines: AutomationPipeline[]
}

export interface AutomationApi {
  getState(): Promise<AutomationState>
  setEnabled(enabled: boolean): Promise<{ ok: boolean }>
  savePipelines(pipelines: AutomationPipeline[], isEnabled?: boolean): Promise<{ saved: boolean }>
  runNow(pipelineId: string): Promise<{ ok: boolean }>
  getSupportedSteps(): Promise<{ steps: string[] }>
}

export const automationApi: AutomationApi = {
  async getState() {
    return invoke<AutomationState>('automation.getState', {})
  },

  async setEnabled(enabled) {
    return invoke<{ ok: boolean }>('automation.setEnabled', { enabled })
  },

  async savePipelines(pipelines, isEnabled) {
    const params: Record<string, unknown> = { pipelines }
    if (isEnabled !== undefined) params.isEnabled = isEnabled
    return invoke<{ saved: boolean }>('automation.savePipelines', params)
  },

  async runNow(pipelineId) {
    return invoke<{ ok: boolean }>('automation.runNow', { pipelineId })
  },

  async getSupportedSteps() {
    return invoke<{ steps: string[] }>('automation.getSupportedSteps', {})
  },
}
