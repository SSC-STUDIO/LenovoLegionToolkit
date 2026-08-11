import { invoke } from './bridge'

export interface PowerApi {
  restart(): Promise<{ ok: boolean }>
}

export const powerApi: PowerApi = {
  async restart() {
    return invoke<{ ok: boolean }>('power.restart', {})
  }
}
