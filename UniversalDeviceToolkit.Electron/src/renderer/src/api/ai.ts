import { invoke } from './bridge'

export interface AiStatus {
  supported: boolean
  enabled: boolean
}

export const aiApi = {
  getStatus: (): Promise<AiStatus> => invoke<AiStatus>('ai.getStatus', {}),
  setEnabled: (enabled: boolean): Promise<{ ok: boolean }> =>
    invoke<{ ok: boolean }>('ai.setEnabled', { enabled })
}
